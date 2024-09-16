namespace Trading.Service.Contracts;

public record PurchaseRequested(Guid UserId, Guid ItemId, int Quantity, Guid CorrelationId);

public record GetPruchaseState(Guid CorrelationId);