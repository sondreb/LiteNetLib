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
            EventBasedNetListener listener = new EventBasedNetListener();
            EventBasedNatPunchListener natPunchListener = new EventBasedNatPunchListener();

            listener.PeerConnectedEvent += peer =>
            {
                Console.WriteLine("CLIENT: PeerConnected: " + peer.EndPoint);
            };

            listener.ConnectionRequestEvent += request =>
            {
                Console.WriteLine("CLIENT: ConnectionRequestEvent: " + ConnectionKey);
                request.AcceptIfKey(ConnectionKey);
            };

            listener.PeerDisconnectedEvent += (peer, disconnectInfo) =>
            {
                Console.WriteLine("CLIENT: PeerDisconnected: " + disconnectInfo.Reason);
                if (disconnectInfo.AdditionalData.AvailableBytes > 0)
                {
                    Console.WriteLine("CLIENT: Disconnect data: " + disconnectInfo.AdditionalData.GetInt());
                }
            };

            listener.NetworkReceiveEvent += (NetPeer peer, NetPacketReader reader, DeliveryMethod deliveryMethod) => {
                Console.WriteLine("CLIENT: NetworkReceiveEvent");
            };

            listener.NetworkErrorEvent += (IPEndPoint endPoint, System.Net.Sockets.SocketError socketError) => {
                Console.WriteLine("CLIENT: NetworkErrorEvent");
            };

            listener.NetworkReceiveUnconnectedEvent += (IPEndPoint remoteEndPoint, NetPacketReader reader, UnconnectedMessageType messageType) => {
                Console.WriteLine("CLIENT: NetworkReceiveUnconnectedEvent");
            };

            listener.NetworkErrorEvent += (IPEndPoint endPoint, System.Net.Sockets.SocketError socketError) => {
                Console.WriteLine("CLIENT: NetworkErrorEvent");
            };

            listener.NetworkLatencyUpdateEvent += (NetPeer peer, int latency) =>  {
                //Console.WriteLine("CLIENT: NetworkLatencyUpdateEvent: Latency: " + latency);
            };

            listener.DeliveryEvent += (NetPeer peer, object userData) => {
                Console.WriteLine("CLIENT: DeliveryEvent");
            };

            natPunchListener.NatIntroductionSuccess += (point, addrType, token) =>
            {
                var peer = client.Connect(point, ConnectionKey);
                Console.WriteLine($"CLIENT: NatIntroductionSuccess. Connecting to: {point}, type: {addrType}, connection created: {peer != null}");
            };

            client = new NetManager(listener)
            {
                IPv6Enabled = false,
                NatPunchEnabled = true
            };

            client.NatPunchModule.Init(natPunchListener);
            client.Start();
            //client.EnableStatistics = true;

            var data = Encoding.UTF8.GetBytes("TEST");
            client.SendToAll(data, DeliveryMethod.ReliableUnordered);

            // Send our LAN/WAN IP and port to the gateway.
            client.NatPunchModule.SendNatIntroduceRequest(gateway, gatewayPort, "token1");

            while (true)
            {
                DateTime nowTime = DateTime.UtcNow;

                client.NatPunchModule.PollEvents();
                client.PollEvents();

                //Console.WriteLine(client.Statistics);

                Thread.Sleep(10);
            }

            client.Stop();
        }
    }
}
