using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace AccessBattle
{
    public class GameClient
    {
        Game _game = new Game();
        public Game Game { get { return _game; } }

        TcpClient _client;

        public GameClient()
        {
            // TODO
        }

        public bool Connect(string server, ushort port)
        {
            Disconnect();
            _client = new TcpClient(server, port);

            // TODO

            return true;
        }

        public void Disconnect()
        {
            if (_client == null) return;
            _client.Close();
            _client = null;

            // TODO

        }

    }
}
