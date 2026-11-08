namespace AutoLineCRM.SalesService.Dtos;

using AutoLineCRM.SharedKernel.Enums;

public record CreateRequestDto(int CarId, int ClientId);
public record UpdateStatusDto(int Status);
public record RecordPaymentDto(PaymentMethod Method, decimal Amount, string? TransactionId, string? ProviderName);
public record PaymentInfoDto(int Id, int RequestId, int ClientId, decimal Amount, PaymentMethod Method,
                             PaymentStatus Status, string? TransactionId, string? ProviderName, DateTime DateTime,
                             RequestStatus RequestStatus);
