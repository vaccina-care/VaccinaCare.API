using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using VaccinaCare.API.Architechture;

var builder = WebApplication.CreateBuilder(args);

// Configure services
builder.ConfigureServices();

// Build the app
var app = builder.Build();

// Configure the pipeline
app.ConfigurePipeline();

// Run the app
app.Run();