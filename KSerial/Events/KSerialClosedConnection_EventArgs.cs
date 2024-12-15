using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KSerial.Events
{
    public class KSerialClosedConnection_EventArgs : EventArgs
    {
        public KSerialClosedConnection_EventArgs(object sender, EventArgs eventArgs)
        {
            Sender = sender;
            EventArgs = eventArgs;
        }

        public object Sender { get; private set; }
        public EventArgs EventArgs { get; private set; }
    }
}
