using AutoLineCRM.SharedKernel.Entities;
using AutoLineCRM.SharedKernel.Enums;
using Microsoft.EntityFrameworkCore;

namespace AutoLineCRM.CatalogService.Data;

public static class CatalogSeeder
{
    private record Sample(string Brand, string Model, string Trim, int Year, int Mileage,
                          decimal Price, CarStatus Status, string Photo);

    private static readonly Sample[] Samples =
    {
        new("BMW", "5 Series", "530i xDrive", 2021, 42000, 38500, CarStatus.Available, "bmw"),
        new("BMW", "X5", "xDrive40i", 2022, 25000, 62900, CarStatus.Available, "bmw"),
        new("Audi", "A6", "45 TFSI quattro", 2020, 58000, 34900, CarStatus.Available, "audi"),
        new("Audi", "Q7", "55 TFSI", 2021, 36000, 59500, CarStatus.Reserved, "audi"),
        new("Mercedes-Benz", "E-Class", "E 220 d", 2019, 77000, 31800, CarStatus.Available, "mercedes"),
        new("Mercedes-Benz", "GLE", "350 de 4MATIC", 2022, 18000, 71200, CarStatus.Available, "mercedes"),
        new("Toyota", "Camry", "2.5 Hybrid", 2021, 39000, 27900, CarStatus.Available, "toyota"),
        new("Toyota", "RAV4", "2.5 AWD", 2020, 61000, 28400, CarStatus.Sold, "toyota"),
        new("Tesla", "Model 3", "Long Range", 2022, 21000, 36900, CarStatus.Available, "tesla"),
        new("Volkswagen", "Passat", "2.0 TDI", 2018, 98000, 17900, CarStatus.Available, "volkswagen"),
        new("Porsche", "Macan", "S", 2021, 33000, 69800, CarStatus.Reserved, "porsche"),
        new("Skoda", "Octavia", "RS", 2020, 52000, 24600, CarStatus.Available, "skoda"),
    };

    public static async Task SeedAsync(CatalogDbContext db)
    {
        var existing = (await db.Cars.Select(c => c.VIN).ToListAsync()).ToHashSet();

        for (var i = 0; i < Samples.Length; i++)
        {
            var s = Samples[i];
            var vin = $"ALCSEED{i + 1:D10}";   // 17 символів
            if (existing.Contains(vin)) continue;

            var car = new Car
            {
                VIN = vin,
                Brand = s.Brand,
                Model = s.Model,
                Trim = s.Trim,
                Year = s.Year,
                Mileage = s.Mileage,
                Price = s.Price,
                Status = s.Status
            };
            car.Images.Add(new CarImage
            {
                Url = $"https://loremflickr.com/800/500/{s.Photo}?lock={i + 1}",
                IsMain = true
            });
            db.Cars.Add(car);
        }

        await db.SaveChangesAsync();
    }
}