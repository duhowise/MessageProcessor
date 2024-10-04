# Escape Akka.Hosting's Child Actor Migration Hell with DependencyResolver

Migrating to Akka.Hosting can present challenges, especially when dealing with child actors and dependency injection. However, by leveraging the `DependencyResolver` provided by `Akka.DependencyInjection`, you can streamline the process and enhance your actor management. This article will guide you through the steps to effectively use `DependencyResolver` for creating child actors in an Akka.Hosting environment.

## Setting Up Akka.Hosting

First, ensure you have the necessary dependencies in your project. You will need the Akka.Hosting and Akka.DependencyInjection package, which can be added via NuGet:

```bash
dotnet add package Akka.Hosting
dotnet add package Akka.DependencyInjection
```

Next, set up your `ActorSystem` in the `Program.cs` of your application:

```csharp
 builder.Services.AddAkka("MessageProcessor", (configurationBuilder) =>
  {
    configurationBuilder.WithActors((system, registry, resolver) =>
    {
         //register actors here
     });         
 });
```

## Setup Main Actor

Once your `ActorSystem` is set up, you can set up your main actor that will be responsible for creating child actors later:

1. **Define Your Main Actor**: Create a main actor that will handle messages and create child actors as needed. Here is an example of a main actor with message handling:

```csharp
﻿using Akka.Actor;
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
```

2. **Register the Actor**: Use the Akka.Hosting API to register your actor in the `ConfigureServices` method.

```csharp
var builder = WebApplication.CreateBuilder(args);

             
builder.Services.AddAkka("MessageProcessor", (configurationBuilder) =>
{
 configurationBuilder.WithActors((system, registry, resolver) =>
 {
  //props
  var mainActorProps = resolver.Props<MainActor>()
    .WithSupervisorStrategy(SupervisorStrategy.DefaultStrategy);
    //instance
    var mainActor = system.ActorOf(mainActorProps, nameof(MainActor));
   //registry
   registry.Register<MainActor>(mainActor);
 });
});
var app = builder.Build();
```

3.**Refactor MainActor into Constituent Child Actors**

```csharp
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
```

4. **New MainActor Without the `DependencyResolver`**

After we are done separating the main actor logic into each child actor; it is time to instantiate each of the child actors and their dependencies, but this would present a number of drawbacks such as tight coupling,reduced testability etc.
See below how that would look like:

```csharp
public class MainActor : ReceiveActor
{
    private readonly IActorRef _mediumWeatherForecastActor;
    private readonly IActorRef _lowWeatherForecastActor;
    private readonly IActorRef _highWeatherForecastActor;

    public MainActor()
    {
        _highWeatherForecastActor = Context.ActorOf(Props.Create(() => new HighWeatherForecastActor()), "HighWeatherForecastActor");
        _lowWeatherForecastActor = Context.ActorOf(Props.Create(() => new LowWeatherForecastActor()), "LowWeatherForecastActor");
        _mediumWeatherForecastActor = Context.ActorOf(Props.Create(() => new MediumWeatherForecastActor()), "MediumWeatherForecastActor");

        Receive<LowWeatherForecast>(message => _lowWeatherForecastActor.Forward(message));
        Receive<MediumWeatherForecast>(message => _mediumWeatherForecastActor.Forward(message));
        Receive<highWeatherForecastActor>(message => _highWeatherForecastActor.Forward(message));
    }
}
```
## Refactor to Dependency Resolver
1. **Recreate the Child Actors**: In your main actor using the `DependencyResolver.For`.
   We refactor the same Main Actor to swap out the various manual initialisations in favour of the dependency resolver.

```csharp
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
        var mediumWeatherForecastActor = Context.ActorOf(resolver.Props<MediumWeatherForecastActor>(),nameof(MediumWeatherForecastActor));
        var highWeatherForecastActor = Context.ActorOf(resolver.Props<HighWeatherForecastActor>(),nameof(HighWeatherForecastActor));
        
        
        
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
```


## Considerations for Child Actors with Dependencies

Actors in Akka are designed to be long-lived and can be thought of as "singletons" within their context. This characteristic has implications for how dependencies are managed, particularly when dealing with scoped dependencies. Scoped dependencies are typically created per request or per operation, which contrasts with the long-lived nature of actors. 
When injecting scoped dependencies into actors that use the `DependencyResolver`, it important to note that scoped dependencies will throw a `TypeLoadException`. Registering dependencies as singletons or transient however work. Depending on your usecase, this may or may not pose challenges to other parts of your system and should be considered carefully.
```csharp
var builder = WebApplication.CreateBuilder(args);

 // add transient or singleton services for actor dependencies.
 //scoped usually causes`TypeLoadException`
 builder.Services.AddTransient<WeatherService>();

 var app = builder.Build();

 //actor class using the dependency
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
```

## Conclusion

By utilizing the `DependencyResolver` in `Akka.DependencyInjection`, you can effectively manage child actors and their dependencies, simplifying the migration process. This approach not only enhances the maintainability of your code but also aligns with modern dependency injection practices. With the steps outlined above, you can escape the complexities of child actor management and focus on building robust Akka applications.

## References
- [Akka.Hosting](https://petabridge.com/blog/intro-akka-hosting)
- [Akka.DependencyInjection](https://getakka.net/articles/actors/dependency-injection.html)
- [Akka.NET](https://getakka.net/)
- [Sample Code](https://github.com/duhowise/MessageProcessor)