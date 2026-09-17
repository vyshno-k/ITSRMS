namespace ITServiceRequest.Api.Services;

public class TicketNumberService
{
    public string Generate() =>
        $"SR-{DateTime.UtcNow:yyyyMMddHHmmssfff}-{Guid.NewGuid():N}"[..25].ToUpperInvariant();
}
