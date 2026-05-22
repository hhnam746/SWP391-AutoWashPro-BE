# AutoWash Pro - Core API Contracts (MVP)

Tài liệu này là bản API contract cho frontend theo hướng client-first, bám theo scope MVP của AutoWash Pro.

Nguồn đồng bộ:

- `Document_AutoWash (1).docx`
- Database schema DBML trong tài liệu yêu cầu
- User stories US-01 đến US-47
- Business Rules BR-01 đến BR-11

## 1. Thay đổi quan trọng so với bản nháp cũ

1. Hệ thống **không có role Staff**.
2. Role chính thức theo DB enum chỉ gồm:
   - `customer`
   - `admin`
3. Các chức năng từng ghi là Staff trong bản cũ được chuyển về **Admin**:
   - Manage Points Config
   - Manage Promotions
   - Manage Rewards
   - View Booking Status
   - Track Booking Slots
4. API style dùng REST theo format giống file mẫu:
   - Base URL: `/api/v1`
   - Auth: JWT access token
   - Request chỉ chứa dữ liệu user nhập/chọn
   - Read response đủ dữ liệu cho UI
   - Write response gọn, không expose DB entity trực tiếp
5. DBML là nguồn ưu tiên cao nhất khi sinh API contract. Nếu một entity hoặc flow detail đã bị comment/xóa trong DBML thì xem như không thuộc scope API MVP.
6. Bảng `service` và `payment` đang bị comment trong DBML, nên contract không thiết kế API service package, pricing catalog, payment, deposit hoặc payment gateway.

## 2. Nguyên tắc thiết kế

### 2.1. Client-first

- Request chỉ chứa field người dùng thật sự nhập/chọn.
- Response chỉ trả field FE cần để render, filter, sort hoặc cập nhật state.
- Không trả `passwordHash`, metadata nội bộ, raw config phức tạp nếu UI không dùng.

### 2.2. Thin write, rich read

- Write APIs (`POST`, `PATCH`, `DELETE`): response gọn, thường gồm `id`, một vài field vừa cập nhật và `message`.
- Read APIs (`GET`): response đầy đủ hơn để render màn hình.

### 2.3. Không expose DB entity trực tiếp

- Backend phải map entity DB sang DTO.
- Các field như `createdAt`, `updatedAt` chỉ trả khi UI cần hiển thị timeline, audit hoặc filter.
- Không trả field nội bộ như `passwordHash`, full verification metadata, internal automation state nếu UI không cần.

### 2.4. Clock-based automation

- Hệ thống không tích hợp IoT/LPR thật trong MVP.
- Check-in/check-out và chuyển trạng thái booking được giả lập bằng thao tác API + cron/background job dựa theo đồng hồ hệ thống.
- Sau 15 phút từ lúc bắt đầu rửa, booking được tự động chuyển sang `completed`.

## 3. Convention chung

### 3.1. Base URL

```text
/api/v1
```

### 3.2. Auth

- Public: không cần token.
- Customer: JWT access token role `customer`.
- Admin: JWT access token role `admin`.
- System: background job nội bộ, không gọi trực tiếp từ FE.

### 3.3. Naming convention

- API path: kebab-case hoặc resource plural.
- JSON field: camelCase.
- DB field: snake_case.
- ID: UUID string.
- Time: ISO8601.
- Money: decimal.
- Point: integer.

### 3.4. Pagination

```json
{
  "data": [],
  "pagination": {
    "page": 1,
    "pageSize": 20,
    "totalCount": 0,
    "totalPages": 0
  }
}
```

### 3.5. Error format

```json
{
  "success": false,
  "error": "Mô tả lỗi ngắn gọn cho user",
  "details": {
    "field": "phone",
    "code": "PHONE_ALREADY_EXISTS"
  },
  "traceId": "guid"
}
```

### 3.6. Common status mapping

DB enum `account_status`:

```text
active | locked | inactive
```

DB enum `booking_status`:

```text
pending | confirmed | check_in | in_progress | completed | cancelled
```

DB enum `voucher_status`:

```text
active | used | expired
```

DB enum `point_transaction_type`:

```text
earn | redeem | reset
```

## 4. Business Rules áp dụng trong API

| Mã BR | Nội dung                                                | API ảnh hưởng                       |
| ----- | ------------------------------------------------------- | ----------------------------------- |
| BR-01 | 1 email/SĐT = 1 tài khoản                               | Auth register                       |
| BR-02 | Đăng ký cần tối thiểu 3 ảnh mặt + 2 biển số             | Auth register                       |
| BR-03 | Mỗi xe chỉ có 1 booking active tại 1 thời điểm          | Booking create                      |
| BR-04 | Mỗi slot = 15 phút, 1 xe/slot, từ 08:00–17:00           | Booking slots/create                |
| BR-05 | Điểm tích lũy cố định mỗi lần rửa, Admin cấu hình       | Admin points config, loyalty engine |
| BR-06 | Hạng tự động nâng khi đủ ngưỡng số lần rửa              | Loyalty engine, tiers               |
| BR-07 | Platinum ưu tiên đặt slot trước N ngày, Admin cấu hình  | Booking slots, tiers                |
| BR-08 | Hệ thống tự cancel booking nếu quá giờ                  | Booking automation                  |
| BR-09 | Checkout tự động sau 15 phút kể từ giờ hẹn              | Booking automation                  |
| BR-10 | Khách được hủy booking trước 30 phút hoặc theo cấu hình | Booking cancel                      |
| BR-11 | Sau 1 năm không cộng điểm thì reset điểm                | Loyalty reset job                   |

## 5. REST API tổng hợp

### Authentication & Profile

| Method | Endpoint                       | Mục đích                  |
| ------ | ------------------------------ | ------------------------- |
| POST   | `/api/v1/auth/register`        | Đăng ký customer          |
| POST   | `/api/v1/auth/login`           | Đăng nhập customer/admin  |
| POST   | `/api/v1/auth/forgot-password` | Gửi OTP quên mật khẩu     |
| POST   | `/api/v1/auth/reset-password`  | Đặt lại mật khẩu bằng OTP |
| POST   | `/api/v1/auth/logout`          | Logout phía FE            |
| GET    | `/api/v1/me`                   | Lấy profile hiện tại      |
| PATCH  | `/api/v1/me`                   | Cập nhật profile          |
| PATCH  | `/api/v1/me/password`          | Đổi mật khẩu              |

### Customer Vehicles

| Method | Endpoint                | Mục đích                  |
| ------ | ----------------------- | ------------------------- |
| GET    | `/api/v1/vehicles`      | Danh sách xe của customer |
| POST   | `/api/v1/vehicles`      | Thêm xe                   |
| GET    | `/api/v1/vehicles/{id}` | Chi tiết xe               |
| PATCH  | `/api/v1/vehicles/{id}` | Cập nhật xe               |
| DELETE | `/api/v1/vehicles/{id}` | Xóa mềm xe                |

### Branches & Public Read

| Method | Endpoint                       | Mục đích                        |
| ------ | ------------------------------ | ------------------------------- |
| GET    | `/api/v1/branches`             | Danh sách chi nhánh active      |
| GET    | `/api/v1/tiers`                | Xem các hạng thành viên         |
| GET    | `/api/v1/promotions/available` | Promotion khả dụng với customer |
| GET    | `/api/v1/rewards`              | Catalog phần thưởng active      |

### Booking

| Method | Endpoint                         | Mục đích                              |
| ------ | -------------------------------- | ------------------------------------- |
| GET    | `/api/v1/bookings`               | Customer xem booking hiện tại/lịch sử |
| POST   | `/api/v1/bookings`               | Tạo booking pending                   |
| GET    | `/api/v1/bookings/{id}`          | Chi tiết booking                      |
| POST   | `/api/v1/bookings/{id}/check-in` | Check-in khi đến rửa                  |
| POST   | `/api/v1/bookings/{id}/cancel`   | Customer hủy booking                  |
| GET    | `/api/v1/booking-slots`          | Xem slot trống/đã đặt                 |

### Loyalty, Rewards, Vouchers

| Method | Endpoint                             | Mục đích                       |
| ------ | ------------------------------------ | ------------------------------ |
| GET    | `/api/v1/loyalty/me`                 | Xem điểm, tier, quyền lợi      |
| GET    | `/api/v1/loyalty/point-transactions` | Lịch sử điểm                   |
| POST   | `/api/v1/rewards/{id}/redeem`        | Đổi điểm lấy reward            |
| GET    | `/api/v1/vouchers`                   | Danh sách voucher của customer |
| POST   | `/api/v1/vouchers/validate`          | Validate voucher khi booking   |

### Wallet

| Method | Endpoint                | Mục đích           |
| ------ | ----------------------- | ------------------ |
| GET    | `/api/v1/wallet`        | Xem số dư wallet   |
| POST   | `/api/v1/wallet/top-up` | Nạp ví giả lập MVP |

### Notifications

| Method | Endpoint                       | Mục đích            |
| ------ | ------------------------------ | ------------------- |
| GET    | `/api/v1/notifications`        | Danh sách thông báo |
| PATCH  | `/api/v1/notifications/status` | Đánh dấu đã đọc     |

### Admin - Users & Operations

| Method | Endpoint                               | Mục đích                          |
| ------ | -------------------------------------- | --------------------------------- |
| GET    | `/api/v1/admin/users`                  | Danh sách customer                |
| GET    | `/api/v1/admin/users/{id}`             | Chi tiết customer                 |
| PATCH  | `/api/v1/admin/users/{id}/status`      | Khóa/mở khóa account              |
| GET    | `/api/v1/admin/bookings`               | Xem booking toàn hệ thống         |
| GET    | `/api/v1/admin/booking-slots`          | Xem slot theo chi nhánh/ngày      |
| POST   | `/api/v1/admin/bookings/{id}/complete` | Hoàn tất booking thủ công khi cần |
| POST   | `/api/v1/admin/bookings/{id}/cancel`   | Admin hủy booking                 |

### Admin - Branches & Tiers

| Method | Endpoint                      | Mục đích              |
| ------ | ----------------------------- | --------------------- |
| GET    | `/api/v1/admin/branches`      | Danh sách chi nhánh   |
| POST   | `/api/v1/admin/branches`      | Tạo chi nhánh         |
| PATCH  | `/api/v1/admin/branches/{id}` | Cập nhật chi nhánh    |
| DELETE | `/api/v1/admin/branches/{id}` | Vô hiệu hóa chi nhánh |
| GET    | `/api/v1/admin/tiers`         | Danh sách tier        |
| POST   | `/api/v1/admin/tiers`         | Tạo tier              |
| PATCH  | `/api/v1/admin/tiers/{id}`    | Cập nhật tier         |
| DELETE | `/api/v1/admin/tiers/{id}`    | Xóa/vô hiệu hóa tier  |

### Admin - Loyalty Config, Promotions, Rewards

| Method | Endpoint                               | Mục đích                  |
| ------ | -------------------------------------- | ------------------------- |
| GET    | `/api/v1/admin/points-config`          | Xem cấu hình điểm         |
| PUT    | `/api/v1/admin/points-config`          | Cập nhật cấu hình điểm    |
| GET    | `/api/v1/admin/promotions`             | Danh sách promotion       |
| POST   | `/api/v1/admin/promotions`             | Tạo promotion             |
| PATCH  | `/api/v1/admin/promotions/{id}`        | Cập nhật promotion        |
| POST   | `/api/v1/admin/promotions/{id}/status` | Bật/tắt promotion         |
| DELETE | `/api/v1/admin/promotions/{id}`        | Xóa promotion chưa active |
| GET    | `/api/v1/admin/rewards`                | Danh sách reward          |
| POST   | `/api/v1/admin/rewards`                | Tạo reward                |
| PATCH  | `/api/v1/admin/rewards/{id}`           | Cập nhật reward           |
| DELETE | `/api/v1/admin/rewards/{id}`           | Xóa/vô hiệu hóa reward    |

### Admin - Reports

| Method | Endpoint                         | Mục đích            |
| ------ | -------------------------------- | ------------------- |
| GET    | `/api/v1/admin/dashboard`        | Tổng quan vận hành  |
| GET    | `/api/v1/admin/reports/revenue`  | Báo cáo doanh thu   |
| GET    | `/api/v1/admin/reports/branches` | Hiệu suất chi nhánh |
| GET    | `/api/v1/admin/reports/loyalty`  | Báo cáo loyalty     |

### AI Personalization

| Method | Endpoint                    | Mục đích                     |
| ------ | --------------------------- | ---------------------------- |
| POST   | `/api/v1/ai/offers/suggest` | Gợi ý ưu đãi cá nhân hóa MVP |
| GET    | `/api/v1/admin/ai-settings` | Xem cấu hình AI              |
| PATCH  | `/api/v1/admin/ai-settings` | Cập nhật cấu hình AI         |

---

# 6. API Contracts theo nhóm flow

## P0 - Authentication & Profile

### `POST /api/v1/auth/register`

- Auth: Public
- Mục đích: tạo tài khoản customer, kèm profile, ảnh mặt, xe ban đầu và wallet.

Request

```json
{
  "email": "customer@example.com",
  "phone": "0900000000",
  "password": "string",
  "firstName": "Nguyen",
  "lastName": "An",
  "cccd": "012345678901",
  "faceImages": [
    {
      "imageUrl": "https://storage/app/face-1.jpg"
    },
    {
      "imageUrl": "https://storage/app/face-2.jpg"
    },
    {
      "imageUrl": "https://storage/app/face-3.jpg"
    }
  ]
}
```

Response `201 Created`

```json
{
  "accessToken": "string",
  "tokenType": "Bearer",
  "expiresIn": 3600,
  "user": {
    "id": "guid",
    "role": "customer",
    "status": "active",
    "email": "customer@example.com",
    "phone": "0900000000",
    "profile": {
      "id": "guid",
      "firstName": "Nguyen",
      "lastName": "An",
      "tier": {
        "id": "guid",
        "name": "Silver",
        "level": 1
      },
      "totalPoints": 0,
      "totalWashes": 0
    },
    "vehicleCount": 2
  }
}
```

Notes

- `409 Conflict` nếu email hoặc phone đã tồn tại.
- `400 Bad Request` nếu `faceImages.length < 3`.
- `400 Bad Request` nếu `vehicles.length < 2`.
- Backend tạo:
  - `user`
  - `customer_profile`
  - `user_face_image`
  - `vehicle`
  - `wallet`
- Backend tự gán tier mặc định thấp nhất, thường là Silver.
- `role` luôn là `customer`, FE không được truyền role khi register.

### `POST /api/v1/auth/login`

- Auth: Public
- Mục đích: đăng nhập bằng email hoặc SĐT.

Request

```json
{
  "identifier": "customer@example.com",
  "password": "string"
}
```

Response `200 OK`

```json
{
  "accessToken": "string",
  "tokenType": "Bearer",
  "expiresIn": 3600,
  "user": {
    "id": "guid",
    "role": "customer",
    "status": "active",
    "email": "customer@example.com",
    "phone": "0900000000",
    "firstName": "Nguyen",
    "lastName": "An"
  }
}
```

Notes

- `identifier` có thể là email hoặc phone.
- `401 Unauthorized` nếu sai thông tin đăng nhập.
- `403 Forbidden` nếu account `locked` hoặc `inactive`.
- Backend cập nhật `last_login_at`.

### `POST /api/v1/auth/forgot-password`

- Auth: Public

Request

```json
{
  "email": "customer@example.com"
}
```

Response `200 OK`

```json
{
  "message": "OTP has been sent if the email exists"
}
```

Notes

- Không tiết lộ email có tồn tại hay không.
- OTP storage có thể dùng cache/table riêng ngoài scope DBML hiện tại.

### `POST /api/v1/auth/reset-password`

- Auth: Public

Request

```json
{
  "email": "customer@example.com",
  "otp": "123456",
  "newPassword": "string"
}
```

Response `200 OK`

```json
{
  "message": "Password reset successfully"
}
```

### `POST /api/v1/auth/logout`

- Auth: Customer/Admin

Request

```json
{}
```

Response `200 OK`

```json
{
  "message": "Logged out successfully"
}
```

Notes

- MVP có thể để FE xóa token local.
- Backend có thể ghi log nếu có audit/log table riêng.

### `GET /api/v1/me`

- Auth: Customer/Admin

Response `200 OK`

```json
{
  "id": "guid",
  "role": "customer",
  "status": "active",
  "email": "customer@example.com",
  "phone": "0900000000",
  "profile": {
    "id": "guid",
    "firstName": "Nguyen",
    "lastName": "An",
    "cccd": "012345678901",
    "tier": {
      "id": "guid",
      "name": "Silver",
      "level": 1
    },
    "totalPoints": 120,
    "totalWashes": 3,
    "lastPointActivityAt": "ISO8601"
  }
}
```

### `PATCH /api/v1/me`

- Auth: Customer

Request

```json
{
  "firstName": "Nguyen",
  "lastName": "Anh",
  "cccd": "012345678901"
}
```

Response `200 OK`

```json
{
  "profileId": "guid",
  "firstName": "Nguyen",
  "lastName": "Anh",
  "cccd": "012345678901"
}
```

Notes

- Không cập nhật ảnh mặt trong endpoint này.
- `cccd` unique nếu có truyền.

### `PATCH /api/v1/me/password`

- Auth: Customer/Admin

Request

```json
{
  "oldPassword": "string",
  "newPassword": "string"
}
```

Response `200 OK`

```json
{
  "message": "Password changed successfully"
}
```

---

## P1 - Customer Vehicles

### `GET /api/v1/vehicles`

- Auth: Customer

Query Params

- `status=active|inactive`
- `page=1`
- `pageSize=20`

Response `200 OK`

```json
{
  "data": [
    {
      "id": "guid",
      "licensePlate": "51A-12345",
      "brand": "Toyota",
      "model": "Vios",
      "color": "White",
      "isActive": true,
      "hasActiveBooking": false
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

### `POST /api/v1/vehicles`

- Auth: Customer

Request

```json
{
  "licensePlate": "51A-99999",
  "brand": "Mazda",
  "model": "CX-5",
  "color": "Red",
  "licensePlateImageUrl": "https://storage/app/plate.jpg"
}
```

Response `201 Created`

```json
{
  "id": "guid",
  "licensePlate": "51A-99999",
  "brand": "Mazda",
  "model": "CX-5",
  "color": "Red",
  "isActive": true
}
```

Notes

- `409 Conflict` nếu license plate đã tồn tại.
- `licensePlateImageUrl` phục vụ verify nhận diện biển số nhưng DB hiện chưa có field riêng; backend có thể lưu metadata ngoài scope hoặc bỏ qua trong MVP nếu chưa cần.

### `GET /api/v1/vehicles/{id}`

- Auth: Customer

Response `200 OK`

```json
{
  "id": "guid",
  "licensePlate": "51A-12345",
  "brand": "Toyota",
  "model": "Vios",
  "color": "White",
  "isActive": true,
  "activeBooking": null
}
```

### `PATCH /api/v1/vehicles/{id}`

- Auth: Customer

Request

```json
{
  "brand": "Toyota",
  "model": "Vios G",
  "color": "White"
}
```

Response `200 OK`

```json
{
  "id": "guid",
  "brand": "Toyota",
  "model": "Vios G",
  "color": "White"
}
```

Notes

- Không cho sửa `licensePlate` nếu đã có booking lịch sử, trừ khi team xác nhận rule khác.

### `DELETE /api/v1/vehicles/{id}`

- Auth: Customer

Response `200 OK`

```json
{
  "message": "Vehicle removed successfully"
}
```

Notes

- Soft delete: set `is_active = false`, `deleted_at = now()`.
- `409 Conflict` nếu xe đang có booking active: `pending`, `confirmed`, `check_in`, `in_progress`.

---

## P2 - Branches & Tiers Public Read

### `GET /api/v1/branches`

- Auth: Customer

Query Params

- `keyword=quan 1`
- `isActive=true`

Response `200 OK`

```json
{
  "data": [
    {
      "id": "guid",
      "name": "AutoWash Quận 1",
      "address": "123 Nguyen Hue, Quan 1",
      "isActive": true
    }
  ]
}
```

### `GET /api/v1/tiers`

- Auth: Customer

Response `200 OK`

```json
{
  "data": [
    {
      "id": "guid",
      "name": "Silver",
      "level": 1,
      "requiredWashes": 0,
      "priorityBookingDays": 0,
      "description": "Hạng mặc định"
    },
    {
      "id": "guid",
      "name": "Gold",
      "level": 2,
      "requiredWashes": 10,
      "priorityBookingDays": 1,
      "description": "Ưu đãi tốt hơn"
    },
    {
      "id": "guid",
      "name": "Platinum",
      "level": 3,
      "requiredWashes": 20,
      "priorityBookingDays": 3,
      "description": "Ưu tiên đặt lịch"
    }
  ]
}
```

---

## P3 - Booking Customer Flow

### `GET /api/v1/booking-slots`

- Auth: Customer
- Mục đích: lấy slot trống/đã đặt theo chi nhánh/ngày.

Query Params

- `branchId=guid`
- `date=2026-05-20`
- `vehicleId=guid` optional, để backend áp dụng priority booking theo tier nếu cần.

Response `200 OK`

```json
{
  "branchId": "guid",
  "date": "2026-05-20",
  "slotDurationMinutes": 15,
  "workingHours": {
    "start": "08:00",
    "end": "17:00"
  },
  "data": [
    {
      "startTime": "2026-05-20T08:00:00+07:00",
      "endTime": "2026-05-20T08:15:00+07:00",
      "status": "available",
      "isPriorityOnly": false
    },
    {
      "startTime": "2026-05-20T08:15:00+07:00",
      "endTime": "2026-05-20T08:30:00+07:00",
      "status": "booked",
      "isPriorityOnly": false
    }
  ]
}
```

Notes

- Slot cố định 15 phút.
- Mỗi chi nhánh có 1 làn nên 1 slot chỉ nhận 1 xe.
- Slot ngoài 08:00–17:00 không được trả hoặc trả `unavailable`.

### `POST /api/v1/bookings`

- Auth: Customer
- Mục đích: tạo booking và giữ slot theo DB hiện tại, không chọn gói dịch vụ.

Request

```json
{
  "branchId": "guid",
  "vehicleId": "guid",
  "bookingDate": "2026-05-20",
  "startTime": "2026-05-20T09:00:00+07:00",
  "voucherCode": "WELCOME10"
}
```

Response `201 Created`

```json
{
  "id": "guid",
  "status": "confirmed",
  "branch": {
    "id": "guid",
    "name": "AutoWash Quận 1"
  },
  "vehicle": {
    "id": "guid",
    "licensePlate": "51A-12345"
  },
  "bookingDate": "2026-05-20",
  "startTime": "2026-05-20T09:00:00+07:00",
  "endTime": "2026-05-20T09:15:00+07:00",
  "basePrice": 80000,
  "discountAmount": 8000,
  "finalPrice": 72000
}
```

Notes

- `409 Conflict` nếu xe đã có booking active.
- `409 Conflict` nếu slot đã bị đặt.
- `400 Bad Request` nếu slot không nằm trong 08:00–17:00.
- Backend tự tính `endTime = startTime + 15 phút`.
- Backend tính `basePrice`, `discountAmount`, `finalPrice` theo rule nội bộ hiện tại và voucher nếu có.
- Vì DBML không có bảng `service` nên request không nhận `serviceId` hoặc `serviceCode`.
- Vì DBML không có bảng `payment` nên booking tạo thành công được xem là đã giữ slot và trả trạng thái `confirmed`.

### `GET /api/v1/bookings`

- Auth: Customer

Query Params

- `status=pending|confirmed|check_in|in_progress|completed|cancelled`
- `fromDate=2026-05-01`
- `toDate=2026-05-31`
- `page=1`
- `pageSize=20`

Response `200 OK`

```json
{
  "data": [
    {
      "id": "guid",
      "status": "confirmed",
      "bookingDate": "2026-05-20",
      "startTime": "2026-05-20T09:00:00+07:00",
      "endTime": "2026-05-20T09:15:00+07:00",
      "branch": {
        "id": "guid",
        "name": "AutoWash Quận 1",
        "address": "123 Nguyen Hue"
      },
      "vehicle": {
        "id": "guid",
        "licensePlate": "51A-12345"
      },
      "finalPrice": 72000
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

### `GET /api/v1/bookings/{id}`

- Auth: Customer

Response `200 OK`

```json
{
  "id": "guid",
  "status": "confirmed",
  "bookingDate": "2026-05-20",
  "startTime": "2026-05-20T09:00:00+07:00",
  "endTime": "2026-05-20T09:15:00+07:00",
  "branch": {
    "id": "guid",
    "name": "AutoWash Quận 1",
    "address": "123 Nguyen Hue"
  },
  "vehicle": {
    "id": "guid",
    "licensePlate": "51A-12345",
    "brand": "Toyota",
    "model": "Vios"
  },
  "voucher": {
    "id": "guid",
    "code": "WELCOME10",
    "discountAmount": 8000
  },
  "basePrice": 80000,
  "discountAmount": 8000,
  "finalPrice": 72000,
  "cancelledAt": null,
  "completedAt": null
}
```

### `POST /api/v1/bookings/{id}/check-in`

- Auth: Customer/Admin
- Mục đích: giả lập customer đến rửa xe và bắt đầu quy trình.

Request

```json
{
  "confirm": true
}
```

Response `200 OK`

```json
{
  "id": "guid",
  "status": "in_progress",
  "checkedInAt": "ISO8601",
  "estimatedCompletedAt": "ISO8601",
  "message": "Check-in successful"
}
```

Notes

- Chỉ check-in booking `confirmed`.
- DB enum có `check_in` và `in_progress`; contract trả `in_progress` sau khi check-in thành công để UI đơn giản hơn. Nếu team muốn hiện bước trung gian thì có thể trả `check_in`.

### `POST /api/v1/bookings/{id}/cancel`

- Auth: Customer

Request

```json
{
  "reason": "Tôi bận đột xuất"
}
```

Response `200 OK`

```json
{
  "id": "guid",
  "status": "cancelled",
  "cancelledAt": "ISO8601",
  "message": "Booking cancelled successfully"
}
```

Notes

- Customer chỉ được hủy trước giờ hẹn ít nhất 30 phút hoặc theo config.
- Không cho hủy booking `in_progress`, `completed`, `cancelled`.

---

## P4 - Loyalty, Rewards, Vouchers

### `GET /api/v1/loyalty/me`

- Auth: Customer

Response `200 OK`

```json
{
  "customerId": "guid",
  "totalPoints": 350,
  "totalWashes": 7,
  "lastPointActivityAt": "ISO8601",
  "currentTier": {
    "id": "guid",
    "name": "Silver",
    "level": 1,
    "description": "Hạng mặc định"
  },
  "nextTier": {
    "id": "guid",
    "name": "Gold",
    "level": 2,
    "requiredWashes": 10,
    "remainingWashes": 3
  },
  "benefits": ["Ưu đãi cơ bản", "Nhận voucher từ reward catalog"]
}
```

### `GET /api/v1/loyalty/point-transactions`

- Auth: Customer

Query Params

- `type=earn|redeem|reset`
- `page=1`
- `pageSize=20`

Response `200 OK`

```json
{
  "data": [
    {
      "id": "guid",
      "type": "earn",
      "points": 50,
      "description": "Cộng điểm sau khi hoàn thành booking",
      "bookingId": "guid",
      "createdAt": "ISO8601"
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

### `GET /api/v1/rewards`

- Auth: Customer

Response `200 OK`

```json
{
  "data": [
    {
      "id": "guid",
      "name": "Free Basic Wash",
      "rewardType": "free_wash",
      "pointsRequired": 500,
      "quantityAvailable": 10,
      "validDays": 30,
      "description": "Đổi 1 lượt rửa xe cơ bản miễn phí",
      "isRedeemable": false,
      "allowedTiers": [
        {
          "id": "guid",
          "name": "Gold"
        }
      ]
    }
  ]
}
```

### `POST /api/v1/rewards/{id}/redeem`

- Auth: Customer

Request

```json
{
  "confirm": true
}
```

Response `200 OK`

```json
{
  "rewardId": "guid",
  "voucher": {
    "id": "guid",
    "code": "RW-ABCD1234",
    "status": "active",
    "discountType": "fixed_amount",
    "discountValue": 80000,
    "expiresAt": "ISO8601"
  },
  "remainingPoints": 150,
  "message": "Reward redeemed successfully"
}
```

Notes

- Backend kiểm tra:
  - Customer đủ điểm
  - Reward active
  - Reward còn quantity nếu giới hạn
  - Tier của customer được phép đổi reward
- Backend tạo:
  - `point_transaction` type `redeem`
  - `voucher`
  - `notification` type `reward_redeemed`

### `GET /api/v1/vouchers`

- Auth: Customer

Query Params

- `status=active|used|expired`

Response `200 OK`

```json
{
  "data": [
    {
      "id": "guid",
      "code": "WELCOME10",
      "status": "active",
      "discountType": "percentage",
      "discountValue": 10,
      "expiresAt": "ISO8601",
      "usedAt": null,
      "source": "promotion"
    }
  ]
}
```

### `POST /api/v1/vouchers/validate`

- Auth: Customer

Request

```json
{
  "code": "WELCOME10",
  "bookingDate": "2026-05-20"
}
```

Response `200 OK`

```json
{
  "isValid": true,
  "voucherId": "guid",
  "discountType": "percentage",
  "discountValue": 10,
  "estimatedDiscountAmount": 8000,
  "message": "Voucher can be applied"
}
```

Notes

- API này không đánh dấu voucher used.
- Voucher chỉ chuyển `used` khi booking confirm/thanh toán thành công.

---

## P5 - Wallet MVP

### `GET /api/v1/wallet`

- Auth: Customer

Response `200 OK`

```json
{
  "id": "guid",
  "balance": 500000,
  "currency": "VND"
}
```

### `POST /api/v1/wallet/top-up`

- Auth: Customer
- Mục đích: nạp ví giả lập cho MVP/demo.

Request

```json
{
  "amount": 500000
}
```

Response `200 OK`

```json
{
  "walletId": "guid",
  "balance": 1000000,
  "message": "Wallet topped up successfully"
}
```

Notes

- Chỉ dùng cho MVP/demo nếu chưa tích hợp payment thật.
- Production nên thay bằng payment gateway.

---

## P6 - Notifications

### `GET /api/v1/notifications`

- Auth: Customer/Admin

Query Params

- `type=booking_created|booking_reminder|booking_cancelled|booking_completed|tier_upgraded|reward_redeemed|system_alert`
- `isRead=true|false`
- `page=1`
- `pageSize=20`

Response `200 OK`

```json
{
  "data": [
    {
      "id": "guid",
      "type": "booking_created",
      "title": "Đặt lịch thành công",
      "content": "Đặt lịch thành công, slot 09:00 tại AutoWash Quận 1",
      "isRead": false,
      "metadata": {
        "bookingId": "guid"
      },
      "createdAt": "ISO8601"
    }
  ],
  "unreadCount": 1,
  "pagination": {
    "page": 1,
    "pageSize": 20,
    "totalCount": 1,
    "totalPages": 1
  }
}
```

### `PATCH /api/v1/notifications/status`

- Auth: Customer/Admin

Request

```json
{
  "ids": ["guid"],
  "isRead": true,
  "markAll": false
}
```

Response `200 OK`

```json
{
  "updatedCount": 1,
  "unreadCount": 0
}
```

---

# 7. Admin API Contracts

## P7 - Admin User Management

### `GET /api/v1/admin/users`

- Auth: Admin

Query Params

- `page=1`
- `pageSize=20`
- `keyword=nguyen`
- `status=active|locked|inactive`
- `tierId=guid`
- `sortBy=lastLoginAt|totalPoints|totalWashes`
- `sortDir=desc`

Response `200 OK`

```json
{
  "data": [
    {
      "id": "guid",
      "email": "customer@example.com",
      "phone": "0900000000",
      "status": "active",
      "lastLoginAt": "ISO8601",
      "profile": {
        "id": "guid",
        "firstName": "Nguyen",
        "lastName": "An",
        "tierName": "Silver",
        "totalPoints": 350,
        "totalWashes": 7
      },
      "vehicleCount": 2,
      "activeBookingCount": 1
    }
  ],
  "pagination": {
    "page": 1,
    "pageSize": 20,
    "totalCount": 100,
    "totalPages": 5
  }
}
```

### `GET /api/v1/admin/users/{id}`

- Auth: Admin

Response `200 OK`

```json
{
  "id": "guid",
  "email": "customer@example.com",
  "phone": "0900000000",
  "role": "customer",
  "status": "active",
  "lastLoginAt": "ISO8601",
  "profile": {
    "id": "guid",
    "firstName": "Nguyen",
    "lastName": "An",
    "cccd": "012345678901",
    "tier": {
      "id": "guid",
      "name": "Silver",
      "level": 1
    },
    "totalPoints": 350,
    "totalWashes": 7
  },
  "wallet": {
    "balance": 500000
  },
  "vehicles": [
    {
      "id": "guid",
      "licensePlate": "51A-12345",
      "isActive": true
    }
  ]
}
```

### `PATCH /api/v1/admin/users/{id}/status`

- Auth: Admin

Request

```json
{
  "status": "locked",
  "reason": "Vi phạm chính sách sử dụng"
}
```

Response `200 OK`

```json
{
  "id": "guid",
  "status": "locked",
  "message": "User status updated"
}
```

Notes

- `status` chỉ nhận `active | locked | inactive`.
- Không cho admin khóa chính mình nếu hệ thống chỉ còn một admin active.
- Khi account `locked`, user không được login hoặc gọi protected customer API.

---

## P8 - Admin Booking Operations

### `GET /api/v1/admin/bookings`

- Auth: Admin

Query Params

- `branchId=guid`
- `date=2026-05-20`
- `status=pending|confirmed|check_in|in_progress|completed|cancelled`
- `keyword=51A-12345`
- `page=1`
- `pageSize=20`

Response `200 OK`

```json
{
  "data": [
    {
      "id": "guid",
      "status": "confirmed",
      "bookingDate": "2026-05-20",
      "startTime": "2026-05-20T09:00:00+07:00",
      "endTime": "2026-05-20T09:15:00+07:00",
      "customer": {
        "id": "guid",
        "fullName": "Nguyen An",
        "phone": "0900000000",
        "tierName": "Silver"
      },
      "vehicle": {
        "id": "guid",
        "licensePlate": "51A-12345"
      },
      "branch": {
        "id": "guid",
        "name": "AutoWash Quận 1"
      },
      "finalPrice": 72000
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

### `GET /api/v1/admin/booking-slots`

- Auth: Admin

Query Params

- `branchId=guid`
- `date=2026-05-20`

Response `200 OK`

```json
{
  "branchId": "guid",
  "date": "2026-05-20",
  "slotDurationMinutes": 15,
  "data": [
    {
      "startTime": "2026-05-20T09:00:00+07:00",
      "endTime": "2026-05-20T09:15:00+07:00",
      "status": "booked",
      "booking": {
        "id": "guid",
        "status": "confirmed",
        "licensePlate": "51A-12345",
        "customerName": "Nguyen An"
      }
    }
  ]
}
```

### `POST /api/v1/admin/bookings/{id}/complete`

- Auth: Admin
- Mục đích: cho phép admin hoàn tất thủ công nếu automation lỗi hoặc cần demo.

Request

```json
{
  "note": "Manual complete by admin"
}
```

Response `200 OK`

```json
{
  "id": "guid",
  "status": "completed",
  "completedAt": "ISO8601",
  "pointsEarned": 50,
  "message": "Booking completed and loyalty points applied"
}
```

Notes

- Chỉ áp dụng cho booking `in_progress` hoặc `check_in`.
- Khi complete, backend trigger Loyalty Engine:
  - Cộng điểm
  - Tăng `total_washes`
  - Check tier upgrade
  - Gửi notification

### `POST /api/v1/admin/bookings/{id}/cancel`

- Auth: Admin

Request

```json
{
  "reason": "Customer no-show"
}
```

Response `200 OK`

```json
{
  "id": "guid",
  "status": "cancelled",
  "cancelledAt": "ISO8601",
  "message": "Booking cancelled"
}
```

---

## P9 - Admin Branches

### `GET /api/v1/admin/branches`

- Auth: Admin

Query Params

- `isActive=true|false`
- `keyword=quan 1`

Response `200 OK`

```json
{
  "data": [
    {
      "id": "guid",
      "name": "AutoWash Quận 1",
      "address": "123 Nguyen Hue",
      "isActive": true
    }
  ]
}
```

### `POST /api/v1/admin/branches`

- Auth: Admin

Request

```json
{
  "name": "AutoWash Quận 1",
  "address": "123 Nguyen Hue, Quan 1, HCM"
}
```

Response `201 Created`

```json
{
  "id": "guid",
  "name": "AutoWash Quận 1",
  "address": "123 Nguyen Hue, Quan 1, HCM",
  "isActive": true
}
```

### `PATCH /api/v1/admin/branches/{id}`

- Auth: Admin

Request

```json
{
  "name": "AutoWash Quận 1",
  "address": "456 Le Loi, Quan 1, HCM",
  "isActive": true
}
```

Response `200 OK`

```json
{
  "id": "guid",
  "name": "AutoWash Quận 1",
  "address": "456 Le Loi, Quan 1, HCM",
  "isActive": true
}
```

### `DELETE /api/v1/admin/branches/{id}`

- Auth: Admin

Response `200 OK`

```json
{
  "message": "Branch deactivated successfully"
}
```

Notes

- Soft deactivate: `is_active = false`.
- Không hard delete nếu có booking lịch sử.

---

## P10 - Admin Points Config

### `GET /api/v1/admin/points-config`

- Auth: Admin

Response `200 OK`

```json
{
  "pointsPerCompletedWash": 50,
  "cancelBeforeMinutes": 30,
  "slotDurationMinutes": 15,
  "workingHours": {
    "start": "08:00",
    "end": "17:00"
  },
  "pointResetAfterDays": 365
}
```

### `PUT /api/v1/admin/points-config`

- Auth: Admin

Request

```json
{
  "pointsPerCompletedWash": 50,
  "cancelBeforeMinutes": 30,
  "slotDurationMinutes": 15,
  "workingHours": {
    "start": "08:00",
    "end": "17:00"
  },
  "pointResetAfterDays": 365
}
```

Response `200 OK`

```json
{
  "message": "Points and booking config updated successfully"
}
```

Notes

- Lưu vào `system_config`.
- Config này ảnh hưởng Booking Automation và Loyalty Engine.

---

## P11 - Admin Membership Tiers

### `GET /api/v1/admin/tiers`

- Auth: Admin

Response `200 OK`

```json
{
  "data": [
    {
      "id": "guid",
      "name": "Silver",
      "level": 1,
      "requiredWashes": 0,
      "priorityBookingDays": 0,
      "description": "Hạng mặc định"
    }
  ]
}
```

### `POST /api/v1/admin/tiers`

- Auth: Admin

Request

```json
{
  "name": "Gold",
  "level": 2,
  "requiredWashes": 10,
  "priorityBookingDays": 1,
  "description": "Hạng khách hàng thân thiết"
}
```

Response `201 Created`

```json
{
  "id": "guid",
  "name": "Gold",
  "level": 2,
  "requiredWashes": 10,
  "priorityBookingDays": 1
}
```

### `PATCH /api/v1/admin/tiers/{id}`

- Auth: Admin

Request

```json
{
  "name": "Gold",
  "requiredWashes": 12,
  "priorityBookingDays": 1,
  "description": "Hạng khách hàng thân thiết"
}
```

Response `200 OK`

```json
{
  "id": "guid",
  "name": "Gold",
  "requiredWashes": 12,
  "priorityBookingDays": 1
}
```

### `DELETE /api/v1/admin/tiers/{id}`

- Auth: Admin

Response `200 OK`

```json
{
  "message": "Tier deleted successfully"
}
```

Notes

- Không được xóa tier đang có customer sử dụng.
- Không được xóa tier mặc định thấp nhất nếu hệ thống cần fallback.
- `requiredWashes` là rule chính để auto upgrade theo DB hiện tại.

---

## P12 - Admin Promotions

### `GET /api/v1/admin/promotions`

- Auth: Admin

Query Params

- `isActive=true|false`
- `tierId=guid`
- `page=1`
- `pageSize=20`

Response `200 OK`

```json
{
  "data": [
    {
      "id": "guid",
      "name": "Khuyến mãi hè",
      "description": "Giảm 10% cho khách Gold",
      "discountType": "percentage",
      "discountValue": 10,
      "startDate": "ISO8601",
      "endDate": "ISO8601",
      "isGlobal": false,
      "isActive": true,
      "tiers": [
        {
          "id": "guid",
          "name": "Gold"
        }
      ]
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

### `POST /api/v1/admin/promotions`

- Auth: Admin

Request

```json
{
  "name": "Khuyến mãi hè",
  "description": "Giảm 10% cho khách Gold",
  "discountType": "percentage",
  "discountValue": 10,
  "startDate": "2026-06-01T00:00:00+07:00",
  "endDate": "2026-06-30T23:59:59+07:00",
  "isGlobal": false,
  "tierIds": ["guid"]
}
```

Response `201 Created`

```json
{
  "id": "guid",
  "name": "Khuyến mãi hè",
  "isActive": true
}
```

Notes

- Nếu `isGlobal = true`, promotion áp dụng toàn hệ thống và có thể bỏ `tierIds`.
- Nếu `isGlobal = false`, phải có ít nhất 1 tier trong `tierIds`.
- Backend tạo `promotion` và `promotion_tier`.

### `PATCH /api/v1/admin/promotions/{id}`

- Auth: Admin

Request

```json
{
  "name": "Khuyến mãi hè 2026",
  "description": "Giảm 15% cho khách Gold",
  "discountType": "percentage",
  "discountValue": 15,
  "startDate": "2026-06-01T00:00:00+07:00",
  "endDate": "2026-06-30T23:59:59+07:00",
  "isGlobal": false,
  "tierIds": ["guid"]
}
```

Response `200 OK`

```json
{
  "id": "guid",
  "name": "Khuyến mãi hè 2026",
  "discountValue": 15
}
```

### `POST /api/v1/admin/promotions/{id}/status`

- Auth: Admin

Request

```json
{
  "isActive": false
}
```

Response `200 OK`

```json
{
  "id": "guid",
  "isActive": false
}
```

### `DELETE /api/v1/admin/promotions/{id}`

- Auth: Admin

Response `200 OK`

```json
{
  "message": "Promotion deleted successfully"
}
```

Notes

- Chỉ hard delete promotion chưa active/chưa phát sinh voucher.
- Nếu đã dùng, nên set `is_active = false`.

### `GET /api/v1/promotions/available`

- Auth: Customer

Response `200 OK`

```json
{
  "data": [
    {
      "id": "guid",
      "name": "Khuyến mãi hè",
      "description": "Giảm 10%",
      "discountType": "percentage",
      "discountValue": 10,
      "endDate": "ISO8601"
    }
  ]
}
```

---

## P13 - Admin Rewards

### `GET /api/v1/admin/rewards`

- Auth: Admin

Query Params

- `isActive=true|false`
- `rewardType=free_wash|voucher`
- `page=1`
- `pageSize=20`

Response `200 OK`

```json
{
  "data": [
    {
      "id": "guid",
      "name": "Free Basic Wash",
      "rewardType": "free_wash",
      "pointsRequired": 500,
      "quantityAvailable": 10,
      "validDays": 30,
      "description": "Đổi lượt rửa xe miễn phí",
      "isActive": true,
      "tiers": [
        {
          "id": "guid",
          "name": "Gold"
        }
      ]
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

### `POST /api/v1/admin/rewards`

- Auth: Admin

Request

```json
{
  "name": "Free Basic Wash",
  "rewardType": "free_wash",
  "pointsRequired": 500,
  "quantityAvailable": 10,
  "validDays": 30,
  "description": "Đổi lượt rửa xe miễn phí",
  "tierIds": ["guid"]
}
```

Response `201 Created`

```json
{
  "id": "guid",
  "name": "Free Basic Wash",
  "rewardType": "free_wash",
  "pointsRequired": 500,
  "isActive": true
}
```

### `PATCH /api/v1/admin/rewards/{id}`

- Auth: Admin

Request

```json
{
  "name": "Free Premium Wash",
  "pointsRequired": 800,
  "quantityAvailable": 5,
  "validDays": 30,
  "description": "Đổi lượt rửa xe cao cấp",
  "isActive": true,
  "tierIds": ["guid"]
}
```

Response `200 OK`

```json
{
  "id": "guid",
  "name": "Free Premium Wash",
  "pointsRequired": 800,
  "isActive": true
}
```

### `DELETE /api/v1/admin/rewards/{id}`

- Auth: Admin

Response `200 OK`

```json
{
  "message": "Reward deactivated successfully"
}
```

Notes

- Nên deactivate thay vì hard delete nếu đã có redemption.
- Backend cập nhật `reward_tier` theo `tierIds`.

---

## P14 - Admin Dashboard & Reports

### `GET /api/v1/admin/dashboard`

- Auth: Admin

Query Params

- `fromDate=2026-05-01`
- `toDate=2026-05-31`
- `branchId=guid` optional

Response `200 OK`

```json
{
  "summary": {
    "totalCustomers": 1200,
    "activeCustomers": 1100,
    "lockedCustomers": 12,
    "totalBookings": 5200,
    "completedBookings": 4300,
    "cancelledBookings": 300,
    "totalRevenue": 350000000,
    "totalBranches": 5,
    "activeBranches": 5
  },
  "todayBookings": [
    {
      "id": "guid",
      "startTime": "ISO8601",
      "status": "confirmed",
      "branchName": "AutoWash Quận 1",
      "licensePlate": "51A-12345"
    }
  ],
  "topBranches": [
    {
      "branchId": "guid",
      "branchName": "AutoWash Quận 1",
      "completedBookings": 120,
      "revenue": 9600000
    }
  ]
}
```

### `GET /api/v1/admin/reports/revenue`

- Auth: Admin

Query Params

- `fromDate=2026-05-01`
- `toDate=2026-05-31`
- `branchId=guid`

Response `200 OK`

```json
{
  "fromDate": "2026-05-01",
  "toDate": "2026-05-31",
  "totalRevenue": 350000000,
  "data": [
    {
      "date": "2026-05-01",
      "bookingCount": 20,
      "completedBookingCount": 18,
      "revenue": 1440000
    }
  ]
}
```

### `GET /api/v1/admin/reports/branches`

- Auth: Admin

Query Params

- `fromDate=2026-05-01`
- `toDate=2026-05-31`

Response `200 OK`

```json
{
  "data": [
    {
      "branchId": "guid",
      "branchName": "AutoWash Quận 1",
      "completedBookings": 120,
      "cancelledBookings": 8,
      "revenue": 9600000
    }
  ]
}
```

### `GET /api/v1/admin/reports/loyalty`

- Auth: Admin

Query Params

- `fromDate=2026-05-01`
- `toDate=2026-05-31`

Response `200 OK`

```json
{
  "summary": {
    "totalPointsEarned": 120000,
    "totalPointsRedeemed": 30000,
    "totalRewardsRedeemed": 250,
    "tierUpgradeCount": 40
  },
  "tierDistribution": [
    {
      "tierName": "Silver",
      "customerCount": 800
    },
    {
      "tierName": "Gold",
      "customerCount": 300
    },
    {
      "tierName": "Platinum",
      "customerCount": 100
    }
  ]
}
```

---

## P15 - AI Personalization MVP

### `POST /api/v1/ai/offers/suggest`

- Auth: Customer
- Mục đích: gợi ý ưu đãi cá nhân hóa dựa trên tier, lịch sử rửa xe, điểm và voucher hiện có.

Request

```json
{
  "context": "booking",
  "branchId": "guid"
}
```

Response `200 OK`

```json
{
  "suggestions": [
    {
      "type": "voucher",
      "title": "Dùng voucher WELCOME10",
      "description": "Bạn có thể giảm 10% cho lần đặt này.",
      "voucherCode": "WELCOME10"
    },
    {
      "type": "reward",
      "title": "Gần đủ điểm đổi lượt rửa miễn phí",
      "description": "Bạn cần thêm 150 điểm để đổi Free Basic Wash."
    }
  ],
  "source": "RuleBased"
}
```

Notes

- MVP có thể dùng rule-based trước, chưa cần gọi LLM thật.
- Không trả prompt nội bộ hoặc dữ liệu nhạy cảm.
- Nếu có AI provider, config nằm trong `system_config`.

### `GET /api/v1/admin/ai-settings`

- Auth: Admin

Response `200 OK`

```json
{
  "modelName": "gpt-4o-mini",
  "temperature": 0.7,
  "maxTokens": 1000,
  "isEnabled": false,
  "apiKeyMasked": "sk-...xxxx"
}
```

### `PATCH /api/v1/admin/ai-settings`

- Auth: Admin

Request

```json
{
  "modelName": "gpt-4o-mini",
  "temperature": 0.7,
  "maxTokens": 1000,
  "apiKey": "string | null",
  "isEnabled": true
}
```

Response `200 OK`

```json
{
  "modelName": "gpt-4o-mini",
  "isEnabled": true
}
```

Notes

- Không trả API key đầy đủ.
- Nếu `apiKey = null`, giữ nguyên API key đang lưu.

---

# 8. System Automation Contracts

Các flow dưới đây không phải endpoint FE gọi trực tiếp. Backend chạy bằng background job/cron.

## 8.1. Booking Automation

Trigger

- Chạy mỗi 1 phút hoặc theo scheduler phù hợp.

Rules

1. Booking `pending` quá `depositHoldMinutes` mà chưa confirm → `cancelled`.
2. Booking `confirmed` đến giờ hẹn nhưng chưa check-in quá ngưỡng cho phép → `cancelled`.
3. Booking `in_progress` đủ 15 phút từ `start_time` hoặc `checkedInAt` → `completed`.
4. Khi booking completed → trigger Loyalty Engine.

Pseudo response/log nội bộ

```json
{
  "processedCount": 10,
  "cancelledCount": 2,
  "completedCount": 5,
  "errors": []
}
```

## 8.2. Loyalty Engine

Trigger

- Booking chuyển sang `completed`.

Steps

1. Lấy `pointsPerCompletedWash` từ `system_config`.
2. Tạo `point_transaction` type `earn`.
3. Cộng `customer_profile.total_points`.
4. Tăng `customer_profile.total_washes`.
5. Cập nhật `last_point_activity_at`.
6. Kiểm tra tier mới theo `tier.required_washes`.
7. Nếu tier tăng, cập nhật `customer_profile.tier_id`.
8. Gửi notification `booking_completed`.
9. Nếu tier tăng, gửi notification `tier_upgraded`.

## 8.3. Point Reset Job

Trigger

- Chạy hằng ngày.

Rules

- Nếu `last_point_activity_at` quá 365 ngày hoặc theo config `pointResetAfterDays`, reset điểm về 0.
- Tạo `point_transaction` type `reset`.
- Gửi notification `system_alert` nếu cần.

---

# 9. Field response giữ lại có chủ đích

Các field thời gian được giữ vì UI cần:

- `booking.startTime`, `booking.endTime`: hiển thị lịch và slot.
- `booking.cancelledAt`, `booking.completedAt`: lịch sử booking.
- `voucher.expiresAt`, `voucher.usedAt`: trạng thái voucher.
- `notification.createdAt`: timeline thông báo.
- `pointTransaction.createdAt`: lịch sử điểm.
- `user.lastLoginAt`: màn admin quản lý user.
- `customerProfile.lastPointActivityAt`: rule reset điểm.

Các field không nên trả mặc định:

- `passwordHash`
- raw OTP
- raw AI API key
- internal job logs
- `updatedAt` trong write response nếu FE không dùng
- full `metadata` nếu không cần render

---

# 10. Ghi chú triển khai cho Backend ASP.NET

- Dùng DTO riêng cho request/response, không trả entity EF trực tiếp.
- Dùng transaction DB cho các flow:
  - Register tạo user/profile/vehicles/wallet
  - Booking complete + cộng điểm + nâng tier + notification
  - Redeem reward trừ điểm + tạo voucher + notification
- Các query list nên có pagination.
- Các endpoint Admin phải check role `admin`.
- Các endpoint Customer phải check ownership theo `customer_profile.id`.
- Các thao tác xóa nên ưu tiên soft delete/deactivate nếu entity đã có dữ liệu lịch sử.
- PostgreSQL dùng `numeric(12,2)` cho tiền, `timestamptz` cho thời gian.

# 11. Ghi chú triển khai cho Frontend React

- Không tự tính trạng thái booking nếu backend đã trả `status`.
- Không tự tính điểm/tier; FE hiển thị theo API `loyalty/me`.
- Khi tạo booking:
  1. Gọi branches/vehicles
  2. Gọi booking-slots
  3. Gọi vouchers/validate nếu có voucher
  4. POST bookings
- Khi admin quản lý hệ thống:
  - Dùng `/admin/bookings` và `/admin/booking-slots` để theo dõi vận hành.
  - Dùng `/admin/points-config`, `/admin/tiers`, `/admin/promotions`, `/admin/rewards` cho loyalty/promotion/reward.

# 12. Tổng kết

Bản API contract này đã đồng bộ theo hướng:

- Không còn role Staff.
- Chỉ dùng `customer` và `admin` đúng với DB enum `user_role`.
- Các chức năng cấu hình/vận hành đều thuộc Admin.
- Giữ format client-first giống file mẫu.
- Tôn trọng DBML hiện tại: entity bị comment như `service` và `payment` không được đưa vào API contract MVP.
- Tập trung vào 3 flow chính:
  1. Customer đăng ký → quản lý xe → đặt lịch
  2. Booking lifecycle → admin theo dõi trạng thái
  3. Loyalty → tier upgrade → reward/voucher
