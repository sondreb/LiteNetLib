using System;

namespace HubbyClient
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("--- Hubby Client ---");

            HubbyClientManager manager = new HubbyClientManager();
            manager.Run("localhost", 15050);
        }
    }
}
