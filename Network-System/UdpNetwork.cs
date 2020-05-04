using System.Net.Sockets;
using System.Threading;
using System.Net;
using System;
using System.Runtime.CompilerServices;

namespace BBLegacyServer {
	/// <summary>
	/// Custom socket class for handling a UDP server.
	/// </summary>
	public class UdpServerSocket {
		/*
		 * NOTE: It's called "UdpClient" however it's better understood as a UDP Server since UDP protocol is a
		 * connection-less host, rather than a node(client which connects).
		*/
		public UdpClient ServerHandle;					// UDP Client Socket Class(not socket ID).
		public ByteBuffer ReadBuffer;						// Read Buffer(For Reading Received Data).
		public ByteBuffer WriteBuffer;					// Write Buffer(For Writing and Sending Data).
		public long SocketId = -1;							// Socket ID of the UDP Socket(not client socket class).
		public bool Status = false;						// Online/Offline Status Of The Socket.
		public int Port = 0;									// Port of the UDP Client socket.
		public byte HeaderId = 0;							// Packet Header ID For Every Packet Sent/Received.
		public IPEndPoint SenderInfo;						// The IP and Port Info of whomever sent data.

		/// <summary>
		/// Destructor for the UDP Server Socket class :: Unbinds the UDP socket ID and closes the socket.
		/// </summary>
		public void UdpServerClose() {
			SocketSystem.UnbindSocket( SocketId );
			ServerHandle.Close();
		}

		/// <summary>
		/// Constructor for the UDP Server Socket class :: Creates and starts the UDP client socket.
		/// </summary>
		/// <param name="myPort"></param>
		/// <param name="myBufferRSize"></param>
		/// <param name="myBufferWSize"></param>
		/// <param name="myBufferAlign"></param>
		/// <param name="myHeader"></param>
		public UdpServerSocket( int myPort , int myBufferRSize , int myBufferWSize , int myBufferAlign , int myHeader ) {
			// Attempt to process the code, if not successful, throw an exception.
			try {
				// Initiate the UDP socket ID and set status to online.
				SocketId = SocketSystem.BindSocket();
				Status = true;
				
				// Set the indicated port and header IDs.
				Port = myPort;
				HeaderId = ( byte ) myHeader;

				// Create a new UDP Socket class and allow support for NAT Traversal.
				ServerHandle = new UdpClient( myPort );
				ServerHandle.AllowNatTraversal( true );
				
				// Create new read/write buffers of the indicated total byte size and byte alignment.
				ReadBuffer = new ByteBuffer( myBufferRSize , myBufferAlign );
				WriteBuffer = new ByteBuffer( myBufferWSize , myBufferAlign );
				
				// Call the UDP Handle, function is asynchronous, so UdpStart() will still exit as expected.
				ThreadPool.QueueUserWorkItem( myThread => UdpHandle() );
			} catch( Exception ) {}
		}

		/// <summary>
		/// Handles receiving data and passing the received data to the read buffer, which is processed by UdpPacketRead().
		/// </summary>
		public async void UdpHandle() {
			// While the UDP client status is active, run the asynchronous UDP Handle code.
			while( Status == true ) {
				// Attempt to process the code, if not successful, throw an exception and close the server.
				
                Thread.Sleep( 1 );

				try {
					// When data is received, process the data.
					if ( ServerHandle.Available > 0 ) {
						// Clear the read and write buffers to respond to the newly received packet.
						ReadBuffer.Reset();
						WriteBuffer.Reset();

						// Retrieve the received data and copy it into the read buffer.
						UdpReceiveResult Receiver = await ServerHandle.ReceiveAsync();
						Array.Copy( Receiver.Buffer , ReadBuffer.Buffer , Receiver.Buffer.Length );
						
						// Retrieve the sender's IP and Port information in the form of an EndPoint, then start reading the packet.
						UdpPackets.UdpPacketRead( this );
					}

					// Send any additional information that is independent of receiving data.
					UdpPackets.UdpPacketSend( this );
				} catch( Exception ) {
					Status = false;
				}
			}

			// Finalize stopping the server by closing the client.
			UdpServerClose();
		}
	}
}
