using Akka.Actor;
using Akka.Hosting;
using MessageProcessor.Actors;
using MessageProcessor.Models;
using Microsoft.AspNetCore.Mvc;

namespace MessageProcessor.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class WeatherForecastController : ControllerBase
    {
        

        private readonly ILogger<WeatherForecastController> _logger;
        private readonly IActorRef _mainActor;

        public WeatherForecastController(ILogger<WeatherForecastController> logger,IRequiredActor<MainActor> mainActorRequired)
        {
            _logger = logger;
            _mainActor = mainActorRequired.ActorRef;
        }
        
        [HttpPost("high")]
        public async Task<IActionResult> PostHighWeatherForecast()
        {
            _mainActor.Tell(new HighWeatherForecast());
          await  Task.CompletedTask;
            return Ok();
        } 
        
        [HttpPost("medium")]
        public async Task<IActionResult> PostMediumWeatherForecast()
        {
            _mainActor.Tell(new MediumWeatherForecast());
          await  Task.CompletedTask;
            return Ok();
        }
        [HttpPost("low")]
        public async Task<IActionResult> PostLowWeatherForecast()
        {
            _mainActor.Tell(new LowWeatherForecast());
          await  Task.CompletedTask;
            return Ok();
        }
    }
}
