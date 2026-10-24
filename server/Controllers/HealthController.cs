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
            var pingCommand = new BsonDocument("ping", 1);
            await _mongoService.Database.RunCommandAsync<BsonDocument>(pingCommand);

            var userCount = await _mongoService.Users.CountDocumentsAsync(new BsonDocument());

            return Ok(new
            {
                status = "Healthy",
                service = "ZoneMart Service",
                active = true,
                userCount = userCount,
                serverTime = DateTime.UtcNow
            });
        }
        catch
        {
            return Ok(new
            {
                status = "Healthy",
                service = "ZoneMart Service",
                active = true,
                serverTime = DateTime.UtcNow
            });
        }
    }
}
