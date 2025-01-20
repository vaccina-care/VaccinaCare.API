using System.Text.Json;
using System.Text.Json.Serialization;
using VaccinaCare.API.Architechture;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    });
// Add environment variables to configuration
builder.Configuration
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables();

builder.ConfigureServices();
builder.Services.AddEndpointsApiExplorer();

var app = builder.Build();

app.ConfigurePipeline();
app.UseCors("CorsPolicy");

app.UseAuthentication();

app.UseAuthorization();
app.Run();
