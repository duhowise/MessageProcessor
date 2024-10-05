using Akka.Actor;
using Akka.DependencyInjection;
using MessageProcessor.Models;

namespace MessageProcessor.Actors;

public class MainActor:ReceiveActor
{
    public MainActor()
    {
        var resolver = DependencyResolver.For(Context.System);
        var lowWeatherForecastActor = Context.ActorOf(resolver.Props<LowWeatherForecastActor>(),nameof(LowWeatherForecastActor));
        var mediumWeatherForecastActor = Context.ActorOf(Props.Create <MediumWeatherForecastActor >(),nameof(MediumWeatherForecastActor));
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