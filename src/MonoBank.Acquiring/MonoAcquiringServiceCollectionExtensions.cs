using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace MonoBank.Acquiring;

public static class MonoAcquiringServiceCollectionExtensions
{
    public static IHttpClientBuilder AddMonoAcquiring(
        this IServiceCollection services,
        Action<MonoAcquiringOptions> configure)
    {
        if (services is null)
            throw new ArgumentNullException(nameof(services));
        if (configure is null)
            throw new ArgumentNullException(nameof(configure));

        services.AddOptions<MonoAcquiringOptions>().Configure(configure);
        return AddClient(services);
    }

    public static IHttpClientBuilder AddMonoAcquiring(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        if (services is null)
            throw new ArgumentNullException(nameof(services));
        if (configuration is null)
            throw new ArgumentNullException(nameof(configuration));

        services.AddOptions<MonoAcquiringOptions>()
            .Bind(configuration.GetSection(MonoAcquiringOptions.SectionName))
            .Validate(o => !string.IsNullOrWhiteSpace(o.Token), "MonoBank:Token is required.");
        return AddClient(services);
    }

    private static IHttpClientBuilder AddClient(IServiceCollection services) =>
        services.AddHttpClient<MonoAcquiringClient>((sp, http) =>
            {
                var options = sp.GetRequiredService<IOptions<MonoAcquiringOptions>>().Value;
                http.BaseAddress = options.BaseAddress;
            })
            .AddTypedClient((http, sp) =>
                new MonoAcquiringClient(http, sp.GetRequiredService<IOptions<MonoAcquiringOptions>>().Value));
}
