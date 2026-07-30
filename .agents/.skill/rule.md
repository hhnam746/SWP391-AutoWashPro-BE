# AGENTS.md — Quy tắc bắt buộc cho AI Agent trong repo này

## 1. Không được đẩy `.agent` lên GitHub

- KHÔNG `git add`, `git commit`, hoặc `git push` bất kỳ filethư mục nào bên trong `.agent` lên bất kỳ remote nào (GitHub, GitLab...).
- `.agent` chứa cấu hình, context, cache, log nội bộ phục vụ AI agent — không phải mã nguồn dự án, không nên xuất hiện trong lịch sử git.
- Trước khi chạy `git commit`, luôn kiểm tra `git status`. Nếu thấy `.agent` nằm trong danh sách staged, phải bỏ ra bằng `git restore --staged .agent` trước khi commit tiếp.
- Nếu agent tự tạo file mới bên trong `.agent`, không tự ý thêm chúng vào commit dù user không nói gì thêm — mặc định coi `.agent` là vùng cấm với git.

## 2. Đảm bảo `.gitignore` có khai báo đúng

Đây là cách chặn triệt để nhất — kiểm tra file `.gitignore` ở root repo đã có dòng sau chưa, nếu chưa thì thêm vào

```gitignore
.agent
```

## 3. Nếu `.agent` đã lỡ bị commit trước đó

- Không tự động chạy lệnh xóa lịch sử khi chưa được user xác nhận rõ ràng.
- Chỉ cảnh báo user và đề xuất các bước sau để họ quyết định
  1. Bỏ tracking nhưng giữ file trên máy `git rm -r --cached .agent`
  2. Commit lại `git commit -m chore stop tracking .agent directory`
  3. Nếu `.agent` từng chứa thông tin nhạy cảm (token, credential...) và đã push lên remote, cần xóa khỏi lịch sử bằng công cụ như `git filter-repo` hoặc BFG Repo-Cleaner, và phải đổi lại các secret đã bị lộ.

## 4. Vì sao có quy tắc này

- `.agent` thường chứa state phiên làm việc, cache, có thể chứa thông tin nhạy cảm hoặc không có giá trị với người khác trong team.
- Đẩy `.agent` lên remote làm phình lịch sử git, gây nhiễu diff khi review, và có thể vô tình để lộ dữ liệu nội bộ.

## Checklist nhanh trước mỗi lần push

- [ ] `git status` không hiển thị bất kỳ file nào trong `.agent`
- [ ] `.gitignore` đã có `.agent`
- [ ] Không có secrettoken nào bên trong `.agent` từng bị commit trong lịch sử
