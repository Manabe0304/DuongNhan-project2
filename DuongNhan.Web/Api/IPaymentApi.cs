using DuongNhan.Shared.Contracts;
using DuongNhan.Shared.Dtos.Payments;
using Refit;

namespace DuongNhan.Web.Api;

/// <summary>
/// Bank-transfer payments. The bearer token is passed explicitly (see ISubscriptionApi): the HttpClientFactory
/// handler runs outside the Blazor circuit and cannot read it from browser storage.
/// </summary>
public interface IPaymentApi
{
    [Post(ApiRoutes.Payments.Orders)]
    Task<PaymentOrderDto> CreateOrderAsync(
        [Body] CreatePaymentOrderRequest body,
        [Header("Authorization")] string authorization,
        CancellationToken ct = default);

    // Refit does not understand ASP.NET route constraints such as "{id:guid}", so these use the constraint-free form.
    [Get("/api/payments/orders/{id}")]
    Task<PaymentOrderDto> GetOrderAsync(
        Guid id,
        [Header("Authorization")] string authorization,
        CancellationToken ct = default);

    [Post("/api/payments/orders/{id}/cancel")]
    Task<PaymentOrderDto> CancelOrderAsync(
        Guid id,
        [Header("Authorization")] string authorization,
        CancellationToken ct = default);
}
