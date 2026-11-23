using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace MonoBank.Acquiring.Tests;

public class DependencyInjectionTests
{
    [Fact]
    public void AddMonoAcquiring_resolves_typed_client()
    {
        var services = new ServiceCollection();
        services.AddMonoAcquiring(o =>
        {
            o.Token = "test-token";
            o.Cms = "tests";
        });

        using var sp = services.BuildServiceProvider();
        var client = sp.GetRequiredService<MonoAcquiringClient>();
        Assert.NotNull(client);
    }

    [Fact]
    public void AddMonoAcquiring_binds_configuration_section()
    {
        var json = """{"MonoBank":{"Token":"from-config","Cms":"cfg"}}"""u8.ToArray();
        var config = new ConfigurationBuilder()
            .AddJsonStream(new MemoryStream(json))
            .Build();

        var services = new ServiceCollection();
        services.AddMonoAcquiring(config);

        using var sp = services.BuildServiceProvider();
        var client = sp.GetRequiredService<MonoAcquiringClient>();
        Assert.NotNull(client);
    }
}
