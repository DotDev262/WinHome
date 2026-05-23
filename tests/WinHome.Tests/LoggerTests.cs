using System.Text.Json;
using WinHome.Interfaces;
using WinHome.Services.Logging;

namespace WinHome.Tests
{
    public class ConsoleLoggerTests
    {
        [Fact]
        public void DefaultMinLevel_ShowsInfoAndAbove()
        {
            var logger = new ConsoleLogger();
            var output = new StringWriter();
            var originalOut = Console.Out;
            Console.SetOut(output);

            try
            {
                logger.Log("trace msg", LogLevel.Trace);
                logger.Log("debug msg", LogLevel.Debug);
                logger.LogInfo("info msg");
                logger.LogSuccess("success msg");
                logger.LogWarning("warning msg");
                logger.LogError("error msg");

                var result = output.ToString();
                Assert.DoesNotContain("trace msg", result);
                Assert.DoesNotContain("debug msg", result);
                Assert.Contains("info msg", result);
                Assert.Contains("success msg", result);
                Assert.Contains("warning msg", result);
                Assert.Contains("error msg", result);
            }
            finally
            {
                Console.SetOut(originalOut);
            }
        }

        [Fact]
        public void Verbose_ShowsAllLevels()
        {
            var logger = new ConsoleLogger();
            logger.SetMinLevel(LogLevel.Trace);
            var output = new StringWriter();
            var originalOut = Console.Out;
            Console.SetOut(output);

            try
            {
                logger.Log("trace msg", LogLevel.Trace);
                logger.Log("debug msg", LogLevel.Debug);
                logger.LogInfo("info msg");

                var result = output.ToString();
                Assert.Contains("trace msg", result);
                Assert.Contains("debug msg", result);
                Assert.Contains("info msg", result);
            }
            finally
            {
                Console.SetOut(originalOut);
            }
        }

        [Fact]
        public void Quiet_ShowsOnlyWarningsAndErrors()
        {
            var logger = new ConsoleLogger();
            logger.SetMinLevel(LogLevel.Warning);
            var output = new StringWriter();
            var originalOut = Console.Out;
            Console.SetOut(output);

            try
            {
                logger.LogInfo("info msg");
                logger.LogSuccess("success msg");
                logger.LogWarning("warning msg");
                logger.LogError("error msg");

                var result = output.ToString();
                Assert.DoesNotContain("info msg", result);
                Assert.DoesNotContain("success msg", result);
                Assert.Contains("warning msg", result);
                Assert.Contains("error msg", result);
            }
            finally
            {
                Console.SetOut(originalOut);
            }
        }
    }

    public class JsonLoggerTests
    {
        [Fact]
        public void DefaultMinLevel_ShowsInfoAndAbove()
        {
            var logger = new JsonLogger();

            logger.Log("trace msg", LogLevel.Trace);
            logger.Log("debug msg", LogLevel.Debug);
            logger.LogInfo("info msg");
            logger.LogSuccess("success msg");
            logger.LogWarning("warning msg");
            logger.LogError("error msg");

            var json = logger.ToJson();
            Assert.DoesNotContain("trace msg", json);
            Assert.DoesNotContain("debug msg", json);
            Assert.Contains("info msg", json);
            Assert.Contains("success msg", json);
            Assert.Contains("warning msg", json);
            Assert.Contains("error msg", json);
        }

        [Fact]
        public void Verbose_ShowsAllLevels()
        {
            var logger = new JsonLogger();
            logger.SetMinLevel(LogLevel.Trace);

            logger.Log("trace msg", LogLevel.Trace);
            logger.Log("debug msg", LogLevel.Debug);
            logger.LogInfo("info msg");

            var json = logger.ToJson();
            Assert.Contains("trace msg", json);
            Assert.Contains("debug msg", json);
            Assert.Contains("info msg", json);
        }

        [Fact]
        public void Quiet_ShowsOnlyWarningsAndErrors()
        {
            var logger = new JsonLogger();
            logger.SetMinLevel(LogLevel.Warning);

            logger.LogInfo("info msg");
            logger.LogSuccess("success msg");
            logger.LogWarning("warning msg");
            logger.LogError("error msg");

            var json = logger.ToJson();
            Assert.DoesNotContain("info msg", json);
            Assert.DoesNotContain("success msg", json);
            Assert.Contains("warning msg", json);
            Assert.Contains("error msg", json);
        }
    }
}
