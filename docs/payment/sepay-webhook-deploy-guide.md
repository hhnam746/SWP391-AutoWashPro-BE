# SePay Webhook Deploy Guide

## 1. Document purpose

Tài liệu này mô tả cách dùng flow SePay wallet top-up trên môi trường deploy/UAT/production, bao gồm:

- API tạo top-up request
- webhook callback từ SePay
- manual test bằng Swagger/Postman
- cách phân biệt `transactionId`, `referenceCode` và `id` của SePay

Tài liệu bám theo implementation hiện tại trong:

- `SWP391-AutoWashPro-BE.Api/Controllers/WalletController.cs`
- `SWP391-AutoWashPro-BE.Api/Controllers/PaymentController.cs`
- `SWP391-AutoWashPro-BE.Service/Wallet/Service.cs`
- `SWP391-AutoWashPro-BE.Service/SePay/Service.cs`

## 2. Scope

Phạm vi chỉ áp dụng cho flow nạp ví bằng SePay:

- `PATCH /api/v2/wallet/top-up`
- `GET /api/v2/wallet/top-up/{transactionId}`
- `POST /api/v1/payment/sepay/webhook`

Không áp dụng cho endpoint cũ `PATCH /api/v1/wallet/top-up`.

## 3. Backend architecture

Flow hiện tại gồm 3 actor:

- FE gọi API tạo top-up request
- User thực hiện chuyển khoản ngân hàng
- SePay callback vào webhook của backend sau khi phát hiện giao dịch

Backend không chủ động gọi webhook trong flow thật. Backend chỉ expose endpoint để SePay gọi vào.

## 4. Main modules

- `WalletController`
  - expose API tạo top-up request
- `Wallet.Service`
  - tạo `Transaction` pending
  - sinh `ReferenceCode`
  - trả QR code cho FE
  - trả `ExpiredAt` cho countdown
  - trả transaction status cho polling
- `PaymentController`
  - expose webhook endpoint cho SePay
- `SePay.Service`
  - verify request
  - extract payment code từ `content` hoặc `description`
  - tìm transaction pending
  - validate amount/account/type
  - cộng tiền vào ví
  - lưu metadata provider

## 5. API summary

### 5.1 Create top-up request

- Route: `PATCH /api/v2/wallet/top-up`
- Auth: `Authorize`
- Request body:

```json
{
  "balance": 5000
}
```

- Response mẫu:

```json
{
  "transactionId": "39035722-ce1b-4209-8365-f0d3fec3192e",
  "amount": 5000,
  "currency": "VND",
  "bankName": "TPBank",
  "bankAccount": "00005668350",
  "referenceCode": "TOPUP-87ab7c599cdf4dc097902250e118c49c",
  "description": "TOPUP-87ab7c599cdf4dc097902250e118c49c",
  "qrCode": "https://vietqr.app/img?bank=TPBank&acc=00005668350&amount=5000&des=TOPUP-87ab7c599cdf4dc097902250e118c49c&template=compact&showinfo=false",
  "status": "Pending",
  "expiredAt": "2026-08-05T13:15:00+00:00",
  "message": "Create wallet top-up request successfully"
}
```

### 5.2 Get top-up status

- Route: `GET /api/v2/wallet/top-up/{transactionId}`
- Auth: `Authorize`

- Response mẫu:

```json
{
  "transactionId": "39035722-ce1b-4209-8365-f0d3fec3192e",
  "amount": 5000,
  "currency": "VND",
  "referenceCode": "TOPUP-87ab7c599cdf4dc097902250e118c49c",
  "status": "Pending",
  "createdAt": "2026-08-05T13:00:00+00:00",
  "expiredAt": "2026-08-05T13:15:00+00:00",
  "paidAt": null,
  "externalTransactionId": null,
  "bankReferenceCode": null
}
```

### 5.3 SePay webhook

- Route: `POST /api/v1/payment/sepay/webhook`
- Auth attribute: `AllowAnonymous`
- Protected by service-level secret/signature validation if configured

- Request DTO:

```json
{
  "id": 0,
  "gateway": "string",
  "transactionDate": "string",
  "accountNumber": "string",
  "subAccount": "string",
  "code": "string",
  "content": "string",
  "transferType": "string",
  "description": "string",
  "transferAmount": 0,
  "accumulated": 0,
  "referenceCode": "string"
}
```

## 6. Data model summary

Trong flow này cần phân biệt rõ 3 loại mã:

- `Transaction.Id`
  - ID nội bộ của hệ thống
  - trả về cho FE trong field `transactionId`
- `Transaction.ReferenceCode`
  - mã hệ thống sinh ra để embed vào nội dung chuyển khoản
  - ví dụ: `TOPUP-87ab7c599cdf4dc097902250e118c49c`
- `Transaction.ExternalTransactionId`
  - ID giao dịch do SePay gửi ở webhook field `id`
  - không tồn tại trước khi callback đến

Một số field liên quan khác:

- `BankReferenceCode`
  - map từ webhook field `referenceCode`
- `Status`
  - `Pending`, `Succeeded`, `Expired`, `Failed`
- `ExpiredAt`
  - thời hạn của top-up request
- `WalletBalanceBefore`, `WalletBalanceAfter`
  - audit balance trước và sau khi cộng ví

## 7. Authentication and authorization

`POST /api/v1/payment/sepay/webhook` dùng `AllowAnonymous` vì caller thật là server của SePay, không phải user đăng nhập.

Tuy nhiên service vẫn có thể chặn request bằng cấu hình:

- `SecretKey`
- `UseHmacSignature`

Behavior hiện tại:

- nếu `SecretKey` rỗng: không enforce secret/signature
- nếu `UseHmacSignature = false` và có `SecretKey`: request phải gửi một trong các giá trị sau
  - header `X-SePay-Secret`
  - header `X-Api-Key`
  - bearer token trong `Authorization`
- nếu `UseHmacSignature = true`: request phải gửi header `X-SePay-Signature`

## 8. Business rules

Webhook chỉ được xử lý thành công khi thỏa tất cả điều kiện sau:

1. `transferType = "in"`
2. `accountNumber` khớp `SePayOptions.BankAccount` nếu cấu hình có giá trị
3. `content` hoặc `description` chứa `ReferenceCode` của hệ thống
4. Transaction tìm được phải là `WalletTopup`
5. Transaction phải còn `Pending`
6. `transferAmount` phải khớp `Transaction.Amount`
7. Transaction chưa hết hạn
8. `id` của SePay chưa từng được xử lý trước đó

Khi hợp lệ:

- cộng tiền vào `Wallet`
- cập nhật transaction thành `Succeeded`
- lưu `ExternalTransactionId`
- lưu `BankReferenceCode`
- lưu raw payload phục vụ audit

## 9. Production callback flow

Đây là flow chạy thật trên deploy:

1. FE gọi `PATCH /api/v2/wallet/top-up`
2. Backend tạo transaction `Pending`
3. Backend trả `transactionId`, `referenceCode`, `qrCode`, `expiredAt`
4. FE hiển thị QR cho user
5. FE countdown theo `expiredAt` và poll `GET /api/v2/wallet/top-up/{transactionId}`
6. User chuyển khoản
7. SePay phát hiện giao dịch ngân hàng
8. SePay tự gọi `POST /api/v1/payment/sepay/webhook`
9. Backend nhận request, tìm transaction bằng `ReferenceCode`
10. Backend cập nhật ví và transaction
11. FE poll lại status và thấy `Pending -> Succeeded` hoặc `Expired`

Điểm quan trọng:

- FE không gọi webhook
- FE không biết trước `id` của SePay
- backend không tự nhập `id` của SePay
- `id` chỉ xuất hiện khi SePay callback

## 10. Manual test flow on deploy/UAT

Manual test chỉ dùng để giả lập callback provider khi chưa có giao dịch thật.

### 10.1 Bước 1: tạo top-up request

Gọi `PATCH /api/v2/wallet/top-up`, ví dụ số tiền `5000`.

Lấy từ response:

- `transactionId`
- `referenceCode`
- `bankAccount`
- `expiredAt`

Ví dụ:

- `transactionId = 39035722-ce1b-4209-8365-f0d3fec3192e`
- `referenceCode = TOPUP-87ab7c599cdf4dc097902250e118c49c`
- `bankAccount = 00005668350`
- `expiredAt = 2026-08-05T13:15:00+00:00`

### 10.2 Bước 2: gọi webhook bằng tay

Gọi `POST /api/v1/payment/sepay/webhook` với payload giả lập.

Ví dụ:

```json
{
  "id": 100001,
  "gateway": "TPBank",
  "transactionDate": "2026-08-05 20:53:34",
  "accountNumber": "00005668350",
  "subAccount": "",
  "code": "TEST-SEPAY",
  "content": "TOPUP87ab7c599cdf4dc097902250e118c49c-050826-20:53:33",
  "transferType": "in",
  "description": "BankAPINotify TOPUP87ab7c599cdf4dc097902250e118c49c-050826-20:53:33",
  "transferAmount": 5000,
  "accumulated": 54083,
  "referenceCode": "BANKREF001"
}
```

### 10.3 Ý nghĩa của các field khi manual test

- `id`
  - số dương bất kỳ, do tester tự đặt
  - phải chưa bị dùng trước đó
- `accountNumber`
  - phải khớp tài khoản nhận cấu hình trên backend
- `content`
  - phải chứa `referenceCode` của top-up request
  - backend có normalize nên có thể thiếu dấu `-`
- `transferType`
  - phải là `"in"`
- `transferAmount`
  - phải đúng số tiền top-up pending
- `referenceCode`
  - mã tham chiếu ngân hàng giả lập
  - không phải `ReferenceCode` nội bộ của hệ thống

### 10.4 Poll status cho FE hoặc tester

Sau khi tạo top-up request, FE hoặc tester có thể poll:

```http
GET /api/v2/wallet/top-up/39035722-ce1b-4209-8365-f0d3fec3192e
Authorization: Bearer <access-token>
```

Ý nghĩa của một số trạng thái:

- `Pending`
  - transaction đang chờ thanh toán hoặc chờ SePay callback
- `Succeeded`
  - webhook đã xử lý thành công và ví đã được cộng
- `Expired`
  - top-up request đã hết hạn, FE phải yêu cầu tạo request mới

## 11. Response contract

### 11.1 Processed

```json
{
  "success": true,
  "code": "processed",
  "message": "Webhook processed successfully.",
  "transactionId": "39035722-ce1b-4209-8365-f0d3fec3192e",
  "alreadyProcessed": false,
  "transactionStatus": "Succeeded"
}
```

### 11.2 Duplicate

```json
{
  "success": true,
  "code": "duplicate",
  "message": "Webhook was already processed.",
  "transactionId": "39035722-ce1b-4209-8365-f0d3fec3192e",
  "alreadyProcessed": true,
  "transactionStatus": "Succeeded"
}
```

### 11.3 Ignored

Một số lý do thường gặp:

- `transferType` không phải `in`
- `accountNumber` không đúng
- không extract được `ReferenceCode`
- không tìm thấy transaction
- transaction không còn `Pending`

### 11.4 Expired

Nếu top-up request đã hết hạn:

```json
{
  "success": true,
  "code": "expired",
  "message": "Wallet top-up transaction has expired.",
  "transactionId": "39035722-ce1b-4209-8365-f0d3fec3192e",
  "alreadyProcessed": false,
  "transactionStatus": "Expired"
}
```

## 12. Configuration

Các config SePay hiện có:

- `SePayOptions:BankName`
- `SePayOptions:BankAccount`
- `SePayOptions:AccountHolder`
- `SePayOptions:WebhookUrl`
- `SePayOptions:QrBaseUrl`
- `SePayOptions:QrTemplate`
- `SePayOptions:QrShowInfo`
- `SePayOptions:IncludeAccountHolderInQr`
- `SePayOptions:TransferContentPrefix`
- `SePayOptions:UseHmacSignature`
- `SePayOptions:SecretKey`

Lưu ý:

- không ghi secret thật vào tài liệu hoặc source control
- nếu bật secret/signature thì manual test cũng phải gửi đúng header tương ứng

## 13. Testing notes

Các case nên test trên deploy/UAT:

- top-up hợp lệ -> `processed`
- create top-up trả đúng `expiredAt`
- poll status trả `Pending` ngay sau khi tạo top-up
- poll status trả `Succeeded` sau khi webhook thành công
- gửi lại cùng `id` -> `duplicate`
- sai `accountNumber` -> `ignored`
- sai `transferAmount` -> `amount_mismatch`
- `transferType = out` -> `ignored`
- content không chứa payment code -> `ignored`
- transaction hết hạn -> `expired`

Sau khi webhook thành công, cần kiểm tra:

- `wallet.balance` tăng đúng
- `transaction.status = Succeeded`
- `transaction.external_transaction_id` đã có giá trị
- `transaction.bank_reference_code` đã được lưu
- `transaction.raw_payload` đã được lưu

## 14. Open questions

- Endpoint cũ `PATCH /api/v1/wallet/top-up` vẫn tồn tại và không thuộc flow SePay pending/callback.
- Nếu môi trường production dùng callback thật từ SePay, team cần xác minh bộ header chính xác SePay gửi để cấu hình `SecretKey` hoặc `UseHmacSignature` đúng chuẩn provider.
