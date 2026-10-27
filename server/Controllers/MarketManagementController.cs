using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using ZoneMart.Server.Models;
using ZoneMart.Server.Services;

namespace ZoneMart.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MarketManagementController : ControllerBase
{
    private readonly MongoDbService _mongoService;

    public MarketManagementController(MongoDbService mongoService)
    {
        _mongoService = mongoService;
    }

    /// <summary>
    /// Lấy danh sách Xã / Phường
    /// </summary>
    [HttpGet("wards")]
    public async Task<IActionResult> GetWards()
    {
        try
        {
            var wards = await _mongoService.CommuneWards.Find(_ => true).SortBy(w => w.WardName).ToListAsync();
            return Ok(new { success = true, data = wards });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    private static bool _isSeeded = false;

    private async Task EnsureHanoiMarketsSeededAsync()
    {
        if (_isSeeded) return;
        try
        {
            var existingMarkets = await _mongoService.Markets.Find(_ => true).ToListAsync();
            var marketCodeSet = new HashSet<string>(existingMarkets.Select(m => m.MarketCode).Where(c => !string.IsNullOrEmpty(c)));

            // Danh sách Ban Quản Lý mở rộng theo các Quận / Huyện
            var seedBoards = new[]
            {
                new { Code = "BQL_BD", Name = "Ban quản lý chợ Quận Ba Đình", Leader = "Lê Hoàng Nam", Phone = "0912111222", Email = "bql_badinh@zonemart.vn", District = "Quận Ba Đình" },
                new { Code = "BQL_HK", Name = "Ban quản lý chợ Quận Hoàn Kiếm", Leader = "Phạm Quốc Khánh", Phone = "0903444555", Email = "bql_hoankiem@zonemart.vn", District = "Quận Hoàn Kiếm" },
                new { Code = "BQL_DX", Name = "Công ty CP Đồng Xuân", Leader = "Vũ Đình Trọng", Phone = "0918889999", Email = "dongxuan@zonemart.vn", District = "Quận Hoàn Kiếm" },
                new { Code = "BQL_DD", Name = "Ban quản lý chợ Quận Đống Đa", Leader = "Đỗ Văn Tuấn", Phone = "0936777888", Email = "bql_dongda@zonemart.vn", District = "Quận Đống Đa" },
                new { Code = "BQL_HBT", Name = "Ban quản lý chợ Quận Hai Bà Trưng", Leader = "Nguyễn Thị Mai", Phone = "0912333444", Email = "bql_haibatrung@zonemart.vn", District = "Quận Hai Bà Trưng" },
                new { Code = "BQL_MO", Name = "Ban quản lý chợ Mơ", Leader = "Ngô Quang Huy", Phone = "0988555666", Email = "bql_chomo@zonemart.vn", District = "Quận Hai Bà Trưng" },
                new { Code = "BQL_TH", Name = "Ban quản lý chợ Quận Tây Hồ", Leader = "Bùi Văn Đức", Phone = "0904112233", Email = "bql_tayho@zonemart.vn", District = "Quận Tây Hồ" },
                new { Code = "BQL_TX", Name = "Ban quản lý chợ Quận Thanh Xuân", Leader = "Hoàng Minh Trí", Phone = "0915667788", Email = "bql_thanhxuan@zonemart.vn", District = "Quận Thanh Xuân" },
                new { Code = "BQL_HM", Name = "Ban quản lý chợ Quận Hoàng Mai", Leader = "Đinh Văn Cường", Phone = "0977223344", Email = "bql_hoangmai@zonemart.vn", District = "Quận Hoàng Mai" },
                new { Code = "BQL_LB", Name = "Ban quản lý chợ Quận Long Biên", Leader = "Trần Đức Thắng", Phone = "0913889900", Email = "bql_longbien@zonemart.vn", District = "Quận Long Biên" },
                new { Code = "BQL_NTL", Name = "Ban quản lý chợ Quận Nam Từ Liêm", Leader = "Đặng Thái Sơn", Phone = "0902998877", Email = "bql_namtuliem@zonemart.vn", District = "Quận Nam Từ Liêm" },
                new { Code = "BQL_PK", Name = "Ban quản lý chợ Phùng Khoang", Leader = "Phan Đình Dũng", Phone = "0983114455", Email = "phungkhoang@zonemart.vn", District = "Quận Nam Từ Liêm" },
                new { Code = "BQL_BTL", Name = "Ban quản lý chợ Quận Bắc Từ Liêm", Leader = "Vũ Hữu Bằng", Phone = "0916554433", Email = "bql_bactuliem@zonemart.vn", District = "Quận Bắc Từ Liêm" },
                new { Code = "BQL_HD", Name = "Ban quản lý chợ Quận Hà Đông", Leader = "Tạ Đình Phong", Phone = "0934112299", Email = "bql_hadong@zonemart.vn", District = "Quận Hà Đông" },
                new { Code = "BQL_VP", Name = "Ban quản lý làng nghề Vạn Phúc", Leader = "Nguyễn Hữu Thọ", Phone = "0908776655", Email = "vanphuc@zonemart.vn", District = "Quận Hà Đông" },
                new { Code = "BQL_DA", Name = "Ban quản lý chợ Huyện Đông Anh", Leader = "Hoàng Kim Quy", Phone = "0912998811", Email = "bql_donganh@zonemart.vn", District = "Huyện Đông Anh" },
                new { Code = "BQL_GL", Name = "Ban quản lý chợ Huyện Gia Lâm", Leader = "Trịnh Văn Bách", Phone = "0984332211", Email = "bql_gialam@zonemart.vn", District = "Huyện Gia Lâm" },
                new { Code = "BQL_HDC", Name = "Ban quản lý chợ Huyện Hoài Đức", Leader = "Vương Đình Sâm", Phone = "0903221144", Email = "bql_hoaiduc@zonemart.vn", District = "Huyện Hoài Đức" },
                new { Code = "BQL_TT", Name = "Ban quản lý chợ Huyện Thanh Trì", Leader = "Nguyễn Văn Nghiệp", Phone = "0919445566", Email = "bql_thanhtri@zonemart.vn", District = "Huyện Thanh Trì" }
            };

            var existingBoards = await _mongoService.ManagementBoards.Find(_ => true).ToListAsync();
            var boardMap = new Dictionary<string, ManagementBoard>();
            foreach (var eb in existingBoards)
            {
                if (!string.IsNullOrEmpty(eb.BoardName)) boardMap[eb.BoardName] = eb;
            }

            foreach (var sb in seedBoards)
            {
                if (!boardMap.ContainsKey(sb.Name))
                {
                    var newB = new ManagementBoard
                    {
                        BoardCode = sb.Code,
                        BoardName = sb.Name,
                        LeaderName = sb.Leader,
                        PhoneNumber = sb.Phone,
                        Email = sb.Email,
                        MembersCount = 5,
                        Status = "Active"
                    };
                    await _mongoService.ManagementBoards.InsertOneAsync(newB);
                    boardMap[sb.Name] = newB;
                }
            }

            // Danh sách chợ truyền thống nổi tiếng cần bổ sung
            var seedMarkets = new[]
            {
                // Ba Đình
                new { Code = "MKT_BD_TC", Name = "Chợ Thành Công", Ward = "Phường Thành Công", District = "Quận Ba Đình", Addr = "Khu tập thể Thành Công, Ba Đình, Hà Nội", Stalls = 150, Bql = "Ban quản lý chợ Quận Ba Đình" },
                new { Code = "MKT_BD_CL", Name = "Chợ Châu Long", Ward = "Phường Trúc Bạch", District = "Quận Ba Đình", Addr = "Phố Châu Long, Trúc Bạch, Ba Đình, Hà Nội", Stalls = 110, Bql = "Ban quản lý chợ Quận Ba Đình" },
                new { Code = "MKT_BD_CV", Name = "Chợ Cống Vị", Ward = "Phường Cống Vị", District = "Quận Ba Đình", Addr = "Đường Đội Cấn, Cống Vị, Ba Đình, Hà Nội", Stalls = 95, Bql = "Ban quản lý chợ Quận Ba Đình" },
                new { Code = "MKT_BD_NH", Name = "Chợ Ngọc Hà", Ward = "Phường Ngọc Hà", District = "Quận Ba Đình", Addr = "Phố Hoàng Hoa Thám, Ngọc Hà, Ba Đình, Hà Nội", Stalls = 80, Bql = "Ban quản lý chợ Quận Ba Đình" },
                // Hoàn Kiếm
                new { Code = "MKT_HK_DX", Name = "Chợ Đồng Xuân", Ward = "Phường Đồng Xuân", District = "Quận Hoàn Kiếm", Addr = "Phố Đồng Xuân, Hoàn Kiếm, Hà Nội", Stalls = 350, Bql = "Công ty CP Đồng Xuân" },
                new { Code = "MKT_HK_HB", Name = "Chợ Hàng Bè", Ward = "Phường Hàng Bạc", District = "Quận Hoàn Kiếm", Addr = "Phố Hàng Bè, Hàng Bạc, Hoàn Kiếm, Hà Nội", Stalls = 90, Bql = "Ban quản lý chợ Quận Hoàn Kiếm" },
                new { Code = "MKT_HK_CN", Name = "Chợ Cửa Nam", Ward = "Phường Cửa Nam", District = "Quận Hoàn Kiếm", Addr = "Phố Cửa Nam, Hoàn Kiếm, Hà Nội", Stalls = 80, Bql = "Ban quản lý chợ Quận Hoàn Kiếm" },
                // Đống Đa
                new { Code = "MKT_DD_KL", Name = "Chợ Kim Liên", Ward = "Phường Kim Liên", District = "Quận Đống Đa", Addr = "Phố Lương Định Của, Kim Liên, Đống Đa, Hà Nội", Stalls = 140, Bql = "Ban quản lý chợ Quận Đống Đa" },
                new { Code = "MKT_DD_ND", Name = "Chợ Nam Đồng", Ward = "Phường Nam Đồng", District = "Quận Đống Đa", Addr = "Phố Hồ Đắc Di, Nam Đồng, Đống Đa, Hà Nội", Stalls = 90, Bql = "Ban quản lý chợ Quận Đống Đa" },
                new { Code = "MKT_DD_TH", Name = "Chợ Thái Hà", Ward = "Phường Trung Liệt", District = "Quận Đống Đa", Addr = "Phố Thái Hà, Trung Liệt, Đống Đa, Hà Nội", Stalls = 110, Bql = "Ban quản lý chợ Quận Đống Đa" },
                new { Code = "MKT_DD_KT", Name = "Chợ Khâm Thiên", Ward = "Phường Khâm Thiên", District = "Quận Đống Đa", Addr = "Phố Khâm Thiên, Đống Đa, Hà Nội", Stalls = 85, Bql = "Ban quản lý chợ Quận Đống Đa" },
                // Hai Bà Trưng
                new { Code = "MKT_HBT_CH", Name = "Chợ Hôm - Đức Viên", Ward = "Phường Phố Huế", District = "Quận Hai Bà Trưng", Addr = "Phố Huế, Hai Bà Trưng, Hà Nội", Stalls = 220, Bql = "Ban quản lý chợ Quận Hai Bà Trưng" },
                new { Code = "MKT_HBT_CM", Name = "Chợ Mơ (TTTM Chợ Mơ)", Ward = "Phường Trương Định", District = "Quận Hai Bà Trưng", Addr = "Phố Bạch Mai, Trương Định, Hai Bà Trưng, Hà Nội", Stalls = 280, Bql = "Ban quản lý chợ Mơ" },
                // Tây Hồ
                new { Code = "MKT_TH_B", Name = "Chợ Bưởi", Ward = "Phường Bưởi", District = "Quận Tây Hồ", Addr = "Đường Hoàng Hoa Thám, Bưởi, Tây Hồ, Hà Nội", Stalls = 160, Bql = "Ban quản lý chợ Quận Tây Hồ" },
                new { Code = "MKT_TH_TL", Name = "Chợ Tứ Liên", Ward = "Phường Tứ Liên", District = "Quận Tây Hồ", Addr = "Phố Tứ Liên, Tây Hồ, Hà Nội", Stalls = 80, Bql = "Ban quản lý chợ Quận Tây Hồ" },
                // Thanh Xuân
                new { Code = "MKT_TX_NC", Name = "Chợ Nhân Chính", Ward = "Phường Nhân Chính", District = "Quận Thanh Xuân", Addr = "Đường Quan Nhân, Nhân Chính, Thanh Xuân, Hà Nội", Stalls = 130, Bql = "Ban quản lý chợ Quận Thanh Xuân" },
                new { Code = "MKT_TX_TD", Name = "Chợ Thượng Đình", Ward = "Phường Thượng Đình", District = "Quận Thanh Xuân", Addr = "Đường Nguyễn Trãi, Thượng Đình, Thanh Xuân, Hà Nội", Stalls = 110, Bql = "Ban quản lý chợ Quận Thanh Xuân" },
                // Hoàng Mai
                new { Code = "MKT_HM_MD", Name = "Chợ Mai Động", Ward = "Phường Mai Động", District = "Quận Hoàng Mai", Addr = "Phố Mai Động, Hoàng Mai, Hà Nội", Stalls = 130, Bql = "Ban quản lý chợ Quận Hoàng Mai" },
                new { Code = "MKT_HM_DC", Name = "Chợ Định Công", Ward = "Phường Định Công", District = "Quận Hoàng Mai", Addr = "Khu đô thị Định Công, Hoàng Mai, Hà Nội", Stalls = 90, Bql = "Ban quản lý chợ Quận Hoàng Mai" },
                // Long Biên
                new { Code = "MKT_LB_NL", Name = "Chợ Ngọc Lâm", Ward = "Phường Ngọc Lâm", District = "Quận Long Biên", Addr = "Phố Ngọc Lâm, Long Biên, Hà Nội", Stalls = 120, Bql = "Ban quản lý chợ Quận Long Biên" },
                new { Code = "MKT_LB_GT", Name = "Chợ Gia Thụy", Ward = "Phường Gia Thụy", District = "Quận Long Biên", Addr = "Phố Gia Thụy, Long Biên, Hà Nội", Stalls = 90, Bql = "Ban quản lý chợ Quận Long Biên" },
                new { Code = "MKT_LB_SD", Name = "Chợ Sài Đồng", Ward = "Phường Sài Đồng", District = "Quận Long Biên", Addr = "Khu đô thị Sài Đồng, Long Biên, Hà Nội", Stalls = 100, Bql = "Ban quản lý chợ Quận Long Biên" },
                new { Code = "MKT_LB_PD", Name = "Chợ Phúc Đồng", Ward = "Phường Phúc Đồng", District = "Quận Long Biên", Addr = "Phố Chu Huy Mân, Phúc Đồng, Long Biên, Hà Nội", Stalls = 75, Bql = "Ban quản lý chợ Quận Long Biên" },
                // Nam Từ Liêm
                new { Code = "MKT_NTL_MD", Name = "Chợ Mỹ Đình", Ward = "Phường Mỹ Đình 1", District = "Quận Nam Từ Liêm", Addr = "Đường Mỹ Đình, Nam Từ Liêm, Hà Nội", Stalls = 160, Bql = "Ban quản lý chợ Quận Nam Từ Liêm" },
                new { Code = "MKT_NTL_PK", Name = "Chợ Phùng Khoang", Ward = "Phường Trung Văn", District = "Quận Nam Từ Liêm", Addr = "Phố Phùng Khoang, Trung Văn, Nam Từ Liêm, Hà Nội", Stalls = 200, Bql = "Ban quản lý chợ Phùng Khoang" },
                // Bắc Từ Liêm
                new { Code = "MKT_BTL_CN", Name = "Chợ Cổ Nhuế", Ward = "Phường Cổ Nhuế 1", District = "Quận Bắc Từ Liêm", Addr = "Đường Cổ Nhuế, Bắc Từ Liêm, Hà Nội", Stalls = 120, Bql = "Ban quản lý chợ Quận Bắc Từ Liêm" },
                new { Code = "MKT_BTL_XD", Name = "Chợ Xuân Đỉnh", Ward = "Phường Xuân Đỉnh", District = "Quận Bắc Từ Liêm", Addr = "Đường Xuân Đỉnh, Bắc Từ Liêm, Hà Nội", Stalls = 95, Bql = "Ban quản lý chợ Quận Bắc Từ Liêm" },
                // Hà Đông
                new { Code = "MKT_HD_HD", Name = "Chợ Hà Đông", Ward = "Phường Nguyễn Trãi", District = "Quận Hà Đông", Addr = "Đường Lê Lợi, Nguyễn Trãi, Hà Đông, Hà Nội", Stalls = 300, Bql = "Ban quản lý chợ Quận Hà Đông" },
                new { Code = "MKT_HD_VP", Name = "Chợ Lụa Vạn Phúc", Ward = "Phường Vạn Phúc", District = "Quận Hà Đông", Addr = "Phố Vạn Phúc, Hà Đông, Hà Nội", Stalls = 110, Bql = "Ban quản lý làng nghề Vạn Phúc" },
                // Đông Anh
                new { Code = "MKT_DA_TO", Name = "Chợ Tó (Chợ Trung Tâm Đông Anh)", Ward = "Xã Uy Nỗ", District = "Huyện Đông Anh", Addr = "Đường Cao Lỗ, Uy Nỗ, Đông Anh, Hà Nội", Stalls = 220, Bql = "Ban quản lý chợ Huyện Đông Anh" },
                new { Code = "MKT_DA_MN", Name = "Chợ Mun", Ward = "Xã Kim Chung", District = "Huyện Đông Anh", Addr = "KCN Bắc Thăng Long, Kim Chung, Đông Anh, Hà Nội", Stalls = 180, Bql = "Ban quản lý chợ Huyện Đông Anh" },
                // Gia Lâm
                new { Code = "MKT_GL_NH", Name = "Chợ Vải Ninh Hiệp", Ward = "Xã Ninh Hiệp", District = "Huyện Gia Lâm", Addr = "Làng Ninh Hiệp, Gia Lâm, Hà Nội", Stalls = 500, Bql = "Ban quản lý chợ Huyện Gia Lâm" },
                new { Code = "MKT_GL_BT", Name = "Chợ Gốm Bát Tràng", Ward = "Xã Bát Tràng", District = "Huyện Gia Lâm", Addr = "Làng cổ Bát Tràng, Gia Lâm, Hà Nội", Stalls = 180, Bql = "Ban quản lý chợ Huyện Gia Lâm" },
                // Hoài Đức
                new { Code = "MKT_HD_SD", Name = "Chợ Sơn Đồng", Ward = "Xã Sơn Đồng", District = "Huyện Hoài Đức", Addr = "Làng nghề Sơn Đồng, Hoài Đức, Hà Nội", Stalls = 120, Bql = "Ban quản lý chợ Huyện Hoài Đức" },
                new { Code = "MKT_HD_TT", Name = "Chợ Trạm Trôi", Ward = "Thị trấn Trạm Trôi", District = "Huyện Hoài Đức", Addr = "Khu trung tâm Trạm Trôi, Hoài Đức, Hà Nội", Stalls = 150, Bql = "Ban quản lý chợ Huyện Hoài Đức" },
                // Thanh Trì
                new { Code = "MKT_TT_VD", Name = "Chợ Văn Điển", Ward = "Thị trấn Văn Điển", District = "Huyện Thanh Trì", Addr = "Đường Ngọc Hồi, Văn Điển, Thanh Trì, Hà Nội", Stalls = 160, Bql = "Ban quản lý chợ Huyện Thanh Trì" },
                // Thị xã Sơn Tây (Bổ sung chợ chưa phân công để Admin phân công cho Quản lý ca trực)
                new { Code = "MKT_ST_MIA", Name = "Chợ Mía", Ward = "Xã Đường Lâm", District = "Thị xã Sơn Tây", Addr = "Thôn Đông Sàng, Xã Đường Lâm, Thị xã Sơn Tây, Hà Nội", Stalls = 150, Bql = "Chưa phân công" },
                new { Code = "MKT_ST_NGHE", Name = "Chợ Nghệ", Ward = "Phường Quang Trung", District = "Thị xã Sơn Tây", Addr = "Phố Quang Trung, Thị xã Sơn Tây, Hà Nội", Stalls = 200, Bql = "Chưa phân công" }
            };

            var existingWards = await _mongoService.CommuneWards.Find(_ => true).ToListAsync();
            var wardMap = new Dictionary<string, CommuneWard>();
            foreach (var ew in existingWards)
            {
                if (!string.IsNullOrEmpty(ew.WardName)) wardMap[ew.WardName] = ew;
            }

            var marketsToInsert = new List<Market>();
            foreach (var sm in seedMarkets)
            {
                if (marketCodeSet.Contains(sm.Code)) continue;

                if (!wardMap.TryGetValue(sm.Ward, out var wObj))
                {
                    wObj = new CommuneWard
                    {
                        WardCode = "WARD_" + sm.Code,
                        WardName = sm.Ward,
                        DistrictName = sm.District,
                        CityName = "Hà Nội"
                    };
                    await _mongoService.CommuneWards.InsertOneAsync(wObj);
                    wardMap[sm.Ward] = wObj;
                }

                ManagementBoard? bObj = null;
                if (!string.IsNullOrEmpty(sm.Bql) && sm.Bql != "Chưa phân công")
                {
                    boardMap.TryGetValue(sm.Bql, out bObj);
                }

                var mObj = new Market
                {
                    MarketCode = sm.Code,
                    MarketName = sm.Name,
                    WardId = wObj.Id!,
                    WardName = sm.Ward,
                    DistrictName = sm.District,
                    ManagementBoardId = bObj?.Id,
                    ManagementBoardName = bObj?.BoardName ?? (sm.Bql == "Chưa phân công" ? "Chưa phân công" : sm.Bql),
                    Address = sm.Addr,
                    MarketType = "Chợ dân sinh truyền thống",
                    TotalStalls = sm.Stalls,
                    Status = "Active"
                };
                marketsToInsert.Add(mObj);
            }

            if (marketsToInsert.Count > 0)
            {
                await _mongoService.Markets.InsertManyAsync(marketsToInsert);
            }

            _isSeeded = true;
        }
        catch (Exception ex)
        {
            Console.WriteLine("[EnsureHanoiMarketsSeeded] Warning: " + ex.Message);
        }
    }



    /// <summary>
    /// Lấy danh sách Chợ địa phương (Có thể lọc theo Xã/Phường hoặc Quận/Huyện)
    /// </summary>
    [HttpGet("markets")]
    public async Task<IActionResult> GetMarkets([FromQuery] string? wardId, [FromQuery] string? district)
    {
        try
        {
            await EnsureHanoiMarketsSeededAsync();
            var builder = Builders<Market>.Filter;
            var filter = builder.Empty;
            if (!string.IsNullOrWhiteSpace(wardId))
            {
                filter &= builder.Eq(m => m.WardId, wardId);
            }
            if (!string.IsNullOrWhiteSpace(district))
            {
                filter &= builder.Regex(m => m.DistrictName, new MongoDB.Bson.BsonRegularExpression(district, "i"));
            }

            var markets = await _mongoService.Markets.Find(filter).SortBy(m => m.MarketName).ToListAsync();
            return Ok(new { success = true, data = markets });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }


    /// <summary>
    /// Lấy danh sách Ban Quản Lý Chợ
    /// </summary>
    [HttpGet("boards")]
    public async Task<IActionResult> GetManagementBoards([FromQuery] string? wardId)
    {
        try
        {
            var filter = string.IsNullOrWhiteSpace(wardId)
                ? Builders<ManagementBoard>.Filter.Empty
                : Builders<ManagementBoard>.Filter.Eq(b => b.WardId, wardId);

            var boards = await _mongoService.ManagementBoards.Find(filter).SortBy(b => b.BoardName).ToListAsync();
            return Ok(new { success = true, data = boards });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }


    /// <summary>
    /// Lấy lịch sử phân công quản lý chợ (Đáp ứng Quy tắc 3)
    /// </summary>
    [HttpGet("assignment-history")]
    public async Task<IActionResult> GetAssignmentHistory([FromQuery] string? marketId, [FromQuery] string? boardId)
    {
        try
        {
            var filterBuilder = Builders<MarketAssignmentHistory>.Filter;
            var filter = filterBuilder.Empty;

            if (!string.IsNullOrWhiteSpace(marketId))
            {
                filter &= filterBuilder.Eq(h => h.MarketId, marketId);
            }
            if (!string.IsNullOrWhiteSpace(boardId))
            {
                filter &= filterBuilder.Eq(h => h.ManagementBoardId, boardId);
            }

            var histories = await _mongoService.MarketAssignmentHistories
                .Find(filter)
                .SortByDescending(h => h.AssignedDate)
                .ToListAsync();

            return Ok(new { success = true, data = histories });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }


    public class AssignMarketRequest
    {
        public string MarketId { get; set; } = string.Empty;
        public string ManagementBoardId { get; set; } = string.Empty;
        public string DecisionNumber { get; set; } = string.Empty;
        public string AssignedBy { get; set; } = "UBND Xã / Phường";
        public string Notes { get; set; } = string.Empty;
    }

    /// <summary>
    /// Nghiệp vụ Phân công Ban Quản Lý / Quản Lý Ca Trực cho Chợ & Tự động lưu lịch sử phân công
    /// </summary>
    [HttpPost("assign")]
    public async Task<IActionResult> AssignMarket([FromBody] AssignMarketRequest req)
    {
        try
        {
            var market = await _mongoService.Markets.Find(m => m.Id == req.MarketId).FirstOrDefaultAsync();
            if (market == null) return NotFound(new { success = false, message = "Không tìm thấy Chợ cần phân công!" });

            // Tìm trong danh sách Ban Quản Lý hoặc Tài Khoản Quản Lý Ca Trực (ShiftManagers hoặc User có IsManager)
            var board = await _mongoService.ManagementBoards.Find(b => b.Id == req.ManagementBoardId).FirstOrDefaultAsync();
            ShiftManager? shiftManager = null;
            User? managerUser = null;
            if (board == null)
            {
                shiftManager = await _mongoService.ShiftManagers.Find(sm => sm.Id == req.ManagementBoardId || sm.ManagerCode == req.ManagementBoardId).FirstOrDefaultAsync();
                if (shiftManager == null)
                {
                    managerUser = await _mongoService.Users.Find(u => u.Id == req.ManagementBoardId && (u.IsManager || u.IsAdmin)).FirstOrDefaultAsync();
                }
            }

            if (board == null && shiftManager == null && managerUser == null)
            {
                return NotFound(new { success = false, message = "Không tìm thấy Ban quản lý hoặc Quản lý ca trực được chọn!" });
            }

            string assignedId = board?.Id ?? shiftManager?.Id ?? managerUser!.Id!;
            string assignedName = board?.BoardName ?? (shiftManager != null ? $"{shiftManager.FullName} (Quản lý ca trực)" : $"{managerUser!.FullName} (Quản lý ca trực)");

            if (shiftManager != null)
            {
                await _mongoService.ShiftManagers.UpdateOneAsync(
                    sm => sm.Id == shiftManager.Id,
                    Builders<ShiftManager>.Update.Inc(sm => sm.AssignedMarketsCount, 1)
                );
            }

            // 1. Chuyển trạng thái các bản ghi lịch sử phân công cũ của chợ này sang 'Transferred'
            var oldFilter = Builders<MarketAssignmentHistory>.Filter.And(
                Builders<MarketAssignmentHistory>.Filter.Eq(h => h.MarketId, market.Id),
                Builders<MarketAssignmentHistory>.Filter.Eq(h => h.Status, "Active")
            );
            var updateOld = Builders<MarketAssignmentHistory>.Update
                .Set(h => h.Status, "Transferred")
                .Set(h => h.EndDate, DateTime.UtcNow);
            await _mongoService.MarketAssignmentHistories.UpdateManyAsync(oldFilter, updateOld);

            // 2. Cập nhật thông tin Ban quản lý / Quản lý ca trực hiện tại vào Chợ
            await _mongoService.Markets.UpdateOneAsync(
                m => m.Id == market.Id,
                Builders<Market>.Update
                    .Set(m => m.ManagementBoardId, assignedId)
                    .Set(m => m.ManagementBoardName, assignedName)
            );

            // 3. Cập nhật danh sách chợ phụ trách vào Ban quản lý (nếu là Ban Quản Lý)
            if (board != null && !board.ManagedMarketIds.Contains(market.Id!))
            {
                board.ManagedMarketIds.Add(market.Id!);
                await _mongoService.ManagementBoards.UpdateOneAsync(
                    b => b.Id == board.Id,
                    Builders<ManagementBoard>.Update.Set(b => b.ManagedMarketIds, board.ManagedMarketIds)
                );
            }

            // 4. Tạo bản ghi Lịch sử phân công mới (Audit trail)
            var historyRecord = new MarketAssignmentHistory
            {
                DecisionNumber = !string.IsNullOrWhiteSpace(req.DecisionNumber) ? req.DecisionNumber.Trim() : $"QD-{DateTime.UtcNow:yyyyMMdd}-{market.MarketCode}",
                MarketId = market.Id!,
                MarketName = market.MarketName,
                ManagementBoardId = assignedId,
                BoardName = assignedName,
                AssignedBy = req.AssignedBy,
                AssignedDate = DateTime.UtcNow,
                Status = "Active",
                Notes = req.Notes
            };
            await _mongoService.MarketAssignmentHistories.InsertOneAsync(historyRecord);

            return Ok(new
            {
                success = true,
                message = $"Đã phân công thành công {assignedName} phụ trách {market.MarketName} và lưu vết lịch sử!",
                history = historyRecord
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }


    private static object? _cachedMarketsOverview = null;
    private static DateTime _lastOverviewCacheTime = DateTime.MinValue;
    private static readonly TimeSpan _overviewCacheDuration = TimeSpan.FromSeconds(60);

    public static void InvalidateMarketsOverviewCache()
    {
        _cachedMarketsOverview = null;
    }

    /// <summary>
    /// Lấy danh sách Chợ kèm toàn bộ thông tin các Gian Hàng / Chủ Tiệm thuộc Chợ đó
    /// </summary>
    [HttpGet("markets-overview")]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<IActionResult> GetMarketsOverview([FromQuery] bool force = false)
    {
        try
        {
            if (!force && _cachedMarketsOverview != null && (DateTime.UtcNow - _lastOverviewCacheTime) < _overviewCacheDuration)
            {
                return Ok(_cachedMarketsOverview);
            }

            await EnsureHanoiMarketsSeededAsync();

            var wardsTask = _mongoService.CommuneWards.Find(_ => true).ToListAsync();
            var marketsTask = _mongoService.Markets.Find(_ => true).SortBy(m => m.MarketCode).ToListAsync();
            var boardsTask = _mongoService.ManagementBoards.Find(_ => true).ToListAsync();
            var storesTask = _mongoService.Stores.Find(_ => true).ToListAsync();
            var managersTask = _mongoService.Users.Find(u => (u.IsManager || u.IsAdmin) && !u.IsDeleted).ToListAsync();
            var shiftManagersTask = _mongoService.ShiftManagers.Find(m => !m.IsDeleted).ToListAsync();
            var usersTask = _mongoService.Users.Find(u => u.IsSeller && !u.IsDeleted).ToListAsync();

            await Task.WhenAll(wardsTask, marketsTask, boardsTask, storesTask, managersTask, shiftManagersTask, usersTask);

            var wards = await wardsTask;
            var markets = await marketsTask;
            var boards = await boardsTask;
            var stores = await storesTask;
            var managers = await managersTask;
            var shiftManagers = await shiftManagersTask;
            var users = await usersTask;

            var result = markets.Select(m =>
            {
                var matchedStores = stores.Where(s =>
                    (string.Equals(s.Status, "Approved", StringComparison.OrdinalIgnoreCase) || string.Equals(s.Status, "Active", StringComparison.OrdinalIgnoreCase)) &&
                    ((!string.IsNullOrEmpty(s.MarketId) && s.MarketId == m.Id) ||
                    (!string.IsNullOrEmpty(s.MarketName) && s.MarketName.Trim().ToLower() == m.MarketName.Trim().ToLower()) ||
                    (!string.IsNullOrEmpty(s.MarketName) && (s.MarketName.ToLower().Contains(m.MarketName.ToLower()) || m.MarketName.ToLower().Contains(s.MarketName.ToLower()))) ||
                    (!string.IsNullOrEmpty(s.MarketName) && !string.IsNullOrEmpty(m.MarketCode) && s.MarketName.ToLower().Contains(m.MarketCode.ToLower())))
                ).ToList();

                var board = boards.FirstOrDefault(b => b.Id == m.ManagementBoardId);
                var shiftManager = shiftManagers.FirstOrDefault(sm => sm.Id == m.ManagementBoardId || sm.ManagerCode == m.ManagementBoardId);
                var manager = board == null && shiftManager == null ? managers.FirstOrDefault(u => u.Id == m.ManagementBoardId) : null;
                var ward = wards.FirstOrDefault(w => w.Id == m.WardId);

                string bName = "Chưa phân công";
                if (shiftManager != null)
                {
                    bName = $"{shiftManager.FullName} (Quản lý ca trực)";
                }
                else if (board != null)
                {
                    bName = board.BoardName;
                }
                else if (manager != null)
                {
                    bName = $"{manager.FullName} (Quản lý ca trực)";
                }
                else if (!string.IsNullOrWhiteSpace(m.ManagementBoardName) && m.ManagementBoardName != "Chưa phân công")
                {
                    bName = m.ManagementBoardName;
                }

                string bLeader = shiftManager?.FullName ?? board?.LeaderName ?? manager?.FullName ?? "";
                string bPhone = shiftManager?.Phone ?? shiftManager?.Email ?? board?.PhoneNumber ?? manager?.Phone ?? manager?.PhoneEmail ?? "";

                var storeDetailsList = matchedStores.Select(s =>
                {
                    var ownerUser = users.FirstOrDefault(u =>
                        (!string.IsNullOrEmpty(s.UserId) && (u.Id == s.UserId || u.UserCode == s.UserId)) ||
                        (!string.IsNullOrEmpty(s.PhoneEmail) && !string.IsNullOrEmpty(u.PhoneEmail) && u.PhoneEmail.Trim().ToLower() == s.PhoneEmail.Trim().ToLower()) ||
                        (!string.IsNullOrEmpty(s.OwnerFullName) && !string.IsNullOrEmpty(u.FullName) && u.FullName.Trim().ToLower() == s.OwnerFullName.Trim().ToLower())
                    );

                    return new
                    {
                        storeId = s.Id,
                        userId = s.UserId ?? ownerUser?.Id ?? "",
                        storeCode = s.StoreCode,
                        storeName = s.StoreName,
                        ownerFullName = !string.IsNullOrWhiteSpace(s.OwnerFullName) ? s.OwnerFullName : (ownerUser?.FullName ?? "Chủ Gian Hàng"),
                        phoneEmail = !string.IsNullOrWhiteSpace(s.PhoneEmail) ? s.PhoneEmail : (ownerUser?.PhoneEmail ?? ""),
                        isSeller = true,
                        isShipper = ownerUser?.IsShipper ?? false,
                        isBuyer = ownerUser?.IsBuyer ?? true,
                        isManager = ownerUser?.IsManager ?? false,
                        isAdmin = ownerUser?.IsAdmin ?? false,
                        cccdNumber = !string.IsNullOrWhiteSpace(s.CccdNumber) ? s.CccdNumber : (ownerUser?.CccdNumber ?? ""),
                        dateOfBirth = s.DateOfBirth,
                        gender = s.Gender,
                        nguyenQuan = s.NguyenQuan,
                        permanentAddress = s.PermanentAddress,
                        stallNumber = !string.IsNullOrWhiteSpace(s.StallNumber) ? s.StallNumber : "Quầy sạp tại chợ",
                        address = s.Address,
                        category = s.Category,
                        openHours = s.OpenHours,
                        bankName = s.BankName,
                        bankAccountNumber = s.BankAccountNumber,
                        sellerType = s.SellerType,
                        status = s.Status,
                        rejectReason = s.RejectReason,
                        createdAt = s.CreatedAt.ToString("dd/MM/yyyy HH:mm"),
                        avatarUrl = ownerUser?.AvatarUrl ?? "https://images.unsplash.com/photo-1544005313-94ddf0286df2?auto=format&fit=crop&w=200",
                        cccdFrontImage = s.CccdFrontImage,
                        foodSafetyCertImage = s.FoodSafetyCertImage
                    };
                }).ToList();

                return new
                {
                    id = m.Id,
                    marketCode = m.MarketCode,
                    marketName = m.MarketName,
                    wardId = m.WardId,
                    wardName = m.WardName ?? ward?.WardName ?? "Phường Dịch Vọng Hậu",
                    districtName = !string.IsNullOrWhiteSpace(m.DistrictName) ? m.DistrictName : (ward?.DistrictName ?? "Quận Cầu Giấy"),
                    managementBoardId = m.ManagementBoardId,
                    managementBoardName = bName,
                    boardLeader = bLeader,
                    boardPhone = bPhone,
                    address = m.Address,
                    marketType = m.MarketType,
                    totalStalls = m.TotalStalls,
                    registeredStallsCount = storeDetailsList.Count,
                    occupancyRate = m.TotalStalls > 0 ? Math.Round((double)storeDetailsList.Count / m.TotalStalls * 100, 1) : 0,
                    status = m.Status,
                    stores = storeDetailsList
                };
            }).ToList();

            var unassignedStores = stores.Where(s =>
                string.IsNullOrEmpty(s.MarketId) &&
                (string.IsNullOrEmpty(s.MarketName) || !markets.Any(m => s.MarketName.ToLower().Contains(m.MarketName.ToLower()) || m.MarketName.ToLower().Contains(s.MarketName.ToLower()))) &&
                (string.Equals(s.Status, "Approved", StringComparison.OrdinalIgnoreCase) || string.Equals(s.Status, "Active", StringComparison.OrdinalIgnoreCase))
            ).Select(s =>
            {
                var ownerUser = users.FirstOrDefault(u =>
                    (!string.IsNullOrEmpty(s.UserId) && (u.Id == s.UserId || u.UserCode == s.UserId)) ||
                    (!string.IsNullOrEmpty(s.PhoneEmail) && !string.IsNullOrEmpty(u.PhoneEmail) && u.PhoneEmail.Trim().ToLower() == s.PhoneEmail.Trim().ToLower()) ||
                    (!string.IsNullOrEmpty(s.OwnerFullName) && !string.IsNullOrEmpty(u.FullName) && u.FullName.Trim().ToLower() == s.OwnerFullName.Trim().ToLower())
                );
                return new
                {
                    storeId = s.Id,
                    userId = s.UserId ?? ownerUser?.Id ?? "",
                    storeCode = s.StoreCode,
                    storeName = s.StoreName,
                    ownerFullName = s.OwnerFullName,
                    phoneEmail = s.PhoneEmail,
                    cccdNumber = s.CccdNumber,
                    ward = s.Ward,
                    district = s.District,
                    marketName = s.MarketName,
                    stallNumber = s.StallNumber ?? "Tự do",
                    address = s.Address,
                    category = s.Category,
                    status = s.Status,
                    isSeller = true,
                    isShipper = ownerUser?.IsShipper ?? false,
                    isBuyer = ownerUser?.IsBuyer ?? true,
                    isManager = ownerUser?.IsManager ?? false,
                    isAdmin = ownerUser?.IsAdmin ?? false,
                    createdAt = s.CreatedAt.ToString("dd/MM/yyyy HH:mm")
                };
            }).ToList();

            var responseObj = new
            {
                success = true,
                totalWards = wards.Count,
                totalMarkets = markets.Count,
                totalBoards = boards.Count + shiftManagers.Count + managers.Count,
                totalStores = stores.Count,
                markets = result,
                boards = boards.Select(b => new { id = b.Id ?? "", name = b.BoardName ?? "", leader = b.LeaderName ?? "", phone = b.PhoneNumber ?? "" })
                    .Concat(shiftManagers.Select(sm => new { id = sm.Id ?? "", name = $"{sm.FullName} ({sm.Id} - Quản lý ca trực)", leader = sm.FullName ?? "", phone = sm.Email ?? "" }))
                    .Concat(managers.Where(m => !shiftManagers.Any(sm => sm.Email == m.PhoneEmail)).Select(m => new { id = m.Id ?? "", name = $"{m.FullName} (Quản lý ca trực)", leader = m.FullName ?? "", phone = m.PhoneEmail ?? "" })),
                unassignedStores
            };

            _cachedMarketsOverview = responseObj;
            _lastOverviewCacheTime = DateTime.UtcNow;

            return Ok(responseObj);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    public class AssignStoreToMarketRequest
    {
        public string StoreId { get; set; } = string.Empty;
        public string MarketId { get; set; } = string.Empty;
        public string? StallNumber { get; set; }
    }

    public class UnassignStoreFromMarketRequest
    {
        public string StoreId { get; set; } = string.Empty;
    }


    /// <summary>
    /// Gán hoặc chuyển đổi Gian Hàng vào một Chợ cụ thể
    /// </summary>
    [HttpPost("assign-store-to-market")]
    public async Task<IActionResult> AssignStoreToMarket([FromBody] AssignStoreToMarketRequest req)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(req.StoreId) || string.IsNullOrWhiteSpace(req.MarketId))
            {
                return BadRequest(new { success = false, message = "StoreId và MarketId không được để trống!" });
            }

            var market = await _mongoService.Markets.Find(m => m.Id == req.MarketId).FirstOrDefaultAsync();
            if (market == null)
            {
                return NotFound(new { success = false, message = "Không tìm thấy Chợ tương ứng!" });
            }

            var updateDef = Builders<Store>.Update
                .Set(s => s.MarketId, market.Id)
                .Set(s => s.MarketName, market.MarketName);

            if (!string.IsNullOrWhiteSpace(req.StallNumber))
            {
                updateDef = updateDef.Set(s => s.StallNumber, req.StallNumber);
            }

            var updateResult = await _mongoService.Stores.UpdateOneAsync(s => s.Id == req.StoreId || s.StoreCode == req.StoreId, updateDef);
            if (updateResult.ModifiedCount > 0)
            {
                InvalidateMarketsOverviewCache();
                return Ok(new { success = true, message = $"Đã gắn gian hàng vào {market.MarketName} thành công!" });
            }

            return BadRequest(new { success = false, message = "Không thể cập nhật gian hàng (có thể gian hàng đã ở chợ này)." });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }


    /// <summary>
    /// Bỏ gian hàng khỏi Chợ (gỡ liên kết MarketId, MarketName, StallNumber để trở thành gian hàng tự do)
    /// </summary>
    [HttpPost("unassign-store-from-market")]
    public async Task<IActionResult> UnassignStoreFromMarket([FromBody] UnassignStoreFromMarketRequest req)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(req.StoreId))
            {
                return BadRequest(new { success = false, message = "StoreId không được để trống!" });
            }

            var store = await _mongoService.Stores.Find(s => s.Id == req.StoreId || s.StoreCode == req.StoreId).FirstOrDefaultAsync();
            if (store == null)
            {
                return NotFound(new { success = false, message = "Không tìm thấy gian hàng tương ứng!" });
            }

            var oldMarketName = !string.IsNullOrWhiteSpace(store.MarketName) ? store.MarketName : "Chợ";

            var updateDef = Builders<Store>.Update
                .Set(s => s.MarketId, null)
                .Set(s => s.MarketName, string.Empty)
                .Set(s => s.StallNumber, null);

            var updateResult = await _mongoService.Stores.UpdateOneAsync(s => s.Id == store.Id, updateDef);
            InvalidateMarketsOverviewCache();
            return Ok(new { success = true, message = $"Đã bỏ gian hàng '{store.StoreName}' khỏi {oldMarketName} thành công!" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }



    /// <summary>
    /// Khởi tạo dữ liệu mẫu chuẩn theo Sơ đồ 1.2
    /// </summary>
    [HttpPost("seed-diagram-1-2")]
    public async Task<IActionResult> SeedDiagramData()
    {
        try
        {
            // Xóa dữ liệu cũ nếu muốn làm mới
            await _mongoService.CommuneWards.DeleteManyAsync(_ => true);
            await _mongoService.ManagementBoards.DeleteManyAsync(_ => true);
            await _mongoService.Markets.DeleteManyAsync(_ => true);
            await _mongoService.MarketAssignmentHistories.DeleteManyAsync(_ => true);

            // 1. Tạo Cấp Xã / Phường
            var ward = new CommuneWard
            {
                WardCode = "WARD_DICH_VONG_HAU",
                WardName = "Phường Dịch Vọng Hậu",
                DistrictName = "Quận Cầu Giấy",
                CityName = "Hà Nội"
            };
            await _mongoService.CommuneWards.InsertOneAsync(ward);

            // 2. Tạo 2 Ban Quản Lý Chợ Số
            var board1 = new ManagementBoard
            {
                BoardCode = "BQL_01",
                BoardName = "Ban quản lý chợ số 01",
                WardId = ward.Id!,
                WardName = ward.WardName,
                LeaderName = "Nguyễn Văn Hùng",
                PhoneNumber = "0912345678",
                Email = "bql01@zonemart.vn",
                MembersCount = 6,
                Status = "Active"
            };

            var board2 = new ManagementBoard
            {
                BoardCode = "BQL_02",
                BoardName = "Ban quản lý chợ số 02",
                WardId = ward.Id!,
                WardName = ward.WardName,
                LeaderName = "Trần Thị Lan",
                PhoneNumber = "0987654321",
                Email = "bql02@zonemart.vn",
                MembersCount = 4,
                Status = "Active"
            };

            await _mongoService.ManagementBoards.InsertManyAsync([board1, board2]);

            // 3. Tạo 3 Chợ: Chợ A, Chợ B, Chợ C
            var marketA = new Market
            {
                MarketCode = "MKT_A",
                MarketName = "Chợ A (Chợ Nhà Xanh)",
                WardId = ward.Id!,
                WardName = ward.WardName,
                ManagementBoardId = board1.Id,
                ManagementBoardName = board1.BoardName,
                Address = "Phố Phan Văn Trường, Dịch Vọng Hậu, Cầu Giấy",
                TotalStalls = 120,
                Status = "Active"
            };

            var marketB = new Market
            {
                MarketCode = "MKT_B",
                MarketName = "Chợ B (Chợ Nghĩa Tân)",
                WardId = ward.Id!,
                WardName = ward.WardName,
                ManagementBoardId = board1.Id,
                ManagementBoardName = board1.BoardName,
                Address = "Phố Tô Hiệu, Nghĩa Tân, Cầu Giấy",
                TotalStalls = 85,
                Status = "Active"
            };

            var marketC = new Market
            {
                MarketCode = "MKT_C",
                MarketName = "Chợ C (Chợ Dịch Vọng)",
                WardId = ward.Id!,
                WardName = ward.WardName,
                ManagementBoardId = board2.Id,
                ManagementBoardName = board2.BoardName,
                Address = "Đường Cầu Giấy, Dịch Vọng Hậu, Cầu Giấy",
                TotalStalls = 60,
                Status = "Active"
            };

            await _mongoService.Markets.InsertManyAsync([marketA, marketB, marketC]);

            // Cập nhật danh sách chợ phụ trách vào Ban quản lý
            board1.ManagedMarketIds = [marketA.Id!, marketB.Id!];
            await _mongoService.ManagementBoards.UpdateOneAsync(b => b.Id == board1.Id, Builders<ManagementBoard>.Update.Set(b => b.ManagedMarketIds, board1.ManagedMarketIds));

            board2.ManagedMarketIds = [marketC.Id!];
            await _mongoService.ManagementBoards.UpdateOneAsync(b => b.Id == board2.Id, Builders<ManagementBoard>.Update.Set(b => b.ManagedMarketIds, board2.ManagedMarketIds));

            // 4. Lưu Lịch Sử Phân Công Quản Lý (Audit Trail)
            var histories = new List<MarketAssignmentHistory>
            {
                new() {
                    DecisionNumber = "QD-01/UBND-DVH",
                    MarketId = marketA.Id!,
                    MarketName = marketA.MarketName,
                    ManagementBoardId = board1.Id!,
                    BoardName = board1.BoardName,
                    AssignedBy = "UBND Phường Dịch Vọng Hậu",
                    AssignedDate = DateTime.UtcNow.AddMonths(-3),
                    Status = "Active",
                    Notes = "Phân công quản lý và số hóa tiểu thương Chợ A"
                },
                new() {
                    DecisionNumber = "QD-02/UBND-DVH",
                    MarketId = marketB.Id!,
                    MarketName = marketB.MarketName,
                    ManagementBoardId = board1.Id!,
                    BoardName = board1.BoardName,
                    AssignedBy = "UBND Phường Dịch Vọng Hậu",
                    AssignedDate = DateTime.UtcNow.AddMonths(-2),
                    Status = "Active",
                    Notes = "Phân công kiêm nhiệm phụ trách Chợ B theo chủ trương sáp nhập tổ quản lý"
                },
                new() {
                    DecisionNumber = "QD-03/UBND-DVH",
                    MarketId = marketC.Id!,
                    MarketName = marketC.MarketName,
                    ManagementBoardId = board2.Id!,
                    BoardName = board2.BoardName,
                    AssignedBy = "UBND Phường Dịch Vọng Hậu",
                    AssignedDate = DateTime.UtcNow.AddMonths(-1),
                    Status = "Active",
                    Notes = "Phân công quản lý hoạt động số hóa Chợ C"
                }
            };
            await _mongoService.MarketAssignmentHistories.InsertManyAsync(histories);

            return Ok(new
            {
                success = true,
                message = "Đã khởi tạo thành công dữ liệu mẫu chuẩn 100% theo Sơ đồ 1.2!",
                ward,
                boards = new[] { board1, board2 },
                markets = new[] { marketA, marketB, marketC },
                histories
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

}

