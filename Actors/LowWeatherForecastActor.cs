using Akka.Actor;
using MessageProcessor.Models;

namespace MessageProcessor.Actors;

public class LowWeatherForecastActor:ReceiveActor
{
    public LowWeatherForecastActor()
    {
        Receive<LowWeatherForecast>(forecast =>
        {
            Console.WriteLine($"Received a low weather forecast with temperature {forecast.ForecastType}");
        });
    }
}