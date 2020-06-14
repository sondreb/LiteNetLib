using LiteNetLib;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading;

namespace HubbyGateway
{
    public class HubInfo
    {
        public string Name
        {
            get; set;
        }
    }

    public class HubbyGatewayManager : INatPunchListener
    {
        private const int ServerPort = 15050;
        private const string ConnectionKey = "12345";
        private readonly Dictionary<string, WaitPeer> connectedHubs = new Dictionary<string, WaitPeer>();
        private NetManager server;

        public void OnNatIntroductionRequest(IPEndPoint localEndPoint, IPEndPoint remoteEndPoint, string token)
        {
            var hubInfo = new HubInfo();
            hubInfo.Name = "Hub";

            var data = Encoding.UTF8.GetBytes("HUBINFO");
            this.server.SendToAll(data, DeliveryMethod.ReliableUnordered);

            if (connectedHubs.TryGetValue(token, out var wpeer))
            {
                if (wpeer.InternalAddr.Equals(localEndPoint) &&
                    wpeer.ExternalAddr.Equals(remoteEndPoint))
                {
                    wpeer.Refresh();
                    return;
                }

                Console.WriteLine("GATEWAY: Wait peer found, sending introduction...");

                //found in list - introduce client and host to eachother
                Console.WriteLine("GATEWAY: host - i({0}) e({1}) | client - i({2}) e({3})",
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
                connectedHubs.Remove(token);
            }
            else
            {
                Console.WriteLine("GATEWAY: Wait peer created. i({0}) e({1})", localEndPoint, remoteEndPoint);
                connectedHubs[token] = new WaitPeer(localEndPoint, remoteEndPoint);
            }
        }

        public void OnNatIntroductionSuccess(IPEndPoint targetEndPoint, NatAddressType type, string token)
        {
            //Ignore we are server
        }

        public void Run()
        {
            EventBasedNetListener listener = new EventBasedNetListener();

            listener.PeerConnectedEvent += peer =>
            {
                Console.WriteLine("GATEWAY: PeerConnected: " + peer.EndPoint);
            };

            listener.ConnectionRequestEvent += request =>
            {
                Console.WriteLine("GATEWAY: ConnectionRequestEvent: " + ConnectionKey);
                request.AcceptIfKey(ConnectionKey);
            };

            listener.PeerDisconnectedEvent += (peer, disconnectInfo) =>
            {
                Console.WriteLine("GATEWAY: PeerDisconnected: " + disconnectInfo.Reason);
                if (disconnectInfo.AdditionalData.AvailableBytes > 0)
                {
                    Console.WriteLine("GATEWAY: Disconnect data: " + disconnectInfo.AdditionalData.GetInt());
                }
            };

            listener.NetworkReceiveEvent += (NetPeer peer, NetPacketReader reader, DeliveryMethod deliveryMethod) => {
                Console.WriteLine("GATEWAY: NetworkReceiveEvent");
            };

            listener.NetworkErrorEvent += (IPEndPoint endPoint, System.Net.Sockets.SocketError socketError) => {
                Console.WriteLine("GATEWAY: NetworkErrorEvent");
            };

            listener.NetworkReceiveUnconnectedEvent += (IPEndPoint remoteEndPoint, NetPacketReader reader, UnconnectedMessageType messageType) => {
                Console.WriteLine("GATEWAY: NetworkReceiveUnconnectedEvent");
            };

            listener.NetworkErrorEvent += (IPEndPoint endPoint, System.Net.Sockets.SocketError socketError) => {
                Console.WriteLine("GATEWAY: NetworkErrorEvent");
            };

            listener.NetworkLatencyUpdateEvent += (NetPeer peer, int latency) => {
                Console.WriteLine("GATEWAY: NetworkLatencyUpdateEvent: Latency: " + latency);
            };

            listener.DeliveryEvent += (NetPeer peer, object userData) => {
                Console.WriteLine("GATEWAY: DeliveryEvent");
            };

            server = new NetManager(listener)
            {
                IPv6Enabled = false,
                NatPunchEnabled = true
            };

            server.Start(ServerPort);
            server.NatPunchModule.Init(this);
            //server.EnableStatistics = true;

            while (true)
            {
                DateTime nowTime = DateTime.UtcNow;

                server.NatPunchModule.PollEvents();
                server.PollEvents();

                foreach (var hub in connectedHubs)
                {
                    Console.WriteLine("Hub: " + hub.Value.InternalAddr.ToString());
                }

                //var data = Encoding.UTF8.GetBytes("HUBINFO");
                //server.ConnectedPeerList[0].Send(data, DeliveryMethod.ReliableUnordered);

                //server.SendToAll()
                //Console.WriteLine(server.Statistics);

                Thread.Sleep(10);
            }

            server.Stop();
        }
    }
}
