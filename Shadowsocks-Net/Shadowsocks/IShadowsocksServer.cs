using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Shadowsocks
{
    public interface IShadowsocksServer
    {
        void Start();
        void Stop();
    }
}
