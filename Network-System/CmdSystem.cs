using System.Collections.Generic;
using System;
using System.Runtime.CompilerServices;

namespace BBLegacyServer {
	public static class CmdSystem {
		public static List<string> CmdLog = new List<string>();
        public static bool LastCommandSuccessful = false;
        public static string LastLog = "null";

		public static void AddLog( string myLog ) {
			long RunTime = ClockTimer.RunTime.ElapsedMilliseconds;
			CmdLog.Add( RunTime.ToString() + "ms :: " + myLog.ToString() );

			if ( CmdLog.Count >= 100 ) {
				CmdLog.RemoveAt( 0 );
			}

            //I also want it to appear live.
            Console.WriteLine(RunTime.ToString() + "ms :: " + myLog.ToString());
            LastLog = myLog;
        }

		public static void RunCmd( string myCmd ) {
            LastCommandSuccessful = true;

            long RunTime = ClockTimer.GetRunTime();
            string command = myCmd.ToLower();

            TcpListenerSocket myTcpServer = MainServer.myTcpSocket;
            UdpServerSocket myUdpServer = MainServer.myUdpSocket;

            //Special parameter-needing commands
            if (command.StartsWith("setmsg welcome ")) { MainServer.WelcomeMessage = myCmd.Substring(15); MessageUpdater.SendMessagePacket(); CmdSystem.AddLog("Welcome message set to '" + MainServer.WelcomeMessage + "'"); return; }
            if (command.StartsWith("setmsg kick ")) { MainServer.KickMessage = myCmd.Substring(12); MessageUpdater.SendMessagePacket(); CmdSystem.AddLog("Kick message set to '" + MainServer.KickMessage + "'"); return; }
            if (command.StartsWith("setmsg dc ")) { MainServer.DCMessage = myCmd.Substring(10); MessageUpdater.SendMessagePacket(); CmdSystem.AddLog("Disconnected message set to '" + MainServer.DCMessage + "'"); return; }
            if (command.StartsWith("setmsg goodbye ")) { MainServer.GoodbyeMessage = myCmd.Substring(15); MessageUpdater.SendMessagePacket(); CmdSystem.AddLog("Disconnected message set to '" + MainServer.GoodbyeMessage + "'"); return; }
            if (command.StartsWith("setmsg info ")) { MainServer.InfoMessage = myCmd.Substring(12); MessageUpdater.SendMessagePacket(); CmdSystem.AddLog("Info message set to '" + MainServer.InfoMessage + "'"); return; }
            if (command.StartsWith("mm ")) { MessageUpdater.BroadcastMessage(myCmd.Substring(3)); CmdSystem.AddLog("BROADCASTED: "+ myCmd.Substring(3)); return; }

            if (command.StartsWith("closeroom "))
            {
                foreach (Room i in MainServer.RoomList)
                    if (i.id == Int32.Parse(myCmd.Substring(10)))
                    {
                        i.Dispose();
                        CmdSystem.AddLog("Room " + myCmd.Substring(10) + " disposed");
                        return;
                    }
                CmdSystem.AddLog("Could not find room " + myCmd.Substring(10) );
                return;
            }
            if (command.StartsWith("kick ")) {
                foreach (KeyValuePair<long, TcpClientSocket> i in TcpListenerSocket.ClientList)
                    if (i.Value.SocketId == Int32.Parse(myCmd.Substring(5)))
                    {
                        var Write = i.Value.WriteBuffer;
                        Write.Clear();
                        Write.WriteUByte(i.Value.HeaderId);
                        Write.WriteUByte(Code.ConnectionEnd);
                        Write.SendTcp(i.Value.DataStream);

                        i.Value.UserImposedDisconnection = true;
                        i.Value.Connected = false;

                        CmdSystem.AddLog("Player " + i.Value.Name + " has been kicked");
                        return;
                    }
                CmdSystem.AddLog("Could not find player with socket " + myCmd.Substring(5));
                return;
            }
            if (command.StartsWith("admin "))
            {
                foreach (KeyValuePair<long, TcpClientSocket> i in TcpListenerSocket.ClientList)
                    if (i.Value.ID == Int32.Parse(myCmd.Substring(6)))
                    {
                        if (i.Value.Tag == 2)
                            i.Value.Tag = 1;
                        else
                            i.Value.Tag = 2;

                        CmdSystem.AddLog("Player " + i.Value.Name + " admin: "+(i.Value.Tag == 2));
                        return;
                    }
                CmdSystem.AddLog("Could not find player with ID " + myCmd.Substring(6));
                return;
            }

            //Normal commands
            switch ( command ) {

				case "help":
					Console.WriteLine("========================" );
                    Console.WriteLine();
                    Console.WriteLine("    " + "help - displays list of commands");
                    Console.WriteLine("    " + "tcpstop - ends TCP communications");
                    Console.WriteLine("    " + "udpstop - ends UDP communications");
                    Console.WriteLine("    " + "count - counts all the TCP connections");
                    Console.WriteLine("    " + "status - tells how the server is doing");
                    Console.WriteLine("    " + "unlog - clears the log");
                    Console.WriteLine("    " + "clear - clears the console");
                    Console.WriteLine("    " + "update - force update ini file");
                    Console.WriteLine();
                    Console.WriteLine("    " + "roomlist - displays a list of all open rooms");
                    Console.WriteLine("    " + "players - lists all online players");
                    Console.WriteLine("    " + "room solo - Creates a solo play room for all modes");
                    Console.WriteLine("    " + "room team - Creates a team play room for all modes");
                    Console.WriteLine();
                    Console.WriteLine("    " + "clear servers - closes all server rooms");
                    Console.WriteLine("    " + "clear rooms - closes all rooms");
                    Console.WriteLine("    " + "kickall - kicks everyone off the server");
                    Console.WriteLine("    " + "clear pl - clears the player list");
                    Console.WriteLine("    " + "closeroom [roomID]");
                    Console.WriteLine("    " + "kick [socketID]");
                    Console.WriteLine("    " + "admin [playerid] - toggle admin status of this user");
                    Console.WriteLine();
                    Console.WriteLine("    " + "setmsg welcome [message]");
                    Console.WriteLine("    " + "setmsg goodbye [message]");
                    Console.WriteLine("    " + "setmsg kick [message]");
                    Console.WriteLine("    " + "setmsg dc [message]");
                    Console.WriteLine("    " + "setmsg info [message]");
                    Console.WriteLine("    " + "toggle makerooms - toggles player creation of rooms");
                    Console.WriteLine("    " + "toggle notifybots - toggles notification of tag 0 players");
                    Console.WriteLine();
                    Console.WriteLine("========================");
                break;

                case "tcpstop":
					if ( myTcpServer != null && myTcpServer.Status == true ) {
						myTcpServer.Status = false;
						Console.WriteLine( "Time(" + RunTime.ToString() + ") :: TCP Server Stopped" );
					}
				break;

				case "udpstop":
					if ( myUdpServer != null && myUdpServer.Status == true ) {
						myUdpServer.Status = false;
						Console.WriteLine( "Time(" + RunTime.ToString() + ") :: UDP Server Stopped" );
					}
				break;

                case "exit":
                    {
                        if ( myTcpServer != null && myTcpServer.Status == true ) {
						myTcpServer.Status = false;
						Console.WriteLine( "Time(" + RunTime.ToString() + ") :: TCP Server Stopped" );}
                            if ( myUdpServer != null && myUdpServer.Status == true ) {
						myUdpServer.Status = false;
						Console.WriteLine( "Time(" + RunTime.ToString() + ") :: UDP Server Stopped" );}

                        MainServer.SettingsFile.Flush();

                        break;
                    }

				case "status":
                    Console.WriteLine("Time(" + RunTime.ToString() + ") :: Server Status:");
					if ( myTcpServer != null && myTcpServer.Status == true ) 
                    {
						Console.WriteLine("    :: TCP Status(" + myTcpServer.Status.ToString() + ")" );
					}
                    if (myUdpServer != null && myUdpServer.Status == true)
                    {
                        Console.WriteLine("    :: UDP Status(" + myUdpServer.Status.ToString() + ")");
                    }
                    if (myTcpServer != null && myTcpServer.Status == true)
                    {
                        Console.WriteLine("    :: Client Count(" + TcpListenerSocket.ClientList.Count.ToString() + ")");
                    }
				break;

				case "unlog":
					CmdLog.Clear();
					Console.WriteLine( RunTime.ToString() + "ms :: Log Cleared" );
				break;

				case "clear":
					Console.Clear();
					Console.WriteLine( RunTime.ToString() + "ms :: Console Cleared" );
				break;

                case "clear pl":
                    MainServer.PlayerList.Clear();
                    Console.WriteLine(RunTime.ToString() + "ms :: Player List Cleared");
                break;

                case "update":
                    UpdateINI.NowBackup();
                    Console.WriteLine(RunTime.ToString() + "ms :: INI Update Complete");
                break;

                case "room solo":
                    var rm = new Room();
                    rm.teams = false;
                break;

                case "room team":
                    var room = new Room();
                    room.teams = true;
                break;

                case "players":
                    if (TcpListenerSocket.ClientList.Count == 0)
                    {
                        Console.WriteLine("    No players are connected!");
                        break;
                    }
                    Console.WriteLine("Current Player List");
                    foreach (KeyValuePair<long, TcpClientSocket> Player in TcpListenerSocket.ClientList)
                    {
                        Console.WriteLine("   (" + Player.Value.SocketId + ") id:" + Player.Value.ID + " n:" + Player.Value.Name);
                    }
                break;

                case "roomlist":
                    if (MainServer.RoomList.Count == 0)
                    {
                        Console.WriteLine("    No rooms are currently open!");
                        break;
                    }
                    Console.WriteLine("Current Room List");
                    foreach (Room Room in MainServer.RoomList)
                    {
                        Console.WriteLine("   " + Room.id + ": " + Room.name + " hosted by " + Room.host_name);
                        Console.WriteLine("      "  + "Mode: " + Room.mode + " | "
                                                    + "Teams: " + Room.teams + " | "
                                                    + "Mins: " + Room.minutes + " | "
                                                    + "MaxPlyrs: " + Room.maxplayers + " | "
                                                    + "RoomState: " + Room.playstate + " | ");
                        foreach (TcpClientSocket Player in Room.players)
                        {
                            Console.WriteLine("      " + Player.Slot + ". " + Player.Name + "| C: "+ Player.Character+(Room.teams? " T: " + Player.Team : "" ));
                        }
                        Console.WriteLine();
                    }
                break;

                case "clear rooms":
                try
                {
                    List<Room> Rooms = new List<Room>(MainServer.RoomList);
                    foreach (Room i in Rooms)
                        i.Dispose();
                    CmdSystem.AddLog("    All rooms have been cleared, and players kicked out to the online lobby.");
                }
                catch
                {
                    CmdSystem.AddLog("    Failed to clear roomlist.");
                }
                MainServer.RoomNumb = 0;
                break;

                case "clear servers":
                try
                {
                    List<Room> Rooms = new List<Room>(MainServer.RoomList);
                    foreach (Room i in Rooms)
                        if (i.tag == 0) i.Dispose();
                    CmdSystem.AddLog("    All server rooms have been cleared, and their players kicked out to the main menu.");
                }
                catch
                {
                    CmdSystem.AddLog("    Failed to clear.");
                }
                MainServer.RoomNumb = 0;
                break;

                case "kickall":
                try
                {
                    foreach (KeyValuePair<long, TcpClientSocket> Player in TcpListenerSocket.ClientList)
                    {
                        var Write = Player.Value.WriteBuffer;
                        Write.Clear();
                        Write.WriteUByte(Player.Value.HeaderId);
                        Write.WriteUByte(Code.ConnectionEnd);
                        Write.SendTcp(Player.Value.DataStream);

                        Player.Value.UserImposedDisconnection = true;
                        Player.Value.Connected = false;
                    }
                    CmdSystem.AddLog("    All players have been kicked.");
                }
                catch
                { CmdSystem.AddLog("    Kicking failed."); }
                break;

                case "toggle makerooms":
                    MainServer.CanCreateRooms = !MainServer.CanCreateRooms;
                    Console.WriteLine("    Player creation of rooms set to " + MainServer.CanCreateRooms + ".");
                break;

                case "toggle notifybots":
                    MainServer.NotifyOfBots = !MainServer.NotifyOfBots;
                    Console.WriteLine("    Bot notifications set to " + MainServer.NotifyOfBots + ".");
                break;

                default: CmdSystem.AddLog("    Unknown command"); LastCommandSuccessful = false;  break;
			}
		}
	}
}
