# AutoWashPro API Docs (Theo Controllers Hiện Tại)

Tài liệu này chỉ dựa trên API đang có trong thư mục `SWP391-AutoWashPro-BE.Api/Controllers`:
- `AuthController`
- `UserController`
- `AdminController`
- `WeatherForecastController`

Ngày cập nhật: 2026-05-23

## 1. Response format chung

Hầu hết endpoint trả về `ApiResponse`:

```json
{
  "success": true,
  "message": "string",
  "data": {},
  "errors": null,
  "traceId": "string",
  "timestampUtc": "2026-05-23T10:00:00.0000000Z"
}
```

Lưu ý:
- Field JSON dùng `camelCase`.
- Enum mặc định serialize dạng số (không có `JsonStringEnumConverter` trong `Program.cs`).
- Mapping enum:
  - `UserRole`: `0 = Admin`, `1 = Customer`
  - `AccountStatus`: `0 = Active`, `1 = Locked`, `2 = Inactive`

## 2. Error format và HTTP status

Global middleware trả lỗi theo `ApiResponse`:

```json
{
  "success": false,
  "message": "string",
  "data": null,
  "errors": {
    "detail": "string"
  },
  "traceId": "string",
  "timestampUtc": "2026-05-23T10:00:00.0000000Z"
}
```

HTTP status mapping:
- `400`: `ArgumentException`, `InvalidOperationException`
- `401`: `UnauthorizedAccessException`
- `403`: `ForbiddenAccessException`
- `404`: `KeyNotFoundException`
- `500`: lỗi khác

## 3. Danh sách endpoint

| Method | Endpoint | Auth |
| --- | --- | --- |
| POST | `/api/v1/auth/register` | Public |
| POST | `/api/v1/auth/login` | Public |
| GET | `/api/v1/me` | Bearer token |
| PATCH | `/api/v1/me` | Bearer token |
| PATCH | `/api/v1/me/password` | Bearer token |
| PATCH | `/api/v1/admin/users/{userId}/verify` | Admin |
| GET | `/api/v1/admin/users` | Admin |
| GET | `/api/v1/admin/users/pending-verification` | Admin |
| GET | `/api/v1/admin/users/{userId}` | Admin |
| PATCH | `/api/v1/admin/users/{userId}/status` | Admin |
| GET | `/api/v1/admin/users/{userId}/status` | Admin |
| GET | `/WeatherForecast` | Public |

## 4. Chi tiết request/response theo endpoint

### 4.1 POST `/api/v1/auth/register`

- Content-Type: `multipart/form-data`
- Request (`FromForm`, `Request.RegisterRequest`):
  - `email` (string, required)
  - `phone` (string, required)
  - `password` (string, required)
  - `firstName` (string, required)
  - `lastName` (string, required)
  - `cccd` (string, optional)
  - `faceImages` (file[], required, tối thiểu 3 file)

Response `200 OK`:

```json
{
  "success": true,
  "message": "Create user successfully",
  "data": "User registered successfully!",
  "errors": null,
  "traceId": "0H...",
  "timestampUtc": "2026-05-23T10:00:00.0000000Z"
}
```

### 4.2 POST `/api/v1/auth/login`

- Content-Type: `multipart/form-data`
- Request (`FromForm`, `Request.LoginRequest`):
  - `identifier` (string, required; email hoặc phone)
  - `password` (string, required)

Response `200 OK`:

```json
{
  "success": true,
  "message": "Login successfully",
  "data": {
    "access_token": "jwt_token",
    "isVerify": false
  },
  "errors": null,
  "traceId": "0H...",
  "timestampUtc": "2026-05-23T10:00:00.0000000Z"
}
```

### 4.3 GET `/api/v1/me`

- Auth: `[Authorize]`
- Request body: none

Response `200 OK` (`data` = `ProfileResponse`):

```json
{
  "success": true,
  "message": "Get profile successfully",
  "data": {
    "id": "guid",
    "email": "customer@example.com",
    "phone": "0900000000",
    "role": 1,
    "status": 0,
    "profileData": {
      "id": "guid",
      "firstName": "Nguyen",
      "lastName": "An",
      "cccd": "012345678901",
      "tierData": {
        "id": "guid",
        "name": "Silver",
        "level": 1
      }
    },
    "totalPoints": 100,
    "totalWashes": 5,
    "lastPointActivityAt": "2026-05-23T09:00:00+07:00"
  },
  "errors": null,
  "traceId": "0H...",
  "timestampUtc": "2026-05-23T10:00:00.0000000Z"
}
```

### 4.4 PATCH `/api/v1/me`

- Auth: `[Authorize]`
- Content-Type: `application/json`
- Request (`FromBody`, `Request.UpdateProfileRequest`):
  - `firstName` (string, optional)
  - `lastName` (string, optional)
  - `cccd` (string, optional; truyền `null`/rỗng để xóa)

Response `200 OK`:

```json
{
  "success": true,
  "message": "Update profile successfully",
  "data": "Update customer profile successfully",
  "errors": null,
  "traceId": "0H...",
  "timestampUtc": "2026-05-23T10:00:00.0000000Z"
}
```

Trường hợp không thay đổi dữ liệu:

```json
{
  "success": true,
  "message": "Update profile successfully",
  "data": "No profile changes detected",
  "errors": null,
  "traceId": "0H...",
  "timestampUtc": "2026-05-23T10:00:00.0000000Z"
}
```

### 4.5 PATCH `/api/v1/me/password`

- Auth: `[Authorize]`
- Content-Type: `application/json`
- Request (`FromBody`, `Request.UpdateProfileByPassword`):
  - `newPassword` (string, required theo validation service)

Response `200 OK`:

```json
{
  "success": true,
  "message": "Update new password successfully",
  "data": "Update new password successfully",
  "errors": null,
  "traceId": "0H...",
  "timestampUtc": "2026-05-23T10:00:00.0000000Z"
}
```

### 4.6 PATCH `/api/v1/admin/users/{userId}/verify`

- Auth: Admin policy (`JwtExtensions.AdminPolicy`)
- Route param:
  - `userId` (guid, required)
- Request body: none

Response `200 OK`:

```json
{
  "success": true,
  "message": "Admin verification status updated",
  "data": "Verify user successfully",
  "errors": null,
  "traceId": "0H...",
  "timestampUtc": "2026-05-23T10:00:00.0000000Z"
}
```

Trường hợp đã verify:

```json
{
  "success": true,
  "message": "Admin verification status updated",
  "data": "User is already verified.",
  "errors": null,
  "traceId": "0H...",
  "timestampUtc": "2026-05-23T10:00:00.0000000Z"
}
```

### 4.7 GET `/api/v1/admin/users`

- Auth: Admin policy
- Query params:
  - `searchTerm` (string, optional)
  - `pageIndex` (int, default `1`)
  - `pageSize` (int, default `10`)

Response `200 OK` (`data` = `PageResult<AllProfileResponse>`):

```json
{
  "success": true,
  "message": "Get all users",
  "data": {
    "items": [
      {
        "id": "guid",
        "email": "customer@example.com",
        "phone": "0900000000",
        "role": 1,
        "status": 0,
        "isVerified": true,
        "profileData": {
          "id": "guid",
          "firstName": "Nguyen",
          "lastName": "An",
          "cccd": "012345678901",
          "totalPoints": 100,
          "totalWashes": 5,
          "tierData": {
            "id": "guid",
            "name": "Silver",
            "level": 1
          }
        },
        "lastLoginAt": "2026-05-23T09:00:00+07:00",
        "vehicleCount": 2,
        "activeBookingCount": 1
      }
    ],
    "totalItems": 1,
    "pageSize": 10,
    "pageIndex": 1
  },
  "errors": null,
  "traceId": "0H...",
  "timestampUtc": "2026-05-23T10:00:00.0000000Z"
}
```

### 4.8 GET `/api/v1/admin/users/pending-verification`

- Auth: Admin policy
- Query params:
  - `searchTerm` (string, optional)
  - `pageIndex` (int, default `1`)
  - `pageSize` (int, default `10`)

Response `200 OK`:
- Cấu trúc giống `GET /api/v1/admin/users`.
- `message`: `Get users pending verification`.

### 4.9 GET `/api/v1/admin/users/{userId}`

- Auth: Admin policy
- Route param:
  - `userId` (guid, required)

Response `200 OK` (`data` = `GetUserByIdResponse`):

```json
{
  "success": true,
  "message": "Get user by id",
  "data": {
    "id": "guid",
    "email": "customer@example.com",
    "phone": "0900000000",
    "role": 1,
    "status": 0,
    "isVerified": true,
    "profileData": {
      "id": "guid",
      "firstName": "Nguyen",
      "lastName": "An",
      "cccd": "012345678901",
      "totalPoints": 100,
      "totalWashes": 5,
      "tierData": {
        "id": "guid",
        "name": "Silver",
        "level": 1
      }
    },
    "lastLoginAt": "2026-05-23T09:00:00+07:00",
    "vehicleCount": 2,
    "activeBookingCount": 1,
    "wallet": {
      "balance": 0
    },
    "vehicles": [
      {
        "id": "guid",
        "licensePlate": "51A-12345",
        "isActive": true
      }
    ]
  },
  "errors": null,
  "traceId": "0H...",
  "timestampUtc": "2026-05-23T10:00:00.0000000Z"
}
```

### 4.10 PATCH `/api/v1/admin/users/{userId}/status`

- Auth: Admin policy
- Route param:
  - `userId` (guid, required)
- Content-Type: `application/json`
- Request (`FromBody`, `Request.UpdateUserByStatusRequest`):
  - `status` (required)
  - Giá trị hợp lệ: `0`, `1`, `2` tương ứng `Active`, `Locked`, `Inactive`

Ví dụ request:

```json
{
  "status": 1
}
```

Response `200 OK`:

```json
{
  "success": true,
  "message": "Update user status",
  "data": "Update user status successfully",
  "errors": null,
  "traceId": "0H...",
  "timestampUtc": "2026-05-23T10:00:00.0000000Z"
}
```

### 4.11 GET `/api/v1/admin/users/{userId}/status`

- Auth: Admin policy
- Route param:
  - `userId` (guid, required)

Response `200 OK` (`data` = `GetUserStatusResponse`):

```json
{
  "success": true,
  "message": "Get user status",
  "data": {
    "userId": "guid",
    "status": 0
  },
  "errors": null,
  "traceId": "0H...",
  "timestampUtc": "2026-05-23T10:00:00.0000000Z"
}
```

### 4.12 GET `/WeatherForecast`

Lưu ý:
- Endpoint này không dùng `ApiResponse` wrapper.
- Trả trực tiếp `IEnumerable<WeatherForecast>`.

Response `200 OK`:

```json
[
  {
    "date": "2026-05-24",
    "temperatureC": 25,
    "temperatureF": 76,
    "summary": "Warm"
  }
]
```

