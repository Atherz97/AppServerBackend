using System;
using System.Threading;
using System.Net.Sockets;
using System.Collections.Generic;
using System.Net;

namespace BBLegacyServer
{
    /// <summary>
    /// Base class that handles a TCP Server(Listener) with client connection support.
    /// </summary>
    public class TcpListenerSocket {
		public TcpListener ServerHandle;								// TCP Listener(Server) Socket Class(not socket ID).
		public static Dictionary<long,TcpClientSocket> ClientList;	// Hashmap of socket IDs and their corrosponding TcpClients.
		public IPEndPoint AddressInfo;								// IP Address and Port info that the TCP Listener(Server) is hosted on.
		public int BufferReadSize;										// Read buffer size(For Reading Received Data).
		public int BufferWriteSize;									// Write buffer size(for writing and sending data).
		public int BufferAlignment;									// Alignment size of the Read and Write buffers.
		public long SocketId = -1;										// Socket ID of the TCP Socket(not Tcplistener class).
		public bool Status = false;									// Online/Offline status of the socket.
		public byte PacketHeader = 0;									// Packet Header ID For every packet sent/received.
		public int MaxClients = 0;										// Maximum number of clients that can connect to the server.
		/// <summary>
		/// Destructor for the TCP Listener(Server) Socket class :: Handles freeing up any left over data in the class.
		/// </summary>
		public void TcpListenerClose() {
			// If the server was succesfully initiated and assigned a socket ID, free it's memory.
			// NOTE: The server has no memory created if it fails to bind to a socket.
			if ( SocketId >= 0 ) {
				ServerHandle.Stop();
				Status = false;
				SocketSystem.UnbindSocket( SocketId );
				
				foreach( KeyValuePair<long,TcpClientSocket> ClientSocket in ClientList ) {
					ClientSocket.Value.ClientHandle.GetStream().Close();
					ClientSocket.Value.ClientHandle.Close();
					ClientSocket.Value.Connected = false;
				}
			}
		}

		/// <summary>
		/// Constructor for the TCP Listener(Server) Socket class :: Creates and starts the TCP listener
		/// </summary>
		/// <param name="myIPAddress">IP Address to host the listener on.</param>
		/// <param name="myPort">Port to host the listener on.</param>
		/// <param name="myMaxClients">Maximum number of clients allowed to connect.</param>
		/// <param name="myBufferRSize">Default read buffer size for every client.</param>
		/// <param name="myBufferWSize">Default write buffer size for every client.</param>
		/// <param name="myBufferAlign">Default buffer alignment for every client.</param>
		/// <param name="myHeader">Universal packet ID for received/sent packets.</param>
		public TcpListenerSocket( string myIPAddress , int myPort , int myMaxClients , int myBufferRSize , int myBufferWSize , int myBufferAlign , int myHeader ) {
			// Attempt to process the code, if not successful, throw an exception.
			try {
				// Initiate the TCP socket ID and set status to online.
				SocketId = SocketSystem.BindSocket();
				Status = true;

				// Setup server properties.
				MaxClients = myMaxClients;
				PacketHeader = ( byte ) myHeader;
                AddressInfo = new IPEndPoint(IPAddress.Any /*IPAddress.Parse(IPAddress.Any)*/, myPort);

				// Setup default buffer sizes for incoming packets.
				BufferReadSize = myBufferRSize;
				BufferWriteSize = myBufferWSize;
				BufferAlignment = myBufferAlign;

				// Create a new TCP Socket class and setup the client list.
				ServerHandle = new TcpListener( AddressInfo );
				ClientList = new Dictionary<long,TcpClientSocket>( MaxClients );
                ServerHandle.Start();

				// Call the TCP client connection accept, function is asynchronous, so TcpAccept() will still exit as expected.
				//System.Threading.Tasks.Task.Run( () => TcpAccept() );
				ThreadPool.QueueUserWorkItem( myThread => TcpAccept() );

                Console.WriteLine("TCP Connection Listener Established (" + myIPAddress + " :: " + myPort + ")");
                Console.WriteLine("     The server is set to have a maximum of " + MaxClients + " connections.");
            }
            catch (Exception e) { Console.WriteLine("ERROR: TCP SERVER FAILURE"); Console.WriteLine(e.Message); }
		}

		/// <summary>
		/// Accepts incoming client connections.
		/// </summary>
		public async void TcpAccept() {
			// While the server is online accept incoming client connections.
			while( Status == true ) {
				// Attempt to process the code, if not successful, throw an exception and close the server.
				
                Thread.Sleep( 10 );

				try {
					// If a pending client connection is found, accept the client connection.
					if ( ServerHandle.Pending() == true ) {
						// If the client connection is accepted, setup the new clent.
						TcpClientSocket NewClient = new TcpClientSocket( BufferReadSize , BufferWriteSize , BufferAlignment , PacketHeader );
						NewClient.ClientHandle = await ServerHandle.AcceptTcpClientAsync();
                        CmdSystem.AddLog("New connection. Verifying...");
                        NewClient.ClientHandle.LingerState = new LingerOption( true , 0 );
						NewClient.ClientHandle.NoDelay = true;
						NewClient.DataStream = NewClient.ClientHandle.GetStream();

						// Add the client to the server's client socket list.
						ClientList.Add( NewClient.SocketId , NewClient );

						// Start running the client and processing data for it, be it sending or receiving data.
						ThreadPool.QueueUserWorkItem( myThread => ClientHandle( NewClient ) );
					}
				} catch( Exception e ) {
                    CmdSystem.AddLog("===== ERROR =====");
                    CmdSystem.AddLog(e.Message);
                    CmdSystem.AddLog("=================");
				}
			}

			TcpListenerClose();
		}

		/// <summary>
		/// Starts handling the new client. If the client connects after MaxClients is exceeded, the client is disconnected.
		/// </summary>
        /// <param name="myClient">The client socket class that is being handled.</param>
		public async void ClientHandle( TcpClientSocket myClient ) {
			if ( ClientList.Count < MaxClients ) 
            {
				// Attempt to process the code, if not successful, throw an exception and disconnect the client.
				try {
					// While the client is connected, process any data and respond accordingly.
					while( myClient.Connected == true ) 
                    {
						// Only process data if data is available(sent from the client to the server).
						
                        if (myClient.CurrentRoom != null)
                            Thread.Sleep( 1 );
                        else
                            Thread.Sleep( 20 );
						
						// If a client-issued(not server-issued) disconnection has been detected close the client connection.
						// Usually this is an unexpected disconnection, a proper disconnection should have a client send a message/packet requesting disconnection.
						if ( myClient.ClientHandle.Connected == false ) {
							myClient.WriteBuffer.SetPeek( 0 );
							myClient.WriteBuffer.WriteUByte( PacketHeader );
							myClient.WriteBuffer.SendTcp( myClient.DataStream );
							myClient.Connected = myClient.ClientHandle.Connected;
						}

						if ( myClient.DataStream.DataAvailable == true ) {
                            myClient.DCTimer.Enabled = false;
                            myClient.DCTimer.Interval = 30000;
                            
                            // Clear the read and write buffers to respond to the newly received packet.
							myClient.ReadBuffer.Reset();
							myClient.WriteBuffer.Reset();

							// Copy the received data from the client's network stream to the client's read buffer.
							int PacketSize = myClient.ClientHandle.Available;
							await myClient.DataStream.ReadAsync( myClient.ReadBuffer.Buffer , 0 , PacketSize );
							
							// Start reading the received data and respond accordingly.
							TcpPackets.TcpPacketRead( myClient , this );
						}
                        else
                        {
                            //So we didn't get any data. Set the timer for 30 seconds to force-remove the player unless they respond again.
                            myClient.DCTimer.Enabled = true;
                        }
                        
					}

				} 
                catch( Exception e ) 
                {
					myClient.Connected = false;

                    //if (myClient.UserImposedDisconnection == false)
                    {
                        CmdSystem.AddLog(myClient.Name + " has been disconnected.");
                        Console.WriteLine("===");
                        Console.WriteLine(e.Message);
                        Console.WriteLine(e.StackTrace);
                        Console.WriteLine("===");
                    }
				}
			}

			// If the client disconnects, run a method that happens upon client disconnection.
			/*
			 *	NOTE: If a client disconnections by itself you can NOT use this to send messages back to the client.
			 *	Even though you can't send a message back to the client, you can still do something else. Such as
			 *	displaying a console output or sending a message to other clients that this client disconnected.
			*/
			// You CAN send messages back to the client if the client is being disconnected by the server instead!
			// This is because the client is still connected to the server while the ClientDisconnect() method is run.
			// However, the client is disconnected AFTER the ClientDisconnect() method.

            if (myClient.CurrentRoom != null)
                myClient.CurrentRoom.Remove(myClient);

			TcpPackets.TcpDisconnect( myClient );

			// Remove the client from the client list when it disconnects.
			ClientList.Remove( myClient.SocketId );
			myClient.TcpClientClose();

            if (myClient.ListSlot != null) myClient.ListSlot.GoOffline();
        }
	}

	/// <summary>
	/// Base TCP Client handling class that is managed by the TcpListenerSocket class.
	/// </summary>
	public class TcpClientSocket {
		public TcpClient ClientHandle;		// Client socket that manages the client's connection and data.
		public NetworkStream DataStream;		// Network stream of the client socket that handles receiving data and passing it to the read buffer.
		public ByteBuffer ReadBuffer;			// Buffer used for processing data received that was sent from the client.
		public ByteBuffer WriteBuffer;		// Buffer used to process and send data back to the client.
		public IPEndPoint AddressInfo;		// IP and Port information of the client in the form of an IP endpoint.
        internal System.Timers.Timer DCTimer;
		public bool Connected = true;			// Whether the client is connected or not(can be set to false to disconnect the client).
		public long SocketId = -1;				// Socket identifier that represents the client on the server.
		public byte HeaderId = 0;				// Header identifier used to verify received packets and label sent packets.
        public bool UserImposedDisconnection = false;
        //Some variables specifically for this representing of a player on the network.

        public double ID = 0;
        public string Name = "A nobody";
        public Room CurrentRoom = null;
        public Record Record = null;
        public PlayerListItem ListSlot = null;
        public byte Icon = 0;
        public uint XP = 0;
        public byte Tag = 1;
        public byte Character = 1;
        public byte Team = 0;
        public byte Slot = 0;

		/// <summary>
		/// Destructor for the TCP client socket class :: Frees up the client's memory and closes the connection to the client.
		/// </summary>
		public void TcpClientClose() {
			// When the client disconnects unbind it's socket ID and queue the socket ID for use again with another socket.
			SocketSystem.UnbindSocket( SocketId );
			DataStream.Dispose();
			ClientHandle.Close();
		}

		/// <summary>
		/// Constructor for the TCP Client Socket class :: Sets up the client on the server.
		/// </summary>
		/// <param name="myBufferReadSize">Read buffer size for the client.</param>
		/// <param name="myBufferWriteSize">Write buffer size for the client.</param>
		/// <param name="myAlignment">Alignment(in bytes) for the client.</param>
		/// <param name="myPacketHeader">Server/client packet header ID.</param>
		public TcpClientSocket( int myBufferReadSize , int myBufferWriteSize , int myAlignment , byte myPacketHeader ) {
			// Creates the socket ID, packet ID and read and write buffers upon constructing the class.
			ReadBuffer = new ByteBuffer( myBufferReadSize , myAlignment );
			WriteBuffer = new ByteBuffer( myBufferWriteSize , myAlignment );
			SocketId = SocketSystem.BindSocket();
			HeaderId = myPacketHeader;

            DCTimer = new System.Timers.Timer(30000);
            DCTimer.Elapsed += ForceDisconnect;
		}

        /// <summary>
        /// If the client doesn't send any data, we will force disconnect them. This was due to errors force-closing the game,
        /// and the server thought the client was still there.
        /// </summary>
        internal void ForceDisconnect(object sender, System.Timers.ElapsedEventArgs e)
        {
            Connected = false;
            CmdSystem.AddLog(Name + " was force-disconnected.");
            DCTimer.Enabled = false;
            if (ListSlot != null) ListSlot.GoOffline();
        }
	}
}
