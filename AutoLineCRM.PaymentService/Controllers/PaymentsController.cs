using AutoLineCRM.PaymentService.Services;
using AutoLineCRM.SharedKernel.Entities;
using AutoLineCRM.SharedKernel.Enums;
using Microsoft.AspNetCore.Mvc;

namespace AutoLineCRM.PaymentService.Controllers;

public record PayRequestDto(int RequestId, decimal Amount, PaymentMethod Method, string Currency, string? CardToken);

[ApiController]
[Route("api/payments")]
public class PaymentsController : ControllerBase
{
    private readonly PaymentProcessor _processor;

    public PaymentsController(PaymentProcessor processor) => _processor = processor;

    [HttpPost]
    public async Task<IActionResult> Pay([FromBody] PayRequestDto dto)
    {
        if (!_processor.Supports(dto.Method))
            return BadRequest(new { error = $"Спосіб оплати {dto.Method} не підтримується" });

        var payment = new Payment
        {
            RequestId = dto.RequestId,
            Amount = dto.Amount,
            Method = dto.Method,
            Status = PaymentStatus.Pending
        };

        var result = await _processor.ProcessAsync(payment, dto.Currency, dto.CardToken ?? "");

        if (!result.Success)
            return BadRequest(new { error = result.ErrorMessage });

        return Ok(new { transactionId = result.TransactionId, provider = payment.ProviderName });
    }
}