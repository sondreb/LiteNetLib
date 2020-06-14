using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace HubbyGateway
{
    public class WaitPeer
    {
        public IPEndPoint InternalAddr { get; }
        public IPEndPoint ExternalAddr { get; }
        public DateTime RefreshTime { get; private set; }

        public void Refresh()
        {
            RefreshTime = DateTime.UtcNow;
        }

        public WaitPeer(IPEndPoint internalAddr, IPEndPoint externalAddr)
        {
            Refresh();
            InternalAddr = internalAddr;
            ExternalAddr = externalAddr;
        }
    }
}
