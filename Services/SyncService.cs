using PlatformWellSync.Data;
using PlatformWellSync.Models;
using Microsoft.EntityFrameworkCore;

namespace PlatformWellSync.Services;

public class SyncService
{
    private readonly AppDbContext _db;
    private readonly ApiService _api;
    private readonly ILogger<SyncService> _logger;

    public SyncService(AppDbContext db, ApiService api, ILogger<SyncService> logger)
    {
        _db = db;
        _api = api;
        _logger = logger;
    }

    public async Task RunAsync()
    {
        // Step 1: Login and get token
        var token = await _api.LoginAsync();

        // Step 2: Fetch data — already deserialized into List<Platform> with nested Wells
        var platforms = await _api.GetPlatformWellActualAsync(token);

        // Step 3: Load ALL existing IDs in one query each — no per-record DB hits
        var existingPlatformIds = await _db.Platforms
            .Select(p => p.Id)
            .ToHashSetAsync();

        var existingWellIds = await _db.Wells
            .Select(w => w.Id)
            .ToHashSetAsync();

        int platformsAdded = 0, platformsUpdated = 0;
        int wellsAdded = 0, wellsUpdated = 0;

        foreach (var platform in platforms)
        {
            if (existingPlatformIds.Contains(platform.Id))
            {
                // UPDATE — platform already exists in DB
                var existing = await _db.Platforms.FindAsync(platform.Id);
                if (existing != null)
                {
                    existing.UniqueName = platform.UniqueName;
                    existing.Latitude = platform.Latitude;
                    existing.Longitude = platform.Longitude;
                    existing.UpdatedAt = platform.UpdatedAt;
                    // CreatedAt is never updated — it stays as original
                    platformsUpdated++;
                }
            }
            else
            {
                // INSERT — new platform
                _db.Platforms.Add(new Platform
                {
                    Id = platform.Id,
                    UniqueName = platform.UniqueName,
                    Latitude = platform.Latitude,
                    Longitude = platform.Longitude,
                    CreatedAt = platform.CreatedAt,
                    UpdatedAt = platform.UpdatedAt
                });
                existingPlatformIds.Add(platform.Id); // prevent duplicate in same batch
                platformsAdded++;
            }

            // Step 4: Process wells nested inside this platform
            var wells = platform.Wells ?? new List<Well>();

            foreach (var well in wells)
            {
                if (existingWellIds.Contains(well.Id))
                {
                    // UPDATE — well already exists in DB
                    var existingWell = await _db.Wells.FindAsync(well.Id);
                    if (existingWell != null)
                    {
                        existingWell.UniqueName = well.UniqueName;
                        existingWell.Latitude = well.Latitude;
                        existingWell.Longitude = well.Longitude;
                        existingWell.PlatformId = well.PlatformId;
                        existingWell.UpdatedAt = well.UpdatedAt;
                        // CreatedAt is never updated
                        wellsUpdated++;
                    }
                }
                else
                {
                    // INSERT — new well
                    _db.Wells.Add(new Well
                    {
                        Id = well.Id,
                        PlatformId = well.PlatformId,
                        UniqueName = well.UniqueName,
                        Latitude = well.Latitude,
                        Longitude = well.Longitude,
                        CreatedAt = well.CreatedAt,
                        UpdatedAt = well.UpdatedAt
                    });
                    existingWellIds.Add(well.Id); // prevent duplicate in same batch
                    wellsAdded++;
                }
            }
        }

        // Step 5: Save ALL changes in ONE single database round-trip
        await _db.SaveChangesAsync();

        _logger.LogInformation(
            "Sync complete. Platforms: +{PA} updated:{PU} | Wells: +{WA} updated:{WU}",
            platformsAdded, platformsUpdated, wellsAdded, wellsUpdated
        );
    }
}