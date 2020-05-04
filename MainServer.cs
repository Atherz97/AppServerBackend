using System;
using System.Threading;
using STA.Settings;
using System.Text.RegularExpressions;
using System.Net;
using System.IO;
using System.Collections.Generic;

namespace BBLegacyServer 
{
	public static class MainServer 
    {
        
        private static int ReadSize = 1024;
        private static int WriteSize = 512;
        private static int Alignment = 1;
        public const int PacketHeader = 203;
        private static string TheIPAddress;
        public static TcpListenerSocket myTcpSocket;
        public static UdpServerSocket myUdpSocket;
        public static Automation Event;

        //Load settings from the INI file.
        public static INIFile SettingsFile = new INIFile("settings.ini");
        public static int Port = SettingsFile.GetValue("Server Configuration", "Port", 43598);
        public static int MaxClients = SettingsFile.GetValue("Server Configuration", "MaxClients", 100);
        public static bool IsLocal = SettingsFile.GetValue("Server Configuration", "Local", 0) == 1;
        public static bool CanCreateRooms = SettingsFile.GetValue("Server Configuration", "PlayersCanCreateRooms", 1) == 1;
        public static bool NotifyOfBots = SettingsFile.GetValue("Server Configuration", "NotifyOfBots", 0) == 1;
        //This is where we load our Server Messages. The client will ask for each of these and will be shown at the appropriate time.
        //If you wish to NOT use these, give them a value of "(null)"
        public static string WelcomeMessage = SettingsFile.GetValue("Server Configuration", "WelcomeMessage", "Connection made. Welcome to the Network.");
            public static string KickMessage = SettingsFile.GetValue("Server Configuration", "KickMessage", "(null)");
            public static string DCMessage = SettingsFile.GetValue("Server Configuration", "DCMessage", "You have been disconnected from the network.");
            public static string GoodbyeMessage = SettingsFile.GetValue("Server Configuration", "GoodbyeMessage", "Thanks for playing, {NAME}.");
            public static string InfoMessage = SettingsFile.GetValue("Server Configuration", "InfoMessage", "Welcome to this network! The rooms are shown below. Choose a room, or create your own, and start playing! " + "[Svr v. 1.1]");
        //We should create a list of values for the roomlist.
        public static List<Room> RoomList = new List<Room>();
        public static List<PlayerListItem> PlayerList = new List<PlayerListItem>();
        public static uint RoomNumb = 0;
        public static List<byte> SessionNumberList = new List<byte>();
        public static MessageUpdater MessageUpdate;

		public static void Main() 
        {

			ClockTimer.RecordRunTime();

            Console.Clear();
            Console.WriteLine("    ====================================================    ");
            Console.WriteLine("    Welcome to the BBLegacy Dedicated Server [prototype]    ");
            Console.WriteLine();
            Console.WriteLine("     INIFile Management Code by S.T.A. snc (C)2009-2013     ");
            Console.WriteLine("    Networking System by FatalSleep from the GMCommunity    ");
            Console.WriteLine("    ====================================================    ");
            Console.WriteLine();

            //Fetch the External IP Address, if they're not using local.
            
            if (IsLocal)
            {
                TheIPAddress = "127.0.0.1";
            }
            else
            {
                Console.WriteLine("Fetching IP Address... ");
                TheIPAddress = getExternalIp();

                Console.WriteLine("     " + TheIPAddress + ", LAN " + getInternalIP());
            }

            Console.WriteLine("Loading player database...");
            new UpdateINI(3600000);
            MessageUpdate = new MessageUpdater(120000);
            Event = new Automation();

            var tmp = 1;
            while (tmp <= SettingsFile.GetValue("Server Configuration", "NumberOfPlayers",1))
            {
                PlayerListItem tmpp = new PlayerListItem();
                tmpp.ID = SettingsFile.GetValue("PlayerListItem" + tmp, "ID", 0.00);
                tmpp.Name = SettingsFile.GetValue("PlayerListItem" + tmp, "Name", "{NULL}");
                tmpp.Tag = (byte)SettingsFile.GetValue("PlayerListItem" + tmp, "Tag", 0);
                Console.WriteLine("    PL" + tmpp.ID + " " + tmpp.Name + " (" + tmpp.Tag + ")");
                tmp++;

                if (tmpp.Tag == 0) tmpp.Automate();
            }

            Console.WriteLine("done.");

            myTcpSocket = new TcpListenerSocket(TheIPAddress, Port, MaxClients, ReadSize, WriteSize, Alignment, PacketHeader);
            
            myUdpSocket = new UdpServerSocket(Port, ReadSize, WriteSize, Alignment, PacketHeader);

            Console.WriteLine("UDP Connection Listener Established (" + TheIPAddress + " :: " + Port + ")");
            string Command = "";
            
            if (!CanCreateRooms)
                Console.WriteLine("WARNING: Player creation of rooms is disabled. You MUST use room creation commands in order for them to play on your server!");

            Thread.Sleep(2000);
            Console.WriteLine("You can now type 'help' for some commands.");
            Console.WriteLine();
            Console.WriteLine("    ====================================================    ");

            while (myTcpSocket.Status == true || myUdpSocket.Status == true)
                {
                    try
                    {
                            Command = Console.ReadLine();
                            CmdSystem.RunCmd(Command);
                    }
                    catch (Exception e)
                    {
                        CmdSystem.AddLog("UNCAUGHT EXCEPTION: "+e.GetType()+" :: "+e.Message);
                        CmdSystem.AddLog("Stack Trace: ");
                        Console.WriteLine();
                        Console.WriteLine(e.StackTrace);
                        Console.WriteLine();
                    }
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
            catch { return "{NULL IP}"; }
        }

        private static string getInternalIP()
        {
            IPHostEntry host;
            string localIP = "";
            host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (IPAddress ip in host.AddressList)
            {
                if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                {
                localIP = ip.ToString();
                break;
                }
            }
            return localIP;
        }

    }

}
