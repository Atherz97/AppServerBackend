using System.Net.Sockets;
using System.Collections.Generic;
using System.Net;
using System.Runtime.CompilerServices;

namespace BBLegacyServer {
	public static class SocketSystem {
		public static List<long> SocketList = new List<long>();
		public static uint SocketAssigner = 0;

		public static long BindSocket() {
			long SocketId = -1;

			if ( SocketList.Count == 0 ) {
				SocketId = SocketAssigner;
				SocketAssigner ++;
			} else {
				SocketId = SocketList[ 0 ];
				SocketList.RemoveAt( 0 );
			}

			return SocketId;
		}

		public static void UnbindSocket( long mySocketId ) {
			if ( mySocketId >= 0 ) {
				SocketList.Add( mySocketId );
				SocketList.Sort();
			}
		}
	}
}
