using getEthInfo;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace Functions.Tests
{
    public class FunctionsTests
    {
        private readonly ILogger logger = TestFactory.CreateLogger();


        [Fact]
        public async Task Call_Timer()
        {
            var logger = (ListLogger)TestFactory.CreateLogger(LoggerTypes.List);
            await GetAndSendEthInfo.RunAsync(null, logger);
            var msg = logger.Logs[0];
            Assert.Contains("C# Timer trigger function executed at", msg);
        }

        [Fact]
        public async Task Call_Text_Received()
        {
            var logger = (ListLogger)TestFactory.CreateLogger(LoggerTypes.List);
            var path = Path.GetDirectoryName(typeof(FunctionsTests).Assembly.Location);
            var json = File.ReadAllText("../../../input.json");
            JObject o1 = JObject.Parse(File.ReadAllText("../../../input.json"));
            await ReceiveText.RunAsync((Newtonsoft.Json.Linq.JObject)o1, logger);
            var msg = logger.Logs[0];
            Assert.Contains("C# Timer trigger function executed at", msg);
        }
    }
}