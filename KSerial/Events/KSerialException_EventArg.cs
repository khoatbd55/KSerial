using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KSerial.Events
{
    public class KSerialException_EventArg : EventArgs
    {
        public KSerialException_EventArg(object sender, Exception ex)
        {
            Sender = sender;
            Ex = ex;
        }

        public object Sender { get; private set; }
        public Exception Ex { get; private set; }
    }
}
