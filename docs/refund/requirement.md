# Refund Flow Requirement

## 1. Document Purpose

Tài liệu này mô tả refund flow hiện đang được implement trong backend và cách dùng các API liên quan để:

- customer hủy booking và nhận refund nếu đủ điều kiện
- admin hủy booking và hoàn tiền về wallet của customer
- tra cứu refund transaction từ transaction history
- test flow refund trên Swagger

Tài liệu này ưu tiên behavior đang chạy trong source code. Nếu có khác biệt với tài liệu thiết kế cũ, source code được xem là nguồn sự thật cho phase hiện tại.

## 2. Scope

Bao gồm:

- customer cancel booking refund
- admin cancel booking refund
- refund transaction record trong bảng `transaction`
- transaction history API cho customer
- auth rule của các endpoint refund

Không bao gồm:

- refund ra ngân hàng
- refund qua SePay
- partial refund nhiều mức
- compensation flow ngoài booking cancellation

## 3. Business Goal

Khi booking bị hủy trong các tình huống cho phép, hệ thống phải:

- tính số tiền refund từ số tiền đã thu thực tế của booking
- cộng tiền refund vào `wallet` nội bộ của customer
- tạo một dòng `transaction` mới với `Type = Refund`
- trả kết quả refund trong response của API cancel booking

## 4. Refund Source Of Truth

Refund chỉ tính trên tổng số tiền đã thu thực tế của booking:

- `TransactionType.Deposit`
- `TransactionType.FullPayment`

Backend không nhận `refundAmount` từ client.

Refund amount được backend tự tính.

## 5. Refund Rules

### 5.1 Customer cancel booking

Customer chỉ được hủy booking khi booking đang ở trạng thái `Confirmed`.

Refund rule:

- Nếu customer hủy trước mốc `booking.StartTime - CancellationDeadlineHours`:
  - refund `100%` tổng tiền đã thu
  - `RefundReasonCode = "customer_cancel_before_deadline"`
- Nếu customer hủy từ mốc đó trở đi:
  - refund `0%`
  - `RefundReasonCode = "customer_cancel_after_deadline"`

`CancellationDeadlineHours` được lấy từ bảng `SystemConfig`.

### 5.2 Admin cancel booking

Admin cancel booking sẽ refund `100%` tổng tiền đã thu thực tế của booking.

- `RefundReasonCode = "admin_cancel"`

### 5.3 Auto-cancel

Flow auto-cancel hiện tại đi theo hướng không refund:

- refund `0%`
- `RefundReasonCode = "auto_cancel_no_checkin"`

## 6. Conditions That Block Refund

Refund transaction sẽ không được tạo trong các trường hợp sau:

- booking bị customer hủy sau deadline
- booking không có tiền đã thu thực tế
- booking không ở trạng thái hợp lệ để cancel
- booking đã bị cancel trước đó

Customer không thể cancel các booking có trạng thái:

- `InProgress`
- `Completed`

Customer cũng không thể cancel booking nếu trạng thái khác `Confirmed`.

## 7. Data Model Usage

Refund được lưu bằng chính bảng `transaction`.

### 7.1 Refund transaction fields

Khi refund thành công, backend tạo một row mới với các giá trị chính sau:

- `Type = Refund`
- `Status = Succeeded`
- `TransferType = In`
- `Provider = Internal`
- `CustomerId = customer profile id`
- `BookingId = booking id`
- `Amount = refund amount`
- `WalletBalanceBefore = số dư ví trước refund`
- `WalletBalanceAfter = số dư ví sau refund`
- `RawContent = refund reason code`
- `ProviderDescription = refund reason code`

### 7.2 Meaning of transaction type and status

- `Type = Refund` nghĩa là đây là giao dịch hoàn tiền
- `Status = Succeeded` nghĩa là giao dịch refund đã được áp dụng thành công vào ví

`Refund` là loại giao dịch, không phải booking status.

## 8. Authentication And Authorization

### 8.1 Customer refund endpoint

Endpoint customer cancel booking được bảo vệ bằng `UserPolicy`.

Chỉ customer token hợp lệ mới được gọi endpoint này:

- `POST /api/v1/bookings/{id}/cancel`

### 8.2 Admin refund endpoint

Endpoint admin cancel booking được bảo vệ bằng `AdminPolicy`.

Chỉ admin token hợp lệ mới được gọi endpoint này:

- `POST /api/v1/admin/bookings/{id:guid}/cancel`

## 9. API Summary

### 9.1 Customer cancel booking

Endpoint:

```text
POST /api/v1/bookings/{id}/cancel
```

Authorization:

```text
Bearer customer token
```

Request body:

```json
{
  "reason": "Khong the den dung gio"
}
```

Success response shape:

```json
{
  "id": "booking-guid",
  "status": "Cancelled",
  "cancelledAt": "2026-08-04T09:30:00+07:00",
  "refundApplied": true,
  "refundAmount": 30000,
  "refundTransactionId": "refund-transaction-guid",
  "refundReasonCode": "customer_cancel_before_deadline",
  "message": "Booking cancelled successfully and refund applied"
}
```

Possible response meaning:

- `refundApplied = true`: backend đã cộng ví và tạo refund transaction
- `refundApplied = false`: booking đã hủy nhưng không có refund transaction
- `refundTransactionId = null`: không có dòng refund được tạo

### 9.2 Admin cancel booking

Endpoint:

```text
POST /api/v1/admin/bookings/{id}/cancel
```

Authorization:

```text
Bearer admin token
```

Request body:

```json
{
  "reason": "Branch gap su co"
}
```

Success response shape:

```json
{
  "success": true,
  "message": "Booking cancelled manually",
  "data": {
    "id": "booking-guid",
    "status": "Cancelled",
    "cancelledAt": "2026-08-04T09:30:00+07:00",
    "refundApplied": true,
    "refundAmount": 30000,
    "refundTransactionId": "refund-transaction-guid",
    "refundReasonCode": "admin_cancel",
    "message": "Booking cancelled and refund applied"
  }
}
```

Lưu ý:

- refund được cộng vào ví của customer, không phải ví của admin
- refund history cần xem bằng transaction API của customer tương ứng

### 9.3 Customer transaction history

Endpoint:

```text
GET /api/v1/Transaction
GET /api/v2/Transaction
```

Authorization:

```text
Bearer customer token
```

Query params:

- `pageIndex`
- `pageSize`
- `description`
- `type`
- `fromDate`
- `toDate`

Ví dụ lấy toàn bộ refund transaction:

```text
GET /api/v1/Transaction?pageIndex=1&pageSize=20&type=Refund
```

Ví dụ lấy transaction detail theo id:

```text
GET /api/v1/Transaction/{refundTransactionId}
GET /api/v2/Transaction/{refundTransactionId}
```

### 9.4 Admin wallet topup history

Endpoint:

```text
GET /api/v1/admin/wallet-topup-transactions
```

Endpoint này chỉ dành cho `WalletTopup`.

Endpoint này không phải refund history endpoint.

Nếu dùng endpoint này để tìm refund thì sẽ không thấy.

## 10. Swagger Test Guide

### 10.1 Customer cancel with refund

Điều kiện test:

- customer đã login
- có booking `Confirmed`
- booking đã có `Deposit` hoặc `FullPayment`
- thời điểm hủy còn sớm hơn deadline

Các bước:

1. Authorize bằng customer token trên Swagger.
2. Gọi `POST /api/v1/bookings/{id}/cancel`.
3. Kiểm tra response:
   - `refundApplied = true`
   - `refundAmount > 0`
   - `refundTransactionId != null`
4. Gọi `GET /api/v1/Transaction?pageIndex=1&pageSize=20&type=Refund`.
5. Kiểm tra transaction list có dòng refund vừa tạo.

### 10.2 Customer cancel without refund

Điều kiện test:

- booking đang `Confirmed`
- thời điểm hủy đã chạm hoặc vượt deadline

Các bước:

1. Authorize bằng customer token.
2. Gọi `POST /api/v1/bookings/{id}/cancel`.
3. Kiểm tra response:
   - `refundApplied = false`
   - `refundAmount = 0`
   - `refundTransactionId = null`
4. Gọi lại transaction history để xác nhận không có refund line mới.

### 10.3 Admin cancel with refund

Điều kiện test:

- admin đã login
- booking của customer đang `Confirmed`
- booking đã có tiền thu thực tế

Các bước:

1. Authorize bằng admin token.
2. Gọi `POST /api/v1/admin/bookings/{id}/cancel`.
3. Kiểm tra response:
   - `refundApplied = true`
   - `refundAmount > 0`
   - `refundTransactionId != null`
4. Đăng nhập bằng token customer của booking đó.
5. Gọi `GET /api/v1/Transaction?pageIndex=1&pageSize=20&type=Refund`.
6. Xác nhận refund xuất hiện trong transaction history của customer.

## 11. Expected API Behavior

### 11.1 Customer cancel response

Customer cancel response phải trả đủ các field sau:

- `RefundApplied`
- `RefundAmount`
- `RefundTransactionId`
- `RefundReasonCode`

### 11.2 Admin cancel response

Admin cancel response cũng phải trả cùng bộ field refund để frontend hoặc QA có thể kiểm tra ngay kết quả hoàn tiền.

### 11.3 Transaction list visibility

Refund transaction chỉ hiện cho đúng customer sở hữu ví được refund.

Customer khác hoặc admin không thấy refund này qua customer transaction API của chính họ.

## 12. Known Data Compatibility Risk

Backend hiện map `transaction.type` theo lowercase:

- `deposit`
- `full_payment`
- `wallet_topup`
- `refund`

Nếu dữ liệu trong DB bị lưu sai case, ví dụ `"Refund"` thay vì `"refund"`, transaction query có thể ném lỗi `ArgumentOutOfRangeException`.

Do đó, dữ liệu trong DB phải thống nhất đúng format lowercase đang dùng bởi converter.

## 13. Error Handling Notes

Các lỗi thường gặp:

- `Reason is required.` khi body cancel thiếu lý do
- `Booking not found` khi booking không thuộc customer hoặc không tồn tại
- `Only confirmed bookings can be cancelled` khi booking không ở trạng thái `Confirmed`
- `This booking cannot be cancelled` khi booking đang `InProgress` hoặc `Completed`
- `CancellationDeadlineHours config not found` khi thiếu config hệ thống
- lỗi transaction history nếu DB đang có `transaction.type` lưu sai format enum string

## 14. Test Cases

Các test case tối thiểu nên có:

1. Customer cancel trước deadline thì refund `100%`.
2. Customer cancel sau deadline thì không refund.
3. Admin cancel thì refund `100%`.
4. Refund tạo đúng `transaction` với `Type = Refund`.
5. Transaction history trả được refund cho đúng customer.
6. `type=Refund` filter hoạt động đúng.
7. Customer token không gọi được admin cancel endpoint.
8. Admin token không gọi được customer cancel endpoint.

## 15. Open Questions

- Có cần support partial refund nhiều mức theo thời gian trong phase sau không.
- Có cần thêm cột riêng `RefundReasonCode` trong DB thay vì tận dụng `RawContent` và `ProviderDescription` không.
- Có cần làm transaction enum parser tolerant hơn với dữ liệu cũ sai case trong DB không.
