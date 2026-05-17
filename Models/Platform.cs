using Newtonsoft.Json;

namespace PlatformWellSync.Models;

public class Platform
{
    public int Id { get; set; }
    public string? UniqueName { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    [JsonProperty("well")]                                         // ← maps "well" from API to Wells property
    public ICollection<Well> Wells { get; set; } = new List<Well>();
}