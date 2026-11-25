using System.Net;
using Microsoft.AspNetCore.HttpOverrides;

namespace CorrespondenceApi.Common;

/// <summary>
/// Робота за зворотним проксі (секція <c>ForwardedHeaders</c>): IP клієнта й схема (HTTPS) беруться
/// із заголовків X-Forwarded-For / X-Forwarded-Proto — лише від перелічених довірених проксі.
/// Від них залежать ліміт спроб входу за IP і прапорець Secure для cookie.
/// Якщо список порожній, заголовки ігноруються (інакше будь-хто підробив би свою IP-адресу).
/// </summary>
public static class ForwardedHeadersSetup
{
    public static bool AddTrustedForwardedHeaders(this IServiceCollection services, IConfiguration configuration)
    {
        var section = configuration.GetSection("ForwardedHeaders");
        var proxies = section.GetSection("KnownProxies").Get<string[]>() ?? [];
        var networks = section.GetSection("KnownNetworks").Get<string[]>() ?? [];
        if (proxies.Length == 0 && networks.Length == 0) return false;

        services.Configure<ForwardedHeadersOptions>(o =>
        {
            o.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
            o.KnownProxies.Clear();
            o.KnownIPNetworks.Clear();
            foreach (var proxy in proxies) o.KnownProxies.Add(IPAddress.Parse(proxy));
            foreach (var network in networks) o.KnownIPNetworks.Add(System.Net.IPNetwork.Parse(network));
        });
        return true;
    }
}
