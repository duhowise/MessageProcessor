namespace MessageProcessor.Models;

public class LowWeatherForecast
{
    public ForecastType ForecastType { get; set; }
    public LowWeatherForecast()
    {
        ForecastType = ForecastType.Low;
    }
}