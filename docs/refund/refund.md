# Booking Refund Flow Design

## Summary

Thiết kế refund tập trung cho booking cancellation theo các quyết định đã chốt:

- Refund đổ về wallet nội bộ, không refund ra ngân hàng/SePay.
- Refund chạy tự động theo rule engine, không cần admin approve trước.
- Rule refund theo matrix by scenario.
- Customer tự hủy: refund theo mốc thời gian.
- Admin hủy hoặc system/branch fault: hoàn 100%.
- Auto-cancel do no check-in/quá hạn: không hoàn.

Refund chỉ áp dụng cho số tiền đã thu thực tế của booking tại thời điểm hủy:

- Nếu booking mới thu Deposit, refund tối đa phần Deposit.
- Nếu booking đã thu thêm FullPayment, refund tối đa Deposit + FullPayment.

## Key Changes

### 1. Refund business rules

Áp dụng matrix cố định sau:

• Proposed Plan


# Booking Refund Flow Design

## Summary

Thiết kế refund tập trung cho booking cancellation theo các quyết định đã chốt:

- Refund đổ về wallet nội bộ, không refund ra ngân hàng/SePay.
- Refund chạy tự động theo rule engine, không cần admin approve trước.
- Rule refund theo matrix by scenario.
- Customer tự hủy: refund theo mốc thời gian.
- Admin hủy hoặc system/branch fault: hoàn 100%.
- Auto-cancel do no check-in/quá hạn: không hoàn.

Refund chỉ áp dụng cho số tiền đã thu thực tế của booking tại thời điểm hủy:

- Nếu booking mới thu Deposit, refund tối đa phần Deposit.
- Nếu booking đã thu thêm FullPayment, refund tối đa Deposit + FullPayment.

## Key Changes

### 1. Refund business rules

Áp dụng matrix cố định sau:

Scenario                                         Booking status lúc hủy                                                Refund rule
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━  ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━  ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
Customer cancel trước deadline                   Confirmed                                                             Hoàn 100% số tiền đã thu
───────────────────────────────────────────────  ────────────────────────────────────────────────────────────────────  ──────────────────────────────────
Customer cancel sau deadline                     Confirmed                                                             Không hoàn
───────────────────────────────────────────────  ────────────────────────────────────────────────────────────────────  ──────────────────────────────────
Admin cancel                                     Pending, Confirmed, CheckIn                                           Hoàn 100% số tiền đã thu
───────────────────────────────────────────────  ────────────────────────────────────────────────────────────────────  ──────────────────────────────────
Branch/System cannot serve                       Pending, Confirmed, CheckIn, InProgress nếu service chưa thực hiện    Hoàn 100% số tiền đã thu
───────────────────────────────────────────────  ────────────────────────────────────────────────────────────────────  ──────────────────────────────────
Auto-cancel do no check-in / quá hạn check-in    Confirmed                                                             Không hoàn
───────────────────────────────────────────────  ────────────────────────────────────────────────────────────────────  ──────────────────────────────────
Booking create failed sau khi đã trừ ví          transaction committed một phần / inconsistency                        Hoàn 100% bằng compensation flow
───────────────────────────────────────────────  ────────────────────────────────────────────────────────────────────  ──────────────────────────────────
Duplicate charge / overcharge                    bất kỳ                                                                Hoàn phần bị thu thừa

Quy tắc trạng thái:

- Customer chỉ được cancel khi booking đang Confirmed, giữ nguyên rule hiện tại.
- Admin có thể cancel khi booking chưa Completed và chưa Cancelled.
- Refund không chạy cho Completed.
- Refund cho InProgress chỉ cho system/admin fault và phải có cờ xác nhận “service not delivered”.

### 2. Wallet + transaction model

Dùng bảng Transaction hiện có với TransactionType.Refund làm ledger hoàn tiền.

Khi phát sinh refund:

- Tăng wallet.Balance đúng bằng số tiền refund.
- Insert một Transaction mới:
    - Type = Refund
    - Amount = refundAmount
    - CustomerId = booking.CustomerId
    - BookingId = booking.Id
    - Description = <refund reason normalized>
    - TransactionDate = DateTime.UtcNow
    - Provider = Internal
    - TransferType = In
    - CreatedAt = DateTimeOffset.UtcNow

Không sửa ngược transaction Deposit hoặc FullPayment cũ.

Cần bổ sung metadata refund để khỏi mất audit. Chọn phương án ít đổi nhất:

- Thêm vào Transaction các cột:
    - RefundReasonCode (customer_cancel_before_deadline, admin_cancel, system_fault, duplicate_charge, ...)
    - RefundActorType (customer, admin, system)
    - RefundActorId (Guid?)
    - RefundSourceTransactionType (Deposit / FullPayment / Mixed)

- Nếu team muốn audit đầy đủ hơn về sau, có thể tách RefundRecord; nhưng cho phase này mặc định dùng luôn Transaction để tránh mở rộng lớn.

### 3. API and service behavior

Giữ endpoint customer:

- POST /api/v1/bookings/{id}/cancel

Mở rộng request của customer cancel:

- reason giữ nguyên.
- Không cho client truyền refundAmount; backend tự tính.

Mở rộng response customer/admin cancel:

- refundApplied: bool
- refundAmount: decimal
- refundTransactionId: Guid?
- refundReasonCode: string?

Admin cancel:

- giữ endpoint hiện tại ở AdminController
- backend dùng cùng refund engine nhưng actor = admin

Tạo shared refund method ở booking/payment layer, ví dụ:

- CalculateRefund(booking, cancelActor, cancelReason, now)
- ApplyRefundAsync(booking, refundDecision, actor, cancellationToken)

Decision object cần chứa:

- IsRefundable
- RefundAmount
- ReasonCode
- ActorType
- ShouldCreateRefundTransaction

### 4. Config and timing rules

Chọn config-driven để FE không hardcode và backend dễ đổi rule:

- Dùng CancellationDeadlineHours hiện có làm mốc customer được hoàn 100%.
- Thêm config:
    - CustomerCancelRefundPercentBeforeDeadline = 100
    - CustomerCancelRefundPercentAfterDeadline = 0
    - AutoCancelRefundPercent = 0
    - AdminCancelRefundPercent = 100
    - SystemCancelRefundPercent = 100

Nếu chưa muốn thêm quá nhiều config, phase 1 có thể hardcode bằng service constants nhưng thiết kế chính thức vẫn nên đọc từ SystemConfig.

Refund amount formula:

- refundAmount = totalPaidAmount * refundPercent / 100
- totalPaidAmount = tổng transaction Deposit + FullPayment đã thành công/đã ghi nhận cho booking
- với current code phase này thực tế thường là:
    - cancel trước check-in: chỉ có Deposit
    - sau check-in chưa service delivered và admin/system cancel: có thể có cả Deposit + FullPayment

### 5. Transaction, idempotency, and failure handling

Mọi cancel + refund phải chạy trong cùng DB transaction:

- cập nhật Booking.Status = Cancelled
- set CancelledAt, UpdatedAt
- release voucher nếu có
- cộng wallet.Balance nếu có refund
- insert TransactionType.Refund
- tạo notification

Idempotency:

- nếu booking đã Cancelled, không refund lại
- trước khi tạo refund, check đã có TransactionType.Refund cho BookingId + ReasonCode tương ứng chưa
- duplicate refund request phải trả kết quả an toàn, không cộng ví lần hai

Compensation:

- nếu booking bị cancel nhưng refund insert/balance update fail, rollback toàn bộ
- nếu logic nền sau commit thất bại (notification), không rollback refund

## Test Plan

Cần có test cho các case sau:

- Customer cancel trước CancellationDeadlineHours:
    - booking Confirmed
    - wallet được cộng lại đúng bằng Deposit
    - tạo TransactionType.Refund

- Customer cancel sau deadline:
    - booking không bị hủy hoặc bị từ chối theo rule hiện tại
    - không có refund transaction

- Admin cancel booking Confirmed:
    - refund 100%
    - notification có kèm lý do

- Auto-cancel do no check-in:
    - booking chuyển Cancelled
    - không refund

- System/branch fault cancel:
    - refund 100%

- Duplicate cancel request:
    - không tạo refund transaction thứ hai

- Booking có voucher reserved:
    - voucher được release đúng
    - refund vẫn tính độc lập với voucher

- Booking có cả Deposit và FullPayment:
    - totalPaidAmount tính đúng
    - rule admin/system refund đúng tổng đã thu

- Refund transaction history:
    - GET /api/v1/transaction trả được Type = Refund

## Assumptions

- Phase này chỉ refund vào wallet; không tích hợp outbound bank refund.
- Deposit và FullPayment là nguồn dữ liệu chuẩn để tính số tiền đã thu.
- Customer cancel flow tiếp tục giới hạn ở booking Confirmed.
- CancellationDeadlineHours là mốc duy nhất để phân biệt customer được hoàn hay không hoàn.
- Auto-cancel do no check-in được xem là lỗi phía customer nên mặc định 0% refund.
- Nếu cần partial refund nhiều mức theo thời gian sau này, mở rộng bằng thêm config tiers; không làm trong phase này.