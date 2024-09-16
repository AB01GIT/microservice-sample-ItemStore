using System.Security.Claims;
using MassTransit;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Trading.Service.Contracts;
using Trading.Service.Dtos;
using Trading.Service.StateMachines;

namespace Trading.Service.Controllers;

[ApiController]
[Route("purchase")]
[Authorize]
public class PurchaseController : ControllerBase
{
    private readonly IPublishEndpoint publishEndpoint;
    private readonly IRequestClient<GetPruchaseState> purchaseClient;

    public PurchaseController(IPublishEndpoint publishEndpoint, IRequestClient<GetPruchaseState> purchaseClient)
    {
        this.publishEndpoint = publishEndpoint;
        this.purchaseClient = purchaseClient;
    }

    [HttpGet("status/{idempotencyId}")]
    public async Task<ActionResult<PurchaseDto>> GetStatusAsync(Guid idempotencyId)
    {
        var response = await purchaseClient.GetResponse<PurchaseState>(new GetPruchaseState(idempotencyId));

        var PurchaseState = response.Message;

        var purchase = new PurchaseDto(
            PurchaseState.UserId,
            PurchaseState.ItemId,
            PurchaseState.PurchaseTotal,
            PurchaseState.Quantity,
            PurchaseState.CurrentState,
            PurchaseState.ErrorMessage,
            PurchaseState.Received,
            PurchaseState.LastUpdated
        );

        return Ok(purchase);
    }

    [HttpPost]
    public async Task<IActionResult> PostAsync(SubmitPurchaseDto purchase)
    {
        var userId = User.FindFirstValue("sub");

        var message = new PurchaseRequested(
            Guid.Parse(userId),
            purchase.ItemId.Value,
            purchase.Quantity,
            purchase.IdempotencyId.Value
        );

        await publishEndpoint.Publish(message);

        return AcceptedAtAction(nameof(GetStatusAsync), new { purchase.IdempotencyId }, new { purchase.IdempotencyId });
    }


}