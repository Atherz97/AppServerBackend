using System.Timers;

namespace BBLegacyServer
{
    public class PlayerListItem
    {
        public double ID = 0;
        public byte Tag = 0;
        public string Name = "";
        public byte Status = 0;
        public TcpClientSocket Client;
        private Timer autobot;

        public PlayerListItem()
        {
            MainServer.PlayerList.Add(this);
        }

        public void Link(TcpClientSocket obj)
        {
            Client = obj;
            Name = obj.Name;
            Tag = obj.Tag;
            ID = obj.ID;
            obj.ListSlot = this;
        }

        public void GoOnline()
        {
            Status = Code.Status_AOnline;
            if (Tag == 0 && MainServer.NotifyOfBots) CmdSystem.AddLog("Bot "+Name+" has connected");
        }

        public void GoOffline()
        {
            Status = Code.Status_Offline;
            if (Tag == 0 && MainServer.NotifyOfBots) CmdSystem.AddLog("Bot " + Name + " has left");
        }

        public void GoPlayingOnline()
        {
            Status = Code.Status_POnline;
            if (Tag == 0 && MainServer.NotifyOfBots) CmdSystem.AddLog("Bot " + Name + " has entered a room");
        }

        public void Automate()
        {
            if (Tag != 0) return;

            autobot = new Timer(1000);
            autobot.Elapsed += Autobot_Elapsed;
            autobot.Enabled = true;
        }

        private void Autobot_Elapsed(object sender, ElapsedEventArgs e)
        {
            //What we want to do here is juggle the Online Status so that the person looks active.
            //They will be active for just about as long as a human should.
            
            System.Random r = new System.Random();
            double mins = r.NextDouble();
            //If available, can either wait another minute or go into battle.
            if (this.Status == Code.Status_AOnline)
            {
                if (r.Next(1, 3) == 1)
                {
                    mins = r.Next(1, 5);
                    GoPlayingOnline(); //Go in a room
                }
                else
                {
                    mins = r.NextDouble()*2; //Just chill
                    if (r.Next(1,8) == 1)
                    { 
                        GoOffline();
                        mins = r.NextDouble() * 60; //Go offline
                    }
                }
            }
            else
            {
                var rr = r.Next(1, 3);
                if (rr == 1)
                {
                    mins = r.NextDouble(); //Come online
                    GoOnline();
                }
            }
            autobot.Interval = 60000 * mins;
        }
    }
}
