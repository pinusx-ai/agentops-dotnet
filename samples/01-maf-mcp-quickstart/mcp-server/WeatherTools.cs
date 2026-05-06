using ModelContextProtocol.Server;
using System.ComponentModel;

namespace AgentOps.Quickstart.McpServer;

[McpServerToolType]
public static class WeatherTools
{
    [McpServerTool, Description("Returns the current weather for a city")]
    public static string GetWeather(
        [Description("City name, e.g. 'Salem'")] string city)
    {
        // Hardcoded for now — real version would call a weather API
        return city.ToLowerInvariant() switch
        {
            "salem" => "Salem: 32°C, partly cloudy, light breeze",
            "newark" => "Newark: 18°C, overcast, chance of rain",
            "seattle" => "Seattle: 12°C, raining (obviously)",
            _ => $"No weather data for '{city}' yet."
        };
    }

    [McpServerTool, Description("Returns a list of supported cities")]
    public static string[] ListCities() =>
        ["Salem", "Newark", "Seattle"];
}