using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Http.Resilience;
using PlatformWellSync.Configuration;
using PlatformWellSync.Data;
using PlatformWellSync.Services;
using PlatformWellSync.Workers;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((ctx, services) =>
    {
        // 1. Bind appsettings.json "Api" section to ApiSettings class
        services.Configure<ApiSettings>(ctx.Configuration.GetSection("Api"));

        // 2. Register DbContext with SQL Server LocalDB connection string
        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlServer(
               ctx.Configuration.GetConnectionString("DefaultConnection")
            )
        );

        // 3. Register ApiService with HttpClient + retry policy
        //    Retries 3 times with exponential backoff: 2s → 4s → 8s
        services.AddHttpClient<ApiService>()
            .AddStandardResilienceHandler();

        // 4. Register SyncService as scoped
        //    Scoped = fresh instance created and disposed per sync run
        services.AddScoped<SyncService>();

        // 5. Register SyncWorker as the background hosted service
        //    This is what runs the timer loop
        services.AddHostedService<SyncWorker>();
    })
    .Build();

// 6. Auto-apply EF Core migrations on startup
//    Creates the database and tables if they don't exist yet
//    No need to run "dotnet ef database update" manually
using (var scope = host.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}

await host.RunAsync();