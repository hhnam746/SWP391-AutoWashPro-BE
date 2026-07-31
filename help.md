# Hướng dẫn FE: cảnh báo hai booking quá gần nhau

## 1. Mục tiêu

Khi customer tạo một booking mới, backend kiểm tra booking đó với tất cả booking active
của cùng customer:

- Áp dụng cho cả cùng chi nhánh và khác chi nhánh.
- Áp dụng cả khi customer sử dụng hai xe khác nhau.
- Mặc định cảnh báo nếu khoảng cách giữa hai booking không quá 30 phút.
- Booking `completed` và `cancelled` không được tính.
- Đây là warning có thể xác nhận tiếp tục, không phải lỗi chặn vĩnh viễn.
- Riêng trường hợp trùng chính xác slot tại cùng chi nhánh vẫn là lỗi cứng và không thể bỏ qua.

FE cần triển khai flow:

```text
POST booking lần đầu
        |
        +-- 200: tạo booking thành công
        |
        +-- 409 + BOOKING_TIME_TOO_CLOSE
                  |
                  +-- Hiển thị popup
                          |
                          +-- Hủy: đóng popup, không gọi lại API
                          |
                          +-- Tiếp tục: gửi lại request với các conflict ID
                                        |
                                        +-- 200: thành công
                                        |
                                        +-- 409: có conflict mới, hiển thị lại popup
```

## 2. API tạo booking

Endpoint:

```http
POST /api/v1/bookings
Authorization: Bearer <access-token>
Content-Type: application/json
```

Request lần đầu:

```json
{
  "branchId": "1c920d24-85dc-41da-b650-15c608aaf755",
  "vehicleId": "94731fcb-0615-42c6-bd41-20a8dce7fe17",
  "voucherId": null,
  "bookingDate": "2026-08-01",
  "startTime": "2026-08-01T09:00:00+07:00",
  "redemPoint": false,
  "acknowledgedScheduleConflictIds": []
}
```

Lưu ý:

- Phải giữ nguyên tên `redemPoint` theo API hiện tại.
- `acknowledgedScheduleConflictIds` ở lần gọi đầu là mảng rỗng.
- Không tự tính `endTime`; backend tính theo slot.
- `startTime` cần gửi kèm offset `+07:00`.

## 3. Response warning

Khi có booking quá gần, backend trả HTTP `409 Conflict`:

```json
{
  "success": false,
  "message": "Booking time is too close to another booking.",
  "data": null,
  "errors": {
    "code": "BOOKING_TIME_TOO_CLOSE",
    "severity": "warning",
    "thresholdMinutes": 30,
    "conflicts": [
      {
        "bookingId": "919768ee-52c2-4d44-bccf-8d64a6bdb834",
        "branchId": "fc6fcad6-71ec-46eb-9e6a-57969a80d066",
        "branchName": "AutoWash Quận 1",
        "startTime": "2026-08-01T08:15:00+07:00",
        "endTime": "2026-08-01T08:30:00+07:00",
        "isSameBranch": false,
        "gapMinutes": 30
      }
    ]
  },
  "traceId": "0H...",
  "timestampUtc": "2026-07-31T05:00:00Z"
}
```

FE chỉ mở popup xác nhận khi đồng thời thỏa:

```ts
status === 409 &&
response.data?.errors?.code === "BOOKING_TIME_TOO_CLOSE" &&
response.data?.errors?.severity === "warning"
```

Không nên dựa vào nội dung `message`, vì message có thể được đổi hoặc dịch.

## 4. Nội dung popup đề xuất

Tiêu đề:

```text
Lịch đặt quá gần nhau
```

Nội dung chung:

```text
Booking mới cách một lịch đặt khác của bạn không quá 30 phút.
Bạn có chắc chắn muốn tiếp tục đặt lịch không?
```

Với từng conflict:

```text
AutoWash Quận 1
08:15 - 08:30, 01/08/2026
Cách lịch mới: 30 phút
Khác chi nhánh
```

Nếu `isSameBranch === true`, hiển thị `Cùng chi nhánh`.

Nếu `gapMinutes === 0`, nên hiển thị:

```text
Thời gian hai booking bị trùng hoặc liền nhau.
```

Nút popup:

- `Quay lại`: đóng popup, không tạo booking.
- `Vẫn đặt lịch`: gọi lại API với toàn bộ conflict IDs.

Trong lúc retry API, disable cả hai nút để tránh double submit.

## 5. TypeScript types

```ts
export interface CreateBookingRequest {
  branchId: string;
  vehicleId: string;
  voucherId: string | null;
  bookingDate: string;
  startTime: string;
  redemPoint: boolean;
  acknowledgedScheduleConflictIds: string[];
}

export interface BookingScheduleConflict {
  bookingId: string;
  branchId: string;
  branchName: string;
  startTime: string;
  endTime: string;
  isSameBranch: boolean;
  gapMinutes: number;
}

export interface BookingScheduleWarning {
  code: "BOOKING_TIME_TOO_CLOSE";
  severity: "warning";
  thresholdMinutes: number;
  conflicts: BookingScheduleConflict[];
}

export interface BookingWarningErrorResponse {
  success: false;
  message: string;
  data: null;
  errors: BookingScheduleWarning;
  traceId?: string;
  timestampUtc: string;
}
```

## 6. Axios service mẫu

Không nên tự động retry bên trong interceptor toàn cục, vì cần customer xác nhận trên UI.

```ts
import axios, { AxiosError } from "axios";

export async function createBooking(payload: CreateBookingRequest) {
  const response = await api.post("/api/v1/bookings", payload);
  return response.data;
}

export function getBookingScheduleWarning(
  error: unknown,
): BookingScheduleWarning | null {
  if (!axios.isAxiosError(error)) {
    return null;
  }

  const axiosError =
    error as AxiosError<BookingWarningErrorResponse>;
  const warning = axiosError.response?.data?.errors;

  if (
    axiosError.response?.status === 409 &&
    warning?.code === "BOOKING_TIME_TOO_CLOSE" &&
    warning.severity === "warning"
  ) {
    return warning;
  }

  return null;
}
```

## 7. Flow xử lý trong component/hook

```ts
const [pendingRequest, setPendingRequest] =
  useState<CreateBookingRequest | null>(null);
const [scheduleWarning, setScheduleWarning] =
  useState<BookingScheduleWarning | null>(null);
const [isSubmitting, setIsSubmitting] = useState(false);

async function submitBooking(formValue: BookingFormValue) {
  const request: CreateBookingRequest = {
    branchId: formValue.branchId,
    vehicleId: formValue.vehicleId,
    voucherId: formValue.voucherId ?? null,
    bookingDate: formValue.bookingDate,
    startTime: formValue.startTime,
    redemPoint: formValue.redemPoint,
    acknowledgedScheduleConflictIds: [],
  };

  await sendBookingRequest(request);
}

async function sendBookingRequest(request: CreateBookingRequest) {
  try {
    setIsSubmitting(true);

    const booking = await createBooking(request);

    setPendingRequest(null);
    setScheduleWarning(null);

    // Hiển thị toast thành công hoặc chuyển sang trang booking detail.
    navigate(`/bookings/${booking.id}`);
  } catch (error) {
    const warning = getBookingScheduleWarning(error);

    if (warning) {
      // Lưu nguyên request để retry, tránh lấy lại form state đã bị thay đổi.
      setPendingRequest(request);
      setScheduleWarning(warning);
      return;
    }

    // Dùng error handler hiện tại cho lỗi slot, validation, auth...
    handleApiError(error);
  } finally {
    setIsSubmitting(false);
  }
}

async function confirmCloseBooking() {
  if (!pendingRequest || !scheduleWarning) {
    return;
  }

  const conflictIds = scheduleWarning.conflicts.map(
    (conflict) => conflict.bookingId,
  );

  await sendBookingRequest({
    ...pendingRequest,
    // Dùng toàn bộ IDs backend vừa trả về, không chỉ conflict đầu tiên.
    acknowledgedScheduleConflictIds: conflictIds,
  });
}

function cancelCloseBooking() {
  setScheduleWarning(null);
  setPendingRequest(null);
}
```

Backend sẽ kiểm tra lại khi customer nhấn `Vẫn đặt lịch`. Nếu trong thời gian popup đang mở
có booking conflict mới xuất hiện, backend trả `409` mới. Hàm trên sẽ cập nhật popup bằng
danh sách conflict mới nhất và yêu cầu customer xác nhận lại.

## 8. Phân biệt warning với lỗi slot

Không phải mọi response `409` đều là warning có thể bỏ qua.

Chỉ cho phép nút `Vẫn đặt lịch` khi:

```ts
errors.code === "BOOKING_TIME_TOO_CLOSE"
```

Nếu slot tại chi nhánh đã có người đặt:

- Không retry bằng `acknowledgedScheduleConflictIds`.
- Hiển thị lỗi slot đã được đặt.
- Yêu cầu customer chọn slot khác.
- Refresh lại danh sách booking slots nếu màn hình có hỗ trợ.

## 9. Format thời gian

Backend trả `startTime` và `endTime` với offset Việt Nam. FE có thể format:

```ts
function formatBookingTime(value: string) {
  return new Intl.DateTimeFormat("vi-VN", {
    hour: "2-digit",
    minute: "2-digit",
    day: "2-digit",
    month: "2-digit",
    year: "numeric",
    hour12: false,
    timeZone: "Asia/Ho_Chi_Minh",
  }).format(new Date(value));
}
```

Không cắt chuỗi ISO bằng `substring`, vì cách đó dễ hiển thị sai khi timezone thiết bị
không phải Việt Nam.

## 10. Checklist test FE

- Tạo booking không có conflict: không mở popup, chuyển sang màn hình thành công.
- Cùng chi nhánh và cách đúng 30 phút: mở popup, hiển thị `Cùng chi nhánh`.
- Khác chi nhánh và cách dưới 30 phút: mở popup, hiển thị `Khác chi nhánh`.
- Có nhiều conflict: hiển thị đầy đủ và gửi toàn bộ `bookingId`.
- Nhấn `Quay lại`: không gọi API lần hai.
- Nhấn `Vẫn đặt lịch`: request lần hai giữ nguyên dữ liệu và có conflict IDs.
- Backend trả conflict mới khi retry: cập nhật và mở lại popup.
- Double click nút xác nhận: chỉ gửi một request.
- Slot đã bị đặt: không hiển thị nút bỏ qua warning.
- Lỗi `401`, `403`, `400`, `500`: đi qua error handler hiện tại.
- Mobile: nội dung nhiều conflict có thể scroll, hai nút vẫn nhìn thấy.

## 11. Điều FE không cần làm

- Không tự tải toàn bộ lịch sử booking để tính conflict.
- Không tự quyết định ngưỡng 30 phút.
- Không tự phân biệt conflict theo customer, xe hoặc branch.
- Không tạo booking trước rồi mới hiển thị warning.
- Không lưu conflict IDs lâu dài; chúng chỉ dùng cho request xác nhận hiện tại.

Backend là nguồn dữ liệu chính xác cho warning và sẽ kiểm tra lại ngay trước khi tạo booking.
