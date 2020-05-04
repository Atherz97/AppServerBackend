using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace BBLegacyServer
{
    public class Room
    {
        public byte playstate = 1;
        public uint id = 0;
        public sbyte tag = 1;
        public bool visible = true;
        /* 
         * ROOM TAGS: 0 (Server-Created, shows larger on game), 1 (Player Created), 2 (Special Room, shows larger on game)
         */
        public String name = "";
        public String host_name = "";
        public List<TcpClientSocket> players;

        internal byte maxplayers = 2;
        internal byte minutes = 2;
        internal byte mode = 1;
        internal bool teams = false;
        internal byte maxteams = 0;
        internal byte currentstage = 0;

        public bool is_custom_items = false;
        public byte citm_1 = 0;
        public byte citm_2 = 0;
        public byte citm_3 = 0;
        public byte citm_4 = 0;
        public byte citm_5 = 0;

        private TcpClientSocket host;
        private Voter VoteMachine;
        private byte ReadyCount = 0;

        /// <summary>
        /// Creates a new Room, which acts as a Server, or a Room, or a Party, for players to join.
        /// Automatically places the host in there too, because... empty rooms? Nahh.
        /// </summary>
        /// <param name="host">Hosting Player</param>
        /// <param name="name">Name of the Room, will be seen by other players</param>
        /// <param name="mode">The starting mode, which can be changed later</param>
        /// <param name="isTeam">Are teams involved?</param>
        /// <param name="BattleLength">Length of the battles, in minutes</param>
        /// <param name="MaxPlayers">Maximum number of players</param>
        /// <param name="NumbTeams">Number of allowed teams</param>
        public Room(TcpClientSocket host, String name, byte mode, bool isTeam, byte NumbTeams, byte BattleLength, byte MaxPlayers)
        {
            MainServer.RoomList.Add(this);
            this.id = ++MainServer.RoomNumb;
            this.host = host;
            this.host_name = host.Name;
            this.name = name;
            this.mode = mode;
            this.teams = isTeam;
            this.minutes = BattleLength;
            this.maxplayers = MaxPlayers;
            this.maxteams = NumbTeams;
            this.tag = 1;
            players = new List<TcpClientSocket>();
            players.Add(host);
            host.CurrentRoom = this;
            host.Record = new Record(); //Recording for battles.
            host.Slot = 1;
            CmdSystem.AddLog(host.Name + " has created room " + this.name + " (ID:"+id+")");

            MainServer.Event.RoomCreated(this);
        }

        /// <summary>
        /// Creates a new Room, with all default values. This is normally called by the server.
        /// </summary>
        public Room()
        {
            MainServer.RoomList.Add(this);
            this.id = ++MainServer.RoomNumb;
            this.host = null;
            this.name = "Server Room " + this.id;
            this.host_name = "System";
            this.mode = 1;
            this.teams = false;
            this.minutes = 3;
            this.maxplayers = 8;
            this.maxteams = 2;
            this.tag = 0;
            CmdSystem.AddLog(this.name + " created.");
            players = new List<TcpClientSocket>();

            MainServer.Event.RoomCreated(this);
        }

        public uint Join(TcpClientSocket NewGuy)
        {
            //Already in a room!
            if (NewGuy.CurrentRoom != null) return (4000000002);
            
            //Room is full!
            if ((players.Count + 1) > maxplayers) return (4000000000);

            //Room is Voting or in Battle! (let's not do spectator yet)
            if ((tag != 0 && playstate != 1) || (tag == 0 && playstate > 2)) return (4000000001);

            //Success! Tell all current players that a new one's coming in.

            foreach (TcpClientSocket Player in players)
            {
                var Write = Player.WriteBuffer;
                Write.Clear();
                Write.WriteUByte(Player.HeaderId);
                Write.WriteUByte(Code.UpdatePlayers);
                Write.WriteBool(true);
                //A player has joined. Here's his or her name, icon, XP.
                Write.WriteString(NewGuy.Name);
                Write.WriteUByte(NewGuy.Tag);
                Write.WriteUByte(NewGuy.Icon);
                Write.WriteUInt(NewGuy.XP);
                Write.WriteUByte(NewGuy.Character);
                Write.WriteUByte(NewGuy.Team);
                Write.WriteDouble(NewGuy.ID);
                Write.SendTcp(Player.DataStream);
            }

            players.Add(NewGuy);

            NewGuy.CurrentRoom = this;
            NewGuy.Record = new Record(); //Recording for battles.

            NewGuy.Slot = (byte)(this.players.IndexOf(NewGuy) + 1);

            CmdSystem.AddLog(NewGuy.Name + " has joined room " + this.name + " (ID:" + this.id + ")");

            return (this.id);

        }

        /// <summary>
        /// Takes the player out of the room.
        /// </summary>
        /// <param name="Quitter">The Player to remove.</param>
        public byte Remove(TcpClientSocket Quitter)
        {
            if (Quitter.CurrentRoom == null) return 0;
            
            var place = players.IndexOf(Quitter) + 1;
            players.Remove(Quitter);

            try
            {
                foreach (TcpClientSocket Player in players)
                {
                    var Write = Player.WriteBuffer;
                    Write.Clear();
                    Write.WriteUByte(Player.HeaderId);
                    Write.WriteUByte(Code.UpdatePlayers);
                    Write.WriteBool(false);
                    //A player has left. Here's his place, but you might have to request the list again.
                    Write.WriteUByte((byte)place);
                    Write.SendTcp(Player.DataStream);

                    if (playstate < 3)
                    Player.Slot = (byte)(this.players.IndexOf(Player) + 1);

                }
            }
            catch { }

            Quitter.CurrentRoom = null;
            Quitter.Record = null;
            Quitter.Slot = 0;

            CmdSystem.AddLog(Quitter.Name + " has left room " + this.name + " (ID:" + this.id + ")");

            if (players.Count == 0)
            {
                if (tag == 0) { playstate = 1; } else Dispose();
            }
            else if (players.Count == 1 && playstate > 1)
            {
                EnterLobby();
            }
            else if (playstate > 1)
            {
                PlayerCompleted();
                PlayerFinished();
                PlayerReady();

                if (playstate == 2 && VoteMachine.Votes.Count >= this.players.Count)
                {
                    VoteMachine.Clock.Enabled = true;
                }
            }

            return (1);
        }

        /// <summary>
        /// Changes the character of this player and notifies everyone.
        /// </summary>
        public void ChangeCharacter(TcpClientSocket who, byte toWhat)
        {
            if (playstate > 2) return;

            who.Character = toWhat;

            var Write = who.WriteBuffer;
            Write.WriteUByte(Code.ChangeCharacter);
            Write.WriteUByte(who.Slot);
            Write.WriteUByte(who.Character);

            foreach (TcpClientSocket Player in players)
            {
                Write.SendTcp(Player.DataStream);
            }
        }

        /// <summary>
        /// Changes the team of this player and notifies everyone.
        /// </summary>
        public void ChangeTeam(TcpClientSocket who, byte toWhat)
        {
            if (playstate != 1 || !this.teams) return;

            who.Team = toWhat;

            var Write = who.WriteBuffer;
            Write.WriteUByte(Code.ChangeTeam);
            Write.WriteUByte(who.Slot);
            Write.WriteUByte(who.Team);

            foreach (TcpClientSocket Player in players)
            {
                Write.SendTcp(Player.DataStream);
            }
        }

        /// <summary>
        /// Sends every player's information, including Name, Icon, XP, ID, Char and Team.
        /// </summary>
        /// <param name="ToWhom">To who shall we send it to?</param>
        public void SendPlayerList(TcpClientSocket ToWhom)
        {
            var Write = ToWhom.WriteBuffer;
            Write.Clear();
            Write.WriteUByte(ToWhom.HeaderId);
            Write.WriteUByte(Code.RequestPlayerList);
            foreach (TcpClientSocket Player in players)
            {
                Write.WriteBool(true);
                //A player has left. Here's his place, but you might have to request the list again.
                Write.WriteUByte(Player.Tag);
                Write.WriteUByte(Player.Icon);
                Write.WriteUInt(Player.XP);
                Write.WriteUByte(Player.Character);
                Write.WriteUByte(Player.Team);
                Write.WriteDouble(Player.ID);
                Write.WriteString(Player.Name);
            }
            Write.WriteBool(false);
            Write.SendTcp(ToWhom.DataStream);
        }

        /// <summary>
        /// For player rooms, after each battle we return to the lobby.
        /// </summary>
        public void EnterLobby()
        {
            playstate = 1;
            if (VoteMachine != null)
                VoteMachine = null;

            //Tell the players that we're ready to begin!
            foreach (TcpClientSocket Player in players)
            {
                var Write = Player.WriteBuffer;
                Write.Clear();
                Write.WriteUByte(Player.HeaderId);
                Write.WriteUByte(Code.RoomState);
                Write.WriteUByte(playstate);
                Write.SendTcp(Player.DataStream);
            }

            CmdSystem.AddLog("Room '"+this.name+"' has entered its Lobby.");

            MainServer.Event.RoomExit(this);
        }

        public void EnterVoteRoom()
        {
            byte failcode = 0;

            //First we must do some checks, such as if the player is alone, or teams are all the same.
            if (this.teams)
            {
                var d = false;
                var pl = players.FirstOrDefault();
                foreach (TcpClientSocket Player in players)
                {
                    if (Player.Team != pl.Team)
                    { d = true; break; }
                }

                //If all the teams are the same...
                if (!d) failcode = 2; 
            }

            if (players.Count == 1)
                failcode = 1;
            //Now we can tell the players that we're ready to begin!
            foreach (TcpClientSocket Player in players)
            {
                var Write = Player.WriteBuffer;
                Write.Clear();
                Write.WriteUByte(Player.HeaderId);
                Write.WriteUByte(Code.RoomState);
                Write.WriteUByte(2);
                Write.WriteUByte(failcode);
                Write.SendTcp(Player.DataStream);

                //Also send this custom item thing. :)
                Write.Clear();
                Write.WriteUByte(Player.HeaderId);
                Write.WriteUByte(Code.ItemConfig);
                Write.WriteBool(is_custom_items);
                Write.WriteUByte(citm_1);
                Write.WriteUByte(citm_2);
                Write.WriteUByte(citm_3);
                Write.WriteUByte(citm_4);
                Write.WriteUByte(citm_5);
                Write.SendTcp(Player.DataStream);

                Player.Slot = (byte)(this.players.IndexOf(Player) + 1);
            }
            if (failcode != 0) return;

            VoteMachine = new Voter(this);
            playstate = 2;

            CmdSystem.AddLog("Room '" + this.name + "' (ID:" + this.id + ") has started voting.");

            MainServer.Event.RoomVoting(this);
        }

        /// <summary>
        /// Adds the vote to the VoteMachine.
        /// </summary>
        /// <param name="stage_id">What stage did he or she vote?</param>
        /// <param name="who">Who sent the vote?</param>
        public void AcceptVote(TcpClientSocket who, byte stage_id)
        {
            if (playstate != 2) return;

            VoteMachine.Votes.Add(stage_id);

            if (VoteMachine.Votes.Count == this.players.Count)
            {
                VoteMachine.Clock.Enabled = true;
            }

            //Tell the players that this person voted and what they chose.
            foreach (TcpClientSocket Player in players)
            {
                var Write = Player.WriteBuffer;
                Write.Clear();
                Write.WriteUByte(Player.HeaderId);
                Write.WriteUByte(Code.Vote);
                Write.WriteUByte(who.Slot);
                Write.WriteUByte(stage_id);
                Write.SendTcp(Player.DataStream);
            }
        }

        /// <summary>
        /// Lets all players know that this is the stage we're going to.
        /// </summary>
        public void NotifyPlayersOfStage(byte stage_id)
        {
            if (playstate != 2) return;

            foreach (TcpClientSocket Player in players)
            {
                var Write = Player.WriteBuffer;
                Write.Clear();
                Write.WriteUByte(Player.HeaderId);
                Write.WriteUByte(Code.RoomState);
                Write.WriteUByte(3);
                //3 means that we need a stage ID.
                    Write.WriteUByte(stage_id);
                Write.SendTcp(Player.DataStream);

                //Clear the last battle's records.
                Player.Record.Clear();
            }

            VoteMachine = null;
            playstate = 3;
            ReadyCount = 0;
            //At this point, we wait for all players to finish loading.
            CmdSystem.AddLog("Room '" + this.name + "' (ID:" + this.id + ") has entered the battlefield.");

            MainServer.Event.RoomStarted(this);
        }

        /// <summary>
        /// To be called when a player says they're ready for battle.
        /// </summary>
        public void PlayerReady()
        {
            if (playstate != 3) return;

            ReadyCount++;
            if (ReadyCount >= players.Count)
            {
                playstate = 4;
                //Tell the players that we're ready to begin!
                foreach (TcpClientSocket Player in players)
                {
                    var Write = Player.WriteBuffer;
                    Write.Clear();
                    Write.WriteUByte(Player.HeaderId);
                    Write.WriteUByte(Code.BattleReady);
                    Write.SendTcp(Player.DataStream);
                }
                ReadyCount = 0;

                MainServer.Event.RoomReady(this);
            }
        }

        /// <summary>
        /// To be called when a player's battle is finished.
        /// </summary>
        public void PlayerFinished()
        {
            if (playstate != 4) return;
                
            ReadyCount++;
            if (ReadyCount >= players.Count)
            {
                //Tell the players that we're ready to begin!
                foreach (TcpClientSocket Player in players)
                {
                    var Write = Player.WriteBuffer;
                    Write.Clear();
                    Write.WriteUByte(Player.HeaderId);
                    Write.WriteUByte(Code.BattleEnd);
                    Write.SendTcp(Player.DataStream);
                }
                ReadyCount = 0;
                playstate = 5;

                MainServer.Event.RoomFinished(this);
            }
        }

        /// <summary>
        /// To be called when a player has pressed the Next Battle button.
        /// </summary>
        public void PlayerCompleted()
        {
            if (playstate != 5) return;

            ReadyCount++;
            if (ReadyCount >= players.Count)
            {
                CmdSystem.AddLog("Room '" + this.name + "' (ID:" + this.id + ") has finished its battle.");

                if (tag == 0)
                {
                    SwitchThingsUp(); 
                    EnterVoteRoom();
                    
                    foreach (TcpClientSocket Player in players)
                    {
                        SendMyInfo(Player);
                    }
                }
                else
                    EnterLobby();
                ReadyCount = 0;
            }
        }

        /// <summary>
        /// After battle, each player submits their stats. This method lets everyone else know.
        /// </summary>
        public void DistributeStats(TcpClientSocket who)
        {
            if (playstate != 5) return;

            var Write = who.WriteBuffer;
            Write.WriteUShort(who.Record.HitsGiven);
            Write.WriteUShort(who.Record.Kills); 
            Write.WriteUShort(who.Record.Items);
            Write.WriteUShort(who.Record.HitsTaken);
            Write.WriteUShort(who.Record.Deaths);
            Write.WriteUShort(who.Record.CoinsGained);
            Write.WriteUShort(who.Record.CoinsLost);

            foreach (TcpClientSocket Player in players)
            {
                Write.SendTcp(Player.DataStream);
            }
        }

        public void SendMyInfo(TcpClientSocket ToWho)
        {
            var Write = ToWho.WriteBuffer;
            Write.Clear();
            Write.WriteUByte(ToWho.HeaderId);
            Write.WriteUByte(Code.RequestRoomInformation);

            Write.WriteUInt(id);
            Write.WriteString(name);
            Write.WriteString(host_name);
            Write.WriteSByte(tag);
            Write.WriteUByte((byte)players.Count);
            Write.WriteUByte(maxplayers);
            Write.WriteUByte(mode);
            Write.WriteBool(teams);
            Write.WriteUByte(playstate);
            Write.WriteUByte(maxteams);
            Write.WriteUByte(minutes);
            
            Write.SendTcp(ToWho.DataStream);
        }

        /// <summary>
        /// Kicks all of the players out of the room, either due to only 1 player remaining, or a room error.
        /// </summary>
        public void Dispose()
        {
            foreach (TcpClientSocket i in players)
            {
                var Write = i.WriteBuffer;
                Write.Clear();
                Write.WriteUByte(i.HeaderId);
                Write.WriteUByte(Code.SessionLeave);
                Write.WriteUByte(2);
                Write.SendTcp(i.DataStream);

                i.CurrentRoom = null;
                i.Slot = 0;
            }

            MainServer.RoomList.Remove(this);

            CmdSystem.AddLog("Room " + this.id + " disposed.");
        }

        /// <summary>
        /// Returns the player that created the room.
        /// </summary>
        public TcpClientSocket GetHost()
        {
            return (host);
        }

        /// <summary>
        /// This special method done by Server rooms only changes up the modes after each battle.
        /// </summary>
        private void SwitchThingsUp()
        {
            this.mode++;
            if (this.mode == 3) this.mode++;
            if (this.mode == 5) this.mode = 1;

            //Normally not recommended since rooms should either stick to solo or stick to teamed.
            //this.teams = !teams;
            if (teams) Teamify();

            CmdSystem.AddLog("Room '" + this.name + "' (ID:" + this.id + ") automatic team swap complete.");
        }

        /// <summary>
        /// Switches everyone's teams for the best balance.
        /// This also forces two-team play, regardless of the setting.
        /// </summary>
        public void Teamify()
        {
            if (!teams || players.Count == 0) return;

            maxteams = 2;

            if (players.Count == 2)
            {
                ChangeTeam(players.ElementAtOrDefault(0), 0);
                ChangeTeam(players.ElementAtOrDefault(1), 1);
            }
            else
            {
                byte startingteam = (byte)new Random().Next(0, 2);
                uint total_xp = 0;
                uint blue_xp = 0;
                TcpClientSocket member;

                List<TcpClientSocket> TeamQueue = new List<TcpClientSocket>(players);
                //Find the average xp
                foreach (TcpClientSocket Player in players)
                {
                    total_xp += Player.XP;
                    Player.Team = startingteam;
                }
                //All players to the opposite team until they seem balanced.
                do
                {
                    member = TeamQueue.ElementAtOrDefault(new Random().Next(0, players.Count));
                    //Change the member in the corresponding lists's team.
                    ChangeTeam(players.ElementAtOrDefault(TeamQueue.IndexOf(member)), member.Team);

                    member.Team = (byte)(1 - startingteam);
                    blue_xp += member.XP;
                    total_xp -= member.XP;
                }
                while (blue_xp >= total_xp);
            }
        }
    }

    /// <summary>
    /// The Voter Class holds all of the player votes in a room and then chooses one vote for them to play in for that round.
    /// </summary>
    public class Voter
    {
        internal List<Byte> Votes;
        internal Room HostRoom;
        internal Timer Clock;

        /// <summary>
        /// The Voter Class holds all of the player votes in a room and then chooses one vote for them to play in for that round.
        /// </summary>
        /// <param name="parent">The Room that creates this instance.</param>
        public Voter(Room parent)
        {
            Votes = new List<Byte>(parent.maxplayers);
            HostRoom = parent;

            Clock = new Timer(3000);
            Clock.Elapsed += PickStage;
        }

        /// <summary>
        /// After three seconds, this event is called to choose one stage out of the list of accepted votes.
        /// </summary>
        internal void PickStage(object sender, ElapsedEventArgs e)
        {
            //Pick a stage.
            byte stg = Votes.ElementAtOrDefault(new Random().Next(1,Votes.Count));
            //Tell the host that this is the stage.
            HostRoom.NotifyPlayersOfStage(stg);
            Clock.Enabled = false;
        }
    }

    public class Record
    {
        public ushort HitsGiven = 0;
        public ushort Kills = 0;
        public ushort Items = 0;
        public ushort HitsTaken = 0;
        public ushort Deaths = 0;
        public ushort CoinsGained = 0;
        public ushort CoinsLost = 0;

        public Record()
        {

        }

        public void Clear()
        {
            this.HitsGiven = 0;
            this.Kills = 0;
            this.Items = 0;
            this.HitsTaken = 0;
            this.Deaths = 0;
            this.CoinsGained = 0;
            this.CoinsLost = 0;
        }
    }
}