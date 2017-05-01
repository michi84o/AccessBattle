using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace AccessBattle.Networking
{
    public abstract class NetworkBase
    {
        string _className;
        protected NetworkBase()
        {
            _className = GetType().Name;
        }

        protected bool Send(string message, byte packetType, Socket connection, CryptoHelper encrypter = null)
        {
            return Send(Encoding.ASCII.GetBytes(message), packetType, connection, encrypter);
        }
       
        protected bool Send(byte[] message, byte packetType, Socket connection, CryptoHelper encrypter = null)
        {
            if (connection == null || !connection.Connected || message == null)
                return false;
            try
            {
                var data = message;
                if (encrypter != null && data.Length > 0)
                    data = encrypter.Encrypt(data);
                var packet = (new NetworkPacket(data, packetType)).ToByteArray();                
                return connection.Send(packet) == packet.Length;
            }
            catch (Exception e)
            {
                Log.WriteLine(_className + ": Error sending data: " + e.Message);
                return false;
            }
        }

        protected void ReceiveAsync(Socket connection, uint token)
        {
            if (connection == null) return;
            var buffer = new byte[256];
            var args = new SocketAsyncEventArgs();
            args.SetBuffer(buffer, 0, buffer.Length);
            args.UserToken = token;

            args.Completed += Receive_Completed;
            connection.ReceiveAsync(args);
        }

        protected abstract void Receive_Completed(object sender, SocketAsyncEventArgs e);
    }
}
