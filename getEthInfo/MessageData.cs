using System;
using System.Collections.Generic;
using System.Text;

namespace getEthInfo
{
    class MessageData
    {
        public string messageId { get; set; }
        public string from { get; set; }
        public string to { get; set; }
        public string message { get; set; }
        public string receivedTimestamp { get; set; }
    }
}
