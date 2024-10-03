using Akka.Actor;
using MessageProcessor.Models;

namespace MessageProcessor.Actors;

public class HighWeatherForecastActor:ReceiveActor
{
    public HighWeatherForecastActor()
    {
        Receive<HighWeatherForecast>(forecast =>
        {
            Console.WriteLine($"Received a high weather forecast with temperature {forecast.ForecastType}");
        });
    }
}