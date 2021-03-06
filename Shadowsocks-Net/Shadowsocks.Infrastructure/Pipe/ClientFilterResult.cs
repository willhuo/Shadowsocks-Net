﻿/*
 * Shadowsocks-Net https://github.com/shadowsocks/Shadowsocks-Net
 */

using System;
using System.Collections.Generic;
using System.Text;

namespace Shadowsocks.Infrastructure.Pipe
{
    using Sockets;
    public readonly struct ClientFilterResult
    {
        public readonly IClient Client;
        public readonly SmartBuffer Buffer;
        public readonly bool Continue;

        public ClientFilterResult(IClient client, SmartBuffer buffer = null, bool @continue = true)
        {
            Client = client;
            Buffer = buffer;
            Continue = @continue;
        }
    }
}
