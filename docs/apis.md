# APIs (Only from `SWP391-AutoWashPro-BE.Api/Controllers`)

Updated date: 2026-05-23

### `POST /api/v1/auth/register`

- Auth: Public

Request

```json
{
  "email": "customer@example.com",
  "phone": "0900000000",
  "password": "string",
  "firstName": "Nguyen",
  "lastName": "An",
  "cccd": "012345678901",
  "faceImages": ["<file-1>", "<file-2>", "<file-3>"]
}
```

Response `200 OK`

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

Notes

- Content-Type: `multipart/form-data`.
- `faceImages` must have at least 3 files.
- API returns `ApiResponse` wrapper.

---

### `POST /api/v1/auth/login`

- Auth: Public

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

Notes

- Content-Type: `multipart/form-data`.
- `identifier` can be email or phone.
- API returns `ApiResponse` wrapper.

---

### `GET /api/v1/me`

- Auth: Bearer token

Request

```json
{}
```

Response `200 OK`

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

Notes

- API returns `ApiResponse` wrapper.
- Enum is numeric in JSON:
- `role`: `0 = Admin`, `1 = Customer`.
- `status`: `0 = Active`, `1 = Locked`, `2 = Inactive`.

---

### `PATCH /api/v1/me`

- Auth: Bearer token

Request

```json
{
  "firstName": "Nguyen",
  "lastName": "An",
  "cccd": "012345678901"
}
```

Response `200 OK`

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

Notes

- Content-Type: `application/json`.
- `firstName`, `lastName`, `cccd` are optional, but at least 1 field must be provided.
- API returns `ApiResponse` wrapper.

---

### `PATCH /api/v1/me/password`

- Auth: Bearer token

Request

```json
{
  "newPassword": "new_password_123"
}
```

Response `200 OK`

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

Notes

- Content-Type: `application/json`.
- `newPassword` is required by service validation.
- API returns `ApiResponse` wrapper.

---

### `PATCH /api/v1/admin/users/{userId}/verify`

- Auth: Admin

Request

```json
{}
```

Response `200 OK`

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

Notes

- `userId` must be `guid`.
- Only active customer account can be verified.
- API returns `ApiResponse` wrapper.

---

### `GET /api/v1/admin/users`

- Auth: Admin

Request

```json
{
  "searchTerm": "an",
  "pageIndex": 1,
  "pageSize": 10
}
```

Response `200 OK`

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

Notes

- Query params: `searchTerm`, `pageIndex` (default `1`), `pageSize` (default `10`).
- API returns `ApiResponse` wrapper.

---

### `GET /api/v1/admin/users/pending-verification`

- Auth: Admin

Request

```json
{
  "searchTerm": "an",
  "pageIndex": 1,
  "pageSize": 10
}
```

Response `200 OK`

```json
{
  "success": true,
  "message": "Get users pending verification",
  "data": {
    "items": [],
    "totalItems": 0,
    "pageSize": 10,
    "pageIndex": 1
  },
  "errors": null,
  "traceId": "0H...",
  "timestampUtc": "2026-05-23T10:00:00.0000000Z"
}
```

Notes

- Query params: `searchTerm`, `pageIndex` (default `1`), `pageSize` (default `10`).
- API returns `ApiResponse` wrapper.

---

### `GET /api/v1/admin/users/{userId}`

- Auth: Admin

Request

```json
{}
```

Response `200 OK`

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

Notes

- `userId` must be `guid`.
- API returns `ApiResponse` wrapper.

---

### `PATCH /api/v1/admin/users/{userId}/status`

- Auth: Admin

Request

```json
{
  "status": 1
}
```

Response `200 OK`

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

Notes

- `status` chi nhan `0 | 1 | 2` (tuong ung `Active | Locked | Inactive`).
- `reason` khong ton tai trong request DTO hien tai.
- Khong cho admin khoa/chuyen `inactive` chinh minh neu he thong chi con 1 admin active.
- Khi account bi `locked` hoac `inactive`, user khong login duoc.
- Khi account bi `locked` hoac `inactive`, token se bi reject o protected API vi middleware chi cho `AccountStatus.Active`.

---

### `GET /api/v1/admin/users/{userId}/status`

- Auth: Admin

Request

```json
{}
```

Response `200 OK`

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

Notes

- `userId` must be `guid`.
- Enum `status`: `0 = Active`, `1 = Locked`, `2 = Inactive`.
- API returns `ApiResponse` wrapper.

---

### `GET /WeatherForecast`

- Auth: Public

Request

```json
{}
```

Response `200 OK`

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

Notes

- Endpoint nay khong dung `ApiResponse` wrapper.
- Route theo controller token: `[Route("[controller]")]` => `/WeatherForecast`.

---
