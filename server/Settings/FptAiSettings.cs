namespace ZoneMart.Server.Settings;

public class FptAiSettings
{
    public string ApiKey { get; set; } = string.Empty;
    public string Endpoint { get; set; } = "https://api.fpt.ai/vision/id-recognition/v2";
    public int TimeoutSeconds { get; set; } = 30;
}

