namespace AutoLineCRM.SharedKernel.Enums;

public interface INotificationSender
{
    Task SendEmailAsync(string to, string subject, string body, CancellationToken ct = default);

    Task SendSmsAsync(string phoneNumber, string message, CancellationToken ct = default);
}