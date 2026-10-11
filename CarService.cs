using AutoLineCRM.CatalogService.Data;
using AutoLineCRM.SharedKernel.Entities;
using AutoLineCRM.SharedKernel.Enums;
using Microsoft.EntityFrameworkCore;

namespace AutoLineCRM.CatalogService.Services;

public class CarService
{
    private readonly CatalogDbContext _db;

    public CarService(CatalogDbContext db)
    {
        _db = db;
    }
    public async Task<List<Car>> SearchAsync(string? brand , string? model, decimal? minPrice, decimal? maxPrice, CarStatus? status, int page = 1, int size = 20)
    {
        var q =_db.Cars.AsNotracking().AsQueryable();

        if(string.IsNullOrWhiteSpace(brand))
            q = q.Where(c => EF.Functions.Like(c.Brand, $"%{brand}%"));
        if(string.IsNullOrWhiteSpace(model))
            q = q.Where(c => EF.Functions.Like(c.Model, $"%{model}%"));
        if(minPrice.HasValue)
            q = q.Where(c => c.Price >= minPrice.Value);
        if(maxPrice.HasValue)
            q = q.Where(c => c.Price <= maxPrice.Value);
        if(status.HasValue)
            q = q.Where(c => c.Status == status.Value);
        return await q.OrderBy(c => c.Brand).ThenBy(c => c.Model)
                     .Skip((page - 1) * size).Take(size)
                     .ToListAsync();
    }

    public async Task<Car?> GetByIdAsync(Guid id) => 
        _db.Cars.Include(c => c.Images).FirstOrDefaultAsync(c => c.Id == id);)
    public async Task<bool>UpdateAsync(int id, Car updated)
    {
        var car = await _db.Cars.FindAsync(id);
        if (car = is null) return false;

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
}