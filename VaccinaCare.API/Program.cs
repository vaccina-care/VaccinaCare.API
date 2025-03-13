using System.IdentityModel.Tokens.Jwt;
using System.Text.Json;
using System.Text.Json.Serialization;
using SwaggerThemes;
using VaccinaCare.API.Architechture;

var builder = WebApplication.CreateBuilder(args);
// Load cấu hình từ appsettings.json và environment variables
builder.Configuration.AddEnvironmentVariables(); // Đọc từ biến môi trường
builder.Configuration.AddJsonFile("appsettings.json", true, true); // Đọc từ appsettings.json nếu có

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    });

// Tắt việc map claim mặc định
JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();

builder.WebHost.UseUrls("http://0.0.0.0:5000");
builder.Services.SetupIOCContainer();
builder.Services.AddEndpointsApiExplorer();

// Build the app
var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "VaccinaCare API v1");
        c.RoutePrefix = string.Empty;
        c.InjectStylesheet("/swagger-ui/custom-theme.css");
        c.HeadContent = $"<style>{SwaggerTheme.GetSwaggerThemeCss(Theme.Dracula)}</style>";
    });
}

// app.UseHttpsRedirection();

app.UseRouting();
try
{
    app.ApplyMigrations(app.Logger);
}
catch (Exception e)
{
    app.Logger.LogError(e, "An problem occurred during migration!");
}

app.UseStaticFiles();

app.UseSwagger();

app.UseSwaggerUI(c =>
{
    c.InjectJavascript("./custom-swagger.js");
    c.InjectStylesheet("./custom-swagger.css");
});
app.UseCors("CorsPolicy");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();