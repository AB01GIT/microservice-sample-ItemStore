using Catalog.Service.Dtos;
using Catalog.Service.Entities;

namespace Catalog.Service;

public static class Extensions
{
    public static ItemDto AsDto(this Item item)
    {
        return new ItemDto(item.Id, item.Name, item.Description, item.Price, item.CreatedDate);
    }
}