using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KSerial.Models.Message
{
    public class KSerialLineMessage: KSerialBaseMessage
    {
        public string Message { get; set; }
        public byte[] Raw { get; set; }
    }
}
