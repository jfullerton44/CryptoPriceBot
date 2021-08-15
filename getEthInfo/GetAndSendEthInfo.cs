using System;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using Azure;
using Azure.Communication;
using Azure.Communication.Sms;
using System.Threading.Tasks;
using System.Net.Http;
using System.Net.Http.Headers;
using Newtonsoft.Json;

namespace getEthInfo
{
    public class GasPrices
    {
        public int LastBlock { get; set; }
        public int SafeGasPrice { get; set; }
        public int ProposeGasPrice { get; set; }
        public int FastGasPrice { get; set; }
    }

    public class ETHPrices
    {
        public double ethbtc { get; set; }
        public int ethbtc_timestamp { get; set; }
        public double ethusd { get; set; }
        public int ethusd_timestamp { get; set; }

    }
    public class GasPriceResponse
    {
        public GasPrices result { get; set; }
        public string status { get; set; }
        public string message { get; set; }

    }

    public class EthPriceResponse
    {
        public ETHPrices result { get; set; }
        public string status { get; set; }
        public string message { get; set; }

    }
    public static class GetAndSendEthInfo
    {
        private static readonly HttpClient client = new HttpClient();

        [FunctionName("GetAndSendEthInfo")]
        public static async Task RunAsync([TimerTrigger("0 0 5,17 * * *")]TimerInfo myTimer, ILogger logger)
        {
            logger.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");
            TimeZoneInfo pstZone = TimeZoneInfo.FindSystemTimeZoneById("Pacific Standard Time");
            DateTime pstTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, pstZone);

            client.DefaultRequestHeaders.Accept.Clear();
            string apiKey = Environment.GetEnvironmentVariable("APIKEY");
            var stringTask = client.GetStringAsync("https://api.etherscan.io/api?module=gastracker&action=gasoracle&apikey=" + apiKey);
            var msg = await stringTask;
            logger.LogInformation(msg);
            var gasResponse = JsonConvert.DeserializeObject<GasPriceResponse>(msg as string);
            logger.LogInformation(gasResponse.result.ProposeGasPrice.ToString());

            var getEthPrice = await client.GetStringAsync("https://api.etherscan.io/api?module=stats&action=ethprice&apikey=" + apiKey);
            logger.LogInformation(getEthPrice);
            var ethResponse = JsonConvert.DeserializeObject<EthPriceResponse>(getEthPrice as string);
            logger.LogInformation(ethResponse.result.ethusd.ToString());
            var gasPrice = gasResponse.result.ProposeGasPrice;
            var ethPrice = Math.Round(ethResponse.result.ethusd, 2);
            var gasForEthTransfer = 21000;
            var ethToGas = 0.000000001;
            var gasForErc20Transfer = 65000;
            var priceOfEthTransfer = Math.Round(gasPrice * ethToGas * gasForEthTransfer * ethPrice, 2);
            var priceOfErc20Transfer = Math.Round(gasPrice * ethToGas * gasForErc20Transfer * ethPrice, 2);
            logger.LogInformation("Eth Transfer: " + priceOfEthTransfer.ToString() + " ERC20 Transfer: " + priceOfErc20Transfer.ToString());
            string connectionString = Environment.GetEnvironmentVariable("CONNECTION_STRING");
            SmsClient smsClient = new SmsClient(connectionString);
            SmsSendResult sendResult = smsClient.Send(
            from: "+18443999088",
            to: "+18138921753",
            message: $"{pstTime}\nETH Price: ${ethResponse.result.ethusd} \nGas Price: {gasResponse.result.ProposeGasPrice} \nETH Transfer Price: ${priceOfEthTransfer}\nERC20 Transfer Price: ${priceOfErc20Transfer}\n"
            );
        }
    }
}
