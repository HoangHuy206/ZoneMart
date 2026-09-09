using ZoneMart.Server.Services;
using ZoneMart.Server.Settings;

var builder = WebApplication.CreateBuilder(args);

// 1. Cấu hình MongoDB Atlas
builder.Services.Configure<MongoDBSettings>(
    builder.Configuration.GetSection("MongoDBSettings")
);
builder.Services.AddSingleton<MongoDbService>();

// 2. Cấu hình Email & Telegram Settings
builder.Services.Configure<EmailSettings>(
    builder.Configuration.GetSection("EmailSettings")
);
builder.Services.Configure<TelegramSettings>(
    builder.Configuration.GetSection("TelegramSettings")
);

// 3. Đăng ký Services & HttpClient
builder.Services.AddHttpClient();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<ITelegramService, TelegramService>();

// 4. Cấu hình CORS cho phép Vue 3 Frontend (localhost:5173) gọi API
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowVueClient", policy =>
    {
        policy.WithOrigins("http://localhost:5173", "http://127.0.0.1:5173")
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

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
#if NET9_0_OR_GREATER
    app.MapOpenApi();
#else
    app.UseSwagger();
    app.UseSwaggerUI();
#endif
}
else
{
    app.UseHttpsRedirection();
}

app.UseCors("AllowVueClient");

// app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

Console.WriteLine("🚀 ZoneMart C# ASP.NET Core Web API đang khởi động...");
Console.WriteLine("📧 Đã nạp cấu hình gửi thư Gmail: hh9393100@gmail.com");
Console.WriteLine("🤖 Đã nạp cấu hình Bot Telegram: @ZoneMarttt_bot");

app.Run("http://localhost:5000");
