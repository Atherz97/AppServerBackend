namespace BBLegacyServer
{
    /// <summary>
    /// This is the set of code constants that are used as the ID system for packet headers.
    /// </summary>
    class Code
    {
        public const byte   PacketHeader = (byte)MainServer.PacketHeader;
        public const byte   ConnectionBegin = 1,
                            ConnectionEnd = 2,
                            MenuStatus = 3,
                                Status_Offline = 0, //The byte after the header.
                                Status_Playing = 1, //1 Playing
                                Status_AOnline = 2, //Available (not in room)
                                Status_POnline = 3, //Playing (in room)
                                Status_HOnline = 4, //Hosting (in room)
                            SessionJoin = 4,
                            SessionLeave = 5,
                            SessionCreate = 6,
                            UpdatePlayers = 7,
                            RequestPlayerList = 8,
                            SessionClose = 9,
                            ChangeCharacter = 10,
                            ChangeTeam = 11,
                            ChangeBattleSettings = 12,
                            StartRoom = 13,
                            RoomState = 14,
                            PlayerInfo = 15,
                            IAmHere = 16,
                            RequestRoomList = 17,
                            RequestRoomInformation = 18,
                            SendMe = 19,
                            Vote = 20,
                            BattleReady = 21,
                            Movement = 22,
                            Stopment = 23,
                            Item = 24,
                            Hit = 25,
                            Death = 26,
                            Score = 27,
                            CoinDrop = 28,
                            AbsoluteScore = 29,
                            PurpleSpawn = 30,
                            BattleEnd = 31,
                            PlayerStats = 32,
                            PlayerRecap = 33,
                            BattleComplete = 34,
                            ItemDrop = 35,
                            ServerMessages = 36,
                            UpdateXP = 37,
                            CanICreateRoomsYet = 38,
                            PlayerList = 39,
                            ItemConfig = 40,
                            PL_Single = 41,
                            ItemInvoke = 42,
                            Command = 43,
                            Message = 44;
    }

    /// <summary>
    /// For now you'll have to manually update the admin list here. These are player IDs, without the dashes.
    /// </summary>
    class AdminList
    {
        public static bool isAdmin(double ID)
        {
            if (ID == 169775756952) return (true);
            return false;
        }
    }

}
