# SePay Storage Design In Transaction

## 1. Document purpose

Tài liệu này mô tả thiết kế lưu thông tin thanh toán SePay theo quyết định mới:

- Không tách metadata SePay ra bảng riêng cho ledger chính.
- Lưu luôn thông tin thanh toán SePay vào bảng `Transaction`.
- Vẫn cho phép hệ thống dùng `Transaction` để hiển thị lịch sử nạp ví, đặt cọc và thanh toán phần còn lại.

Tài liệu này chỉ áp dụng cho flow:

- SePay dùng để `nạp tiền vào ví`
- Không hỗ trợ `rút tiền từ ví ra ngoài hệ thống`

## 2. Decision summary

Thiết kế được chốt là:

- `Transaction` sẽ vừa đóng vai trò ledger giao dịch ví
- vừa lưu thêm metadata cần thiết từ SePay cho giao dịch `WalletTopup`

Nói cách khác:

- với `WalletTopup`, `Transaction` là record trung tâm
- với `Deposit` và `FullPayment`, `Transaction` vẫn hoạt động như hiện tại

## 3. Current state in repository

Repo hiện có:

- entity `Transaction`
- enum `TransactionType`
  - `Deposit`
  - `FullPayment`
  - `WalletTopup`
- bảng `Wallet`
- API xem lịch sử giao dịch của customer

Entity `Transaction` hiện tại có:

- `Amount`
- `Type`
- `Description`
- `TransactionDate`
- `CustomerId`
- `BookingId`
- `CreatedAt`
- `UpdatedAt`

Thiếu:

- trạng thái giao dịch top-up
- mã giao dịch SePay
- dữ liệu đối soát từ webhook
- field để chống xử lý trùng

## 4. Why store SePay data in Transaction

Thiết kế này phù hợp nếu team muốn:

- ít bảng hơn
- query lịch sử ví đơn giản hơn
- customer và admin cùng đọc từ một nguồn ledger chính
- tránh phải join giữa `Transaction` và `WalletTopupRequest` cho màn lịch sử

Lợi ích:

- mọi giao dịch tiền nằm trong một bảng
- admin dễ filter `WalletTopup`, `Deposit`, `FullPayment`
- customer xem lịch sử ví từ một nguồn duy nhất

Tradeoff:

- `Transaction` sẽ phải chứa nhiều field chỉ dùng cho `WalletTopup`
- record `Deposit` và `FullPayment` sẽ có nhiều cột null
- cần thiết kế cẩn thận để không làm bảng `Transaction` quá mơ hồ

## 5. Recommended model

### 5.1 Keep one transaction row per wallet event

Quy tắc chính:

- Mỗi event tác động số dư ví sẽ có đúng 1 record trong `Transaction`

Ví dụ:

- Nạp ví thành công qua SePay -> 1 `Transaction` type `WalletTopup`
- Trừ tiền đặt cọc booking -> 1 `Transaction` type `Deposit`
- Trừ phần còn lại khi check-in -> 1 `Transaction` type `FullPayment`

### 5.2 Transaction is also the top-up state holder

Đối với `WalletTopup`, record `Transaction` không chỉ là ledger cuối cùng, mà còn giữ luôn trạng thái flow:

- `Pending`
- `Succeeded`
- `Failed`
- `Expired`

Điều này có nghĩa:

- khi user tạo yêu cầu nạp ví, backend tạo ngay 1 `Transaction` type `WalletTopup`
- record này ban đầu ở trạng thái `Pending`
- khi SePay callback hợp lệ, backend update record sang `Succeeded` và cộng ví

## 6. Proposed schema change for Transaction

### 6.1 Existing fields kept

Giữ lại:

- `Id`
- `Amount`
- `Type`
- `Description`
- `TransactionDate`
- `CustomerId`
- `BookingId`
- `CreatedAt`
- `UpdatedAt`

### 6.2 New fields should be added

Đề xuất bổ sung vào `Transaction`:

- `Status`
- `ReferenceCode`
- `Provider`
- `ExternalTransactionId`
- `TransferType`
- `Gateway`
- `AccountNumber`
- `ProviderCode`
- `BankReferenceCode`
- `ProviderTransactionDate`
- `RawContent`
- `ProviderDescription`
- `RawPayload`
- `PaidAt`
- `ExpiredAt`
- `WalletBalanceBefore`
- `WalletBalanceAfter`

## 7. Purpose of each new field

### 7.1 Core business fields

Các field này nên xem là bắt buộc cho flow SePay:

| Field | Purpose |
| --- | --- |
| `Status` | Biết transaction top-up đang chờ, thành công hay thất bại |
| `ReferenceCode` | Mã nội bộ để match transaction với nội dung chuyển khoản |
| `Provider` | Biết giao dịch đến từ `SePay` |
| `ExternalTransactionId` | Lưu `id` từ SePay để chống trùng |
| `TransferType` | Xác nhận đây là tiền vào |
| `ProviderTransactionDate` | Thời điểm giao dịch do SePay báo về |
| `PaidAt` | Thời điểm hệ thống xác nhận top-up thành công |
| `ExpiredAt` | Hết hạn thanh toán nếu user không chuyển khoản kịp |

### 7.2 Reconciliation and audit fields

Các field này rất nên có để vận hành:

| Field | Purpose |
| --- | --- |
| `Gateway` | Biết giao dịch đến từ ngân hàng nào |
| `AccountNumber` | Biết tài khoản nhận tiền |
| `ProviderCode` | Lưu `code` của SePay nếu team dùng cấu trúc mã thanh toán |
| `BankReferenceCode` | Mã tham chiếu phía ngân hàng |
| `RawContent` | Nội dung chuyển khoản gốc |
| `ProviderDescription` | Mô tả đầy đủ từ webhook |
| `RawPayload` | Lưu raw JSON để audit/debug |

### 7.3 Wallet trace fields

Các field này không bắt buộc tuyệt đối nhưng rất hữu ích:

| Field | Purpose |
| --- | --- |
| `WalletBalanceBefore` | Biết số dư ví trước giao dịch |
| `WalletBalanceAfter` | Biết số dư ví sau giao dịch |

## 8. Field mapping from SePay webhook

Theo payload webhook SePay đã kiểm tra ngày `2026-08-03`, backend có thể map như sau:

| SePay field | Transaction field |
| --- | --- |
| `id` | `ExternalTransactionId` |
| `gateway` | `Gateway` |
| `transactionDate` | `ProviderTransactionDate` |
| `accountNumber` | `AccountNumber` |
| `code` | `ProviderCode` |
| `content` | `RawContent` |
| `transferType` | `TransferType` |
| `description` | `ProviderDescription` |
| `transferAmount` | dùng để verify với `Amount` |
| `referenceCode` | `BankReferenceCode` |
| raw JSON | `RawPayload` |

## 9. Additional enum proposal

### 9.1 TransactionStatus

Nên thêm enum mới cho `Transaction.Status`:

- `Pending`
- `Succeeded`
- `Failed`
- `Expired`

### 9.2 ProviderType

Nếu muốn chừa đường mở rộng cho cổng khác:

- `Internal`
- `SePay`

Mapping gợi ý:

- `Deposit` -> `Provider = Internal`
- `FullPayment` -> `Provider = Internal`
- `WalletTopup` -> `Provider = SePay`

## 10. Business rules

### 10.1 When creating a wallet top-up request

Khi customer tạo yêu cầu nạp ví:

1. Backend validate amount.
2. Backend sinh `ReferenceCode`.
3. Backend tạo 1 `Transaction` mới:
   - `Type = WalletTopup`
   - `Status = Pending`
   - `Provider = SePay`
   - `Amount = requested amount`
   - `ReferenceCode = generated code`
   - `ExpiredAt = now + topup expiry`
4. Backend trả QR, amount, reference code cho frontend.

### 10.2 When SePay sends valid webhook

Khi webhook hợp lệ:

1. Tìm `Transaction` type `WalletTopup` theo `ReferenceCode`.
2. Kiểm tra:
   - `Status = Pending`
   - `TransferType = in`
   - amount từ webhook khớp `Transaction.Amount`
   - `ExternalTransactionId` chưa từng được xử lý
3. Mở DB transaction.
4. Update record `Transaction`:
   - `Status = Succeeded`
   - `ExternalTransactionId`
   - `Gateway`
   - `AccountNumber`
   - `ProviderCode`
   - `BankReferenceCode`
   - `ProviderTransactionDate`
   - `RawContent`
   - `ProviderDescription`
   - `RawPayload`
   - `PaidAt`
   - `WalletBalanceBefore`
   - `WalletBalanceAfter`
5. Cộng `wallet.Balance`.
6. Commit.

### 10.3 When webhook is duplicated

Nếu webhook bị gửi lại:

- không tạo thêm record mới
- không cộng ví lần 2
- trả success để SePay không retry tiếp

### 10.4 When customer pays booking

Với `Deposit` và `FullPayment`:

- vẫn tạo `Transaction` như flow hiện tại
- `Provider = Internal`
- `Status = Succeeded`
- các field SePay để null

## 11. Validation rules

### 11.1 Rules for WalletTopup

- `Amount > 0`
- `Status` không được null
- `ReferenceCode` phải unique
- `ExternalTransactionId` phải unique khi khác null
- chỉ `WalletTopup` mới được phép có metadata SePay

### 11.2 Rules for Deposit and FullPayment

- `Status` mặc định là `Succeeded`
- `Provider = Internal`
- không bắt buộc có `ReferenceCode`
- không được có `ExternalTransactionId` trừ khi team mở rộng flow mới

## 12. Database constraints

Đề xuất:

- unique index cho `ReferenceCode`
- unique index cho `ExternalTransactionId` khi khác null
- index cho `CustomerId`
- index cho `Type`
- index cho `Status`
- index cho `TransactionDate`
- index cho `PaidAt`

## 13. API impact

### 13.1 Customer APIs

Customer vẫn có thể dùng:

- `GET /api/v1/transaction`
- `GET /api/v1/transaction/{id}`

Nhưng response nên mở rộng thêm:

- `status`
- `referenceCode`
- `provider`
- `paidAt`
- `expiredAt`
- `direction`

### 13.2 Top-up create API

Đề xuất:

- `POST /api/v1/wallet/topup-transactions`

API này sẽ:

- tạo transaction `WalletTopup` ở trạng thái `Pending`
- trả dữ liệu thanh toán cho FE

### 13.3 SePay webhook API

Đề xuất:

- `POST /api/v1/payment/sepay/webhook`

API này sẽ:

- update `Transaction` pending
- cộng ví
- không insert transaction mới nếu record pending đã tồn tại

## 14. Example record design

### 14.1 Pending WalletTopup

```json
{
  "id": "guid",
  "amount": 200000,
  "type": "WalletTopup",
  "status": "Pending",
  "provider": "SePay",
  "referenceCode": "TOPUP-7F3A9B",
  "externalTransactionId": null,
  "paidAt": null,
  "expiredAt": "2026-08-03T20:30:00+07:00"
}
```

### 14.2 Succeeded WalletTopup

```json
{
  "id": "guid",
  "amount": 200000,
  "type": "WalletTopup",
  "status": "Succeeded",
  "provider": "SePay",
  "referenceCode": "TOPUP-7F3A9B",
  "externalTransactionId": "92704",
  "gateway": "Vietcombank",
  "accountNumber": "1017588888",
  "providerCode": "SEVN63DC8E5C",
  "bankReferenceCode": "FT24012345678",
  "paidAt": "2026-08-03T19:14:11+07:00"
}
```

### 14.3 Internal Deposit

```json
{
  "id": "guid",
  "amount": 30000,
  "type": "Deposit",
  "status": "Succeeded",
  "provider": "Internal",
  "bookingId": "guid"
}
```

## 15. Recommended implementation notes

### 15.1 Keep nullability intentional

Các field SePay trong `Transaction` nên để nullable vì:

- `Deposit` không có metadata SePay
- `FullPayment` không có metadata SePay

### 15.2 Do not use RawPayload for business decision

`RawPayload` chỉ dùng cho:

- audit
- debug
- replay investigation

Không dùng `RawPayload` làm nguồn business chính.

### 15.3 Match order

Thứ tự match nên là:

1. `ReferenceCode`
2. `Type = WalletTopup`
3. `Status = Pending`
4. amount webhook == `Amount`
5. `ExternalTransactionId` chưa tồn tại

## 16. Risks and tradeoffs

Rủi ro của hướng này:

- bảng `Transaction` sẽ phình hơn
- khó tách bạch giữa payment-request state và ledger state
- sau này nếu có nhiều payment provider hơn thì `Transaction` sẽ mang nhiều field provider-specific

Cách giảm rủi ro:

- giữ tên field rõ nghĩa
- chỉ dùng metadata SePay cho `WalletTopup`
- không tái dùng các field này cho flow nội bộ một cách mơ hồ

## 17. Final recommendation

Nếu bạn chốt hướng lưu metadata SePay ngay trong `Transaction`, tôi khuyên:

1. Thêm `Status` cho toàn bộ transaction.
2. Thêm `ReferenceCode` và `ExternalTransactionId`.
3. Thêm nhóm field provider:
   - `Provider`
   - `Gateway`
   - `AccountNumber`
   - `ProviderCode`
   - `BankReferenceCode`
   - `ProviderTransactionDate`
   - `RawContent`
   - `ProviderDescription`
   - `RawPayload`
4. Dùng chính record `WalletTopup` làm transaction pending ngay từ lúc tạo yêu cầu nạp ví.
5. Khi webhook thành công, update record đó và cộng ví, thay vì insert thêm một transaction mới.

## 18. Summary

Thiết kế này cho phép:

- lưu thông tin SePay trực tiếp trong `Transaction`
- dùng một bảng duy nhất để hiển thị lịch sử ví
- cho admin dễ theo dõi tiền vào và tiền ra nội bộ
- vẫn chống trùng webhook và đối soát được nếu thêm đúng các field bắt buộc

Đổi lại, `Transaction` sẽ không còn là bảng ledger tối giản nữa, mà trở thành bảng ledger + payment metadata cho flow `WalletTopup`.
