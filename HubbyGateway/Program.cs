using System;

namespace HubbyGateway
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("--- Hubby Gateway ---");

            HubbyGatewayManager manager = new HubbyGatewayManager();
            manager.Run();

            Console.WriteLine("Press ENTER to exit.");
            Console.ReadLine();
        }
    }
}
