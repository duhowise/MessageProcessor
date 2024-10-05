using Akka.Actor;
using MessageProcessor.Models;
using MessageProcessor.Services;

namespace MessageProcessor.Actors;

public class MainActor:ReceiveActor
{
    public MainActor(WeatherService weatherService)
    {
        var lowWeatherForecastActor = Context.ActorOf(Props.Create(()=>new LowWeatherForecastActor(weatherService)), nameof(LowWeatherForecastActor));
        var mediumWeatherForecastActor = Context.ActorOf(Props.Create < MediumWeatherForecastActor >(),nameof(MediumWeatherForecastActor));
        var highWeatherForecastActor = Context.ActorOf(Props.Create<HighWeatherForecastActor>(),nameof(HighWeatherForecastActor));
        
        
        
        Receive<LowWeatherForecast>(forecast =>
        {
            lowWeatherForecastActor.Forward(forecast);
        });
        Receive<MediumWeatherForecast>(forecast =>
        {
            mediumWeatherForecastActor.Forward(forecast);
        }); 
        Receive<HighWeatherForecast>(forecast =>
        {
            highWeatherForecastActor.Forward(forecast);
        });
    }
}