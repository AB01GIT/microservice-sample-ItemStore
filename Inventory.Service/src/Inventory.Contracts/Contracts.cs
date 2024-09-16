using System;

namespace Inventory.Service;

public record GrantItems(Guid UserId, Guid CatalogItemId, int Quantity, Guid CorrelationId);

public record InventoryItemsGranted(Guid CorrelationId);

public record SubtractItems(Guid UserId, Guid CatalogItemId, int Quantity, Guid CorrelationId);

public record InventoryItemsSubtracted(Guid CorrelationId);

public record InventoryItemUpdated(Guid UserId, Guid CtalogItemId, int NewTotalQuantity);

