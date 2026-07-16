# Kế hoạch kiểm thử Personalized Promotion/Voucher bằng Swagger

## 1. Mục tiêu

Tài liệu này hướng dẫn kiểm thử thủ công tính năng voucher cá nhân hóa trên đúng code hiện tại của AutoWashPro BE. Mỗi trường hợp kiểm thử xác định:

- Điều kiện dữ liệu phải có trước khi chạy.
- Request thực hiện trên Swagger.
- Cách kích hoạt thật nếu voucher được cấp bởi Quartz hoặc luồng booking.
- HTTP status và response mong đợi.
- Dữ liệu cần kiểm tra trong PostgreSQL.
- Cách hoàn nguyên dữ liệu test.

Tài liệu không tạo endpoint chạy job thủ công và không giả định một API không tồn tại trong codebase.

## 2. Phạm vi và ngoài phạm vi

### 2.1. Trong phạm vi

- Quản trị `Promotion` và `PersonalizedPromotionRule`.
- Birthday voucher.
- Inactive/Win-back voucher (`InactiveCustomer`).
- Welcome voucher.
- No-first-booking voucher (`NoFirstBooking`).
- Tier Upgrade voucher.
- In-app notification, email delivery và retry.
- Danh sách voucher của Customer, validate voucher legacy và sử dụng voucher khi booking.
- Báo cáo Admin.
- Regression Reward → Voucher, điểm, số lượng Reward và promotion hiện tại.
- Authorization, validation, idempotency và ownership.

### 2.2. Ngoài phạm vi

- Load/performance test.
- Kiểm tra giao diện frontend.
- Kiểm thử SMTP/SignalR ở mức hạ tầng production.
- Tự tạo hoặc tự chạy migration trong tài liệu này.
- Các trigger chưa có trong enum/code: Wash Milestone, Service Recovery, Expected Wash Due, Branch Affinity, Off-peak và Near-tier-upgrade.

## 3. Kết luận kiểm tra codebase trước khi test

### 3.1. Trigger đang có thật

Enum `PersonalizedVoucherTriggerType` hiện chỉ có:

1. `Birthday`
2. `InactiveCustomer`
3. `Welcome`
4. `NoFirstBooking`
5. `TierUpgrade`

Các enum gửi qua Swagger là chuỗi PascalCase đúng như trên. Giá trị lưu trong PostgreSQL lần lượt là `birthday`, `inactive_customer`, `welcome`, `no_first_booking`, `tier_upgrade`.

### 3.2. Cơ chế kích hoạt thật

| Trigger | Phân loại | Cơ chế thực tế |
|---|---|---|
| Birthday | Quartz Scheduled Job + Requires Test Database Setup | `ProcessBirthdayVoucherJob` gọi `ProcessBirthdayAsync` |
| InactiveCustomer | Quartz Scheduled Job + Requires Test Database Setup | `ProcessInactiveCustomerVoucherJob` gọi `ProcessInactiveCustomersAsync` |
| Welcome | Indirect Swagger + Quartz Scheduled Job | Admin duyệt tài khoản bằng Swagger; `ProcessAcquisitionVoucherJob` cấp voucher sau đó |
| NoFirstBooking | Quartz Scheduled Job + Requires Test Database Setup | `ProcessAcquisitionVoucherJob`; cần chỉnh tuổi tài khoản trong DB test |
| TierUpgrade | Indirect Swagger + Domain Event-like application flow | Được gọi sau khi check-in booking cập nhật tier và commit thành công |

Không có public/internal endpoint để bấm chạy các Quartz job. Không dùng endpoint giả như `/trigger`, `/run-job` hoặc `/issue-voucher`.

### 3.3. Các điều kiện chặn test hiện tại

1. Source hiện có entity/mapping mới nhưng **chưa có migration** chứa `personalized_promotion_rule`, `personalized_voucher_issuance`, `customer_date_of_birth_correction`, `user.verified_at`, `customer_profile.date_of_birth` và `date_of_birth_set_at`. Người chạy test phải tự tạo/apply migration trên DB local/test trước khi mở Swagger. Nếu không, các API liên quan có thể trả `500` do thiếu bảng/cột.
2. Không chạy các test này trên DB production hoặc DB dùng chung. Test có chỉnh trực tiếp ngày sinh, `last_login_at`, `verified_at`, `created_at`, trạng thái account và số lượt rửa.
3. Các file `appsettings*.json` hiện chưa khai báo section `PersonalizedVoucher`/cron mới; code dùng giá trị mặc định nếu thiếu.

## 4. Môi trường và cấu hình bắt buộc

### 4.1. Thành phần

- Backend chạy profile HTTP mặc định: `http://localhost:5207`.
- Swagger UI: `http://localhost:5207/swagger/index.html`.
- PostgreSQL local/test đã có toàn bộ migration của model hiện tại.
- Redis có thể kết nối để các API auth hiện tại hoạt động.
- Tài khoản Admin và ít nhất bốn tài khoản Customer test riêng biệt.
- Một branch active, một vehicle thuộc từng Customer cần booking, wallet đủ tiền.
- Ít nhất hai tier liên tiếp, ví dụ tier level 1 và level 2.

### 4.2. Cấu hình Quartz khuyến nghị cho test local

Chỉ bật nhanh job đang test và tắt/deactivate rule của trigger khác để tránh job chạy chéo. Ví dụ cấu hình local do tester tự quản lý:

```json
{
  "PersonalizedVoucher": {
    "BatchSize": 100,
    "DeliveryMaxAttempts": 3,
    "DeliveryRetryDelayMinutes": 1,
    "TimeZoneId": "Asia/Ho_Chi_Minh"
  },
  "Quartz": {
    "BirthdayVoucherCron": "0/20 * * * * ?",
    "InactiveCustomerVoucherCron": "5/20 * * * * ?",
    "AcquisitionVoucherCron": "10/20 * * * * ?",
    "PersonalizedVoucherDeliveryRetryCron": "15/20 * * * * ?"
  }
}
```

Sau khi đổi cấu hình phải restart backend. Khi hoàn tất, trả cron về cấu hình của môi trường. Cron mặc định trong code:

- Birthday: `0 0 1 * * ?`
- Inactive: `0 15 1 * * ?`
- Acquisition: `0 0/15 * * * ?`
- Delivery retry: `0 0/10 * * * ?`

Tất cả job cá nhân hóa dùng timezone `Asia/Ho_Chi_Minh` mặc định và có `[DisallowConcurrentExecution]`.

### 4.3. Quy tắc chạy an toàn

- Mỗi lần chỉ để một campaign/rule cần test ở trạng thái active.
- Tên dữ liệu test dùng prefix `SWAGGER-PV-<ngày>-<trigger>` để dễ tìm và cleanup.
- Không login lại Customer sau khi đã chỉnh `last_login_at` cho test Inactive vì `POST /api/v1/auth/login` sẽ cập nhật `last_login_at = now`.
- Ghi lại `USER_ID` và `CUSTOMER_ID`; hai ID này khác nhau.
- Các câu SQL trong tài liệu chỉ được dùng trên DB test.

## 5. Contract response và authorization

### 5.1. Envelope API mới

Các API mới dùng `ApiResponseFactory` trả camelCase:

```json
{
  "success": true,
  "message": "...",
  "data": {},
  "errors": null,
  "traceId": "...",
  "timestampUtc": "2026-07-16T00:00:00Z"
}
```

Khi service ném `ArgumentException` hoặc `InvalidOperationException`, middleware trả `400`:

```json
{
  "success": false,
  "message": "Thông báo cụ thể từ service",
  "data": null,
  "errors": {
    "detail": "Chỉ có trong Development"
  },
  "traceId": "...",
  "timestampUtc": "..."
}
```

`KeyNotFoundException` → `404`; `UnauthorizedAccessException` → `401`; `ForbiddenAccessException` → `403`. Lỗi `Exception` chung của API legacy/booking hiện bị map thành `500` với message `An unexpected error occurred`; trong Development mới thấy `errors.detail`.

### 5.2. API legacy không dùng envelope

Các endpoint `/Promotion/...`, `/Reward/...`, `/Voucher/...`, `/api/v1/bookings` trả trực tiếp string/DTO/page result. Không kỳ vọng trường `success` ở các response này.

### 5.3. Token

Login dùng `multipart/form-data`:

```text
POST /api/v1/auth/login
identifier = <email hoặc phone>
password   = <password>
```

Response `200`:

```json
{
  "success": true,
  "message": "Login successfully",
  "data": {
    "access_token": "<JWT>",
    "isVerify": true
  },
  "errors": null,
  "traceId": "...",
  "timestampUtc": "..."
}
```

Trên Swagger bấm **Authorize** và nhập `Bearer <JWT>`.

## 6. Test data registry

Điền bảng này trước khi chạy. Không ghi mật khẩu/token thật vào commit.

| Biến | Giá trị |
|---|---|
| `BASE_URL` | `http://localhost:5207` |
| `ADMIN_USER_ID` | `<uuid>` |
| `ADMIN_TOKEN` | `<jwt>` |
| `BIRTHDAY_USER_ID` / `BIRTHDAY_CUSTOMER_ID` | `<uuid>` / `<uuid>` |
| `INACTIVE_USER_ID` / `INACTIVE_CUSTOMER_ID` | `<uuid>` / `<uuid>` |
| `ACQUISITION_USER_ID` / `ACQUISITION_CUSTOMER_ID` | `<uuid>` / `<uuid>` |
| `TIER_USER_ID` / `TIER_CUSTOMER_ID` | `<uuid>` / `<uuid>` |
| `CUSTOMER_B_TOKEN` | `<jwt của customer khác>` |
| `CURRENT_TIER_ID` | `<uuid>` |
| `NEXT_TIER_ID` | `<uuid>` |
| `NEXT_TIER_REQUIRED_WASHES` | `<int>` |
| `BRANCH_ID` | `<uuid>` |
| `VEHICLE_ID` | `<uuid thuộc đúng customer>` |
| `PROMO_BIRTHDAY_ID` / `RULE_BIRTHDAY_ID` | `<uuid>` / `<uuid>` |
| `PROMO_INACTIVE_7_ID` / `RULE_INACTIVE_7_ID` | `<uuid>` / `<uuid>` |
| `PROMO_INACTIVE_30_ID` / `RULE_INACTIVE_30_ID` | `<uuid>` / `<uuid>` |
| `PROMO_WELCOME_ID` / `RULE_WELCOME_ID` | `<uuid>` / `<uuid>` |
| `PROMO_NO_FIRST_ID` / `RULE_NO_FIRST_ID` | `<uuid>` / `<uuid>` |
| `PROMO_TIER_ID` / `RULE_TIER_ID` | `<uuid>` / `<uuid>` |
| `ISSUANCE_ID` / `VOUCHER_ID` / `VOUCHER_CODE` | `<sau khi được cấp>` |
| `BOOKING_ID` | `<uuid>` |

Lấy cặp ID từ DB test:

```sql
SELECT u.id AS user_id,
       cp.id AS customer_id,
       u.email,
       u.status,
       u.is_verify,
       u.last_login_at,
       u.verified_at,
       cp.tier_id,
       cp.total_points,
       cp.total_washes,
       cp.date_of_birth
FROM "user" u
JOIN customer_profile cp ON cp.user_id = u.id
WHERE u.email LIKE '%@example.test';
```

## 7. Traceability matrix

| ID | Requirement | Test case |
|---|---|---|
| R-PV-01 | Rule CRUD/status/auth/validation | TC-001 → TC-006 |
| R-PV-02 | Birthday đúng ngày, sai ngày, năm, leap day | TC-007 → TC-012 |
| R-PV-03 | Inactive threshold, null login, priority, retry | TC-013 → TC-019 |
| R-PV-04 | Welcome sau verify, một lần, first booking | TC-020 → TC-023 |
| R-PV-05 | No-first age, no booking, acquisition precedence | TC-024 → TC-027 |
| R-PV-06 | Tier upgrade qua check-in thật | TC-028 → TC-030 |
| R-PV-07 | List/validate/use/status/ownership/expiry | TC-031 → TC-037 |
| R-PV-08 | Report và delivery counts | TC-038 → TC-039 |
| R-PV-09 | Reward/promotion/point regression | TC-040 → TC-045 |
| R-PV-10 | DOB set/correction/audit | TC-046 → TC-048 |

## 8. Thiết lập Promotion và rule chung

### 8.1. Lấy tier

Swagger:

```text
GET /api/v1/tiers
```

Expected `200` raw response có `data[]`, mỗi phần tử có `id`, `name`, `level`, `requiredWashes`, `priorityBookingDays`.

Chuẩn bị dữ liệu booking cho đúng Customer token:

```text
GET /api/v1/branches?isActive=true
GET /api/v1/vehicles?page=1&pageSize=20
GET /api/v1/wallet
```

Chọn `BRANCH_ID` active và `VEHICLE_ID` từ chính response của Customer. Nếu wallet không đủ tiền, trên DB test có thể top-up bằng Swagger:

```text
PATCH /api/v1/wallet/top-up
```

```json
{
  "balance": 1000000
}
```

Expected `200` raw DTO có `message="Wallet topped up successfully"` và `balance` bằng số dư cũ cộng `1000000`.

### 8.2. Tạo Promotion global dùng cho batch test

Swagger, Admin token:

```text
POST /Promotion/admin/create-promotion
Content-Type: application/json
```

```json
{
  "name": "SWAGGER-PV-20260716-BIRTHDAY",
  "description": "Swagger personalized voucher test only",
  "discountType": "Percentage",
  "discountValue": 15,
  "startDate": "2026-07-15T00:00:00+07:00",
  "endDate": "2026-08-15T23:59:59+07:00",
  "isGlobal": true,
  "tierIds": []
}
```

Expected: `200`, raw JSON string `"Promotion created successfully"`. Sau đó lấy ID bằng:

```text
GET /Promotion/admin/promotions?searchTerm=SWAGGER-PV-20260716-BIRTHDAY&pageSize=20&pageIndex=1
```

Expected `200`:

```json
{
  "items": [
    {
      "id": "<PROMOTION_ID>",
      "name": "SWAGGER-PV-20260716-BIRTHDAY",
      "discountType": "Percentage",
      "discountValue": 15,
      "isGlobal": true,
      "isActive": true
    }
  ],
  "totalItems": 1,
  "pageSize": 20,
  "pageIndex": 1
}
```

Lưu ý: endpoint GET promotions hiện không có `[Authorize]`. Đây là hành vi hiện tại, không phải lý do bỏ token cho các API Admin còn lại.

Riêng Tier Upgrade phải tạo Promotion non-global chỉ dành cho tier mới:

```text
POST /Promotion/admin/create-promotion
```

```json
{
  "name": "SWAGGER-PV-20260716-TIER-UPGRADE",
  "description": "Voucher for reaching the next tier",
  "discountType": "FixedAmount",
  "discountValue": 25000,
  "startDate": "2026-07-15T00:00:00+07:00",
  "endDate": "2026-08-15T23:59:59+07:00",
  "isGlobal": false,
  "tierIds": [
    "<NEXT_TIER_ID>"
  ]
}
```

Expected `200`, raw string `"Promotion created successfully"`. Không gắn promotion này vào `CURRENT_TIER_ID`, vì service chọn rule TierUpgrade theo tier mới sau check-in.

### 8.3. Body rule chuẩn theo trigger

Birthday:

```json
{
  "promotionId": "<PROMO_BIRTHDAY_ID>",
  "triggerType": "Birthday",
  "thresholdDays": null,
  "voucherValidityDays": 14,
  "priority": 10,
  "isActive": true,
  "sendInAppNotification": true,
  "sendEmail": false,
  "notificationTitleTemplate": "Chúc mừng sinh nhật {CustomerName}",
  "notificationContentTemplate": "Bạn nhận {Discount} từ {PromotionName}. Mã {VoucherCode}, hạn {ExpiresAt}.",
  "emailSubjectTemplate": null,
  "emailBodyTemplate": null,
  "callToActionUrl": "https://example.test/booking"
}
```

Inactive 30 ngày:

```json
{
  "promotionId": "<PROMO_INACTIVE_30_ID>",
  "triggerType": "InactiveCustomer",
  "thresholdDays": 30,
  "voucherValidityDays": 10,
  "priority": 10,
  "isActive": true,
  "sendInAppNotification": true,
  "sendEmail": true,
  "notificationTitleTemplate": "AutoWashPro nhớ bạn",
  "notificationContentTemplate": "Mã {VoucherCode} giảm {Discount}, hạn {ExpiresAt}.",
  "emailSubjectTemplate": "Ưu đãi quay lại cho {CustomerName}",
  "emailBodyTemplate": "Xin chào {CustomerName}, {PromotionName} tặng {Discount}, hết hạn {ExpiresAt}. Đặt lịch tại {BookingUrl}. Mã {VoucherCode}.",
  "callToActionUrl": "https://example.test/booking"
}
```

Welcome:

```json
{
  "promotionId": "<PROMO_WELCOME_ID>",
  "triggerType": "Welcome",
  "thresholdDays": null,
  "voucherValidityDays": 7,
  "priority": 20,
  "isActive": true,
  "sendInAppNotification": true,
  "sendEmail": false,
  "notificationTitleTemplate": "Chào mừng {CustomerName}",
  "notificationContentTemplate": "Mã {VoucherCode} giảm {Discount}, hạn {ExpiresAt}.",
  "emailSubjectTemplate": null,
  "emailBodyTemplate": null,
  "callToActionUrl": "https://example.test/booking"
}
```

No-first-booking:

```json
{
  "promotionId": "<PROMO_NO_FIRST_ID>",
  "triggerType": "NoFirstBooking",
  "thresholdDays": 7,
  "voucherValidityDays": 7,
  "priority": 10,
  "isActive": true,
  "sendInAppNotification": true,
  "sendEmail": false,
  "notificationTitleTemplate": "Ưu đãi cho lần đặt lịch đầu tiên",
  "notificationContentTemplate": "Mã {VoucherCode} giảm {Discount}, hạn {ExpiresAt}.",
  "emailSubjectTemplate": null,
  "emailBodyTemplate": null,
  "callToActionUrl": "https://example.test/booking"
}
```

Tier Upgrade, dùng Promotion chỉ gắn `NEXT_TIER_ID`:

```json
{
  "promotionId": "<PROMO_TIER_ID>",
  "triggerType": "TierUpgrade",
  "thresholdDays": null,
  "voucherValidityDays": 14,
  "priority": 10,
  "isActive": true,
  "sendInAppNotification": true,
  "sendEmail": false,
  "notificationTitleTemplate": "Quà nâng hạng cho {CustomerName}",
  "notificationContentTemplate": "Mã {VoucherCode} giảm {Discount}, hạn {ExpiresAt}.",
  "emailSubjectTemplate": null,
  "emailBodyTemplate": null,
  "callToActionUrl": "https://example.test/booking"
}
```

Tạo rule bằng:

```text
POST /api/v1/admin/personalized-promotion-rules
```

Expected `200`, `message = "Create personalized promotion rule successfully"`, `data.id` khác rỗng và các trường trong `data` khớp body. Lưu `data.id` làm `RULE_*_ID`.

### 8.4. Query xác nhận voucher/issuance chung

```sql
SELECT i.id AS issuance_id,
       i.customer_id,
       i.promotion_id,
       i.promotion_rule_id,
       i.voucher_id,
       i.trigger_type,
       i.cycle_key,
       i.trigger_reference,
       i.notification_status,
       i.notification_attempt_count,
       i.notification_last_error,
       i.email_status,
       i.email_attempt_count,
       i.email_last_error,
       i.created_at,
       v.code,
       v.reward_id,
       v.promotion_id AS voucher_promotion_id,
       v.status AS voucher_status,
       v.discount_type,
       v.discount_value,
       v.expires_at,
       v.used_at
FROM personalized_voucher_issuance i
JOIN voucher v ON v.id = i.voucher_id
WHERE i.customer_id = '<CUSTOMER_ID>'::uuid
ORDER BY i.created_at DESC;
```

Điều kiện đúng cho mọi voucher cá nhân hóa:

- `reward_id IS NULL`.
- `voucher_promotion_id = issuance.promotion_id`.
- Code bắt đầu bằng `PV-`.
- `voucher_status = 'active'` ngay sau khi cấp.
- `expires_at` là thời điểm nhỏ hơn giữa `now + voucherValidityDays` và `promotion.end_date`.
- Chỉ một row cho `(customer_id, trigger_type, cycle_key)`.

## 9. Test cases quản trị rule

### TC-001 — Admin tạo Birthday rule thành công

- Phân loại: Direct Swagger.
- Tiền điều kiện: `PROMO_BIRTHDAY_ID` tồn tại, active, trong thời hạn; Admin token hợp lệ; chưa có rule cùng promotion/trigger/threshold.
- Thực hiện: `POST /api/v1/admin/personalized-promotion-rules` với body Birthday ở mục 8.3.
- Expected HTTP/response: `200`; `success=true`; `message="Create personalized promotion rule successfully"`; `data.triggerType="Birthday"`; `data.thresholdDays=null`; `data.isActive=true`.
- DB: một row `personalized_promotion_rule` có `trigger_type='birthday'`.
- Cleanup: `PATCH /api/v1/admin/personalized-promotion-rules/{id}/status` body `{"isActive":false}`.

### TC-002 — Truy cập API rule không có token

- Phân loại: Direct Swagger.
- Thực hiện: bỏ Authorize rồi gọi `GET /api/v1/admin/personalized-promotion-rules?pageIndex=1&pageSize=10`.
- Expected: `401 Unauthorized`; response có thể rỗng vì do ASP.NET authorization pipeline trả, không bắt buộc envelope.
- DB: không thay đổi.
- Cleanup: không có.

### TC-003 — Customer token truy cập API Admin

- Phân loại: Direct Swagger.
- Thực hiện: Authorize bằng Customer token, gọi cùng endpoint TC-002.
- Expected: `403 Forbidden`; không có dữ liệu rule bị lộ trong response.
- DB: không thay đổi.
- Cleanup: không có.

### TC-004 — Inactive rule thiếu placeholder bắt buộc

- Phân loại: Direct Swagger.
- Tiền điều kiện: Promotion hợp lệ.
- Input: body Inactive nhưng đổi `emailBodyTemplate` thành `"Xin chào {CustomerName}, ưu đãi {Discount}."`.
- Expected: `400`; `success=false`; message lần lượt báo placeholder đầu tiên bị thiếu, ví dụ `Inactive email template must contain {PromotionName}.`.
- DB: không tạo rule.
- Cleanup: không có.

### TC-005 — Threshold sai theo trigger

- Phân loại: Direct Swagger.
- Thực hiện A: Birthday với `thresholdDays=7`.
- Expected A: `400`, message `ThresholdDays is not supported for this trigger.`
- Thực hiện B: NoFirstBooking với `thresholdDays=null`.
- Expected B: `400`, message `ThresholdDays must be greater than 0 for this trigger.`
- DB: không tạo rule.
- Cleanup: không có.

### TC-006 — Duplicate rule và bật/tắt rule

- Phân loại: Direct Swagger.
- Tiền điều kiện: rule từ TC-001 tồn tại.
- Thực hiện A: POST lại cùng `promotionId`, `triggerType`, `thresholdDays`.
- Expected A: `400`; message `A rule with the same promotion, trigger, and threshold already exists.`
- Thực hiện B: `PATCH /api/v1/admin/personalized-promotion-rules/{RULE_ID}/status`, body `{"isActive":false}`.
- Expected B: `200`, `data.isActive=false`, `updatedAt` có giá trị.
- DB: vẫn chỉ một rule; status false.
- Cleanup: bật lại nếu các case sau cần dùng.

## 10. Birthday voucher

### TC-007 — Cấp Birthday voucher đúng ngày

- Phân loại: Quartz Scheduled Job + Requires Test Database Setup.
- Tiền điều kiện: rule Birthday active; account Customer `active`, `is_verify=true`; Promotion active/còn hạn; không có issuance Birthday năm hiện tại.
- Chuẩn bị bằng Swagger: Customer có thể đặt DOB lần đầu qua `PATCH /api/v1/me` body `{"dateOfBirth":"<YYYY-MM-DD có tháng/ngày hôm nay tại Asia/Ho_Chi_Minh>"}`. Expected `200`, `data="Update customer profile successfully."`. Nếu DOB đã có, Admin dùng `PATCH /api/v1/admin/customers/{USER_ID}/date-of-birth` với body `{"dateOfBirth":"...","reason":"Prepare TC-007 birthday test"}`; expected `200` và `data="Customer date of birth corrected successfully."`.
- Kích hoạt: chờ `ProcessBirthdayVoucherJob` theo cron test. Không login lại nếu dùng chung customer cho Inactive.
- Expected Swagger xác nhận: `GET /api/v1/vouchers?pageSize=20&pageIndex=1` trả `200`, `data.items` có đúng một item `source="Promotion"`, `promotionName` đúng campaign, `triggerType="Birthday"`, `cycleKey="BIRTHDAY:<năm local>"`, `status="Active"`.
- DB: đúng một voucher/issuance; `trigger_reference` là ngày local dạng `yyyy-MM-dd`; notification `sent` nếu SignalR/DB notification thành công.
- Cleanup: deactivate Birthday rule; khôi phục DOB bằng Admin correction nếu cần giữ user; xóa issuance/voucher chỉ khi DB test cô lập và voucher chưa gắn booking.

### TC-008 — Birthday sai ngày không được cấp

- Phân loại: Quartz Scheduled Job + Requires Test Database Setup.
- Tiền điều kiện: Customer đủ điều kiện nhưng DOB là ngày mai hoặc hôm qua theo timezone local; rule active.
- Kích hoạt: chờ job một lượt.
- Expected: `GET /api/v1/vouchers` không có item mới với `triggerType="Birthday"` và cycle năm hiện tại.
- DB: count issuance Birthday của customer không tăng.
- Cleanup: khôi phục DOB.

### TC-009 — Rerun Birthday cùng năm không trùng

- Phân loại: Quartz Scheduled Job + idempotency.
- Tiền điều kiện: TC-007 đã cấp thành công.
- Kích hoạt: chờ thêm ít nhất hai lượt job với cùng ngày/năm.
- Expected: danh sách voucher vẫn chỉ có một item có `cycleKey="BIRTHDAY:<year>"`.
- DB: `COUNT(*) = 1` cho customer/`birthday`/cycle. Không có HTTP 500 trong log job.
- Cleanup: như TC-007.

### TC-010 — Promotion hoặc rule inactive/expired

- Phân loại: Direct Swagger setup + Quartz Scheduled Job.
- Biến thể A: tắt rule qua status API.
- Biến thể B: bật rule, tắt Promotion qua `PATCH /Promotion/admin/update-promotion-status/{PROMOTION_ID}`, body `{"isActive":false}`.
- Biến thể C: dùng Promotion có `endDate < now`.
- Kích hoạt: mỗi biến thể chờ một lượt Birthday job trên customer chưa có cycle.
- Expected: không có voucher/issuance mới.
- Cleanup: bật lại đúng trạng thái và ngày nếu dùng lại dữ liệu.

### TC-011 — Account Locked/Inactive/Unverified bị bỏ qua

- Phân loại: Direct Swagger setup + Quartz Scheduled Job.
- Locked/Inactive: `PATCH /api/v1/admin/users/{USER_ID}/status`, body lần lượt `{"status":"Locked"}` hoặc `{"status":"Inactive"}`; expected setup `200`.
- Unverified: dùng account mới đăng ký nhưng chưa được duyệt; status `Pending`, `isVerify=false`.
- Kích hoạt: chờ job.
- Expected: không có voucher/issuance Birthday cho từng account.
- Cleanup: đổi account đã verify về `Active`; account pending giữ nguyên hoặc xóa trên DB test.

### TC-012 — Sinh ngày 29/02

- Phân loại: Quartz Scheduled Job + Requires Test Database Setup.
- Tiền điều kiện: DOB `2000-02-29`.
- Trường hợp năm không nhuận: chỉnh clock/môi trường test hoặc chạy integration test tại local date 28/02; expected được cấp vào 28/02, không cấp 01/03.
- Trường hợp năm nhuận: expected chỉ cấp 29/02, không cấp 28/02.
- Expected voucher: cycle `BIRTHDAY:<year>`.
- Ghi chú: Swagger không điều khiển clock; nếu không có test clock thì case này bị blocked ở manual Swagger và phải xác nhận bằng integration test `LeapDayBirthday_UsesFebruary28OnlyInNonLeapYears`.
- Cleanup: khôi phục DOB/clock và deactivate rule.

## 11. Inactive/Win-back voucher

### TC-013 — Đủ threshold 30 ngày được cấp

- Phân loại: Quartz Scheduled Job + Requires Test Database Setup.
- Tiền điều kiện: Inactive 30 rule active, email template hợp lệ; Customer active/verified; Promotion active/còn hạn.
- DB setup:

```sql
UPDATE "user"
SET last_login_at = now() - interval '31 days'
WHERE id = '<INACTIVE_USER_ID>'::uuid;
```

- Kích hoạt: không login lại Customer; chờ `ProcessInactiveCustomerVoucherJob`.
- Expected Swagger: `GET /api/v1/vouchers` có một item `triggerType="InactiveCustomer"`; `cycleKey` bắt đầu `INACTIVE:30:`.
- DB: voucher vẫn tồn tại dù email fail; `trigger_reference` là timestamp login ISO; email status `sent` nếu SMTP thành công hoặc `failed` nếu SMTP lỗi.
- Cleanup: deactivate rule; restore `last_login_at` sau khi chụp evidence.

### TC-014 — Chưa đủ threshold

- Phân loại: Quartz Scheduled Job + Requires Test Database Setup.
- Setup: `last_login_at = now() - interval '29 days'`, chỉ có rule 30 ngày active.
- Kích hoạt: chờ job.
- Expected: không có issuance/voucher mới.
- Cleanup: restore `last_login_at`.

### TC-015 — `LastLoginAt = null` bị bỏ qua

- Phân loại: Quartz Scheduled Job + Requires Test Database Setup.
- Setup:

```sql
UPDATE "user" SET last_login_at = NULL
WHERE id = '<INACTIVE_USER_ID>'::uuid;
```

- Kích hoạt: chờ job.
- Expected: không có issuance Inactive.
- Cleanup: restore giá trị ban đầu.

### TC-016 — Nhiều threshold chọn mức lớn nhất, sau đó priority

- Phân loại: Direct Swagger + Quartz Scheduled Job + Requires Test Database Setup.
- Tiền điều kiện: tạo các rule threshold/priority: `7/100`, `30/1`, `30/5`, `60/100`; mỗi rule dùng promotion hợp lệ cho cùng tier.
- Setup: `last_login_at = now() - interval '45 days'`.
- Kích hoạt: chờ job.
- Expected: chỉ một issuance; rule được chọn có `thresholdDays=30`, `priority=5`; cycle bắt đầu `INACTIVE:30:`. Không chọn threshold 7 dù priority cao hơn; không chọn 60 vì chưa đủ ngày.
- Cleanup: deactivate cả bốn rule và restore login.

### TC-017 — Rerun không cấp lại hằng ngày

- Phân loại: Quartz Scheduled Job + idempotency.
- Tiền điều kiện: TC-013 đã cấp; `last_login_at` không thay đổi.
- Kích hoạt: chờ thêm hai lượt job.
- Expected: chỉ một voucher cho `INACTIVE:30:<same utc ticks>`.
- DB: unique tuple customer/trigger/cycle giữ count bằng 1.
- Cleanup: như TC-013.

### TC-018 — Được nhận threshold cao hơn về sau

- Phân loại: Quartz Scheduled Job + Requires Test Database Setup.
- Tiền điều kiện: đã có issuance threshold 7 cho cùng `last_login_at`; rule 30 cũng active.
- Setup: đưa `last_login_at` về ít nhất 31 ngày trước trong một test cycle mới, hoặc dùng cùng login cũ đã đủ 30 ngày.
- Kích hoạt: chờ job.
- Expected: có thể có thêm một issuance `INACTIVE:30:<ticks>`, nhưng không thêm lại `INACTIVE:7:<ticks>`. Đây là behavior chủ đích hiện tại.
- Cleanup: deactivate rules.

### TC-019 — Email/notification fail không rollback và retry đúng giới hạn

- Phân loại: Quartz Scheduled Job + Requires Test Database Setup.
- Setup: rule bật cả notification/email; cấu hình SMTP sai có kiểm soát trên local/test; `DeliveryMaxAttempts=3`, `DeliveryRetryDelayMinutes=1`; chuẩn bị customer đủ threshold.
- Kích hoạt: chờ Inactive job, sau đó chờ Retry job sau mỗi delay.
- Expected sau lần cấp: voucher xuất hiện ngay trong `GET /api/v1/vouchers`; issuance có `email_status='failed'`, `email_attempt_count=1`, `email_last_error='EMAIL_DELIVERY_FAILED'`. Notification có thể `sent` độc lập.
- Expected sau retry: attempt tăng tối đa đến 3, không tăng ở các lượt sau; vẫn chỉ một voucher/issuance. Khi sửa SMTP trước giới hạn, status chuyển `sent`, `email_sent_at` có giá trị, error null.
- Cleanup: khôi phục SMTP/config, deactivate rule; không xóa evidence trước khi ghi kết quả.

## 12. Welcome voucher

### TC-020 — Register không cấp ngay; Admin approval + Acquisition job mới cấp

- Phân loại: Indirect Swagger + Quartz Scheduled Job.
- Register bằng `POST /api/v1/auth/register`, `multipart/form-data`:

```text
email       = pv-welcome-<timestamp>@example.test
phone       = <số chưa dùng>
password    = <mật khẩu hợp lệ>
firstName   = Welcome
lastName    = Tester
cccd        = <CCCD chưa dùng>
dateOfBirth = 2000-01-01
faceImages  = <ít nhất 3 file>
```

- Expected register: `200`, `success=true`, `message="Create user successfully"`; account `Pending`, `isVerify=false`, `verified_at IS NULL`; chưa có voucher.
- Lấy `USER_ID` mới bằng Admin Swagger: `GET /api/v1/admin/users/pending-verification?searchTerm=pv-welcome-<timestamp>@example.test&pageIndex=1&pageSize=10`. Expected `200`, `success=true`, `data.items` có đúng email vừa đăng ký; lưu trường `id` cấp user.
- Admin duyệt: `PATCH /api/v1/admin/users/{USER_ID}/approval`, không body.
- Expected approval: `200`, `data="Verify user successfully"`; DB `status='active'`, `is_verify=true`, `verified_at` có giá trị.
- Kích hoạt: chờ Acquisition job.
- Expected voucher: `triggerType="Welcome"`, cycle bắt đầu `WELCOME:`; không có booking nào của Customer.
- Cleanup: deactivate Welcome rule; lưu user làm dữ liệu test hoặc dọn trên DB cô lập.

### TC-021 — Tài khoản cũ `VerifiedAt = null` không nhận Welcome hồi tố

- Phân loại: Quartz Scheduled Job + Requires Test Database Setup.
- Setup: customer `active`, `is_verify=true`, chưa từng booking, chưa issuance; `verified_at=NULL`.
- Kích hoạt: chờ Acquisition job.
- Expected: không có Welcome issuance. Nếu account đủ NoFirst threshold và NoFirst rule active, nó có thể nhận NoFirst; để cô lập phải tắt NoFirst rule.
- Cleanup: restore `verified_at` nếu cần.

### TC-022 — Welcome chỉ một lần, login/approval lặp không cấp thêm

- Phân loại: Indirect Swagger + Quartz idempotency.
- Tiền điều kiện: TC-020 đã có Welcome issuance.
- Thực hiện: login nhiều lần rồi chờ Acquisition job; gọi approval lại trên user đã active.
- Expected login: `200`; không tạo voucher trực tiếp. Approval lặp hiện có thể trả `404`/lỗi theo truy vấn service; không được tạo voucher.
- Expected cuối: chỉ một issuance có `trigger_type='welcome'` cho customer.
- Cleanup: deactivate rule.

### TC-023 — Welcome chỉ dùng được cho booking đầu tiên

- Phân loại: Direct Swagger booking + business validation.
- Tiền điều kiện: Customer chưa có bất kỳ booking nào; có Welcome voucher active; branch/vehicle/wallet hợp lệ.
- Request:

```text
POST /api/v1/bookings
```

```json
{
  "branchId": "<BRANCH_ID>",
  "vehicleId": "<VEHICLE_ID>",
  "voucherId": "<WELCOME_VOUCHER_ID>",
  "bookingDate": "<ngày tương lai hợp lệ>",
  "startTime": "<slot hợp lệ, ví dụ 2026-07-20T08:00:00+07:00>",
  "redemPoint": false
}
```

- Expected lần đầu: `200` raw DTO; `id` có giá trị, `status="Confirmed"`, `discountAmount > 0`, `finalPrice = basePrice - discountAmount` sau khi cap về base price.
- Negative: với Customer đã có bất kỳ booking nào, kể cả booking Cancelled, tạo booking dùng acquisition voucher phải trả `400` envelope với message `Welcome and no-first-booking vouchers can only be used for the customer's first booking.`
- DB: booking đầu tiên gắn đúng `voucher_id`. Voucher vẫn Active cho đến check-in.
- Cleanup: cancel booking qua `POST /api/v1/bookings/{id}/cancel` nếu còn trong điều kiện cancel; nếu không, dọn DB test theo dependency.

## 13. No-first-booking voucher

### TC-024 — Đủ tuổi tài khoản và chưa booking được cấp

- Phân loại: Quartz Scheduled Job + Requires Test Database Setup.
- Tiền điều kiện: NoFirst rule threshold 7 active; Welcome rule tắt hoặc customer không đủ Welcome; Customer active/verified; không có booking; không có acquisition voucher active.
- Setup:

```sql
UPDATE "user"
SET created_at = now() - interval '8 days',
    verified_at = NULL
WHERE id = '<ACQUISITION_USER_ID>'::uuid;
```

- Kích hoạt: chờ Acquisition job.
- Expected: một voucher `triggerType="NoFirstBooking"`, cycle bắt đầu `NO_FIRST_BOOKING:7:`.
- DB: không có Welcome issuance mới; voucher active.
- Cleanup: restore dates và deactivate rule.

### TC-025 — Chưa đủ tuổi tài khoản

- Phân loại: Quartz Scheduled Job + Requires Test Database Setup.
- Setup: `created_at = now() - interval '6 days'`, threshold 7; Welcome tắt.
- Kích hoạt: chờ job.
- Expected: không có NoFirst issuance.
- Cleanup: restore date.

### TC-026 — Đã có booking thì không được cấp NoFirst

- Phân loại: Direct Swagger setup + Quartz Scheduled Job.
- Tiền điều kiện: tạo một booking hợp lệ bằng `POST /api/v1/bookings` với `voucherId=null`; account đủ 7 ngày.
- Kích hoạt: chờ Acquisition job.
- Expected: không có NoFirst voucher. Logic hiện dùng `!Bookings.Any()`, nên booking Cancelled cũng làm account không còn đủ điều kiện.
- Cleanup: dọn booking trên DB test hoặc dùng customer riêng cho case sau.

### TC-027 — Welcome ưu tiên và không có hai acquisition voucher active cùng lúc

- Phân loại: Quartz Scheduled Job.
- Tiền điều kiện: Customer vừa được verify, account cũng đủ threshold NoFirst, chưa booking; cả hai rule active.
- Kích hoạt lần 1: chờ Acquisition job.
- Expected: chỉ Welcome được cấp; NoFirst chưa được cấp.
- Setup tiếp: đánh dấu Welcome voucher expired trên DB test:

```sql
UPDATE voucher
SET status = 'expired', expires_at = now() - interval '1 minute'
WHERE id = '<WELCOME_VOUCHER_ID>'::uuid;
```

- Kích hoạt lần 2: chờ job.
- Expected: một NoFirst voucher mới có thể được cấp; tại mọi thời điểm chỉ có một Welcome/NoFirst voucher active. Issuance history có thể chứa cả hai.
- Cleanup: deactivate hai rule.

## 14. Tier Upgrade voucher

### TC-028 — Nâng tier tại check-in và cấp voucher

- Phân loại: Indirect Swagger + Requires Test Database Setup.
- Tiền điều kiện:
  - Có tier hiện tại level N và tier kế tiếp level N+1.
  - Promotion của TierUpgrade là non-global, `tierIds=[NEXT_TIER_ID]`, active/còn hạn.
  - Rule TierUpgrade active.
  - Wallet đủ trả phần còn lại.
  - Customer hiện ở tier N.
- Setup DB test trước booking:

```sql
UPDATE customer_profile
SET total_washes = <NEXT_TIER_REQUIRED_WASHES> - 1,
    tier_id = '<CURRENT_TIER_ID>'::uuid
WHERE id = '<TIER_CUSTOMER_ID>'::uuid;
```

- Tạo booking qua `POST /api/v1/bookings` với `voucherId=null`, `redemPoint=false` và slot hợp lệ; lưu `BOOKING_ID`.
- Vì check-in chỉ cho phép từ `start_time` đến `start_time + CancelTimeMinutes`, chỉnh riêng booking test vào cửa sổ hiện tại nếu không thể đặt đúng giờ:

```sql
UPDATE booking
SET booking_date = (now() AT TIME ZONE 'Asia/Ho_Chi_Minh')::date,
    start_time = now() - interval '1 minute',
    end_time = now() + interval '14 minutes',
    status = 'confirmed'
WHERE id = '<BOOKING_ID>'::uuid;
```

- Kích hoạt thật bằng Admin Swagger: `POST /api/v1/admin/bookings/{BOOKING_ID}/check-in`, không body.
- Expected `200` envelope; `data.status="InProgress"`; `data.message="Check-in successful"`.
- Expected DB: `total_washes` tăng đúng 1; `tier_id=NEXT_TIER_ID`; một issuance `trigger_type='tier_upgrade'`, `cycle_key='TIER_UPGRADE:<NEXT_TIER_ID>'`, `trigger_reference='<BOOKING_ID>'`.
- Expected voucher list: một item `triggerType="TierUpgrade"`.
- Quan trọng: code hiện kích hoạt voucher tại **check-in**, không phải lúc booking chuyển `Completed`.
- Cleanup: deactivate rule; hoàn nguyên tier/washes trên customer test; dọn booking/transaction/voucher trên DB cô lập.

### TC-029 — Check-in không đổi tier thì không cấp voucher

- Phân loại: Indirect Swagger + Requires Test Database Setup.
- Setup: `total_washes` còn cách `nextTier.requiredWashes` ít nhất 2; tạo và check-in booking như TC-028.
- Expected: check-in `200`, washes tăng 1 nhưng tier không đổi; không có issuance TierUpgrade mới.
- Cleanup: hoàn nguyên washes và booking.

### TC-030 — Lặp xử lý cùng tier không tạo voucher thứ hai

- Phân loại: Idempotency + Requires Test Database Setup.
- Tiền điều kiện: TC-028 đã có issuance cycle `TIER_UPGRADE:<NEXT_TIER_ID>`.
- Không có Swagger endpoint gọi lại personalization. Xác minh bằng một booking/check-in khác không làm customer nâng lại cùng tier; hoặc chạy integration service test hiện có.
- Expected: count issuance cho customer/cycle vẫn bằng 1; unique constraint bảo vệ kể cả hai worker đồng thời.
- Cleanup: như TC-028.

## 15. Kiểm tra voucher sau khi cấp

### TC-031 — Customer lấy toàn bộ voucher của chính mình

- Phân loại: Direct Swagger.
- Request: `GET /api/v1/vouchers?pageSize=20&pageIndex=1`, UserPolicy token.
- Expected `200`, envelope message `Get customer vouchers successfully`; `data.items` gồm cả Reward voucher và Promotion voucher. Personalized item có `source="Promotion"`, `promotionName`, `triggerType`, `cycleKey`; Reward item có `source="Reward"`, `rewardName`, `triggerType=null`, `cycleKey=null`.
- Negative pagination: `pageSize=0` hoặc `pageIndex=0` → `400`, message `PageSize and PageIndex must be greater than 0.`
- DB: không thay đổi.
- Cleanup: không có.

### TC-032 — Legacy list vẫn hoạt động

- Phân loại: Direct Swagger regression.
- Request: `GET /Voucher/vouchers?userId=<USER_ID>&pageSize=20&pageIndex=1`.
- Expected `200` raw page result. Item personalized có `rewardName=null` nhưng endpoint legacy không trả promotion/trigger/cycle.
- DB: không thay đổi.
- Cleanup: không có.

### TC-033 — Validate personalized voucher hợp lệ

- Phân loại: Direct Swagger legacy.
- Request:

```text
POST /Voucher/vouchers/validate?userId=<USER_ID>
```

```json
{
  "code": "<VOUCHER_CODE>",
  "totalAmount": 200000
}
```

- Với voucher Percentage 15%, expected `200` raw DTO: `isValid=true`, `message="Voucher is valid"`, `discountAmount=30000`, `finalAmount=170000`, `rewardName=null`.
- Với FixedAmount 25000, expected `discountAmount=25000`, `finalAmount=175000`. Discount được cap không vượt totalAmount.
- DB: không thay đổi.
- Cleanup: không có.

### TC-034 — Booking dùng voucher và voucher chuyển Used khi check-in

- Phân loại: Direct/Indirect Swagger.
- Tiền điều kiện: personalized voucher active, chưa hết hạn, đúng owner. Để đo riêng voucher discount, sau khi cấp nên tắt tất cả global promotions; lưu ý tier promotion query legacy vẫn có thể cộng thêm promotion.
- Tạo booking như TC-023.
- Expected create `200`; booking gắn `voucherId`; voucher vẫn `Active` và `usedAt=null` ngay sau create.
- Check-in trong cửa sổ hợp lệ.
- Expected sau check-in: voucher `status="Used"` trong `GET /api/v1/vouchers`.
- DB hiện tại: `voucher.status='used'`, nhưng code không set `used_at`, vì vậy `used_at` vẫn có thể null. Ghi đây là GAP, không ghi fail test nếu chỉ xác nhận behavior hiện tại.
- Cleanup: dọn booking/transaction trên DB test sau khi lưu evidence.

### TC-035 — Không dùng lại voucher đã Used

- Phân loại: Direct Swagger negative.
- Tiền điều kiện: voucher đã chuyển Used sau TC-034.
- Thực hiện: tạo booking khác với cùng `voucherId`.
- Expected theo code hiện tại: `500` envelope, `message="An unexpected error occurred"`; Development có `errors.detail="Voucher is inactive"`. Đây là status hiện tại do service ném `Exception`, dù business expectation tốt hơn là `400`.
- DB: booking thứ hai không được tạo.
- Cleanup: không có.

### TC-036 — Ownership: Customer khác không dùng được voucher

- Phân loại: Direct Swagger negative.
- Tiền điều kiện: voucher thuộc Customer A; Authorize Customer B; vehicle/branch/wallet của B hợp lệ.
- Input: POST booking với `voucherId` của A.
- Expected theo code hiện tại: `500`, message public `An unexpected error occurred`; Development detail `Voucher not found`.
- DB: không tạo booking B với voucher A.
- Cleanup: không có.

### TC-037 — Voucher hết hạn

- Phân loại: Direct Swagger + Requires Test Database Setup.
- Setup DB test: `UPDATE voucher SET expires_at=now()-interval '1 minute' WHERE id='<VOUCHER_ID>'::uuid;`
- Validate legacy hoặc tạo booking dùng voucher.
- Expected theo code hiện tại: `500`; Development detail `Voucher expired`.
- DB: voucher có thể vẫn mang status `active`; report vẫn tính expired theo `expires_at <= now`.
- Cleanup: restore `expires_at` hoặc giữ để test report.

## 16. Báo cáo và delivery

### TC-038 — Admin report nhóm đúng campaign/rule/trigger

- Phân loại: Direct Swagger.
- Request:

```text
GET /api/v1/admin/personalized-vouchers/report?fromDate=2026-07-01&toDate=2026-07-31&triggerType=Birthday
```

- Expected `200`, envelope message `Get personalized voucher report`; mỗi item có `promotionId`, `promotionRuleId`, `campaignName`, `triggerType`, `issuedCount`, `activeCount`, `usedCount`, `expiredCount`, 3 notification counts, 3 email counts, `conversionRate`.
- Công thức: `conversionRate = round(usedCount * 100 / issuedCount, 2)`.
- Negative: `fromDate > toDate` → `400`, message `FromDate cannot be later than ToDate.`
- DB: không thay đổi.
- Cleanup: không có.

### TC-039 — In-app notification và metadata

- Phân loại: Direct Swagger read + Quartz delivery.
- Tiền điều kiện: rule `sendInAppNotification=true`, voucher đã được cấp.
- Request: `GET /api/v1/notifications?type=PersonalizedVoucher&isRead=false&page=1&pageSize=20` với Customer token.
- Expected `200` raw DTO; `data[]` có `type="PersonalizedVoucher"`, title/content đã render, metadata JSON chứa `VoucherId`, `PromotionId`, `RuleId`, `TriggerType`, `CycleKey`.
- DB: `notification.type='personalized_voucher'`; issuance `notification_status='sent'`, attempt >= 1, `notification_id` trùng notification.
- Security note: controller Notification hiện thiếu `[Authorize]`, nhưng service vẫn cần claim user. Cần tạo issue riêng để bổ sung annotation/policy nhất quán.
- Cleanup: có thể mark read bằng `PATCH /api/v1/notifications/status`, body `{"ids":["<NOTIFICATION_ID>"],"isRead":true,"markAll":false}`.

## 17. Regression tests

### TC-040 — Reward → Voucher vẫn trừ điểm và quantity

- Phân loại: Direct Swagger regression.
- Tiền điều kiện: Reward active, đúng tier, quantity > 0, Customer đủ điểm. Ghi điểm/quantity trước test qua `GET /api/v1/loyalty/me`, `GET /api/v1/rewards` hoặc DB.
- Request: `POST /Reward/redeem-reward?rewardId=<REWARD_ID>` với Customer token.
- Expected `200`, raw string `"Reward redeemed successfully"`.
- DB: Customer points giảm đúng `pointsRequired`; Reward quantity giảm 1; voucher mới có `reward_id=REWARD_ID`, `promotion_id IS NULL`; point transaction type `redeem` và points âm.
- Cleanup: dùng DB test cô lập; nếu hoàn nguyên thủ công phải hoàn nguyên đồng thời point transaction, points, quantity và voucher.

### TC-041 — Personalized voucher không trừ điểm/Reward quantity

- Phân loại: Quartz/Indirect regression.
- Tiền điều kiện: chụp `total_points` và một Reward quantity trước khi job cấp personalized voucher.
- Kích hoạt: chạy bất kỳ flow TC-007/013/020/024/028.
- Expected: points và Reward quantity không đổi; voucher `reward_id IS NULL`, `promotion_id` có giá trị.
- Cleanup: theo flow tương ứng.

### TC-042 — Booking không voucher

- Phân loại: Direct Swagger regression.
- Request POST booking với `voucherId=null`, `redemPoint=false`.
- Expected `200`; `status="Confirmed"`; chỉ promotion legacy đang active mới ảnh hưởng discount; không phát sinh personalized issuance.
- DB: booking `voucher_id IS NULL`; points chưa trừ tại create.
- Cleanup: cancel/dọn booking test.

### TC-043 — Global promotion hiện tại

- Phân loại: Direct Swagger regression.
- Tiền điều kiện: chỉ một global Promotion active, discount 10%; không voucher; không redeem point.
- Tạo booking.
- Expected current behavior: `discountAmount = basePrice * 10 / 100`, cap tối đa basePrice; `finalPrice=basePrice-discountAmount`.
- GAP cần quan sát: query global trong booking chỉ lọc `isActive`/`isGlobal`, không lọc StartDate, EndDate hoặc IsDeleted.
- Cleanup: tắt Promotion.

### TC-044 — Tier promotion hiện tại

- Phân loại: Direct Swagger regression.
- Tiền điều kiện: tier hiện tại có Promotion; không voucher/global promotion/redeem point.
- Tạo booking.
- Expected current behavior: booking lấy `FirstOrDefault` PromotionTier cho tier và cộng discount của promotion đó.
- GAP: query không lọc promotion active/date/deleted và không có thứ tự xác định nếu nhiều promotion tier cùng tồn tại.
- Cleanup: tắt/dọn promotion test.

### TC-045 — Không double-discount ngoài chủ đích

- Phân loại: Direct Swagger regression/GAP discovery.
- Tiền điều kiện: personalized voucher được sinh từ một Promotion đang đồng thời active trong cơ chế global/tier booking.
- Tạo booking dùng voucher.
- Expected theo code hiện tại: discount voucher **có thể bị cộng thêm lần nữa** bởi chính Promotion nền và cộng cùng các promotion khác; tổng cuối được cap bằng basePrice.
- Kết quả chấp nhận của test hiện trạng: ghi rõ `voucherDiscountAmount`, `promotionDiscountAmount`, `discountAmount` quan sát được. Nếu yêu cầu nghiệp vụ là một campaign không được tính hai lần, đánh dấu FAIL/GAP; không sửa expected thành chỉ một discount khi code chưa đảm bảo.
- Cleanup: tắt promotion/rule và dọn booking.

## 18. Date of birth và audit hỗ trợ Birthday

### TC-046 — Customer tự đặt DOB lần đầu

- Phân loại: Direct Swagger.
- Tiền điều kiện: Customer active/verified và `date_of_birth IS NULL`.
- Request: `PATCH /api/v1/me`, body `{"dateOfBirth":"2000-07-16"}`.
- Expected `200`; `success=true`; `data="Update customer profile successfully."`. `GET /api/v1/me` trả `data.profileData.dateOfBirth="2000-07-16"`.
- DB: DOB được set và `date_of_birth_set_at` có giá trị.
- Cleanup: không có API Customer xóa DOB; dùng customer test riêng.

### TC-047 — Customer không tự đổi DOB đã set

- Phân loại: Direct Swagger negative.
- Tiền điều kiện: DOB đã là `2000-07-16`.
- Gửi cùng ngày: expected `200`, `data="No profile changes detected."`, DB không đổi.
- Gửi ngày khác: expected `400`, message `Date of birth has already been set. Contact an administrator to request a correction.`
- DB: không tạo audit correction từ thao tác Customer.
- Cleanup: không có.

### TC-048 — Admin sửa DOB và tạo audit

- Phân loại: Direct Swagger.
- Request:

```text
PATCH /api/v1/admin/customers/{USER_ID}/date-of-birth
```

```json
{
  "dateOfBirth": "2000-07-17",
  "reason": "Correct DOB for TC-048"
}
```

- Expected `200`; message `Correct customer date of birth successfully`; `data="Customer date of birth corrected successfully."`.
- DB: một row `customer_date_of_birth_correction` lưu `customer_id`, `admin_user_id`, `previous_date_of_birth`, `new_date_of_birth`, reason đã trim và `created_at`; profile đổi DOB.
- Negative: reason rỗng → `400`, message `Correction reason is required.`; ngày tương lai → `400`, message `Date of birth cannot be in the future.`; cùng ngày → `200`, `data="No date of birth changes detected."` và không thêm audit.
- Cleanup: nếu cần hoàn nguyên hãy dùng chính Admin endpoint với reason `Rollback TC-048`; giữ audit history.

## 19. GAP: trigger chưa triển khai

| Trigger yêu cầu | Trạng thái | Evidence từ code hiện tại | Hướng test sau khi triển khai |
|---|---|---|---|
| Wash Milestone | Not Implemented | Không có enum/rule/job/issuance flow. Booking hiện có rule miễn phí khi `TotalWashes > 0 && TotalWashes % 5 == 0`, không tạo voucher | Cần chốt xung đột với miễn phí mỗi 5 lần rửa trước khi thêm trigger |
| Service Recovery | Not Implemented | Không có trigger; booking chưa cung cấp actor/reason code đủ tin cậy cho personalization | Cần cancellation actor + structured reason rồi mới có event/test |
| Expected Wash Due | Not Implemented | Không có enum, dữ liệu cadence hoặc job | Cần định nghĩa cách tính due date và cycle key |
| Branch Affinity | Not Implemented | Không có enum/rule targeting branch trên Voucher | Cần branch constraint và validation lúc booking |
| Off-peak | Not Implemented | Không có enum/time-window targeting trên Voucher | Cần time window/timezone và booking validation |
| Near-tier-upgrade | Not Implemented | Không có enum/audience rule | Cần định nghĩa khoảng cách tier, cycle và precedence với TierUpgrade |

Không gửi các chuỗi trigger trên vào `POST /api/v1/admin/personalized-promotion-rules`: model binding sẽ trả `400` vì enum không hợp lệ.

## 20. GAP và rủi ro khác phát hiện từ source

| ID | Mức độ | Mô tả |
|---|---|---|
| GAP-01 | Blocker môi trường | Chưa có migration cho model personalized/DOB/VerifiedAt. Swagger test chỉ chạy sau khi tester tự apply migration trên DB test. |
| GAP-02 | Thiết kế test | Không có endpoint chạy job thủ công; Birthday/Inactive/Acquisition phụ thuộc cron và DB setup. |
| GAP-03 | API consistency | Nhiều lỗi booking/voucher legacy ném `Exception` nên trả 500 thay vì 400/404. |
| GAP-04 | Voucher lifecycle | Check-in đổi `status=Used` nhưng không set `used_at`. |
| GAP-05 | Concurrency/use | Một voucher active có thể được gắn vào nhiều booking Confirmed trước khi booking đầu tiên check-in vì Booking không có unique VoucherId và voucher chưa bị reserve. |
| GAP-06 | Discount | Promotion nền của personalized voucher có thể được tính thêm như global/tier promotion, tạo double-discount. |
| GAP-07 | Promotion filtering | Booking global/tier promotion chưa lọc đầy đủ thời hạn/deleted; tier promotion còn không lọc active. |
| GAP-08 | Notification auth | `NotificationController` chưa có `[Authorize]` dù service yêu cầu identity claim. |
| GAP-09 | Acquisition semantics | `Bookings.Any()` tính cả booking Cancelled là đã từng booking. Cần xác nhận đúng nghiệp vụ. |
| GAP-10 | Manual leap-day | Swagger không có test clock; test 29/02 bị phụ thuộc ngày hệ thống hoặc integration test. |

## 21. Cleanup chuẩn

Ưu tiên DB disposable: snapshot evidence rồi reset toàn bộ DB test. Nếu cần giữ DB, thực hiện:

1. Tắt rule:

```text
PATCH /api/v1/admin/personalized-promotion-rules/{RULE_ID}/status
{"isActive":false}
```

2. Tắt Promotion:

```text
PATCH /Promotion/admin/update-promotion-status/{PROMOTION_ID}
{"isActive":false}
```

3. Khôi phục user fields đã chỉnh:

```sql
UPDATE "user"
SET status = 'active',
    last_login_at = <GIÁ_TRỊ_BAN_ĐẦU>,
    verified_at = <GIÁ_TRỊ_BAN_ĐẦU>,
    created_at = <GIÁ_TRỊ_BAN_ĐẦU>
WHERE id = '<USER_ID>'::uuid;
```

4. Với issuance chưa gắn booking, xóa có scope trong transaction, lưu ID trước khi xóa:

```sql
BEGIN;

CREATE TEMP TABLE cleanup_voucher_ids AS
SELECT voucher_id, notification_id
FROM personalized_voucher_issuance
WHERE customer_id = '<CUSTOMER_ID>'::uuid
  AND promotion_rule_id = '<RULE_ID>'::uuid;

DELETE FROM personalized_voucher_issuance
WHERE customer_id = '<CUSTOMER_ID>'::uuid
  AND promotion_rule_id = '<RULE_ID>'::uuid;

DELETE FROM notification
WHERE id IN (SELECT notification_id FROM cleanup_voucher_ids WHERE notification_id IS NOT NULL);

DELETE FROM voucher
WHERE id IN (SELECT voucher_id FROM cleanup_voucher_ids);

COMMIT;
```

Nếu voucher đã được gắn booking, không dùng script trên trước khi dọn các transaction/booking liên quan. Nên reset DB disposable để tránh vi phạm FK và tránh hoàn nguyên sai dữ liệu loyalty/wallet.

## 22. Execution log

| Test case | Thời gian | Tester | Environment/commit | Kết quả Pass/Fail/Blocked | Evidence (response/SQL/log) | Bug/GAP |
|---|---|---|---|---|---|---|
| TC-001 | | | | | | |
| TC-002 | | | | | | |
| TC-003 | | | | | | |
| TC-004 | | | | | | |
| TC-005 | | | | | | |
| TC-006 | | | | | | |
| TC-007 | | | | | | |
| TC-008 | | | | | | |
| TC-009 | | | | | | |
| TC-010 | | | | | | |
| TC-011 | | | | | | |
| TC-012 | | | | | | |
| TC-013 | | | | | | |
| TC-014 | | | | | | |
| TC-015 | | | | | | |
| TC-016 | | | | | | |
| TC-017 | | | | | | |
| TC-018 | | | | | | |
| TC-019 | | | | | | |
| TC-020 | | | | | | |
| TC-021 | | | | | | |
| TC-022 | | | | | | |
| TC-023 | | | | | | |
| TC-024 | | | | | | |
| TC-025 | | | | | | |
| TC-026 | | | | | | |
| TC-027 | | | | | | |
| TC-028 | | | | | | |
| TC-029 | | | | | | |
| TC-030 | | | | | | |
| TC-031 | | | | | | |
| TC-032 | | | | | | |
| TC-033 | | | | | | |
| TC-034 | | | | | | |
| TC-035 | | | | | | |
| TC-036 | | | | | | |
| TC-037 | | | | | | |
| TC-038 | | | | | | |
| TC-039 | | | | | | |
| TC-040 | | | | | | |
| TC-041 | | | | | | |
| TC-042 | | | | | | |
| TC-043 | | | | | | |
| TC-044 | | | | | | |
| TC-045 | | | | | | |
| TC-046 | | | | | | |
| TC-047 | | | | | | |
| TC-048 | | | | | | |

## 23. Điều kiện hoàn tất đợt test

- 48 test cases đã có trạng thái Pass/Fail/Blocked và evidence.
- Cả năm trigger đã triển khai được kiểm tra ít nhất một happy path, một negative path và idempotency phù hợp.
- Voucher cá nhân hóa không thay đổi points hoặc Reward quantity.
- Reward → Voucher legacy vẫn hoạt động.
- Voucher ownership và first-booking constraint được xác nhận.
- Delivery fail không rollback voucher và không retry quá `DeliveryMaxAttempts`.
- Report đếm đúng issuance/voucher/delivery.
- Sáu trigger chưa triển khai được ghi GAP, không bị báo nhầm là đã test.
- Mọi dữ liệu test đã được deactivate/cleanup hoặc DB disposable đã được reset bởi tester.
