using Fido2NetLib;
using ZoneMart.Server.Services;
using ZoneMart.Server.Settings;

var builder = WebApplication.CreateBuilder(args);

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
builder.Services.AddHostedService<PythonFaceServiceManager>();

// 4. MemoryCache & WebAuthn / Passkeys (FIDO2)
builder.Services.AddMemoryCache();
builder.Services.AddFido2(options =>
{
    options.RPID = "localhost";
    options.RPName = "ZoneMart";
    options.Origins = new HashSet<string>
    {
        "http://localhost:5173",
        "http://127.0.0.1:5173",
        "http://localhost:5000",
        "http://127.0.0.1:5000",
        "https://localhost:5173",
        "https://localhost:5000"
    };
});

// 5. Cấu hình CORS cho phép Vue 3 Frontend, Capacitor iOS & LAN gọi API
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowVueClient", policy =>
    {
        policy.SetIsOriginAllowed(origin => true)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
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

#if NET9_0_OR_GREATER
app.MapOpenApi();
#else
app.UseSwagger();
app.UseSwaggerUI();
#endif

app.UseCors("AllowVueClient");

// app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

Console.WriteLine("🚀 ZoneMart C# ASP.NET Core Web API đang khởi động...");
Console.WriteLine("📧 Đã nạp cấu hình gửi thư Gmail: hh9393100@gmail.com");
Console.WriteLine("🤖 Đã nạp cấu hình Bot Telegram: @ZoneMarttt_bot");
Console.WriteLine("🛡️ Đã sẵn sàng hệ thống WebAuthn / Passkey Sinh trắc học!");

Console.WriteLine("🌐 Lắng nghe trên tất cả cổng mạng LAN: http://0.0.0.0:5000");

app.Run("http://0.0.0.0:5000");
