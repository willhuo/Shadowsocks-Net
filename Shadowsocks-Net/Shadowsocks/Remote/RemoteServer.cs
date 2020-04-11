using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Argument.Check;
using Shadowsocks.Infrastructure;
using Shadowsocks.Infrastructure.Sockets;
using Shadowsocks.Infrastructure.Pipe;
using System.Runtime.CompilerServices;

namespace Shadowsocks.Remote
{
    /// <summary>
    /// This one runs on server.
    /// </summary>
    public sealed class RemoteServer : IShadowsocksServer
    {
        RemoteServerConfig _remoteServerConfig { get; set; }
        ILogger _logger { get; set; }

        TcpServer _tcpServer { get; set; }
        UdpServer _udpServer { get; set; }
        DnsCache _dnsCache { get; set; }
        ISocks5Handler _socks5Handler { get; set; }
        CancellationTokenSource _cancellationStop { get; set; }


        public RemoteServer(RemoteServerConfig remoteServerConfig, ILogger logger)
        {
            this._remoteServerConfig = remoteServerConfig;
            this._logger = logger;

            var serverConfig = new ServerConfig()
            {
                BindPoint = _remoteServerConfig.GetIPEndPoint(),
                MaxNumClient = Defaults.MaxNumClient
            };

            _tcpServer = new TcpServer(serverConfig, _logger);
            _udpServer = new UdpServer(serverConfig, _logger);
            _dnsCache = new DnsCache(_logger);
        }



        #region IShadowsocksServer

        [MethodImpl(MethodImplOptions.Synchronized)]
        public void Start()
        {
            _cancellationStop = new CancellationTokenSource();
            _socks5Handler = new StandardRemoteSocks5Handler(_remoteServerConfig, _dnsCache, _logger);

            _tcpServer.Listen();
            _udpServer.Listen();

            if (_tcpServer.IsRunning)
                Task.Run(() => ProcessTcp(_cancellationStop.Token));

            if (_tcpServer.IsRunning && _udpServer.IsRunning)
                Task.Run(() => ProcessUdp(_cancellationStop.Token));
        }

        public void Stop()
        {
            if (null != _cancellationStop)
            {
                _cancellationStop.Cancel();
                _cancellationStop = null;
            }

            _tcpServer.StopListen();
            _udpServer.StopListen();

            if (null != _socks5Handler)
            {
                _socks5Handler.Dispose();
                _socks5Handler = null;
            }
        }
        #endregion

        async ValueTask ProcessTcp(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested && _tcpServer.IsRunning)
            {
                var client = await _tcpServer.Accept();
                if (null != client)
                {
                    if (cancellationToken.IsCancellationRequested) { client.Close(); return; }
                    if (null != _socks5Handler)                    
                        await _socks5Handler.HandleTcp(client, this._cancellationStop.Token);                    
                }
                else
                {
                    _logger.LogWarning("tcpclient is null");
                    break;
                }
            }
        }

        async ValueTask ProcessUdp(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested && _tcpServer.IsRunning && _udpServer.IsRunning)
            {
                var client = await _udpServer.Accept();
                if (null != client)
                {
                    if (cancellationToken.IsCancellationRequested) { client.Close(); return; }
                    if (null != _socks5Handler)
                        await _socks5Handler.HandleUdp(client, this._cancellationStop.Token);
                }
                else 
                {
                    _logger.LogWarning("udp client is null");
                }
            }
        }
    }
}
