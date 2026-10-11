namespace AutoLineCRM.SharedKernel.Entities;

public class Review
{
    public int Id { get; set; }
    public int Rating { get; set; }
    public string Comment { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsPublished { get; set; }

    public int ClientId { get; set; }
    public Client Client { get; set; } = null!;

    public int CarId { get; set; }
    public Car Car { get; set; } = null!;
}