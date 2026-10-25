using AutoLineCRM.SalesService.Dtos;
using AutoLineCRM.SalesService.Services; 
using Microsoft.AspNetCore.Mvc;
using AutoLineCRM.SharedKernel.Enums;

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
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var result = await _requestService.GetAllRequestsAsync();
        return Ok(result);
    }

    [HttpPut("{id:int}/status")]
    public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateStatusDto dto)
    {
        var success = await _requestService.UpdateRequestStatusAsync(id, (RequestStatus)dto.Status);
        return success ? Ok() : NotFound();
    }
}