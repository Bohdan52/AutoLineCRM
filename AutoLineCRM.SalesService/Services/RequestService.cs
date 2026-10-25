using AutoLineCRM.SalesService.Data;
using AutoLineCRM.SalesService.Dtos;
using AutoLineCRM.SharedKernel.Entities;
using AutoLineCRM.SharedKernel.Enums;
using Microsoft.EntityFrameworkCore;

namespace AutoLineCRM.SalesService.Services;

public class RequestService
{
    private readonly SalesDbContext _db;
    public RequestService(SalesDbContext db) => _db = db;

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
        return request;
    }

    public async Task<List<Request>> GetClientRequestsAsync(int clientId)
    {
        return await _db.Requests
            .Where(r => r.ClientId == clientId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();
    }
}