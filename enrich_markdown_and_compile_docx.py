import os
import re
import docx
from docx.shared import Inches, Pt, RGBColor
from docx.enum.text import WD_ALIGN_PARAGRAPH
from docx.enum.table import WD_TABLE_ALIGNMENT, WD_ALIGN_VERTICAL
from docx.oxml import OxmlElement, parse_xml
from docx.oxml.ns import nsdecls, qn
from PIL import Image

WORKSPACE = r"D:\ZoneMart"
MEDIA_DIR = os.path.join(WORKSPACE, "extracted_media")
USECASE_DIR = os.path.join(WORKSPACE, "extracted_usecase_images")
UI_DIR = os.path.join(WORKSPACE, "extracted_ui_images")
MD_PATH = r"C:\Users\kayy0\.gemini\antigravity\brain\ff1a3665-6bfa-430b-8f81-e5f1fb5e7295\BaoCao_TotNghiep_ZoneMart.md"
OUT_DOCX = os.path.join(WORKSPACE, "BaoCao_TotNghiep_ZoneMart.docx")

from docx_image_config import (
    IMAGE_MAPPING, 
    set_table_borders, 
    set_cell_background, 
    set_cell_margins, 
    make_callout_box, 
    insert_image_with_caption
)

def enrich_markdown():
    with open(MD_PATH, "r", encoding="utf-8") as f:
        content = f.read()

    # 1. Enrich DANH MỤC HÌNH ẢNH
    new_danh_muc_hinh = """## DANH MỤC HÌNH ẢNH

### Chương 1: Tổng quan đề tài & Kiến trúc
- **Hình 1.1:** Mô hình tổng thể phân tầng hệ thống ZoneMart (Client - Server - Microservices).
- **Hình 1.2:** Mô hình quản lý bán kính siêu cục bộ <= 3 km và mạng lưới chi nhánh điều phối.
- **Hình 1.3:** Chuỗi giá trị nghiệp vụ thương mại điện tử giao hàng hỏa tốc ZoneMart.

### Chương 2: Phân tích và thiết kế hệ thống
- **Hình 2.1:** Tổng quan luồng nghiệp vụ liên hoàn toàn hệ thống ZoneMart.
- **Hình 2.2a:** Luồng nghiệp vụ đăng ký mở gian hàng và thẩm định hồ sơ KYC.
- **Hình 2.2b:** Luồng mua hàng và trợ lý ảo AI tư vấn thông minh (Astra 6).
- **Hình 2.2c:** Luồng xử lý và đóng gói đơn hàng tại gian hàng đối tác.
- **Hình 2.2d:** Luồng điều phối và giao hàng hỏa tốc theo GPS bán kính <= 3 km.
- **Hình 2.2e:** Luồng kiểm soát chất lượng ATTP và kiểm duyệt chéo ảnh - tên (Gemini + Grok).
- **Hình 2.2f:** Tổng hợp mô hình phân tích chức năng hệ thống ZoneMart.
- **Hình 2.2g:** Sơ đồ phân tích yêu cầu nghiệp vụ chi tiết theo tác nhân.
- **Hình 2.3:** Sơ đồ Use Case tổng quát toàn bộ hệ thống ZoneMart.
- **Hình 2.3a:** Sơ đồ Use Case phân hệ Khách hàng / Người mua.
- **Hình 2.3b:** Sơ đồ Use Case phân hệ Chủ gian hàng (Người bán).
- **Hình 2.3c:** Sơ đồ Use Case phân hệ Tài xế (Shipper).
- **Hình 2.3d:** Sơ đồ Use Case phân hệ Ban quản trị (Admin / Manager).
- **Hình 2.4:** Sơ đồ Use Case Đăng nhập và khôi phục tài khoản.
- **Hình 2.5:** Sơ đồ Use Case Đăng ký tài khoản và nâng cấp đối tác.
- **Hình 2.6:** Sơ đồ Use Case Tìm kiếm và xem sản phẩm trong bán kính siêu cục bộ <= 3 km.
- **Hình 2.7:** Sơ đồ Use Case Quản lý giỏ hàng phân nhóm gian hàng.
- **Hình 2.8:** Sơ đồ Use Case Đặt hàng và thanh toán VietQR / SePay tự động.
- **Hình 2.9:** Sơ đồ Use Case Theo dõi đơn mua và gửi yêu cầu hỗ trợ.
- **Hình 2.10:** Sơ đồ Use Case Quản lý tài khoản cá nhân & Face ID.
- **Hình 2.11:** Sơ đồ Use Case Quản lý sản phẩm của gian hàng.
- **Hình 2.12:** Sơ đồ Use Case Xử lý đơn hàng của Người bán.
- **Hình 2.13:** Sơ đồ Use Case Quản lý gian hàng & Doanh thu.
- **Hình 2.14:** Sơ đồ Use Case Nhận và giao đơn hàng của Tài xế.
- **Hình 2.15:** Sơ đồ Use Case Quản lý thu nhập và hồ sơ Tài xế.
- **Hình 2.16:** Sơ đồ Use Case Thẩm định hồ sơ đối tác KYC.
- **Hình 2.17:** Sơ đồ Use Case Kiểm duyệt sản phẩm gắn cờ AI.
- **Hình 2.18:** Sơ đồ Use Case Quản lý người dùng & Chế tài vi phạm 3 mức.
- **Hình 2.19:** Sơ đồ Use Case Quản lý tài khoản Quản lý ca trực.
- **Hình 2.20:** Sơ đồ Use Case Thống kê toàn sàn và Nhật ký hệ thống (Audit Log).
- **Hình 2.21a:** Sơ đồ hoạt động (Activity Diagram) Đăng ký và thẩm định duyệt Store KYC.
- **Hình 2.21b:** Sơ đồ hoạt động (Activity Diagram) Mua hàng, AI tư vấn và Thanh toán SePay.
- **Hình 2.21c:** Sơ đồ hoạt động (Activity Diagram) Điều phối tài xế hỏa tốc GPS bán kính <= 3 km.
- **Hình 2.21d:** Sơ đồ hoạt động (Activity Diagram) Khiếu nại và kiểm soát chất lượng ATTP.
- **Hình 2.22:** Sơ đồ ERD tổng thể CSDL NoSQL MongoDB ZoneMart.
- **Hình 2.22a:** Lược đồ dữ liệu phân hệ Người dùng và Sinh trắc học Face ID.
- **Hình 2.22b:** Lược đồ dữ liệu phân hệ Gian hàng và KYC.
- **Hình 2.22c:** Lược đồ dữ liệu phân hệ Sản phẩm và Kiểm duyệt AI.
- **Hình 2.22d:** Lược đồ dữ liệu phân hệ Giỏ hàng, Đơn cha và Đơn con.
- **Hình 2.22e:** Lược đồ dữ liệu phân hệ Logistics và Tài xế GPS.

### Chương 3: Cài đặt và thực nghiệm hệ thống
- **Hình 3.1a:** Kiến trúc tích hợp các dịch vụ bên ngoài (Gemini, Grok, Astra 6, SePay, UniFace, Telegram).
- **Hình 3.1b:** Kiến trúc ứng dụng phân tầng Backend ASP.NET Core & Microservices.
- **Hình 3.2a:** Thiết kế tổng quan hệ thống giao diện UI trên các thiết bị.
- **Hình 3.2b:** Thiết kế giao diện nâng cao, bản đồ GPS và tính năng đặc thù.
- **Hình 3.1:** Giao diện Đăng nhập đa phương thức (Email/SĐT & Face ID).
- **Hình 3.2:** Giao diện Trang chủ ZoneMart hiển thị sản phẩm theo khu vực địa lý.
- **Hình 3.3:** Giao diện Tìm kiếm và Bộ lọc bán kính giao hàng siêu cục bộ <= 3 km.
- **Hình 3.4:** Giao diện Chi tiết sản phẩm với Chứng nhận kiểm duyệt an toàn AI.
- **Hình 3.5:** Giao diện Giỏ hàng thông minh phân nhóm theo gian hàng lân cận.
- **Hình 3.6:** Giao diện Thanh toán tách đơn con & Quét mã VietQR động / SePay.
- **Hình 3.7:** Giao diện Theo dõi trạng thái đơn hàng thời gian thực & Bản đồ lộ trình.
- **Hình 3.8:** Giao diện Kênh Người bán (Seller Dashboard) quản lý doanh thu và sản phẩm.
- **Hình 3.9:** Giao diện Đăng bán sản phẩm với phản hồi kiểm duyệt AI trực quan.
- **Hình 3.10:** Giao diện Ra-đa nhận đơn hỏa tốc dành cho Tài xế (Shipper Radar 3 km).
- **Hình 3.11:** Giao diện Bảng điều khiển Quản trị viên (Admin Hub) & Thẩm định KYC.
- **Hình 3.12:** Giao diện Quản lý người dùng, Chế tài 3 mức và Nhật ký kiểm toán (Audit Log).
- **Hình 3.13:** Giao diện Trang quản lý danh mục sản phẩm.
- **Hình 3.14:** Giao diện Trang quản lý sản phẩm chi tiết.
- **Hình 3.15:** Giao diện Trang thêm mới sản phẩm.
- **Hình 3.16:** Giao diện Trang chỉnh sửa thông tin sản phẩm.
- **Hình 3.17:** Giao diện Trang quản lý tài khoản người dùng và phân quyền.
- **Hình 3.18:** Giao diện Trang quản lý và cập nhật trạng thái đơn hàng."""

    # Replace old DANH MỤC HÌNH ẢNH block
    dm_pattern = r'## DANH MỤC HÌNH ẢNH[\s\S]*?(?=---|\n## DANH MỤC BẢNG BIỂU)'
    content = re.sub(dm_pattern, new_danh_muc_hinh + "\n\n", content)

    # 2. Enrich Chapter 1 with Hình 1.1, 1.2, 1.3
    if "Hình 1.1: Mô hình tổng thể" not in content:
        c1_old = "### 1.6.1 Nền tảng Backend ASP.NET Core Web API (.NET 9)"
        c1_new = """Hình 1.1: Mô hình tổng thể phân tầng hệ thống ZoneMart (Client - Server - Microservices).

Hình 1.2: Mô hình quản lý bán kính siêu cục bộ <= 3 km và mạng lưới chi nhánh điều phối.

Hình 1.3: Chuỗi giá trị nghiệp vụ thương mại điện tử giao hàng hỏa tốc ZoneMart.

### 1.6.1 Nền tảng Backend ASP.NET Core Web API (.NET 9)"""
        content = content.replace(c1_old, c1_new)

    # 3. Enrich Section 2.1 with Business Flow images
    if "Hình 2.1: Tổng quan luồng nghiệp vụ" not in content:
        sec21_old = "## 2.1 Phân tích và thiết kế các chức năng hệ thống"
        sec21_new = """## 2.1 Phân tích và thiết kế các chức năng hệ thống

Hình 2.1: Tổng quan luồng nghiệp vụ liên hoàn toàn hệ thống ZoneMart.

Hình 2.2a: Luồng nghiệp vụ đăng ký mở gian hàng và thẩm định hồ sơ KYC.

Hình 2.2b: Luồng mua hàng và trợ lý ảo AI tư vấn thông minh (Astra 6).

Hình 2.2c: Luồng xử lý và đóng gói đơn hàng tại gian hàng đối tác.

Hình 2.2d: Luồng điều phối và giao hàng hỏa tốc theo GPS bán kính <= 3 km.

Hình 2.2e: Luồng kiểm soát chất lượng ATTP và kiểm duyệt chéo ảnh - tên (Gemini + Grok).

Hình 2.2f: Tổng hợp mô hình phân tích chức năng hệ thống ZoneMart.

Hình 2.2g: Sơ đồ phân tích yêu cầu nghiệp vụ chi tiết theo tác nhân."""
        content = content.replace(sec21_old, sec21_new)

    # 4. Enrich Section 2.2 with Package Use Case Diagrams (2.3a - 2.3d)
    if "Hình 2.3a: Sơ đồ Use Case phân hệ Khách hàng" not in content:
        sec22_old = "### 2.2.2 Use Case tổng quát\nSơ đồ Use Case tổng quát mô tả toàn bộ các chức năng chính của hệ thống ZoneMart và mối quan hệ giữa các tác nhân với hệ thống."
        sec22_new = """### 2.2.2 Use Case tổng quát
Sơ đồ Use Case tổng quát mô tả toàn bộ các chức năng chính của hệ thống ZoneMart và mối quan hệ giữa các tác nhân với hệ thống.

Hình 2.3: Sơ đồ Use Case tổng quát toàn bộ hệ thống ZoneMart.

Hình 2.3a: Sơ đồ Use Case phân hệ Khách hàng / Người mua.

Hình 2.3b: Sơ đồ Use Case phân hệ Chủ gian hàng (Người bán).

Hình 2.3c: Sơ đồ Use Case phân hệ Tài xế (Shipper).

Hình 2.3d: Sơ đồ Use Case phân hệ Ban quản trị (Admin / Manager)."""
        content = content.replace(sec22_old, sec22_new)

    # 5. Enrich Section 2.4 with Activity Diagrams
    if "Hình 2.21a: Sơ đồ hoạt động" not in content:
        sec24_old = "## 2.4 Biểu đồ tuần tự (Sequence Diagrams)"
        sec24_new = """## 2.4 Sơ đồ Hoạt động (Activity Diagrams) và Biểu đồ Tuần tự (Sequence Diagrams)

### 2.4.1 Các sơ đồ hoạt động nghiệp vụ cốt lõi (Activity Diagrams)

Sơ đồ hoạt động mô tả chi tiết luồng điều khiển, rẽ nhánh điều kiện và sự phối hợp giữa các đối tượng tham gia trong các quy trình nghiệp vụ then chốt của ZoneMart:

Hình 2.21a: Sơ đồ hoạt động (Activity Diagram) Đăng ký và thẩm định duyệt Store KYC.

Hình 2.21b: Sơ đồ hoạt động (Activity Diagram) Mua hàng, AI tư vấn và Thanh toán SePay.

Hình 2.21c: Sơ đồ hoạt động (Activity Diagram) Điều phối tài xế hỏa tốc GPS bán kính <= 3 km.

Hình 2.21d: Sơ đồ hoạt động (Activity Diagram) Khiếu nại và kiểm soát chất lượng ATTP.

### 2.4.2 Các biểu đồ tuần tự hệ thống (Sequence Diagrams)"""
        content = content.replace(sec24_old, sec24_new)

    # 6. Enrich Section 2.5 with ERD images
    if "Hình 2.22: Sơ đồ ERD tổng thể" not in content:
        sec25_old = "### 2.5.1 Mô hình dữ liệu tổng quát (Document-Oriented Data Model)"
        sec25_new = """### 2.5.1 Mô hình dữ liệu tổng quát (Document-Oriented Data Model)

Hình 2.22: Sơ đồ ERD tổng thể CSDL NoSQL MongoDB ZoneMart.

Hình 2.22a: Lược đồ dữ liệu phân hệ Người dùng và Sinh trắc học Face ID.

Hình 2.22b: Lược đồ dữ liệu phân hệ Gian hàng và KYC.

Hình 2.22c: Lược đồ dữ liệu phân hệ Sản phẩm và Kiểm duyệt AI.

Hình 2.22d: Lược đồ dữ liệu phân hệ Giỏ hàng, Đơn cha và Đơn con.

Hình 2.22e: Lược đồ dữ liệu phân hệ Logistics và Tài xế GPS."""
        content = content.replace(sec25_old, sec25_new)

    # 7. Enrich Chapter 3 with Architecture and UI images
    if "Hình 3.1a: Kiến trúc tích hợp" not in content:
        sec31_old = "## 3.1 Kiến trúc triển khai hệ thống ZoneMart"
        sec31_new = """## 3.1 Kiến trúc triển khai hệ thống ZoneMart

Hình 3.1a: Kiến trúc tích hợp các dịch vụ bên ngoài (Gemini, Grok, Astra 6, SePay, UniFace, Telegram).

Hình 3.1b: Kiến trúc ứng dụng phân tầng Backend ASP.NET Core & Microservices."""
        content = content.replace(sec31_old, sec31_new)

    if "Hình 3.2a: Thiết kế tổng quan" not in content:
        sec32_old = "## 3.2 Phân hệ Xác thực & Bảo mật (Đăng nhập, Đăng ký, Face ID)"
        sec32_new = """## 3.2 Thiết kế giao diện và thực nghiệm các phân hệ người dùng

Hình 3.2a: Thiết kế tổng quan hệ thống giao diện UI trên các thiết bị.

Hình 3.2b: Thiết kế giao diện nâng cao, bản đồ GPS và tính năng đặc thù.

### 3.2.1 Phân hệ Xác thực & Bảo mật (Đăng nhập, Đăng ký, Face ID)"""
        content = content.replace(sec32_old, sec32_new)

    # Also add remaining UI screenshots in Section 3.6 if not yet there
    if "Hình 3.13: Giao diện Trang quản lý danh mục" not in content:
        sec36_old = "Module Quản lý người dùng & Chế tài 3 mức (Hình 3.19):"
        sec36_new = """Module Quản lý người dùng & Chế tài 3 mức (Hình 3.12):

Hình 3.13: Giao diện Trang quản lý danh mục sản phẩm.

Hình 3.14: Giao diện Trang quản lý sản phẩm chi tiết.

Hình 3.15: Giao diện Trang thêm mới sản phẩm.

Hình 3.16: Giao diện Trang chỉnh sửa thông tin sản phẩm.

Hình 3.17: Giao diện Trang quản lý tài khoản người dùng và phân quyền.

Hình 3.18: Giao diện Trang quản lý và cập nhật trạng thái đơn hàng."""
        content = content.replace("Module Quản lý người dùng & Chế tài 3 mức (Hình 3.19):", sec36_new)

    with open(MD_PATH, "w", encoding="utf-8") as f:
        f.write(content)
    print("[SUCCESS] Markdown successfully enriched with all diagram references!")

def generate_docx():
    with open(MD_PATH, "r", encoding="utf-8") as f:
        lines = f.readlines()
        
    doc = docx.Document()
    
    # 1. Page Setup: Standard HHT Format (A4: 20mm Top/Bottom/Right, 30mm Left)
    for section in doc.sections:
        section.top_margin = Inches(0.79)     # 20 mm
        section.bottom_margin = Inches(0.79)  # 20 mm
        section.left_margin = Inches(1.18)    # 30 mm
        section.right_margin = Inches(0.79)   # 20 mm
        section.header_distance = Inches(0.5)
        section.footer_distance = Inches(0.5)
        
    normal_style = doc.styles['Normal']
    normal_font = normal_style.font
    normal_font.name = 'Times New Roman'
    normal_font.size = Pt(13)
    normal_font.color.rgb = RGBColor(0, 0, 0)
    
    i = 0
    total_lines = len(lines)
    in_mermaid = False
    mermaid_lines = []
    mermaid_title = ""
    
    # Track inserted figures to avoid duplicate adjacent insertions
    inserted_figures = set()

    while i < total_lines:
        line = lines[i].rstrip('\r\n')
        
        # Check page break
        if line.strip() == r'\newpage':
            doc.add_page_break()
            i += 1
            continue
            
        if line.strip() == '---':
            i += 1
            continue
            
        # Check Mermaid block
        if line.strip().startswith('```mermaid'):
            in_mermaid = True
            mermaid_lines = []
            mermaid_title = "SƠ ĐỒ QUY TRÌNH HỆ THỐNG (UML / SEQUENCE / ARCHITECTURE)"
            i += 1
            continue
        elif in_mermaid:
            if line.strip().startswith('```'):
                in_mermaid = False
                make_callout_box(doc, f"📌 {mermaid_title}", mermaid_lines)
                i += 1
                continue
            else:
                mermaid_lines.append(line)
                i += 1
                continue

        # Check Table block (markdown table starting with |)
        if line.strip().startswith('|') and '|' in line.strip()[1:]:
            table_lines = [line]
            i += 1
            while i < total_lines and lines[i].strip().startswith('|'):
                table_lines.append(lines[i].rstrip('\r\n'))
                i += 1
                
            rows_data = []
            for t_line in table_lines:
                cells = [c.strip() for c in t_line.strip().split('|')]
                if len(cells) >= 3:
                    row_cells = cells[1:-1]
                    if all(re.match(r'^:?-+:?$', c) for c in row_cells if c):
                        continue
                    rows_data.append(row_cells)
                    
            if rows_data:
                num_rows = len(rows_data)
                num_cols = max(len(r) for r in rows_data)
                
                table = doc.add_table(rows=num_rows, cols=num_cols)
                table.alignment = WD_TABLE_ALIGNMENT.CENTER
                set_table_borders(table)
                
                for r_idx, row in enumerate(rows_data):
                    trPr = table.rows[r_idx]._tr.get_or_add_trPr()
                    trPr.append(parse_xml(f'<w:cantSplit {nsdecls("w")}/>'))
                    if r_idx == 0:
                        trPr.append(parse_xml(f'<w:tblHeader {nsdecls("w")}/>'))
                        
                    for c_idx in range(num_cols):
                        cell = table.cell(r_idx, c_idx)
                        text = row[c_idx] if c_idx < len(row) else ""
                        
                        set_cell_margins(cell, top=90, bottom=90, left=130, right=130)
                        cell.vertical_alignment = WD_ALIGN_VERTICAL.CENTER
                        
                        if r_idx == 0:
                            set_cell_background(cell, "D9E1F2")
                        elif r_idx % 2 == 1:
                            set_cell_background(cell, "FFFFFF")
                        else:
                            set_cell_background(cell, "F9FAFC")
                            
                        p = cell.paragraphs[0]
                        p.paragraph_format.space_before = Pt(2)
                        p.paragraph_format.space_after = Pt(2)
                        p.paragraph_format.line_spacing = 1.15
                        
                        text = text.replace('<br/>', '\n').replace('<br>', '\n')
                        parts = re.split(r'(\*\*.*?\*\*)', text)
                        for part in parts:
                            if part.startswith('**') and part.endswith('**'):
                                r = p.add_run(part[2:-2])
                                r.bold = True
                            elif part.startswith('*') and part.endswith('*'):
                                r = p.add_run(part[1:-1])
                                r.italic = True
                            else:
                                r = p.add_run(part)
                            r.font.name = "Times New Roman"
                            r.font.size = Pt(11) if r_idx > 0 else Pt(11.5)
                            if r_idx == 0:
                                r.bold = True
                                r.font.color.rgb = RGBColor(0x0C, 0x23, 0x40)
                
                doc.add_paragraph().paragraph_format.space_after = Pt(6)
            continue

        # Check Figure Caption & Image Insertion
        clean_line = line.strip().replace('**', '').replace('*', '')
        fig_match = re.match(r'^(Hình\s+[\d\.\w]+:)(.*)$', clean_line)
        if fig_match:
            fig_key = fig_match.group(1).rstrip(':')
            img_path = None
            for k, p in IMAGE_MAPPING.items():
                if k.rstrip(':') == fig_key:
                    if os.path.exists(p):
                        img_path = p
                        break
            
            if img_path and clean_line not in inserted_figures:
                inserted_figures.add(clean_line)
                insert_image_with_caption(doc, img_path, clean_line)
                i += 1
                continue
            elif clean_line in inserted_figures:
                # Already inserted with image, skip duplicate caption text
                i += 1
                continue
            
        # Check Headings
        if line.startswith('# '):
            title_text = line[2:].strip()
            p = doc.add_paragraph()
            p.paragraph_format.space_before = Pt(14)
            p.paragraph_format.space_after = Pt(8)
            p.paragraph_format.alignment = WD_ALIGN_PARAGRAPH.CENTER
            run = p.add_run(title_text)
            run.font.name = "Times New Roman"
            run.font.size = Pt(18)
            run.bold = True
            run.font.color.rgb = RGBColor(0x0F, 0x20, 0x42)
            i += 1
            continue
            
        elif line.startswith('## '):
            h_text = line[3:].strip()
            p = doc.add_paragraph()
            p.paragraph_format.space_before = Pt(16)
            p.paragraph_format.space_after = Pt(6)
            p.paragraph_format.keep_with_next = True
            run = p.add_run(h_text)
            run.font.name = "Times New Roman"
            run.font.size = Pt(15)
            run.bold = True
            run.font.color.rgb = RGBColor(0x1B, 0x36, 0x5D)
            i += 1
            continue

        elif line.startswith('### '):
            h_text = line[4:].strip()
            p = doc.add_paragraph()
            p.paragraph_format.space_before = Pt(12)
            p.paragraph_format.space_after = Pt(4)
            p.paragraph_format.keep_with_next = True
            run = p.add_run(h_text)
            run.font.name = "Times New Roman"
            run.font.size = Pt(13.5)
            run.bold = True
            run.font.color.rgb = RGBColor(0x22, 0x22, 0x22)
            i += 1
            continue

        elif line.startswith('#### '):
            h_text = line[5:].strip()
            p = doc.add_paragraph()
            p.paragraph_format.space_before = Pt(8)
            p.paragraph_format.space_after = Pt(3)
            p.paragraph_format.keep_with_next = True
            run = p.add_run(h_text)
            run.font.name = "Times New Roman"
            run.font.size = Pt(13)
            run.bold = True
            run.italic = True
            run.font.color.rgb = RGBColor(0x33, 0x33, 0x33)
            i += 1
            continue

        # Check Bullet list
        if line.strip().startswith('- ') or line.strip().startswith('* '):
            bullet_text = line.strip()[2:]
            p = doc.add_paragraph(style='List Bullet')
            p.paragraph_format.space_before = Pt(2)
            p.paragraph_format.space_after = Pt(2)
            p.paragraph_format.line_spacing = 1.3
            p.paragraph_format.alignment = WD_ALIGN_PARAGRAPH.JUSTIFY
            
            parts = re.split(r'(\*\*.*?\*\*|\*.*?\*)', bullet_text)
            for part in parts:
                if part.startswith('**') and part.endswith('**'):
                    r = p.add_run(part[2:-2])
                    r.bold = True
                elif part.startswith('*') and part.endswith('*'):
                    r = p.add_run(part[1:-1])
                    r.italic = True
                else:
                    r = p.add_run(part)
                r.font.name = "Times New Roman"
                r.font.size = Pt(13)
            i += 1
            continue

        # Check numbered list
        num_match = re.match(r'^(\d+\.)\s+(.*)$', line.strip())
        if num_match:
            num_prefix = num_match.group(1)
            num_text = num_match.group(2)
            p = doc.add_paragraph()
            p.paragraph_format.left_indent = Inches(0.25)
            p.paragraph_format.space_before = Pt(2)
            p.paragraph_format.space_after = Pt(2)
            p.paragraph_format.line_spacing = 1.3
            p.paragraph_format.alignment = WD_ALIGN_PARAGRAPH.JUSTIFY
            
            r_num = p.add_run(f"{num_prefix} ")
            r_num.bold = True
            r_num.font.name = "Times New Roman"
            r_num.font.size = Pt(13)
            
            parts = re.split(r'(\*\*.*?\*\*|\*.*?\*)', num_text)
            for part in parts:
                if part.startswith('**') and part.endswith('**'):
                    r = p.add_run(part[2:-2])
                    r.bold = True
                elif part.startswith('*') and part.endswith('*'):
                    r = p.add_run(part[1:-1])
                    r.italic = True
                else:
                    r = p.add_run(part)
                r.font.name = "Times New Roman"
                r.font.size = Pt(13)
            i += 1
            continue

        # Normal paragraph
        if line.strip():
            align = WD_ALIGN_PARAGRAPH.JUSTIFY
            text_to_process = line.strip()
            
            if '<div align="right">' in text_to_process:
                align = WD_ALIGN_PARAGRAPH.RIGHT
                text_to_process = text_to_process.replace('<div align="right">', '').replace('</div>', '')
            elif '<div align="center">' in text_to_process:
                align = WD_ALIGN_PARAGRAPH.CENTER
                text_to_process = text_to_process.replace('<div align="center">', '').replace('</div>', '')
                
            p = doc.add_paragraph()
            p.paragraph_format.space_before = Pt(3)
            p.paragraph_format.space_after = Pt(4)
            p.paragraph_format.line_spacing = 1.3
            p.paragraph_format.alignment = align
            
            text_to_process = text_to_process.replace('<br/>', '\n').replace('<br>', '\n')
            parts = re.split(r'(\*\*.*?\*\*|\*.*?\*)', text_to_process)
            for part in parts:
                if part.startswith('**') and part.endswith('**'):
                    r = p.add_run(part[2:-2])
                    r.bold = True
                elif part.startswith('*') and part.endswith('*'):
                    r = p.add_run(part[1:-1])
                    r.italic = True
                else:
                    r = p.add_run(part)
                r.font.name = "Times New Roman"
                r.font.size = Pt(13)
                
        i += 1

    doc.save(OUT_DOCX)
    print(f"[SUCCESS] Exported Word Document successfully with {len(inserted_figures)} images embedded to: {OUT_DOCX}")

if __name__ == '__main__':
    print("[1/2] Enriching markdown...")
    enrich_markdown()
    print("[2/2] Generating Word docx...")
    generate_docx()
