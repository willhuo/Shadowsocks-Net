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
        RemoteServerConfig _RemoteServerConfig { get; set; }
        ILogger _Logger { get; set; }

        TcpServer _TcpServer { get; set; }
        UdpServer _UdpServer { get; set; }
        DnsCache _DnsCache { get; set; }
        ISocks5Handler _Socks5Handler { get; set; }
        CancellationTokenSource _CancellationStop { get; set; }


        public RemoteServer(RemoteServerConfig remoteServerConfig, ILogger logger = null)
        {
            this._RemoteServerConfig = remoteServerConfig;
            this._Logger = logger;

            var serverConfig = new ServerConfig()
            {
                BindPoint = _RemoteServerConfig.GetIPEndPoint(),
                MaxNumClient = Defaults.MaxNumClient
            };

            _TcpServer = new TcpServer(serverConfig, _Logger);
            _UdpServer = new UdpServer(serverConfig, _Logger);
            _DnsCache = new DnsCache(_Logger);
        }



        #region IShadowsocksServer

        [MethodImpl(MethodImplOptions.Synchronized)]
        public void Start()
        {
            _CancellationStop = new CancellationTokenSource();
            _Socks5Handler = new StandardRemoteSocks5Handler(_RemoteServerConfig, _DnsCache, _Logger);

            _TcpServer.Listen();
            _UdpServer.Listen();

            if (_TcpServer.IsRunning)
                Task.Run(() => ProcessTcp(_CancellationStop.Token));

            if (_TcpServer.IsRunning && _UdpServer.IsRunning)
                Task.Run(() => ProcessUdp(_CancellationStop.Token));
        }

        public void Stop()
        {
            if (null != _CancellationStop)
            {
                _CancellationStop.Cancel();
                _CancellationStop = null;
            }

            _TcpServer.StopListen();
            _UdpServer.StopListen();

            if (null != _Socks5Handler)
            {
                _Socks5Handler.Dispose();
                _Socks5Handler = null;
            }
        }
        #endregion

        async ValueTask ProcessTcp(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested && _TcpServer.IsRunning)
            {
                var client = await _TcpServer.Accept();
                if (null != client)
                {
                    if (cancellationToken.IsCancellationRequested) { client.Close(); return; }
                    if (null != _Socks5Handler)                    
                        await _Socks5Handler.HandleTcp(client, this._CancellationStop.Token);                    
                }
                else
                {
                    _Logger.LogWarning("tcpclient is null");
                    break;
                }
            }
        }

        async ValueTask ProcessUdp(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested && _TcpServer.IsRunning && _UdpServer.IsRunning)
            {
                var client = await _UdpServer.Accept();
                if (null != client)
                {
                    if (cancellationToken.IsCancellationRequested) { client.Close(); return; }
                    if (null != _Socks5Handler)
                        await _Socks5Handler.HandleUdp(client, this._CancellationStop.Token);
                }
                else 
                {
                    _Logger.LogWarning("udp client is null");
                }
            }
        }
    }
}
