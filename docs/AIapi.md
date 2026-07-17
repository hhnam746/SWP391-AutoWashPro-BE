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
  - Hoi thong tin tai khoan ca nhan cua user dang dang nhap
- `Loyalty`
  - Hoi diem thuong, hang thanh vien, thong tin loyalty
- `Booking`
  - Hoi danh sach booking gan day, booking sap toi, booking theo chi nhanh, bien so, trang thai
- `BookingDetail`
  - Hoi chi tiet 1 booking cu the khi co `bookingId`
- `Voucher`
  - Hoi voucher kha dung, voucher da het han, voucher da dung
- `Promotion`
  - Hoi khuyen mai hien co
- `Branch`
  - Hoi danh sach chi nhanh, dia chi chi nhanh
- `NearestBranch`
  - Da detect duoc intent nhung hien tai backend chi tra loi la chua ho tro o v1
- `TopBranch`
  - Da detect duoc intent nhung hien tai backend chi tra loi la chua ho tro o v1
- `Faq`
  - Hoi thong tin tong quan ve AutoWashPro, cach dat lich, tinh nang he thong

### Vi du cau hoi co the gui vao API

#### 1. Thong tin tai khoan

```json
{
  "conversationId": null,
  "message": "Thong tin tai khoan cua toi la gi?"
}
```

```json
{
  "conversationId": null,
  "message": "Ho so ca nhan cua toi"
}
```

#### 2. Loyalty

```json
{
  "conversationId": null,
  "message": "Toi dang o hang thanh vien nao?"
}
```

```json
{
  "conversationId": null,
  "message": "Toi hien co bao nhieu diem?"
}
```

#### 3. Booking

```json
{
  "conversationId": null,
  "message": "Toi co booking nao sap toi khong?"
}
```

```json
{
  "conversationId": null,
  "message": "Lich su dat lich cua toi"
}
```

```json
{
  "conversationId": null,
  "message": "Booking cua toi o chi nhanh Thu Duc la gi?"
}
```

```json
{
  "conversationId": null,
  "message": "Cho toi xem booking cua xe bien so 51A-12345"
}
```

#### 4. Chi tiet booking

```json
{
  "conversationId": null,
  "message": "Cho toi xem chi tiet booking 11111111-2222-3333-4444-555555555555"
}
```

```json
{
  "conversationId": null,
  "message": "Booking id 11111111-2222-3333-4444-555555555555 cua toi dang trang thai gi?"
}
```

#### 5. Voucher

```json
{
  "conversationId": null,
  "message": "Toi co voucher nao dang dung duoc?"
}
```

```json
{
  "conversationId": null,
  "message": "Voucher cua toi co cai nao het han chua?"
}
```

#### 6. Khuyen mai

```json
{
  "conversationId": null,
  "message": "Hien tai co khuyen mai nao khong?"
}
```

#### 7. Chi nhanh

```json
{
  "conversationId": null,
  "message": "Cho toi danh sach chi nhanh"
}
```

```json
{
  "conversationId": null,
  "message": "Dia chi cac chi nhanh AutoWashPro"
}
```

#### 8. FAQ / thong tin chung

```json
{
  "conversationId": null,
  "message": "AutoWashPro ho tro nhung gi?"
}
```

```json
{
  "conversationId": null,
  "message": "Lam sao de dat lich rua xe?"
}
```

### Luu y ve pham vi cau hoi

- API nay khong phai chatbot tong quat. No duoc toi uu de tra loi dua tren du lieu nghiep vu trong he thong AutoWashPro.
- Backend se uu tien tra loi cac cau hoi lien quan den:
  - tai khoan user hien tai
  - booking
  - voucher
  - loyalty
  - branch
  - promotion
- Neu user hoi nhung noi dung ngoai pham vi he thong, chatbot co the tra loi han che hoac bao khong du du lieu.
- Khong bat buoc phai co `bookingId` neu user dang hoi danh sach booking theo chi nhanh, bien so hoac trang thai.
- Neu can lay chi tiet 1 booking cu the bang ma, `bookingId` nen truyen dung dinh dang `Guid`.

### Bang keyword -> intent de test nhanh

Bang duoi day mo ta cac tu khoa backend hien dang dung de detect intent. Day khong phai danh sach duy nhat, nhung la nhung keyword nen uu tien khi test.

| Intent | Keyword / cum tu goi y | Vi du cau hoi |
| --- | --- | --- |
| `UserProfile` | `thong tin`, `ho so`, `tai khoan`, `profile`, `ca nhan` | `Thong tin tai khoan cua toi la gi?` |
| `Loyalty` | `diem`, `hang`, `loyalty`, `gold`, `silver`, `point` | `Toi con bao nhieu diem?` |
| `BookingDetail` | `booking id`, `ma booking`, hoac co `Guid` | `Chi tiet booking 11111111-2222-3333-4444-555555555555` |
| `Booking` | `lich su dat`, `lich su booking`, `dat lich`, `booking`, `don hang`, `chi nhanh`, `bien so`, `sap toi`, `hoan thanh`, `da huy` | `Booking cua toi o chi nhanh Thu Duc` |
| `Voucher` | `voucher`, `ma giam`, `coupon` | `Toi co voucher nao dang dung duoc?` |
| `Promotion` | `khuyen mai`, `promotion`, `uu dai` | `Hien co uu dai nao khong?` |
| `NearestBranch` | `chi nhanh gan nhat`, `gan nhat`, `nearest branch` | `Chi nhanh gan nhat o dau?` |
| `TopBranch` | `top chi nhanh`, `chi nhanh tot nhat`, `top branch` | `Chi nhanh nao tot nhat?` |
| `Branch` | `chi nhanh`, `dia chi`, `branch` | `Cho toi danh sach chi nhanh` |
| `Faq` | `autowashpro`, `gio mo cua`, `huong dan`, `faq`, `lam sao`, `cach` | `Lam sao de dat lich rua xe?` |

### Thu tu uu tien detect intent

Backend detect intent theo thu tu uu tien noi bo. Dieu nay quan trong khi 1 cau hoi co nhieu keyword.

Thu tu uu tien hien tai:

1. `UserProfile`
2. `Loyalty`
3. `BookingDetail`
4. `Booking`
5. `Voucher`
6. `Promotion`
7. `NearestBranch`
8. `TopBranch`
9. `Branch`
10. `Faq`
11. `Unknown`

Vi du:

- Neu cau hoi co `bookingId` hop le, backend se uu tien detect `BookingDetail`
- Neu cau hoi co `chi tiet booking` nhung khong co `bookingId`, backend se xu ly nhu truy van danh sach booking va co the loc theo chi nhanh/bien so
- Neu cau hoi vua co tu `chi nhanh` vua co `gan nhat`, backend se detect `NearestBranch`
- Neu cau hoi qua chung chung va khong match keyword, intent co the la `Unknown`

### Khuyen nghi khi FE/tester tao prompt

- Nen viet cau hoi ngan, ro, tap trung 1 y
- Voi danh sach booking, FE co the cho user hoi bang ten chi nhanh, bien so xe, hoac trang thai ma khong can hien `bookingId`
- Voi chi tiet booking theo ma cu the, nen kem `bookingId`
- Voi voucher, nen hoi truc tiep nhu:
  - `Toi co voucher nao dang dung duoc?`
  - `Voucher nao cua toi da het han?`
- Han che cau hoi gom nhieu y trong 1 lan, vi detect intent hien tai dang theo keyword don gian
- Neu can do on dinh cao, FE co the cung cap quick prompt buttons theo intent

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

### `data` Fields

- `conversationId`: ID cua hoi thoai
- `answer`: cau tra loi cua chatbot
- `createdAt`: thoi diem tao tin nhan assistant
- `intent`: intent backend detect duoc, vi du:
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
  - `Unknown`

### Vi du `curl`

```bash
curl -X POST "http://localhost:5000/api/v1/chat" \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer <access_token>" \
  -d '{
    "conversationId": null,
    "message": "Toi co booking nao sap toi khong?"
  }'
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

### Muc dich

- Lay toan bo lich su tin nhan cua 1 hoi thoai
- Chi owner cua hoi thoai moi xem duoc

### Path Parameter

- `conversationId`: `Guid`

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

### History Item Fields

- `messageId`: ID cua tin nhan
- `role`: `User` hoac `Assistant`
- `content`: noi dung tin nhan
- `intent`: intent gan voi tin nhan, co the `null`
- `createdAt`: thoi diem tao tin nhan

### Vi du `curl`

```bash
curl "http://localhost:5000/api/v1/chat/d7be2eaf-f0d5-4f55-a55b-7b3ec6af8c2e/history" \
  -H "Authorization: Bearer <access_token>"
```

## 3. Xoa Hoi Thoai

### Endpoint

```http
DELETE /api/v1/chat/{conversationId}
```

### Muc dich

- Soft delete hoi thoai cua user hien tai
- Sau khi xoa, hoi thoai se khong con tra ve duoc nua

### Path Parameter

- `conversationId`: `Guid`

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

### Vi du `curl`

```bash
curl -X DELETE "http://localhost:5000/api/v1/chat/d7be2eaf-f0d5-4f55-a55b-7b3ec6af8c2e" \
  -H "Authorization: Bearer <access_token>"
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

## Ghi Chu Nghiep Vu

- Backend tu detect intent dua tren noi dung cau hoi.
- User da dang nhap nen chatbot uu tien du lieu theo tai khoan hien tai.
- Voi voucher, backend da co fallback noi bo de tra loi tu du lieu nghiep vu ngay ca khi AI provider bi quota, timeout hoac tam thoi khong kha dung.
- Client khong can goi truc tiep Google AI Studio API. FE chi can goi cac endpoint trong `AiChatController`.

## Luong Goi API De Xay UI Chat

1. User gui tin nhan dau tien bang `POST /api/v1/chat` voi `conversationId = null`
2. Client luu `conversationId` tu response
3. Moi lan chat tiep theo, gui lai `conversationId` do
4. Khi mo lai man hinh chat, goi `GET /api/v1/chat/{conversationId}/history`
5. Khi user muon xoa cuoc tro chuyen, goi `DELETE /api/v1/chat/{conversationId}`

## FE Flow Su Dung Cac API Nay

Phan nay mo ta flow thong thuong de frontend xay man hinh chat voi `AiChatController`.

### 1. Mo man hinh chat

- FE khoi tao state chat:
  - `conversationId = null`
  - `messages = []`
  - `loading = false`
- Neu day la phien chat moi, FE chua can goi API ngay
- Neu FE dang mo lai 1 hoi thoai cu va da co `conversationId`, FE goi:

```http
GET /api/v1/chat/{conversationId}/history
```

de nap lai toan bo lich su tin nhan

### 2. User gui tin nhan dau tien

Khi user gui tin nhan dau tien, FE goi:

```http
POST /api/v1/chat
```

voi body:

```json
{
  "conversationId": null,
  "message": "Toi co voucher nao dang dung duoc?"
}
```

FE xu ly response:

- doc `data.conversationId`
- luu `conversationId` vao state
- append cau hoi cua user vao UI
- append cau tra loi cua assistant vao UI

### 3. User chat tiep trong cung hoi thoai

Sau khi da co `conversationId`, moi tin nhan tiep theo deu gui lai ID nay:

```json
{
  "conversationId": "d7be2eaf-f0d5-4f55-a55b-7b3ec6af8c2e",
  "message": "Voucher nao cua toi da het han?"
}
```

Muc dich:

- giu context hoi thoai
- de backend luu tiep message vao dung conversation
- de AI co lich su chat truoc do

### 4. Reload trang hoac mo lai conversation

Neu user refresh trang, dong mo lai app, hoac bam vao 1 cuoc tro chuyen da co, FE khong goi lai `POST` ngay.

Thay vao do:

```http
GET /api/v1/chat/{conversationId}/history
```

FE dung response nay de:

- render lai danh sach bubble chat
- sap xep theo thu tu thoi gian
- phuc hoi lich su conversation len UI

### 5. Xoa hoi thoai

Khi user bam xoa conversation:

```http
DELETE /api/v1/chat/{conversationId}
```

Neu success, FE nen:

- xoa `conversationId` khoi state
- clear `messages`
- dua UI ve trang thai chat moi

### 6. Phan tach vai tro cua tung API

- `POST /api/v1/chat`
  - Dung de gui tin nhan moi
  - Tao conversation moi hoac chat tiep conversation cu

- `GET /api/v1/chat/{conversationId}/history`
  - Dung de load lai lich su hoi thoai
  - Khong dung de gui message moi

- `DELETE /api/v1/chat/{conversationId}`
  - Dung de xoa 1 conversation

### 7. Goi y state cho frontend

```ts
type ChatMessage = {
  id?: string;
  role: "User" | "Assistant";
  content: string;
  intent?: string | null;
  createdAt?: string;
};

type ChatState = {
  conversationId: string | null;
  messages: ChatMessage[];
  loading: boolean;
};
```

### 8. Goi y xu ly UI khi gui tin nhan

Frontend co the dung flow sau:

1. User nhap noi dung va bam gui
2. FE append tam tin nhan user len UI
3. FE goi `POST /api/v1/chat`
4. Neu success:
   - luu `conversationId` neu la lan dau
   - append message assistant vao UI
5. Neu fail:
   - hien toast loi
   - hoac hien 1 bubble thong bao khong gui duoc

### 9. Luu y cho frontend

- Luon gui `Authorization: Bearer <access_token>`
- Khong can FE tu detect intent, backend tu xu ly
- `conversationId` la key de giu context chat
- Neu backend fallback khi AI provider loi, FE van chi can render `data.answer` nhu binh thuong
