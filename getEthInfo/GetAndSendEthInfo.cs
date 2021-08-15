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
using System.Configuration;
using System.IO;

namespace getEthInfo
{
    public static class GetAndSendEthInfo
    {
        static void ConfigureEnvironmentVariablesFromLocalSettings()
        {
            var path = Path.GetDirectoryName(typeof(GetAndSendEthInfo).Assembly.Location);
            var json = File.ReadAllText(Path.Join(path, "local.settings.json"));
            var parsed = Newtonsoft.Json.Linq.JObject.Parse(json).Value<Newtonsoft.Json.Linq.JObject>("Values");

            foreach (var item in parsed)
            {
                Environment.SetEnvironmentVariable(item.Key, item.Value.ToString());
            }
        }
        private static readonly HttpClient client = new HttpClient();

        [FunctionName("GetAndSendEthInfo")]
        public static async Task RunAsync([TimerTrigger("0 0 5,17 * * *")] TimerInfo myTimer, ILogger logger)
        {
            logger.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");
            TimeZoneInfo pstZone = TimeZoneInfo.FindSystemTimeZoneById("Pacific Standard Time");
            DateTime pstTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, pstZone);

            client.DefaultRequestHeaders.Accept.Clear();
            string apiKey = Environment.GetEnvironmentVariable("APIKEY");
            if (apiKey == null)
            {
                ConfigureEnvironmentVariablesFromLocalSettings();
                apiKey = Environment.GetEnvironmentVariable("APIKEY");
            }
            try
            {
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
                from: Environment.GetEnvironmentVariable("OUTGOING_NUMBER"),
                to: Environment.GetEnvironmentVariable("INCOMING_NUMBER"),
                message: $"{pstTime}\nETH Price: ${ethResponse.result.ethusd} \nGas Price: {gasResponse.result.ProposeGasPrice} \nETH Transfer Price: ${priceOfEthTransfer}\nERC20 Transfer Price: ${priceOfErc20Transfer}\n"
                );
            }
            catch(Exception e)
            {
                logger.LogError("Task Run Error", e);
                throw e;
            }
            

            
        }
    }
}
