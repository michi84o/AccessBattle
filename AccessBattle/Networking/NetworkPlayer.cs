using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace AccessBattle.Networking
{
    /// <summary>
    /// Class for storing information about network players. Used by the game server.
    /// </summary>
    public class NetworkPlayer : IPlayer, IDisposable
    {
        /// <summary>
        /// Unique ID to identify this player.
        /// ID must only be unique until the server is restarted.
        /// </summary>
        public uint UID { get; private set; }
        /// <summary>
        /// Login name for the player. Should be unique.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Login status of the player.
        /// </summary>
        public bool IsLoggedIn { get; set; }

        /// <summary>
        /// Connection to the player.
        /// </summary>
        public Socket Connection { get; private set; }

        /// <summary>
        /// Current game this player has joined.
        /// </summary>
        public Game CurrentGame { get; set; }

        /// <summary>
        /// Used to decrypt data that was received from the player.
        /// </summary>
        public CryptoHelper ServerCrypto { get; private set; }
                
        /// <summary>
        /// Used to encrypt data that is sent to the player.
        /// </summary>
        /// <remarks>
        /// This crypto must be provided by the client and is received later.
        /// Therefore this property was made read/write. 
        /// It should only be written once the key was received.</remarks>
        public CryptoHelper ClientCrypto { get; set; }

        ByteBuffer _receiveBuffer = new ByteBuffer(4096);
        /// <summary>
        /// Receive buffer for this player.
        /// </summary>
        public ByteBuffer ReceiveBuffer { get { return _receiveBuffer; } }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="connection">Connection to player.</param>
        /// <param name="uid">Unique ID.</param>
        /// <param name="serverCrypto">Decryptor for player data.</param>
        public NetworkPlayer(Socket connection, uint uid, CryptoHelper serverCrypto)
        {
            Connection = connection;
            UID = uid;
            ServerCrypto = serverCrypto;
        }

        /// <summary>
        /// Notify the player to make his move.
        /// </summary>
        public void PlayTurn()
        {
            Log.WriteLine("Network Player with UID " + UID + " requested to play turn");
            // TODO           
        }

        #region IDisposable

        bool disposed = false;
        /// <summary>
        /// Dispose this instance.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Dispose this instance.
        /// </summary>
        /// <param name="disposing"></param>
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
