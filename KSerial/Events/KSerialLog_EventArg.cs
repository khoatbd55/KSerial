using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KSerial.Events
{
    public class KSerialLog_EventArg : EventArgs
    {
        public KSerialLog_EventArg(object sender, string message)
        {
            Sender = sender;
            Message = message;
        }

        public object Sender { get; private set; }
        public string Message { get; private set; }
    }
}
