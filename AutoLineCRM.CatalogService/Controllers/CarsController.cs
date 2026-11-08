using AutoLineCRM.CatalogService.Services;
using AutoLineCRM.SharedKernel.Entities;
using AutoLineCRM.SharedKernel.Enums;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using System.Runtime.ConstrainedExecution;

namespace AutoLineCRM.CatalogService.Controllers;

public record CreateCarDto(string Vin, string Brand, string Model, string Trim,
                           int Year, int Mileage, decimal Price, string? PhotoUrl);
public record UpdateCarDto(string Brand, string Model, string? Trim, int Year, int Mileage,
                           decimal Price, CarStatus Status, string? PhotoUrl);

[ApiController]
[Route("api/cars")]
public class CarsController : ControllerBase
{
    private readonly CarService _cars;
    public CarsController(CarService cars) => _cars = cars;

    [HttpGet]
    public async Task<IActionResult> Search(
        [FromQuery] string? brand,
        [FromQuery] string? model,
        [FromQuery] decimal? minPrice,
        [FromQuery] decimal? maxPrice,
        [FromQuery] CarStatus? status,
        [FromQuery] int page = 1,
        [FromQuery] int size = 20)
    {
        var result = await _cars.SearchAsync(brand, model, minPrice, maxPrice, status, page, size);
        return Ok(result);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var car = await _cars.GetByIdAsync(id);
        return car is null ? NotFound() : Ok(car);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] Car car)
    {
       try
       {
            var created = await _cars.CreateAsync(car);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
       }
       catch (InvalidOperationException ex) { return Conflict(new { error = ex.Message }); }
       catch (ArgumentException ex) { return BadRequest(new { error = ex.Message }); }
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] Car car)
    {
        var ok = await _cars.UpdateAsync(id, car);
        return ok ? NoContent() : NotFound();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var ok = await _cars.DeleteAsync(id);
        return ok ? NoContent() : NotFound();
    }
    [HttpPost("new")]
    public async Task<IActionResult> CreateFromDto([FromBody] CreateCarDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Brand) || string.IsNullOrWhiteSpace(dto.Model))
            return BadRequest(new { error = "Вкажіть марку та модель" });
        if (dto.Price <= 0)
            return BadRequest(new { error = "Ціна має бути більшою за нуль" });

        var car = new Car
        {
            VIN = dto.Vin,
            Brand = dto.Brand,
            Model = dto.Model,
            Trim = dto.Trim ?? "",
            Year = dto.Year,
            Mileage = dto.Mileage,
            Price = dto.Price,
            Status = CarStatus.Available
        };
        if (!string.IsNullOrWhiteSpace(dto.PhotoUrl))
            car.Images.Add(new CarImage { Url = dto.PhotoUrl, IsMain = true });

        try
        {
            var created = await _cars.CreateAsync(car);
            return Ok(new { created.Id });
        }
        catch (InvalidOperationException ex) { return Conflict(new { error = ex.Message }); }
        catch (ArgumentException ex) { return BadRequest(new { error = ex.Message }); }
    }
    [HttpPut("{id:int}/details")]
    public async Task<IActionResult> UpdateDetails(int id, [FromBody] UpdateCarDto dto)
    {
        try
        {
            var ok = await _cars.UpdateDetailsAsync(id, dto.Brand, dto.Model, dto.Trim ?? "", dto.Year,
                                                    dto.Mileage, dto.Price, dto.Status, dto.PhotoUrl);
            return ok ? NoContent() : NotFound();
        }
        catch (ArgumentException ex) { return BadRequest(new { error = ex.Message }); }
    }
}