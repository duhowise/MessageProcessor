using Akka.Actor;
using MessageProcessor.Models;

namespace MessageProcessor.Actors;

public class MainActor:ReceiveActor
{
    public MainActor()
    {
        Receive<LowWeatherForecast>(forecast =>
        {
            Console.WriteLine($"Received a low weather forecast with temperature {forecast.ForecastType}");
        });
        Receive<MediumWeatherForecast>(forecast =>
        {
            Console.WriteLine($"Received a medium weather forecast with temperature {forecast.ForecastType}");
        }); 
        Receive<HighWeatherForecast>(forecast =>
        {
            Console.WriteLine($"Received a High weather forecast with temperature {forecast.ForecastType}");
        });
    }
}