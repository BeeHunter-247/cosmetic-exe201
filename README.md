Cosmetics Shop API
Đây là dự án Web API cho hệ thống quản lý bán hàng mỹ phẩm, hỗ trợ các chức năng quản lý đơn hàng, sản phẩm, người dùng, affiliate, thanh toán, v.v.

Mục lục
Giới thiệu
Cấu trúc dự án
Yêu cầu hệ thống
Cài đặt & Chạy dự án
Các chức năng chính
Thông tin cấu hình
Liên hệ
Giới thiệu
Dự án xây dựng hệ thống API cho cửa hàng mỹ phẩm, hỗ trợ:

Quản lý sản phẩm, danh mục, thương hiệu
Quản lý đơn hàng, chi tiết đơn hàng
Quản lý người dùng, phân quyền (Admin, Affiliate, Customer)
Hệ thống Affiliate (hoa hồng, rút tiền, thống kê)
Tích hợp thanh toán PayOS
Xác thực JWT, gửi OTP qua email
Cấu trúc dự án
Yêu cầu hệ thống
.NET 7 trở lên
SQL Server (cấu hình trong appsettings.json)
Visual Studio 2022 hoặc VS Code
Cài đặt & Chạy dự án
Clone source code:

Cấu hình chuỗi kết nối DB:

Sửa appsettings.json phần "ConnectionStrings:DefaultConnection"
Khởi tạo database:

Chạy lệnh migration nếu có (hoặc import DB mẫu)
Restore package & build:

Chạy ứng dụng:

API mặc định chạy ở https://localhost:7191 hoặc http://localhost:5192
Các chức năng chính
Quản lý sản phẩm, danh mục, thương hiệu
Đặt hàng, cập nhật trạng thái đơn hàng
Quản lý người dùng, phân quyền
Affiliate:
Đăng ký affiliate
Tạo link affiliate, thống kê click/hoa hồng
Rút tiền, quản lý trạng thái rút tiền
Thanh toán PayOS
Gửi OTP xác thực email
Swagger UI: Tự động sinh tài liệu API
Thông tin cấu hình
appsettings.json chứa:
Chuỗi kết nối DB
Thông tin JWT
Cloudinary (nếu dùng upload ảnh)
PayOS (tích hợp thanh toán)
Liên hệ
Tác giả: [Trần Ngọc Long & Nguyễn Phi Hùng & Phạm Quốc Duy]
Email: [ngoclong24072003@gmail.com]
Facebook: [[Link Facebook](https://www.facebook.com/long.tran.587650)]
Để biết chi tiết API, hãy chạy dự án và truy cập /swagger để xem tài liệu Swagger UI.
