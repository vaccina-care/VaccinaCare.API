using Microsoft.EntityFrameworkCore;
using VaccinaCare.Domain;

namespace VaccinaCare.API.Architechture
{
    public static class MigrationExtensions
    {

        public static void WaitForDatabase(this IApplicationBuilder app, ILogger logger, int retries = 10, int delay = 5000)
        {
            for (int i = 0; i < retries; i++)
            {
                try
                {
                    using var scope = app.ApplicationServices.CreateScope();
                    var dbContext = scope.ServiceProvider.GetRequiredService<VaccinaCareDbContext>();
                    dbContext.Database.CanConnect();
                    logger.LogInformation("Database is ready.");
                    return;
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Database is not ready. Retrying in {Delay}ms ({Attempt}/{Retries})", delay, i + 1, retries);
                    Thread.Sleep(delay);
                }
            }

            logger.LogError("Unable to connect to the database after {Retries} attempts.", retries);
            throw new Exception("Database connection failed.");
        }
        public static void ApplyMigrations(this IApplicationBuilder app, ILogger _logger)
        {
            const int maxRetries = 5; // Số lần retry
            const int delayMilliseconds = 5000; // Thời gian đợi giữa các lần retry (5 giây)

            for (int retry = 0; retry < maxRetries; retry++)
            {
                try
                {
                    _logger.LogInformation("Migration process started. Attempt {Retry}/{MaxRetries}", retry + 1, maxRetries);

                    // Bắt đầu tạo scope
                    using IServiceScope scope = app.ApplicationServices.CreateScope();
                    _logger.LogDebug("Service scope created successfully.");

                    // Lấy DbContext từ scope
                    using VaccinaCareDbContext dbContext =
                        scope.ServiceProvider.GetRequiredService<VaccinaCareDbContext>();
                    _logger.LogDebug("DbContext resolved from service provider successfully.");

                    // Áp dụng migration
                    _logger.LogInformation("Applying migrations...");
                    dbContext.Database.Migrate();
                    _logger.LogInformation("Migrations applied successfully on attempt {Retry}/{MaxRetries}!", retry + 1, maxRetries);

                    return; // Thành công thì thoát
                }
                catch (Exception e)
                {
                    // Log lỗi chi tiết
                    _logger.LogError(e, "Migration failed on attempt {Retry}/{MaxRetries}", retry + 1, maxRetries);

                    if (retry == maxRetries - 1)
                    {
                        _logger.LogCritical("All migration attempts failed after {MaxRetries} retries. Aborting.", maxRetries);
                        throw; // Quăng lỗi khi hết số lần retry
                    }

                    // Log cảnh báo và chờ retry
                    _logger.LogWarning("Retrying migration in {DelayMilliseconds} milliseconds... (Attempt {Retry}/{MaxRetries})", delayMilliseconds, retry + 1, maxRetries);
                    Thread.Sleep(delayMilliseconds); // Đợi trước khi thử lại
                }
            }
        }
    }


}
