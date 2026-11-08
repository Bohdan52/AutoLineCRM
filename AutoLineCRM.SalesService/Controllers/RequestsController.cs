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
        try
        {
            var success = await _requestService.UpdateRequestStatusAsync(id, (RequestStatus)dto.Status);
            return success ? Ok() : NotFound();
        }
        catch (ArgumentException ex) { return BadRequest(new { error = ex.Message }); }
    }
    [HttpPost("{id:int}/payments")]
    public async Task<IActionResult> RecordPayment(int id, [FromBody] RecordPaymentDto dto)
    {
        try
        {
            var payment = await _requestService.RecordPaymentAsync(id, dto);
            return Ok(new { payment.Id, Status = (int)payment.Status });
        }
        catch (KeyNotFoundException ex) { return NotFound(new { error = ex.Message }); }
        catch (InvalidOperationException ex) { return Conflict(new { error = ex.Message }); }
        catch (ArgumentException ex) { return BadRequest(new { error = ex.Message }); }
    }

    [HttpGet("payments")]
    public async Task<IActionResult> GetPayments([FromQuery] int? clientId)
        => Ok(await _requestService.GetPaymentsAsync(clientId));
    
    [HttpPost("payments/{paymentId:int}/confirm")]
    public async Task<IActionResult> ConfirmPayment(int paymentId, [FromQuery] int? managerId)
    {
        try
        {
            return await _requestService.ApprovePaymentAsync(paymentId, managerId) ? Ok() : NotFound();
        }
        catch (InvalidOperationException ex) { return Conflict(new { error = ex.Message }); }
    }
}