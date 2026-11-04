using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.HttpOverrides;
using Fido2NetLib;
using ZoneMart.Server.Services;
using ZoneMart.Server.Settings;

// Tự động nạp file biến môi trường .env từ thư mục gốc dự án (nếu có)
LoadDotEnvIfExists();

var builder = WebApplication.CreateBuilder(args);

// Đồng bộ biến môi trường từ .env vào cấu hình ASP.NET Core
var envMongoConn = Environment.GetEnvironmentVariable("MONGODB_CONNECTION_STRING");
if (!string.IsNullOrWhiteSpace(envMongoConn))
{
    builder.Configuration["MongoDBSettings:ConnectionString"] = envMongoConn;
}
var envSepayKey = Environment.GetEnvironmentVariable("SEPAY_API_KEY");
if (!string.IsNullOrWhiteSpace(envSepayKey))
{
    builder.Configuration["SepaySettings:ApiKey"] = envSepayKey;
}
var envTelegramBot = Environment.GetEnvironmentVariable("TELEGRAM_BOT_TOKEN");
if (!string.IsNullOrWhiteSpace(envTelegramBot))
{
    builder.Configuration["TelegramSettings:BotToken"] = envTelegramBot;
}
var envGeminiKey = Environment.GetEnvironmentVariable("GEMINI_API_KEY");
if (!string.IsNullOrWhiteSpace(envGeminiKey))
{
    builder.Configuration["GeminiSettings:ApiKey"] = envGeminiKey;
}

// 1. Cấu hình MongoDB Atlas
builder.Services.Configure<MongoDBSettings>(
    builder.Configuration.GetSection("MongoDBSettings")
);
builder.Services.AddSingleton<MongoDbService>();

// 2. Cấu hình Email & Telegram & Gemini Settings
builder.Services.Configure<EmailSettings>(
    builder.Configuration.GetSection("EmailSettings")
);
builder.Services.Configure<TelegramSettings>(
    builder.Configuration.GetSection("TelegramSettings")
);
builder.Services.Configure<GeminiSettings>(
    builder.Configuration.GetSection("GeminiSettings")
);
builder.Services.Configure<KiraSettings>(
    builder.Configuration.GetSection("KiraSettings")
);
builder.Services.Configure<FptAiSettings>(
    builder.Configuration.GetSection("FptAiSettings")
);

// 3. Đăng ký Services & HttpClient
builder.Services.AddHttpClient();
builder.Services.AddHttpClient("GeminiClient", client =>
{
    client.Timeout = TimeSpan.FromSeconds(45);
});
builder.Services.AddHttpClient("KiraClient", client =>
{
    client.Timeout = TimeSpan.FromSeconds(30);
});
builder.Services.AddHttpClient("FptAiClient", client =>
{
    client.Timeout = TimeSpan.FromSeconds(35);
});
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<ITelegramService, TelegramService>();
builder.Services.AddScoped<IProductModerationService, ProductModerationService>();
builder.Services.AddScoped<IHumanHandoverService, HumanHandoverService>();
builder.Services.AddHostedService<PythonFaceServiceManager>();
builder.Services.AddSingleton<OrderRealtimeManager>();
builder.Services.AddSingleton<SellerChatManager>();

// 4. MemoryCache & WebAuthn / Passkeys (FIDO2)
builder.Services.AddMemoryCache();
var configuredRpId = Environment.GetEnvironmentVariable("WEBAUTHN_RPID") ?? builder.Configuration["WebAuthn:RPID"] ?? "localhost";
builder.Services.AddFido2(options =>
{
    options.RPID = configuredRpId;
    options.RPName = "ZoneMart";
    var webAuthnOrigins = new HashSet<string>
    {
        "http://localhost:5173",
        "http://127.0.0.1:5173",
        "http://localhost:5000",
        "http://127.0.0.1:5000",
        "https://localhost:5173",
        "https://localhost:5000"
    };
    var extraOrigin = Environment.GetEnvironmentVariable("WEBAUTHN_ORIGIN");
    if (!string.IsNullOrWhiteSpace(extraOrigin))
    {
        webAuthnOrigins.Add(extraOrigin.Trim());
    }
    options.Origins = webAuthnOrigins;
});

// 5. Cấu hình CORS cho phép Vue 3 Frontend, Capacitor iOS & LAN gọi API
var allowedOriginsEnv = Environment.GetEnvironmentVariable("ALLOWED_ORIGINS");
var configuredOrigins = !string.IsNullOrWhiteSpace(allowedOriginsEnv)
    ? allowedOriginsEnv.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
    : Array.Empty<string>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowVueClient", policy =>
    {
        if (configuredOrigins.Length > 0)
        {
            policy.WithOrigins(configuredOrigins)
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials();
        }
        else
        {
            policy.SetIsOriginAllowed(origin => true)
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials();
        }
    });
});

// 6. Cấu hình Rate Limiting chống Brute-force mật khẩu & spam OTP
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddFixedWindowLimiter("auth-rate-limit", opt =>
    {
        opt.PermitLimit = 30; // Tối đa 30 request / phút / IP
        opt.Window = TimeSpan.FromMinutes(1);
        opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        opt.QueueLimit = 0;
    });
});

builder.Services.AddControllers();

#if NET9_0_OR_GREATER
builder.Services.AddOpenApi();
#else
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
#endif

var app = builder.Build();

// Xử lý Forwarded Headers khi triển khai sau Reverse Proxy (Nginx, Cloudflare, Caddy, Docker)
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});

#if NET9_0_OR_GREATER
app.MapOpenApi();
#else
app.UseSwagger();
app.UseSwaggerUI();
#endif

app.UseCors("AllowVueClient");
app.UseRateLimiter();

// Thiết lập Security Headers tiêu chuẩn cho toàn bộ HTTP Responses
app.Use(async (context, next) =>
{
    context.Response.Headers.Append("X-Frame-Options", "SAMEORIGIN");
    context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
    context.Response.Headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");
    context.Response.Headers.Append("Content-Security-Policy",
        "default-src 'self'; script-src 'self' 'unsafe-inline' 'unsafe-eval' https:; style-src 'self' 'unsafe-inline' https:; img-src 'self' data: blob: https:; connect-src 'self' http: https: ws: wss:; font-src 'self' data: https:;");
    await next();
});

// Endpoint Health Check tiêu chuẩn
app.MapGet("/api/health", () => Results.Ok(new
{
    status = "Healthy",
    service = "ZoneMart API",
    active = true,
    time = DateTime.UtcNow
}));

// app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

Console.WriteLine("🚀 ZoneMart C# ASP.NET Core Web API đang khởi động...");
Console.WriteLine("📧 Đã nạp cấu hình gửi thư Gmail: hh9393100@gmail.com");
Console.WriteLine("🤖 Đã nạp cấu hình Bot Telegram: @ZoneMarttt_bot");
Console.WriteLine($"🛡️ Đã sẵn sàng hệ thống WebAuthn / Passkey Sinh trắc học (RPID: {configuredRpId})!");

var listenUrl = Environment.GetEnvironmentVariable("ASPNETCORE_URLS") ?? "http://0.0.0.0:5000";
Console.WriteLine($"🌐 Lắng nghe Backend C# trên tất cả cổng mạng LAN: {listenUrl}");

app.Run(listenUrl);

// ==============================================================================
// Helper Function: Tự động nạp file .env từ thư mục hiện tại hoặc các thư mục cha
// ==============================================================================
static void LoadDotEnvIfExists()
{
    try
    {
        var searchDirs = new List<string>
        {
            Directory.GetCurrentDirectory(),
            AppDomain.CurrentDomain.BaseDirectory,
            Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..")),
            Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "../..")),
            Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "../../..")),
            Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "../../../.."))
        };

        foreach (var dir in searchDirs)
        {
            var envPath = Path.Combine(dir, ".env");
            if (File.Exists(envPath))
            {
                var lines = File.ReadAllLines(envPath);
                foreach (var line in lines)
                {
                    var trimmed = line.Trim();
                    if (string.IsNullOrWhiteSpace(trimmed) || trimmed.StartsWith("#") || !trimmed.Contains('='))
                        continue;

                    var parts = trimmed.Split('=', 2);
                    var key = parts[0].Trim();
                    var val = parts[1].Trim().Trim('"', '\'');

                    if (!string.IsNullOrEmpty(key) && string.IsNullOrEmpty(Environment.GetEnvironmentVariable(key)))
                    {
                        Environment.SetEnvironmentVariable(key, val);
                    }
                }
                break;
            }
        }
    }
    catch { }
}
