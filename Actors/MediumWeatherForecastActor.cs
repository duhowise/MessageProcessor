using Akka.Actor;
using MessageProcessor.Models;

namespace MessageProcessor.Actors;

public class MediumWeatherForecastActor:ReceiveActor
{
    public MediumWeatherForecastActor()
    {
        Receive<MediumWeatherForecast>(forecast =>
        {
            Console.WriteLine($"Received a medium weather forecast with temperature {forecast.ForecastType}");
        });
    }
}