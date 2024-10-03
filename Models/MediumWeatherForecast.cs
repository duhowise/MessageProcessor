namespace MessageProcessor.Models;

public class MediumWeatherForecast
{
    public ForecastType ForecastType { get; set; }
    public MediumWeatherForecast()
    {
        ForecastType = ForecastType.Medium;
    }
}