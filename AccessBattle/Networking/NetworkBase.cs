using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace AccessBattle.Networking
{
    /// <summary>
    /// Base class for networking.
    /// </summary>
    public abstract class NetworkBase : PropChangeNotifier
    {
        /// <summary>
        /// Default JSON serializer settings.
        /// </summary>
        protected JsonSerializerSettings _serializerSettings = new JsonSerializerSettings { DefaultValueHandling = DefaultValueHandling.Ignore };

        /// <summary>
        /// Name of class that implements this base class. Used for error messages.
        /// </summary>
        string _className;

        /// <summary>
        /// This constructor reads the name of class that implements this base class.
        /// It is used for error messages.
        /// </summary>
        protected NetworkBase()
        {
            _className = GetType().Name;
        }

        /// <summary>
        /// Sends data over the provides connection and optionally encrypts it.
        /// </summary>
        /// <param name="message">Message to send.</param>
        /// <param name="packetType">Packet type.</param>
        /// <param name="connection">Connection to use.</param>
        /// <param name="encrypter">Optional encryptor.</param>
        /// <returns>True if send was successful.</returns>
        protected bool Send(string message, byte packetType, Socket connection, CryptoHelper encrypter = null)
        {
            return Send(Encoding.ASCII.GetBytes(message), packetType, connection, encrypter);
        }

        /// <summary>
        /// Sends data over the provides connection and optionally encrypts it.
        /// </summary>
        /// <param name="message">Message to send.</param>
        /// <param name="packetType">Packet type.</param>
        /// <param name="connection">Connection to use.</param>
        /// <param name="encrypter">Optional encryptor.</param>
        /// <returns>True if send was successful.</returns>
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

        /// <summary>
        /// Start receiving data. Uses events.
        /// </summary>
        /// <param name="connection">Connection to listen for data.</param>
        /// <param name="token">A token to identify the right event. Receive_Completed events with other tokens can be ignored.</param>
        protected void ReceiveAsync(Socket connection, uint token)
        {
            if (connection == null) return;
            var buffer = new byte[256];
            var args = new SocketAsyncEventArgs();
            args.SetBuffer(buffer, 0, buffer.Length);
            args.UserToken = token;

            args.Completed += Receive_Completed;
            try
            {
                connection.ReceiveAsync(args);
            }
            catch (Exception)
            {
                // object disposed exception when connection is closed
                // TODO: Close connection ???
                return;
            }
        }

        /// <summary>
        /// Handler for the ReceiveAsync method.
        /// </summary>
        /// <param name="sender">Sender.</param>
        /// <param name="e">Event args.</param>
        protected abstract void Receive_Completed(object sender, SocketAsyncEventArgs e);
    }
}
