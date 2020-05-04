using System;

namespace BBLegacyServer
{
    /// <summary>
    /// Simple static class to separate TCP packet reading from the low-level TCP server system.
    /// </summary>
    public static class TcpPackets {
		public static void TcpDisconnect( TcpClientSocket myClient ) {
			// Notify the client that the server has disconnected it from the server.
            myClient.WriteBuffer.Clear();
            myClient.WriteBuffer.WriteUByte(Code.ConnectionEnd);
            myClient.WriteBuffer.WriteString(MainServer.KickMessage);
            myClient.WriteBuffer.SendTcp(myClient.DataStream);
		}

		/// <summary>
		/// Send packets to a client or process data for a client independantly of reading/receiving data.
		/// </summary>
        /// <param name="myClient">Client socket class that is being handled.</param>
		/// <param name="myServer">Server's handle(listener) socket class.</param>
		public static void TcpPacketSend( TcpClientSocket myClient , TcpListenerSocket myServer ) {
			// Send messages to a client without receiving data.
			
		}

		/// <summary>
		/// Read and process packets received from the client.
		/// </summary>
		/// <param name="Player">Client socket class that is being handled.</param>
		/// <param name="myServer">Server's handle(listener) socket class.</param>
		public static void TcpPacketRead( TcpClientSocket Player , TcpListenerSocket myServer ) {

			byte myCheck = Player.ReadBuffer.StartRead( 0 );

            var buff = Player.DataStream;

            //Not all functions require a writeback, but it helps to have this ready.
            var Write = Player.WriteBuffer;
            var Read = Player.ReadBuffer;
            var File = MainServer.SettingsFile;
            var PLID = Player.ID.ToString();

			// Check for packets by searching for packet headers.
			while( myCheck == Player.HeaderId ) 
            {
                Write.Clear();
                Write.WriteUByte(Player.HeaderId);

				byte myPacketId = Read.ReadUByte();
                
				switch (myPacketId) 
                {
                    /*  To READ from the buffer...
                        float myFloat = Player.ReadBuffer.Readf32();
                        int myInteger = Player.ReadBuffer.Readu32();
                        string myString = Player.ReadBuffer.ReadStr();
                     
                        To WRITE to the buffer and send...
                        Player.WriteBuffer.SetPeek( 0 );
                        Player.WriteBuffer.Writeu8( TcpNetwork.TcpPacketHeader );
                        Player.WriteBuffer.Writeu8( 254 );
                        Player.WriteBuffer.SendTcp( Player.DataStream );*/

                    //They sent this to see if the connection works. Echo their packet back.
                    case Code.ConnectionBegin:
                    {
                        //Assigns the given PlayerID to this specific client. Also the name should've given their name.
                        Player.ID = Read.ReadDouble();
                        Player.Name = Read.ReadString();
                        Player.Icon = Read.ReadUByte();
                        Player.XP = Read.ReadUInt();
                        //Tells them that they connected.
                        Write.WriteUByte(Code.ConnectionBegin);
                        Write.WriteString(MainServer.WelcomeMessage);
                        Write.WriteBool(MainServer.CanCreateRooms);
                        CmdSystem.AddLog(Player.Name+" has connected (ID:"+Player.ID.ToString()+")");
                        Write.SendTcp(Player.DataStream);
                        //If they're admins...
                        if (AdminList.isAdmin(Player.ID)) Player.Tag = 2;

                        //If they're not on the list, put them on the list.
                        //This also means that they're new players, 1st time joiners!
                        var b = false;

                        foreach (PlayerListItem i in MainServer.PlayerList)
                            if (i.ID == Player.ID)
                            {
                                b = true;
                                Player.Tag = i.Tag;
                                i.Link(Player);
                            }

                        if (!b) Player.ListSlot = new PlayerListItem();

                        Player.ListSlot.Link(Player);
                        Player.ListSlot.GoOnline();

                        MainServer.Event.PlayerConnected(Player);
                        break;
                    }

                    case Code.ConnectionEnd:
                    {
                        Player.UserImposedDisconnection = true;
                        Player.Connected = false;
                        CmdSystem.AddLog(Player.Name + " has left");
                        if (Player.ListSlot != null)
                            Player.ListSlot.GoOffline();

                        MainServer.Event.PlayerDisconnected(Player);
                        break;
                    }

                    // == ADD MENU STATUS ==

                    //They want to join this room. Returns ID upon success, or a negative number upon failure.
                    case Code.SessionJoin:
                    {
                        var id = Read.ReadUInt();

                        uint success = 4000000004;
                        byte pnum = 0;
                        String nm = " ";
                        foreach (Room i in MainServer.RoomList)
                        {
                            if (i.id == id)
                            {
                                success = i.Join(Player);
                                pnum = (byte)i.players.Count;
                                nm = i.name;
                                break;
                            }
                        }
                        Write.Clear();
                        Write.WriteUByte(Player.HeaderId);
                        Write.WriteUByte(Code.SessionJoin);
                        Write.WriteUInt(success);
                        Write.WriteUByte(pnum);
                        byte plst = 1;
                        if (Player.CurrentRoom != null)
                            plst = Player.CurrentRoom.playstate;
                        Write.WriteUByte(plst);
                        Write.WriteString(nm);
                        Write.SendTcp(buff);
                        
                        if (success < 4000000000)
                        {
                            Player.ListSlot.GoPlayingOnline();
                        }
                        //Now that they know they joined, we can now auto-start the room if needed.
                        foreach (Room i in MainServer.RoomList)
                        {
                            if (i.id == id)
                            {
                                if (i.tag == 0 && i.playstate <= 2)
                                {
                                    if (i.players.Count > 1 && i.playstate == 1) { i.EnterVoteRoom(); }
                                    i.Teamify();
                                }
                                        
                            }
                        }

                            MainServer.Event.PlayerJoinedRoom(Player,Player.CurrentRoom);
                        break;
                    }

                    case Code.SessionLeave:
                    {
                        byte success = 1;
                        Room rm = Player.CurrentRoom;

                        if (Player.CurrentRoom == null) 
                            success = 0; 
                        else
                            success = Player.CurrentRoom.Remove(Player);
                        Write.WriteUByte(Code.SessionLeave);
                        Write.WriteUByte(success);
                        Write.SendTcp(Player.DataStream);

                        if (success > 0)
                        {
                            Player.ListSlot.GoOnline();
                            MainServer.Event.PlayerLeftRoom(Player, rm);
                        }

                            break;
                    }

                    //Creates a session, returning room ID if the operation was successful.
                    case Code.SessionCreate:
                    {
                        //Host, Name, Mode, Team, Team Number, Minutes, Max Players
                        var rs1 = Read.ReadString();
                        var ub2 = Read.ReadUByte();
                        var bo3 = Read.ReadBool();
                        var ub4 = Read.ReadUByte();
                        var ub5 = Read.ReadUByte();
                        var ub6 = Read.ReadUByte();

                        var bo7 = Read.ReadBool();
                        var ub8 = Read.ReadUByte();
                        var ub9 = Read.ReadUByte();
                        var ub10 = Read.ReadUByte();
                        var ub11 = Read.ReadUByte();
                        var ub12 = Read.ReadUByte();

                        Write.WriteUByte(Code.SessionCreate);

                        if (MainServer.CanCreateRooms && Player.CurrentRoom == null)
                        {
                            Room rm = new Room(Player, rs1, ub2, bo3, ub4, ub5, ub6);
                            Write.WriteBool(true);
                            Write.WriteUInt(rm.id);
                            Write.WriteString(rm.name);

                            rm.is_custom_items = bo7;
                            rm.citm_1 = ub8;
                            rm.citm_2 = ub9;
                            rm.citm_3 = ub10;
                            rm.citm_4 = ub11;
                            rm.citm_5 = ub12;
                            //So for special admins, we should highlight their room by making it a special tagged room.
                            if (AdminList.isAdmin(Player.ID)) rm.tag = 2;

                            Player.ListSlot.GoPlayingOnline();
                        }
                        else
                        {
                            Write.WriteBool(false);
                        }

                        Write.SendTcp(buff);

                            MainServer.Event.PlayerCreatedRoom(Player, Player.CurrentRoom);
                            break;
                    }

                    //Not sent by client.
                    case Code.UpdatePlayers: break;

                    //Host-invoked room starting.
                    case Code.StartRoom:
                    {
                        var rm = Player.CurrentRoom;

                        if (rm == null || rm.tag == 0 || rm.players.IndexOf(Player) != 0 || rm.playstate != 1) break;

                        rm.EnterVoteRoom();
                        break;
                    }

                    //A player changes his or her character in the lobby. Let the room tell the others.
                    case Code.ChangeCharacter:
                    {
                        var c = Read.ReadUByte();

                        if (Player.CurrentRoom == null) break;

                        Player.CurrentRoom.ChangeCharacter(Player, c);
                            MainServer.Event.PlayerChangedChar(Player, Player.Character);
                            break;
                    }

                    //A player changes his or her team. Let the room tell the others.
                    case Code.ChangeTeam:
                    {
                        var t = Read.ReadUByte();

                        if (Player.CurrentRoom == null) break;

                        Player.CurrentRoom.ChangeTeam(Player,t);
                            MainServer.Event.PlayerChangedTeam(Player, Player.Team);
                            break;
                    }

                    //The host changes these battle settings. Let their room update and then tell everyone.
                    case Code.ChangeBattleSettings:
                    {

                        break;
                    }

                    //Not sent by client.
                    case Code.RoomState: break;

                    //The player requests the information of another player with this ID.
                    case Code.PlayerInfo:
                    {

                        break;
                    }

                    //Simple packet to say that we're responding. Can be used for ping.
                    case Code.IAmHere:
                    {
                        Write.WriteUByte(Code.IAmHere);
                        Write.SendTcp(buff);
                        break;
                    }

                    //The player wants a list of all open rooms.
                    case Code.RequestRoomList:
                    {
                        Write.WriteUByte(Code.RequestRoomList);
                        //Send just their IDs and a 0 at the end. The game will ask later.
                        //The order will go like SERVER ROOMS; OPEN PLAYER ROOMS; CLOSED PLAYER ROOMS.
                        //The extra bool reports their size (big or small).
                        foreach (Room i in MainServer.RoomList)
                            if (i.visible && i.tag == 0) { Write.WriteUInt(i.id); Write.WriteBool(true); }

                        foreach (Room i in MainServer.RoomList)
                            if (i.visible && i.tag == 2 && i.playstate == 1) { Write.WriteUInt(i.id); Write.WriteBool(true); }

                        foreach (Room i in MainServer.RoomList)
                            if (i.visible && i.tag == 1 && i.playstate == 1) { Write.WriteUInt(i.id); Write.WriteBool(false); }

                        foreach (Room i in MainServer.RoomList)
                            if (i.visible && i.tag == 2 && i.playstate != 1) { Write.WriteUInt(i.id); Write.WriteBool(true); }

                        foreach (Room i in MainServer.RoomList)
                            if (i.visible && i.tag == 1 && i.playstate != 1) { Write.WriteUInt(i.id); Write.WriteBool(false); }
                        
                        Write.WriteUInt(0);
                        Write.SendTcp(buff);
                        break;
                    }

                    //The player wants this specific room's information.
                    case Code.RequestRoomInformation:
                    {
                        var rm = Read.ReadUInt();

                        var found = false;

                        if (MainServer.RoomList.Count == 0) break;

                        Write.WriteUByte(Code.RequestRoomInformation);

                        foreach (Room i in MainServer.RoomList)
                        {
                            if (i.id == rm)
                            {
                                Write.WriteUInt(i.id);
                                Write.WriteString(i.name);
                                Write.WriteString(i.host_name);
                                Write.WriteSByte(i.tag);
                                Write.WriteUByte((byte)i.players.Count);
                                Write.WriteUByte(i.maxplayers);
                                Write.WriteUByte(i.mode);
                                Write.WriteBool(i.teams);
                                Write.WriteUByte(i.playstate);
                                Write.WriteUByte(i.maxteams);
                                Write.WriteUByte(i.minutes);
                                found = true;
                                break;
                            }
                        }
                        if (found)
                            Write.SendTcp(buff);
                        else
                            Write.Clear();
                        
                        break;
                    }

                    //The player wants a list of all players.
                    case Code.RequestPlayerList:
                    {
                        if (Player.CurrentRoom == null) break;
                        Player.CurrentRoom.SendPlayerList(Player);
                        break;
                    }

                    //Packet echo'ing.
                    case Code.SendMe:
                    {
                        Write.WriteUByte(Code.SendMe);
                        byte DataType = Read.ReadUByte();
                        int val;
                        uint uval;
                        float valf;
                        double vald;
                        Write.WriteUByte(DataType);
                        switch (DataType)
                        {
                            case 1: val = Read.ReadUByte(); Write.WriteUByte((byte)val); CmdSystem.AddLog(Player.Name + " echo request: " + (byte)val); break;
                            case 2: val = Read.ReadSByte(); Write.WriteSByte((sbyte)val); CmdSystem.AddLog(Player.Name + " echo request: " + (sbyte)val); break;

                            case 3: val = Read.ReadUShort(); Write.WriteUShort((ushort)val); CmdSystem.AddLog(Player.Name + " echo request: " + (ushort)val); break;
                            case 4: val = Read.ReadSShort(); Write.WriteSShort((short)val); CmdSystem.AddLog(Player.Name + " echo request: " + (short)val); break;

                            case 5: uval = Read.ReadUInt(); Write.WriteUInt(uval); CmdSystem.AddLog(Player.Name + " echo request: " + uval); break;
                            case 6: val = (int)Read.ReadSInt(); Write.WriteSInt(val); CmdSystem.AddLog(Player.Name + " echo request: " + val); break;

                            case 7: valf = Read.ReadFloat(); Write.WriteFloat(valf); CmdSystem.AddLog(Player.Name + " echo request: " + valf); break;
                            case 8: vald = Read.ReadDouble(); Write.WriteDouble(vald); CmdSystem.AddLog(Player.Name + " echo request: " + vald); break;

                            default: Read.ReadUByte(); Write.WriteUByte(0); CmdSystem.AddLog(Player.Name + " blank echo request."); break;
                        }
                        Write.SendTcp(Player.DataStream);
                        break;
                    }

                    //A player sends a vote to his room.
                    case Code.Vote:
                    {
                        byte stg = Read.ReadUByte();

                        if (Player.CurrentRoom == null) break;

                        Player.CurrentRoom.AcceptVote(Player,stg);
                        break;
                    }

                    //The player has finished loading and is ready to start the battle.
                    case Code.BattleReady:
                    {
                        if (Player.CurrentRoom == null) break;
                        Player.CurrentRoom.PlayerReady();
                        break;
                    }

                    /*
                     * NOW, for the next few codes, the server is simply a relayer to all
                     * the other players. It doesn't really keep any of this data, which
                     * we should in the future so we can keep some spam/hack control.
                     */

                    //The player moved, let everyone else know.
                    case Code.Movement:
                    {
                        if (Player.CurrentRoom == null) break;
                        
                        var ss1 = Read.ReadSShort();
                        var ss2 = Read.ReadSShort();
                        var ub3 = Read.ReadUByte();

                        Write.WriteUByte(Code.Movement);
                        Write.WriteUByte(Player.Slot);
                        
                        Write.WriteSShort(ss1);
                        Write.WriteSShort(ss2);
                        Write.WriteUByte(ub3);

                        foreach (TcpClientSocket Others in Player.CurrentRoom.players)
                        {
                            if (!Others.Equals(Player))
                            Write.SendTcp(Others.DataStream);
                        }
                        break;
                    }

                    //The player stopped, let everyone else know.
                    case Code.Stopment:
                    {
                        if (Player.CurrentRoom == null) break;

                        var ss1 = Read.ReadSShort();
                        var ss2 = Read.ReadSShort();

                        Write.WriteUByte(Code.Stopment);
                        Write.WriteUByte(Player.Slot);
                        
                        Write.WriteSShort(ss1);
                        Write.WriteSShort(ss2);
                        
                        foreach (TcpClientSocket Others in Player.CurrentRoom.players)
                        {
                            if (!Others.Equals(Player))
                            Write.SendTcp(Others.DataStream);
                        }
                        break;
                    }

                    //The player used an item, let everyone else know.
                    case Code.Item:
                    {
                        if (Player.CurrentRoom == null) break;

                        var ub1 = Read.ReadUByte();
                        var bo1 = Read.ReadBool();
                        var ss2 = Read.ReadSShort();
                        var ss3 = Read.ReadSShort();
                        var ff4 = Read.ReadFloat();
                        var ff5 = Read.ReadFloat();

                        Write.WriteUByte(Code.Item);
                        Write.WriteUByte(Player.Slot);
                        
                        Write.WriteUByte(ub1);
                        Write.WriteBool(bo1);
                        Write.WriteSShort(ss2);
                        Write.WriteSShort(ss3);
                        Write.WriteFloat(ff4);
                        Write.WriteFloat(ff5);
                        
                        foreach (TcpClientSocket Others in Player.CurrentRoom.players)
                        {
                            if (!Others.Equals(Player))
                            Write.SendTcp(Others.DataStream);
                        }

                        MainServer.Event.PlayerUsedItem(Player, ub1);
                        break;
                    }

                    //The player got hit, let everyone else know.
                    case Code.Hit:
                    {
                        if (Player.CurrentRoom == null) break;

                        var ub1 = Read.ReadUByte();
                        var ss2 = Read.ReadSShort();
                        var ss3 = Read.ReadSShort();
                        var ff4 = Read.ReadFloat();
                        var ff5 = Read.ReadFloat();
                        var ff6 = Read.ReadFloat();

                        Write.WriteUByte(Code.Hit);
                        Write.WriteUByte(Player.Slot);
                        
                        Write.WriteUByte(ub1);
                        Write.WriteSShort(ss2);
                        Write.WriteSShort(ss3);
                        Write.WriteFloat(ff4);
                        Write.WriteFloat(ff5);
                        Write.WriteFloat(ff6);

                        foreach (TcpClientSocket Others in Player.CurrentRoom.players)
                        {
                            if (!Others.Equals(Player))
                            Write.SendTcp(Others.DataStream);
                        }

                        MainServer.Event.PlayerHit(Player, ub1, ff6);
                        break;
                    }

                    //The player got killed, let everyone else know.
                    case Code.Death:
                    {
                        if (Player.CurrentRoom == null) break;

                        var ss1 = Read.ReadSShort();
                        var ss2 = Read.ReadSShort();
                        var ff3 = Read.ReadFloat();
                        var ub4 = Read.ReadUByte();

                        Write.WriteUByte(Code.Death);
                        Write.WriteUByte(Player.Slot);
                        
                        Write.WriteSShort(ss1);
                        Write.WriteSShort(ss2);
                        Write.WriteFloat(ff3);
                        Write.WriteUByte(ub4);
                        
                        foreach (TcpClientSocket Others in Player.CurrentRoom.players)
                        {
                            if (!Others.Equals(Player))
                            Write.SendTcp(Others.DataStream);
                        }

                        MainServer.Event.PlayerDeath(Player, ff3);
                        break;
                    }

                    //The player got a point, let everyone else know.
                    case Code.Score:
                    {
                        if (Player.CurrentRoom == null) break;

                        var ub1 = Read.ReadUByte();
                        var ub2 = Read.ReadUByte();
                        var ss3 = Read.ReadSShort();

                        Write.WriteUByte(Code.Score);
                        
                        Write.WriteUByte(ub1);
                        Write.WriteUByte(ub2);
                        Write.WriteSShort(ss3);
                        
                        foreach (TcpClientSocket Others in Player.CurrentRoom.players)
                        {
                            if (!Others.Equals(Player))
                            Write.SendTcp(Others.DataStream);
                        }

                        MainServer.Event.PlayerScored(Player, ss3);
                        break;
                    }

                    //Absolute Score Update
                    case Code.AbsoluteScore:
                    {
                        if (Player.CurrentRoom == null) break;

                        var ss1 = Read.ReadSShort();

                        Write.WriteUByte(Code.AbsoluteScore);
                        Write.WriteUByte(Player.Slot);
                        
                        Write.WriteSShort(ss1);

                        foreach (TcpClientSocket Others in Player.CurrentRoom.players)
                        {
                            if (!Others.Equals(Player))
                            Write.SendTcp(Others.DataStream);
                        }
                        break;
                    }

                    //The player dropped coins, let everyone else know.
                    case Code.CoinDrop:
                    {
                        if (Player.CurrentRoom == null) break;

                        var ub1 = Read.ReadUByte();

                        Write.WriteUByte(Code.CoinDrop);
                        Write.WriteUByte(Player.Slot);
                        
                        Write.WriteUByte(ub1);

                        foreach (TcpClientSocket Others in Player.CurrentRoom.players)
                        {
                            if (!Others.Equals(Player))
                            Write.SendTcp(Others.DataStream);
                        }
                        break;
                    }

                    //The player dropped coins, let everyone else know.
                    case Code.ItemDrop:
                    {
                        if (Player.CurrentRoom == null) break;

                        var ss1 = Read.ReadSShort();
                        var ss2 = Read.ReadSShort();
                        var fl3 = Read.ReadFloat();
                        var ub4 = Read.ReadUByte();
                        var ub5 = Read.ReadUByte();
                        var ub6 = Read.ReadUByte();

                        Write.WriteUByte(Code.ItemDrop);
                        Write.WriteUByte(Player.Slot);

                        Write.WriteSShort(ss1);
                        Write.WriteSShort(ss2);
                        Write.WriteFloat(fl3);
                        Write.WriteUByte(ub4);
                        Write.WriteUByte(ub5);
                        Write.WriteUByte(ub6);

                        foreach (TcpClientSocket Others in Player.CurrentRoom.players)
                        {
                            if (!Others.Equals(Player))
                                Write.SendTcp(Others.DataStream);
                        }
                        break;
                    }

                    //The player's purple spawned, let everyone else know.
                    case Code.PurpleSpawn:
                    {
                        if (Player.CurrentRoom == null) break;

                        var ss1 = Read.ReadSShort();
                        var ss2 = Read.ReadSShort();

                        Write.WriteUByte(Code.PurpleSpawn);
                        Write.WriteUByte(Player.Slot);
                        
                        Write.WriteSShort(ss1);
                        Write.WriteSShort(ss2);

                        foreach (TcpClientSocket Others in Player.CurrentRoom.players)
                        {
                            if (!Others.Equals(Player))
                            {
                                Write.SendTcp(Others.DataStream);
                            }
                        }
                        break;
                    }

                    //The player has finished, here are their stats.
                    case Code.PlayerRecap:
                    {
                        if (Player.Record == null || Player.CurrentRoom == null) break;

                        Player.Record.HitsGiven = Read.ReadUShort();
                        Player.Record.Kills = Read.ReadUShort();
                        Player.Record.Items = Read.ReadUShort();
                        Player.Record.HitsTaken = Read.ReadUShort();
                        Player.Record.Deaths = Read.ReadUShort();
                        Player.Record.CoinsGained = Read.ReadUShort();
                        Player.Record.CoinsLost = Read.ReadUShort();

                        Write.WriteUByte(Code.PlayerRecap);
                        Write.WriteUByte(Player.Slot);
                        Player.CurrentRoom.DistributeStats(Player);
                        break;
                    }

                    //The player has to send their statistics for proper attack, defense, etc.
                    case Code.PlayerStats:
                    {
                        if (Player.CurrentRoom == null) break;

                        var ub1 = Read.ReadUByte();
                        var ub2 = Read.ReadUByte();
                        var ub3 = Read.ReadUByte();
                        var ub4 = Read.ReadUByte();

                        Write.WriteUByte(Code.PlayerStats);
                        Write.WriteUByte(Player.Slot);

                        Write.WriteUByte(ub1);
                        Write.WriteUByte(ub2);
                        Write.WriteUByte(ub3);
                        Write.WriteUByte(ub4);

                        foreach (TcpClientSocket Others in Player.CurrentRoom.players)
                        {
                            if (!Others.Equals(Player))
                                Write.SendTcp(Others.DataStream);
                        }
                        break;
                    }

                    //They sent a Battle Complete packet and are now waiting for everyone else to finish.
                    case Code.BattleEnd:
                    {
                        if (Player.CurrentRoom == null) break;

                        Player.CurrentRoom.PlayerFinished();
                        break;
                    }

                    //They pressed the Next Battle button and are waiting for everyone else to do the same.
                    case Code.BattleComplete:
                    {
                        if (Player.CurrentRoom == null) break;

                        Player.CurrentRoom.PlayerCompleted();
                        break;
                    }

                    //They asked for an updated Server Message pack.
                    case Code.ServerMessages:
                    {
                        Write.WriteUByte(Code.ServerMessages);
                        Write.WriteString(MainServer.GoodbyeMessage);
                        Write.WriteString(MainServer.DCMessage);
                        Write.WriteString(MainServer.KickMessage);
                        Write.WriteString(MainServer.InfoMessage);
                        Write.SendTcp(Player.DataStream);
                        break;
                    }

                    //Update their XP. Simple as that.
                    case Code.UpdateXP:
                    {
                        Player.XP = Read.ReadUInt();
                        break;
                    }

                    //Yes or no?
                    case Code.CanICreateRoomsYet:
                    {
                        Write.WriteUByte(Code.CanICreateRoomsYet);
                        Write.WriteBool(MainServer.CanCreateRooms);
                        Write.SendTcp(Player.DataStream);
                        break;
                    }

                    //
                    case Code.PlayerList:
                    {
                        Write.WriteUByte(Code.PlayerList);
                        
                        foreach (PlayerListItem i in MainServer.PlayerList)
                        if (i.Status == Code.Status_AOnline)
                        { Write.WriteBool(true); Write.WriteDouble(i.ID); Write.WriteUByte(i.Tag); Write.WriteUByte(i.Status); Write.WriteString(i.Name); }
                        
                        foreach (PlayerListItem i in MainServer.PlayerList)
                        if (i.Status == Code.Status_POnline)
                        { Write.WriteBool(true); Write.WriteDouble(i.ID); Write.WriteUByte(i.Tag); Write.WriteUByte(i.Status); Write.WriteString(i.Name); }

                        foreach (PlayerListItem i in MainServer.PlayerList)
                        if (i.Status == Code.Status_Offline)
                        { Write.WriteBool(true); Write.WriteDouble(i.ID); Write.WriteUByte(i.Tag); Write.WriteUByte(i.Status); Write.WriteString(i.Name); }

                        Write.WriteBool(false);
                        Write.SendTcp(Player.DataStream);
                        break;
                    }

                    case Code.PL_Single:
                    {
                        var id = Read.ReadDouble();

                        foreach (PlayerListItem i in MainServer.PlayerList)
                        if (i.ID == id)
                        {
                            Write.WriteUByte(Code.PL_Single);
                            Write.WriteDouble(id);
                            Write.WriteUByte(i.Status);
                            Write.SendTcp(Player.DataStream);
                        }
                    }
                    break;

                    //The player dropped coins, let everyone else know.
                    case Code.ItemInvoke:
                        {
                            if (Player.CurrentRoom == null) break;

                            var ub1 = Read.ReadUByte();
                            var ss2 = Read.ReadSShort();
                            var ss3 = Read.ReadSShort();

                            Write.WriteUByte(Code.ItemInvoke);
                            Write.WriteUByte(Player.Slot);

                            Write.WriteUByte(ub1);
                            Write.WriteSShort(ss2);
                            Write.WriteSShort(ss3);

                            foreach (TcpClientSocket Others in Player.CurrentRoom.players)
                            {
                                if (!Others.Equals(Player))
                                    Write.SendTcp(Others.DataStream);
                            }
                            break;
                        }

                    case Code.Command:
                        var s = Read.ReadString();
                        Console.WriteLine("USER INVOKED COMMAND:");
                        Console.WriteLine("    "+s);
                        Console.WriteLine("    by "+Player.Name);
                        var done = true;
                        if (Player.Tag != 2)
                        {
                            done = false;
                            CmdSystem.AddLog("User missing permissions, access denied");
                        }   
                        else
                        {
                            CmdSystem.RunCmd(s);
                            done = CmdSystem.LastCommandSuccessful;
                        }
                        Write.WriteUByte(Code.Command);
                        Write.WriteBool(done);
                        Write.WriteString(CmdSystem.LastLog);
                        Write.SendTcp(Player.DataStream);
                    break;

                    default: break;
				}

				// Back-up the read/peek position of the buffer and check for a secondary/merged packet.
				int myHeaderId = Player.ReadBuffer.EndRead( false );
				myCheck = ( byte ) ( ( myHeaderId != -1 ) ? myHeaderId : ~myHeaderId );
			}
		}

        private delegate void TcpBroadcast();
        
	}
}
