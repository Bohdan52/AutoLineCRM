using AutoLineCRM.SalesService.Dtos;
using AutoLineCRM.SalesService.Services; // <-- підключаємо простір імен
using Microsoft.AspNetCore.Mvc;

namespace AutoLineCRM.SalesService.Controllers;

[ApiController]
[Route("api/requests")]
public class RequestsController : ControllerBase
{
    private readonly RequestService _requestService;

    public RequestsController(RequestService requestService) => _requestService = requestService;

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateRequestDto dto)
    {
        var result = await _requestService.CreateRequestAsync(dto);
        return Ok(result);
    }

    [HttpGet("client/{clientId:int}")]
    public async Task<IActionResult> GetByClient(int clientId)
    {
        var result = await _requestService.GetClientRequestsAsync(clientId);
        return Ok(result);
    }
}