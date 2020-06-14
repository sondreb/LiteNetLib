using LiteNetLib;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading;

namespace HubbyGateway
{
    public class HubbyGatewayManager : INatPunchListener
    {
        private const int ServerPort = 15050;
        private const string ConnectionKey = "12345";
        private readonly Dictionary<string, WaitPeer> waitingPeers = new Dictionary<string, WaitPeer>();
        private NetManager server;

        public void OnNatIntroductionRequest(IPEndPoint localEndPoint, IPEndPoint remoteEndPoint, string token)
        {
            if (waitingPeers.TryGetValue(token, out var wpeer))
            {
                if (wpeer.InternalAddr.Equals(localEndPoint) &&
                    wpeer.ExternalAddr.Equals(remoteEndPoint))
                {
                    wpeer.Refresh();
                    return;
                }

                Console.WriteLine("Wait peer found, sending introduction...");

                //found in list - introduce client and host to eachother
                Console.WriteLine(
                    "host - i({0}) e({1})\nclient - i({2}) e({3})",
                    wpeer.InternalAddr,
                    wpeer.ExternalAddr,
                    localEndPoint,
                    remoteEndPoint);

                server.NatPunchModule.NatIntroduce(
                    wpeer.InternalAddr, // host internal
                    wpeer.ExternalAddr, // host external
                    localEndPoint, // client internal
                    remoteEndPoint, // client external
                    token // request token
                    );

                //Clear dictionary
                waitingPeers.Remove(token);
            }
            else
            {
                Console.WriteLine("Wait peer created. i({0}) e({1})", localEndPoint, remoteEndPoint);
                waitingPeers[token] = new WaitPeer(localEndPoint, remoteEndPoint);
            }
        }

        public void OnNatIntroductionSuccess(IPEndPoint targetEndPoint, NatAddressType type, string token)
        {
            //Ignore we are server
        }

        public void Run()
        {
            EventBasedNetListener clientListener = new EventBasedNetListener();

            clientListener.PeerConnectedEvent += peer =>
            {
                Console.WriteLine("PeerConnected: " + peer.EndPoint);
            };

            clientListener.ConnectionRequestEvent += request =>
            {
                Console.WriteLine("ConnectionRequestEvent: " + ConnectionKey);
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

            server = new NetManager(clientListener)
            {
                IPv6Enabled = false,
                NatPunchEnabled = true
            };

            server.Start(ServerPort);
            server.NatPunchModule.Init(this);

            while (true)
            {
                DateTime nowTime = DateTime.UtcNow;

                server.NatPunchModule.PollEvents();
            
                Thread.Sleep(10);
            }

            server.Stop();
        }
    }
}
