using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KSerial.Models.Message
{
    public class KSerialLogMessage : KSerialBaseMessage
    {
        public KSerialLogMessage(DateTime createdAt, string content)
        {
            CreatedAt = createdAt;
            Content = content;
        }
        public DateTime CreatedAt { get; private set; }
        public string Content { get; private set; }
    }
}
