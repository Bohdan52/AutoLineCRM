using AutoLineCRM.CatalogService.Services;
using AutoLineCRM.SharedKernel.Entities;
using AutoLineCRM.SharedKernel.Enums;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using System.Runtime.ConstrainedExecution;

namespace AutoLineCRM.CatalogService.Controllers;

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
        var created = await _cars.CreateAsync(car);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
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
}