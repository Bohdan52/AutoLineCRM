using AutoLineCRM.SharedKernel.Enums;

namespace AutoLineCRM.SharedKernel.Entities;

public class Car
{
    public int Id { get; set; }
    public string VIN { get; set; } = string.Empty;
    public string Brand { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public string Trim { get; set; } = string.Empty;    
    public int Year { get; set; }
    public int Mileage { get; set; }
    public decimal Price { get; set; }
    public CarStatus Status { get; set; } = CarStatus.Available;

    public DateTime AddedAt { get; set; } = DateTime.UtcNow;

    public ICollection<CarImage> Images { get; set; } = new List<CarImage>();
    
    public override string ToString() => $"[{Id}] {Brand} {Model} ({Year}) — {Price:C} — {Status}";
}