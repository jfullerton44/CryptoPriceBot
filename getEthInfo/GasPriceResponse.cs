using System;
using System.Collections.Generic;
using System.Text;

namespace getEthInfo
{
    public class GasPriceResponse
    {
        public GasPrices result { get; set; }
        public string status { get; set; }
        public string message { get; set; }

    }
}
