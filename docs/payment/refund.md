# Booking Refund Design

## 1. Document purpose

Tài liệu này mô tả thiết kế `refund` cho booking trong AutoWash Pro, tập trung vào các trường hợp booking bị hủy sau khi hệ thống đã thu tiền từ `wallet`.

Mục tiêu:

- xác định khi nào booking được hoàn tiền
- xác định số tiền được hoàn
- xác định cách ghi nhận refund vào `wallet` và `transaction`
- thống nhất flow giữa customer cancel, admin cancel và auto-cancel

Tài liệu này chỉ tập trung vào `refund` nội bộ về ví. Không mô tả refund ra ngân hàng hoặc hoàn tiền qua SePay.

## 2. Scope

Trong phạm vi tài liệu này:

- customer tự hủy booking
- admin hủy booking
- auto-cancel do quá hạn check-in
- hủy do branch hoặc system không thể phục vụ
- refund cho trường hợp bị trừ tiền thừa hoặc cần compensation sau lỗi hệ thống

Ngoài phạm vi:

- refund ra tài khoản ngân hàng
- refund tự động qua SePay
- chargeback/dispute từ phía ngân hàng
- thiết kế UI chi tiết cho frontend

## 3. Current backend state

Theo code hiện có trong repo ngày `2026-08-04`:

- `Wallet` là nguồn tiền thanh toán booking.
- Khi tạo booking, backend trừ tiền đặt cọc từ ví và tạo `TransactionType.Deposit`.
- Khi customer check-in, backend trừ phần tiền còn lại và tạo `TransactionType.FullPayment`.
- Hệ thống đã có `TransactionType.Refund` trong enum nhưng chưa có flow refund hoàn chỉnh đang được dùng trong `BookingService`.
- Customer cancel đang có endpoint:
  - `POST /api/v1/bookings/{id}/cancel`
- Admin cancel đang có endpoint:
  - cancel booking thủ công trong `AdminController`
- Auto-cancel job đang có:
  - `ProcessBookingAutoCancelJob`
  - job này chuyển booking `Confirmed` quá giờ check-in sang `Cancelled`
  - hiện chưa cộng lại tiền về ví

Hành vi cancel hiện tại:

- customer chỉ được hủy booking đang `Confirmed`
- không cho hủy booking `InProgress`, `Completed`, `Cancelled`
- khi cancel hiện chỉ:
  - đổi trạng thái booking
  - set `CancelledAt`
  - release voucher nếu có
  - tạo notification
- chưa có bước:
  - tính refund amount
  - cộng tiền về ví
  - insert `TransactionType.Refund`

## 4. Design principles

- Refund chỉ áp dụng trên số tiền hệ thống đã thu thực tế từ `wallet`.
- Refund luôn đổ về `wallet`, không đi ra ngoài hệ thống.
- Refund phải được ghi nhận bằng `TransactionType.Refund`.
- Không sửa ngược transaction `Deposit` hoặc `FullPayment` cũ.
- Refund phải idempotent: cùng một booking và cùng một lý do refund không được cộng ví hai lần.
- Flow refund phải chạy cùng DB transaction với booking cancellation để tránh lệch dữ liệu.

## 5. Refund rule matrix

Thiết kế refund dùng rule matrix theo tình huống:

| Scenario | Actor | Booking status | Refund percent | Notes |
| --- | --- | --- | --- | --- |
| Customer cancel trước deadline | `customer` | `Confirmed` | `100%` | Hoàn toàn bộ số tiền đã thu |
| Customer cancel sau deadline | `customer` | `Confirmed` | `0%` | Không hoàn |
| Admin cancel | `admin` | `Pending`, `Confirmed`, `CheckIn` | `100%` | Hoàn toàn bộ số tiền đã thu |
| Branch/system cannot serve | `system` hoặc `admin` | `Pending`, `Confirmed`, `CheckIn`, `InProgress` nếu chưa phục vụ | `100%` | Dùng cho sự cố vận hành |
| Auto-cancel do no check-in | `system` | `Confirmed` | `0%` | Xem là vi phạm điều kiện check-in |
| Duplicate charge / overcharge | `system` hoặc `admin` | bất kỳ | phần bị thu thừa | Refund compensation |
| Booking create failed after charge | `system` | bất kỳ | `100%` | Refund compensation |

Quy tắc mặc định đã chốt:

- Customer cancel: refund theo `CancellationDeadlineHours`
- Admin cancel: luôn hoàn `100%`
- System/branch fault: luôn hoàn `100%`
- Auto-cancel do no check-in: không hoàn

## 6. Refund amount calculation

Refund phải tính trên `totalPaidAmount`, không tính trực tiếp từ `FinalPrice`.

### 6.1 Total paid amount

`totalPaidAmount` là tổng các giao dịch tiền ra đã thu thành công cho booking:

- `TransactionType.Deposit`
- `TransactionType.FullPayment`

Công thức:

```text
totalPaidAmount = sum(Deposit + FullPayment) của booking
refundAmount = totalPaidAmount * refundPercent / 100
```

### 6.2 Expected behavior by booking phase

- Trước check-in:
  - thông thường booking mới chỉ có `Deposit`
  - nếu refund `100%` thì hoàn lại đúng số tiền đặt cọc
- Sau check-in nhưng service chưa được phục vụ và admin/system hủy:
  - booking có thể đã có cả `Deposit` và `FullPayment`
  - refund `100%` thì hoàn lại toàn bộ số đã thu
- Auto-cancel no check-in:
  - booking thường chỉ có `Deposit`
  - refund amount = `0`

### 6.3 Deadline for customer refund

Mốc so sánh dùng config hiện có:

- `CancellationDeadlineHours`

Rule:

- nếu `now < booking.StartTime - CancellationDeadlineHours` thì customer được refund `100%`
- nếu `now >= booking.StartTime - CancellationDeadlineHours` thì customer refund `0%`

## 7. Data model and ledger design

### 7.1 Reuse existing `Transaction`

Thiết kế phase đầu dùng luôn bảng `Transaction` hiện có để lưu refund ledger.

Khi refund thành công, insert một transaction mới:

- `Type = Refund`
- `Amount = refundAmount`
- `CustomerId = booking.CustomerId`
- `BookingId = booking.Id`
- `Description = refund description`
- `TransactionDate = DateTime.UtcNow`
- `Provider = Internal`
- `TransferType = In`
- `CreatedAt = DateTimeOffset.UtcNow`

### 7.2 Recommended metadata extension

Để audit refund tốt hơn, nên mở rộng `Transaction` cho refund metadata:

- `RefundReasonCode`
- `RefundActorType`
- `RefundActorId`
- `RefundSourceTransactionType`

Định nghĩa đề xuất:

- `RefundReasonCode`
  - `customer_cancel_before_deadline`
  - `customer_cancel_after_deadline`
  - `admin_cancel`
  - `system_fault`
  - `branch_fault`
  - `auto_cancel_no_checkin`
  - `duplicate_charge`
  - `booking_create_compensation`
- `RefundActorType`
  - `customer`
  - `admin`
  - `system`
- `RefundSourceTransactionType`
  - `Deposit`
  - `FullPayment`
  - `Mixed`

Nếu team chưa muốn đổi schema lớn ngay, phase đầu vẫn có thể dùng `Description` để lưu semantic chính, nhưng đây chỉ là giải pháp tạm.

## 8. API and service design

### 8.1 Customer cancel API

Giữ endpoint hiện có:

- `POST /api/v1/bookings/{id}/cancel`

Request body:

```json
{
  "reason": "Khong the den dung gio"
}
```

Backend tự tính refund. Client không được truyền `refundAmount`.

### 8.2 Admin cancel API

Giữ flow admin cancel hiện có trong `AdminService`.

Admin cancel dùng cùng refund engine với actor là `admin`.

### 8.3 Response extension

Response cancel nên mở rộng để trả rõ kết quả refund:

```json
{
  "id": "booking-guid",
  "status": "cancelled",
  "cancelledAt": "2026-08-04T10:00:00+07:00",
  "refundApplied": true,
  "refundAmount": 50000,
  "refundTransactionId": "refund-transaction-guid",
  "refundReasonCode": "customer_cancel_before_deadline",
  "message": "Booking cancelled successfully"
}
```

Field đề xuất:

- `refundApplied`
- `refundAmount`
- `refundTransactionId`
- `refundReasonCode`

### 8.4 Shared refund service behavior

Nên tách logic refund thành shared method để customer cancel, admin cancel và auto-cancel cùng dùng chung.

Đề xuất service methods:

- `CalculateRefundDecision(...)`
- `ApplyRefundAsync(...)`

`RefundDecision` nên chứa:

- `IsRefundable`
- `RefundAmount`
- `RefundPercent`
- `ReasonCode`
- `ActorType`
- `ShouldCreateRefundTransaction`

## 9. Booking cancellation and refund flow

### 9.1 Customer cancel flow

1. Customer gọi `POST /api/v1/bookings/{id}/cancel`.
2. Backend validate:
   - booking tồn tại
   - booking thuộc customer hiện tại
   - booking chưa `Cancelled`
   - booking chưa `InProgress`
   - booking chưa `Completed`
   - booking đang `Confirmed`
3. Backend đọc `CancellationDeadlineHours`.
4. Backend tính `RefundDecision`.
5. Backend mở DB transaction:
   - update booking sang `Cancelled`
   - set `CancelledAt`
   - release voucher nếu có
   - nếu `refundAmount > 0`:
     - cộng `wallet.Balance`
     - insert `TransactionType.Refund`
   - insert notification
6. Commit.
7. Trả response kèm refund result.

### 9.2 Admin cancel flow

1. Admin gọi cancel endpoint.
2. Backend validate booking chưa `Completed` và chưa `Cancelled`.
3. Backend xác định lý do cancel:
   - admin manual cancel
   - branch fault
   - system fault
4. Backend tính `RefundDecision = 100%` trên số tiền đã thu.
5. Backend chạy DB transaction tương tự customer cancel.
6. Trả response kèm refund result.

### 9.3 Auto-cancel flow

1. `ProcessBookingAutoCancelJob` quét các booking `Confirmed` quá thời gian check-in.
2. Backend đổi booking sang `Cancelled`.
3. Release voucher nếu có.
4. Không refund.
5. Tạo notification auto-cancel.

Trong phase refund này, auto-cancel phải đi qua cùng refund engine nhưng cho ra:

- `IsRefundable = false`
- `RefundAmount = 0`
- `ReasonCode = auto_cancel_no_checkin`

## 10. Transaction and idempotency rules

### 10.1 Atomicity

Cancel booking và refund phải cùng một DB transaction:

- booking status update
- voucher release
- wallet balance update
- refund transaction insert
- notification insert

Nếu một bước fail trước commit, rollback toàn bộ.

### 10.2 Idempotency

Phải chống refund trùng:

- nếu booking đã `Cancelled`, không xử lý lại
- nếu booking đã có refund transaction hợp lệ cho cùng scenario, không cộng ví lại

Rule đề xuất:

- với refund tự động do cancel flow, chỉ cho phép tối đa một `TransactionType.Refund` cho mỗi `BookingId` trong mỗi incident refund
- nếu cần nhiều refund riêng lẻ cho `duplicate_charge` hoặc `overcharge`, phải có reason code riêng để phân biệt

### 10.3 Compensation cases

Hệ thống cần hỗ trợ refund đặc biệt cho:

- booking create failed sau khi đã trừ ví
- duplicate charge
- overcharge

Các case này không đi qua flow customer cancel thường, nhưng vẫn phải:

- cộng tiền về ví
- tạo `TransactionType.Refund`
- ghi rõ `RefundReasonCode`

## 11. Notification behavior

Khi refund phát sinh, notification nên thể hiện rõ:

- booking đã bị hủy
- số tiền refund nếu có
- lý do refund hoặc lý do không hoàn

Gợi ý nội dung:

- customer cancel trước deadline:
  - `Your booking has been cancelled. Refund amount: 50,000 VND has been returned to your wallet.`
- customer cancel sau deadline:
  - `Your booking has been cancelled. No refund was applied because the cancellation deadline has passed.`
- auto-cancel:
  - `Your booking has been automatically cancelled due to no check-in. No refund was applied.`

## 12. Configuration

### 12.1 Existing config reused

- `CancellationDeadlineHours`

### 12.2 Recommended new config

Để tránh hardcode trong backend và FE, nên thêm:

- `CustomerCancelRefundPercentBeforeDeadline = 100`
- `CustomerCancelRefundPercentAfterDeadline = 0`
- `AdminCancelRefundPercent = 100`
- `SystemCancelRefundPercent = 100`
- `AutoCancelRefundPercent = 0`

Nếu phase đầu chưa thêm config mới, có thể hardcode trong service. Tuy nhiên thiết kế target vẫn nên là config-driven.

## 13. Testing notes

Test cases tối thiểu:

1. Customer cancel trước deadline:
   - booking `Confirmed`
   - refund `100% Deposit`
   - wallet tăng đúng số tiền
   - tạo `TransactionType.Refund`

2. Customer cancel sau deadline:
   - booking bị từ chối hoặc bị hủy với `0 refund` theo business rule cuối cùng
   - không tạo refund transaction

3. Admin cancel booking:
   - refund `100%`
   - release voucher nếu có
   - tạo notification

4. Auto-cancel no check-in:
   - booking thành `Cancelled`
   - không refund

5. Booking có cả `Deposit` và `FullPayment`:
   - admin/system cancel hoàn lại đúng tổng đã thu

6. Duplicate cancel request:
   - không cộng ví lần hai
   - không insert refund transaction thứ hai

7. Compensation case:
   - duplicate charge hoặc booking create fail
   - refund được ghi nhận bằng `TransactionType.Refund`

8. Transaction history:
   - `GET /api/v1/transaction` hiển thị được refund

## 14. Open questions

- Có cần cho customer cancel sau deadline nhưng vẫn được hủy với `0 refund`, hay phải chặn hủy hoàn toàn?
- Có cần support partial refund nhiều mức theo khoảng thời gian, ví dụ `100%`, `50%`, `0%`?
- Có cần tách bảng `RefundRecord` riêng ở phase sau để audit chi tiết hơn?
- Với `InProgress`, điều kiện nào đủ để coi là `service chưa được phục vụ` và cho phép admin/system refund `100%`?

## 15. Recommended implementation order

1. Tạo `RefundDecision` và shared refund calculation logic.
2. Bổ sung refund flow vào customer cancel.
3. Dùng lại refund flow cho admin cancel.
4. Cập nhật auto-cancel để đi qua cùng refund decision path với `0 refund`.
5. Insert `TransactionType.Refund` và cộng lại `wallet`.
6. Mở rộng response cancel để trả refund result.
7. Thêm test cho refund matrix và idempotency.

## 16. Summary

Thiết kế refund phù hợp nhất cho hệ thống hiện tại là:

- refund nội bộ về `wallet`
- dùng `TransactionType.Refund` để ghi ledger
- customer cancel hoàn tiền theo deadline
- admin/system fault hoàn `100%`
- auto-cancel do no check-in không hoàn
- cancel và refund chạy cùng DB transaction để tránh lệch dữ liệu

Thiết kế này bám logic thanh toán booking hiện có bằng ví, không yêu cầu refund ra ngoài hệ thống, và có thể triển khai theo từng bước nhỏ mà không phá flow booking hiện tại.
