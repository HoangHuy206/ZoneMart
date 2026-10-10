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

    /// <summary>
    /// Lấy danh sách Chợ địa phương (Có thể lọc theo Xã/Phường)
    /// </summary>
    [HttpGet("markets")]
    public async Task<IActionResult> GetMarkets([FromQuery] string? wardId)
    {
        try
        {
            var filter = string.IsNullOrWhiteSpace(wardId) 
                ? Builders<Market>.Filter.Empty 
                : Builders<Market>.Filter.Eq(m => m.WardId, wardId);

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
    /// Nghiệp vụ Phân công Ban Quản Lý cho Chợ & Tự động lưu lịch sử phân công
    /// </summary>
    [HttpPost("assign")]
    public async Task<IActionResult> AssignMarket([FromBody] AssignMarketRequest req)
    {
        try
        {
            var market = await _mongoService.Markets.Find(m => m.Id == req.MarketId).FirstOrDefaultAsync();
            if (market == null) return NotFound(new { success = false, message = "Không tìm thấy Chợ cần phân công!" });

            var board = await _mongoService.ManagementBoards.Find(b => b.Id == req.ManagementBoardId).FirstOrDefaultAsync();
            if (board == null) return NotFound(new { success = false, message = "Không tìm thấy Ban quản lý chợ!" });

            // 1. Chuyển trạng thái các bản ghi lịch sử phân công cũ của chợ này sang 'Transferred'
            var oldFilter = Builders<MarketAssignmentHistory>.Filter.And(
                Builders<MarketAssignmentHistory>.Filter.Eq(h => h.MarketId, market.Id),
                Builders<MarketAssignmentHistory>.Filter.Eq(h => h.Status, "Active")
            );
            var updateOld = Builders<MarketAssignmentHistory>.Update
                .Set(h => h.Status, "Transferred")
                .Set(h => h.EndDate, DateTime.UtcNow);
            await _mongoService.MarketAssignmentHistories.UpdateManyAsync(oldFilter, updateOld);

            // 2. Cập nhật thông tin Ban quản lý hiện tại vào Chợ
            await _mongoService.Markets.UpdateOneAsync(
                m => m.Id == market.Id,
                Builders<Market>.Update
                    .Set(m => m.ManagementBoardId, board.Id)
                    .Set(m => m.ManagementBoardName, board.BoardName)
            );

            // 3. Cập nhật danh sách chợ phụ trách vào Ban quản lý
            if (!board.ManagedMarketIds.Contains(market.Id!))
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
                ManagementBoardId = board.Id!,
                BoardName = board.BoardName,
                AssignedBy = req.AssignedBy,
                AssignedDate = DateTime.UtcNow,
                Status = "Active",
                Notes = req.Notes
            };
            await _mongoService.MarketAssignmentHistories.InsertOneAsync(historyRecord);

            return Ok(new
            {
                success = true,
                message = $"Đã phân công thành công {board.BoardName} quản lý {market.MarketName} và lưu vết lịch sử!",
                history = historyRecord
            });
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

