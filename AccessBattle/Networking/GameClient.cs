using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace AccessBattle.Networking
{
    public class GameClient
    {
        Game _game = new Game();
        public Game Game { get { return _game; } }

        Socket _connection;
        public GameClient()
        {
            // TODO
        }

        public bool SendMessage(string message)
        {
            var con = _connection;
            if (con == null) return false;
            var bytes = Encoding.ASCII.GetBytes(message);
            try
            {
                return con.Send(bytes) == bytes.Length;
            }
            catch (Exception e)
            {
                Console.WriteLine("GameClient: Error sending message: " + e);
                return false;
            }
        }

        public bool Connect(string server, ushort port)
        {
            Disconnect();
            _connection = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            try
            {
                _connection.Connect(server, port);
                ReceiveAsync();
            }
            catch (Exception e)
            {
                Console.WriteLine("Client connect failed: " + e);
                return false;
            }
            Console.WriteLine("Client connect success!");
            return true;
        }

        void ReceiveAsync()
        {
            var con = _connection;
            if (con == null) return;

            var buffer = new byte[64];
            var args = new SocketAsyncEventArgs();
            args.SetBuffer(buffer, 0, buffer.Length);
            args.UserToken = 0;

            args.Completed += ClientReceive_Completed;
            con.ReceiveAsync(args);
        }

        void ClientReceive_Completed(object sender, SocketAsyncEventArgs e)
        {
            try
            {

            }
            // No catch for now. Exceptions will crash the program.
            finally
            {
                ReceiveAsync();
            }
        }

        /// <summary>
        /// </summary>
        public void Disconnect()
        {
            if (_connection == null) return;
            try
            {
                _connection.Close();
                _connection.Dispose();
            }
            catch { }
            _connection = null;
        }

    }
}
