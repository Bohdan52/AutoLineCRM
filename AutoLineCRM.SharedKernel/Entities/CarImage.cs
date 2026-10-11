namespace AutoLineCRM.SharedKernel.Entities;

public class CarImage
{
    public int Id { get; set; }
    public string Url { get; set; } = string.Empty;
    public bool IsMain { get; set; }            

    public int CarId { get; set; }
    public Car Car { get; set; } = null!;
}