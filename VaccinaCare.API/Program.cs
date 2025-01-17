using Microsoft.EntityFrameworkCore;
using VaccinaCare.API.Architechture;
using VaccinaCare.Domain;
using VaccinaCare.Repository;
using VaccinaCare.Repository.Interfaces;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.UseUrls("http://0.0.0.0:5000");

builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();


builder.Configuration
    .AddEnvironmentVariables();

builder.Services.AddDbContext<VaccinaCareDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    if (string.IsNullOrEmpty(connectionString))
    {
        throw new InvalidOperationException("Connection string không được để trống!");
    }
    options.UseSqlServer(connectionString);
});
var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "VaccinaCare API v1");
        c.RoutePrefix = string.Empty; // Để Swagger UI ở root (http://localhost:5000)
    });

}

// app.UseHttpsRedirection();
app.UseRouting();


using (var scope = app.Services.CreateScope())
{
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    app.WaitForDatabase(logger);
    app.ApplyMigrations(logger);
}


app.UseAuthorization();

app.MapControllers();

app.Run();
