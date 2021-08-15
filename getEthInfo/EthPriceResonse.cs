using System;
using System.Collections.Generic;
using System.Text;

namespace getEthInfo
{
    public class EthPriceResponse
    {
        public EthPrices result { get; set; }
        public string status { get; set; }
        public string message { get; set; }

    }
}
