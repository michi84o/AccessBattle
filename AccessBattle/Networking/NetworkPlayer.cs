using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace AccessBattle.Networking
{
    public class NetworkPlayer : IDisposable
    {
        public int UID { get; private set; }
        public Socket Connection { get; private set; }
        public CryptoHelper ServerCrypto { get; private set; }
        public CryptoHelper ClientCrypto { get; set; }

        public NetworkPlayer(Socket connection, int uid, CryptoHelper serverCrypto)
        {
            Connection = connection;
            UID = uid;
            ServerCrypto = serverCrypto;
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
