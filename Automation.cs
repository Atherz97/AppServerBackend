using System.Threading;

namespace BBLegacyServer
{
    /// <summary>
    /// In order for some extra customization, this is the place where you can
    /// make the server yours. There's a timer event which happens once per second,
    /// and a while loop that happens 1000 times a second.
    /// 
    /// There are several events raised by various occurrences from players
    /// that can be managed here. You can e.g. limit the usages of specific items,
    /// control stages, message of the day, etc!
    /// </summary>
    public class Automation
    {
        //Iteration variables for timers, etc.
        private int a = 0;
        Thread thread;

        public Automation()
        {
            thread = new Thread(new ThreadStart(Automate));
            thread.Priority = ThreadPriority.BelowNormal;
            thread.Start();

            System.Timers.Timer time = new System.Timers.Timer(1000);
            time.AutoReset = true;
            time.Enabled = true;
            time.Elapsed += Alarm;
        }

        /// <summary>
        /// The main "intelligence" behind management of the server. I've provided
        /// the backbone. This is the consciousness.
        /// Use "RunCmd(command);" to run a command.
        /// Use "MainServer.variable.etc.etc()" to invoke methods in various places.
        /// </summary>
        private void Automate()
        {
            while (true)
            {
                Thread.Sleep(10);
            }
        }

        /// <summary>
        /// This might not be as necessary. It's a timer for events 
        /// and happens only 10 times per second.
        /// </summary>
        private void Alarm(object sender, System.Timers.ElapsedEventArgs e)
        {

        }

        /// <summary>
        /// When a room is created.
        /// </summary>
        public void RoomCreated(Room Room)
        { }

        /// <summary>
        /// When a room is closed.
        /// </summary>
        public void RoomClosed(Room Room)
        { }

        /// <summary>
        /// When a room is started for voting.
        /// </summary>
        public void RoomVoting(Room Room)
        { }

        /// <summary>
        /// When a room is started for battling.
        /// </summary>
        public void RoomStarted(Room Room)
        { }

        /// <summary>
        /// When all players have finished loading and the battle officially begins.
        /// </summary>
        /// <param name="Room"></param>
        public void RoomReady(Room Room)
        { }

        /// <summary>
        /// When a battle is finished.
        /// </summary>
        public void RoomFinished(Room Room)
        { }

        /// <summary>
        /// When a room is exited after battle.
        /// </summary>
        public void RoomExit(Room Room)
        { }

        public void PlayerConnected(TcpClientSocket Player)
        { }

        public void PlayerDisconnected(TcpClientSocket Player)
        { }

        public void PlayerCreatedRoom(TcpClientSocket Player, Room Room)
        { }

        public void PlayerJoinedRoom(TcpClientSocket Player, Room Room)
        { }

        public void PlayerLeftRoom(TcpClientSocket Player, Room Room)
        { }

        public void PlayerChangedChar(TcpClientSocket Player, byte Char)
        { }

        public void PlayerChangedTeam(TcpClientSocket Player, byte Team)
        { }

        public void PlayerUsedItem(TcpClientSocket Player, byte Item)
        { }

        public void PlayerScored(TcpClientSocket Player, short Score)
        { }

        public void PlayerDeath(TcpClientSocket Player, float Time)
        { }

        public void PlayerHit(TcpClientSocket Player, byte Type, float Time)
        { }
    }
}