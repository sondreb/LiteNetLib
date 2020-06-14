using LiteNetLib;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading;

namespace HubbyClient
{
    public class HubbyClientManager
    {
        private const string ConnectionKey = "12345";
        private NetManager client;

        public void Run(string gateway, int gatewayPort)
        {
            EventBasedNetListener clientListener = new EventBasedNetListener();
            EventBasedNatPunchListener natPunchListener = new EventBasedNatPunchListener();

            clientListener.PeerConnectedEvent += peer =>
            {
                Console.WriteLine("PeerConnected: " + peer.EndPoint);
            };

            clientListener.ConnectionRequestEvent += request =>
            {
                request.AcceptIfKey(ConnectionKey);
            };

            clientListener.PeerDisconnectedEvent += (peer, disconnectInfo) =>
            {
                Console.WriteLine("PeerDisconnected: " + disconnectInfo.Reason);
                if (disconnectInfo.AdditionalData.AvailableBytes > 0)
                {
                    Console.WriteLine("Disconnect data: " + disconnectInfo.AdditionalData.GetInt());
                }
            };

            natPunchListener.NatIntroductionSuccess += (point, addrType, token) =>
            {
                var peer = client.Connect(point, ConnectionKey);
                Console.WriteLine($"NatIntroductionSuccess. Connecting to: {point}, type: {addrType}, connection created: {peer != null}");
            };

            client = new NetManager(clientListener)
            {
                IPv6Enabled = false,
                NatPunchEnabled = true
            };

            client.NatPunchModule.Init(natPunchListener);
            client.Start();

            // Send our LAN/WAN IP and port to the gateway.
            client.NatPunchModule.SendNatIntroduceRequest(gateway, gatewayPort, "token1");

            while (true)
            {
                DateTime nowTime = DateTime.UtcNow;

                client.NatPunchModule.PollEvents();
                client.PollEvents();

                Thread.Sleep(10);
            }

            client.Stop();
        }
    }
}
