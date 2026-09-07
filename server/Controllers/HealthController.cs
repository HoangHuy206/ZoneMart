using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using MongoDB.Driver;
using ZoneMart.Server.Services;

namespace ZoneMart.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    private readonly MongoDbService _mongoService;

    public HealthController(MongoDbService mongoService)
    {
        _mongoService = mongoService;
    }

    [HttpGet]
    public async Task<IActionResult> GetStatus()
    {
        try
        {
            // Kiểm tra ping tới MongoDB Atlas
            var pingCommand = new BsonDocument("ping", 1);
            await _mongoService.Database.RunCommandAsync<BsonDocument>(pingCommand);

            var userCount = await _mongoService.Users.CountDocumentsAsync(new BsonDocument());

            return Ok(new
            {
                status = "Healthy",
                backend = "ASP.NET Core (.NET 9.0 - C#)",
                database = "MongoDB Atlas Connected ✅",
                databaseName = _mongoService.Database.DatabaseNamespace.DatabaseName,
                userCount = userCount,
                serverTime = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new
            {
                status = "Unhealthy",
                error = ex.Message
            });
        }
    }
}
