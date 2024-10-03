
using Akka.Actor;
using Akka.Hosting;
using MessageProcessor.Actors;

namespace MessageProcessor
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.

            builder.Services.AddControllers();
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();
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

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();

            app.UseAuthorization();


            app.MapControllers();

            app.Run();
        }
    }
}
