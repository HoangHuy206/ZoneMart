using ZoneMart.Server.Services;
using ZoneMart.Server.Settings;

var builder = WebApplication.CreateBuilder(args);

// 1. Cấu hình MongoDB Atlas
builder.Services.Configure<MongoDBSettings>(
    builder.Configuration.GetSection("MongoDBSettings")
);
builder.Services.AddSingleton<MongoDbService>();

// 2. Cấu hình CORS cho phép Vue 3 Frontend (localhost:5173) gọi API
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
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseCors("AllowVueClient");

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

Console.WriteLine("🚀 ZoneMart C# ASP.NET Core Web API đang khởi động...");
Console.WriteLine("📦 Kết nối Database: MongoDB Atlas (Cluster0)");

app.Run();
