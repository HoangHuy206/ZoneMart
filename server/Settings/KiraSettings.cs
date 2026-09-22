namespace ZoneMart.Server.Settings;

public class KiraSettings
{
    public string ApiKey { get; set; } = string.Empty;
    public string Endpoint { get; set; } = "https://api.kira.ai/v1/chat/completions";
    public string Model { get; set; } = "gpt-6-astra";
    public int TimeoutSeconds { get; set; } = 15;
}

