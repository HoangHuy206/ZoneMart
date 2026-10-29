using System;
using System.Collections.Generic;
using ZoneMart.Server.Models;

namespace ZoneMart.Server.Services
{
    public static class CatalogSeedData
    {
        public static List<Product> GetCatalogProducts()
        {
        var catalogProducts = new List<Product>
        {
            new()
            {
                Id = "65f000000000000000000001",
                StoreId = "65f01234567890abcdef0001",
                ProductName = "Rau Xà Lách Mỡ Thủy Canh VietGAP",
                Category = "Rau củ sạch",
                Price = 35000m,
                Description = "Xà lách mỡ canh tác theo phương pháp thủy canh tuần hoàn khép kín tại trang trại Ba Vì. Lá rau dày, giòn ngọt, hoàn toàn không sử dụng thuốc trừ sâu hay chất kích thích sinh trưởng.",
                ImageUrl = "https://images.unsplash.com/photo-1622206151226-18ca2c9ab4a1?auto=format&fit=crop&w=800&q=80",
                StoreName = "Vườn Rau Hữu Cơ Bác Ba",
                StockQuantity = 100,
                StockStatus = "in_stock",
                AiStatus = "approved",
                CreatedAt = DateTime.UtcNow
            },
            new()
            {
                Id = "65f000000000000000000002",
                StoreId = "65f01234567890abcdef0001",
                ProductName = "Rau Muống Hữu Cơ Ba Vì Non Xanh",
                Category = "Rau củ sạch",
                Price = 18000m,
                Description = "Rau muống ngọn nhỏ, thân giòn đanh, ngắt ngọn non mơn mởn. Luộc nước trong veo, xào tỏi xanh mướt không bị nát hay đen cọng.",
                ImageUrl = "https://images.unsplash.com/photo-1540420773420-3366772f4999?auto=format&fit=crop&w=800&q=80",
                StoreName = "Vườn Rau Hữu Cơ Bác Ba",
                StockQuantity = 100,
                StockStatus = "in_stock",
                AiStatus = "approved",
                CreatedAt = DateTime.UtcNow
            },
            new()
            {
                Id = "65f000000000000000000003",
                StoreId = "65f01234567890abcdef0001",
                ProductName = "Cà Chua Bi Socola Ngọt Đậm Vị Hữu Cơ",
                Category = "Rau củ sạch",
                Price = 42000m,
                Description = "Giống cà chua bi màu nâu sẫm socola giàu chất chống oxy hóa Anthocyanin. Vỏ mỏng căng mọng, vị ngọt đậm đà, ít hạt.",
                ImageUrl = "https://images.unsplash.com/photo-1592924357228-91a4daadcfea?auto=format&fit=crop&w=800&q=80",
                StoreName = "Vườn Rau Hữu Cơ Bác Ba",
                StockQuantity = 100,
                StockStatus = "in_stock",
                AiStatus = "approved",
                CreatedAt = DateTime.UtcNow
            },
            new()
            {
                Id = "65f000000000000000000004",
                StoreId = "65f01234567890abcdef0001",
                ProductName = "Bí Xanh Thơm Cắt Khoanh Tiêu Chuẩn VietGAP",
                Category = "Rau củ sạch",
                Price = 25000m,
                Description = "Bí đao chanh thơm dẻo, ruột đặc ít hạt. Nấu canh sườn, canh tôm ngọt lịm hoặc ép nước thanh nhiệt cơ thể.",
                ImageUrl = "https://images.unsplash.com/photo-1598170845058-32b9d6a5da37?auto=format&fit=crop&w=800&q=80",
                StoreName = "Vườn Rau Hữu Cơ Bác Ba",
                StockQuantity = 100,
                StockStatus = "in_stock",
                AiStatus = "approved",
                CreatedAt = DateTime.UtcNow
            },
            new()
            {
                Id = "65f000000000000000000005",
                StoreId = "65f01234567890abcdef0002",
                ProductName = "Hộp Dâu Tây Giống Hana Nhật Bản Hái Sáng Nay",
                Category = "Trái cây tươi",
                Price = 95000m,
                Description = "Dâu tây giống Nhật Hana trồng nhà kính vùng cao Lạc Dương, không khí sương mờ giúp quả mọng nước, thơm lừng và ngọt sắc.",
                ImageUrl = "https://images.unsplash.com/photo-1464965911861-746a04b4bca6?auto=format&fit=crop&w=800&q=80",
                StoreName = "Nông Trại Xanh Lạc Dương",
                StockQuantity = 100,
                StockStatus = "in_stock",
                AiStatus = "approved",
                CreatedAt = DateTime.UtcNow
            },
            new()
            {
                Id = "65f000000000000000000006",
                StoreId = "65f01234567890abcdef0002",
                ProductName = "Bơ Sáp 034 Đặc Sản Lâm Đồng Dẻo Quánh",
                Category = "Trái cây tươi",
                Price = 75000m,
                Description = "Bơ sáp dáng dài 034 nức tiếng Bảo Lộc - Lạc Dương. Thịt quả vàng ươm dẻo như sáp, hạt tiêu nhỏ tí xíu, béo ngậy tự nhiên.",
                ImageUrl = "https://images.unsplash.com/photo-1523049673857-eb18f1d7b578?auto=format&fit=crop&w=800&q=80",
                StoreName = "Nông Trại Xanh Lạc Dương",
                StockQuantity = 100,
                StockStatus = "in_stock",
                AiStatus = "approved",
                CreatedAt = DateTime.UtcNow
            },
            new()
            {
                Id = "65f000000000000000000007",
                StoreId = "65f01234567890abcdef0002",
                ProductName = "Khoai Lang Mật Đà Lạt Nướng Chảy Mật Thơm Lừng",
                Category = "Nông sản tươi",
                Price = 45000m,
                Description = "Khoai lang giống Nhật trồng trên đất đỏ bazan Lạc Dương, củ đã ủ đủ ngày nên nướng hoặc luộc là tứa mật óng ả ngọt bùi.",
                ImageUrl = "https://images.unsplash.com/photo-1596097635121-14b63b7a0c19?auto=format&fit=crop&w=800&q=80",
                StoreName = "Nông Trại Xanh Lạc Dương",
                StockQuantity = 100,
                StockStatus = "in_stock",
                AiStatus = "approved",
                CreatedAt = DateTime.UtcNow
            },
            new()
            {
                Id = "65f000000000000000000008",
                StoreId = "65f01234567890abcdef0002",
                ProductName = "Ớt Chuông Sweet Palermo Giòn Ngọt Thanh Mát",
                Category = "Rau củ sạch",
                Price = 55000m,
                Description = "Giống ớt ngọt cao cấp Palermo nhập khẩu từ Hà Lan, trồng công nghệ cao tại Lạc Dương. Không cay nồng, giòn sần sật và mọng nước ngọt thanh.",
                ImageUrl = "https://images.unsplash.com/photo-1563565375-f3fdfdbefa83?auto=format&fit=crop&w=800&q=80",
                StoreName = "Nông Trại Xanh Lạc Dương",
                StockQuantity = 100,
                StockStatus = "in_stock",
                AiStatus = "approved",
                CreatedAt = DateTime.UtcNow
            },
            new()
            {
                Id = "65f000000000000000000009",
                StoreId = "65f01234567890abcdef0002",
                ProductName = "Bưởi Da Xanh Bến Tre Loại 1 Trái Mọng Tép Đỏ",
                Category = "Trái cây tươi",
                Price = 68000m,
                Description = "Bưởi da xanh cắt tại vườn Mỏ Cày Bắc (Bến Tre). Vỏ mỏng dính, tép bưởi màu hồng đỏ căng mọng nước, vị ngọt thanh không hề đắng hậu.",
                ImageUrl = "https://images.unsplash.com/photo-1587049352846-4a222e784d38?auto=format&fit=crop&w=800&q=80",
                StoreName = "Vựa Trái Cây Sáu Thảo Miền Tây",
                StockQuantity = 100,
                StockStatus = "in_stock",
                AiStatus = "approved",
                CreatedAt = DateTime.UtcNow
            },
            new()
            {
                Id = "65f00000000000000000000a",
                StoreId = "65f01234567890abcdef0002",
                ProductName = "Cam Sành Hàm Yên Mọng Nước Ngọt Tự Nhiên",
                Category = "Trái cây tươi",
                Price = 38000m,
                Description = "Cam sành trái to tròn, vỏ mỏng rám nắng đúng chuẩn cam già cây. Vắt được cực nhiều nước, vị ngọt đậm đà thơm ngát.",
                ImageUrl = "https://images.unsplash.com/photo-1613478223719-2ab802602423?auto=format&fit=crop&w=800&q=80",
                StoreName = "Vựa Trái Cây Sáu Thảo Miền Tây",
                StockQuantity = 100,
                StockStatus = "in_stock",
                AiStatus = "approved",
                CreatedAt = DateTime.UtcNow
            },
            new()
            {
                Id = "65f00000000000000000000b",
                StoreId = "65f01234567890abcdef0002",
                ProductName = "Xoài Cát Hòa Lộc Chín Cây Vàng Óng Thơm Lừng",
                Category = "Trái cây tươi",
                Price = 85000m,
                Description = "Xoài cát Hòa Lộc Tiền Giang chuẩn gốc. Trái thuôn dài, da vàng mịn màng, thịt quả dày chắc mịn không hề có xơ, ngọt lịm sắc nét.",
                ImageUrl = "https://images.unsplash.com/photo-1553279768-865429fa0078?auto=format&fit=crop&w=800&q=80",
                StoreName = "Vựa Trái Cây Sáu Thảo Miền Tây",
                StockQuantity = 100,
                StockStatus = "in_stock",
                AiStatus = "approved",
                CreatedAt = DateTime.UtcNow
            },
            new()
            {
                Id = "65f00000000000000000000c",
                StoreId = "65f01234567890abcdef0002",
                ProductName = "Dừa Xiêm Xanh Bến Tre Ngọt Mát Đã Gọt Trọc",
                Category = "Đồ uống tươi",
                Price = 22000m,
                Description = "Dừa xiêm xanh bánh tẻ gọt vỏ kim cương tiện lợi. Nước dừa ngọt lịm thanh mát, cùi dừa non mềm dẻo nạo ăn cực ngon miệng.",
                ImageUrl = "https://images.unsplash.com/photo-1550258987-190a2d41a8ba?auto=format&fit=crop&w=800&q=80",
                StoreName = "Vựa Trái Cây Sáu Thảo Miền Tây",
                StockQuantity = 100,
                StockStatus = "in_stock",
                AiStatus = "approved",
                CreatedAt = DateTime.UtcNow
            },
            new()
            {
                Id = "65f00000000000000000000d",
                StoreId = "65f01234567890abcdef0001",
                ProductName = "Thịt Bò Mỹ Nhập Khẩu Thượng Hạng USDA Choice",
                Category = "Thực phẩm tươi",
                Price = 185000m,
                Description = "Thịt bò Black Angus nhập khẩu Mỹ chuẩn hạng USDA Choice. Vân mỡ cẩm thạch xen kẽ đều tăm tắp, thịt mềm mọng tan trong miệng khi áp chảo hay nướng.",
                ImageUrl = "https://images.unsplash.com/photo-1607623814075-e51df1bdc82f?auto=format&fit=crop&w=800&q=80",
                StoreName = "Thực Phẩm Tươi Sống ZoneMart Cầu Giấy",
                StockQuantity = 100,
                StockStatus = "in_stock",
                AiStatus = "approved",
                CreatedAt = DateTime.UtcNow
            },
            new()
            {
                Id = "65f00000000000000000000e",
                StoreId = "65f01234567890abcdef0001",
                ProductName = "Cá Hồi Na Uy Tươi Phi Lê Cắt Miếng Trong Ngày",
                Category = "Thực phẩm tươi",
                Price = 195000m,
                Description = "Cá hồi Đại Tây Dương nhập khẩu nguyên con từ vùng biển lạnh Na Uy, phi lê tươi mới mỗi sáng tại kho lạnh ZoneMart. Đạt chuẩn ăn gỏi Sashimi.",
                ImageUrl = "https://images.unsplash.com/photo-1519708227418-c8fd9a32b7a2?auto=format&fit=crop&w=800&q=80",
                StoreName = "Thực Phẩm Tươi Sống ZoneMart Cầu Giấy",
                StockQuantity = 100,
                StockStatus = "in_stock",
                AiStatus = "approved",
                CreatedAt = DateTime.UtcNow
            },
            new()
            {
                Id = "65f00000000000000000000f",
                StoreId = "65f01234567890abcdef0001",
                ProductName = "Thịt Gà Ta Thả Vườn Làm Sạch Nguyên Con",
                Category = "Thực phẩm tươi",
                Price = 160000m,
                Description = "Gà ta thả đồi Ba Vì ăn ngô thóc, thịt săn chắc, thơm ngọt và da vàng giòn sần sật. Đã mổ moi làm sạch lông khử khuẩn đóng túi lạnh.",
                ImageUrl = "https://images.unsplash.com/photo-1587593810167-a84920ea0781?auto=format&fit=crop&w=800&q=80",
                StoreName = "Thực Phẩm Tươi Sống ZoneMart Cầu Giấy",
                StockQuantity = 100,
                StockStatus = "in_stock",
                AiStatus = "approved",
                CreatedAt = DateTime.UtcNow
            },
            new()
            {
                Id = "65f000000000000000000010",
                StoreId = "65f01234567890abcdef0001",
                ProductName = "Cánh Gà Tươi CP Loại 1 Đóng Khay Tiệt Trùng",
                Category = "Thực phẩm tươi",
                Price = 72000m,
                Description = "Cánh gà tươi giữa khúc ngon nhất, da mỏng ít mỡ. Thích hợp làm món cánh gà chiên mắm, nướng mật ong hay sốt me chua cay.",
                ImageUrl = "https://images.unsplash.com/photo-1567620832903-9fc6debc209f?auto=format&fit=crop&w=800&q=80",
                StoreName = "Thực Phẩm Tươi Sống ZoneMart Cầu Giấy",
                StockQuantity = 100,
                StockStatus = "in_stock",
                AiStatus = "approved",
                CreatedAt = DateTime.UtcNow
            },
            new()
            {
                Id = "65f000000000000000000011",
                StoreId = "65f01234567890abcdef0003",
                ProductName = "Cơm Tấm Sườn Bì Chả Đặc Biệt Kèm Canh Chua",
                Category = "Món ăn nóng",
                Price = 55000m,
                Description = "Đĩa cơm tấm gạo thơm dẻo, miếng sườn cốt lết nướng than hoa mật ong xém cạnh thơm nức mũi, chả trứng chưng vàng óng, bì sợi giòn dai và mỡ hành tóp mỡ béo ngậy.",
                ImageUrl = "https://images.unsplash.com/photo-1546069901-ba9599a7e63c?auto=format&fit=crop&w=800&q=80",
                StoreName = "Bếp Cơm Niêu & Ẩm Thực Nóng Cô Ba",
                StockQuantity = 100,
                StockStatus = "in_stock",
                AiStatus = "approved",
                CreatedAt = DateTime.UtcNow
            },
            new()
            {
                Id = "65f000000000000000000012",
                StoreId = "65f01234567890abcdef0003",
                ProductName = "Cơm Gà Xối Mỡ Da Giòn Nóng Hổi Sốt Tỏi Ớt",
                Category = "Món ăn nóng",
                Price = 50000m,
                Description = "Đùi gà góc tư chiên xối mỡ giòn rụm lớp da ngoài, thịt bên trong mềm ngọt mọng nước chấm sốt tỏi ớt đặc chế ăn cùng cơm đảo cà chua đỏ au thơm nức.",
                ImageUrl = "https://images.unsplash.com/photo-1562967914-608f82629710?auto=format&fit=crop&w=800&q=80",
                StoreName = "Bếp Cơm Niêu & Ẩm Thực Nóng Cô Ba",
                StockQuantity = 100,
                StockStatus = "in_stock",
                AiStatus = "approved",
                CreatedAt = DateTime.UtcNow
            },
            new()
            {
                Id = "65f000000000000000000013",
                StoreId = "65f01234567890abcdef0003",
                ProductName = "Canh Cua Rau Đay Cà Pháo Nấu Nồi Đất Chuẩn Vị",
                Category = "Món ăn nóng",
                Price = 40000m,
                Description = "Cua đồng giã tay đóng gạch dày cộp nổi tảng, nấu với rau đay, mướp hương thơm nức mũi. Kèm bát cà pháo giòn tan chống ngấy ngày hè.",
                ImageUrl = "https://images.unsplash.com/photo-1547592180-85f173990554?auto=format&fit=crop&w=800&q=80",
                StoreName = "Bếp Cơm Niêu & Ẩm Thực Nóng Cô Ba",
                StockQuantity = 100,
                StockStatus = "in_stock",
                AiStatus = "approved",
                CreatedAt = DateTime.UtcNow
            },
            new()
            {
                Id = "65f000000000000000000014",
                StoreId = "65f01234567890abcdef0003",
                ProductName = "Trà Tắc Mật Ong Khổng Lồ Mát Lạnh Giải Nhiệt",
                Category = "Đồ uống",
                Price = 18000m,
                Description = "Trà lài ủ lạnh thơm nức pha cùng nước cốt tắc tươi và mật ong hoa rừng nguyên chất. Chua ngọt sảng khoái, đập tan cơn khát ngày hè oi ả.",
                ImageUrl = "https://images.unsplash.com/photo-1556881286-fc6915169721?auto=format&fit=crop&w=800&q=80",
                StoreName = "Bếp Cơm Niêu & Ẩm Thực Nóng Cô Ba",
                StockQuantity = 100,
                StockStatus = "in_stock",
                AiStatus = "approved",
                CreatedAt = DateTime.UtcNow
            },
            new()
            {
                Id = "65f000000000000000000015",
                StoreId = "65f01234567890abcdef0003",
                ProductName = "Combo Bánh Mì Chảo Nóng Hổi Kèm Pate & Xúc Xích",
                Category = "Món ăn nóng",
                Price = 45000m,
                Description = "Chảo bánh mì nóng hổi gồm trứng ốp la lòng đào, pate gan Hải Phòng tự làm béo ngậy, xúc xích giòn rụm và sốt cà chua đậm đà kèm bánh mì nướng giòn tan.",
                ImageUrl = "https://images.unsplash.com/photo-1509722747041-616f39b57569?auto=format&fit=crop&w=800&q=80",
                StoreName = "Tiệm Bánh Mì & Cà Phê Zone Sáng",
                StockQuantity = 100,
                StockStatus = "in_stock",
                AiStatus = "approved",
                CreatedAt = DateTime.UtcNow
            },
            new()
            {
                Id = "65f000000000000000000016",
                StoreId = "65f01234567890abcdef0003",
                ProductName = "Bánh Mì Kẹp Thịt Nướng Ngũ Vị Giòn Rụm",
                Category = "Món ăn nóng",
                Price = 28000m,
                Description = "Ổ bánh mì đặc ruột vỏ giòn tan kẹp ngập tràn thịt nạc vai nướng sả ngũ vị thơm lừng, dưa leo giòn mát, đồ chua chua ngọt và sốt bơ trứng nhà làm.",
                ImageUrl = "https://images.unsplash.com/photo-1528735602780-2552fd46c7af?auto=format&fit=crop&w=800&q=80",
                StoreName = "Tiệm Bánh Mì & Cà Phê Zone Sáng",
                StockQuantity = 100,
                StockStatus = "in_stock",
                AiStatus = "approved",
                CreatedAt = DateTime.UtcNow
            },
            new()
            {
                Id = "65f000000000000000000017",
                StoreId = "65f01234567890abcdef0003",
                ProductName = "Cà Phê Sữa Đá Pha Phin Đậm Đà Chuẩn Gu",
                Category = "Đồ uống",
                Price = 22000m,
                Description = "Hạt Robusta Buôn Ma Thuột rang mộc mạc pha phin nhôm truyền thống, hòa quyện sữa đặc béo ngậy. Đậm đà đánh thức mọi giác quan bắt đầu ngày làm việc mới.",
                ImageUrl = "https://images.unsplash.com/photo-1517701550927-30cf4ba1dba5?auto=format&fit=crop&w=800&q=80",
                StoreName = "Tiệm Bánh Mì & Cà Phê Zone Sáng",
                StockQuantity = 100,
                StockStatus = "in_stock",
                AiStatus = "approved",
                CreatedAt = DateTime.UtcNow
            },
            new()
            {
                Id = "65f000000000000000000018",
                StoreId = "65f01234567890abcdef0003",
                ProductName = "Bánh Bao Trứng Muối Xá Xíu Nóng Hổi Vừa Ra Lò",
                Category = "Món ăn nóng",
                Price = 20000m,
                Description = "Vỏ bánh trắng mịn xốp mềm ngòn ngọt, nhân thịt nạc vai xá xíu đậm đà, mộc nhĩ nấm hương cùng nguyên 1 quả trứng muối bùi bùi béo ngậy.",
                ImageUrl = "https://images.unsplash.com/photo-1563245372-f21724e3856d?auto=format&fit=crop&w=800&q=80",
                StoreName = "Tiệm Bánh Mì & Cà Phê Zone Sáng",
                StockQuantity = 100,
                StockStatus = "in_stock",
                AiStatus = "approved",
                CreatedAt = DateTime.UtcNow
            },
            new()
            {
                Id = "65f000000000000000000019",
                StoreId = "65f01234567890abcdef0001",
                ProductName = "Mật Ong Rừng Hoa Cà Phê Gia Lai Nguyên Chất 100%",
                Category = "Đặc sản vùng cao",
                Price = 135000m,
                Description = "Mật ong hoa cà phê nguyên chất mùa hoa nở rộ đất Tây Nguyên. Màu vàng cánh gián óng ả, vị ngọt sắc thanh dịu không gắt cổ, đặc quánh thơm lừng.",
                ImageUrl = "https://images.unsplash.com/photo-1587049352851-8d4e89133924?auto=format&fit=crop&w=800&q=80",
                StoreName = "Hợp Tác Xã Đặc Sản Vùng Cao Tây Bắc",
                StockQuantity = 100,
                StockStatus = "in_stock",
                AiStatus = "approved",
                CreatedAt = DateTime.UtcNow
            },
            new()
            {
                Id = "65f00000000000000000001a",
                StoreId = "65f01234567890abcdef0001",
                ProductName = "Gạo ST25 Ông Cua Túi 5kg Chuẩn Vị Thơm Dẻo",
                Category = "Nhu yếu phẩm",
                Price = 175000m,
                Description = "Gạo ST25 lúa tôm Sóc Trăng chuẩn tem chống giả. Hạt gạo dài trắng trong, khi nấu tỏa hương lá dứa thơm nức, cơm dẻo dai ngọt hạt dù để nguội.",
                ImageUrl = "https://images.unsplash.com/photo-1586201375761-83865001e31c?auto=format&fit=crop&w=800&q=80",
                StoreName = "Hợp Tác Xã Đặc Sản Vùng Cao Tây Bắc",
                StockQuantity = 100,
                StockStatus = "in_stock",
                AiStatus = "approved",
                CreatedAt = DateTime.UtcNow
            },
            new()
            {
                Id = "65f00000000000000000001b",
                StoreId = "65f01234567890abcdef0001",
                ProductName = "Nấm Hương Rừng Sa Pa Thơm Nức Phơi Tự Nhiên",
                Category = "Nông sản khô",
                Price = 85000m,
                Description = "Nấm hương rừng mọc tự nhiên trên thân cây gỗ mục vùng cao Sa Pa. Cánh nấm dày, viền uốn cong, khi ngâm nở thơm ngát đặc trưng không nấm trồng nào sánh được.",
                ImageUrl = "https://images.unsplash.com/photo-1511690656952-34342bb7c2f2?auto=format&fit=crop&w=800&q=80",
                StoreName = "Hợp Tác Xã Đặc Sản Vùng Cao Tây Bắc",
                StockQuantity = 100,
                StockStatus = "in_stock",
                AiStatus = "approved",
                CreatedAt = DateTime.UtcNow
            },
            new()
            {
                Id = "65f00000000000000000001c",
                StoreId = "65f01234567890abcdef0001",
                ProductName = "Thịt Trâu Gác Bếp Sơn La Chuẩn Vị Hạt Dổi Mắc Khén",
                Category = "Đặc sản vùng cao",
                Price = 240000m,
                Description = "Thịt bắp trâu tươi ướp mắc khén, hạt dổi rừng, ớt bột và hun khói than củi nhãn theo bí quyết người Thái đen. Từng thớ thịt đỏ hồng đượm khói, xé sợi chấm chẩm chéo cực đã.",
                ImageUrl = "https://images.unsplash.com/photo-1529692236671-f1f6cf9683ba?auto=format&fit=crop&w=800&q=80",
                StoreName = "Hợp Tác Xã Đặc Sản Vùng Cao Tây Bắc",
                StockQuantity = 100,
                StockStatus = "in_stock",
                AiStatus = "approved",
                CreatedAt = DateTime.UtcNow
            },
            new()
            {
                Id = "65f00000000000000000001d",
                StoreId = "65f01234567890abcdef0001",
                ProductName = "Bộ Nồi Inox 3 Đáy Cao Cấp Nấu Mọi Loại Bếp Từ",
                Category = "Đồ gia dụng",
                Price = 420000m,
                Description = "Bộ 3 nồi Inox SUS304 chuẩn an toàn sức khỏe kích thước 16-20-24cm. Đáy 3 lớp truyền nhiệt nhanh, tỏa nhiệt đều, chống cháy khét thức ăn trên bếp từ và bếp gas.",
                ImageUrl = "https://images.unsplash.com/photo-1584269600464-37b1b58a9fe7?auto=format&fit=crop&w=800&q=80",
                StoreName = "Tổng Kho Đồ Gia Dụng & Tiêu Dùng Mỹ Đình",
                StockQuantity = 100,
                StockStatus = "in_stock",
                AiStatus = "approved",
                CreatedAt = DateTime.UtcNow
            },
            new()
            {
                Id = "65f00000000000000000001e",
                StoreId = "65f01234567890abcdef0001",
                ProductName = "Nước Rửa Bát Hữu Cơ Tinh Dầu Quế & Chanh Gừng",
                Category = "Đồ dùng gia đình",
                Price = 48000m,
                Description = "Nước rửa chén chiết xuất từ enzyme bồ hòn lên men tự nhiên và tinh dầu quế ấm nồng. Đánh bay dầu mỡ cứng đầu, khử tanh triệt để và an toàn cho da tay nhạy cảm.",
                ImageUrl = "/images/nuoc_rua_bat_huu_co.jpg",
                StoreName = "Tổng Kho Đồ Gia Dụng & Tiêu Dùng Mỹ Đình",
                StockQuantity = 100,
                StockStatus = "in_stock",
                AiStatus = "approved",
                CreatedAt = DateTime.UtcNow
            },
            new()
            {
                Id = "65f00000000000000000001f",
                StoreId = "65f01234567890abcdef0001",
                ProductName = "Chảo Chống Dính Vân Đá Ceramic Đáy Từ Siêu Bền",
                Category = "Đồ gia dụng",
                Price = 185000m,
                Description = "Chảo chiên rán đường kính 26cm phủ lớp men gốm Ceramic vân đá hoa cương chống trầy xước. Chiên trứng không cần dầu mỡ, tay cầm cách nhiệt chống bỏng an toàn.",
                ImageUrl = "https://images.unsplash.com/photo-1556911220-e15b29be8c8f?auto=format&fit=crop&w=800&q=80",
                StoreName = "Tổng Kho Đồ Gia Dụng & Tiêu Dùng Mỹ Đình",
                StockQuantity = 100,
                StockStatus = "in_stock",
                AiStatus = "approved",
                CreatedAt = DateTime.UtcNow
            },
            new()
            {
                Id = "65f000000000000000000020",
                StoreId = "65f01234567890abcdef0001",
                ProductName = "Túi Rác Tự Hủy Sinh Học Thân Thiện Môi Trường",
                Category = "Đồ dùng gia đình",
                Price = 35000m,
                Description = "Bộ 3 cuộn túi rác tự hủy sinh học làm từ tinh bột ngô thiên nhiên. Dẻo dai dai bền, đáy xếp hình sao chống rò rỉ nước rác và tự phân hủy sau 180 ngày chôn lấp.",
                ImageUrl = "https://images.unsplash.com/photo-1530587191325-3db32d826c18?auto=format&fit=crop&w=800&q=80",
                StoreName = "Tổng Kho Đồ Gia Dụng & Tiêu Dùng Mỹ Đình",
                StockQuantity = 100,
                StockStatus = "in_stock",
                AiStatus = "approved",
                CreatedAt = DateTime.UtcNow
            },
            new()
            {
                Id = "65f000000000000000000021",
                StoreId = "65f01234567890abcdef0001",
                ProductName = "Đậu Phụ Non Tofu Mơ Tươi Mềm Mịn (Hộp 400g)",
                Category = "Thực phẩm tươi sống",
                Price = 16000m,
                Description = "Đậu phụ non tươi ngon béo ngậy được làm từ đậu nành nguyên chất, thích hợp ăn lẩu, chiên giòn hoặc sốt cà chua.",
                ImageUrl = "https://images.unsplash.com/photo-1546069901-ba9599a7e63c?auto=format&fit=crop&w=800&q=80",
                StoreName = "Vườn Rau Hữu Cơ Bác Ba",
                StockQuantity = 100,
                StockStatus = "in_stock",
                AiStatus = "approved",
                CreatedAt = DateTime.UtcNow
            },
            new()
            {
                Id = "65f000000000000000000022",
                StoreId = "65f01234567890abcdef0001",
                ProductName = "Bò Viên Gân Tươi Giòn Thơm Hảo Hạng (Gói 500g)",
                Category = "Thịt tươi",
                Price = 85000m,
                Description = "Bò viên gân giòn sần sật, đậm vị thịt bò tươi 100%, không hàn the, cực kỳ ngon khi nhúng lẩu hoặc nấu phở.",
                ImageUrl = "https://images.unsplash.com/photo-1529692236671-f1f6cf9683ba?auto=format&fit=crop&w=800&q=80",
                StoreName = "Thực Phẩm Tươi Sống Hàng Bè",
                StockQuantity = 100,
                StockStatus = "in_stock",
                AiStatus = "approved",
                CreatedAt = DateTime.UtcNow
            },
            new()
            {
                Id = "65f000000000000000000023",
                StoreId = "65f01234567890abcdef0001",
                ProductName = "Nước Cốt Nấu Lẩu Truyền Thống Đậm Đà (Gói 300g)",
                Category = "Gia vị & Đồ khô",
                Price = 32000m,
                Description = "Nước cốt lẩu cô đặc ninh từ xương hầm và thảo mộc tự nhiên, chỉ cần pha với 1.5L nước là có ngay nồi nước dùng ngọt thanh chuẩn vị.",
                ImageUrl = "https://images.unsplash.com/photo-1547592180-85f173990554?auto=format&fit=crop&w=800&q=80",
                StoreName = "Thực Phẩm Tươi Sống Hàng Bè",
                StockQuantity = 100,
                StockStatus = "in_stock",
                AiStatus = "approved",
                CreatedAt = DateTime.UtcNow
            },
            new()
            {
                Id = "65f000000000000000000024",
                StoreId = "65f01234567890abcdef0001",
                ProductName = "Nấm Kim Châm Tươi Giòn Ngọt VietGAP (Gói 150g)",
                Category = "Rau củ sạch",
                Price = 14000m,
                Description = "Nấm kim châm thân trắng giòn ngọt, giàu dinh dưỡng, nhúng lẩu thanh ngọt tự nhiên.",
                ImageUrl = "https://images.unsplash.com/photo-1504674900247-0877df9cc836?auto=format&fit=crop&w=800&q=80",
                StoreName = "Vườn Rau Hữu Cơ Bác Ba",
                StockQuantity = 100,
                StockStatus = "in_stock",
                AiStatus = "approved",
                CreatedAt = DateTime.UtcNow
            },
            new()
            {
                Id = "65f000000000000000000025",
                StoreId = "65f01234567890abcdef0001",
                ProductName = "Rau Cải Xanh Nhúng Lẩu Tươi Non Hữu Cơ (Túi 500g)",
                Category = "Rau củ sạch",
                Price = 22000m,
                Description = "Rau cải xanh tươi non hữu cơ trồng tại Ba Vì, vị ngọt thanh cay nhẹ, cực hợp nhúng lẩu các loại.",
                ImageUrl = "https://images.unsplash.com/photo-1540420773420-3366772f4999?auto=format&fit=crop&w=800&q=80",
                StoreName = "Vườn Rau Hữu Cơ Bác Ba",
                StockQuantity = 100,
                StockStatus = "in_stock",
                AiStatus = "approved",
                CreatedAt = DateTime.UtcNow
            },
            new()
            {
                Id = "65f099887766554433221109",
                StoreId = "65f01234567890abcdef0001",
                StoreName = "Nông Sản & Đặc Sản Vùng Cao",
                SellerEmail = "dovanbinh487@gmail.com",
                ProductName = "Thịt Heo Rừng Tự Nhiên Đóng Khay",
                Category = "Thịt cá tươi",
                Description = "Thịt heo rừng tự nhiên nuôi thả vùng cao đóng khay sạch sẽ, thịt chắc thơm ngọt đậm đà.",
                Price = 130000m,
                Weight = 1.0,
                StockQuantity = 10,
                StockStatus = "in_stock",
                AiStatus = "pending",
                AiCheckNote = "Nghi ngờ nguồn gốc: Mặt hàng thịt rừng cần Ban Quản Trị đối chiếu giấy kiểm dịch thú y trước khi mở bán.",
                ImageUrl = "https://images.unsplash.com/photo-1544025162-d76694265947?auto=format&fit=crop&w=600&q=80",
                CreatedAt = DateTime.UtcNow
            },
            new()
            {
                Id = "65f099887766554433221110",
                StoreId = "65f01234567890abcdef0002",
                StoreName = "Siêu Thị Trái Cây Xanh VietGAP",
                SellerEmail = "seller@zonemart.vn",
                ProductName = "Nho Mẫu Đơn Shine Muscat Nhập Khẩu",
                Category = "Trái cây & Nông sản",
                Description = "Nho mẫu đơn nhập khẩu tươi giòn ngọt lịm thơm mùi sữa, chùm quả đều đẹp.",
                Price = 350000m,
                Weight = 0.6,
                StockQuantity = 15,
                StockStatus = "in_stock",
                AiStatus = "pending",
                AiCheckNote = "AI Vision Cảnh báo: Hình ảnh đăng tải có độ lệch với mẫu chuẩn quả nho mẫu đơn Nhật Bản (độ khớp 48%).",
                ImageUrl = "https://images.unsplash.com/photo-1596363505729-4190a9506133?auto=format&fit=crop&w=600&q=80",
                CreatedAt = DateTime.UtcNow
            }
        };

            return catalogProducts;
        }
    }
}
