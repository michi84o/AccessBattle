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
        ByteBuffer _receiveBuffer = new ByteBuffer(4096);


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
                if (e.SocketError != SocketError.Success)
                {
                    // Will be hit after client disconnect
                    Console.WriteLine("Client receive not successful: " + e.SocketError);
                }
                else if (e.BytesTransferred > 0)
                {
                    Console.WriteLine("Client received " + e.BytesTransferred + " bytes of data.");
                    _receiveBuffer.Add(e.Buffer, 0, e.BytesTransferred);
                    Console.WriteLine("Clients receive buffer has now " + _receiveBuffer.Length + " bytes of data.");

                    byte[] packData;
                    if (_receiveBuffer.Take(NetworkPacket.STX, NetworkPacket.ETX, out packData))
                    {
                        Console.WriteLine("Client received full packet");
                        var pack = NetworkPacket.FromByteArray(packData);
                        if (pack == null)
                            Console.WriteLine("Client coud not parse packet!");
                        else
                        {
                            Console.WriteLine("Client received packet of type " + pack.PacketType + " with " + pack.Data.Length + " bytes of data.");
                        }
                    }
                }

            }
            // No catch for now. Exceptions will crash the program.
            finally
            {
                // TODO: Call not required after disconnect
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
