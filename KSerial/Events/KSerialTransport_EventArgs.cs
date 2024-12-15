using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KSerial.Events
{
    public class KSerialTransport_EventArgs:EventArgs
    {
        public string Message { get; set; }
        public byte[] Raw { get; set; }
    }
}
