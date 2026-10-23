/**
 * =======================================================================
 * DỮ LIỆU ĐỊA GIỚI HÀNH CHÍNH & CHỢ TRUYỀN THỐNG CHUẨN TOÀN THÀNH PHỐ HÀ NỘI
 * Đầy đủ 30 Quận / Huyện / Thị xã và hơn 500 Xã / Phường / Thị trấn
 * =======================================================================
 */

export interface WardItem {
  id: string;
  wardCode: string;
  wardName: string;
  districtName: string;
}

export interface MarketItem {
  id: string;
  marketCode: string;
  marketName: string;
  wardId?: string;
  wardName?: string;
  districtName: string;
  address: string;
  totalStalls: number;
  managementBoardName?: string;
}

// 12 Quận nội thành Hà Nội
export const INNER_DISTRICTS = [
  "Quận Ba Đình",
  "Quận Hoàn Kiếm",
  "Quận Đống Đa",
  "Quận Hai Bà Trưng",
  "Quận Tây Hồ",
  "Quận Thanh Xuân",
  "Quận Hoàng Mai",
  "Quận Long Biên",
  "Quận Nam Từ Liêm",
  "Quận Bắc Từ Liêm",
  "Quận Hà Đông"
];

// 18 Huyện & Thị xã ngoại thành Hà Nội
export const OUTER_DISTRICTS = [
  "Thị xã Sơn Tây",
  "Huyện Ba Vì",
  "Huyện Chương Mỹ",
  "Huyện Đan Phượng",
  "Huyện Đông Anh",
  "Huyện Gia Lâm",
  "Huyện Hoài Đức",
  "Huyện Mê Linh",
  "Huyện Mỹ Đức",
  "Huyện Phú Xuyên",
  "Huyện Phúc Thọ",
  "Huyện Quốc Oai",
  "Huyện Sóc Sơn",
  "Huyện Thạch Thất",
  "Huyện Thanh Oai",
  "Huyện Thanh Trì",
  "Huyện Thường Tín",
  "Huyện Ứng Hòa"
];

export const ALL_HANOI_DISTRICTS = [
  "Quận Cầu Giấy",
  ...INNER_DISTRICTS,
  ...OUTER_DISTRICTS
];

// Bản đồ toàn bộ Xã / Phường theo từng Quận / Huyện của Hà Nội
export const HANOI_WARDS_RAW: Record<string, string[]> = {
  "Quận Cầu Giấy": [
    "Phường Dịch Vọng Hậu",
    "Phường Dịch Vọng",
    "Phường Nghĩa Tân",
    "Phường Mai Dịch",
    "Phường Quan Hoa",
    "Phường Nghĩa Đô",
    "Phường Yên Hòa",
    "Phường Trung Hòa"
  ],
  "Quận Ba Đình": [
    "Phường Cống Vị",
    "Phường Điện Biên",
    "Phường Đội Cấn",
    "Phường Giảng Võ",
    "Phường Kim Mã",
    "Phường Liễu Giai",
    "Phường Ngọc Hà",
    "Phường Ngọc Khánh",
    "Phường Nguyễn Trung Trực",
    "Phường Phúc Xá",
    "Phường Quán Thánh",
    "Phường Thành Công",
    "Phường Trúc Bạch",
    "Phường Vĩnh Phúc"
  ],
  "Quận Hoàn Kiếm": [
    "Phường Chương Dương",
    "Phường Cửa Đông",
    "Phường Cửa Nam",
    "Phường Đồng Xuân",
    "Phường Hàng Bạc",
    "Phường Hàng Bài",
    "Phường Hàng Bồ",
    "Phường Hàng Bông",
    "Phường Hàng Buồm",
    "Phường Hàng Đào",
    "Phường Hàng Gai",
    "Phường Hàng Mã",
    "Phường Hàng Trống",
    "Phường Lý Thái Tổ",
    "Phường Phan Chu Trinh",
    "Phường Phúc Tân",
    "Phường Tràng Tiền",
    "Phường Trần Hưng Đạo"
  ],
  "Quận Đống Đa": [
    "Phường Cát Linh",
    "Phường Hàng Bột",
    "Phường Khâm Thiên",
    "Phường Khương Thượng",
    "Phường Kim Liên",
    "Phường Láng Hạ",
    "Phường Láng Thượng",
    "Phường Nam Đồng",
    "Phường Ngã Tư Sở",
    "Phường Ô Chợ Dừa",
    "Phường Phương Liên",
    "Phường Phương Mai",
    "Phường Quang Trung",
    "Phường Quốc Tử Giám",
    "Phường Thịnh Quang",
    "Phường Thổ Quan",
    "Phường Trung Liệt",
    "Phường Trung Phụng",
    "Phường Trung Tự",
    "Phường Văn Chương",
    "Phường Văn Miếu"
  ],
  "Quận Hai Bà Trưng": [
    "Phường Bạch Đằng",
    "Phường Bách Khoa",
    "Phường Bạch Mai",
    "Phường Cầu Dền",
    "Phường Đống Mác",
    "Phường Đồng Nhân",
    "Phường Đồng Tâm",
    "Phường Lê Đại Hành",
    "Phường Minh Khai",
    "Phường Nguyễn Du",
    "Phường Phạm Đình Hổ",
    "Phường Phố Huế",
    "Phường Quỳnh Lôi",
    "Phường Quỳnh Mai",
    "Phường Thanh Lương",
    "Phường Thanh Nhàn",
    "Phường Trương Định",
    "Phường Vĩnh Tuy"
  ],
  "Quận Tây Hồ": [
    "Phường Bưởi",
    "Phường Nhật Tân",
    "Phường Phú Thượng",
    "Phường Quảng An",
    "Phường Thụy Khuê",
    "Phường Tứ Liên",
    "Phường Xuân La",
    "Phường Yên Phụ"
  ],
  "Quận Thanh Xuân": [
    "Phường Hạ Đình",
    "Phường Khương Đình",
    "Phường Khương Mai",
    "Phường Khương Trung",
    "Phường Kim Giang",
    "Phường Nhân Chính",
    "Phường Phương Liệt",
    "Phường Thanh Xuân Bắc",
    "Phường Thanh Xuân Nam",
    "Phường Thanh Xuân Trung",
    "Phường Thượng Đình"
  ],
  "Quận Hoàng Mai": [
    "Phường Đại Kim",
    "Phường Định Công",
    "Phường Giáp Bát",
    "Phường Hoàng Liệt",
    "Phường Hoàng Văn Thụ",
    "Phường Lĩnh Nam",
    "Phường Mai Động",
    "Phường Tân Mai",
    "Phường Thanh Trì",
    "Phường Thịnh Liệt",
    "Phường Trần Phú",
    "Phường Tương Mai",
    "Phường Vĩnh Hưng",
    "Phường Yên Sở"
  ],
  "Quận Long Biên": [
    "Phường Bồ Đề",
    "Phường Cự Khối",
    "Phường Đức Giang",
    "Phường Gia Thụy",
    "Phường Giang Biên",
    "Phường Long Biên",
    "Phường Ngọc Lâm",
    "Phường Ngọc Thụy",
    "Phường Phúc Đồng",
    "Phường Phúc Lợi",
    "Phường Sài Đồng",
    "Phường Thạch Bàn",
    "Phường Thượng Thanh",
    "Phường Việt Hưng"
  ],
  "Quận Nam Từ Liêm": [
    "Phường Cầu Diễn",
    "Phường Đại Mỗ",
    "Phường Mễ Trì",
    "Phường Mỹ Đình 1",
    "Phường Mỹ Đình 2",
    "Phường Phú Đô",
    "Phường Phương Canh",
    "Phường Tây Mỗ",
    "Phường Trung Văn",
    "Phường Xuân Phương"
  ],
  "Quận Bắc Từ Liêm": [
    "Phường Cổ Nhuế 1",
    "Phường Cổ Nhuế 2",
    "Phường Đông Ngạc",
    "Phường Đức Thắng",
    "Phường Liên Mạc",
    "Phường Minh Khai",
    "Phường Phú Diễn",
    "Phường Phúc Diễn",
    "Phường Tây Tựu",
    "Phường Thượng Cát",
    "Phường Thụy Phương",
    "Phường Xuân Đỉnh",
    "Phường Xuân Tảo"
  ],
  "Quận Hà Đông": [
    "Phường Biên Giang",
    "Phường Đồng Mai",
    "Phường Dương Nội",
    "Phường Hà Cầu",
    "Phường Kiến Hưng",
    "Phường La Khê",
    "Phường Mộ Lao",
    "Phường Nguyễn Trãi",
    "Phường Phú La",
    "Phường Phú Lãm",
    "Phường Phú Lương",
    "Phường Phúc La",
    "Phường Quang Trung",
    "Phường Vạn Phúc",
    "Phường Văn Quán",
    "Phường Yên Nghĩa",
    "Phường Yết Kiêu"
  ],
  "Thị xã Sơn Tây": [
    "Phường Lê Lợi",
    "Phường Ngô Quyền",
    "Phường Phú Thịnh",
    "Phường Quang Trung",
    "Phường Sơn Lộc",
    "Phường Trung Hưng",
    "Phường Trung Sơn Trầm",
    "Phường Viên Sơn",
    "Phường Xuân Khanh",
    "Xã Cổ Đông",
    "Xã Đường Lâm",
    "Xã Kim Sơn",
    "Xã Sơn Đông",
    "Xã Thanh Mỹ",
    "Xã Xuân Sơn"
  ],
  "Huyện Ba Vì": [
    "Thị trấn Tây Đằng",
    "Xã Ba Trại",
    "Xã Ba Vì",
    "Xã Cẩm Lĩnh",
    "Xã Cam Thượng",
    "Xã Châu Sơn",
    "Xã Chu Minh",
    "Xã Cổ Đô",
    "Xã Đông Quang",
    "Xã Đồng Thái",
    "Xã Khánh Thượng",
    "Xã Minh Châu",
    "Xã Minh Quang",
    "Xã Phong Vân",
    "Xã Phú Châu",
    "Xã Phú Cường",
    "Xã Phú Đông",
    "Xã Phú Phương",
    "Xã Phú Sơn",
    "Xã Sơn Đà",
    "Xã Tản Hồng",
    "Xã Tản Lĩnh",
    "Xã Thái Hòa",
    "Xã Thuần Mỹ",
    "Xã Thụy An",
    "Xã Tiên Phong",
    "Xã Tòng Bạt",
    "Xã Vân Hòa",
    "Xã Vạn Thắng",
    "Xã Vật Lại",
    "Xã Yên Bài"
  ],
  "Huyện Chương Mỹ": [
    "Thị trấn Chúc Sơn",
    "Thị trấn Xuân Mai",
    "Xã Đại Yên",
    "Xã Đông Phương Yên",
    "Xã Đông Sơn",
    "Xã Đồng Lạc",
    "Xã Đồng Phú",
    "Xã Hòa Chính",
    "Xã Hoàng Diệu",
    "Xã Hoàng Văn Thụ",
    "Xã Hợp Đồng",
    "Xã Hữu Văn",
    "Xã Lam Điền",
    "Xã Mỹ Lương",
    "Xã Nam Phương Tiến",
    "Xã Ngọc Hòa",
    "Xã Phú Nam An",
    "Xã Phú Nghĩa",
    "Xã Phụng Châu",
    "Xã Quảng Bị",
    "Xã Tân Tiến",
    "Xã Tiên Phương",
    "Xã Tốt Động",
    "Xã Thanh Bình",
    "Xã Thủy Xuân Tiên",
    "Xã Thụy Hương",
    "Xã Thượng Vực",
    "Xã Trần Phú",
    "Xã Trung Hòa",
    "Xã Trường Yên",
    "Xã Văn Võ"
  ],
  "Huyện Đan Phượng": [
    "Thị trấn Phùng",
    "Xã Đan Phượng",
    "Xã Đồng Tháp",
    "Xã Hạ Mỗ",
    "Xã Hồng Hà",
    "Xã Liên Hà",
    "Xã Liên Hồng",
    "Xã Liên Trung",
    "Xã Phương Đình",
    "Xã Song Phượng",
    "Xã Tân Hội",
    "Xã Tân Lập",
    "Xã Thọ An",
    "Xã Thọ Xuân",
    "Xã Thượng Mỗ",
    "Xã Trung Châu"
  ],
  "Huyện Đông Anh": [
    "Thị trấn Đông Anh",
    "Xã Bắc Hồng",
    "Xã Cổ Loa",
    "Xã Dục Tú",
    "Xã Đại Mạch",
    "Xã Đông Hội",
    "Xã Hải Bối",
    "Xã Kim Chung",
    "Xã Kim Nỗ",
    "Xã Liên Hà",
    "Xã Mai Lâm",
    "Xã Nam Hồng",
    "Xã Nguyên Khê",
    "Xã Tàm Xá",
    "Xã Thụy Lâm",
    "Xã Tiên Dương",
    "Xã Uy Nỗ",
    "Xã Vân Hà",
    "Xã Vân Nội",
    "Xã Việt Hùng",
    "Xã Vĩnh Ngọc",
    "Xã Võng La",
    "Xã Xuân Canh",
    "Xã Xuân Nộn"
  ],
  "Huyện Gia Lâm": [
    "Thị trấn Trâu Quỳ",
    "Thị trấn Yên Viên",
    "Xã Bát Tràng",
    "Xã Cổ Bi",
    "Xã Dương Hà",
    "Xã Dương Quang",
    "Xã Dương Xá",
    "Xã Đa Tốn",
    "Xã Đặng Xá",
    "Xã Đình Xuyên",
    "Xã Đông Dư",
    "Xã Kiêu Kỵ",
    "Xã Kim Lan",
    "Xã Kim Sơn",
    "Xã Lệ Chi",
    "Xã Ninh Hiệp",
    "Xã Phù Đổng",
    "Xã Phú Thị",
    "Xã Trung Mầu",
    "Xã Văn Đức",
    "Xã Yên Thường",
    "Xã Yên Viên"
  ],
  "Huyện Hoài Đức": [
    "Thị trấn Trạm Trôi",
    "Xã An Khánh",
    "Xã An Thượng",
    "Xã Cát Quế",
    "Xã Đắc Sở",
    "Xã Di Trạch",
    "Xã Đông La",
    "Xã Đức Giang",
    "Xã Đức Thượng",
    "Xã Kim Chung",
    "Xã La Phù",
    "Xã Lại Yên",
    "Xã Minh Khai",
    "Xã Song Phương",
    "Xã Sơn Đồng",
    "Xã Tiền Yên",
    "Xã Vân Canh",
    "Xã Vân Côn",
    "Xã Yên Sở"
  ],
  "Huyện Mê Linh": [
    "Thị trấn Chi Đông",
    "Thị trấn Quang Minh",
    "Xã Chu Phan",
    "Xã Đại Thịnh",
    "Xã Hoàng Kim",
    "Xã Kim Hoa",
    "Xã Liên Mạc",
    "Xã Mê Linh",
    "Xã Tam Đồng",
    "Xã Thạch Đà",
    "Xã Thanh Lâm",
    "Xã Tiền Phong",
    "Xã Tiến Thắng",
    "Xã Tiến Thịnh",
    "Xã Tự Lập",
    "Xã Tráng Việt",
    "Xã Văn Khê",
    "Xã Vạn Yên"
  ],
  "Huyện Mỹ Đức": [
    "Thị trấn Đại Nghĩa",
    "Xã An Mỹ",
    "Xã An Phú",
    "Xã An Tiến",
    "Xã Bột Xuyên",
    "Xã Đại Hưng",
    "Xã Đốc Tín",
    "Xã Đồng Tâm",
    "Xã Hồng Sơn",
    "Xã Hợp Thanh",
    "Xã Hợp Tiến",
    "Xã Hùng Tiến",
    "Xã Hương Sơn",
    "Xã Lê Thanh",
    "Xã Mỹ Thành",
    "Xã Phù Lưu Tế",
    "Xã Phúc Lâm",
    "Xã Phùng Xá",
    "Xã Thượng Lâm",
    "Xã Tuy Lai",
    "Xã Vạn Kim",
    "Xã Xuy Xá"
  ],
  "Huyện Phú Xuyên": [
    "Thị trấn Phú Xuyên",
    "Thị trấn Phú Minh",
    "Xã Bạch Hạ",
    "Xã Châu Can",
    "Xã Chuyên Mỹ",
    "Xã Đại Thắng",
    "Xã Đại Xuyên",
    "Xã Hoàng Long",
    "Xã Hồng Minh",
    "Xã Hồng Thái",
    "Xã Khai Thái",
    "Xã Nam Phong",
    "Xã Nam Triều",
    "Xã Phú Túc",
    "Xã Phú Yên",
    "Xã Phúc Tiến",
    "Xã Phượng Dực",
    "Xã Quang Lãng",
    "Xã Quang Trung",
    "Xã Sơn Hà",
    "Xã Tân Dân",
    "Xã Thụy Phú",
    "Xã Tri Thủy",
    "Xã Tri Trung",
    "Xã Văn Hoàng",
    "Xã Vân Từ"
  ],
  "Huyện Phúc Thọ": [
    "Thị trấn Phúc Thọ",
    "Xã Hát Môn",
    "Xã Hiệp Thuận",
    "Xã Liên Hiệp",
    "Xã Long Xuyên",
    "Xã Ngọc Tảo",
    "Xã Phúc Hòa",
    "Xã Phụng Thượng",
    "Xã Sen Phương",
    "Xã Tam Hiệp",
    "Xã Tam Thuấn",
    "Xã Thanh Đa",
    "Xã Thọ Lộc",
    "Xã Thượng Cốc",
    "Xã Tích Giang",
    "Xã Trạch Mỹ Lộc",
    "Xã Vân Hà",
    "Xã Vân Nam",
    "Xã Vân Phúc",
    "Xã Võng Xuyên",
    "Xã Xuân Đình"
  ],
  "Huyện Quốc Oai": [
    "Thị trấn Quốc Oai",
    "Xã Cấn Hữu",
    "Xã Cộng Hòa",
    "Xã Đại Thành",
    "Xã Đồng Quang",
    "Xã Đông Yên",
    "Xã Hòa Thạch",
    "Xã Liệp Tuyết",
    "Xã Nghĩa Hương",
    "Xã Ngọc Liệp",
    "Xã Ngọc Mỹ",
    "Xã Phú Cát",
    "Xã Phú Mãn",
    "Xã Phượng Cách",
    "Xã Sài Sơn",
    "Xã Tân Hòa",
    "Xã Tân Phú",
    "Xã Thạch Thán",
    "Xã Tuyết Nghĩa",
    "Xã Yên Sơn",
    "Xã Đông Xuân"
  ],
  "Huyện Sóc Sơn": [
    "Thị trấn Sóc Sơn",
    "Xã Bắc Phú",
    "Xã Bắc Sơn",
    "Xã Đông Xuân",
    "Xã Đức Hòa",
    "Xã Hiền Ninh",
    "Xã Hồng Kỳ",
    "Xã Kim Lũ",
    "Xã Mai Đình",
    "Xã Minh Phú",
    "Xã Minh Trí",
    "Xã Nam Sơn",
    "Xã Phú Cường",
    "Xã Phù Linh",
    "Xã Phù Lỗ",
    "Xã Phú Minh",
    "Xã Quang Tiến",
    "Xã Tân Dân",
    "Xã Tân Hưng",
    "Xã Tân Minh",
    "Xã Tiên Dược",
    "Xã Trung Giã",
    "Xã Việt Long",
    "Xã Xuân Giang",
    "Xã Xuân Thu"
  ],
  "Huyện Thạch Thất": [
    "Thị trấn Liên Quan",
    "Xã Bình Phú",
    "Xã Bình Yên",
    "Xã Cẩm Yên",
    "Xã Cần Kiệm",
    "Xã Canh Nậu",
    "Xã Chàng Sơn",
    "Xã Đại Đồng",
    "Xã Dị Nậu",
    "Xã Đồng Trúc",
    "Xã Hạ Bằng",
    "Xã Hương Ngải",
    "Xã Hữu Bằng",
    "Xã Kim Quan",
    "Xã Lại Thượng",
    "Xã Phú Kim",
    "Xã Phùng Xá",
    "Xã Tân Xã",
    "Xã Thạch Hòa",
    "Xã Thạch Xá",
    "Xã Tiến Xuân",
    "Xã Yên Bình",
    "Xã Yên Trung"
  ],
  "Huyện Thanh Oai": [
    "Thị trấn Kim Bài",
    "Xã Bích Hòa",
    "Xã Bình Minh",
    "Xã Cao Dương",
    "Xã Cao Viên",
    "Xã Cự Khê",
    "Xã Dân Hòa",
    "Xã Đỗ Động",
    "Xã Hồng Dương",
    "Xã Kim An",
    "Xã Kim Thư",
    "Xã Liên Châu",
    "Xã Mỹ Hưng",
    "Xã Phương Trung",
    "Xã Tam Hưng",
    "Xã Tân Ước",
    "Xã Thanh Cao",
    "Xã Thanh Mai",
    "Xã Thanh Thùy",
    "Xã Thanh Văn",
    "Xã Xuân Dương"
  ],
  "Huyện Thanh Trì": [
    "Thị trấn Văn Điển",
    "Xã Đại Áng",
    "Xã Đông Mỹ",
    "Xã Duyên Hà",
    "Xã Hữu Hòa",
    "Xã Liên Ninh",
    "Xã Ngọc Hồi",
    "Xã Ngũ Hiệp",
    "Xã Tả Thanh Oai",
    "Xã Tam Hiệp",
    "Xã Tân Triều",
    "Xã Thanh Liệt",
    "Xã Tứ Hiệp",
    "Xã Vạn Phúc",
    "Xã Vĩnh Quỳnh",
    "Xã Yên Mỹ"
  ],
  "Huyện Thường Tín": [
    "Thị trấn Thường Tín",
    "Xã Chương Dương",
    "Xã Dũng Tiến",
    "Xã Duyên Thái",
    "Xã Hà Hồi",
    "Xã Hiền Giang",
    "Xã Hòa Bình",
    "Xã Khánh Hà",
    "Xã Hồng Vân",
    "Xã Lê Lợi",
    "Xã Liên Phương",
    "Xã Minh Cường",
    "Xã Nghiêm Xuyên",
    "Xã Nguyễn Trãi",
    "Xã Nhị Khê",
    "Xã Ninh Sở",
    "Xã Quất Động",
    "Xã Tân Minh",
    "Xã Thắng Lợi",
    "Xã Thống Nhất",
    "Xã Thư Phú",
    "Xã Tiền Phong",
    "Xã Tô Hiệu",
    "Xã Tự Nhiên",
    "Xã Vạn Điểm",
    "Xã Văn Bình",
    "Xã Văn Phú",
    "Xã Văn Tự",
    "Xã Vân Tảo"
  ],
  "Huyện Ứng Hòa": [
    "Thị trấn Vân Đình",
    "Xã Cao Thành",
    "Xã Đại Cường",
    "Xã Đại Hùng",
    "Xã Đội Bình",
    "Xã Đông Lỗ",
    "Xã Hoa Đường",
    "Xã Hòa Lâm",
    "Xã Hòa Nam",
    "Xã Hòa Phú",
    "Xã Hòa Xá",
    "Xã Hồng Quang",
    "Xã Kim Đường",
    "Xã Liên Bạt",
    "Xã Lưu Hoàng",
    "Xã Minh Đức",
    "Xã Phù Lưu",
    "Xã Phương Tú",
    "Xã Quảng Phú Cầu",
    "Xã Tảo Dương Văn",
    "Xã Trầm Lộng",
    "Xã Trung Tú",
    "Xã Trường Thịnh",
    "Xã Vạn Thái",
    "Xã Viên An",
    "Xã Viên Nội"
  ]
};

// Tạo danh sách WardItem chuẩn cho toàn bộ hệ thống
export const ALL_HANOI_WARDS: WardItem[] = [];

// Seed Cầu Giấy từ MongoDB Atlas IDs nếu có
const CAU_GIAY_DB_IDS: Record<string, string> = {
  "Phường Dịch Vọng Hậu": "6ac9efa0fe18a75d21e7a5a4",
  "Phường Dịch Vọng": "6ad71cbbc474b4d1ceae36bc",
  "Phường Nghĩa Tân": "6ad71cbbc474b4d1ceae36bb",
  "Phường Mai Dịch": "6ad71cbbc474b4d1ceae36bd",
  "Phường Quan Hoa": "6ad71cbcc474b4d1ceae36c0",
  "Phường Nghĩa Đô": "6ad71cbcc474b4d1ceae36c2",
  "Phường Yên Hòa": "6ad71cbcc474b4d1ceae36c4",
  "Phường Trung Hòa": "6ad71cbcc474b4d1ceae36c6",
};

for (const [distName, wardNames] of Object.entries(HANOI_WARDS_RAW)) {
  for (const wName of wardNames) {
    const slug = wName
      .toLowerCase()
      .normalize("NFD")
      .replace(/[\u0300-\u036f]/g, "")
      .replace(/[^a-z0-9]/g, "_");
    const dSlug = distName
      .toLowerCase()
      .normalize("NFD")
      .replace(/[\u0300-\u036f]/g, "")
      .replace(/[^a-z0-9]/g, "_");

    const id = CAU_GIAY_DB_IDS[wName] || `w_${dSlug}_${slug}`;
    const wardCode = `WARD_${dSlug}_${slug}`.toUpperCase();

    ALL_HANOI_WARDS.push({
      id,
      wardCode,
      wardName: wName,
      districtName: distName
    });
  }
}

// Danh sách chợ truyền thống & chợ số nổi tiếng toàn thành phố Hà Nội
export const ALL_HANOI_MARKETS: MarketItem[] = [
  // Cầu Giấy (10 cụm chợ chuẩn DB)
  { id: "6ac9efa0fe18a75d21e7a5a7", marketCode: "MKT_A", marketName: "Chợ A (Chợ Nhà Xanh)", wardId: "6ac9efa0fe18a75d21e7a5a4", wardName: "Phường Dịch Vọng Hậu", districtName: "Quận Cầu Giấy", address: "Phố Phan Văn Trường, Dịch Vọng Hậu, Cầu Giấy", totalStalls: 120, managementBoardName: "Ban quản lý chợ số 01" },
  { id: "6ad71c8c48c1f7a7ed524d58", marketCode: "MKT_DVH", marketName: "Chợ Dịch Vọng Hậu", wardId: "6ac9efa0fe18a75d21e7a5a4", wardName: "Phường Dịch Vọng Hậu", districtName: "Quận Cầu Giấy", address: "Đường Trần Thái Tông, Dịch Vọng Hậu, Cầu Giấy", totalStalls: 60, managementBoardName: "Ban quản lý chợ số 01" },
  { id: "6ad71cbbc474b4d1ceae36bb", marketCode: "MKT_B", marketName: "Chợ B (Chợ Nghĩa Tân)", wardId: "6ad71cbbc474b4d1ceae36bb", wardName: "Phường Nghĩa Tân", districtName: "Quận Cầu Giấy", address: "Phố Tô Hiệu, Nghĩa Tân, Cầu Giấy", totalStalls: 85, managementBoardName: "Ban quản lý chợ số 01" },
  { id: "6ad71cbbc474b4d1ceae36bc", marketCode: "MKT_C", marketName: "Chợ C (Chợ Dịch Vọng)", wardId: "6ad71cbbc474b4d1ceae36bc", wardName: "Phường Dịch Vọng", districtName: "Quận Cầu Giấy", address: "Đường Cầu Giấy, Dịch Vọng, Cầu Giấy", totalStalls: 60, managementBoardName: "Ban quản lý chợ số 02" },
  { id: "6ad71cbcc474b4d1ceae36be", marketCode: "MKT_DONG_XA", marketName: "Chợ Đồng Xa", wardId: "6ad71cbbc474b4d1ceae36bd", wardName: "Phường Mai Dịch", districtName: "Quận Cầu Giấy", address: "Phố Trần Bình, Mai Dịch, Cầu Giấy", totalStalls: 95, managementBoardName: "Ban quản lý chợ số 02" },
  { id: "6ad71cbcc474b4d1ceae36bf", marketCode: "MKT_MAI_DICH", marketName: "Chợ Dân Sinh Mai Dịch", wardId: "6ad71cbbc474b4d1ceae36bd", wardName: "Phường Mai Dịch", districtName: "Quận Cầu Giấy", address: "Đường Hồ Tùng Mậu, Mai Dịch, Cầu Giấy", totalStalls: 70, managementBoardName: "Ban quản lý chợ số 02" },
  { id: "6ad71cbcc474b4d1ceae36c1", marketCode: "MKT_CAU_GIAY", marketName: "Chợ Cầu Giấy", wardId: "6ad71cbcc474b4d1ceae36c0", wardName: "Phường Quan Hoa", districtName: "Quận Cầu Giấy", address: "Đường Quan Hoa, Cầu Giấy", totalStalls: 80, managementBoardName: "Ban quản lý chợ số 01" },
  { id: "6ad71cbcc474b4d1ceae36c3", marketCode: "MKT_NGHIA_DO", marketName: "Chợ Nghĩa Đô", wardId: "6ad71cbcc474b4d1ceae36c2", wardName: "Phường Nghĩa Đô", districtName: "Quận Cầu Giấy", address: "Đường Lạc Long Quân, Nghĩa Đô, Cầu Giấy", totalStalls: 65, managementBoardName: "Ban quản lý chợ số 01" },
  { id: "6ad71cbcc474b4d1ceae36c5", marketCode: "MKT_TRUNG_KINH", marketName: "Chợ Trung Kính - Yên Hòa", wardId: "6ad71cbcc474b4d1ceae36c4", wardName: "Phường Yên Hòa", districtName: "Quận Cầu Giấy", address: "Phố Trung Kính, Yên Hòa, Cầu Giấy", totalStalls: 90, managementBoardName: "Ban quản lý chợ số 02" },
  { id: "6ad71cbcc474b4d1ceae36c7", marketCode: "MKT_TRUNG_HOA", marketName: "Chợ Trung Hòa (Cụm Chợ Số Trung Hòa)", wardId: "6ad71cbcc474b4d1ceae36c6", wardName: "Phường Trung Hòa", districtName: "Quận Cầu Giấy", address: "Phố Nguyễn Thị Định, Trung Hòa, Cầu Giấy", totalStalls: 75, managementBoardName: "Ban quản lý chợ số 02" },

  // Ba Đình
  { id: "m_bd_thanhcong", marketCode: "MKT_BD_TC", marketName: "Chợ Thành Công", wardName: "Phường Thành Công", districtName: "Quận Ba Đình", address: "Khu tập thể Thành Công, Ba Đình, Hà Nội", totalStalls: 150, managementBoardName: "Ban quản lý chợ Ba Đình" },
  { id: "m_bd_chaulong", marketCode: "MKT_BD_CL", marketName: "Chợ Châu Long", wardName: "Phường Trúc Bạch", districtName: "Quận Ba Đình", address: "Phố Châu Long, Trúc Bạch, Ba Đình, Hà Nội", totalStalls: 110, managementBoardName: "Ban quản lý chợ Ba Đình" },
  { id: "m_bd_congvi", marketCode: "MKT_BD_CV", marketName: "Chợ Cống Vị", wardName: "Phường Cống Vị", districtName: "Quận Ba Đình", address: "Đường Đội Cấn, Cống Vị, Ba Đình, Hà Nội", totalStalls: 95, managementBoardName: "Ban quản lý chợ Ba Đình" },
  { id: "m_bd_ngocha", marketCode: "MKT_BD_NH", marketName: "Chợ Ngọc Hà", wardName: "Phường Ngọc Hà", districtName: "Quận Ba Đình", address: "Phố Hoàng Hoa Thám, Ngọc Hà, Ba Đình, Hà Nội", totalStalls: 80, managementBoardName: "Ban quản lý chợ Ba Đình" },
  { id: "m_bd_phucxa", marketCode: "MKT_BD_PX", marketName: "Chợ Phúc Xá", wardName: "Phường Phúc Xá", districtName: "Quận Ba Đình", address: "Phố Phúc Xá, Ba Đình, Hà Nội", totalStalls: 120, managementBoardName: "Ban quản lý chợ Ba Đình" },

  // Hoàn Kiếm
  { id: "m_hk_dongxuan", marketCode: "MKT_HK_DX", marketName: "Chợ Đồng Xuân", wardName: "Phường Đồng Xuân", districtName: "Quận Hoàn Kiếm", address: "Phố Đồng Xuân, Hoàn Kiếm, Hà Nội", totalStalls: 350, managementBoardName: "Công ty CP Đồng Xuân" },
  { id: "m_hk_hangbe", marketCode: "MKT_HK_HB", marketName: "Chợ Hàng Bè", wardName: "Phường Hàng Bạc", districtName: "Quận Hoàn Kiếm", address: "Phố Hàng Bè, Hàng Bạc, Hoàn Kiếm, Hà Nội", totalStalls: 90, managementBoardName: "Ban quản lý chợ Hoàn Kiếm" },
  { id: "m_hk_cuanam", marketCode: "MKT_HK_CN", marketName: "Chợ Cửa Nam", wardName: "Phường Cửa Nam", districtName: "Quận Hoàn Kiếm", address: "Phố Cửa Nam, Hoàn Kiếm, Hà Nội", totalStalls: 80, managementBoardName: "Ban quản lý chợ Hoàn Kiếm" },

  // Đống Đa
  { id: "m_dd_kimlien", marketCode: "MKT_DD_KL", marketName: "Chợ Kim Liên", wardName: "Phường Kim Liên", districtName: "Quận Đống Đa", address: "Phố Lương Định Của, Kim Liên, Đống Đa, Hà Nội", totalStalls: 140, managementBoardName: "Ban quản lý chợ Đống Đa" },
  { id: "m_dd_namdong", marketCode: "MKT_DD_ND", marketName: "Chợ Nam Đồng", wardName: "Phường Nam Đồng", districtName: "Quận Đống Đa", address: "Phố Hồ Đắc Di, Nam Đồng, Đống Đa, Hà Nội", totalStalls: 90, managementBoardName: "Ban quản lý chợ Đống Đa" },
  { id: "m_dd_thaiha", marketCode: "MKT_DD_TH", marketName: "Chợ Thái Hà", wardName: "Phường Trung Liệt", districtName: "Quận Đống Đa", address: "Phố Thái Hà, Trung Liệt, Đống Đa, Hà Nội", totalStalls: 110, managementBoardName: "Ban quản lý chợ Đống Đa" },
  { id: "m_dd_khamthien", marketCode: "MKT_DD_KT", marketName: "Chợ Khâm Thiên", wardName: "Phường Khâm Thiên", districtName: "Quận Đống Đa", address: "Phố Khâm Thiên, Đống Đa, Hà Nội", totalStalls: 85, managementBoardName: "Ban quản lý chợ Đống Đa" },
  { id: "m_dd_ngatuso", marketCode: "MKT_DD_NTS", marketName: "Chợ Ngã Tư Sở", wardName: "Phường Ngã Tư Sở", districtName: "Quận Đống Đa", address: "Đường Tây Sơn, Ngã Tư Sở, Đống Đa, Hà Nội", totalStalls: 180, managementBoardName: "Ban quản lý chợ Đống Đa" },

  // Hai Bà Trưng
  { id: "m_hbt_chohom", marketCode: "MKT_HBT_CH", marketName: "Chợ Hôm - Đức Viên", wardName: "Phường Phố Huế", districtName: "Quận Hai Bà Trưng", address: "Phố Huế, Hai Bà Trưng, Hà Nội", totalStalls: 220, managementBoardName: "Ban quản lý chợ Hai Bà Trưng" },
  { id: "m_hbt_chomo", marketCode: "MKT_HBT_CM", marketName: "Chợ Mơ (TTTM Chợ Mơ)", wardName: "Phường Trương Định", districtName: "Quận Hai Bà Trưng", address: "Phố Bạch Mai, Trương Định, Hai Bà Trưng, Hà Nội", totalStalls: 280, managementBoardName: "Ban quản lý chợ Mơ" },
  { id: "m_hbt_nguyencongtru", marketCode: "MKT_HBT_NCT", marketName: "Chợ Nguyễn Công Trứ", wardName: "Phường Phố Huế", districtName: "Quận Hai Bà Trưng", address: "Khu tập thể Nguyễn Công Trứ, Hai Bà Trưng, Hà Nội", totalStalls: 110, managementBoardName: "Ban quản lý chợ Hai Bà Trưng" },

  // Thanh Xuân
  { id: "m_tx_nhanchinh", marketCode: "MKT_TX_NC", marketName: "Chợ Nhân Chính", wardName: "Phường Nhân Chính", districtName: "Quận Thanh Xuân", address: "Đường Quan Nhân, Nhân Chính, Thanh Xuân, Hà Nội", totalStalls: 130, managementBoardName: "Ban quản lý chợ Thanh Xuân" },
  { id: "m_tx_thuongdinh", marketCode: "MKT_TX_TD", marketName: "Chợ Thượng Đình", wardName: "Phường Thượng Đình", districtName: "Quận Thanh Xuân", address: "Đường Nguyễn Trãi, Thượng Đình, Thanh Xuân, Hà Nội", totalStalls: 110, managementBoardName: "Ban quản lý chợ Thanh Xuân" },
  { id: "m_tx_khuongdinh", marketCode: "MKT_TX_KD", marketName: "Chợ Khương Đình", wardName: "Phường Khương Đình", districtName: "Quận Thanh Xuân", address: "Phố Khương Hạ, Khương Đình, Thanh Xuân, Hà Nội", totalStalls: 85, managementBoardName: "Ban quản lý chợ Thanh Xuân" },

  // Nam Từ Liêm
  { id: "m_ntl_mydinh", marketCode: "MKT_NTL_MD", marketName: "Chợ Mỹ Đình", wardName: "Phường Mỹ Đình 1", districtName: "Quận Nam Từ Liêm", address: "Đường Mỹ Đình, Nam Từ Liêm, Hà Nội", totalStalls: 160, managementBoardName: "Ban quản lý chợ Nam Từ Liêm" },
  { id: "m_ntl_phungkhoang", marketCode: "MKT_NTL_PK", marketName: "Chợ Phùng Khoang", wardName: "Phường Trung Văn", districtName: "Quận Nam Từ Liêm", address: "Phố Phùng Khoang, Trung Văn, Nam Từ Liêm, Hà Nội", totalStalls: 200, managementBoardName: "Ban quản lý chợ Phùng Khoang" },
  { id: "m_ntl_metri", marketCode: "MKT_NTL_MT", marketName: "Chợ Mễ Trì Hạ", wardName: "Phường Mễ Trì", districtName: "Quận Nam Từ Liêm", address: "Làng Mễ Trì Hạ, Nam Từ Liêm, Hà Nội", totalStalls: 90, managementBoardName: "Ban quản lý chợ Nam Từ Liêm" },

  // Bắc Từ Liêm
  { id: "m_btl_conhue", marketCode: "MKT_BTL_CN", marketName: "Chợ Cổ Nhuế", wardName: "Phường Cổ Nhuế 1", districtName: "Quận Bắc Từ Liêm", address: "Đường Cổ Nhuế, Bắc Từ Liêm, Hà Nội", totalStalls: 120, managementBoardName: "Ban quản lý chợ Bắc Từ Liêm" },
  { id: "m_btl_xuanlo", marketCode: "MKT_BTL_XD", marketName: "Chợ Xuân Đỉnh", wardName: "Phường Xuân Đỉnh", districtName: "Quận Bắc Từ Liêm", address: "Đường Xuân Đỉnh, Bắc Từ Liêm, Hà Nội", totalStalls: 95, managementBoardName: "Ban quản lý chợ Bắc Từ Liêm" },
  { id: "m_btl_taytuu", marketCode: "MKT_BTL_TT", marketName: "Chợ Hoa Tây Tựu", wardName: "Phường Tây Tựu", districtName: "Quận Bắc Từ Liêm", address: "Làng hoa Tây Tựu, Bắc Từ Liêm, Hà Nội", totalStalls: 150, managementBoardName: "Ban quản lý chợ Bắc Từ Liêm" },

  // Tây Hồ
  { id: "m_th_buoi", marketCode: "MKT_TH_B", marketName: "Chợ Bưởi", wardName: "Phường Bưởi", districtName: "Quận Tây Hồ", address: "Đường Hoàng Hoa Thám, Bưởi, Tây Hồ, Hà Nội", totalStalls: 160, managementBoardName: "Ban quản lý chợ Tây Hồ" },
  { id: "m_th_tulien", marketCode: "MKT_TH_TL", marketName: "Chợ Tứ Liên", wardName: "Phường Tứ Liên", districtName: "Quận Tây Hồ", address: "Phố Tứ Liên, Tây Hồ, Hà Nội", totalStalls: 80, managementBoardName: "Ban quản lý chợ Tây Hồ" },

  // Hà Đông
  { id: "m_hd_chohadong", marketCode: "MKT_HD_HD", marketName: "Chợ Hà Đông", wardName: "Phường Nguyễn Trãi", districtName: "Quận Hà Đông", address: "Đường Lê Lợi, Nguyễn Trãi, Hà Đông, Hà Nội", totalStalls: 300, managementBoardName: "Ban quản lý chợ Hà Đông" },
  { id: "m_hd_vanphuc", marketCode: "MKT_HD_VP", marketName: "Chợ Lụa Vạn Phúc", wardName: "Phường Vạn Phúc", districtName: "Quận Hà Đông", address: "Phố Vạn Phúc, Hà Đông, Hà Nội", totalStalls: 110, managementBoardName: "Ban quản lý làng nghề Vạn Phúc" },

  // Hoàng Mai
  { id: "m_hm_maidong", marketCode: "MKT_HM_MD", marketName: "Chợ Mai Động", wardName: "Phường Mai Động", districtName: "Quận Hoàng Mai", address: "Phố Mai Động, Hoàng Mai, Hà Nội", totalStalls: 130, managementBoardName: "Ban quản lý chợ Hoàng Mai" },
  { id: "m_hm_dinhcong", marketCode: "MKT_HM_DC", marketName: "Chợ Định Công", wardName: "Phường Định Công", districtName: "Quận Hoàng Mai", address: "Khu đô thị Định Công, Hoàng Mai, Hà Nội", totalStalls: 90, managementBoardName: "Ban quản lý chợ Hoàng Mai" },

  // Long Biên
  { id: "m_lb_ngoclam", marketCode: "MKT_LB_NL", marketName: "Chợ Ngọc Lâm", wardName: "Phường Ngọc Lâm", districtName: "Quận Long Biên", address: "Phố Ngọc Lâm, Long Biên, Hà Nội", totalStalls: 120, managementBoardName: "Ban quản lý chợ Long Biên" },
  { id: "m_lb_giathuy", marketCode: "MKT_LB_GT", marketName: "Chợ Gia Thụy", wardName: "Phường Gia Thụy", districtName: "Quận Long Biên", address: "Phố Gia Thụy, Long Biên, Hà Nội", totalStalls: 90, managementBoardName: "Ban quản lý chợ Long Biên" },
  { id: "m_lb_saidong", marketCode: "MKT_LB_SD", marketName: "Chợ Sài Đồng", wardName: "Phường Sài Đồng", districtName: "Quận Long Biên", address: "Khu đô thị Sài Đồng, Long Biên, Hà Nội", totalStalls: 100, managementBoardName: "Ban quản lý chợ Long Biên" },
  { id: "m_lb_phucdong", marketCode: "MKT_LB_PD", marketName: "Chợ Phúc Đồng", wardName: "Phường Phúc Đồng", districtName: "Quận Long Biên", address: "Phố Chu Huy Mân, Phúc Đồng, Long Biên, Hà Nội", totalStalls: 75, managementBoardName: "Ban quản lý chợ Long Biên" },

  // Đông Anh
  { id: "m_da_chotoot", marketCode: "MKT_DA_TO", marketName: "Chợ Tó (Chợ Trung Tâm Đông Anh)", wardName: "Xã Uy Nỗ", districtName: "Huyện Đông Anh", address: "Đường Cao Lỗ, Uy Nỗ, Đông Anh, Hà Nội", totalStalls: 220, managementBoardName: "Ban quản lý chợ Đông Anh" },
  { id: "m_da_munong", marketCode: "MKT_DA_MN", marketName: "Chợ Mun", wardName: "Xã Kim Chung", districtName: "Huyện Đông Anh", address: "KCN Bắc Thăng Long, Kim Chung, Đông Anh, Hà Nội", totalStalls: 180, managementBoardName: "Ban quản lý chợ Đông Anh" },

  // Gia Lâm
  { id: "m_gl_ninhhiep", marketCode: "MKT_GL_NH", marketName: "Chợ Vải Ninh Hiệp", wardName: "Xã Ninh Hiệp", districtName: "Huyện Gia Lâm", address: "Làng Ninh Hiệp, Gia Lâm, Hà Nội", totalStalls: 500, managementBoardName: "Ban quản lý chợ Ninh Hiệp" },
  { id: "m_gl_battrang", marketCode: "MKT_GL_BT", marketName: "Chợ Gốm Bát Tràng", wardName: "Xã Bát Tràng", districtName: "Huyện Gia Lâm", address: "Làng cổ Bát Tràng, Gia Lâm, Hà Nội", totalStalls: 180, managementBoardName: "Ban quản lý làng nghề Bát Tràng" },

  // Hoài Đức
  { id: "m_hd_sondong", marketCode: "MKT_HD_SD", marketName: "Chợ Sơn Đồng", wardName: "Xã Sơn Đồng", districtName: "Huyện Hoài Đức", address: "Làng nghề Sơn Đồng, Hoài Đức, Hà Nội", totalStalls: 120, managementBoardName: "Ban quản lý chợ Hoài Đức" },
  { id: "m_hd_tramtroi", marketCode: "MKT_HD_TT", marketName: "Chợ Trạm Trôi", wardName: "Thị trấn Trạm Trôi", districtName: "Huyện Hoài Đức", address: "Khu trung tâm Trạm Trôi, Hoài Đức, Hà Nội", totalStalls: 150, managementBoardName: "Ban quản lý chợ Hoài Đức" },

  // Thanh Trì
  { id: "m_tt_vandien", marketCode: "MKT_TT_VD", marketName: "Chợ Văn Điển", wardName: "Thị trấn Văn Điển", districtName: "Huyện Thanh Trì", address: "Đường Ngọc Hồi, Văn Điển, Thanh Trì, Hà Nội", totalStalls: 160, managementBoardName: "Ban quản lý chợ Thanh Trì" },
];

/**
 * Lấy danh sách Xã/Phường theo Quận/Huyện (chuẩn hóa tiếng Việt, không dấu, so sánh chính xác)
 */
export function getWardsForDistrict(districtName: string): WardItem[] {
  if (!districtName || districtName === "__custom_district__") return [];
  const normalizedInput = districtName.toLowerCase().trim();

  // Tìm trực tiếp theo key trong HANOI_WARDS_RAW
  for (const [distKey, wardNames] of Object.entries(HANOI_WARDS_RAW)) {
    const kNorm = distKey.toLowerCase().trim();
    if (kNorm === normalizedInput || normalizedInput.includes(kNorm) || kNorm.includes(normalizedInput)) {
      return ALL_HANOI_WARDS.filter(w => w.districtName === distKey);
    }
  }

  // Tìm trong ALL_HANOI_WARDS
  const list = ALL_HANOI_WARDS.filter(w => {
    const d = w.districtName.toLowerCase().trim();
    return d === normalizedInput || normalizedInput.includes(d) || d.includes(normalizedInput);
  });

  return list;
}

/**
 * Lấy danh sách Chợ theo Quận/Huyện và Xã/Phường
 */
export function getMarketsForLocation(districtName: string, wardIdOrName?: string): MarketItem[] {
  if (!districtName || districtName === "__custom_district__") return [];
  const dNorm = districtName.toLowerCase().trim();

  let marketsInDistrict = ALL_HANOI_MARKETS.filter(m => {
    const md = m.districtName.toLowerCase().trim();
    return md === dNorm || dNorm.includes(md) || md.includes(dNorm);
  });

  if (!wardIdOrName || wardIdOrName === "__custom_ward__") {
    return marketsInDistrict;
  }

  const wNorm = wardIdOrName.toLowerCase().trim();
  const matchedInWard = marketsInDistrict.filter(m => {
    if (m.wardId && m.wardId === wardIdOrName) return true;
    if (m.wardName && (m.wardName.toLowerCase().trim() === wNorm || wNorm.includes(m.wardName.toLowerCase()))) return true;
    return false;
  });

  return matchedInWard.length > 0 ? matchedInWard : marketsInDistrict;
}

