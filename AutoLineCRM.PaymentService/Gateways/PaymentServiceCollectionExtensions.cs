using AutoLineCRM.PaymentService.Services;
using AutoLineCRM.SharedKernel.Enums;
using AutoLineCRM.SharedKernel.Interfaces;

namespace AutoLineCRM.PaymentService.Gateways;

public static class PaymentServiceCollectionExtensions
{
    public static IServiceCollection AddPaymentProcessing(this IServiceCollection services)
    {
        services.AddSingleton<IReadOnlyDictionary<PaymentMethod, IPaymentGateway>>(_ =>
            new Dictionary<PaymentMethod, IPaymentGateway>
            {
                [PaymentMethod.Credit] = new CardPaymentGateway(),
                [PaymentMethod.Cash] = new CashPaymentGateway(),
                [PaymentMethod.BankTransfer] = new BankTransferPaymentGateway()
            });
        services.AddScoped<PaymentProcessor>();
        return services;
    }
}