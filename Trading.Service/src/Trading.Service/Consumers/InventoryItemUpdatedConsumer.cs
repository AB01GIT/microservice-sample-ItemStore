using Common;
using Inventory.Service;
using MassTransit;
using Trading.Service.Entities;

namespace Trading.Service.Consumer;

public class InventoryItemUpdatedConsumer : IConsumer<InventoryItemUpdated>
{
    private readonly IRepository<InventoryItem> repository;

    public InventoryItemUpdatedConsumer(IRepository<InventoryItem> repository)
    {
        this.repository = repository;
    }

    public async Task Consume(ConsumeContext<InventoryItemUpdated> context)
    {
        var message = context.Message;

        var inventoryItem = await repository.GetAsync(item => item.UserId == message.UserId && item.CatalogItemId == message.CtalogItemId);

        if (inventoryItem == null)
        {
            inventoryItem = new InventoryItem
            {
                CatalogItemId = message.CtalogItemId,
                UserId = message.UserId,
                Quantity = message.NewTotalQuantity
            };

            await repository.CreateAsync(inventoryItem);
        }
        else
        {
            inventoryItem.Quantity = message.NewTotalQuantity;
            await repository.UpdateAsync(inventoryItem);
        }
    }
}