# Báo cáo kiểm thử Personalized Promotion/Voucher

## 1. Kết luận

Đã chạy kế hoạch `PERSONALIZED_PROMOTION_SWAGGER_TEST_PLAN.md` trên API thật bằng HTTP request tương đương thao tác Swagger, PostgreSQL và Redis Docker tách biệt. Không kết nối hoặc thay đổi database được cấu hình trong `appsettings`.

| Kết quả | Số lượng |
|---|---:|
| PASS | 45 |
| PARTIAL | 1 |
| FAIL/GAP | 1 |
| BLOCKED ở Swagger thủ công | 1 |
| Tổng | 48 |

Kết quả verification bổ sung:

- `dotnet build SWP391-AutoWashPro-BE.sln --no-restore`: thành công, 0 warning, 0 error.
- `dotnet test` với PostgreSQL test riêng: 23/23 test pass, gồm 9 unit test và 14 integration test.
- `dotnet ef migrations has-pending-model-changes`: không có model change chưa được migration ghi nhận.
- `git diff --check`: pass.

Tính năng cấp voucher, idempotency, delivery retry, rule validation, ownership, Reward regression, Tier Upgrade và audit DOB hoạt động. Chưa nên kết luận sẵn sàng production trước khi xử lý hoặc chấp thuận rõ các GAP ở mục 4, đặc biệt promotion theo tier không kiểm tra trạng thái và double-discount cùng campaign.

## 2. Môi trường kiểm thử

- Thời điểm: 16/07/2026, timezone `Asia/Ho_Chi_Minh`.
- API: `http://localhost:5207`.
- PostgreSQL tạm: Docker, database `autowash_test`.
- PostgreSQL integration test riêng: database `autowash_integration`.
- Redis tạm: Docker.
- Quartz được rút ngắn còn khoảng 5 giây/lượt để kiểm tra rerun và retry.
- SMTP được trỏ tới endpoint không tồn tại có kiểm soát để kiểm tra delivery failure; `DeliveryMaxAttempts=3`, retry delay bằng 0 trong môi trường test.
- Migration hiện có của project được apply vào database Docker sạch. Không tạo hoặc sửa migration trong lần test này.

Các campaign được tạo qua API Promotion và rule được tạo qua API Admin. Request Promotion chứa `DateTimeOffset` `+07:00` bị lỗi; để tiếp tục test đã dùng timestamp UTC hậu tố `Z`.

## 3. Kết quả theo test case

| TC | Kết quả | Actual result/evidence chính |
|---|---|---|
| TC-001 | PASS | Tạo Birthday rule trả 200, đúng `Birthday`, threshold null và active. |
| TC-002 | PASS | GET rule không token trả 401. |
| TC-003 | PASS | Customer token gọi Admin rule API trả 403. |
| TC-004 | PASS | Inactive template thiếu placeholder trả 400: `Inactive email template must contain {PromotionName}.` |
| TC-005 | PASS | Birthday có threshold và NoFirst thiếu threshold đều trả 400 đúng message. |
| TC-006 | PASS | Duplicate rule trả 400; status API bật/tắt rule trả 200, không tạo thêm row. |
| TC-007 | PASS | Birthday customer nhận đúng một voucher, cycle `BIRTHDAY:2026`, source Promotion, status Active. |
| TC-008 | PASS | Customer có DOB sai ngày không có Birthday issuance. |
| TC-009 | PASS | Chờ nhiều lượt job, count cycle `BIRTHDAY:2026` vẫn bằng 1. |
| TC-010 | PASS | Rule inactive, Promotion inactive và Promotion expired đều không cấp voucher. |
| TC-011 | PASS | Account Locked, Inactive và Pending/unverified đều bị bỏ qua. |
| TC-012 | BLOCKED/PASS automated | Swagger không điều khiển clock nên không thể chạy tay ngày 28/02 hoặc 29/02. Bốn trường hợp leap-day trong unit test đều pass. |
| TC-013 | PASS | Customer 45 ngày nhận rule threshold 30; cycle bắt đầu `INACTIVE:30:`. |
| TC-014 | PASS | Customer 29 ngày không nhận rule 30 ngày. |
| TC-015 | PASS | `last_login_at=NULL` không nhận Inactive voucher. |
| TC-016 | PASS | Khi đủ nhiều threshold, hệ thống chọn threshold 30 rồi priority 5; không chọn threshold 7 priority 100. |
| TC-017 | PASS | Nhiều lượt job không tạo lại cùng cycle; count vẫn bằng 1. |
| TC-018 | PASS | Customer đã có threshold 7 được nhận thêm đúng một threshold 30 khi đủ điều kiện, không lặp threshold 7. |
| TC-019 | PASS | Voucher vẫn tồn tại khi email fail. Notification `sent`, attempt 1; email `failed`, attempt dừng ở 3, error `EMAIL_DELIVERY_FAILED`. |
| TC-020 | PARTIAL | Không gọi register vì endpoint upload ảnh có thể ghi ra Cloudinary ngoài môi trường Docker. Đã dùng account Pending cô lập, Admin approval trả 200, `verified_at` được set và Acquisition job cấp đúng Welcome voucher. |
| TC-021 | PASS | Account active/verified legacy nhưng `verified_at=NULL`, Welcome bật và NoFirst tắt: không có Welcome issuance. |
| TC-022 | PASS/GAP | Login/rerun không cấp thêm; Welcome count vẫn bằng 1. Approval lặp không tạo voucher nhưng trả 500 NullReference, xem GAP-03. |
| TC-023 | PASS | Booking đầu tiên dùng Welcome voucher trả 200, discount 74.000, final 26.000; booking thứ hai trả 400 đúng business message. |
| TC-024 | PASS | Account 10 ngày, chưa booking, `verified_at=NULL` nhận đúng một `NO_FIRST_BOOKING:7:` voucher. |
| TC-025 | PASS | Account 6 ngày không nhận NoFirst voucher. |
| TC-026 | PASS | Account 10 ngày đã có booking không nhận NoFirst voucher. |
| TC-027 | PASS | Khi cùng bật hai rule, lượt đầu chỉ cấp Welcome. Sau khi Welcome hết hạn, lượt sau cấp NoFirst; history có hai issuance nhưng chỉ một voucher còn hiệu lực. |
| TC-028 | PASS | Admin check-in trả 200/InProgress; washes 1→2, tier đổi sang tier kế; tạo cycle `TIER_UPGRADE:<nextTierId>` với booking ID làm reference. |
| TC-029 | PASS | Customer washes 0→1 nhưng tier không đổi; không có TierUpgrade issuance. |
| TC-030 | PASS | Check-in lặp trả 400 vì booking không còn Confirmed; issuance cùng tier vẫn bằng 1. Integration test rerun cùng trigger và concurrent contexts cũng pass. |
| TC-031 | PASS | API mới trả cả Promotion và Reward voucher đúng `source`, trigger/cycle; pagination 0 trả 400. |
| TC-032 | PASS | Legacy list trả 200 raw page result; personalized item có `rewardName=null`. |
| TC-033 | PASS | Percentage 15% trên 200.000 trả discount 30.000/final 170.000; FixedAmount trả 25.000/final 175.000. |
| TC-034 | PASS/GAP | Booking dùng voucher trả 200; check-in chuyển voucher sang `used`. `used_at` vẫn null đúng GAP đã ghi trong plan. |
| TC-035 | PASS | Dùng lại voucher Used trả 500, Development detail `Voucher is inactive`, không tạo booking mới. |
| TC-036 | PASS | Customer khác dùng voucher không thuộc sở hữu trả 500, detail `Voucher not found`, không tạo booking. |
| TC-037 | PASS | Voucher quá hạn validate trả 500, detail `Voucher expired`; report phân loại expired theo `expires_at`. |
| TC-038 | PASS | Report nhóm đúng promotion/rule/trigger, counts và conversion rate; Birthday 1 issued/1 used/100%. Date range ngược trả 400. |
| TC-039 | PASS | Notification API trả đúng type, nội dung đã render; metadata có RuleId, CycleKey, VoucherId, PromotionId, TriggerType; DB notification sent/attempt 1. |
| TC-040 | PASS | Reward redeem trả 200; points 1010→910, quantity 5→4; voucher có reward ID, không có promotion ID; point transaction `redeem=-100`. |
| TC-041 | PASS | Cấp personalized Birthday cho customer khác không đổi points 910 và Reward quantity 4; voucher có reward ID null và promotion ID hợp lệ. |
| TC-042 | PASS | Nhiều booking `voucherId=null`, `redemPoint=false` trả 200/Confirmed; DB voucher ID null. |
| TC-043 | PASS/GAP | Customer tier không target, chỉ Welcome global 10% active: discount 10.000/final 90.000. Với customer tier được target, promotion tier inactive vẫn bị cộng, xem GAP-01. |
| TC-044 | PASS | Chỉ Tier promotion FixedAmount 25.000: discount 25.000/final 75.000. |
| TC-045 | FAIL/GAP | Birthday Promotion đang global 15% đồng thời sinh Birthday voucher 15%. Booking cộng global total 64% và cộng tiếp voucher 15%, actual discount 79.000 trên base 100.000. Cùng campaign bị tính hai lần. |
| TC-046 | PASS | Customer set DOB lần đầu trả 200; GET me trả `2000-07-16`; DB có `date_of_birth_set_at`. |
| TC-047 | PASS | Gửi cùng DOB trả 200/no changes; gửi DOB khác trả 400 đúng message; không có correction audit. |
| TC-048 | PASS | Admin sửa DOB trả 200; audit lưu customer/admin/ngày cũ-ngày mới/reason đã trim/thời điểm. Reason rỗng và ngày tương lai trả 400; cùng ngày không thêm audit. |

## 4. GAP và lỗi phát hiện

### GAP-01 — Promotion theo tier inactive vẫn được áp dụng (High)

Thiết lập chỉ để Welcome global 10% active và đặt Tier promotion FixedAmount 25.000 thành inactive. Customer đang ở tier được target vẫn nhận discount 35.000 thay vì 10.000.

- Expected: discount 10.000, final 90.000.
- Actual: discount 35.000, final 65.000.
- Ảnh hưởng: campaign đã tắt vẫn làm giảm giá booking; Admin không thể dừng campaign bằng status một cách đáng tin cậy.
- Cần kiểm tra query PromotionTier trong Booking service để lọc `IsActive`, thời hạn và `IsDeleted` giống rule nghiệp vụ mong muốn.

### GAP-02 — Double-discount cùng Promotion nền (High/business decision)

Birthday Promotion 15% vừa là global promotion vừa là nguồn sinh voucher 15%. Booking dùng voucher nhận cả phần promotion global và phần voucher:

- Không voucher: tổng global promotions đang bật = 64.000 trên base 100.000.
- Dùng Birthday voucher: discount = 79.000, đúng bằng 64.000 + 15.000.
- Nếu một campaign không được tính hai lần, cần loại Promotion nguồn của voucher khỏi tập promotion booking, hoặc không cho campaign personalization đồng thời tham gia global/tier discount.

### GAP-03 — Approval lặp trả 500 NullReference (Medium)

Gọi lại `PATCH /api/v1/admin/users/{id}/approval` trên account đã Active trả:

```json
{
  "success": false,
  "message": "An unexpected error occurred",
  "errors": {
    "detail": "Object reference not set to an instance of an object."
  }
}
```

Không tạo thêm Welcome voucher, nhưng endpoint nên trả business response xác định như 400/404 thay vì 500.

### GAP-04 — Promotion API không nhận DateTimeOffset `+07:00` cho PostgreSQL timestamptz (Medium)

Body theo timezone Việt Nam, ví dụ `2026-07-15T00:00:00+07:00`, làm create Promotion trả 500 trước khi insert. Cùng request chuyển sang UTC `2026-07-14T17:00:00Z` thì trả 200.

API nên normalize `StartDate`/`EndDate` về UTC trước khi ghi PostgreSQL hoặc contract phải validation và trả 400 rõ ràng.

### GAP-05 — Voucher Used nhưng `used_at` không được set (Low)

Sau check-in, `voucher.status='used'` nhưng `used_at IS NULL`. Report vẫn đếm Used theo status, tuy nhiên audit không cho biết voucher được dùng lúc nào.

### GAP-06 — Business error của legacy voucher đang trả 500 (Low/API quality)

Các trường hợp voucher inactive, không thuộc owner hoặc expired lần lượt trả 500. Đây là behavior đã ghi trong test plan, nhưng về API semantics nên map về 400/404 phù hợp.

## 5. Evidence dữ liệu chính

- Birthday issuance: cycle `BIRTHDAY:2026`, code prefix `PV-`, `reward_id=NULL`.
- Inactive issuance: một cycle threshold 7 và một cycle threshold 30; rerun không tăng count.
- Delivery failure: notification `sent/1`, email `failed/3`, sanitized error `EMAIL_DELIVERY_FAILED`.
- Welcome approval: `verified_at` được set đúng lúc Admin duyệt; cycle bắt đầu `WELCOME:`.
- Tier Upgrade: `total_washes=2`, tier level tiếp theo, trigger reference bằng booking ID.
- Report Birthday: issued 1, used 1, conversion 100%.
- DOB correction audit: previous `2000-07-16`, new `2000-07-17`, reason lưu `Correct DOB for TC-048`, đúng Admin ID.

## 6. Phạm vi chưa chạy trực tiếp qua Swagger

- TC-012 không thể đổi clock từ Swagger; policy leap-day đã được xác nhận bằng 4 unit test pass.
- Phần upload/register của TC-020 không chạy để tránh ghi ảnh ra Cloudinary ngoài Docker. Phần Pending → Admin approval → Welcome issuance đã chạy đầy đủ bằng API và Quartz.
- Không kiểm tra SMTP gửi mail thành công tới mailbox thật; đã kiểm tra đầy đủ failure isolation, retry và giới hạn attempt.

## 7. Cleanup

API process test, PostgreSQL/Redis container, network Docker và toàn bộ dữ liệu seed được dừng/xóa sau khi lưu báo cáo. Không có dữ liệu test nào được ghi vào database cấu hình của project.
