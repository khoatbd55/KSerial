using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KSerial.Models.Message
{
    public class KSerialExceptionMessage : KSerialBaseMessage
    {
        public KSerialExceptionMessage(DateTime createdAt, Exception ex, string messsage)
        {
            CreatedAt = createdAt;
            Ex = ex;
            Message = messsage;
        }
        public DateTime CreatedAt { get; private set; }
        public Exception Ex { get; private set; }
        public string Message { get; private set; }
    }
}
