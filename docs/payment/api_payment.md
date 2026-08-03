# API Payment Design

## 1. API overview

Mục tiêu của payment API trong phase SePay hiện tại là hỗ trợ 4 nhóm chức năng:

- customer xem ví hiện tại
- customer xem lịch sử giao dịch
- customer tạo yêu cầu nạp ví qua SePay và backend nhận webhook để xác nhận giao dịch
- admin xem ai đã nạp ví và nạp bao nhiêu tiền

Theo code hiện có trong repo ngày `2026-08-03`:

- đã có `GET /api/v1/wallet`
- đã có `GET /api/v1/transaction`
- đã có `GET /api/v1/transaction/{id}`
- đang có `PATCH /api/v1/wallet/top-up` nhưng endpoint này cộng ví trực tiếp nên không phù hợp với flow SePay mới
- đã có `PaymentController` nhưng chưa có action

Kết luận thiết kế:

- giữ lại API đọc dữ liệu ví và transaction
- thêm API tạo `WalletTopup` pending
- thêm API webhook cho SePay
- thêm API admin để xem danh sách nạp ví
- không dùng `PATCH /api/v1/wallet/top-up` cho production flow SePay

## 2. Endpoint table

| API | Method | Auth | Purpose | Status |
| --- | --- | --- | --- | --- |
| `/api/v1/wallet` | `GET` | Customer | Lấy số dư ví hiện tại | Keep |
| `/api/v1/transaction` | `GET` | Customer | Lấy danh sách giao dịch | Keep, mở rộng response |
| `/api/v1/transaction/{id}` | `GET` | Customer | Lấy chi tiết 1 giao dịch | Keep, mở rộng response |
| `/api/v1/wallet/topup-transactions` | `POST` | Customer | Tạo yêu cầu nạp ví SePay, sinh transaction pending | New |
| `/api/v1/payment/sepay/webhook` | `POST` | SePay webhook | Xác nhận top-up thành công hoặc duplicate callback | New |
| `/api/v1/admin/wallet-topup-transactions` | `GET` | Admin | Xem danh sách transaction nạp ví của customer | New, route stub đã có trong `AdminController` |
| `/api/v1/wallet/top-up` | `PATCH` | Customer | Cộng ví trực tiếp | Deprecate |

## 3. Request model

### 3.1 `GET /api/v1/wallet`

Không có request body.

### 3.2 `GET /api/v1/transaction`

Query parameters:

| Field | Type | Required | Note |
| --- | --- | --- | --- |
| `pageIndex` | `int` | Yes | `>= 1` |
| `pageSize` | `int` | Yes | `>= 1`, nên giới hạn max `100` ở implementation |
| `description` | `string` | No | filter gần đúng theo mô tả |
| `type` | `TransactionType` | No | `Deposit`, `FullPayment`, `WalletTopup`, `Refund` |
| `fromDate` | `DateTime` | No | filter từ thời điểm |
| `toDate` | `DateTime` | No | filter đến thời điểm |
| `status` | `TransactionStatus` | No | nên bổ sung cho flow top-up |
| `provider` | `ProviderType` | No | nên bổ sung cho flow SePay |

### 3.3 `GET /api/v1/transaction/{id}`

Path parameters:

| Field | Type | Required | Note |
| --- | --- | --- | --- |
| `id` | `Guid` | Yes | transaction id |

### 3.4 `POST /api/v1/wallet/topup-transactions`

Request body:

```json
{
  "amount": 200000
}
```

Request DTO đề xuất:

| Field | Type | Required | Note |
| --- | --- | --- | --- |
| `amount` | `decimal` | Yes | số tiền customer muốn nạp |

### 3.5 `POST /api/v1/payment/sepay/webhook`

Request body phải bám payload thực tế của SePay. Theo tài liệu hiện có, backend đang cần các field sau:

```json
{
  "id": "92704",
  "gateway": "Vietcombank",
  "transactionDate": "2026-08-03T19:14:11+07:00",
  "accountNumber": "1017588888",
  "code": "SEVN63DC8E5C",
  "content": "TOPUP-7F3A9B",
  "transferType": "in",
  "description": "Nap vi TOPUP-7F3A9B",
  "transferAmount": 200000,
  "referenceCode": "FT24012345678"
}
```

Webhook DTO tối thiểu backend nên nhận:

| Field | Type | Required | Note |
| --- | --- | --- | --- |
| `id` | `string` | Yes | map vào `ExternalTransactionId` |
| `gateway` | `string` | No | map vào `Gateway` |
| `transactionDate` | `DateTimeOffset?` | No | map vào `ProviderTransactionDate` |
| `accountNumber` | `string` | No | map vào `AccountNumber` |
| `code` | `string` | No | map vào `ProviderCode` |
| `content` | `string` | Yes | dùng để match `ReferenceCode` |
| `transferType` | `string` | Yes | phải là `in` |
| `description` | `string` | No | map vào `ProviderDescription` |
| `transferAmount` | `decimal` | Yes | verify với `Transaction.Amount` |
| `referenceCode` | `string` | No | map vào `BankReferenceCode` |

### 3.6 `GET /api/v1/admin/wallet-topup-transactions`

Query parameters:

| Field | Type | Required | Note |
| --- | --- | --- | --- |
| `pageIndex` | `int` | No | mặc định `1`, `>= 1` |
| `pageSize` | `int` | No | mặc định `10`, `>= 1`, nên giới hạn max `100` |
| `keyword` | `string` | No | search theo email, phone, full name, `referenceCode` |
| `status` | `TransactionStatus` | No | filter `Pending`, `Succeeded`, `Failed`, `Expired` |
| `fromDate` | `DateTimeOffset` | No | filter theo `CreatedAt` |
| `toDate` | `DateTimeOffset` | No | filter theo `CreatedAt` |
| `minAmount` | `decimal` | No | số tiền tối thiểu |
| `maxAmount` | `decimal` | No | số tiền tối đa |

Request DTO nên theo pattern admin hiện có:

```csharp
public class GetWalletTopupTransactionsRequest
{
    public int PageIndex { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public string? Keyword { get; set; }
    public TransactionStatus? Status { get; set; }
    public DateTimeOffset? FromDate { get; set; }
    public DateTimeOffset? ToDate { get; set; }
    public decimal? MinAmount { get; set; }
    public decimal? MaxAmount { get; set; }
}
```

## 4. Response model

### 4.1 `GET /api/v1/wallet`

```json
{
  "id": "wallet-guid",
  "balance": 350000,
  "currency": "VND"
}
```

### 4.2 `GET /api/v1/transaction`

```json
{
  "transactions": [
    {
      "transactionId": "guid",
      "customerId": "guid",
      "bookingId": null,
      "amount": 200000,
      "type": "WalletTopup",
      "status": "Pending",
      "provider": "SePay",
      "referenceCode": "TOPUP-7F3A9B",
      "description": "Wallet top-up",
      "transactionDate": "2026-08-03T19:00:00Z",
      "paidAt": null,
      "expiredAt": "2026-08-03T20:30:00+07:00",
      "createdAt": "2026-08-03T19:00:00Z",
      "updatedAt": null
    }
  ],
  "pagination": {
    "page": 1,
    "pageSize": 20,
    "totalCount": 1,
    "totalPages": 1
  }
}
```

Response item nên mở rộng từ DTO hiện có:

| Field | Type | Note |
| --- | --- | --- |
| `transactionId` | `Guid` | id giao dịch |
| `customerId` | `Guid` | owner |
| `bookingId` | `Guid?` | có với booking payment |
| `amount` | `decimal` | số tiền |
| `type` | `TransactionType` | loại giao dịch |
| `status` | `TransactionStatus?` | đặc biệt quan trọng cho top-up |
| `provider` | `ProviderType?` | `Internal` hoặc `SePay` |
| `referenceCode` | `string?` | mã customer cần dùng khi chuyển khoản |
| `description` | `string?` | mô tả giao dịch |
| `transactionDate` | `DateTime` | thời điểm transaction |
| `paidAt` | `DateTimeOffset?` | thời điểm payment confirm |
| `expiredAt` | `DateTimeOffset?` | hết hạn thanh toán |
| `createdAt` | `DateTimeOffset` | audit |
| `updatedAt` | `DateTimeOffset?` | audit |

### 4.3 `GET /api/v1/transaction/{id}`

```json
{
  "transactionId": "guid",
  "customerId": "guid",
  "bookingId": null,
  "amount": 200000,
  "type": "WalletTopup",
  "status": "Succeeded",
  "provider": "SePay",
  "referenceCode": "TOPUP-7F3A9B",
  "externalTransactionId": "92704",
  "transferType": "In",
  "gateway": "Vietcombank",
  "accountNumber": "1017588888",
  "providerCode": "SEVN63DC8E5C",
  "bankReferenceCode": "FT24012345678",
  "providerTransactionDate": "2026-08-03T19:14:11+07:00",
  "description": "Wallet top-up",
  "providerDescription": "Nap vi TOPUP-7F3A9B",
  "paidAt": "2026-08-03T19:14:11+07:00",
  "expiredAt": "2026-08-03T20:30:00+07:00",
  "walletBalanceBefore": 150000,
  "walletBalanceAfter": 350000,
  "createdAt": "2026-08-03T19:00:00Z",
  "updatedAt": "2026-08-03T19:14:12Z"
}
```

Ghi chú:

- customer response không nên trả `rawPayload`
- `rawContent` có thể trả nếu team muốn customer tự đối chiếu nội dung chuyển khoản
- `providerDescription` có thể giữ nếu không chứa dữ liệu nhạy cảm

### 4.4 `POST /api/v1/wallet/topup-transactions`

```json
{
  "transactionId": "guid",
  "amount": 200000,
  "type": "WalletTopup",
  "status": "Pending",
  "provider": "SePay",
  "referenceCode": "TOPUP-7F3A9B",
  "expiredAt": "2026-08-03T20:30:00+07:00",
  "payment": {
    "provider": "SePay",
    "qrContent": "bank-transfer-or-qr-payload",
    "accountNumber": "1017588888",
    "accountName": "SWP391 AutoWashPro",
    "bankName": "Vietcombank",
    "amount": 200000,
    "transferContent": "TOPUP-7F3A9B"
  }
}
```

Response field đề xuất:

| Field | Type | Note |
| --- | --- | --- |
| `transactionId` | `Guid` | id transaction pending vừa tạo |
| `amount` | `decimal` | requested amount |
| `type` | `TransactionType` | `WalletTopup` |
| `status` | `TransactionStatus` | `Pending` |
| `provider` | `ProviderType` | `SePay` |
| `referenceCode` | `string` | mã để match webhook |
| `expiredAt` | `DateTimeOffset` | thời điểm hết hạn |
| `payment.provider` | `string` | tên provider |
| `payment.qrContent` | `string` | data cho FE render QR |
| `payment.accountNumber` | `string` | tài khoản nhận tiền |
| `payment.accountName` | `string` | tên chủ tài khoản nhận |
| `payment.bankName` | `string` | ngân hàng nhận |
| `payment.amount` | `decimal` | số tiền cần chuyển |
| `payment.transferContent` | `string` | nội dung chuyển khoản |

### 4.5 `POST /api/v1/payment/sepay/webhook`

Do đây là provider callback, response nên rất ngắn:

```json
{
  "success": true,
  "code": "processed"
}
```

Các giá trị `code` đề xuất:

- `processed`: webhook hợp lệ và đã cộng ví
- `duplicate`: webhook đã được xử lý trước đó
- `ignored`: payload không match pending transaction

### 4.6 `GET /api/v1/admin/wallet-topup-transactions`

```json
{
  "statusCode": 200,
  "message": "Get wallet top-up transactions",
  "data": {
    "items": [
      {
        "transactionId": "guid",
        "customerId": "guid",
        "customerName": "Nguyen Van A",
        "email": "a@example.com",
        "phone": "0901234567",
        "amount": 200000,
        "status": "Succeeded",
        "provider": "SePay",
        "referenceCode": "TOPUP-7F3A9B",
        "externalTransactionId": "92704",
        "paidAt": "2026-08-03T19:14:11+07:00",
        "createdAt": "2026-08-03T19:00:00Z"
      }
    ],
    "totalItems": 1,
    "pageSize": 20,
    "pageIndex": 1
  },
  "errors": null,
  "traceId": "00-abc123"
}
```

Response `data` nên bám `Base.Response.PageResult<T>` vì đây là pattern admin service đang dùng trong hệ thống.

Response item đề xuất:

| Field | Type | Note |
| --- | --- | --- |
| `transactionId` | `Guid` | id transaction |
| `customerId` | `Guid` | id customer |
| `customerName` | `string` | để admin biết ai nạp |
| `email` | `string` | hỗ trợ đối soát |
| `phone` | `string` | hỗ trợ đối soát |
| `amount` | `decimal` | số tiền đã yêu cầu hoặc đã nạp |
| `status` | `TransactionStatus` | trạng thái top-up |
| `provider` | `ProviderType` | `SePay` |
| `referenceCode` | `string?` | mã internal |
| `externalTransactionId` | `string?` | mã từ provider |
| `paidAt` | `DateTimeOffset?` | lúc top-up thành công |
| `createdAt` | `DateTimeOffset` | lúc tạo yêu cầu |
| `totalItems` | `int` | tổng số record sau khi filter |
| `pageSize` | `int` | kích thước trang hiện tại |
| `pageIndex` | `int` | trang hiện tại |

Không thêm `summary` vào v1 để tránh tạo thêm response wrapper riêng cho admin chỉ cho endpoint này. Nếu UI cần tổng tiền theo filter, nên cân nhắc:

- thêm field `TotalAmount` vào response DTO riêng của endpoint này
- hoặc tách endpoint summary riêng để tránh làm `PageResult<T>` lệch pattern chung

## 5. Validation rules

### 5.1 `POST /api/v1/wallet/topup-transactions`

- `amount > 0`
- nên có min amount theo config, ví dụ đọc từ `SystemConfig`
- không tạo top-up nếu user không có wallet hoặc customer profile
- `ReferenceCode` phải unique
- `ExpiredAt` phải lớn hơn thời điểm tạo

### 5.2 `GET /api/v1/transaction`

- `pageIndex >= 1`
- `pageSize >= 1`
- nếu có cả `fromDate` và `toDate` thì `fromDate <= toDate`

### 5.3 `POST /api/v1/payment/sepay/webhook`

- `id` không null hoặc rỗng
- `content` không null hoặc rỗng
- `transferType` phải là `in`
- `transferAmount > 0`
- phải match được transaction có:
  - `Type = WalletTopup`
  - `Status = Pending`
  - `ReferenceCode = content`

### 5.4 `GET /api/v1/admin/wallet-topup-transactions`

- `pageIndex >= 1`
- `pageSize >= 1`
- nếu có cả `minAmount` và `maxAmount` thì `minAmount <= maxAmount`
- nếu có cả `fromDate` và `toDate` thì `fromDate <= toDate`
- chỉ trả transaction có `Type = WalletTopup`
- chỉ trả transaction có `CustomerId` hợp lệ và join được thông tin customer để admin đối soát

## 6. Business rules

### 6.1 Wallet top-up

1. Customer gọi `POST /api/v1/wallet/topup-transactions`.
2. Backend tạo `Transaction`:
   - `Type = WalletTopup`
   - `Status = Pending`
   - `Provider = SePay`
   - `ReferenceCode = generated value`
   - `Amount = request.amount`
   - `ExpiredAt = now + expiry window`
3. Backend trả payment instruction cho frontend.
4. Customer chuyển khoản.
5. SePay gọi webhook.
6. Backend verify payload, update transaction sang `Succeeded`, cộng ví và lưu audit fields.

### 6.2 Booking payment

Theo repo hiện tại, payment cho booking vẫn đi qua flow nội bộ:

- `Deposit`
- `FullPayment`
- có thể có `Refund`

Không cần public API payment riêng mới nếu booking service vẫn tự tạo transaction nội bộ trong quá trình business flow.

### 6.3 Duplicate webhook

- không tạo transaction mới
- không cộng ví thêm lần nữa
- vẫn trả HTTP thành công cho SePay để tránh retry vô hạn

### 6.4 Admin top-up monitoring

- admin phải xem được ai là người tạo giao dịch top-up
- admin phải xem được số tiền của từng giao dịch
- admin nên filter được theo trạng thái để phân biệt:
  - top-up đang chờ chuyển khoản
  - top-up đã thành công
  - top-up thất bại hoặc hết hạn
- danh sách admin chỉ nên lấy các transaction có `Type = WalletTopup`

## 7. Error cases

### 7.1 `POST /api/v1/wallet/topup-transactions`

| HTTP | Case |
| --- | --- |
| `400` | amount không hợp lệ |
| `401` | chưa đăng nhập |
| `403` | user không phải customer |
| `404` | không tìm thấy customer profile hoặc wallet |
| `409` | phát sinh conflict do duplicate `ReferenceCode` |
| `500` | lỗi tạo top-up transaction |

### 7.2 `GET /api/v1/transaction/{id}`

| HTTP | Case |
| --- | --- |
| `401` | chưa đăng nhập |
| `403` | transaction không thuộc user hiện tại |
| `404` | không tìm thấy transaction |

### 7.3 `POST /api/v1/payment/sepay/webhook`

| HTTP | Case |
| --- | --- |
| `400` | payload thiếu field bắt buộc |
| `404` | không match được pending transaction |
| `409` | amount mismatch hoặc trạng thái không hợp lệ |
| `200` | duplicate webhook đã được xử lý trước đó |
| `500` | lỗi nội bộ khi xử lý callback |

### 7.4 `GET /api/v1/admin/wallet-topup-transactions`

| HTTP | Case |
| --- | --- |
| `400` | query filter không hợp lệ |
| `401` | chưa đăng nhập |
| `403` | không có quyền admin |
| `404` | không cần dùng cho danh sách; trường hợp không có dữ liệu nên trả danh sách rỗng |
| `500` | lỗi lấy danh sách top-up |

## 8. Authorization

| API | Authorization |
| --- | --- |
| `GET /api/v1/wallet` | JWT customer |
| `GET /api/v1/transaction` | JWT customer |
| `GET /api/v1/transaction/{id}` | JWT customer và chỉ xem transaction của chính mình |
| `POST /api/v1/wallet/topup-transactions` | JWT customer |
| `POST /api/v1/payment/sepay/webhook` | không dùng JWT customer; phải xác thực bằng cơ chế provider webhook |
| `GET /api/v1/admin/wallet-topup-transactions` | JWT admin, policy `AdminPolicy` |

Ghi chú:

- webhook không nên dùng `[Authorize]` kiểu customer token
- nếu SePay có secret hoặc signature thì phải verify trước khi xử lý business

## 9. Example request

### 9.1 Create top-up transaction

```http
POST /api/v1/wallet/topup-transactions
Authorization: Bearer <customer-jwt>
Content-Type: application/json

{
  "amount": 200000
}
```

### 9.2 Get transaction list

```http
GET /api/v1/transaction?pageIndex=1&pageSize=20&type=WalletTopup&status=Pending
Authorization: Bearer <customer-jwt>
```

### 9.3 SePay webhook

```http
POST /api/v1/payment/sepay/webhook
Content-Type: application/json

{
  "id": "92704",
  "gateway": "Vietcombank",
  "transactionDate": "2026-08-03T19:14:11+07:00",
  "accountNumber": "1017588888",
  "code": "SEVN63DC8E5C",
  "content": "TOPUP-7F3A9B",
  "transferType": "in",
  "description": "Nap vi TOPUP-7F3A9B",
  "transferAmount": 200000,
  "referenceCode": "FT24012345678"
}
```

### 9.4 Admin get wallet top-up transactions

```http
GET /api/v1/admin/wallet-topup-transactions?pageIndex=1&pageSize=20&status=Succeeded&minAmount=100000
Authorization: Bearer <admin-jwt>
```

## 10. Example response

### 10.1 Create top-up transaction

```json
{
  "transactionId": "64515d1f-4692-48b0-a4d1-44b7303c79d4",
  "amount": 200000,
  "type": "WalletTopup",
  "status": "Pending",
  "provider": "SePay",
  "referenceCode": "TOPUP-7F3A9B",
  "expiredAt": "2026-08-03T20:30:00+07:00",
  "payment": {
    "provider": "SePay",
    "qrContent": "000201010212...",
    "accountNumber": "1017588888",
    "accountName": "SWP391 AutoWashPro",
    "bankName": "Vietcombank",
    "amount": 200000,
    "transferContent": "TOPUP-7F3A9B"
  }
}
```

### 10.2 Webhook processed

```json
{
  "success": true,
  "code": "processed"
}
```

### 10.3 Admin top-up list

```json
{
  "statusCode": 200,
  "message": "Get wallet top-up transactions",
  "data": {
    "items": [
      {
        "transactionId": "64515d1f-4692-48b0-a4d1-44b7303c79d4",
        "customerId": "91fd3d36-635a-4bf9-8e12-8d95d6d5d311",
        "customerName": "Nguyen Van A",
        "email": "a@example.com",
        "phone": "0901234567",
        "amount": 200000,
        "status": "Succeeded",
        "provider": "SePay",
        "referenceCode": "TOPUP-7F3A9B",
        "externalTransactionId": "92704",
        "paidAt": "2026-08-03T19:14:11+07:00",
        "createdAt": "2026-08-03T19:00:00Z"
      }
    ],
    "totalItems": 1,
    "pageSize": 20,
    "pageIndex": 1
  },
  "errors": null,
  "traceId": "00-abc123"
}
```

## 11. Implementation notes

### 11.1 Route design

- giữ `TransactionController` cho history API vì repo đã dùng resource `transaction`
- đặt API tạo top-up dưới `wallet` vì đây là hành động của owner wallet
- đặt webhook dưới `payment` vì đây là provider integration endpoint
- đặt API admin dưới `admin` để bám pattern `AdminController` đang có trong repo
- action admin nên nhận `[FromQuery] Request.GetWalletTopupTransactionsRequest request`

### 11.2 Backward compatibility

`PATCH /api/v1/wallet/top-up` hiện tại không còn phù hợp vì:

- cộng ví trực tiếp trước khi có xác nhận từ SePay
- không tạo pending state đúng thiết kế
- không chống duplicate webhook

Khuyến nghị:

- đánh dấu deprecated ngay trong Swagger
- ngừng gọi endpoint này từ frontend mới
- sau khi migration xong, chuyển logic top-up sang `POST /api/v1/wallet/topup-transactions`

### 11.3 DTO notes

- không trả EF entity trực tiếp
- không expose `RawPayload` cho customer API
- response `GET /api/v1/transaction` và `GET /api/v1/transaction/{id}` nên mở rộng từ DTO hiện có thay vì đổi sang shape hoàn toàn mới

### 11.4 Suggested controller split

- `WalletController`
  - `GET /api/v1/wallet`
  - `POST /api/v1/wallet/topup-transactions`
- `TransactionController`
  - `GET /api/v1/transaction`
  - `GET /api/v1/transaction/{id}`
- `PaymentController`
  - `POST /api/v1/payment/sepay/webhook`
- `AdminController`
  - `GET /api/v1/admin/wallet-topup-transactions`

### 11.5 Suggested service contract

Vì `AdminController` hiện đang có stub route nhưng `IService` mới chỉ có `Task<string> GetWalletTopUpTransaction();`, nên contract nên đổi thành:

```csharp
Task<Base.Response.PageResult<Response.WalletTopupTransactionItemResponse>> GetWalletTopupTransactions(
    Request.GetWalletTopupTransactionsRequest request);
```

Controller shape tương ứng:

```csharp
[HttpGet("wallet-topup-transactions")]
public async Task<IActionResult> GetWalletTopUpTransactions(
    [FromQuery] Request.GetWalletTopupTransactionsRequest request)
{
    var result = await _adminService.GetWalletTopupTransactions(request);
    return Ok(ApiResponseFactory.SuccessResponse(
        result,
        "Get wallet top-up transactions",
        HttpContext.TraceIdentifier));
}
```

### 11.6 Assumptions

- flow SePay hiện chỉ áp dụng cho `WalletTopup`
- `Deposit`, `FullPayment`, `Refund` vẫn là internal transaction flow
- admin cần nhìn thấy customer nào đã nạp tiền và số tiền tương ứng
