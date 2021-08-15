using System;
using System.Collections.Generic;
using System.Text;

namespace getEthInfo
{
    public class GasPrices
    {
        public int LastBlock { get; set; }
        public int SafeGasPrice { get; set; }
        public int ProposeGasPrice { get; set; }
        public int FastGasPrice { get; set; }
    }
}
