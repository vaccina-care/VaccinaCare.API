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

builder.ConfigureServices();
builder.Services.AddEndpointsApiExplorer();

var app = builder.Build();

app.ConfigurePipeline();
app.UseCors("CorsPolicy");

app.UseAuthentication();

app.UseAuthorization();
app.Run();
