using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace AccessBattle
{
    public class NetworkPlayer : IDisposable
    {
        Socket _connection;
        public NetworkPlayer(Socket connection)
        {
            _connection = connection;
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
                if (_connection != null) _connection.Dispose();
                _connection = null;
            }

            // Free any unmanaged objects here.            
            disposed = true;
        }
        #endregion
    }
}
