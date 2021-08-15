// Default URL for triggering event grid function in the local environment.
// http://localhost:7071/runtime/webhooks/EventGrid?functionName={functionname}
using System;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Azure.EventGrid.Models;
using Microsoft.Azure.WebJobs.Extensions.EventGrid;
using Microsoft.Extensions.Logging;
using Azure.Communication.Sms;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Net.Http;
using System.IO;
using Newtonsoft.Json.Linq;

namespace getEthInfo
{
    public static class ReceiveText
    {
        private static readonly HttpClient client = new HttpClient();

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

        [FunctionName("ReceiveText")]
        public static async Task RunAsync([EventGridTrigger]JObject eventGridEvent, ILogger logger)
        {
            var eventTrigger = eventGridEvent.ToObject<EventGridEvent>();
            var dataObject = (JObject)eventTrigger.Data;
            var data = dataObject.ToObject<MessageData>();
            //var data = JsonConvert.DeserializeObject<MessageData>(eventTrigger.Data as string);

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
                from: data.to,
                to: data.from,
                message: $"{pstTime}\nETH Price: ${ethResponse.result.ethusd} \nGas Price: {gasResponse.result.ProposeGasPrice} \nETH Transfer Price: ${priceOfEthTransfer}\nERC20 Transfer Price: ${priceOfErc20Transfer}\nYour Message:{data.message}\n"
                );
            }
            catch (Exception e)
            {
                logger.LogError("Task Run Error", e);
                throw e;
            }



        }
    }
}
