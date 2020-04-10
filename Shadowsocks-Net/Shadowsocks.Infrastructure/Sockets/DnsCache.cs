using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System.Buffers;
using Argument.Check;

namespace Shadowsocks.Infrastructure.Sockets
{
    /// <summary>
    /// DNS resolution & caching service.
    /// </summary>
    public class DnsCache
    {

        public static readonly DnsCache Shared = null;


        LruCache<IPAddress[]> _Cache { get; set; }
        ILogger _Logger { set; get; }


        readonly TimeSpan _expiretime = TimeSpan.FromMinutes(30);

        static DnsCache()
        {
            if (null == Shared) { Shared = new DnsCache(null); }
        }
        public DnsCache(ILogger logger)
        {
            this._Cache = new LruCache<IPAddress[]>(TimeSpan.FromMinutes(10));
            this._Logger = logger;
        }

        public async Task<IPAddress[]> ResolveHost(string host)
        {

            var cache = _Cache.Get(host);
            if (null != cache) { return await Task.FromResult(cache); }
            try
            {
                _Logger?.LogInformation($"DnsCache resolving [{host}]...");
                var entry = await Dns.GetHostEntryAsync(host);
                if (null != entry.AddressList && entry.AddressList.Length > 0)
                {
                    _Logger?.LogInformation($"DnsCache resolved [{host}] = [{entry.AddressList[0].ToString()}]");
                    _Cache.Set(host, entry.AddressList, _expiretime);
                    return entry.AddressList;
                }
            }
            catch (SocketException se)
            {
                _Logger?.LogWarning($"DnsCache resolve hostname failed:[{host}]. {se.SocketErrorCode}, {se.Message}.");
            }
            catch (Exception ex)
            {
                _Logger?.LogWarning($"DnsCache resolve hostname failed:[{host}]. {ex.Message}.");
            }

            return null;
        }
    }
}
