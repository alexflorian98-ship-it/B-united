using System.Net;
using System.Net.Http.Headers;
using BUnited.Modules.Billing.Domain;
using BUnited.Modules.Billing.Domain.Entities;
using BUnited.Modules.Billing.Tests.TestSupport;
using BUnited.Modules.Identity.Contracts;

namespace BUnited.Modules.Billing.Tests.Security;

/// <summary>
/// P3.30 (docs/TASKS.md): "Cross-user billing data access denial tests." The application-layer
/// <c>GetMyInvoiceHandlerTests.Throws_not_found_when_the_invoice_belongs_to_another_user</c> already
/// proves the handler's own query filters by the caller's <c>UserId</c>; this closes the remaining
/// gap flagged in TASKS.md — no test previously drove the real <see cref="Api.Controllers.BillingController"/>
/// over real HTTP behind the actual JWT + authorization pipeline, so a wiring mistake in the
/// controller (e.g. reading the wrong claim, or a route binding bug) would not have been caught.
/// Mirrors the same HTTP-level pattern used for P4.33.a/P6.20.a
/// (<c>Questionnaires.Tests.TestSupport.QuestionnairesApiTestHost</c>).
/// </summary>
public sealed class BillingCrossUserAccessTests
{
    [Fact]
    public async Task GetMyInvoice_returns_404_over_real_http_when_the_invoice_belongs_to_another_user()
    {
        await using var host = await BillingApiTestHost.StartAsync();

        var ownerUserId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();

        var offer = ProgramOffer.Create(Guid.NewGuid());
        var price = ProgramPrice.Create(offer.Id, 99m, "RON", DateTime.UtcNow);
        var purchase = Purchase.Create(ownerUserId, offer.ProgramId, offer.Id, price.Id, 99m, "RON");
        var invoice = Invoice.Create(purchase.Id, null, 99m, "RON", InvoiceStatus.Paid, DateTime.UtcNow);
        host.DbContext.AddRange(offer, price, purchase, invoice);
        await host.DbContext.SaveChangesAsync();

        var otherUserToken = host.IssueToken(otherUserId, [WellKnownPermissionKeys.BillingView]);
        host.Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", otherUserToken);

        var response = await host.Client.GetAsync($"/api/v1/billing/my-invoices/{invoice.Id}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetMyInvoice_returns_200_over_real_http_for_the_owning_user()
    {
        await using var host = await BillingApiTestHost.StartAsync();

        var ownerUserId = Guid.NewGuid();

        var offer = ProgramOffer.Create(Guid.NewGuid());
        var price = ProgramPrice.Create(offer.Id, 99m, "RON", DateTime.UtcNow);
        var purchase = Purchase.Create(ownerUserId, offer.ProgramId, offer.Id, price.Id, 99m, "RON");
        var invoice = Invoice.Create(purchase.Id, null, 99m, "RON", InvoiceStatus.Paid, DateTime.UtcNow);
        host.DbContext.AddRange(offer, price, purchase, invoice);
        await host.DbContext.SaveChangesAsync();

        var ownerToken = host.IssueToken(ownerUserId, [WellKnownPermissionKeys.BillingView]);
        host.Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", ownerToken);

        var response = await host.Client.GetAsync($"/api/v1/billing/my-invoices/{invoice.Id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetMyInvoice_returns_401_over_real_http_when_unauthenticated()
    {
        await using var host = await BillingApiTestHost.StartAsync();

        var response = await host.Client.GetAsync($"/api/v1/billing/my-invoices/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
