# Bắt đầu với WindowTab

**Một lần thiết lập để những lần đổi cửa sổ sau đó chỉ còn là một cú vuốt.**

Repo chính thức: https://github.com/nguyenquocan0101/WindowTab

## Máy của bạn cần gì?

- Windows 11 x64.
- Touchpad hỗ trợ thao tác ba ngón và cho gán **Custom shortcut** trong **Advanced gestures**. Precision Touchpad là trường hợp phù hợp nhất; khả năng này vẫn tùy driver của máy.
- Ít nhất hai cửa sổ ứng dụng đang mở, không thu nhỏ, trên cùng màn hình để thử chuyển.

Gói tải sẵn đã kèm .NET runtime. Bạn không cần cài SDK hoặc tự biên dịch.

## 1. Tải và chạy

1. Tải `WindowTab-win-x64.zip` từ [GitHub Releases chính thức](https://github.com/nguyenquocan0101/WindowTab/releases/latest).
2. **Giải nén toàn bộ ZIP** vào một thư mục cố định, ví dụ `C:\Apps\WindowTab`. Không chạy trực tiếp bên trong cửa sổ xem ZIP.
3. Mở `Start-WindowTab.cmd` để chạy app và mở Cài đặt ngay lần đầu. Bạn cũng có thể chạy `WindowTab.exe`, sau đó nhấp phải logo ở khay hệ thống và chọn **Cài đặt…**. Nếu icon nằm trong vùng ẩn, mở mũi tên khay hệ thống để tìm nó.

Chỉ một phiên WindowTab chạy tại một thời điểm. Nếu mở file lần nữa mà không thấy cửa sổ mới, hãy dùng icon đang có ở khay hệ thống.

## 2. Kiểm tra hai phím tắt

Trong tab **Cử chỉ**, bản cài mới dùng:

| Hướng vuốt | Tổ hợp |
|---|---|
| Sang trái | `Ctrl + Alt + Shift + F8` |
| Sang phải | `Ctrl + Alt + Shift + F10` |

Bạn có thể bấm vào ô và nhấn tổ hợp mới. Hai hướng phải khác nhau. Nếu một tổ hợp đã được app khác sử dụng, WindowTab sẽ báo lỗi khi lưu; chọn tổ hợp khác rồi ghi đúng tổ hợp đó trong Windows.

Nếu đã từng dùng WindowTab, app giữ cấu hình cũ của bạn. Luôn lấy **hai tổ hợp đang hiển thị trong app** làm chuẩn.

## 3. Gán vuốt trong Windows

**Giữ cửa sổ Cài đặt của WindowTab mở trong lúc ghi phím ở Windows.** Khi cửa sổ này mở, WindowTab tạm nhả phím tắt để Windows ghi lại được tổ hợp.

1. Bấm **Mở cài đặt Touchpad** trong WindowTab.
2. Trong Windows, đi tới **Bluetooth & devices → Touchpad → Advanced gestures**. Tên và vị trí có thể khác đôi chút theo phiên bản Windows/driver.
3. Ở **Swipe left**, chọn **Custom shortcut**.
4. Bấm **Start recording**, nhấn tổ hợp vuốt trái đang hiển thị trong WindowTab, rồi bấm **Stop recording**. Kiểm tra Windows đã hiện đủ các phím.
5. Làm tương tự với **Swipe right** và tổ hợp vuốt phải.
6. Trở về WindowTab. Nếu có thay đổi, bấm **Lưu thay đổi**; nếu không, đóng bằng **Hủy** hoặc nút đóng cửa sổ.

**Đóng Cài đặt của WindowTab trước khi thử vuốt.** Phím chuyển cửa sổ được khôi phục khi bạn đóng cửa sổ này.

Không cần chọn chức năng chuyển ứng dụng mặc định cho hai hướng đó nữa: Windows sẽ gửi phím tắt đã ghi cho WindowTab.

## 4. Thử cảm giác mới

Mở hai hoặc ba cửa sổ ứng dụng trên cùng màn hình, để chúng không bị thu nhỏ.

- **Vuốt trái:** chuyển tới cửa sổ tiếp theo.
- **Vuốt phải:** quay về cửa sổ trước.
- **Tới đầu/cuối danh sách:** preview dịch khoảng 20% rồi bật trở lại. Đây là dấu hiệu đã chạm biên.
- **Taskbar:** đứng yên trong quá trình chuyển cảnh.

Danh sách ban đầu bắt đầu từ cửa sổ đang dùng. Vì vậy, ngay sau khi chạy app, vuốt phải có thể chỉ bật lại ở biên; hãy vuốt trái để đi tiếp trước.

Đối tượng chuyển là **cửa sổ ứng dụng** như trình duyệt, editor, Discord hoặc File Explorer. Các tab nằm bên trong một cửa sổ trình duyệt vẫn do trình duyệt quản lý.

## 5. Chỉnh cho vừa tay

Tab **Chuyển động** có nút **Xem thử**, ô nhập và thanh kéo được đồng bộ.

| Cảm giác | Thời lượng gợi ý | Chống lặp gợi ý |
|---|---|---|
| Gọn, phản hồi nhanh | 220–300 ms | 100–150 ms |
| Chậm hơn, nhìn rõ ba giai đoạn | 500–600 ms | 200–250 ms |

Đây là gợi ý để bắt đầu. Bạn có thể chọn thời lượng 100–600 ms và chống lặp 0–1000 ms.

Trong tab **Ứng dụng**:

- Bật **Khởi động cùng Windows** nếu muốn app sẵn sàng khi đăng nhập. Hãy giữ thư mục app ở vị trí cố định.
- **Cho phép chuyển tới Desktop** mặc định tắt. Bật tùy chọn này nếu muốn thêm Desktop ở cuối danh sách.

Nút **Hủy** bỏ những chỉnh sửa chưa lưu. Các cài đặt đã lưu được giữ qua những lần mở app và cập nhật phiên bản.

## Khi cần xử lý nhanh

### Vuốt không chuyển hoặc chỉ nghe tiếng báo

1. Đóng Cài đặt của WindowTab và kiểm tra app không bị **Tạm dừng gesture** ở menu khay hệ thống.
2. Kiểm tra hai ô Custom shortcut trong Windows đã có giá trị đầy đủ và trùng với WindowTab.
3. Nếu cần, mở lại Cài đặt WindowTab, ghi lại phím trong Windows rồi đóng Cài đặt trước khi thử.
4. Thử nhấn trực tiếp tổ hợp trên bàn phím sau khi đóng Cài đặt. Nếu bàn phím hoạt động mà vuốt chưa hoạt động, kiểm tra lại mapping/driver touchpad.
5. Kiểm tra có ít nhất hai cửa sổ hợp lệ, không thu nhỏ, trên cùng màn hình.

### Vuốt sang một phía chỉ bật trở lại

Bạn có thể đang ở đầu/cuối danh sách. Thử đi về phía còn lại. WindowTab không quay vòng từ cửa sổ cuối về đầu.

### Một số cửa sổ không chuyển được

Windows có thể từ chối preview hoặc chuyển focus đối với cửa sổ được bảo vệ hay chạy ở mức quyền khác. WindowTab sẽ dùng đường chuyển dự phòng hoặc trả preview về cửa sổ đang dùng khi không lấy được focus.

### Không có Advanced gestures hoặc Custom shortcut

Driver touchpad của bạn có thể không cung cấp tính năng đó. WindowTab chưa có driver nhận dạng ba ngón riêng. Bạn vẫn có thể kiểm tra chức năng chuyển cửa sổ bằng hai phím tắt trên bàn phím.

Tham khảo thêm [hướng dẫn cử chỉ của Microsoft](https://support.microsoft.com/en-us/windows/hardware/input-devices/touch-gestures-for-windows).

## Cập nhật và gỡ app

**Cập nhật:** thoát WindowTab từ khay hệ thống, giải nén bản mới vào thư mục app rồi chạy lại. Cấu hình riêng nằm ở `%LOCALAPPDATA%\WindowTab\settings.json` nên được giữ lại. Nếu đổi thư mục app, mở bản mới để cập nhật đường dẫn khởi động cùng Windows khi tùy chọn đó đang bật.

**Gỡ:** tắt **Khởi động cùng Windows**, thoát app rồi xóa thư mục đã giải nén. Nếu muốn dọn cả cấu hình/nhật ký, chúng nằm trong `%LOCALAPPDATA%\WindowTab`.

## Nguồn tải và kiểm tra tệp

Bản v0.1.0 chưa có chữ ký số. Chỉ lấy gói từ repo/Releases chính thức. Mỗi release có `SHA256SUMS.txt`; bạn có thể đối chiếu giá trị bằng PowerShell:

```powershell
Get-FileHash .\WindowTab-win-x64.zip -Algorithm SHA256
```

Muốn báo lỗi hoặc góp ý, mở [GitHub Issues](https://github.com/nguyenquocan0101/WindowTab/issues) và ghi phiên bản Windows, model touchpad, phiên bản WindowTab, thao tác đã thử và kết quả mong đợi. Hãy kiểm tra và lược bỏ thông tin riêng trước khi đính kèm bất kỳ tệp nào.
