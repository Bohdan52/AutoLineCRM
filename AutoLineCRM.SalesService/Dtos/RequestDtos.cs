namespace AutoLineCRM.SalesService.Dtos;

public record CreateRequestDto(int CarId, int ClientId);
public record UpdateStatusDto(int Status);