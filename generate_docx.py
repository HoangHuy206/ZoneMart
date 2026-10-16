import os
import re
import docx
from docx.shared import Inches, Pt, RGBColor
from docx.enum.text import WD_ALIGN_PARAGRAPH
from docx.enum.table import WD_TABLE_ALIGNMENT, WD_ALIGN_VERTICAL
from docx.oxml import OxmlElement, parse_xml
from docx.oxml.ns import nsdecls, qn

def set_cell_background(cell, fill_hex):
    tcPr = cell._tc.get_or_add_tcPr()
    shd = parse_xml(f'<w:shd {nsdecls("w")} w:fill="{fill_hex}"/>')
    tcPr.append(shd)

def set_cell_margins(cell, top=100, bottom=100, left=150, right=150):
    tcPr = cell._tc.get_or_add_tcPr()
    tcMar = parse_xml(f'''
        <w:tcMar {nsdecls("w")}>
            <w:top w:w="{top}" w:type="dxa"/>
            <w:bottom w:w="{bottom}" w:type="dxa"/>
            <w:left w:w="{left}" w:type="dxa"/>
            <w:right w:w="{right}" w:type="dxa"/>
        </w:tcMar>
    ''')
    tcPr.append(tcMar)

def set_table_borders(table, color="B0B0B0", sz="4", val="single"):
    tblPr = table._tbl.tblPr
    borders = parse_xml(f'''
        <w:tblBorders {nsdecls("w")}>
            <w:top w:val="{val}" w:sz="{sz}" w:space="0" w:color="{color}"/>
            <w:bottom w:val="{val}" w:sz="{sz}" w:space="0" w:color="{color}"/>
            <w:left w:val="{val}" w:sz="{sz}" w:space="0" w:color="{color}"/>
            <w:right w:val="{val}" w:sz="{sz}" w:space="0" w:color="{color}"/>
            <w:insideH w:val="{val}" w:sz="{sz}" w:space="0" w:color="{color}"/>
            <w:insideV w:val="{val}" w:sz="{sz}" w:space="0" w:color="{color}"/>
        </w:tblBorders>
    ''')
    tblPr.append(borders)

def make_callout_box(doc, title, content_lines, is_image=False):
    tbl = doc.add_table(rows=1, cols=1)
    tbl.alignment = WD_TABLE_ALIGNMENT.CENTER
    cell = tbl.cell(0, 0)
    set_cell_background(cell, "F4F6F9" if not is_image else "F0F4F8")
    set_cell_margins(cell, top=150, bottom=150, left=200, right=200)
    
    # Border
    tcPr = cell._tc.get_or_add_tcPr()
    border_color = "4A777A" if is_image else "2E5B82"
    borders = parse_xml(f'''
        <w:tcBorders {nsdecls("w")}>
            <w:top w:val="single" w:sz="6" w:space="0" w:color="{border_color}"/>
            <w:left w:val="single" w:sz="24" w:space="0" w:color="{border_color}"/>
            <w:bottom w:val="single" w:sz="6" w:space="0" w:color="{border_color}"/>
            <w:right w:val="single" w:sz="6" w:space="0" w:color="{border_color}"/>
        </w:tcBorders>
    ''')
    tcPr.append(borders)
    
    p = cell.paragraphs[0]
    p.paragraph_format.space_before = Pt(4)
    p.paragraph_format.space_after = Pt(4)
    p.paragraph_format.line_spacing = 1.15
    run_title = p.add_run(title)
    run_title.bold = True
    run_title.font.name = "Times New Roman"
    run_title.font.size = Pt(12)
    run_title.font.color.rgb = RGBColor(0x1B, 0x36, 0x5D)
    
    for line in content_lines:
        p2 = cell.add_paragraph()
        p2.paragraph_format.space_before = Pt(2)
        p2.paragraph_format.space_after = Pt(2)
        p2.paragraph_format.line_spacing = 1.15
        run = p2.add_run(line)
        run.font.name = "Times New Roman"
        run.font.size = Pt(11)
        run.font.italic = is_image
        if is_image:
            run.font.color.rgb = RGBColor(0x55, 0x55, 0x55)
            
    doc.add_paragraph().paragraph_format.space_after = Pt(6)

def format_run_text(run):
    run.font.name = "Times New Roman"

def main():
    md_path = r"C:\Users\kayy0\.gemini\antigravity\brain\ff1a3665-6bfa-430b-8f81-e5f1fb5e7295\BaoCao_TotNghiep_ZoneMart.md"
    out_docx = r"D:\ZoneMart\BaoCao_TotNghiep_ZoneMart.docx"
    
    with open(md_path, "r", encoding="utf-8") as f:
        lines = f.readlines()
        
    doc = docx.Document()
    
    # 1. Page Setup: Standard HHT Format
    # Top 20mm, Bottom 20mm, Left 30mm, Right 20mm
    sections = doc.sections
    for section in sections:
        section.top_margin = Inches(0.79)     # 20 mm
        section.bottom_margin = Inches(0.79)  # 20 mm
        section.left_margin = Inches(1.18)    # 30 mm
        section.right_margin = Inches(0.79)   # 20 mm
        section.header_distance = Inches(0.5)
        section.footer_distance = Inches(0.5)
        
    # Set default style
    normal_style = doc.styles['Normal']
    normal_font = normal_style.font
    normal_font.name = 'Times New Roman'
    normal_font.size = Pt(13)
    normal_font.color.rgb = RGBColor(0, 0, 0)
    
    i = 0
    total_lines = len(lines)
    
    # Parse cover page specially
    # Look for start of TOC or CHƯƠNG 1
    in_mermaid = False
    mermaid_lines = []
    mermaid_title = ""
    
    in_table = False
    table_lines = []
    
    while i < total_lines:
        line = lines[i].rstrip('\r\n')
        
        # Check page break
        if line.strip() == r'\newpage':
            doc.add_page_break()
            i += 1
            continue
            
        if line.strip() == '---':
            # Separator / Divider
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
            # Collect entire table
            table_lines = [line]
            i += 1
            while i < total_lines and lines[i].strip().startswith('|'):
                table_lines.append(lines[i].rstrip('\r\n'))
                i += 1
                
            # Process table
            # Parse markdown table
            rows_data = []
            for t_line in table_lines:
                # check if separator row like | :--- | :--- |
                cells = [c.strip() for c in t_line.strip().split('|')]
                if len(cells) >= 3:
                    row_cells = cells[1:-1]
                    # check if this is separator line
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
                        
                        # Style cell
                        set_cell_margins(cell, top=100, bottom=100, left=140, right=140)
                        cell.vertical_alignment = WD_ALIGN_VERTICAL.CENTER
                        
                        if r_idx == 0:
                            set_cell_background(cell, "D9E1F2") # Soft professional blue-gray
                        elif r_idx % 2 == 1:
                            set_cell_background(cell, "FFFFFF")
                        else:
                            set_cell_background(cell, "F9FAFC")
                            
                        # Add text
                        p = cell.paragraphs[0]
                        p.paragraph_format.space_before = Pt(3)
                        p.paragraph_format.space_after = Pt(3)
                        p.paragraph_format.line_spacing = 1.15
                        
                        # Clean html breaks <br/>
                        text = text.replace('<br/>', '\n').replace('<br>', '\n')
                        # Check bold formatting **text**
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
            
        # Check Headings
        if line.startswith('# '):
            title_text = line[2:].strip()
            # If cover title
            p = doc.add_paragraph()
            p.paragraph_format.space_before = Pt(12)
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

        # Check image notice
        if "Hình 2." in line or "Hình 3." in line or "Hình 1." in line:
            # Styled image placement box
            p = doc.add_paragraph()
            p.paragraph_format.space_before = Pt(8)
            p.paragraph_format.space_after = Pt(4)
            p.paragraph_format.alignment = WD_ALIGN_PARAGRAPH.CENTER
            
            # Add callout box for the image placeholder
            clean_caption = line.strip().replace('**', '').replace('*', '')
            box_content = [
                "[ KHUNG HÌNH ẢNH MINH HỌA HỆ THỐNG ZONEMART ]",
                f"Tên minh họa: {clean_caption}",
                "(Vui lòng dán ảnh chụp màn hình giao diện thực tế / sơ đồ draw.io tại đây)"
            ]
            make_callout_box(doc, f"📷 {clean_caption}", box_content, is_image=True)
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
            # Check center div / align right
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

    doc.save(out_docx)
    print(f"[SUCCESS] Exported Word Document successfully to: {out_docx}")

if __name__ == '__main__':
    main()
