# AI Chat API

Tai lieu nay mo ta cac API hien dang duoc expose boi `AiChatController` trong backend AutoWashPro.

## Base Route

```text
/api/v1/chat
```

## Authentication

- Tat ca endpoint deu yeu cau dang nhap.
- Header bat buoc:

```http
Authorization: Bearer <access_token>
```

- Controller dang dung policy: `JwtExtensions.UserPolicy`.

## Response Envelope

Tat ca response deu duoc wrap theo `ApiResponse`:

```json
{
  "success": true,
  "message": "string",
  "data": {},
  "errors": null,
  "traceId": "string",
  "timestampUtc": "2026-07-11T10:00:00Z"
}
```

## 1. Tao Tin Nhan Chat Moi Hoac Chat Tiep

### Endpoint

```http
POST /api/v1/chat
```

### Muc dich

- Tao hoi thoai moi neu `conversationId = null`
- Hoac tiep tuc hoi thoai cu neu truyen `conversationId`

### API nay co the hoi nhung gi?

Backend hien tai detect intent tu cau hoi cua user va ho tro tot nhat cho cac nhom cau hoi sau:

- `UserProfile`
- `Loyalty`
- `Booking`
- `BookingDetail`
- `Voucher`
- `Promotion`
- `Branch`
- `NearestBranch`
- `TopBranch`
- `Faq`

### Request Body

```json
{
  "conversationId": null,
  "message": "Toi co voucher nao dang dung duoc?"
}
```

### Request Fields

- `conversationId`: `Guid?`
  - `null`: tao hoi thoai moi
  - co gia tri: tiep tuc hoi thoai da ton tai
- `message`: `string`
  - Noi dung cau hoi cua user
  - Khong duoc de trong

### Success Response

```json
{
  "success": true,
  "message": "Chat completed successfully",
  "data": {
    "conversationId": "d7be2eaf-f0d5-4f55-a55b-7b3ec6af8c2e",
    "answer": "Ban hien co 2 voucher kha dung: WELCOME10, SUMMER15.",
    "createdAt": "2026-07-11T10:15:00Z",
    "intent": "Voucher"
  },
  "errors": null,
  "traceId": "0HN...001",
  "timestampUtc": "2026-07-11T10:15:00Z"
}
```

### Vi du chat tiep trong cung hoi thoai

```json
{
  "conversationId": "d7be2eaf-f0d5-4f55-a55b-7b3ec6af8c2e",
  "message": "booking gan nhat la luc may gio?"
}
```

## 2. Lay Lich Su Hoi Thoai

### Endpoint

```http
GET /api/v1/chat/{conversationId}/history
```

### Success Response

```json
{
  "success": true,
  "message": "Get chat history successfully",
  "data": [
    {
      "messageId": "0d46f641-5867-4dad-9df1-3be0aa7cd8c2",
      "role": "User",
      "content": "Toi co voucher nao dang dung duoc?",
      "intent": "Voucher",
      "createdAt": "2026-07-11T10:14:45Z"
    },
    {
      "messageId": "46ee6273-4f82-417d-85de-9eb3b7e1c1df",
      "role": "Assistant",
      "content": "Ban hien co 2 voucher kha dung: WELCOME10, SUMMER15.",
      "intent": "Voucher",
      "createdAt": "2026-07-11T10:15:00Z"
    }
  ],
  "errors": null,
  "traceId": "0HN...002",
  "timestampUtc": "2026-07-11T10:16:00Z"
}
```

## 3. Xoa Hoi Thoai

### Endpoint

```http
DELETE /api/v1/chat/{conversationId}
```

### Success Response

```json
{
  "success": true,
  "message": "Delete conversation successfully",
  "data": true,
  "errors": null,
  "traceId": "0HN...003",
  "timestampUtc": "2026-07-11T10:20:00Z"
}
```

## Error Cases Thuong Gap

### 400 Bad Request

Khi `message` rong:

```json
{
  "success": false,
  "message": "Message is required.",
  "errors": {
    "detail": "Message is required."
  }
}
```

### 401 Unauthorized

- Thieu token
- Token khong hop le

### 404 Not Found

Khi `conversationId` khong ton tai hoac khong thuoc ve user hien tai:

```json
{
  "success": false,
  "message": "Conversation not found."
}
```

## Luong Goi API De Xay UI Chat

1. User gui tin nhan dau tien bang `POST /api/v1/chat` voi `conversationId = null`
2. Client luu `conversationId` tu response
3. Moi lan chat tiep theo, gui lai `conversationId` do
4. Khi mo lai man hinh chat, goi `GET /api/v1/chat/{conversationId}/history`
5. Khi user muon xoa cuoc tro chuyen, goi `DELETE /api/v1/chat/{conversationId}`

## FE Flow Su Dung Cac API Nay

### 1. Mo man hinh chat

- FE khoi tao state chat:
  - `conversationId = null`
  - `messages = []`
  - `loading = false`
- Neu FE dang mo lai 1 hoi thoai cu va da co `conversationId`, FE goi `GET /api/v1/chat/{conversationId}/history`

### 2. User gui tin nhan dau tien

- FE goi `POST /api/v1/chat`
- Doc `data.conversationId`
- Luu `conversationId` vao state
- Append message user va assistant vao UI

### 3. User chat tiep

- Moi tin nhan tiep theo deu gui lai `conversationId`
- FE khong can detect intent, backend tu xu ly

### 4. Xoa hoi thoai

- FE goi `DELETE /api/v1/chat/{conversationId}`
- Neu success thi clear state va dua UI ve phien chat moi
