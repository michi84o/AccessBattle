using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace AccessBattle.Networking
{
    public class NetworkPlayer : IPlayer, IDisposable
    {
        public uint UID { get; private set; }
        public string Name { get; set; }
        public bool IsLoggedIn { get; set; }
        public Socket Connection { get; private set; }
        /// <summary>
        /// Server crypto containing its private and public key. Used to decrypt data from client.
        /// </summary>
        public CryptoHelper ServerCrypto { get; private set; }
        /// <summary>
        /// Used to encrypt data for client
        /// </summary>
        public CryptoHelper ClientCrypto { get; set; }
        ByteBuffer _receiveBuffer = new ByteBuffer(4096);
        public ByteBuffer ReceiveBuffer { get { return _receiveBuffer; } }

        public NetworkPlayer(Socket connection, uint uid, CryptoHelper serverCrypto)
        {
            Connection = connection;
            UID = uid;
            ServerCrypto = serverCrypto;
        }

        public void PlayTurn()
        {
            Log.WriteLine("Network Player with UID " + UID + " requested to play turn");
            // TODO
            // Remote game must be synchronized
        }

        #region IDisposable
        bool disposed = false;
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposed)
                return;

            if (disposing)
            {
                // Free any managed objects here.
                if (Connection != null) Connection.Dispose();
                Connection = null;
                ServerCrypto = null;
                ClientCrypto = null;
            }

            // Free any unmanaged objects here.            
            disposed = true;
        }
        #endregion
    }
}
