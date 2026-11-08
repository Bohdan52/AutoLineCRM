using AutoLineCRM.CatalogService.Data;
using AutoLineCRM.SharedKernel.Entities;
using AutoLineCRM.SharedKernel.Enums;
using Microsoft.EntityFrameworkCore;

namespace AutoLineCRM.CatalogService.Services;

public class CarService
{
    private readonly CatalogDbContext _db;
    public CarService(CatalogDbContext db) => _db = db;

    public async Task<List<Car>> SearchAsync(
        string? brand, string? model, decimal? minPrice, decimal? maxPrice,
        CarStatus? status, int page = 1, int size = 20)
    {
        page = Math.Max(page, 1);
        size = Math.Clamp(size, 1, 100);
        var q = _db.Cars.AsNoTracking().Include(c => c.Images).AsQueryable();

        if (!string.IsNullOrWhiteSpace(brand))
            q = q.Where(c => EF.Functions.Like(c.Brand, $"%{brand}%"));
        if (!string.IsNullOrWhiteSpace(model))
            q = q.Where(c => EF.Functions.Like(c.Model, $"%{model}%"));
        if (minPrice.HasValue) q = q.Where(c => c.Price >= minPrice.Value);
        if (maxPrice.HasValue) q = q.Where(c => c.Price <= maxPrice.Value);
        if (status.HasValue) q = q.Where(c => c.Status == status.Value);

        return await q.OrderBy(c => c.Brand).ThenBy(c => c.Model)
                      .Skip((page - 1) * size).Take(size)
                      .ToListAsync();
    }

    public Task<Car?> GetByIdAsync(int id) =>
        _db.Cars.Include(c => c.Images).FirstOrDefaultAsync(c => c.Id == id);

    public async Task<Car> CreateAsync(Car car)
    {
        if (string.IsNullOrWhiteSpace(car.VIN))
            throw new ArgumentException("VIN-код є обов'язковим");
        
        if (await _db.Cars.AnyAsync(c => c.VIN == car.VIN))
            throw new InvalidOperationException("Автомобіль з таким VIN-кодом вже існує");
        _db.Cars.Add(car);
        await _db.SaveChangesAsync();
        return car;
    }

    public async Task<bool> UpdateAsync(int id, Car updated)
    {
        var car = await _db.Cars.FindAsync(id);
        if (car is null) return false;

        car.Brand = updated.Brand;
        car.Model = updated.Model;
        car.Trim = updated.Trim;
        car.Year = updated.Year;
        car.Mileage = updated.Mileage;
        car.Price = updated.Price;
        car.Status = updated.Status;

        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var car = await _db.Cars.FindAsync(id);
        if (car is null) return false;
        _db.Cars.Remove(car);
        await _db.SaveChangesAsync();
        return true;
    }
    public async Task<bool> UpdateDetailsAsync(int id, string brand, string model, string trim, int year,
                                           int mileage, decimal price, CarStatus status, string? photoUrl)
    {
        var car = await _db.Cars.Include(c => c.Images).FirstOrDefaultAsync(c => c.Id == id);
        if (car is null) return false;

        if (string.IsNullOrWhiteSpace(brand) || string.IsNullOrWhiteSpace(model))
            throw new ArgumentException("Вкажіть марку та модель");
        if (price <= 0)
            throw new ArgumentException("Ціна має бути більшою за нуль");
        if (!Enum.IsDefined(status))
            throw new ArgumentException("Невідомий статус авто");

        car.Brand = brand;
        car.Model = model;
        car.Trim = trim;
        car.Year = year;
        car.Mileage = mileage;
        car.Price = price;
        car.Status = status;

        if (!string.IsNullOrWhiteSpace(photoUrl))
        {
            var main = car.Images.FirstOrDefault(i => i.IsMain) ?? car.Images.FirstOrDefault();
            if (main != null)
                main.Url = photoUrl;
            else
                car.Images.Add(new CarImage { Url = photoUrl, IsMain = true });
        }

        await _db.SaveChangesAsync();
        return true;
    }
}