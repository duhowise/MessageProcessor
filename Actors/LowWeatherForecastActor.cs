using Akka.Actor;
using MessageProcessor.Models;
using MessageProcessor.Services;

namespace MessageProcessor.Actors;

public class LowWeatherForecastActor:ReceiveActor
{
    public LowWeatherForecastActor(WeatherService weatherService)
    {
        Receive<LowWeatherForecast>(forecast =>
        {
            Console.WriteLine($"Received a low weather forecast with temperature {forecast.ForecastType}");
            Console.WriteLine($"The weather is {weatherService.GetWeather()}");
        });
    }
}