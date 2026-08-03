Payment Feature Brief

A. Back-end

1. Payment Gateway

Tích hợp cổng thanh toán cho booking.

Nhận và xử lý thông tin thanh toán từ payment gateway.

Trả về các thông tin cần thiết cho Front-end, bao gồm:

QR thanh toán.

Số tiền thực tế cần thanh toán.

Mã giao dịch.

Trạng thái thanh toán.

Thời gian hết hạn thanh toán.

Hỗ trợ xử lý hoàn tiền nếu payment gateway được chọn có hỗ trợ refund.

2. Booking Payment

Ghi nhận chi tiết giao dịch nhận tiền từ người dùng.

Lưu lịch sử thanh toán của từng booking.

Trả response chứa đầy đủ thông tin:

Số tiền booking ban đầu.

Số tiền giảm giá.

Số tiền thực tế đã thanh toán.

Phương thức thanh toán.

Trạng thái giao dịch.

Thời gian thanh toán.

Payment transaction ID.

3. Refund

Xử lý các trường hợp hoàn tiền:

Chi nhánh gặp sự cố và không thể phục vụ.

Người dùng hủy booking trước thời hạn cho phép.

Admin hủy booking.

Thanh toán thành công nhưng booking không được tạo.

Giao dịch bị trừ tiền nhiều lần.

Booking bị hủy do lỗi hệ thống.

Thông tin refund cần lưu:

Số tiền hoàn.

Lý do hoàn tiền.

Người thực hiện hoàn tiền.

Thời gian hoàn tiền.

Refund transaction ID.

Trạng thái hoàn tiền.

Booking ID.

Payment transaction ID ban đầu.

4. Early Check-in

Kiểm tra người dùng có thể check-in sớm hay không.

Chỉ cho phép check-in sớm khi:

Slot trước đó đang trống.

Chi nhánh có khả năng phục vụ.

Không ảnh hưởng đến booking khác.

Nếu không thể check-in sớm:

Giữ nguyên giờ booking ban đầu.

Trả về thông báo cụ thể cho người dùng.

Không tự động thay đổi trạng thái booking.

B. Front-end

1. Handle lỗi từ Back-end

Không sử dụng thông báo lỗi fix cứng trên giao diện.

Hiển thị message do Back-end trả về.

Handle lỗi theo:

HTTP status code.

Error code.

Error message.

Validation errors.

Có fallback message khi Back-end không trả về nội dung lỗi phù hợp.

2. Booking Detail Popup trên trang Admin

Hiển thị đầy đủ các thông tin:

Booking ID.

Tên khách hàng.

Số điện thoại.

Biển số xe.

Chi nhánh.

Dịch vụ.

Ngày booking.

Giờ bắt đầu.

Giờ kết thúc.

Giá dịch vụ.

Số tiền giảm giá.

Số tiền thực tế cần thanh toán.

Số tiền đã thanh toán.

Trạng thái booking.

Trạng thái thanh toán.

Phương thức thanh toán.

Thời gian thanh toán.

3. Payment Gateway

Front-end xử lý dữ liệu payment gateway do Back-end trả về:

Hiển thị QR thanh toán.

Hiển thị số tiền thực tế.

Hiển thị nội dung chuyển khoản.

Hiển thị mã giao dịch.

Hiển thị thời gian hết hạn.

Kiểm tra trạng thái thanh toán.

Chuyển trạng thái giao diện khi thanh toán thành công, thất bại hoặc hết hạn.

4. Dynamic Configuration

Loại bỏ các giá trị đang fix cứng trên giao diện, ví dụ:

Chỉ được hủy booking trước tối đa 1 giờ.

Thời gian hết hạn thanh toán.

Phần trăm hoàn tiền.

Số ngày được đặt lịch trước.

Thời gian cho phép check-in sớm.

Thời gian hoạt động của chi nhánh.

Các giá trị này phải được lấy từ Back-end hoặc system configuration.