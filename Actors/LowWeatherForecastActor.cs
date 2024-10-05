using Akka.Actor;
using MessageProcessor.Models;
using MessageProcessor.Services;

namespace MessageProcessor.Actors;

public class LowWeatherForecastActor:ReceiveActor
{
    public LowWeatherForecastActor(IServiceScopeFactory serviceScopeFactory)
    {
        using var scope = serviceScopeFactory.CreateScope();
        var weatherService = scope.ServiceProvider.GetRequiredService<WeatherService>();
        Receive<LowWeatherForecast>(forecast =>
        {
            Console.WriteLine($"Received a low weather forecast with temperature {forecast.ForecastType}");
            Console.WriteLine($"The weather is {weatherService.GetWeather()}");
        });
    }
}