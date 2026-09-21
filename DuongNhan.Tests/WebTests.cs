using Microsoft.Extensions.Logging;

namespace DuongNhan.Tests;

[TestClass]
public class WebTests
{
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromMinutes(2);

    [TestMethod]
    public async Task GetWebResourceRootReturnsOkStatusCode()
    {
        var cancellationToken = new CancellationTokenSource(DefaultTimeout).Token;

        var appHost = await DistributedApplicationTestingBuilder
            .CreateAsync<Projects.DuongNhan_AppHost>(
                args: ["--UseEphemeralContainers=true"],
                cancellationToken: cancellationToken);

        appHost.Services.AddLogging(logging =>
        {
            logging.SetMinimumLevel(LogLevel.Debug);
            logging.AddFilter(appHost.Environment.ApplicationName, LogLevel.Debug);
            logging.AddFilter("Aspire.", LogLevel.Debug);
        });

        appHost.Services.ConfigureHttpClientDefaults(clientBuilder =>
        {
            clientBuilder.AddStandardResilienceHandler();
        });

        await using var app = await appHost.BuildAsync(cancellationToken)
            .WaitAsync(DefaultTimeout, cancellationToken);
        await app.StartAsync(cancellationToken)
            .WaitAsync(DefaultTimeout, cancellationToken);

        var httpClient = app.CreateHttpClient("webfrontend");

        await app.ResourceNotifications
            .WaitForResourceHealthyAsync("webfrontend", cancellationToken)
            .WaitAsync(DefaultTimeout, cancellationToken);

        var response = await httpClient.GetAsync("/", cancellationToken);

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
    }
}