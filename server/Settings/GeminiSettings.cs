namespace ZoneMart.Server.Settings;

public class GeminiSettings
{
    public string ApiKey { get; set; } = string.Empty;
    public string Model { get; set; } = "gemini-3-flash-preview";
    public int TimeoutSeconds { get; set; } = 10;
}

