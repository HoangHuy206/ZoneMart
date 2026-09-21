using System.Diagnostics;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ZoneMart.Server.Services;

/// <summary>
/// Quản lý tiến trình Python UniFace Microservice (Cổng 8000) tự động khi khởi động / dừng ASP.NET Core Server
/// </summary>
public class PythonFaceServiceManager : IHostedService
{
    private readonly ILogger<PythonFaceServiceManager> _logger;
    private readonly IHttpClientFactory _httpClientFactory;
    private Process? _pythonProcess;
    private bool _startedByUs = false;

    public PythonFaceServiceManager(
        ILogger<PythonFaceServiceManager> logger,
        IHttpClientFactory httpClientFactory)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            // 1. Kiểm tra xem cổng 8000 đã có UniFace service đang chạy chưa
            bool alreadyRunning = await IsFaceServiceRunningAsync();
            if (alreadyRunning)
            {
                Console.WriteLine("✅ [FaceService] UniFace Python Microservice đã sẵn sàng tại http://127.0.0.1:8000.");
                return;
            }

            // 2. Tìm thư mục face_service
            string? faceServiceDir = FindFaceServiceDirectory();
            if (string.IsNullOrEmpty(faceServiceDir) || !File.Exists(Path.Combine(faceServiceDir, "main.py")))
            {
                Console.WriteLine("⚠️ [FaceService] Không tìm thấy thư mục face_service/main.py.");
                return;
            }

            Console.WriteLine($"🚀 [FaceService] Tự động khởi chạy UniFace Python Microservice từ '{faceServiceDir}'...");

            var startInfo = new ProcessStartInfo
            {
                FileName = "python",
                Arguments = "-m uvicorn main:app --host 127.0.0.1 --port 8000",
                WorkingDirectory = faceServiceDir,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            _pythonProcess = new Process { StartInfo = startInfo, EnableRaisingEvents = true };

            _pythonProcess.OutputDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrWhiteSpace(e.Data))
                {
                    if (e.Data.Contains("Uvicorn running on") || e.Data.Contains("Application startup complete"))
                    {
                        Console.WriteLine($"🎯 [PythonFaceService] {e.Data}");
                    }
                }
            };

            _pythonProcess.ErrorDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrWhiteSpace(e.Data) && !e.Data.Contains("INFO:"))
                {
                    _logger.LogDebug($"[PythonFaceService] {e.Data}");
                }
            };

            _pythonProcess.Exited += (sender, e) =>
            {
                if (_startedByUs)
                {
                    Console.WriteLine("⚠️ [FaceService] Tiến trình UniFace Python Microservice đã dừng.");
                }
            };

            _pythonProcess.Start();
            _pythonProcess.BeginOutputReadLine();
            _pythonProcess.BeginErrorReadLine();
            _startedByUs = true;

            Console.WriteLine($"✅ [FaceService] Đã kích hoạt UniFace Python Microservice (PID: {_pythonProcess.Id}) trên cổng 8000!");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ [FaceService] Lỗi khi tự động khởi chạy Python Face Service: {ex.Message}");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        if (_startedByUs && _pythonProcess != null && !_pythonProcess.HasExited)
        {
            try
            {
                Console.WriteLine("🛑 [FaceService] Đang dừng UniFace Python Microservice...");
                _pythonProcess.Kill(entireProcessTree: true);
                _pythonProcess.Dispose();
                _pythonProcess = null;
                Console.WriteLine("✅ [FaceService] Đã tắt tiến trình Python Microservice an toàn.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ [FaceService] Lỗi khi tắt tiến trình Python: {ex.Message}");
            }
        }

        return Task.CompletedTask;
    }

    private async Task<bool> IsFaceServiceRunningAsync()
    {
        try
        {
            var client = _httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromMilliseconds(800);
            var res = await client.GetAsync("http://127.0.0.1:8000/api/health");
            return res.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    private static string? FindFaceServiceDirectory()
    {
        var candidates = new[]
        {
            Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "..", "face_service")),
            Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "face_service")),
            Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "face_service")),
            Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "face_service")),
            @"D:\ZoneMart\face_service"
        };

        foreach (var c in candidates)
        {
            if (Directory.Exists(c) && File.Exists(Path.Combine(c, "main.py")))
            {
                return c;
            }
        }

        return null;
    }
}

