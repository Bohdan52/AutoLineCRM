using AutoLineCRM.SalesService.Data;
using AutoLineCRM.SalesService.Dtos;
using AutoLineCRM.SharedKernel.Entities;
using AutoLineCRM.SharedKernel.Enums;
using Microsoft.EntityFrameworkCore;
using AutoLineCRM.SharedKernel.Events;

namespace AutoLineCRM.SalesService.Services;

public class RequestService
{
    private readonly SalesDbContext _db;
    private readonly IEventBus? _eventBus;

    public RequestService(SalesDbContext db, IEventBus? eventBus = null)
    {
        _db = db;
        _eventBus = eventBus;
    }
    public async Task<Request> CreateRequestAsync(CreateRequestDto dto)
    {

        var request = new Request
        {
            CarId = dto.CarId,
            ClientId = dto.ClientId,
            CreatedAt = DateTime.UtcNow,
            Status = RequestStatus.New,
            Notes = "Заявка створена з веб-сайту"
        };

        _db.Requests.Add(request);
        await _db.SaveChangesAsync();
        if (_eventBus is not null)
            await _eventBus.PublishAsync(new RequestCreatedEvent(request.Id, request.ClientId, request.CarId));
        return request;
    }

    public async Task<List<Request>> GetClientRequestsAsync(int clientId)
    {
        return await _db.Requests
            .Where(r => r.ClientId == clientId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();
    }
    public async Task<List<Request>> GetAllRequestsAsync()
    {
        return await _db.Requests
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();
    }

    public async Task<bool> UpdateRequestStatusAsync(int id, RequestStatus status)
    {
        if (!Enum.IsDefined(status))
            throw new ArgumentException("Невідомий статус заявки");

        var request = await _db.Requests.FindAsync(id);
        if (request == null) return false;

        request.Status = status;
        request.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return true;
    }
    public async Task<Payment> RecordPaymentAsync(int requestId, RecordPaymentDto dto)
    {
        var request = await _db.Requests.FindAsync(requestId)
            ?? throw new KeyNotFoundException("Заявку не знайдено");

        if (request.Status != RequestStatus.Confirmed)
            throw new InvalidOperationException("Оплатити можна лише підтверджену заявку");
        if (dto.Amount <= 0)
            throw new ArgumentException("Сума має бути більшою за нуль");
        if (!Enum.IsDefined(dto.Method))
            throw new ArgumentException("Невідомий спосіб оплати");

        var payments = _db.Set<Payment>();
        if (await payments.AnyAsync(p => p.RequestId == requestId &&
                (p.Status == PaymentStatus.Pending || p.Status == PaymentStatus.Completed)))
            throw new InvalidOperationException("Для цієї заявки вже є платіж");

        // Оплата на місці: гроші ще не отримано, платіж чекає підтвердження менеджера
        var onSite = dto.Method == PaymentMethod.Cash;

        var payment = new Payment
        {
            RequestId = requestId,
            Amount = dto.Amount,
            Method = dto.Method,
            Status = onSite ? PaymentStatus.Pending : PaymentStatus.Completed,
            TransactionId = dto.TransactionId,
            ProviderName = dto.ProviderName ?? (onSite ? "Оплата на місці" : null)
        };
        payments.Add(payment);

        if (!onSite)
        {
            request.Status = RequestStatus.Paid;
            request.UpdatedAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync();
        return payment;
    }

    public async Task<bool> ApprovePaymentAsync(int paymentId, int? managerId = null)
    {
        var payment = await _db.Set<Payment>().FindAsync(paymentId);
        if (payment is null) return false;

        var request = await _db.Requests.FindAsync(payment.RequestId)
            ?? throw new InvalidOperationException("Заявку цього платежу не знайдено");

        // Готівка: чекаємо підтвердження отримання грошей.
        // Картка/переказ: гроші списано, чекаємо затвердження менеджера.
        var waitsCash = payment.Method == PaymentMethod.Cash && payment.Status == PaymentStatus.Pending;
        var waitsApproval = payment.Method != PaymentMethod.Cash
                            && payment.Status == PaymentStatus.Completed
                            && request.Status == RequestStatus.Confirmed;

        if (!waitsCash && !waitsApproval)
            throw new InvalidOperationException("Цей платіж не очікує підтвердження менеджера");

        payment.Status = PaymentStatus.Completed;
        request.Status = RequestStatus.Paid;
        request.ManagerID = managerId ?? request.ManagerID;
        request.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<List<PaymentInfoDto>> GetPaymentsAsync(int? clientId = null)
    {
        return await _db.Set<Payment>().AsNoTracking()
            .Where(p => clientId == null || p.Request.ClientId == clientId)
            .OrderByDescending(p => p.DateTime)
            .Select(p => new PaymentInfoDto(p.Id, p.RequestId, p.Request.ClientId, p.Amount, p.Method,
                                            p.Status, p.TransactionId, p.ProviderName, p.DateTime, p.Request.Status))
            .ToListAsync();
    }
}