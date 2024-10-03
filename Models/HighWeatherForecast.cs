namespace MessageProcessor.Models;

public class HighWeatherForecast
{
    public ForecastType ForecastType { get; set; }
    public HighWeatherForecast()
    {
        ForecastType = ForecastType.High;
    }
}