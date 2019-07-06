// ================================================================================================================================
// File:        IPAddressGetter.cs
// Description: 
// Author:      Harley Laurie https://www.github.com/Swaelo/
// ================================================================================================================================

using System;
using System.Net;
using System.Net.Sockets;

namespace Server.Misc
{
    public static class IPAddressGetter
    {
        public static string GetLocalIPAddress()
        {
            var Host = Dns.GetHostEntry(Dns.GetHostName());
            foreach(var IP in Host.AddressList)
            {
                if (IP.AddressFamily == AddressFamily.InterNetwork)
                    return IP.ToString();
            }

            Console.WriteLine("Error looking up local IP address, returning 127.0.0.1");
            return "127.0.0.1";
        }
    }
}