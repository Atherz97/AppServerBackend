using System;
using System.Timers;
using STA.Settings;

namespace BBLegacyServer
{
    /// <summary>
    /// Handling of the data file.
    /// </summary>
    public class UpdateINI
    {
        /// <summary>
        /// An object that handles the updating of the INI File.
        /// </summary>
        /// <param name="InitialTime">Starting time to countdown from (in milliseconds).</param>
        public UpdateINI(int InitialTime)
        {
            Timer update = new Timer(InitialTime);
            update.Elapsed += new ElapsedEventHandler(BackuptheINIFile);
            update.Enabled = true;
        }

        private static void BackuptheINIFile(object source, ElapsedEventArgs e)
        {
            NowBackup();
            Timer timer = (Timer)source;
            timer.Interval = 3600000;
        }

        public static void NowBackup()
        {
            INIFile Settings = (BBLegacyServer.MainServer.SettingsFile);
            CmdSystem.AddLog("INI File Backup");
            Settings.SetValue("Server Configuration", "MaxClients", BBLegacyServer.MainServer.MaxClients);
            Settings.SetValue("Server Configuration", "Port", BBLegacyServer.MainServer.Port);
            Settings.SetValue("Server Configuration", "Local", BBLegacyServer.MainServer.IsLocal);
            Settings.SetValue("Server Configuration", "NumberOfPlayers", BBLegacyServer.MainServer.PlayerList.Count);

            var tmp = 1;
            foreach (PlayerListItem i in MainServer.PlayerList)
            {
                Settings.SetValue("PlayerListItem" + tmp, "ID", i.ID);
                Settings.SetValue("PlayerListItem" + tmp, "Name", i.Name);
                Settings.SetValue("PlayerListItem" + tmp, "Tag", i.Tag);
                tmp++;
            }

            Settings.Flush();
            
        }
    }

    /// <summary>
    /// Handles updating server messages for all clients.
    /// </summary>
    public class MessageUpdater
    {
        public MessageUpdater(int InitialTime)
        {
            Timer update = new System.Timers.Timer(InitialTime);
            update.Elapsed += new ElapsedEventHandler(DoWork);
            update.Enabled = true;
        }
        
        private static void DoWork(object source, ElapsedEventArgs e)
        {
            SendMessagePacket();
        }

        public static void SendMessagePacket()
        {
            foreach (System.Collections.Generic.KeyValuePair<long, TcpClientSocket> Socket in TcpListenerSocket.ClientList)
            {
                var Player = Socket.Value;
                var Write = Player.WriteBuffer;
                Write.Clear();
                Write.WriteUByte(Player.HeaderId);
                Write.WriteUByte(Code.ServerMessages);
                Write.WriteString(MainServer.GoodbyeMessage);
                Write.WriteString(MainServer.DCMessage);
                Write.WriteString(MainServer.KickMessage);
                Write.SendTcp(Player.DataStream);
            }
        }

        public static void BroadcastMessage(string msg)
        {
            foreach (System.Collections.Generic.KeyValuePair<long, TcpClientSocket> Socket in TcpListenerSocket.ClientList)
            {
                var Player = Socket.Value;
                var Write = Player.WriteBuffer;
                Write.Clear();
                Write.WriteUByte(Player.HeaderId);
                Write.WriteUByte(Code.Message);
                Write.WriteString(msg);
                Write.SendTcp(Player.DataStream);
            }
        }
    }
}
