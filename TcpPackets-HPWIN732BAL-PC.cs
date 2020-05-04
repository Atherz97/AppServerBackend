using System.Net.Sockets;
using System.Collections.Generic;
using System.Net;
using BBLegacyServer;
using System;

namespace BBLegacyServer {
	/// <summary>
	/// Simple static class to separate TCP packet reading from the low-level TCP server system.
	/// </summary>
	public static class TcpPackets {
		public static void TcpDisconnect( TcpClientSocket myClient ) {
			// Notify the client that the server has disconnected it from the server.
            myClient.WriteBuffer.SetPeek(0);
            myClient.WriteBuffer.Writeu8(Code.ConnectionEnd);
            myClient.WriteBuffer.WriteStr(MainServer.KickMessage);
            myClient.WriteBuffer.SendTcp(myClient.DataStream);
		}

		/// <summary>
		/// Send packets to a client or process data for a client independantly of reading/receiving data.
		/// </summary>
        /// <param name="myClient">Client socket class that is being handled.</param>
		// <param name="myClientList">List of clients from the server.</param>
		/// <param name="myServer">Server's handle(listener) socket class.</param>
		// <param name="myServerSocketId">Socket ID of the server.</param>
		public static void TcpPacketSend( TcpClientSocket myClient , TcpListenerSocket myServer , Int16 WhatToDo) {
			// Send messages to a client without receiving data.
			
		}

        /// <summary>
        /// Send packets to a client or process data for a client independantly of reading/receiving data.
        /// </summary>
        /// <param name="myClient">Client socket class that is being handled.</param>
        /// <param name="myServer">Server's handle(listener) socket class.</param>
        public static void TcpPacketSend(TcpClientSocket myClient, TcpListenerSocket myServer)
        {
            // Send messages to a client without receiving data.

        }

		/// <summary>
		/// Read and process packets received from the client.
		/// </summary>
		/// <param name="myClient">Client socket class that is being handled.</param>
		// <param name="myClientList">List of clients from the server.</param>
		/// <param name="myServer">Server's handle(listener) socket class.</param>
		// <param name="myServerSocketId">Socket ID of the server.</param>
		public static void TcpPacketRead( TcpClientSocket myClient , TcpListenerSocket myServer ) {

			byte myCheck = myClient.ReadBuffer.StartRead( 0 );

            var WriteBuff = myClient.WriteBuffer;
            var ReadBuff = myClient.ReadBuffer;
            var File = MainServer.SettingsFile;
            var ID = myClient.PlayerID.ToString();

			// Check for packets by searching for packet headers.
			while( myCheck == myClient.HeaderId ) 
            {
				byte myPacketId = ReadBuff.Readu8();
                
				switch (myPacketId) 
                {
                    /*  To READ from the buffer...
                        float myFloat = myClient.ReadBuffer.Readf32();
                        int myInteger = myClient.ReadBuffer.Readu32();
                        string myString = myClient.ReadBuffer.ReadStr();
                     
                        To WRITE to the buffer and send...
                        myClient.WriteBuffer.SetPeek( 0 );
                        myClient.WriteBuffer.Writeu8( TcpNetwork.TcpPacketHeader );
                        myClient.WriteBuffer.Writeu8( 254 );
                        myClient.WriteBuffer.SendTcp( myClient.DataStream );*/

                    //They sent this to see if the connection works. Echo their packet back.
                    case (Code.ConnectionBegin):
                    {
                        //Assigns the given PlayerID to this specific client. Also the name should've given their name.
                        myClient.PlayerID = ReadBuff.Readf64();
                        myClient.PlayerName = ReadBuff.ReadStr();
                        //Tells them that they connected.
                        WriteBuff.SetPeek(0);
                        WriteBuff.Writeu8(myClient.HeaderId);
                        WriteBuff.Writeu8(Code.ConnectionBegin);
                        WriteBuff.WriteStr(MainServer.WelcomeMessage);
                        CmdSystem.AddLog("BBLegacy Client Connected ("+myClient.PlayerName+" ["+myClient.PlayerID.ToString()+"])");
                        WriteBuff.SendTcp(myClient.DataStream);
                        //Update their online status.
                        File.SetValue(myClient.PlayerID.ToString(), "MenuStatus", Code.Status_AOnline);
                        File.SetValue(myClient.PlayerID.ToString(), "Name", myClient.PlayerName);
                        break;
                    }

                    case (Code.ConnectionEnd):
                    {
                        myClient.TcpClientClose();
                        CmdSystem.AddLog("Client " + myClient.SocketId.ToString() + " Requested Disconnection -- Granted.");
                        break;
                    }

                    case (Code.SessionCreate):
                    {
                        //You're actually creating a session :D
                        //It's just like joining one, but you make it, then join.
                        byte RoomId = 0, Failed = 0;
                        var RoomName = ReadBuff.ReadStr();
                        var RoomHostName = myClient.PlayerName;
                        //Can't make one while inside one.
                        if (myClient.isInSession)
                        {
                            Failed = 1;
                        }
                        else
                        for (byte i = 0; i<255; i++)
                        {
                            if (!MainServer.SessionNumberList.Contains(i))
                            {
                                //Found a free room.
                                RoomId = i;
                                MainServer.SessionNumberList.Add(i);
                                break;
                            }
                            if (i == 255)
                                Failed = 2; //WHAT. there's more than 250 rooms?
                        }

                        WriteBuff.SetPeek(0);
                        WriteBuff.Writeu8(myClient.HeaderId);
                        WriteBuff.Writeu8(Code.SessionCreate);
                        WriteBuff.Writeu8(Failed);
                        WriteBuff.SendTcp(myClient.DataStream);

                        break;
                    }

                    case (Code.SessionJoin):
                    {
                        //Request to Join a Room. Which room? This room!
                        var id = ReadBuff.Readu8();
                        Session RoomId = MainServer.SessionList.Find(e => e.GetID() == id);
                        //Cannot join a room while in another.
                        if (myClient.isInSession)
                        {
                            WriteBuff.SetPeek(0);
                            WriteBuff.Writeu8(myClient.HeaderId);
                            WriteBuff.Writeu8(Code.SessionJoin);
                            WriteBuff.Writeu8(0);
                            WriteBuff.SendTcp(myClient.DataStream);
                            break;
                        }
                        
                        //Success, put them in the room...
                        myClient.isInSession = true;
                        myClient.CurrentSession = RoomId;

                        //...and then tell the player.
                        WriteBuff.SetPeek(0);
                        WriteBuff.Writeu8(myClient.HeaderId);
                        WriteBuff.Writeu8(Code.SessionJoin);
                        WriteBuff.Writeu8(1);
                        WriteBuff.SendTcp(myClient.DataStream);

                        break;
                    }

                    case (Code.SessionLeave):
                    {
                        //Request to leave the room. Which room (To tell the players in that room he left)?
                        var RoomId = ReadBuff.Readu8();

                        //Cannot leave a room when you're not in one.
                        if (!myClient.isInSession)
                        {
                            WriteBuff.SetPeek(0);
                            WriteBuff.Writeu8(myClient.HeaderId);
                            WriteBuff.Writeu8(Code.SessionLeave);
                            WriteBuff.Writeu8(0);
                            WriteBuff.SendTcp(myClient.DataStream);
                            break;
                        }

                        //Success, take them out the room...
                        myClient.isInSession = false;
                        myClient.CurrentSession = null;

                        //...and then tell the player.
                        WriteBuff.SetPeek(0);
                        WriteBuff.Writeu8(myClient.HeaderId);
                        WriteBuff.Writeu8(Code.SessionJoin);
                        WriteBuff.Writeu8(1);
                        WriteBuff.SendTcp(myClient.DataStream);

                        //As well as the other players in that room.
                        Session EveryoneElse = MainServer.SessionList.Find(e => e.GetID() == RoomId);
                        //Loop through all connected clients to see which is in the room.
                        {
                            foreach (KeyValuePair<long, TcpClientSocket> ClientSocket in TcpListenerSocket.ClientList)
                            {
                                if (ClientSocket.Value.CurrentSession == EveryoneElse)
                                {
                                    WriteBuff.SetPeek(0);
                                    WriteBuff.Writeu8(ClientSocket.Value.HeaderId);
                                    WriteBuff.Writeu8(Code.PlayerLeave);
                                    WriteBuff.Writeu8(1);
                                    WriteBuff.SendTcp(ClientSocket.Value.DataStream);
                                }
                            }
                        }
                        //Then tell each of them that the player left.

                        break;
                    }

					default: break;
				}

				// Back-up the read/peek position of the buffer and check for a secondary/merged packet.
				int myHeaderId = myClient.ReadBuffer.EndRead( false );
				myCheck = ( byte ) ( ( myHeaderId != -1 ) ? myHeaderId : ~myHeaderId );
			}
		}
        
	}
}
