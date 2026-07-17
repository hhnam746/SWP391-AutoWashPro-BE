# Kế hoạch API Voucher cá nhân hóa

Ngày cập nhật: 16/07/2026

## 1. Mục tiêu và phạm vi

Tài liệu này chỉ mô tả các API mới cần bổ sung cho tính năng voucher cá nhân hóa. Mọi API đã tồn tại trong dự án đều nằm ngoài phạm vi tài liệu và không cần triển khai lại.

Các API mới phải hỗ trợ đủ 9 luồng:

1. Welcome voucher
2. No-first-booking
3. Tier upgrade
4. Wash milestone
5. Service recovery
6. Expected wash due
7. Branch affinity
8. Off-peak targeting
9. Near-tier-upgrade

Các endpoint `/internal/...` trong tài liệu là hợp đồng xử lý nội bộ. Khi triển khai thực tế, chúng có thể là service method, domain event hoặc background job thay vì public controller.

---

## 2. API mới dành cho Customer

### 2.1. `GET /api/v1/vouchers/recommended`

**Nhiệm vụ**

- Lấy danh sách ưu đãi được cá nhân hóa cho customer hiện tại.
- Dùng để hiển thị khu vực "Dành cho bạn" tại trang chủ, màn hình booking hoặc loyalty.
- Chỉ trả về ưu đãi phù hợp với tier, lịch sử booking và hành vi của customer.
- API chỉ trả về gợi ý, chưa tạo voucher thật.

**Request**

Query parameters:

| Tên         | Kiểu | Bắt buộc | Mô tả                                             |
| ----------- | ---- | -------- | ------------------------------------------------- |
| `branchId`  | guid | Không    | Chi nhánh customer đang xem hoặc chuẩn bị booking |
| `pageSize`  | int  | Có       | Số lượng phần tử trên một trang, phải lớn hơn 0   |
| `pageIndex` | int  | Có       | Số trang cần lấy, bắt đầu từ 1                    |

```http
GET /api/v1/vouchers/recommended?pageSize=10&pageIndex=1
```

**Response thành công - `200 OK`**

```json
{
  "items": [
    {
      "promotionId": "guid",
      "title": "Ưu đãi dành cho thành viên Gold",
      "description": "Giảm 200.000 đồng cho lần rửa xe tiếp theo",
      "discountType": "fixed_amount",
      "discountValue": 200000,
      "category": "tier_upgrade",
      "priority": "high",
      "recommendationReason": "Bạn vừa đạt hạng thành viên Gold",
      "claimMode": "claim",
      "applicableBranchId": null,
      "validServiceTimeFrom": null,
      "validServiceTimeTo": null,
      "expiresAt": "2026-08-01T23:59:59+07:00"
    }
  ],
  "totalItems": 12,
  "pageSize": 10,
  "pageIndex": 1
}
```

Danh sách nên được sắp xếp theo `priority` giảm dần, sau đó theo thời gian hết hạn gần nhất trước khi thực hiện phân trang.

---

### 2.2. `POST /api/v1/vouchers/recommended/{promotionId}/claim`

**Nhiệm vụ**

- Cho phép customer nhận một ưu đãi được hệ thống đề xuất.
- Kiểm tra lại điều kiện cá nhân hóa tại thời điểm nhận.
- Tạo voucher thật cho customer sau khi claim thành công.
- Ngăn customer nhận trùng ưu đãi vượt quá giới hạn của campaign.

**Request**

Route parameter:

| Tên           | Kiểu | Bắt buộc | Mô tả                             |
| ------------- | ---- | -------- | --------------------------------- |
| `promotionId` | guid | Có       | Promotion được customer chọn nhận |

Customer được xác định từ access token đang đăng nhập. API này không cần request body.

```http
POST /api/v1/vouchers/recommended/3fa85f64-5717-4562-b3fc-2c963f66afa6/claim
Authorization: Bearer {accessToken}
```

**Response thành công - `201 Created`**

```json
{
  "success": true,
  "message": "Nhận voucher cá nhân hóa thành công",
  "data": {
    "voucherId": "guid",
    "promotionId": "guid",
    "code": "GOLD200",
    "status": "active",
    "discountType": "fixed_amount",
    "discountValue": 200000,
    "applicableBranchId": null,
    "validServiceTimeFrom": null,
    "validServiceTimeTo": null,
    "expiresAt": "2026-08-01T23:59:59+07:00"
  },
  "errors": null
}
```

**Các trường hợp lỗi**

- `400 Bad Request`: promotion hết hạn, không còn hiệu lực hoặc customer không còn đủ điều kiện.
- `404 Not Found`: không tìm thấy promotion.
- `409 Conflict`: customer đã nhận đủ số lần cho phép.

---

## 3. API mới dành cho Admin

### 3.1. `GET /api/v1/admin/personalized-vouchers/report`

**Nhiệm vụ**

- Thống kê hiệu quả voucher theo từng luồng cá nhân hóa.
- Theo dõi số lượng đã cấp, đã được claim, đã sử dụng và tỷ lệ chuyển đổi.
- Hỗ trợ admin đánh giá hiệu quả từng campaign.

**Request**

Query parameters:

| Tên           | Kiểu   | Bắt buộc | Mô tả                                  |
| ------------- | ------ | -------- | -------------------------------------- |
| `fromDate`    | date   | Có       | Ngày bắt đầu thống kê                  |
| `toDate`      | date   | Có       | Ngày kết thúc thống kê                 |
| `triggerType` | string | Không    | Lọc theo một trong 9 luồng cá nhân hóa |

```http
GET /api/v1/admin/personalized-vouchers/report?fromDate=2026-07-01&toDate=2026-07-31&triggerType=welcome_voucher
```

**Response thành công - `200 OK`**

```json
{
  "success": true,
  "message": "Lấy báo cáo voucher cá nhân hóa thành công",
  "data": [
    {
      "campaignId": "guid",
      "campaignName": "Welcome Voucher Campaign",
      "triggerType": "welcome_voucher",
      "issuedCount": 120,
      "claimedCount": 95,
      "usedCount": 70,
      "conversionRate": 58.3
    }
  ],
  "errors": null
}
```

---

## 4. Xử lý nội bộ cho 9 luồng cá nhân hóa

### 4.1. Welcome voucher

`POST /internal/personalized-vouchers/welcome-issue`

**Nhiệm vụ:** Tự động cấp voucher cho lần booking đầu tiên sau khi customer đăng ký và xác thực tài khoản thành công.

**Request**

Không cần request body. Hệ thống lấy `userId` từ `ClaimTypes.NameIdentifier` trong access token của customer.

```http
POST /internal/personalized-vouchers/welcome-issue
Authorization: Bearer {accessToken}
```

**Response**

```json
{
  "trigger": "welcome_voucher",
  "userId": "guid",
  "voucherIssued": true,
  "voucherId": "guid",
  "skippedReason": null
}
```

**Quy tắc:** Claim phải chứa `userId` hợp lệ. Chỉ cấp một lần cho mỗi customer và voucher chỉ áp dụng cho booking đầu tiên. Client không được truyền `userId` để tránh cấp voucher cho tài khoản khác.

---

### 4.2. No-first-booking

`POST /internal/personalized-vouchers/no-first-booking-issue`

**Nhiệm vụ:** Tìm customer đã đăng ký đủ 7 ngày nhưng chưa từng booking, sau đó cấp voucher hoặc tạo gợi ý thúc đẩy lần đặt đầu tiên.

**Request**

```json
{
  "asOfDate": "2026-07-16",
  "registrationAgeDaysThreshold": 7
}
```

**Response**

```json
{
  "trigger": "no_first_booking",
  "processedUsers": 150,
  "matchedUsers": 32,
  "issuedCount": 20,
  "recommendedCount": 12
}
```

**Quy tắc:** Không chọn customer đã có bất kỳ booking nào và không cấp lại trong cùng campaign.

---

### 4.3. Tier upgrade - không cần API mới

**Cách xử lý**

- Tier đã được hệ thống tự động cập nhật trong `CustomerProfile` khi booking hoàn thành, vì vậy không tạo API cập nhật tier và không cập nhật tier lần thứ hai.
- Ngay sau bước cập nhật `CustomerProfile`, so sánh tier trước và sau booking.
- Nếu customer vừa được nâng lên Gold hoặc Platinum, gọi service cấp voucher chúc mừng trong cùng luồng xử lý hoặc phát domain event `CustomerTierUpgraded`.
- Dữ liệu sự kiện chỉ cần gồm `userId`, `oldTierId`, `newTierId` và `bookingId`.

**Kết quả xử lý nội bộ**

```json
{
  "trigger": "tier_upgrade",
  "userId": "guid",
  "bookingId": "guid",
  "oldTierId": "guid",
  "newTierId": "guid",
  "voucherIssued": true,
  "voucherId": "guid",
  "skippedReason": null
}
```

**Quy tắc:** Chỉ cấp voucher một lần cho mỗi lần nâng tier, không cấp khi tier không đổi hoặc bị hạ. Dùng `userId`, `newTierId` và `bookingId` làm dữ liệu chống xử lý trùng.

---

### 4.4. Wash milestone - không cần API mới

**Cách xử lý**

- Khi một booking chuyển sang trạng thái `Completed`, dùng `Booking.CustomerId` để lấy `CustomerProfile` của customer.
- `CustomerProfile` đã có `UserId` và `TotalWashes`, vì vậy không nhận `userId` hoặc `totalCompletedWashes` từ client và cũng không lấy customer từ claim của staff/admin.
- Sau khi tăng `CustomerProfile.TotalWashes`, kiểm tra giá trị mới có phải mốc 5, 10 hoặc 20 hay không.
- Nếu đạt mốc, gọi service cấp voucher giảm giá hoặc miễn phí rửa xe ngay trong luồng hoàn thành booking.

**Kết quả xử lý nội bộ**

```json
{
  "trigger": "wash_milestone",
  "userId": "guid",
  "bookingId": "guid",
  "milestone": 10,
  "benefitType": "discount_voucher",
  "voucherIssued": true,
  "voucherId": "guid",
  "skippedReason": null
}
```

**Quy tắc:** Chỉ tăng số lần rửa khi booking thực sự chuyển sang `Completed`. Mỗi booking chỉ được tính một lần và mỗi mốc chỉ được nhận một voucher. Dùng `bookingId` và `milestone` làm dữ liệu chống xử lý trùng.

---

### 4.5. Service recovery

`POST /internal/personalized-vouchers/service-recovery-issue`

**Nhiệm vụ:** Tự động cấp voucher xin lỗi khi booking bị hệ thống hoặc admin hủy do lỗi vận hành.

**Request**

```json
{
  "bookingId": "guid",
  "userId": "guid",
  "cancelledBy": "admin",
  "cancelReasonCode": "system_operational_issue"
}
```

**Response**

```json
{
  "trigger": "service_recovery",
  "bookingId": "guid",
  "userId": "guid",
  "voucherIssued": true,
  "voucherId": "guid",
  "skippedReason": null
}
```

**Quy tắc:** Không cấp nếu customer tự hủy, booking bị hủy do customer vi phạm hoặc booking đã được bồi thường trước đó.

---

### 4.6. Expected wash due

`POST /internal/personalized-vouchers/expected-wash-due-issue`

**Nhiệm vụ:** Tìm customer đã quá chu kỳ rửa xe thông thường, tạo voucher nhỏ và xếp hàng gửi thông báo nhắc lịch.

**Request**

```json
{
  "asOfDate": "2026-07-16",
  "minimumCompletedWashes": 2,
  "overdueGraceDays": 2
}
```

**Response**

```json
{
  "trigger": "expected_wash_due",
  "processedUsers": 800,
  "matchedUsers": 95,
  "issuedCount": 70,
  "recommendedCount": 25,
  "notificationQueuedCount": 95
}
```

**Quy tắc**

- Chu kỳ dự kiến được tính từ khoảng cách trung bình giữa các booking đã hoàn thành.
- Không chọn customer đang có booking trong tương lai.
- Mỗi customer chỉ được nhắc hoặc cấp ưu đãi một lần trong cùng chu kỳ.

---

### 4.7. Branch affinity

`POST /internal/personalized-vouchers/branch-affinity-issue`

**Nhiệm vụ:** Xác định chi nhánh customer thường sử dụng nhất và tạo ưu đãi chỉ áp dụng tại chi nhánh đó.

**Request**

```json
{
  "lookbackDays": 90,
  "minimumCompletedBookings": 3,
  "minimumAffinityRate": 0.6,
  "branchId": null
}
```

**Response**

```json
{
  "trigger": "branch_affinity",
  "processedUsers": 620,
  "matchedUsers": 140,
  "issuedCount": 100,
  "recommendedCount": 40
}
```

**Quy tắc**

- Tỷ lệ `0.6` nghĩa là ít nhất 60% booking hợp lệ của customer thuộc cùng một chi nhánh.
- Voucher phải lưu `applicableBranchId` và không được sử dụng tại chi nhánh khác.
- Chỉ tính booking đã hoàn thành, không tính booking đã hủy.

---

### 4.8. Off-peak targeting

`POST /internal/personalized-vouchers/off-peak-targeting-issue`

**Nhiệm vụ:** Tìm chi nhánh và khung giờ thấp điểm còn nhiều slot, sau đó tạo voucher chỉ sử dụng trong phạm vi đó cho customer phù hợp.

**Request**

```json
{
  "serviceDateFrom": "2026-07-17",
  "serviceDateTo": "2026-07-24",
  "branchId": null,
  "minimumAvailableSlotRate": 0.5
}
```

**Response**

```json
{
  "trigger": "off_peak_targeting",
  "matchedBranches": 4,
  "matchedTimeWindows": 9,
  "matchedUsers": 180,
  "issuedCount": 120,
  "recommendedCount": 60
}
```

**Quy tắc**

- Voucher phải lưu `applicableBranchId`, `validServiceTimeFrom` và `validServiceTimeTo`.
- Voucher chỉ hợp lệ khi chi nhánh và giờ sử dụng dịch vụ nằm trong phạm vi đã cấu hình.
- Không cấp voucher nếu khung giờ đã hết slot tại thời điểm xử lý.

---

### 4.9. Near-tier-upgrade

`POST /internal/personalized-vouchers/near-tier-upgrade-issue`

**Nhiệm vụ:** Tìm customer chỉ còn 1-2 lần rửa để đạt tier tiếp theo, sau đó cấp voucher cho lượt tiếp theo hoặc tặng bonus point.

**Request**

```json
{
  "remainingWashThreshold": 2,
  "benefitType": "voucher"
}
```

`benefitType` nhận một trong hai giá trị: `voucher` hoặc `bonus_point`.

**Response**

```json
{
  "trigger": "near_tier_upgrade",
  "processedUsers": 500,
  "matchedUsers": 45,
  "voucherIssuedCount": 30,
  "bonusPointIssuedCount": 15
}
```

**Quy tắc**

- Xác định tier tiếp theo và số lần rửa còn thiếu theo tier rule hiện tại.
- Không cấp lặp lại cho cùng customer và cùng tier mục tiêu.
- Khi tặng bonus point, phải tạo point transaction để có thể truy vết.

---

## 5. Bảng đối chiếu tính năng

| Luồng trong yêu cầu | API mới                                                             | Kết quả chính                       | Ưu tiên    |
| ------------------- | ------------------------------------------------------------------- | ----------------------------------- | ---------- |
| Welcome voucher     | `POST /internal/personalized-vouchers/welcome-issue`                | Voucher cho booking đầu tiên        | Cao        |
| No-first-booking    | `POST /internal/personalized-vouchers/no-first-booking-issue`       | Voucher thúc đẩy lần đặt đầu        | Cao        |
| Tier upgrade        | Không cần API mới; xử lý sau khi `CustomerProfile` tự cập nhật tier | Voucher chúc mừng tier mới          | Cao        |
| Wash milestone      | Không cần API mới; xử lý khi booking chuyển sang `Completed`        | Voucher giảm giá hoặc miễn phí rửa  | Cao        |
| Service recovery    | `POST /internal/personalized-vouchers/service-recovery-issue`       | Voucher xin lỗi do lỗi vận hành     | Rất cao    |
| Expected wash due   | `POST /internal/personalized-vouchers/expected-wash-due-issue`      | Nhắc lịch và voucher nhỏ            | Trung bình |
| Branch affinity     | `POST /internal/personalized-vouchers/branch-affinity-issue`        | Ưu đãi riêng theo chi nhánh         | Trung bình |
| Off-peak targeting  | `POST /internal/personalized-vouchers/off-peak-targeting-issue`     | Voucher giới hạn theo giờ thấp điểm | Trung bình |
| Near-tier-upgrade   | `POST /internal/personalized-vouchers/near-tier-upgrade-issue`      | Voucher hoặc bonus point            | Trung bình |

---

## 6. Quy tắc chung khi triển khai

- Mọi luồng cấp voucher phải có khóa chống xử lý trùng theo `userId`, `triggerType` và mốc sự kiện liên quan.
- Chỉ sử dụng promotion đang hoạt động và còn hiệu lực.
- Phải lưu nguồn cấp voucher bằng `triggerType` để phục vụ báo cáo và truy vết.
- Luồng chạy nền phải ghi lại số lượng đã xử lý, thành công, bỏ qua và lỗi.
- Voucher giới hạn theo chi nhánh hoặc khung giờ phải được kiểm tra lại tại thời điểm booking và validate.
- Nếu customer không còn đủ điều kiện tại thời điểm claim thì không tạo voucher.
- Các endpoint nội bộ không được mở công khai; chỉ background worker hoặc service có quyền mới được gọi.

---

## 7. Thứ tự ưu tiên triển khai

### Giai đoạn 1 - Các luồng ưu tiên cao

1. `POST /internal/personalized-vouchers/service-recovery-issue`
2. `POST /internal/personalized-vouchers/welcome-issue`
3. `POST /internal/personalized-vouchers/no-first-booking-issue`
4. Bổ sung handler cấp voucher sau khi `CustomerProfile` tự động nâng tier
5. Bổ sung handler cấp voucher theo `CustomerProfile.TotalWashes` sau khi booking hoàn thành
6. `GET /api/v1/vouchers/recommended`
7. `POST /api/v1/vouchers/recommended/{promotionId}/claim`

### Giai đoạn 2 - Các luồng phân tích hành vi

1. `POST /internal/personalized-vouchers/expected-wash-due-issue`
2. `POST /internal/personalized-vouchers/branch-affinity-issue`
3. `POST /internal/personalized-vouchers/off-peak-targeting-issue`
4. `POST /internal/personalized-vouchers/near-tier-upgrade-issue`

### Giai đoạn 3 - Đo lường hiệu quả

1. `GET /api/v1/admin/personalized-vouchers/report`

## 8. Kết luận

Kế hoạch trên bao phủ đủ 9 luồng cá nhân hóa trong yêu cầu. Tài liệu chỉ liệt kê API mới cần bổ sung, có nhiệm vụ, request, response và quy tắc nghiệp vụ tương ứng; không yêu cầu viết lại API đã tồn tại trong dự án.
