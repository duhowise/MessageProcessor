# Creating Akka.NET Child Actor's in Dependency Injection Environments

Migrating to Akka.Hosting can introduce several challenges, particularly when dealing with child actors and their dependencies in a Dependency Injection (DI) environment. This article explores multiple approaches to managing child actors within Akka.NET including leveraging Akka’s DependencyResolver to simplify actor creation.

From manually passing dependencies and creating child actors within your main actor to utilizing IServiceProvider or IServiceScopeFactory for managing scoped dependencies, each method comes with its own trade-offs. We'll also demonstrate how the DependencyResolver can streamline the process, helping to decouple actors from their dependencies and improving maintainability.

By comparing these various techniques, you'll be able to choose the best approach for your use case, whether you're looking to minimize code complexity, enhance testability, or maintain more granular control over your actor system.

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
```

2. **Register the Actor**: Use the Akka.Hosting API to register your actor in your `Program.cs`.

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

## Without the `DependencyResolver`

Without the `DependencyResolver`,we could do a number of things to create child actors in the main actor.
1. **One of the ways is to manually create the child actors** and forward messages to them. Since one of our Child Actors has a dependency, we will need to pass the dependency to the child actor's constructor; but that also means that the main actor will need to have access to the dependency. 
This is not ideal as it couples the main actor to the dependency, making it harder to test and maintain.
Below is an example :

```csharp
using Akka.Actor;
using MessageProcessor.Models;
using MessageProcessor.Services;

namespace MessageProcessor.Actors
{
    public class MainActor : ReceiveActor
    {
        
        //main actor takes a dependency on the external service
        public MainActor(WeatherService weatherService)
        {
            // Create child actors with their dependencies
            var lowWeatherForecastActor = Context.ActorOf(Props.Create(() => new LowWeatherForecastActor(weatherService)), nameof(LowWeatherForecastActor));
            var mediumWeatherForecastActor = Context.ActorOf(Props.Create(() => new MediumWeatherForecastActor()), nameof(MediumWeatherForecastActor));
            var highWeatherForecastActor = Context.ActorOf(Props.Create(() => new HighWeatherForecastActor()), nameof(HighWeatherForecastActor));

            // Define message handling
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
}

```
For the parameterless child actors, you could use the generic `Props.Create` method to create the child actors like so:
```csharp
var mediumWeatherForecastActor = Context.ActorOf(Props.Create<MediumWeatherForecastActor>(), nameof(MediumWeatherForecastActor));
After this the main actor can be registered in the `Program.cs` as shown below:

```csharp
var builder = WebApplication.CreateBuilder(args);
// add transient or singleton services for actor dependencies.
 //scoped usually causes`TypeLoadException`
 builder.Services.AddSingleton<WeatherService>();

// Configure Akka.NET ActorSystem
builder.Services.AddAkka("MessageProcessor", (configurationBuilder, provider) =>
{
    configurationBuilder.WithActors((system, registry) =>
    {
        var weatherService = provider.GetRequiredService<WeatherService>();
        var mainActorProps = Props.Create(() => new MainActor(weatherService));
        var mainActor = system.ActorOf(mainActorProps, nameof(MainActor));
        registry.Register<MainActor>(mainActor);
    });
    });

var app = builder.Build();
app.Run();

```
It is immediately visible how much more we needed to write to manage one dependency for one child actor. Imagine if this actor had five more and the other child actors, also had two each; this could immediately evolve into a very hard to manage mess, causing readability to suffer.
2. **`IServiceScopeFactory`, `IServiceProvider` to the rescue**. 
A very useful middle ground in manually creating child actors is to take a dependency only on the `IServiceProvider` or the `IServiceScopeFactory` then creating your own scope and resolving all required dependencies yourself. This way, the main actor is not coupled to the dependency, and the child actors can be created with their dependencies. 
Also, the `IServiceProvider` or `IServiceScopeFactory` can be directly injected into the main actor and passed to the child actors without any need to configure additional dependencies(while registering the main actor). It is also important to note that the `IServiceProvider` and `IServiceScopeFactory` allow you to create your own scope, making it possible to resolve and use scoped dependencies without any issues. 
Our actors and configurations should look now like this:
```csharp
//configuration
 var builder = WebApplication.CreateBuilder(args);

     // Add services to the container.
     builder.Services.AddScoped<WeatherService>();
           
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
     //nothing  changes in the actor registration
      });
     });
 var app = builder.Build();
            
    //main actor
public class MainActor:ReceiveActor
{
    //main actor takes a dependency on the IServiceScopeFactory
    public MainActor(IServiceScopeFactory serviceScopeFactory)
    {
        var lowWeatherForecastActor = Context.ActorOf(Props.Create(()=>new LowWeatherForecastActor(serviceScopeFactory)),nameof(LowWeatherForecastActor));
       //Or
        //var lowWeatherForecastActor = Context.ActorOf(Props.Create <LowWeatherForecastActor >(serviceScopeFactory),nameof(LowWeatherForecastActor));
        //which I consider cleaner. plust it avoids the use of the 'new' keyword.
        var mediumWeatherForecastActor = Context.ActorOf(Props.Create <MediumWeatherForecastActor >(),nameof(MediumWeatherForecastActor));
        var highWeatherForecastActor = Context.ActorOf(Props.Create<HighWeatherForecastActor>(),nameof(HighWeatherForecastActor));
        
        
        Receive<LowWeatherForecast>(forecast =>
        {
            lowWeatherForecastActor.Forward(forecast);
        });
    }
}

//child actor with dependency
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
When injecting scoped dependencies into actors that use the `DependencyResolver`, it important to note that scoped dependencies will throw a `TypeLoadException`. Registering dependencies as singletons or transient however work. Depending on your use case, this may or may not pose challenges to other parts of your system and should be considered carefully.
I suggest however that you take dependencies on `IServiceScopeFactory`,or `IServiceProvider` instead to avoid any issues in this regard.
```csharp
var builder = WebApplication.CreateBuilder(args);

 // add transient or singleton services for actor dependencies.
 //scoped usually causes`TypeLoadException`
 //or use IServiceScopeFactory,IserviceProvider
 builder.Services.AddTransient<WeatherService>();

 var app = builder.Build();

 //directly consume dependency in child actor
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

 //OR depend on IServiceScopeFactory
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
```

## Conclusion

By utilizing the `DependencyResolver` in `Akka.DependencyInjection`, you can effectively manage child actors and their dependencies, simplifying the migration process.
You can also redesign your actors to only depend on the service provider to help reduce the number of dependencies injected during manual child actor creation.
These approaches not only enhance the maintainability of your code but also aligns with modern dependency injection practices while creating child actors.
With the steps outlined above, you can escape the complexities of child actor management and focus on building robust Akka applications.

## References
- [Akka.Hosting](https://petabridge.com/blog/intro-akka-hosting)
- [Akka.DependencyInjection](https://getakka.net/articles/actors/dependency-injection.html)
- [Akka.NET](https://getakka.net/)
- [Sample Code](https://github.com/duhowise/MessageProcessor)