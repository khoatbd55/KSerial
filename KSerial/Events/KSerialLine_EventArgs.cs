using KSerial.Models.Message;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KSerial.Events
{
    public class KSerialLine_EventArgs:EventArgs
    {
        public KSerialLine_EventArgs(object sender,KSerialLineMessage message) 
        { 
            Sender = sender;
            Message = message;
        }
        public object Sender { get; private set; }
        public KSerialLineMessage Message { get; private set; }
    }
}
