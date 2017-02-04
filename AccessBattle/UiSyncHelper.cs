using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AccessBattle
{
    public class UiSyncHelper
    {
        static SynchronizationContext _context;
        public static SynchronizationContext Context
        {
            get { return _context ?? new SynchronizationContext(); }
            set { _context = value; }
        }

        public static void Execute(Action action)
        {
            var handler = action;
            if (handler != null)
                Context.Send(o => handler(), null);
        }
    }
}
