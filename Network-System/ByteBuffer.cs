namespace BBLegacyServer {
	public class ByteBuffer {
		private int EndType = 0;
		public int ByteAlign = 1;
		public int BytePeek = 0;
		public byte[] Buffer;

		/// <summary>
		/// Class constructor for the ByteBuffer Class which creates a new buffer for the instance of the class.
		/// </summary>
		/// <param name="mySize">Size of the new buffer.</param>
		/// <param name="myAlign">Alignment of the new buffer.</param>
		public ByteBuffer( int mySize , int myAlign ) {
			Buffer = new byte[ mySize ];
			ByteAlign = myAlign;
		}

		/// <summary>
		/// Default class constructor for the ByteBuffer Class(does not create a new buffer).
		/// </summary>
		public ByteBuffer() {}

		/*
			Buffer System Details:
		 *		The buffer system includes: reading, writing, alignment and end type(+ false padding).
		 *		
		 *		Reading: Reading a value from the buffer pushes the peek position of the buffer forward based on the size of the read data and the alignment of the buffer.
		 *		
		 *		Writing: Writing a value to the buffer pushes the peek position of the buffer foward based on the size of the data being written and the alignment of the buffer.
		 *		
		 *		Alignment: Pads empty bytes between each piece of data to align the data for correct reading. The number of bytes padded is based on the ternary operation below:
		 *					  ( ( datasize >= alignment ) != 0 ) ? datasize : alginment;
		 *		
		 *		End Type: At the end of reading each TCP based packet, the end type is used to back up the read position based on "false padding" between smaller merged TCP packets.
		 *		          False padding is accounted for based on the ternary operation below:
		 *		          ( datasize >= alignment ) ? datasize : alignment - datasize;
		 *		
		 *		False Padding: After reading a value, padding is added based on the alignment of the buffer. However, padding for the last value of each packet is ignored when smaller
		 *							TCP packets are merged together. False padding however is a dynamic issue and thus cannot be dynamically accounted for when pushing forward the peek
		 *							position of the buffer. So the "End Type" and EndRead method are used to manually account for false padding between packets of data.
		*/

		// ------------------------------------------------------Start of System Methods----------------------------------------------------------------------
		
		/// <summary>Begins reading the header of the received packet of data at the specified peek position in the buffer. Returns the unsigned byte header of the new packet if the packet exists, else returns 0.</summary>
		/// <param name="myPeek">Peek position of the buffer.</param>
		public byte StartRead( int myPeek ) {
			BytePeek = myPeek;
			return ReadUByte();
		}

		/// <summary>Finalizes reading a single packet from the buffer, then searches for the next packet in the buffer. Returns true if successful else, false.</summary>
		/// <param name="endProtocol">Set to true if ending a TCP packet, false if ending a UDP packet.</param>
		public int EndRead( bool endProtocol ) {
			if ( BytePeek < Buffer.Length ) {
				BytePeek -= ( endProtocol == true ) ? EndType : 0;
				return ReadUByte();
			} else {
				return -1;
			}
		}

		public bool EndOfBuffer() {
			return ( BytePeek >= Buffer.Length );
		}

		/// <summary>Creates the new buffer for the instance of the ByteBuffer class.</summary>
		/// <param name="mySize">New size of the buffer.</param>
		/// <param name="myAlign">New byte alignmnet of the buffer.</param>
		public void Create( int mySize , int myAlign ) {
			Buffer = new byte[ mySize ];
			ByteAlign = myAlign;
		}

		/// <summary>
		/// Resets the buffer to it's default state(all data as 0s, buffer size does not change), clearing all variables to 0(except alignment).
		/// </summary>
		public void Reset() {
			System.Array.Clear( Buffer , 0 , Buffer.Length );
			BytePeek = 0;
			EndType = 0;
		}
		// -------------------------------------------------------End Of System Methods-----------------------------------------------------------------------

		/// <summary>Sends a TCP message to the specified client.</summary>
		/// <param name="myStream">The network stream of the client used to send data to.</param>
		public async void SendTcp( System.Net.Sockets.NetworkStream myStream ) {
			try {
                await myStream.WriteAsync( Buffer , 0 , BytePeek );
				await myStream.FlushAsync();
			} catch( System.Exception ) { CmdSystem.AddLog("Socket error"); }
		}

		/// <summary>Sends a UDP message to the specified IP address and Port.</summary>
		/// <param name="mySendIP">IP address to send the data to.</param>
        /// <param name="myClient">The UDP Client to send to.</param>
		/// <param name="mySendPort">Port to send the data on.</param>
		public async void SendUdp( System.Net.Sockets.UdpClient myClient , System.Net.IPAddress mySendIP , int mySendPort ) {
			try {
				System.Net.IPEndPoint myEndPoint = new System.Net.IPEndPoint( mySendIP , mySendPort );
				await myClient.SendAsync( Buffer , BytePeek , myEndPoint );
			} catch( System.Exception ) {}
		}

		/// <summary>Returns the buffer of the instance of the ByteBuffer class.</summary>
		public byte[] Source() {
			return Buffer;
		}

		/// <summary>Sets the peek position of the buffer.</summary>
		/// <param name="myPeek"></param>
		public void SetPeek( int myPeek ) {
			BytePeek = myPeek;
		}

        /// <summary>Sets the peek position of the buffer to zero.</summary>
        public void Clear()
        {
            System.Array.Clear(Buffer, 0, Buffer.Length);
            BytePeek = 0;
        }

		/// <summary>Returns the current peek position of the buffer.</summary>
		public int GetPeek() {
			return BytePeek;
		}

		/// <summary>Returns the size(in bytes) of the buffer.</summary>
		public int GetCount() {
			return Buffer.Length;
		}

		/// <summary>Clears a block of data in the buffer to zero values from the current index to the indicated size of the block to clear.</summary>
		public void Clear( int myPeek , int myBytes ) {
			System.Array.Clear( Buffer , myPeek , myBytes );
		}

		/// <summary>Empties the buffer by clearing all indexes in the buffer to zero.</summary>
		public void Empty( int myPeek , int myBytes ) {
			System.Array.Clear( Buffer , 0 , Buffer.Length );
		}
		
		/// <summary>
		/// Copies a portion of the Source buffer into a portion of the buffer of the class.
		/// </summary>
		/// <param name="mySource">The source buffer used to get the data that is being copied.</param>
		/// <param name="mySourceIndex">The starting index of the source buffer for the portion of data to copy.</param>
		/// <param name="myDestinationIndex">The starting index of destination buffer that the source data is being copied to.</param>
		/// <param name="myBytes">The length of the data to copy from the source buffer to the destination buffer.</param>
		public void Copy( ref byte[] mySource , int mySourceIndex , int myDestinationIndex , int myBytes ) {
			System.Array.Copy( mySource , mySourceIndex , Buffer , myDestinationIndex , myBytes );
		}

        /// <summary>Reads and returns an unsigned 8-bit integer from the buffer, which should only have 1 (true) or false (0).</summary>
        public bool ReadBool()
        {
            byte result = Buffer[BytePeek];
            BytePeek += ((1 >= ByteAlign) ? 1 : ByteAlign);
            EndType = ((1 >= ByteAlign) ? 1 : ByteAlign - 1);
            return (result == 1);
        }

		/// <summary>Reads and returns an unsigned 8-bit integer from the buffer.</summary>
		public byte ReadUByte() {
			byte result = Buffer[ BytePeek ];
			BytePeek += ( ( 1 >= ByteAlign ) ? 1 : ByteAlign );
			EndType = ( ( 1 >= ByteAlign ) ? 1 : ByteAlign - 1 );
			return result;
		}

		/// <summary>Reads and returns a signed 8-bit integer from the buffer.</summary>
		public sbyte ReadSByte() {
			sbyte result = ( sbyte ) Buffer[ BytePeek ];
			BytePeek += ( ( 1 >= ByteAlign ) ? 1 : ByteAlign );
			EndType = ( ( 1 >= ByteAlign ) ? 1 : ByteAlign - 1 );
			return result;
		}

		/// <summary>Reads and returns an unsigned 16-bit integer from the buffer.</summary>
		public ushort ReadUShort() {
			ushort result = ( ushort ) ( Buffer[ BytePeek ] + ( Buffer[ BytePeek + 1 ] << 8 ) );
			BytePeek += ( ( 2 >= ByteAlign ) ? 2 : ByteAlign );
			EndType = ( ( 2 >= ByteAlign ) ? 2 : ByteAlign - 2 );
			return result;
		}

		/// <summary>Reads and returns a signed 16-bit integer from the buffer.</summary>
		public short ReadSShort() {
			short result = ( short ) ( Buffer[ BytePeek ] + ( Buffer[ BytePeek + 1 ] << 8 ) );
			BytePeek += ( ( 2 >= ByteAlign ) ? 2 : ByteAlign );
			EndType = ( ( 2 >= ByteAlign ) ? 2 : ByteAlign - 2 );
			return result;
		}

		/// <summary>Reads and returns an unsigned 32-bit integer from the buffer.</summary>
		public uint ReadUInt() {
			uint result = ( uint ) ( Buffer[ BytePeek ] + ( Buffer[ BytePeek + 1 ] << 8 ) + ( Buffer[ BytePeek + 2 ] << 16 ) + ( Buffer[ BytePeek + 3 ] << 24 ) );
			BytePeek += ( ( 4 >= ByteAlign ) ? 4 : ByteAlign );
			EndType = ( ( 4 >= ByteAlign ) ? 4 : ByteAlign - 4 );
			return result;
		}

		/// <summary>Reads and returns a signed 32-bit integer from the buffer.</summary>
		public int ReadSInt() {
			int result = ( int ) ( Buffer[ BytePeek ] + ( Buffer[ BytePeek + 1 ] << 8 ) + ( Buffer[ BytePeek + 2 ] << 16 ) + ( Buffer[ BytePeek + 3 ] << 24 ) );
			BytePeek += ( ( ( 4 >= ByteAlign ) ? 4 : ByteAlign ) );
			EndType = ( ( 4 >= ByteAlign ) ? 4 : ByteAlign - 4 );
			return result;
		}

		/// <summary>Reads and returns a string from the buffer (string is null terminated).</summary>
		public string ReadString() {
			string result = "";

			for( int i = 0; i < Buffer.Length; i ++ ) {
				if ( Buffer[ BytePeek ] == 0 ) {
					BytePeek ++;
					break;
				}

				result += ( char ) Buffer[ BytePeek ];
				BytePeek ++;
			}

			int DynamicAlign = ( ( ( result.Length + 1 ) % ByteAlign != 0 ) ? ByteAlign - ( ( result.Length + 1 ) % ByteAlign ) : 0 );
			BytePeek += DynamicAlign;
			EndType = result.Length + DynamicAlign;
			return result;
		}

		/// <summary>Reads and returns a 32-bit single precision float from the buffer.</summary>
		public float ReadFloat() {
			float myValue = System.BitConverter.ToSingle( Buffer , BytePeek );
			BytePeek += ( ( 4 >= ByteAlign ) ? 4 : ByteAlign );
			EndType = ( ( 4 >= ByteAlign ) ? 4 : ByteAlign - 4 );
			return myValue;
		}

		/// <summary>Reads and returns a 64-bit double precision float from the buffer.</summary>
		public double ReadDouble() {
			double myValue = System.BitConverter.ToDouble( Buffer , BytePeek );
			BytePeek += ( ( 8 >= ByteAlign ) ? 8 : ByteAlign );
			EndType = ( ( 8 >= ByteAlign ) ? 8 : ByteAlign - 8 );
			return myValue;
		}

        /// <summary>Writes an unsigned 8-bit integer to the buffer, with only true (1) or false (0) as values.</summary>
        public void WriteBool(bool myValue)
        {
            if (myValue)
                WriteUByte(1);
            else
                WriteUByte(0);
        }
        
        /// <summary>Writes an unsigned 8-bit integer to the buffer.</summary>
		public void WriteUByte( byte myValue ) {
			Buffer[ BytePeek ] = myValue;
			BytePeek += ( ( 1 >= ByteAlign ) ? 1 : ByteAlign );
		}

		/// <summary>Writes a signed 8-bit integer to the buffer.</summary>
		public void WriteSByte( sbyte myValue ) {
			Buffer[ BytePeek ] = ( byte ) myValue;
			BytePeek += ( ( 1 >= ByteAlign ) ? 1 : ByteAlign );
		}

		/// <summary>Writes an unsigned 16-bit integer to the buffer.</summary>
		public void WriteUShort( ushort myValue ) {
			Buffer[ BytePeek ] = ( byte ) ( myValue >> 0 );
			Buffer[ BytePeek + 1 ] = ( byte ) ( myValue >> 8 );
			BytePeek += ( ( 2 >= ByteAlign ) ? 2 : ByteAlign );
		}

		/// <summary>Writes a signed 16-bit integer to the buffer.</summary>
		public void WriteSShort( short myValue ) {
			Buffer[ BytePeek ] = ( byte ) ( myValue >> 0 );
			Buffer[ BytePeek + 1 ] = ( byte ) ( myValue >> 8 );
			BytePeek += ( ( 2 >= ByteAlign ) ? 2 : ByteAlign );
		}

		/// <summary>Writes an unsigned 32-bit integer to the buffer.</summary>
		public void WriteUInt( uint myValue ) {
			Buffer[ BytePeek ] = ( byte ) ( myValue >> 0 );
			Buffer[ BytePeek + 1 ] = ( byte ) ( myValue >> 8 );
			Buffer[ BytePeek + 2 ] = ( byte ) ( myValue >> 16 );
			Buffer[ BytePeek + 3 ] = ( byte ) ( myValue >> 24 );
			BytePeek += ( ( 4 >= ByteAlign ) ? 4 : ByteAlign );
		}

		/// <summary>Writes a signed 32-bit integer to the buffer.</summary>
		public void WriteSInt( int myValue ) {
			Buffer[ BytePeek ] = ( byte ) ( myValue >> 0 );
			Buffer[ BytePeek + 1 ] = ( byte ) ( myValue >> 8 );
			Buffer[ BytePeek + 2 ] = ( byte ) ( myValue >> 16 );
			Buffer[ BytePeek + 3 ] = ( byte ) ( myValue >> 24 );
			BytePeek += ( ( 4 >= ByteAlign ) ? 4 : ByteAlign );
		}

		/// <summary>Writes a string to the buffer (string is null terminated).</summary>
		public void WriteString( string myString ) {
			byte[] myBytes = System.Text.Encoding.ASCII.GetBytes( myString );
			
			for( int i = 0; i < myBytes.Length; i ++ ) {
				Buffer[ BytePeek ] = myBytes[ i ];
				BytePeek ++;
			}

			if ( ( ( myString.Length + 1 ) % ByteAlign ) != 0 ) {
				BytePeek += ( 1 + ( ByteAlign - ( ( myString.Length + 1 ) % ByteAlign ) ) );
			} else {
				BytePeek ++;
			}
		}

		/// <summary>Writes a 32-bit single precision float to the buffer.</summary>
		public void WriteFloat( float myValue ) {
			byte[] myBytes = System.BitConverter.GetBytes( myValue );

			for( int i = 0; i < myBytes.Length; i ++ ) {
				Buffer[ BytePeek ] = myBytes[ i ];
				BytePeek ++;
			}

			BytePeek += ( ( 4 >= ByteAlign ) ? 0 : ByteAlign - 4 );
		}

		/// <summary>Writes a 64-bit double precision float ot the buffer.</summary>
		public void WriteDouble( double myValue ) {
			byte[] myBytes = System.BitConverter.GetBytes( myValue );

			for( int i = 0; i < myBytes.Length; i ++ ) {
				Buffer[ BytePeek ] = myBytes[ i ];
				BytePeek ++;
			}

			BytePeek += ( ( 8 >= ByteAlign ) ? 0 : ByteAlign - 8 );
		}
	}
}
