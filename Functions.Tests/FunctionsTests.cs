using getEthInfo;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
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
    }
}