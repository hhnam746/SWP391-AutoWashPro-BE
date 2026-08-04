# SePay Webhook Tutorial

Tài liệu này hướng dẫn cấu hình và sử dụng API:

- `POST /api/v1/payment/sepay/webhook`

Mục tiêu của endpoint này là nhận webhook từ SePay để xác nhận giao dịch nạp ví và cộng tiền vào `Wallet` khi giao dịch hợp lệ.

Theo code trong repo ngày `August 3, 2026`, endpoint đã được nối vào:

- `SWP391-AutoWashPro-BE.Api/Controllers/PaymentController.cs`
- `SWP391-AutoWashPro-BE.Service/SePay/Service.cs`

## 1. Route và purpose

- Route: `POST /api/v1/payment/sepay/webhook`
- Auth: `AllowAnonymous`
- Actor gọi API: SePay webhook server
- Mục đích:
  - nhận callback khi khách đã chuyển khoản
  - tìm `Transaction` top-up đang chờ
  - xác minh số tiền và idempotency
  - cộng tiền vào ví
  - cập nhật metadata SePay vào `Transaction`

## 2. Điều kiện tiên quyết

Webhook này chỉ hoạt động đúng nếu hệ thống đã có sẵn `Transaction` pending cho flow top-up.

Record `Transaction` cần có tối thiểu:

- `Type = WalletTopup`
- `Status = Pending`
- `ReferenceCode = nội dung chuyển khoản mà SePay sẽ gửi về trong field content`
- `CustomerId` hợp lệ

Nếu chưa có bước tạo top-up pending đúng chuẩn SePay mà vẫn gọi webhook này, endpoint sẽ trả:

- `code = "ignored"`

vì không tìm thấy transaction phù hợp để xử lý.

## 3. Payload SePay đang dùng

Request DTO hiện tại:

```json
{
  "id": 92704,
  "gateway": "Vietcombank",
  "transactionDate": "2026-08-03T19:14:11+07:00",
  "accountNumber": "1017588888",
  "subAccount": "",
  "code": "SEVN63DC8E5C",
  "content": "TOPUP-7F3A9B",
  "transferType": "in",
  "description": "Nap vi TOPUP-7F3A9B",
  "transferAmount": 200000,
  "accumulated": 5000000,
  "referenceCode": "FT24012345678"
}
```

Mapping chính vào `Transaction`:

- `id` -> `ExternalTransactionId`
- `gateway` -> `Gateway`
- `transactionDate` -> `ProviderTransactionDate`
- `accountNumber` -> `AccountNumber`
- `code` -> `ProviderCode`
- `content` -> dùng để match `ReferenceCode`
- `transferType` -> `TransferType`
- `description` -> `ProviderDescription`
- `transferAmount` -> đối chiếu với `Amount`
- `referenceCode` -> `BankReferenceCode`
- raw JSON -> `RawPayload`

## 4. Response contract

Endpoint hiện trả raw JSON, không bọc `ApiResponseFactory`, để webhook provider nhận response đơn giản:

### 4.1 Processed

```json
{
  "success": true,
  "code": "processed",
  "message": "Webhook processed successfully.",
  "transactionId": "guid"
}
```

### 4.2 Duplicate

```json
{
  "success": true,
  "code": "duplicate",
  "message": "Webhook was already processed.",
  "transactionId": "guid"
}
```

### 4.3 Ignored

```json
{
  "success": true,
  "code": "ignored",
  "message": "No pending wallet top-up transaction matched the webhook reference.",
  "transactionId": null
}
```

### 4.4 Amount mismatch

```json
{
  "success": true,
  "code": "amount_mismatch",
  "message": "Webhook amount did not match the pending transaction.",
  "transactionId": "guid"
}
```

## 5. Business rules đang implement

Service hiện đang xử lý theo các rule sau:

1. Chỉ chấp nhận `transferType = "in"`.
2. Chỉ xử lý transaction:
   - `Type = WalletTopup`
   - `Status = Pending`
   - `ReferenceCode == request.Content`
3. Chống xử lý trùng bằng `ExternalTransactionId`.
4. Chỉ cộng ví khi `transferAmount == Transaction.Amount`.
5. Khi hợp lệ:
   - cộng `Wallet.Balance`
   - cập nhật `Transaction.Status = Succeeded`
   - set `Provider = SePay`
   - set `PaidAt`
   - lưu metadata SePay để audit và đối soát

## 6. Cách cấu hình trên SePay

Trong màn hình cấu hình webhook của SePay, điền:

- URL:
  - `https://<your-domain>/api/v1/payment/sepay/webhook`
- Method:
  - `POST`
- Content-Type:
  - `application/json`

Nếu đang test local, dùng tunnel như:

- `ngrok http <your-port>`

Ví dụ:

- local API chạy ở `https://localhost:5001`
- tạo tunnel public
- cấu hình SePay webhook URL thành:
  - `https://abc123.ngrok-free.app/api/v1/payment/sepay/webhook`

## 7. Cách tạo data test trước khi nhận webhook

Trước khi SePay callback, hệ thống phải tạo transaction pending.

Ví dụ record cần có:

```text
Type = WalletTopup
Status = Pending
Provider = SePay
ReferenceCode = TOPUP-7F3A9B
Amount = 200000
CustomerId = <existing customer id>
```

Nếu flow top-up create chưa được sửa, webhook này sẽ không dùng được cho production flow SePay.

## 8. Cách test thủ công

Gọi thử endpoint bằng request giả lập:

```http
POST /api/v1/payment/sepay/webhook
Content-Type: application/json

{
  "id": 92704,
  "gateway": "Vietcombank",
  "transactionDate": "2026-08-03T19:14:11+07:00",
  "accountNumber": "1017588888",
  "subAccount": "",
  "code": "SEVN63DC8E5C",
  "content": "TOPUP-7F3A9B",
  "transferType": "in",
  "description": "Nap vi TOPUP-7F3A9B",
  "transferAmount": 200000,
  "accumulated": 5000000,
  "referenceCode": "FT24012345678"
}
```

Sau khi gọi thành công, kiểm tra:

- `wallet.balance` tăng đúng số tiền
- `transaction.status = Succeeded`
- `transaction.external_transaction_id` đã được set
- `transaction.paid_at` đã được set
- `transaction.raw_payload` đã được lưu

## 9. Các case kết quả

### `processed`

- webhook hợp lệ
- transaction pending tồn tại
- amount khớp
- ví đã được cộng

### `duplicate`

- webhook `id` đã xử lý trước đó
- không cộng ví lần 2

### `ignored`

- `transferType` không phải `in`
- hoặc không tìm thấy `Transaction` pending với `ReferenceCode` tương ứng

### `amount_mismatch`

- có pending transaction
- nhưng số tiền webhook không khớp số tiền pending

## 10. Giới hạn hiện tại

Các điểm sau chưa được cấu hình thêm trong code hiện tại:

- chưa verify secret/signature từ SePay
- chưa có config `AllowedIp` hoặc whitelist provider
- chưa có flow tạo `WalletTopup` pending chuẩn SePay ở customer API
- chưa có integration test cho webhook này

## 11. Khuyến nghị tiếp theo

Để dùng flow SePay hoàn chỉnh, nên làm tiếp:

1. Tạo API `POST /api/v1/wallet/topup-transactions`.
2. API đó phải sinh:
   - `ReferenceCode`
   - `Status = Pending`
   - `Provider = SePay`
3. Bổ sung verify chữ ký hoặc secret nếu SePay cung cấp.
4. Thêm integration test cho:
   - processed
   - duplicate
   - ignored
   - amount mismatch
