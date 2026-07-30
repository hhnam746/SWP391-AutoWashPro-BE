---
name: code-review-query-clean
description: >
  Review code (dac biet code co truy van du lieu - LINQ/EF Core/SQL builder)
  de kiem tra code co "clean" khong va query da duoc tach bach, toi uu hay chua.
  Dung skill nay bat cu khi nao user yeu cau "review code", "check code sach chua",
  "tach query", hoac noi toi viec gop Where/FirstOrDefault/Select trong cung mot dong.
  Muc tieu cot loi - query don gian, moi query mot doan rieng biet, khong gop chung
  voi thao tac thuc thi (FirstOrDefault, ToList, Single, Count...), de code de doc,
  de review va de viet unit test.
---

# Code Review: Tách Query & Clean Code

Skill này dùng để review một đoạn code (thường là C#/.NET với LINQ, EF Core, hoặc query builder tương tự) theo đúng convention mà user đặt ra: **query phải được xây dựng ở một dòng/đoạn riêng, thao tác thực thi (FirstOrDefault, ToList, Single, Any, Count...) phải nằm ở một dòng/đoạn khác**. Không gộp chung.

## Quy trình review (làm theo đúng thứ tự)

1. Đọc toàn bộ đoạn code user cung cấp (không đoán, không review code không được đưa ra).
2. Xác định từng câu truy vấn dữ liệu (LINQ to Entities, EF Core, Dapper, raw SQL builder...).
3. Với mỗi câu truy vấn, áp dụng **Checklist tách query** bên dưới.
4. Áp dụng thêm **Checklist clean code chung**.
5. Xuất kết quả theo đúng **Output format** bên dưới — không thêm lời dẫn dài dòng, không tự ý sửa code nếu user không yêu cầu sửa.

## Checklist tách query (tiêu chí chính, ưu tiên số 1)

Vi phạm điển hình cần bắt lỗi:

```csharp
// ❌ SAI: gộp query + FirstOrDefault trong 1 dòng
var user = _context.Users.Where(u => u.Id == id && u.IsActive).FirstOrDefault();

// ❌ SAI: gộp nhiều điều kiện phức tạp + Select + FirstOrDefault cùng lúc
var dto = _context.Orders
    .Where(o => o.CustomerId == customerId && o.Status == OrderStatus.Paid)
    .Select(o => new OrderDto { Id = o.Id, Total = o.Total })
    .FirstOrDefault();
```

Sửa đúng theo convention:

```csharp
// ✅ ĐÚNG: query nằm riêng 1 đoạn, có tên biến rõ nghĩa
var activeUserQuery = _context.Users
    .Where(u => u.Id == id && u.IsActive);

// ✅ ĐÚNG: thực thi nằm ở đoạn khác
var user = activeUserQuery.FirstOrDefault();
```

```csharp
// ✅ ĐÚNG: query build riêng
var paidOrderQuery = _context.Orders
    .Where(o => o.CustomerId == customerId && o.Status == OrderStatus.Paid)
    .Select(o => new OrderDto { Id = o.Id, Total = o.Total });

// ✅ ĐÚNG: FirstOrDefault tách riêng
var dto = paidOrderQuery.FirstOrDefault();
```

Khi review, với mỗi query trong code cần trả lời các câu hỏi sau:

- [ ] Query (Where/Select/OrderBy/Join...) có được gán vào một biến riêng (IQueryable/IEnumerable) trước khi thực thi không?
- [ ] Thao tác thực thi (FirstOrDefault, SingleOrDefault, ToList, ToListAsync, Any, Count, Sum...) có nằm ở dòng/đoạn riêng, KHÔNG chain trực tiếp sau Where/Select không?
- [ ] Nếu có nhiều điều kiện lọc phức tạp, các điều kiện có được tách nhỏ (biến trung gian hoặc method riêng như `bool IsPaidOrder(Order o)`) thay vì viết một biểu thức lambda dài, lồng nhau không?
- [ ] Query có bị lồng bên trong query khác (ví dụ Where có chứa sub-query phức tạp) mà lẽ ra nên tách thành query riêng hoặc method riêng không?
- [ ] Tên biến chứa query có mô tả rõ mục đích không (`activeUserQuery` tốt hơn `q` hay `result`)?
- [ ] Vì query đã tách riêng, có thể unit test/mock phần build query độc lập với phần thực thi không? Nếu không tách được, đây là lỗi cần nêu.

## Checklist clean code chung (tiêu chí phụ)

- [ ] Method chỉ làm một việc (không vừa build query, vừa xử lý business logic, vừa map DTO trong cùng 1 khối không phân đoạn).
- [ ] Không có magic number/string trong điều kiện query (nên dùng const/enum).
- [ ] Không lặp lại cùng một điều kiện Where ở nhiều nơi (nên trích thành extension method hoặc specification).
- [ ] Đặt tên biến/method rõ nghĩa, đúng convention của codebase.
- [ ] Không có logic thừa, dead code, hoặc query không dùng tới kết quả.
- [ ] Với EF Core: cảnh báo nếu thấy nguy cơ N+1 query (gọi query trong vòng lặp) hoặc thiếu `.AsNoTracking()` cho query chỉ đọc.

## Output format

Luôn trả lời theo cấu trúc sau (markdown), viết bằng tiếng Việt:

````markdown
## Tóm tắt

[1-2 câu: code đã clean/tách query tốt chưa, tổng số vấn đề tìm thấy]

## Vấn đề phát hiện

### [Mức độ: Cao/Trung bình/Thấp] - [Tên vấn đề ngắn gọn]

- Vị trí: [tên method / số dòng nếu có]
- Vấn đề: [giải thích ngắn gọn, đối chiếu đúng tiêu chí nào ở checklist bị vi phạm]
- Đề xuất sửa:

```csharp
// code gợi ý sửa, tuân thủ convention tách query
```
````

[lặp lại cho từng vấn đề]

## Gợi ý cho unit test

[Chỉ ra phần nào sau khi tách query sẽ dễ viết unit test hơn, ví dụ: có thể test riêng phần build query trả về đúng điều kiện lọc, và test riêng phần xử lý kết quả sau FirstOrDefault]

```

Nếu code đã tuân thủ tốt toàn bộ checklist, nói rõ "Không phát hiện vi phạm" ở phần Vấn đề, không bịa vấn đề để có nội dung.

## Lưu ý khi review

- Không tự động sửa file của user trừ khi được yêu cầu rõ ràng — mặc định chỉ đưa ra review + đoạn code gợi ý.
- Nếu đoạn code không có query nào (chỉ là logic thường), vẫn áp dụng phần "Checklist clean code chung" và bỏ qua phần tách query.
- Nếu không chắc ngôn ngữ/framework (LINQ, Dapper, raw SQL...), hỏi lại user một câu duy nhất trước khi review thay vì đoán sai convention.
```
