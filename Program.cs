

using Microsoft.Data.Sqlite;
using ModStats.Controllers;
using System.Diagnostics.Eventing.Reader;
using System.Runtime.CompilerServices;

namespace ModStats
{
    public class Program
    {
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        public static IWebHostEnvironment Environment { get; private set; }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.


        public static void Main(string[] args)
        {
            RuntimeHelpers.RunClassConstructor(typeof(DataStore).TypeHandle);

            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddControllers();
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            var app = builder.Build();
            Environment = app.Environment;

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();

            app.UseAuthorization();

            //app.Use(async (ctx, next) =>
            //{
            //    Console.WriteLine("HTTP Endpoint displayname: " + ctx.GetEndpoint()?.DisplayName);
            //    await next.Invoke();
            //});

            app.MapControllers();


            //Console.WriteLine("App listening on...");
            //foreach (string url in app.Urls)
            //{
            //    Console.WriteLine($" - {url}");
            //}

            //if (app.Environment.IsDevelopment())
            //app.Run();
            //else
            Console.WriteLine("");
            app.Run("http://stats.extraes.xyz");
        }
    }
}