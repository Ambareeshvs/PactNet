using CustomerApi;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.DependencyInjection;
using PactNet.Verifier;

namespace CustomerProvider.Tests;

public sealed class CustomerProviderContractTests
{
    [Fact]
    public async Task CustomerApi_HonorsCustomerConsumerPact()
    {
        await using var app = ApiApplication.Create([]);
        app.Urls.Add("http://127.0.0.1:0");
        await app.StartAsync();

        var server = app.Services.GetRequiredService<IServer>();
        var address = server.Features.Get<IServerAddressesFeature>()!.Addresses.Single();
        var pactFolder = new DirectoryInfo(Path.Combine(GetPactDirectory()));
        Assert.True(pactFolder.Exists, $"Pact folder not found: {pactFolder.FullName}");

        using var verifier = new PactVerifier("CustomerApi");
        verifier
            .WithHttpEndpoint(new Uri(address))
            .WithDirectorySource(pactFolder)
            .WithProviderStateUrl(new Uri(new Uri(address), "/provider-states"))
            .Verify();

        await app.StopAsync();
    }

    private static string GetPactDirectory()
    {
        return Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../pacts"));
    }
}
