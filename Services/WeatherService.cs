namespace MessageProcessor.Services;

public class WeatherService
{
    private readonly Random _random;

    public WeatherService()
    {
        _random = new Random();
    }

    public string GetWeather()
    {
        string[] weatherConditions = { "Sunny", "Cloudy", "Rainy", "Snowy" };
        int randomIndex = _random.Next(weatherConditions.Length);
        return weatherConditions[randomIndex];
    }
}
