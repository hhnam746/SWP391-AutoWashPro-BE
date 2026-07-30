---
name: commit-message-clean
description: >
  Huong dan viet commit message dung dinh dang, ro rang, clean theo chuan
  Conventional Commits. Dung skill nay bat cu khi nao user yeu cau "viet commit
  message", "commit dum tui", "check commit message", "gop code lai commit",
  hoac dang chuan bi git commit sau khi da sua code/review code xong. Muc tieu:
  commit message phai the hien dung loai thay doi (feat/fix/refactor...), ngan
  gon, mo ta dung "cai gi va tai sao" thay vi "lam nhu the nao", va moi commit
  chi nen chua MOT thay doi logic (atomic commit) de de doc lich su git, de
  revert, de review.
---

# Commit Message Clean

Skill này dùng để soạn hoặc review commit message theo chuẩn **Conventional Commits**, áp dụng nhất quán cho toàn bộ repo.

## Quy trình

1. Nếu có diff/danh sách file đã thay đổi, đọc kỹ để hiểu **bản chất thay đổi** (thêm tính năng, sửa bug, refactor, đổi docs...).
2. Kiểm tra xem các thay đổi trong 1 lần commit có phải **một thay đổi logic duy nhất** không (xem mục "Atomic commit" bên dưới). Nếu không, đề xuất tách thành nhiều commit.
3. Soạn commit message theo đúng cấu trúc bên dưới.
4. Nếu user chỉ đưa code đã sửa mà chưa nói rõ loại thay đổi, tự suy luận từ diff (ví dụ: file test mới → `test`, sửa lỗi logic → `fix`, thêm API mới → `feat`).

## Cấu trúc commit message (Conventional Commits)

```
<type>(<scope>): <subject>

<body>

<footer>
```

### 1. Type (bắt buộc)

| Type       | Khi dùng                                                                        |
| ---------- | ------------------------------------------------------------------------------- |
| `feat`     | Thêm tính năng mới cho user                                                     |
| `fix`      | Sửa bug                                                                         |
| `refactor` | Đổi cấu trúc code, không đổi behavior (ví dụ: tách query như skill review code) |
| `perf`     | Cải thiện performance                                                           |
| `test`     | Thêm/sửa unit test, không đổi code logic chính                                  |
| `docs`     | Chỉ sửa tài liệu, comment, README                                               |
| `style`    | Format code, thêm dấu chấm phẩy... không ảnh hưởng logic                        |
| `chore`    | Việc lặt vặt: update dependency, cấu hình build, CI...                          |
| `build`    | Thay đổi liên quan build system, package                                        |
| `ci`       | Thay đổi pipeline CI/CD                                                         |
| `revert`   | Revert lại một commit trước đó                                                  |

### 2. Scope (tùy chọn nhưng khuyến khích)

Tên module/layer bị ảnh hưởng, viết ngắn gọn, lowercase. Ví dụ: `(auth)`, `(order-service)`, `(user-repository)`.

Nếu thay đổi ảnh hưởng nhiều module không liên quan → dấu hiệu commit đang không atomic, nên tách nhỏ.

### 3. Subject (bắt buộc)

- Viết ở **thì mệnh lệnh** (imperative): "add", "fix", "remove" — không dùng "added", "fixed", "adds".
- Không viết hoa chữ đầu, không có dấu chấm cuối câu.
- Giới hạn khoảng **50-72 ký tự**.
- Mô tả **cái gì thay đổi**, không mô tả cách code hoạt động chi tiết.

```
✅ fix(order): prevent null reference when customer is deleted
❌ fix: Fixed a bug.
❌ update stuff
```

### 4. Body (tùy chọn, nên có với thay đổi phức tạp)

- Giải thích **tại sao** cần thay đổi này (bối cảnh, vấn đề gặp phải) — không lặp lại subject, không mô tả từng dòng code đã sửa (diff đã thể hiện điều đó rồi).
- Mỗi dòng wrap ở khoảng 72 ký tự.
- Có thể dùng bullet nếu thay đổi gồm nhiều điểm nhỏ liên quan tới nhau.

### 5. Footer (tùy chọn)

- `BREAKING CHANGE: <mô tả>` — khi thay đổi phá vỡ tương thích ngược, mô tả rõ cái gì bị ảnh hưởng và cách migrate.
- `Closes #123` / `Refs #123` — liên kết issue/ticket liên quan.

## Atomic commit — mỗi commit một mục đích

Áp dụng cùng tinh thần với skill tách query: **một commit chỉ nên chứa một thay đổi logic**.

Dấu hiệu commit KHÔNG atomic, cần tách:

- Vừa `feat` vừa `fix` trong cùng 1 commit không liên quan tới nhau.
- Vừa sửa logic vừa format lại toàn bộ file (nên tách riêng 1 commit `style` format, 1 commit chứa logic thật).
- Message phải dùng "and" để nối nhiều việc không liên quan: "fix bug and update docs and add test for another feature".
- Diff động tới nhiều module không liên quan tới cùng một mục tiêu.

Khi phát hiện, đề xuất user tách thành nhiều commit riêng, mỗi commit kèm message tương ứng.

## Ví dụ đầy đủ

```
feat(user-repository): add pagination support for GetUsers query

Trước đây GetUsers tra ve toan bo user trong 1 lan goi, gay cham khi
so luong user lon. Them tham so pageIndex/pageSize va tach rieng
buoc build query khoi buoc thuc thi de de viet unit test cho phan loc.

Refs #482
```

```
fix(auth): return 401 instead of 500 when token is expired

Middleware dang throw exception chua duoc catch khi token het han,
khien API tra ve 500 gay nham lan cho client. Bat exception rieng
cho truong hop token expired va tra ve 401 dung chuan.

Closes #501
```

```
refactor(order-service): split query building from FirstOrDefault execution

Khong doi logic nghiep vu, chi tach cac cau LINQ query ra khoi
thao tac thuc thi de de mock/test rieng phan dieu kien loc.
```

## Output format khi user nhờ soạn commit message

````markdown
## Commit message đề xuất

\```
<type>(<scope>): <subject>

<body nếu cần>

<footer nếu cần>
\```

## Ghi chú

[Nếu phát hiện commit không atomic: liệt kê nên tách thành mấy commit, mỗi commit làm gì]
[Nếu thiếu thông tin để xác định type/scope: nêu rõ đang giả định gì]
````

## Lưu ý

- Nếu không có đủ ngữ cảnh (không biết đây là feat hay fix), hỏi lại user một câu ngắn thay vì đoán bừa loại `type`.
- Không tự ý chạy `git commit` trừ khi user yêu cầu rõ ràng — mặc định chỉ đề xuất nội dung message.
- Giữ nhất quán type/scope đã dùng trước đó trong repo nếu có thể quan sát được từ lịch sử commit.
