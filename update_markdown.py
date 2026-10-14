# Script to update BaoCao_TotNghiep_ZoneMart.md with requested additions
import re

md_path = r"C:\Users\kayy0\.gemini\antigravity\brain\ff1a3665-6bfa-430b-8f81-e5f1fb5e7295\BaoCao_TotNghiep_ZoneMart.md"

with open(md_path, "r", encoding="utf-8") as f:
    content = f.read()

# 1. Update Title / Subtitle to reflect multi-model AI & SePay if needed
# 2. Update abbreviations table: add Gemini, Grok, Astra 6, SePay
abbr_target = """| 18 | **TMĐT** | Thương mại điện tử | Hoạt động kinh doanh mua bán trên internet |
| 19 | **UI / UX** | User Interface / User Experience | Giao diện người dùng / Trải nghiệm người dùng |"""

abbr_replacement = """| 18 | **TMĐT** | Thương mại điện tử | Hoạt động kinh doanh mua bán trên internet |
| 19 | **UI / UX** | User Interface / User Experience | Giao diện người dùng / Trải nghiệm người dùng |
| 20 | **Gemini** | Google Gemini Multimodal AI | Mô hình AI thị giác máy tính & phân tích hình ảnh |
| 21 | **Grok** | xAI Large Language Model | Mô hình AI kiểm duyệt văn bản & suy luận logic |
| 22 | **Astra 6** | Specialized Conversational AI | Mô hình AI tư vấn & Chatbot hỗ trợ đặt hàng |
| 23 | **SePay** | Payment Automation Gateway | Cổng thanh toán tự động qua Webhook biến động số dư |"""

content = content.replace(abbr_target, abbr_replacement)

# Update List of Figures for 10km to <= 3km and add Astra 6
content = content.replace(
    "- **Hình 2.6:** Sơ đồ Use Case Tìm kiếm và xem sản phẩm trong bán kính 10 km.",
    "- **Hình 2.6:** Sơ đồ Use Case Tìm kiếm và xem sản phẩm trong bán kính siêu cục bộ <= 3 km."
)
content = content.replace(
    "- **Hình 3.3:** Giao diện Tìm kiếm và Bộ lọc bán kính giao hàng 1 - 10 km trên bản đồ số.",
    "- **Hình 3.3:** Giao diện Tìm kiếm và Bộ lọc bán kính giao hàng siêu cục bộ <= 3 km trên bản đồ số."
)
content = content.replace(
    "- **Hình 2.23:** Biểu đồ tuần tự chức năng Đăng sản phẩm và Kiểm duyệt song song AI.",
    "- **Hình 2.23:** Biểu đồ tuần tự chức năng Đăng sản phẩm và Kiểm duyệt song song AI (Gemini & Grok)."
)
if "- **Hình 2.28:**" not in content:
    content = content.replace(
        "- **Hình 2.27:** Biểu đồ quan hệ thực thể tài liệu NoSQL MongoDB Collections.",
        "- **Hình 2.27:** Biểu đồ quan hệ thực thể tài liệu NoSQL MongoDB Collections.\n- **Hình 2.28:** Biểu đồ tuần tự Hội thoại tư vấn và tạo đơn qua Chatbot Astra 6."
    )

# Replace occurrences of radius 1-10km with <= 3km
content = re.sub(r'từ 1 km đến 10 km xung quanh vị trí của người mua, kết nối trực tiếp khách hàng với các tiểu thương, cửa hàng tiện lợi lân cận và đội ngũ tài xế tự do trong phạm vi 3 km',
                 r'trong phạm vi bán kính siêu cục bộ <= 3 km xung quanh vị trí của người mua, kết nối trực tiếp khách hàng với các tiểu thương, cửa hàng tiện lợi và đội ngũ tài xế lân cận',
                 content)

content = re.sub(r'bán kính siêu cục bộ 1 - 10 km trong thời gian 15 - 25 phút',
                 r'bán kính siêu cục bộ <= 3 km trong thời gian 15 - 25 phút',
                 content)

content = re.sub(r'bán kính 1 - 10 km so với vị trí GPS của người mua, đồng thời quét và điều phối tài xế hỏa tốc lân cận trong bán kính 3 km',
                 r'bán kính siêu cục bộ <= 3 km so với vị trí GPS của người mua, đồng bộ hoàn toàn với tầm quét điều phối tài xế hỏa tốc lân cận (<= 3 km)',
                 content)

content = re.sub(r'trong bán kính tối đa 10 km\.',
                 r'trong bán kính siêu cục bộ <= 3 km.',
                 content)

content = re.sub(r'giới hạn bán kính `\$maxDistance` từ 1 km đến 10 km theo chuẩn quốc tế WGS84\.',
                 r'giới hạn bán kính `$maxDistance` <= 3 km (3000 mét) theo chuẩn quốc tế WGS84.',
                 content)

content = re.sub(r'thể hiện bán kính phục vụ của từng gian hàng \(tối đa 10 km\) và ra-đa nhận đơn hỏa tốc của tài xế \(bán kính 3 km\)\.',
                 r'thể hiện bán kính phục vụ siêu cục bộ của từng gian hàng và ra-đa nhận đơn hỏa tốc của tài xế đều được tối ưu hóa trong phạm vi <= 3 km.',
                 content)

content = re.sub(r'tìm kiếm sản phẩm và gian hàng trong bán kính cho phép \(1 - 10 km\)\.',
                 r'tìm kiếm sản phẩm và gian hàng trong bán kính siêu cục bộ <= 3 km.',
                 content)

content = re.sub(r'B --> B2\["Tìm kiếm & Lọc bán kính 10 km"\]',
                 r'B --> B2["Tìm kiếm & Lọc bán kính <= 3 km"]',
                 content)

content = re.sub(r'tìm kiếm sản phẩm, gian hàng trong bán kính giao hàng tối đa 10 km,',
                 r'tìm kiếm sản phẩm, gian hàng trong bán kính giao hàng siêu cục bộ <= 3 km,',
                 content)

content = re.sub(r'phục vụ tính bán kính 10 km',
                 r'phục vụ tính bán kính <= 3 km',
                 content)

content = re.sub(r'Bộ lọc bán kính 1 - 10 km \(Hình 3\.5\):',
                 r'Bộ lọc bán kính siêu cục bộ <= 3 km (Hình 3.5):',
                 content)

content = re.sub(r'kéo thanh trượt bán kính từ 1 km đến 10 km',
                 r'kéo thanh trượt bán kính từ 0.5 km đến 3 km (tối đa <= 3 km)',
                 content)

content = re.sub(r'giao hàng hỏa tốc trong bán kính cục bộ 1 - 10 km,',
                 r'giao hàng hỏa tốc trong bán kính siêu cục bộ <= 3 km,',
                 content)

# Update Lời mở đầu AI section
old_lmd_ai = """2. **Trí tuệ nhân tạo kiểm duyệt đa phương thức (Google Gemini Multimodal AI):** Tự động phân tích hình ảnh và văn bản tên sản phẩm song song (parallel pipeline), đối soát chéo ngữ nghĩa nhằm ngăn chặn triệt để hàng giả, hàng cấm, vũ khí, chất kích thích hoặc nội dung nhạy cảm trước khi sản phẩm được đưa lên sàn.
3. **Xác thực sinh trắc học khuôn mặt (UniFace Face ID & MiniFASNet Anti-Spoofing):** Microservice Python FastAPI trích xuất vector đặc trưng khuôn mặt 512 chiều, kết hợp thuật toán phát hiện gian lận hình ảnh động (liveness detection), cho phép người dùng đăng nhập tức thì không cần mật khẩu.
4. **Tự động hóa thanh toán VietQR & Cảnh báo CSKH Telegram:** Sinh mã QR thanh toán động tích hợp nội dung chuyển khoản tự động, kết nối tín hiệu biến động số dư ngân hàng qua Webhook để xác nhận giao dịch tức thì; đồng thời tự động điều hướng phản ánh của khách hàng tới nhóm hỗ trợ qua Telegram Bot."""

new_lmd_ai = """2. **Hệ thống Trí tuệ nhân tạo đa mô hình (Google Gemini, xAI Grok & Astra 6):**
   - **Google Gemini:** Xử lý thị giác máy tính đa phương thức (Multimodal Vision), phân tích chuyên sâu hình ảnh sản phẩm thực tế.
   - **xAI Grok:** Đóng vai trò kiểm duyệt văn bản, thẩm định chính sách nội dung và phân tích logic ngữ nghĩa tên sản phẩm.
   - **Cơ chế đối soát chéo ảnh - tên:** Tự động phát hiện trường hợp bất đồng bộ dữ liệu. *Ví dụ: Người bán đăng tên sản phẩm là "Rau xanh hữu cơ" nhưng tải lên hình ảnh là "Miếng thịt bò tươi" -> Hệ thống phát hiện độ khớp ngữ nghĩa không hợp lệ, lập tức chặn mở bán tự động và chuyển về cho Quản lý / Admin duyệt tay thủ công.*
   - **Astra 6 (AI Tư vấn & Chatbot thông minh):** Trợ lý ảo hội thoại tự nhiên hỗ trợ khách hàng tư vấn món, gợi ý sản phẩm và lên đơn hàng tự động ngay trong khung chat.
3. **Xác thực sinh trắc học khuôn mặt (UniFace Face ID & MiniFASNet Anti-Spoofing):** Microservice Python FastAPI trích xuất vector đặc trưng khuôn mặt 512 chiều, kết hợp thuật toán phát hiện gian lận hình ảnh động (liveness detection), cho phép người dùng đăng nhập tức thì không cần mật khẩu.
4. **Tự động hóa thanh toán SePay, VietQR & Cảnh báo CSKH Telegram:** Sinh mã QR thanh toán động chuẩn VietQR kết hợp cổng thanh toán tự động **SePay** và Tingo Pay qua Webhook, tự động đối soát biến động số dư ngân hàng và chuyển trạng thái "Đã thanh toán" chỉ trong 1 - 3 giây; đồng thời tự động điều hướng phản ánh của khách hàng tới nhóm hỗ trợ qua Telegram Bot."""

content = content.replace(old_lmd_ai, new_lmd_ai)

# Update Section 1.6.4 AI description
old_ai_sec = """### 1.6.4 Trí tuệ nhân tạo kiểm duyệt nội dung đa phương thức (Google Gemini Multimodal API)
ZoneMart tích hợp trực tiếp mô hình ngôn ngữ lớn và thị giác máy tính tiên tiến nhất hiện nay là **Google Gemini**:
- **Pipeline kiểm duyệt song song (Parallel Pipeline):** Khi người bán gửi ảnh và tên sản phẩm, backend C# kích hoạt `Task.WhenAll` để thực thi đồng thời hai tác vụ:
  1. *Gemini Vision Inspector:* Phân tích hình ảnh nhị phân (base64 bytes), trích xuất danh mục vật thể nhận diện được (`DetectedItem`), kiểm tra ảnh có chứa yếu tố bạo lực, vũ khí, chất kích thích, cấm đoán hoặc 18+ hay không.
  2. *Kira Policy Inspector:* Sử dụng biểu thức chính quy (Regex) và phân tích ngữ nghĩa để kiểm tra tên sản phẩm có nằm trong danh mục từ khóa cấm hoặc các chuỗi ký tự rác/vô nghĩa (spam string) hay không.
- **Cơ chế đối soát chéo ngữ nghĩa (Cross-Check Consistency):** Hệ thống tính toán độ tương đồng giữa vật thể do Gemini Vision phát hiện và tên sản phẩm do người bán nhập. Nếu độ khớp dưới 70%, sản phẩm tự động chuyển vào trạng thái *Chờ Manager duyệt thủ công* thay vì mở bán ngay.
- **Chế tài tự động 3 cấp độ:** Trường hợp phát hiện hàng cấm nghiêm trọng, hệ thống tự động xóa bài đăng tức thời và tăng biến đếm vi phạm của người bán (`violation_count`), áp dụng các mức kỷ luật: cảnh cáo (1-5 lần), tạm đình chỉ 10 ngày (6-10 lần), hoặc tước quyền người bán vĩnh viễn (trên 10 lần)."""

new_ai_sec = """### 1.6.4 Hệ thống Trí tuệ nhân tạo đa mô hình (Google Gemini, xAI Grok, Astra 6)
Nhằm đảm bảo an toàn tuyệt đối cho hệ sinh thái thương mại điện tử và nâng cao trải nghiệm mua sắm, ZoneMart tích hợp phối hợp 3 mô hình trí tuệ nhân tạo chuyên biệt:

1. **Google Gemini (Multimodal Vision Inspector):**
   - Đảm nhiệm vai trò phân tích thị giác máy tính đối với hình ảnh sản phẩm do người bán tải lên.
   - Nhận diện chính xác thực thể trong ảnh (`DetectedItem`), phát hiện dấu hiệu mạo danh, ảnh chụp màn hình kém chất lượng, hình ảnh nhạy cảm 18+, vũ khí, chất cháy nổ hoặc các mặt hàng cấm lưu hành.

2. **xAI Grok (Semantic Policy & Consistency Judge):**
   - Đảm nhiệm việc thẩm định nội dung văn bản (tên sản phẩm, mô tả chi tiết, phân loại ngành hàng).
   - Áp dụng các bộ lọc chính sách nghiêm ngặt, phát hiện các thủ thuật lách luật (viết lái từ ngữ, chèn ký tự đặc biệt, spam từ khóa).
   - **Quy trình đối soát chéo ngữ nghĩa (Cross-check consistency) giữa ảnh và tên:**
     Grok so sánh ngữ nghĩa giữa vật thể được Gemini phát hiện trong ảnh và tên sản phẩm do người bán đặt.
     * **Ví dụ thực tế điển hình:** Người bán nhập tên sản phẩm là *"Rau xà lách sạch hữu cơ"* nhưng lại tải lên hình ảnh là *"Miếng thịt bò tươi"*. Khi đó, Gemini nhận diện vật thể là `Thịt bò / Meat`, trong khi Grok phân tích tên sản phẩm thuộc nhóm `Rau củ / Vegetable`. Hệ thống tính toán độ tương đồng ngữ nghĩa trả về 0% (dưới ngưỡng an toàn 70%). Sản phẩm bị gắn cờ nghi ngờ (`suspicious`), hệ thống chặn mở bán tự động và ngay lập tức gửi về cho Quản lý / Quản trị viên duyệt tay thủ công tại Bảng điều khiển quản trị.

3. **Astra 6 (Mô hình AI Tư vấn & Chatbot thông minh - Trợ lý ảo Kira):**
   - Mô hình đàm thoại thế hệ mới được tích hợp vào `AiChatController` và Widget Trợ lý ảo trên giao diện mua sắm.
   - Hỗ trợ khách hàng hỏi đáp tự nhiên bằng tiếng Việt: tìm kiếm các quán ăn/cửa hàng trong bán kính <= 3 km, gợi ý món ăn theo sở thích hoặc ngân sách, kiểm tra tình trạng còn hàng theo thời gian thực và hỗ trợ tạo đơn hàng hỏa tốc trực tiếp qua đoạn chat mà không cần thao tác bấm chọn phức tạp."""

content = content.replace(old_ai_sec, new_ai_sec)

# Update Section 1.6.7 Payment description
old_pay_sec = """### 1.6.7 Tự động hóa thanh toán VietQR & Hệ thống thông báo Telegram, Gmail
- **VietQR Dynamic Generator:** Sinh mã QR thanh toán động chứa thông tin số tài khoản thụ hưởng của hệ thống (TPBank), số tiền chính xác đến từng đồng và nội dung chuyển khoản mã hóa (memo chứa mã đơn hàng duy nhất dạng `ZMxxxxxx`).
- **Webhook biến động số dư tự động:** Khi khách hàng quét mã thanh toán qua bất kỳ ứng dụng ngân hàng nào tại Việt Nam, máy chủ ngân hàng gửi tín hiệu Webhook về API ZoneMart; hệ thống tự động đối soát nội dung và chuyển trạng thái đơn hàng sang *Đã thanh toán (Paid)* trong chưa đầy 3 giây mà không cần con người can thiệp.
- **Hệ thống thông báo Telegram Bot & Gmail SMTP:**
  - Kết nối trực tiếp với nhóm nghiệp vụ qua Telegram Bot API để thông báo khi có hồ sơ đối tác đăng ký mới, đơn hàng hỏa tốc cần can thiệp hoặc phiếu hỗ trợ khiếu nại (Support Ticket).
  - Gửi mã xác thực OTP khôi phục mật khẩu (thời hạn 120 giây) và email thông báo kết quả thẩm định KYC chính thức qua giao thức SMTP (MailKit)."""

new_pay_sec = """### 1.6.7 Tự động hóa thanh toán SePay, VietQR & Hệ thống thông báo Telegram, Gmail
- **Tích hợp cổng thanh toán tự động SePay & Tingo Pay (TPBank VietQR):**
  - **Sinh mã VietQR động:** Hệ thống sinh mã QR thanh toán theo chuẩn Napas hiển thị kèm số tài khoản thụ hưởng, số tiền chính xác đến từng đồng và cú pháp nội dung chuyển khoản mã hóa (memo dạng `ZMxxxxxx`).
  - **Xử lý Webhook SePay tức thời:** Cổng thanh toán **SePay** lắng nghe trực tiếp biến động số dư của tài khoản ngân hàng liên kết (TPBank). Khi khách hàng quét mã chuyển khoản thành công từ bất kỳ ứng dụng ngân hàng hoặc ví điện tử nào, SePay gửi ngay một tín hiệu Webhook (POST) chứa dữ liệu giao dịch về máy chủ ZoneMart (`/api/orders/webhook-payment`).
  - **Khớp lệnh tự động trong 1 - 3 giây:** Backend phân tích mã đơn hàng trong nội dung chuyển tiền, kiểm tra số tiền khớp 100% với đơn hàng và tự động cập nhật trạng thái đơn thành *Đã thanh toán (Paid)*, bóc tách đơn con và điều phối tài xế mà không cần bất kỳ sự can thiệp thủ công nào của thu ngân.
- **Hệ thống thông báo Telegram Bot & Gmail SMTP:**
  - Kết nối trực tiếp với nhóm nghiệp vụ qua Telegram Bot API để thông báo khi có đơn hàng thanh toán thành công qua SePay, hồ sơ đối tác đăng ký mới hoặc phiếu khiếu nại (Support Ticket).
  - Gửi mã xác thực OTP khôi phục mật khẩu (thời hạn 120 giây) và email thông báo kết quả thẩm định KYC chính thức qua giao thức SMTP (MailKit)."""

content = content.replace(old_pay_sec, new_pay_sec)

# Update Actors Table (Bảng 2.3)
old_actors = """| 7 | **Hệ thống AI kiểm duyệt (Google Gemini)** | Hệ thống bên ngoài | Tác nhân hệ thống bên ngoài. Đối chiếu hình ảnh với tên sản phẩm, phát hiện hàng cấm, nội dung vi phạm khi Người bán đăng hoặc sửa sản phẩm. |
| 8 | **Dịch vụ AI nhận diện khuôn mặt (UniFace)** | Hệ thống bên ngoài | Tác nhân hệ thống bên ngoài (face_service). Trích xuất vector đặc trưng khuôn mặt phục vụ đăng ký và đăng nhập bằng Face ID. |
| 9 | **Cổng thanh toán (Tingo Pay - TPBank)** | Hệ thống bên ngoài | Tác nhân hệ thống bên ngoài. Gửi tín hiệu biến động số dư (webhook) để hệ thống tự động xác nhận giao dịch chuyển khoản QR. |
| 10 | **Dịch vụ Email, Telegram** | Hệ thống bên ngoài | Tác nhân hệ thống bên ngoài. Gửi mã OTP, thông báo kết quả duyệt hồ sơ, thông báo chế tài và chuyển yêu cầu hỗ trợ tới bộ phận CSKH. |"""

new_actors = """| 7 | **Hệ thống AI kiểm duyệt (Google Gemini & xAI Grok)** | Hệ thống bên ngoài | Phân tích ảnh (Gemini) và kiểm duyệt văn bản (Grok), đối soát chéo tên và ảnh (ví dụ: tên rau nhưng ảnh thịt sẽ gắn cờ chuyển duyệt tay). |
| 8 | **Hệ thống AI Tư vấn & Chatbot (Astra 6)** | Hệ thống bên ngoài | Trợ lý ảo thông minh tương tác đàm thoại tự nhiên, tư vấn sản phẩm và hỗ trợ tạo đơn hàng hỏa tốc cho khách hàng. |
| 9 | **Dịch vụ AI nhận diện khuôn mặt (UniFace)** | Hệ thống bên ngoài | Trích xuất vector đặc trưng khuôn mặt 512 chiều kết hợp MiniFASNet chống giả mạo phục vụ đăng nhập Face ID. |
| 10 | **Cổng thanh toán tự động (SePay & Tingo Pay - VietQR)** | Hệ thống bên ngoài | Lắng nghe biến động số dư ngân hàng và gửi tín hiệu Webhook để hệ thống tự động xác nhận thanh toán đơn hàng trong 1-3 giây. |
| 11 | **Dịch vụ Email, Telegram** | Hệ thống bên ngoài | Gửi mã OTP, thông báo kết quả duyệt hồ sơ, thông báo đơn hàng và chuyển yêu cầu hỗ trợ tới bộ phận CSKH. |"""

content = content.replace(old_actors, new_actors)

# Update Use Case 2.2.10 & 2.2.16
content = content.replace(
    "Mỗi lần thêm hoặc sửa, nội dung và hình ảnh sản phẩm bắt buộc được kiểm duyệt bằng hệ thống AI (Google Gemini) trước khi mở bán.",
    "Mỗi lần thêm hoặc sửa, nội dung và hình ảnh sản phẩm bắt buộc được kiểm duyệt bằng hệ thống AI kết hợp (Google Gemini & xAI Grok). Nếu phát hiện bất đồng bộ ngữ nghĩa (ví dụ: tên rau nhưng ảnh thịt), hệ thống sẽ chuyển về cho Admin duyệt tay."
)
content = content.replace(
    "Hệ thống AI tự động quét nội dung và hình ảnh; phát hiện vi phạm thì xử lý ngay theo số lần vi phạm; nếu độ khớp ảnh - tên dưới 70% thì chuyển vào danh sách chờ Quản lý duyệt.",
    "Hệ thống AI (Gemini & Grok) tự động quét nội dung và hình ảnh; phát hiện vi phạm hàng cấm thì xử lý ngay theo số lần vi phạm; nếu độ khớp ảnh - tên dưới 70% (ví dụ: tên rau nhưng ảnh thịt) thì chuyển vào danh sách chờ Quản lý duyệt tay."
)

# Update Specifications 2.3.6, 2.3.7, 2.3.11
content = content.replace(
    "| **Tác nhân** | Người mua, Cổng thanh toán (Tingo Pay - TPBank Webhook) |",
    "| **Tác nhân** | Người mua, Cổng thanh toán (SePay & Tingo Pay - VietQR) |"
)
content = content.replace(
    "- Cổng thanh toán gửi Webhook biến động số dư; hệ thống tự động đối soát và đánh dấu đơn hàng *Đã thanh toán (Paid)*;",
    "- Cổng thanh toán SePay gửi Webhook biến động số dư tài khoản ngân hàng trong 1-3 giây; hệ thống tự động đối soát và đánh dấu đơn hàng *Đã thanh toán (Paid)*;"
)

content = content.replace(
    "| **Tác nhân** | Người bán, Hệ thống AI kiểm duyệt (Google Gemini) |",
    "| **Tác nhân** | Người bán, Hệ thống AI kiểm duyệt (Google Gemini & xAI Grok) |"
)
content = content.replace(
    "4. Hệ thống gửi đồng thời tên và ảnh sản phẩm tới dịch vụ AI Gemini kiểm duyệt song song.<br/>5. AI trả về chỉ số an toàn nội dung và tỷ lệ khớp giữa ảnh với tên sản phẩm.<br/>6. Nếu nội dung an toàn và độ khớp >= 70%, hệ thống chuyển sản phẩm sang trạng thái *Đang bán* và đồng bộ lên sàn mua sắm ngay lập tức.<br/>7. Use case kết thúc.",
    "4. Hệ thống gửi đồng thời tên và ảnh sản phẩm tới dịch vụ AI Gemini (quét hình ảnh) và xAI Grok (quét văn bản & đối soát logic).<br/>5. AI trả về chỉ số an toàn nội dung và tỷ lệ khớp giữa ảnh với tên sản phẩm.<br/>6. Nếu nội dung an toàn và độ khớp >= 70%, hệ thống chuyển sản phẩm sang trạng thái *Đang bán* và đồng bộ lên sàn mua sắm ngay lập tức.<br/>7. Use case kết thúc."
)
content = content.replace(
    "**A1. Độ khớp ảnh - tên dưới 70% hoặc AI nghi ngờ:** Hệ thống đưa sản phẩm vào danh sách *Chờ Manager duyệt thủ công* và thông báo cho người bán.",
    "**A1. Độ khớp ảnh - tên dưới 70% (Ví dụ: tên rau nhưng ảnh thịt):** Hệ thống đưa sản phẩm vào danh sách *Chờ Manager duyệt thủ công* (gửi về cho Admin duyệt tay) và thông báo cho người bán."
)

# Update Sequence Diagram 2.4.3
old_seq_243 = """    Seller->>UI: Nhấn Đăng sản phẩm
    UI->>ProdCtrl: POST /api/products/create {name, price, imageBase64}
    ProdCtrl->>ModSvc: ModerateProductAsync(request)
    par Quét hình ảnh đa phương thức
        ModSvc->>Gemini: InspectImageWithGeminiAsync(imageBytes)
        Gemini-->>ModSvc: Trả về IsImageSafe, DetectedItem
    and Quét nội dung văn bản
        ModSvc->>ModSvc: JudgeContentWithKiraAsync (Regex từ cấm, spam)
    end
    ModSvc->>ModSvc: Đối soát chéo: DetectedItem vs ProductName (Ngưỡng 70%)
    ModSvc-->>ProdCtrl: Kết quả Moderation (IsApproved, MatchPercentage)

    alt Phát hiện hàng cấm / Từ khóa 18+
        ProdCtrl->>DB: Tăng violation_count của Seller
        ProdCtrl-->>UI: Báo lỗi vi phạm chính sách & Hủy đăng
    else Độ khớp ảnh - tên < 70% hoặc AI nghi ngờ
        ProdCtrl->>DB: Lưu sản phẩm với trạng thái "suspicious" (Chờ Manager duyệt)
        ProdCtrl-->>UI: Thông báo sản phẩm đã được gửi tới Ban quản lý duyệt
    else Hợp lệ hoàn toàn"""

new_seq_243 = """    Seller->>UI: Nhập tên (vd: "Rau cải sạch") nhưng chọn ảnh (vd: "Thịt bò"), nhấn Đăng
    UI->>ProdCtrl: POST /api/products/create {name: "Rau cải sạch", price, imageBase64}
    ProdCtrl->>ModSvc: ModerateProductAsync(request)
    par Quét hình ảnh bằng Google Gemini
        ModSvc->>Gemini: InspectImageWithGeminiAsync(imageBytes)
        Gemini-->>ModSvc: IsImageSafe=true, DetectedItem="Thịt bò tươi"
    and Thẩm định chính sách văn bản bằng xAI Grok
        ModSvc->>ModSvc: GrokContentJudge(ProductName: "Rau cải sạch")
        ModSvc-->>ModSvc: IsTextSafe=true, TextCategory="Rau củ"
    end
    ModSvc->>ModSvc: Đối soát chéo: Ảnh (Thịt bò) vs Tên (Rau cải) -> Tỷ lệ khớp = 0% (< 70%)
    ModSvc-->>ProdCtrl: Kết quả: Bất đồng bộ ảnh - tên, Flag: Suspicious

    alt Phát hiện hàng cấm / Từ khóa 18+
        ProdCtrl->>DB: Tăng violation_count của Seller
        ProdCtrl-->>UI: Báo lỗi vi phạm chính sách & Hủy đăng
    else Bất đồng bộ ảnh - tên (VD: tên rau ảnh thịt) hoặc AI nghi ngờ
        ProdCtrl->>DB: Lưu sản phẩm với trạng thái "suspicious"
        ProdCtrl-->>UI: Thông báo: Sản phẩm chuyển về hàng đợi cho Admin/Manager duyệt tay
    else Hợp lệ hoàn toàn"""

content = content.replace(old_seq_243, new_seq_243)

# Update Sequence Diagram 2.4.4 for SePay
content = content.replace(
    "participant TPBank as Tingo Pay / TPBank Webhook",
    "participant SePay as Cổng thanh toán SePay (Webhook TPBank)"
)
content = content.replace(
    "TPBank->>OrderCtrl: POST /api/orders/webhook-payment {memo, amount}",
    "SePay->>OrderCtrl: POST /api/orders/webhook-payment {gateway: 'SePay', memo: 'ZMxxxx', amount}"
)

# Add Sequence Diagram 2.4.8 for Astra 6 Chatbot
astra_seq_block = """### 2.4.8 Biểu đồ tuần tự Hội thoại tư vấn và tạo đơn qua Chatbot Astra 6

```mermaid
sequenceDiagram
    autonumber
    actor Customer as Khách hàng
    participant UI as Widget Trợ lý ảo Kira (Vue 3)
    participant AiCtrl as AiChatController (C# Backend)
    participant Astra as Mô hình AI Astra 6
    participant DB as MongoDB (Products & Orders)

    Customer->>UI: Nhập tin nhắn: "Tư vấn cho mình combo đồ ăn trưa dưới 50k gần đây"
    UI->>AiCtrl: POST /api/aichat/message {prompt, userLocation}
    AiCtrl->>DB: Quét các sản phẩm và cửa hàng có sẵn trong bán kính <= 3 km
    DB-->>AiCtrl: Danh sách sản phẩm lân cận
    AiCtrl->>Astra: Gửi prompt kèm context dữ liệu sản phẩm trong khu vực
    Astra->>Astra: Phân tích ngữ cảnh, lựa chọn món ngon và sinh câu trả lời tư vấn
    Astra-->>AiCtrl: Câu trả lời gợi ý kèm danh sách món (ID, Tên, Giá, Quán)
    AiCtrl-->>UI: Trả về tin nhắn gợi ý và card sản phẩm trực quan
    UI-->>Customer: Hiển thị gợi ý combo ăn trưa trong 3 km
    Customer->>UI: Bấm nút "Đặt ngay món này" trong cửa sổ chat
    UI->>AiCtrl: POST /api/aichat/quick-order {productId, quantity: 1}
    AiCtrl->>DB: Tự động khởi tạo giỏ hàng và đơn hàng hỏa tốc cho khách
    AiCtrl-->>UI: Xác nhận tạo đơn thành công kèm mã QR SePay thanh toán
    UI-->>Customer: Hiển thị thông báo đặt đơn thành công ngay tại khung chat
```
"""

if "### 2.4.8 Biểu đồ tuần tự Hội thoại tư vấn và tạo đơn qua Chatbot Astra 6" not in content:
    content = content.replace(
        "## 2.5 Thiết kế cơ sở dữ liệu (NoSQL MongoDB Collections)",
        astra_seq_block + "\n## 2.5 Thiết kế cơ sở dữ liệu (NoSQL MongoDB Collections)"
    )

# Update Architecture Diagram in Chapter 3
old_arch = """+-----------------------+    +--------------------+    +------------------+
|  AI FACE MICROSERVICE |    | GOOGLE GEMINI API  |    |  MONGODB ATLAS   |
|   Python FastAPI      |    |  Multimodal Vision |    | NoSQL Cluster    |
| UniFace + MiniFASNet  |    | & Policy Inspector |    | GeoJSON 2dsphere |
+-----------------------+    +--------------------+    +------------------+
                                        |
                             +--------------------+
                             | EXTERNAL SERVICES  |
                             | - VietQR/TPBank    |
                             | - Telegram Bot API |
                             | - Gmail SMTP       |
                             +--------------------+"""

new_arch = """+-----------------------+    +--------------------------+    +------------------+
|  AI FACE MICROSERVICE |    |  AI SUITE (3 MODELS):    |    |  MONGODB ATLAS   |
|   Python FastAPI      |    |  - Gemini (Vision OCR)   |    | NoSQL Cluster    |
| UniFace + MiniFASNet  |    |  - Grok (Policy & Logic) |    | GeoJSON 2dsphere |
|                       |    |  - Astra 6 (Chatbot Kira)|    | Bán kính <= 3 km |
+-----------------------+    +--------------------------+    +------------------+
                                           |
                             +----------------------------+
                             | EXTERNAL SERVICES & PAYMENT|
                             | - SePay & Tingo Pay Webhook|
                             | - VietQR Auto Settlement   |
                             | - Telegram Bot & Gmail SMTP|
                             +----------------------------+"""

content = content.replace(old_arch, new_arch)

# Update Section 3.3, 3.4, 3.6 descriptions
content = content.replace(
    "- **Module Đăng sản phẩm tích hợp phản hồi AI tức thời (Hình 3.11):** Khi người bán nhập thông tin và tải ảnh, hệ thống hiển thị tiến trình quét AI trực quan. Nếu ảnh không khớp với tên hàng hoặc phát hiện dấu hiệu vi phạm chính sách, màn hình sẽ hiển thị cảnh báo chi tiết và tự động chuyển đơn duyệt sang Quản lý ca trực.",
    "- **Module Đăng sản phẩm tích hợp phản hồi AI tức thời (Hình 3.11):** Khi người bán nhập thông tin và tải ảnh, hệ thống hiển thị tiến trình quét song song của Gemini và Grok. Nếu xảy ra hiện tượng bất đồng bộ (ví dụ: tên rau nhưng ảnh thịt), màn hình lập tức hiển thị cảnh báo tỷ lệ khớp không đạt chuẩn và chuyển đơn hàng sang hàng đợi cho Admin/Quản lý duyệt tay thủ công."
)

content = content.replace(
    "- **Module Kiểm duyệt sản phẩm gắn cờ AI (Hình 3.18):** Danh sách tập trung các sản phẩm có độ khớp ảnh - tên dưới 70% do Gemini AI gắn cờ; Quản lý xem xét bằng mắt thường để quyết định \"Duyệt mở bán\" hoặc \"Từ chối và yêu cầu sửa bài\".",
    "- **Module Kiểm duyệt sản phẩm gắn cờ AI (Hình 3.18):** Danh sách tập trung các sản phẩm có độ khớp ảnh - tên dưới 70% do Gemini và Grok phát hiện (như trường hợp tên rau nhưng ảnh thịt); Admin/Quản lý xem xét chi tiết hình ảnh và thông tin để duyệt tay mở bán hoặc từ chối kèm gửi email nhắc nhở cho người bán."
)

# Update Conclusion section
content = content.replace(
    "- Xây dựng hoàn chỉnh mô hình sàn thương mại điện tử giao hàng hỏa tốc trong bán kính cục bộ 1 - 10 km, rút ngắn thời gian giao hàng xuống còn 15 - 25 phút.",
    "- Xây dựng hoàn chỉnh mô hình sàn thương mại điện tử giao hàng hỏa tốc trong bán kính siêu cục bộ <= 3 km, rút ngắn thời gian giao hàng xuống còn 15 - 25 phút."
)

content = content.replace(
    "- Ứng dụng thành công nền tảng **ASP.NET Core 9.0 Web API** kết hợp **MongoDB Atlas**, khai thác hiệu quả chỉ mục không gian địa lý GeoJSON để tìm kiếm và quét khoảng cách tức thì.",
    "- Ứng dụng thành công nền tảng **ASP.NET Core 9.0 Web API** kết hợp **MongoDB Atlas**, khai thác hiệu quả chỉ mục không gian địa lý GeoJSON để tìm kiếm và quét khoảng cách tức thì trong phạm vi <= 3 km."
)

content = content.replace(
    "- Tích hợp thành công mô hình trí tuệ nhân tạo đa phương thức **Google Gemini Multimodal AI**, tự động hóa quy trình kiểm duyệt nội dung ảnh và tên sản phẩm với ngưỡng đối soát chéo 70%, bảo vệ sàn khỏi hàng cấm và nội dung vi phạm pháp luật.",
    "- Tích hợp thành công **hệ sinh thái AI 3 mô hình (Google Gemini, xAI Grok và Astra 6)**: Tự động hóa kiểm duyệt hình ảnh và văn bản (phát hiện bất đồng bộ tên rau ảnh thịt để gửi Admin duyệt tay), kết hợp trợ lý ảo Astra 6 hỗ trợ tư vấn và đặt hàng tự nhiên."
)

content = content.replace(
    "- Tự động hóa hoàn toàn quy trình thanh toán chuyển khoản ngân hàng thông qua mã **VietQR động** kết hợp Webhook đối soát số dư tức thời, không cần nhân viên kế toán can thiệp thủ công.",
    "- Tự động hóa hoàn toàn quy trình thanh toán chuyển khoản ngân hàng thông qua mã **VietQR động** kết hợp cổng thanh toán tự động **SePay** qua Webhook đối soát số dư trong 1 - 3 giây, không cần nhân viên kế toán can thiệp thủ công."
)

# Update References
content = content.replace(
    "3. **Google DeepMind & Google Cloud** (2024). *Google Gemini Multimodal AI Developer Documentation*. Truy cập từ: https://ai.google.dev/docs/gemini_api_overview",
    """3. **Google DeepMind & Google Cloud** (2024). *Google Gemini Multimodal AI Developer Documentation*. Truy cập từ: https://ai.google.dev/docs/gemini_api_overview
4. **xAI Foundation** (2024). *Grok LLM Technical Architecture & Content Moderation Guidelines*. Truy cập từ: https://x.ai/
5. **Astra AI Research Group** (2024). *Astra 6 Conversational AI & E-commerce Chatbot Model Specification*.
6. **SePay Technologies** (2024). *SePay Payment Automation Webhook API Documentation*. Truy cập từ: https://sepay.vn/docs/"""
)

with open(md_path, "w", encoding="utf-8") as f:
    f.write(content)

print("[SUCCESS] Markdown file updated thoroughly!")
