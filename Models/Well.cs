namespace PlatformWellSync.Models;

public class Well
{
    public int Id { get; set; }
    public int PlatformId { get; set; }
    public string? UniqueName { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public Platform? Platform { get; set; }
}