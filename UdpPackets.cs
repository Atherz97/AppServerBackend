using System.Net.Sockets;
using BBLegacyServer;

namespace BBLegacyServer {
	/// <summary>
	/// Simple static class to separate UDP packet reading from the low-level UDP server system.
	/// </summary>
	public static class UdpPackets {
		/// <summary>
		/// Allows for sending data / data handling, independently of receiving data.
		/// </summary>
		/// <param name="myServer">Server socket class that is being handled.</param>
		public static void UdpPacketSend( UdpServerSocket myServer ) {
			
		}

		/// <summary>
		/// Reads the received data from the read buffer that received the data sent to the UDP client.
		/// </summary>
		/// <param name="myServer">Server socket class that is being handled.</param>
		public static void UdpPacketRead( UdpServerSocket myServer ) {
			// Attempt to process the code, if not successful, throw an exception.
			var myCheck = myServer.ReadBuffer.StartRead(12);
				
			while( myCheck == myServer.HeaderId ) {
				var myPacketId = myServer.ReadBuffer.ReadUByte();

                var Buff = myServer.ReadBuffer;

				switch(myPacketId) 
                {
                    //Sets the menu status of this specific player to this status.
					case (Code.MenuStatus):
                        {
                            string playerId = Buff.ReadDouble().ToString();
                            byte OnlineStatus = Buff.ReadUByte();
                            MainServer.SettingsFile.SetValue(playerId,"MenuStatus",OnlineStatus);
                            break;
                        }
						
					default: break;
				}
					
				// If we're done reading the packet back-up the read/peek position of the buffer and check for a secondary/merged packet.
				int myHeaderId = myServer.ReadBuffer.EndRead( false );
				myCheck = ( byte ) ( ( myHeaderId != -1 ) ? myHeaderId : ~myHeaderId );
			}
		}
	}
}
