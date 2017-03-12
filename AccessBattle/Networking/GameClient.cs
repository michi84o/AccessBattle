using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AccessBattle.Networking
{
    public class GameClient : NetworkBase
    {
        Game _game = new Game();
        public Game Game { get { return _game; } }
        ByteBuffer _receiveBuffer = new ByteBuffer(4096);
        CryptoHelper _encrypter;
        CryptoHelper _decrypter;
        Socket _connection;

        public GameClient()
        {
            _decrypter = new CryptoHelper();
        }
        
        public bool Connect(string server, ushort port)
        {
            Log.WriteLine("GameClient: Connecting...");
            try
            {
                //_isConnecting = true;
                Disconnect();
                _connection = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            
                _connection.Connect(server, port);
                ReceiveAsync(_connection, 0);

                // Block until keys are exchanged
                var then = DateTime.UtcNow.AddSeconds(30);
                while (_encrypter == null && (then - DateTime.UtcNow).TotalSeconds > 0)
                {
                    Thread.Sleep(100);
                }
                if (_encrypter == null)
                {
                    Disconnect();
                    Log.WriteLine("GameClient: Connect failed: No key received from server!");
                    return false;
                }
                // Send own public key
                Send(_decrypter.GetPublicKey(), NetworkPacketType.PublicKey, _connection, _encrypter);
            }
            catch (Exception e)
            {
                Log.WriteLine("GameClient: Connect failed: " + e.Message);
                return false;
            }
            finally
            {
                //_isConnecting = false;
            }
            Log.WriteLine("GameClient: Connect success!");
            return true;
        }

        protected override void Receive_Completed(object sender, SocketAsyncEventArgs e)
        {
            try
            {
                if (e.SocketError != SocketError.Success)
                {
                    // Will be hit after client disconnect
                    Log.WriteLine("GameClient: Receive not successful: " + e.SocketError);
                }
                else if (e.BytesTransferred > 0)
                {
                    Log.WriteLine("GameClient: Received " + e.BytesTransferred + " bytes of data");
                    _receiveBuffer.Add(e.Buffer, 0, e.BytesTransferred);
                    Log.WriteLine("GameClient: Receive buffer has now " + _receiveBuffer.Length + " bytes of data");

                    byte[] packData;
                    if (_receiveBuffer.Take(NetworkPacket.STX, NetworkPacket.ETX, out packData))
                    {
                        Log.WriteLine("GameClient: Received full packet");
                        var packet = NetworkPacket.FromByteArray(packData);
                        if (packet == null)
                            Log.WriteLine("GameClient: Could not parse packet!");
                        else
                        {
                            Log.WriteLine("GameClient: Received packet of type " + packet.PacketType + " with " + packet.Data.Length + " bytes of data");
                            ProcessPacket(packet);
                        }
                    }
                }

            }
            // No catch for now. Exceptions will crash the program.
            finally
            {
                e.Dispose();
                // Call not required after disconnect
                if (_connection != null) ReceiveAsync(_connection, 0);
            }
        }

        void ProcessPacket(NetworkPacket packet)
        {
            switch (packet.PacketType)
            {
                case 0x01: // Key
                    try
                    {
                        var serverPubKey = Encoding.ASCII.GetString(packet.Data);
                        _encrypter = new CryptoHelper(serverPubKey);
                    }
                    catch (Exception ex)
                    {
                        _encrypter = null;
                        Log.WriteLine("GameClient: Received key is invalid! " + ex.Message);
                    }
                    break;
                default:
                    Log.WriteLine("");
                    break;
            }
        }

        /// <summary>
        /// </summary>
        public void Disconnect()
        {
            if (_connection == null) return;
            _encrypter = null;
            try
            {
                Log.WriteLine("GameClient: Closing connection...");
                _connection.Close();
                _connection.Dispose();
                Log.WriteLine("GameClient: Connection closed");
            }
            catch (Exception e)
            {
                Log.WriteLine("GameClient: Closing connection caused an exception: " + e.Message);
            }
            _connection = null;
        }
    }
}
