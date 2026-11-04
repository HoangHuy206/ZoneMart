/**
 * ================================================================
 * EXPORT ORDER EXCEL UTILITY (ZoneMart)
 * ================================================================
 * Xuất danh sách đơn hàng ra file bảng tính Microsoft Excel (.xls / .xlsx)
 * Sử dụng chuẩn XML Spreadsheet 2003 của Microsoft:
 * - Đầy đủ màu sắc, tiêu đề, căn giữa, định dạng tiền tệ Việt Nam (VNĐ).
 * - Hiển thị tiếng Việt có dấu chuẩn 100% không bị vỡ font.
 * - Windows nhận diện trực tiếp là Microsoft Excel Worksheet (.xls) với biểu tượng Excel xanh lá.
 */

export interface ExportOrderItem {
  name: string;
  qty?: number;
  quantity?: number;
  price?: number;
}

export interface ExportOrderData {
  id: string;
  orderCode?: string;
  orderDate?: string;
  customerName?: string;
  customerPhone?: string;
  customerAddress?: string;
  priceFormatted?: string;
  paymentMethod?: string;
  status?: string;
  productSummary?: string;
  items?: ExportOrderItem[];
  shipperInfo?: {
    name?: string;
    phone?: string;
    licensePlate?: string;
  };
}

export function escapeXml(str: any): string {
  return String(str ?? "")
    .replace(/&/g, "&amp;")
    .replace(/</g, "&lt;")
    .replace(/>/g, "&gt;")
    .replace(/"/g, "&quot;")
    .replace(/'/g, "&apos;");
}

export function exportOrdersToExcel(
  ordersList: ExportOrderData[],
  storeName = "Test Gian Hàng",
  sellerName = "Chủ Gian Hàng",
  sellerEmail = "baconbao206@gmail.com"
): { isTemplate: boolean; count: number; fileName: string } {
  const now = new Date();
  const dateStr = now.toLocaleDateString("vi-VN");
  const timeStr = now.toLocaleTimeString("vi-VN", { hour: "2-digit", minute: "2-digit" });
  const fileDate = now.toISOString().slice(0, 10).replace(/-/g, "");
  const cleanStore = storeName.replace(/[^a-zA-Z0-9_\u00C0-\u024F\u1EA0-\u1EF9]/g, "_");

  const list = ordersList && ordersList.length > 0 ? ordersList : [];
  const isTemplate = list.length === 0;

  let xml = `<?xml version="1.0" encoding="utf-8"?>
<?mso-application progid="Excel.Sheet"?>
<Workbook xmlns="urn:schemas-microsoft-com:office:spreadsheet"
 xmlns:o="urn:schemas-microsoft-com:office:office"
 xmlns:x="urn:schemas-microsoft-com:office:excel"
 xmlns:ss="urn:schemas-microsoft-com:office:spreadsheet"
 xmlns:html="http://www.w3.org/TR/REC-html40">
 <Styles>
  <Style ss:ID="Default" ss:Name="Normal">
   <Alignment ss:Vertical="Center"/>
   <Font ss:FontName="Segoe UI" ss:Size="10" ss:Color="#1F2937"/>
  </Style>
  <Style ss:ID="Title">
   <Font ss:FontName="Segoe UI" ss:Size="14" ss:Bold="1" ss:Color="#EA580C"/>
   <Alignment ss:Horizontal="Left" ss:Vertical="Center"/>
  </Style>
  <Style ss:ID="SubTitle">
   <Font ss:FontName="Segoe UI" ss:Size="10" ss:Italic="1" ss:Color="#4B5563"/>
  </Style>
  <Style ss:ID="Header">
   <Font ss:FontName="Segoe UI" ss:Size="10" ss:Bold="1" ss:Color="#FFFFFF"/>
   <Interior ss:Color="#EA580C" ss:Pattern="Solid"/>
   <Alignment ss:Horizontal="Center" ss:Vertical="Center"/>
   <Borders>
    <Border ss:Position="Bottom" ss:LineStyle="Continuous" ss:Weight="1" ss:Color="#CBD5E1"/>
    <Border ss:Position="Left" ss:LineStyle="Continuous" ss:Weight="1" ss:Color="#CBD5E1"/>
    <Border ss:Position="Right" ss:LineStyle="Continuous" ss:Weight="1" ss:Color="#CBD5E1"/>
    <Border ss:Position="Top" ss:LineStyle="Continuous" ss:Weight="1" ss:Color="#CBD5E1"/>
   </Borders>
  </Style>
  <Style ss:ID="CellCenter">
   <Alignment ss:Horizontal="Center" ss:Vertical="Center"/>
   <Borders>
    <Border ss:Position="Bottom" ss:LineStyle="Continuous" ss:Weight="1" ss:Color="#E2E8F0"/>
    <Border ss:Position="Left" ss:LineStyle="Continuous" ss:Weight="1" ss:Color="#E2E8F0"/>
    <Border ss:Position="Right" ss:LineStyle="Continuous" ss:Weight="1" ss:Color="#E2E8F0"/>
    <Border ss:Position="Top" ss:LineStyle="Continuous" ss:Weight="1" ss:Color="#E2E8F0"/>
   </Borders>
  </Style>
  <Style ss:ID="CellLeft">
   <Alignment ss:Horizontal="Left" ss:Vertical="Center"/>
   <Borders>
    <Border ss:Position="Bottom" ss:LineStyle="Continuous" ss:Weight="1" ss:Color="#E2E8F0"/>
    <Border ss:Position="Left" ss:LineStyle="Continuous" ss:Weight="1" ss:Color="#E2E8F0"/>
    <Border ss:Position="Right" ss:LineStyle="Continuous" ss:Weight="1" ss:Color="#E2E8F0"/>
    <Border ss:Position="Top" ss:LineStyle="Continuous" ss:Weight="1" ss:Color="#E2E8F0"/>
   </Borders>
  </Style>
  <Style ss:ID="CellNumber">
   <NumberFormat ss:Format="#,##0"/>
   <Alignment ss:Horizontal="Right" ss:Vertical="Center"/>
   <Borders>
    <Border ss:Position="Bottom" ss:LineStyle="Continuous" ss:Weight="1" ss:Color="#E2E8F0"/>
    <Border ss:Position="Left" ss:LineStyle="Continuous" ss:Weight="1" ss:Color="#E2E8F0"/>
    <Border ss:Position="Right" ss:LineStyle="Continuous" ss:Weight="1" ss:Color="#E2E8F0"/>
    <Border ss:Position="Top" ss:LineStyle="Continuous" ss:Weight="1" ss:Color="#E2E8F0"/>
   </Borders>
  </Style>
  <Style ss:ID="TotalRow">
   <Font ss:FontName="Segoe UI" ss:Size="11" ss:Bold="1" ss:Color="#9A3412"/>
   <Interior ss:Color="#FFEDD5" ss:Pattern="Solid"/>
   <NumberFormat ss:Format="#,##0\ &quot;đ&quot;"/>
   <Alignment ss:Horizontal="Right" ss:Vertical="Center"/>
   <Borders>
    <Border ss:Position="Bottom" ss:LineStyle="Continuous" ss:Weight="2" ss:Color="#EA580C"/>
    <Border ss:Position="Top" ss:LineStyle="Continuous" ss:Weight="2" ss:Color="#EA580C"/>
   </Borders>
  </Style>
  <Style ss:ID="TotalLabel">
   <Font ss:FontName="Segoe UI" ss:Size="11" ss:Bold="1" ss:Color="#9A3412"/>
   <Interior ss:Color="#FFEDD5" ss:Pattern="Solid"/>
   <Alignment ss:Horizontal="Left" ss:Vertical="Center"/>
   <Borders>
    <Border ss:Position="Bottom" ss:LineStyle="Continuous" ss:Weight="2" ss:Color="#EA580C"/>
    <Border ss:Position="Top" ss:LineStyle="Continuous" ss:Weight="2" ss:Color="#EA580C"/>
   </Borders>
  </Style>
 </Styles>
 <Worksheet ss:Name="Báo Cáo Đơn Hàng">
  <Table ss:DefaultRowHeight="20">
   <Column ss:Width="45"/>
   <Column ss:Width="95"/>
   <Column ss:Width="110"/>
   <Column ss:Width="130"/>
   <Column ss:Width="105"/>
   <Column ss:Width="210"/>
   <Column ss:Width="190"/>
   <Column ss:Width="110"/>
   <Column ss:Width="120"/>
   <Column ss:Width="130"/>
   <Column ss:Width="170"/>
   <Row ss:Height="26">
    <Cell ss:MergeAcross="10" ss:StyleID="Title"><Data ss:Type="String">BÁO CÁO DANH SÁCH ĐƠN HÀNG - SÀN THƯƠNG MẠI ĐIỆN TỬ ZONEMART</Data></Cell>
   </Row>
   <Row>
    <Cell ss:MergeAcross="10" ss:StyleID="SubTitle"><Data ss:Type="String">Gian Hàng: ${escapeXml(storeName)} | Chủ Sạp: ${escapeXml(sellerName)} (${escapeXml(sellerEmail)}) | Xuất lúc: ${escapeXml(timeStr + " " + dateStr)} | Tổng: ${list.length} đơn</Data></Cell>
   </Row>
   <Row ss:Height="12"></Row>
   <Row ss:Height="24">
    <Cell ss:StyleID="Header"><Data ss:Type="String">STT</Data></Cell>
    <Cell ss:StyleID="Header"><Data ss:Type="String">Mã Đơn</Data></Cell>
    <Cell ss:StyleID="Header"><Data ss:Type="String">Thời Gian Đặt</Data></Cell>
    <Cell ss:StyleID="Header"><Data ss:Type="String">Khách Hàng</Data></Cell>
    <Cell ss:StyleID="Header"><Data ss:Type="String">Số Điện Thoại</Data></Cell>
    <Cell ss:StyleID="Header"><Data ss:Type="String">Địa Chỉ Nhận Hàng</Data></Cell>
    <Cell ss:StyleID="Header"><Data ss:Type="String">Chi Tiết Mặt Hàng</Data></Cell>
    <Cell ss:StyleID="Header"><Data ss:Type="String">Tổng Tiền</Data></Cell>
    <Cell ss:StyleID="Header"><Data ss:Type="String">Thanh Toán</Data></Cell>
    <Cell ss:StyleID="Header"><Data ss:Type="String">Trạng Thái Đơn</Data></Cell>
    <Cell ss:StyleID="Header"><Data ss:Type="String">Tài Xế Phụ Trách</Data></Cell>
   </Row>`;

  if (isTemplate) {
    xml += `
   <Row ss:Height="22">
    <Cell ss:StyleID="CellCenter"><Data ss:Type="String">MẪU</Data></Cell>
    <Cell ss:StyleID="CellCenter"><Data ss:Type="String">ZM948999</Data></Cell>
    <Cell ss:StyleID="CellCenter"><Data ss:Type="String">${escapeXml(timeStr + " " + dateStr)}</Data></Cell>
    <Cell ss:StyleID="CellLeft"><Data ss:Type="String">Nguyễn Văn An</Data></Cell>
    <Cell ss:StyleID="CellCenter"><Data ss:Type="String">0912345678</Data></Cell>
    <Cell ss:StyleID="CellLeft"><Data ss:Type="String">Số 206 Cầu Giấy, Quan Hoa, Cầu Giấy, Hà Nội</Data></Cell>
    <Cell ss:StyleID="CellLeft"><Data ss:Type="String">rau xà lách (x2); thịt ba chỉ (x1)</Data></Cell>
    <Cell ss:StyleID="CellNumber"><Data ss:Type="Number">58000</Data></Cell>
    <Cell ss:StyleID="CellLeft"><Data ss:Type="String">Tiền mặt COD</Data></Cell>
    <Cell ss:StyleID="CellCenter"><Data ss:Type="String">Hoàn thành - Đã giao</Data></Cell>
    <Cell ss:StyleID="CellLeft"><Data ss:Type="String">Nguyễn Văn Shipper (0988777666 - 29N1-67890)</Data></Cell>
   </Row>`;
  } else {
    let totalRev = 0;
    list.forEach((ord, idx) => {
      const stt = idx + 1;
      const code = ord.orderCode || ord.id || `ZM${idx + 1}`;
      const time = ord.orderDate || dateStr;
      const cusName = ord.customerName || "Khách Hàng ZoneMart";
      const cusPhone = ord.customerPhone || "Chưa có SĐT";
      const cusAddr = ord.customerAddress || "Hà Nội";

      let itemsText = "";
      if (ord.items && ord.items.length > 0) {
        itemsText = ord.items.map(it => `${it.name} (x${(it as any).quantity || it.qty || 1})`).join("; ");
      } else {
        itemsText = ord.productSummary || "Sản phẩm gian hàng";
      }

      const rawNum = String(ord.priceFormatted || "0").replace(/[^0-9]/g, "");
      const numPrice = Number(rawNum) || 0;
      totalRev += numPrice;

      const payMethod = ord.paymentMethod || "Tiền mặt COD";
      const status = ord.status || "Chờ xử lý";
      const shipper = ord.shipperInfo
        ? `${ord.shipperInfo.name || ""} (${ord.shipperInfo.phone || ""} - ${ord.shipperInfo.licensePlate || ""})`
        : "Chưa phân bổ tài xế";

      xml += `
   <Row ss:Height="22">
    <Cell ss:StyleID="CellCenter"><Data ss:Type="Number">${stt}</Data></Cell>
    <Cell ss:StyleID="CellCenter"><Data ss:Type="String">${escapeXml(code)}</Data></Cell>
    <Cell ss:StyleID="CellCenter"><Data ss:Type="String">${escapeXml(time)}</Data></Cell>
    <Cell ss:StyleID="CellLeft"><Data ss:Type="String">${escapeXml(cusName)}</Data></Cell>
    <Cell ss:StyleID="CellCenter"><Data ss:Type="String">${escapeXml(cusPhone)}</Data></Cell>
    <Cell ss:StyleID="CellLeft"><Data ss:Type="String">${escapeXml(cusAddr)}</Data></Cell>
    <Cell ss:StyleID="CellLeft"><Data ss:Type="String">${escapeXml(itemsText)}</Data></Cell>
    <Cell ss:StyleID="CellNumber"><Data ss:Type="Number">${numPrice}</Data></Cell>
    <Cell ss:StyleID="CellLeft"><Data ss:Type="String">${escapeXml(payMethod)}</Data></Cell>
    <Cell ss:StyleID="CellCenter"><Data ss:Type="String">${escapeXml(status)}</Data></Cell>
    <Cell ss:StyleID="CellLeft"><Data ss:Type="String">${escapeXml(shipper)}</Data></Cell>
   </Row>`;
    });

    xml += `
   <Row ss:Height="24">
    <Cell ss:MergeAcross="6" ss:StyleID="TotalLabel"><Data ss:Type="String">TỔNG CỘNG DOANH THU ĐƠN HÀNG</Data></Cell>
    <Cell ss:StyleID="TotalRow"><Data ss:Type="Number">${totalRev}</Data></Cell>
    <Cell ss:MergeAcross="2" ss:StyleID="TotalRow"><Data ss:Type="String"></Data></Cell>
   </Row>`;
  }

  xml += `
  </Table>
 </Worksheet>
</Workbook>`;

  const fileName = `ZoneMart_DonHang_${cleanStore}_${fileDate}.xls`;

  if (typeof window !== "undefined" && typeof document !== "undefined") {
    const blob = new Blob(["\uFEFF" + xml], { type: "application/vnd.ms-excel;charset=utf-8;" });
    const url = URL.createObjectURL(blob);
    const a = document.createElement("a");
    a.href = url;
    a.download = fileName;
    document.body.appendChild(a);
    a.click();
    document.body.removeChild(a);
    URL.revokeObjectURL(url);
  }

  return { isTemplate, count: list.length, fileName };
}

