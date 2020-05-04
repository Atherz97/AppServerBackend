using System;
using System.Threading;
using STA.Settings;
using System.Text.RegularExpressions;
using System.Net;
using System.IO;

namespace BBLegacyServer 
{
	public static class MainServer 
    {
        
        private static int ReadSize = 512;
        private static int WriteSize = 512;
        private static int Alignment = 1;
        public static int PacketHeader = 203;
        private static string TheIPAddress;
        
        //Load settings from the INI file.
        public static INIFile SettingsFile = new INIFile("settings.ini");
        public static int Port = SettingsFile.GetValue("Server Configuration", "Port", 43598);
        public static int MaxClients = SettingsFile.GetValue("Server Configuration", "MaxClients", 100);
        public static int IsLocal = SettingsFile.GetValue("Server Configuration", "Local", 0);

		public static void Main() 
        {

			ClockTimer.RecordRunTime();
            
            Console.WriteLine("    ====================================================    ");
            Console.WriteLine("    Welcome to the BBLegacy Dedicated Server [prototype]    ");
            Console.WriteLine();
            Console.WriteLine("     INIFile Management Code by S.T.A. snc (C)2009-2013     ");
            Console.WriteLine("    Networking System by FatalSleep from The GMCommunity    ");
            Console.WriteLine("    ====================================================    ");
            Console.WriteLine();

            //Fetch the External IP Address, if they're not using local.
            string TheIPAddress;
            if (IsLocal == 1)
            {
                TheIPAddress = "127.0.0.1";
            }
            else
            {
                Console.Write("Fetching IP Address... ");
                TheIPAddress = getExternalIp();
                Console.WriteLine("done.");
            }
            UpdateINI Backupper = new UpdateINI(60000);

            TcpListenerSocket myTcpSocket = new TcpListenerSocket(TheIPAddress, Port, MaxClients, ReadSize, WriteSize, Alignment, PacketHeader);
            Console.WriteLine("TCP Connection Listener Established ("+TheIPAddress+" :: "+Port+")");
            Console.WriteLine(" - The connection is set to have a maximum of " + MaxClients + " BBLegacy connections.");
            UdpServerSocket myUdpSocket = new UdpServerSocket(Port, ReadSize, WriteSize, Alignment, PacketHeader);
            Console.WriteLine("UDP Connection Listener Established (" + TheIPAddress + " :: " + Port + ")");
            string Command = "";

            Thread.Sleep(3000);
            
            Thread.Sleep(1000);
            Console.WriteLine("You can now type 'help' for some commands.");
            Console.WriteLine();
            Console.WriteLine("    ====================================================    ");

            while (myTcpSocket.Status == true || myUdpSocket.Status == true)
            {
                Command = Console.ReadLine();
                CmdSystem.RunCmd(Command, myTcpSocket, myUdpSocket);
            }

			ClockTimer.StopRunTime();
		}

        private static string getExternalIp()
        {
            try
            {
                string externalIP;
                externalIP = (new WebClient()).DownloadString("http://checkip.dyndns.org/");
                externalIP = (new Regex(@"\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}"))
                             .Matches(externalIP)[0].ToString();
                return externalIP;
            }
            catch { return null; }
        }
	}

}
