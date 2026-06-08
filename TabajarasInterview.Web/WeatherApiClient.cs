namespace TabajarasInterview.Web;

public class WeatherApiClient()
{
    public WeatherForecast[] GetWeather(int maxItems = 10)
    {
        List<WeatherForecast>? forecasts = null;

        for (int i = 0; i < maxItems; i++)
        {
            var forecast = new WeatherForecast(
                DateOnly.FromDateTime(DateTime.Now.AddDays(i)),
                Random.Shared.Next(-20, 42),
                "Sample summary"
            );
            forecasts ??= [];
            forecasts.Add(forecast);
        }

        return forecasts?.ToArray() ?? [];
    }
}

public record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
