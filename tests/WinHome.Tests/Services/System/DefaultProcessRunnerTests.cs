using System;
using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using System.Threading;
using WinHome.Services.System;
using Xunit;

namespace WinHome.Tests.Services.System
{
    public class DefaultProcessRunnerTests
    {
        /// <summary>RunCommand returns true without executing when dryRun is enabled.</summary>
        [Fact]
        public void RunCommand_DryRunTrue_ReturnsTrueWithoutExecuting()
        {
            // Arrange
            var runner = new DefaultProcessRunner();
            string exe = "zzz-no-such-exe-" + Guid.NewGuid().ToString("N");

            // Act
            bool result = runner.RunCommand(exe, "--version", true);

            // Assert
            Assert.True(result);
        }

        /// <summary>RunCommand returns true for a successful process.</summary>
        [Fact]
        public void RunCommand_SuccessfulProcess_ReturnsTrue()
        {
            // Arrange
            var runner = new DefaultProcessRunner();

            // Act
            bool result = runner.RunCommand("dotnet", "--version", false);

            // Assert
            Assert.True(result);
        }

        /// <summary>RunCommand returns false when the process exits with a non-zero code.</summary>
        [Fact]
        public void RunCommand_ExitNonZero_ReturnsFalse()
        {
            // Arrange
            var runner = new DefaultProcessRunner();
            var (exe, args) = ExitNonZero();

            // Act
            bool result = runner.RunCommand(exe, args, false);

            // Assert
            Assert.False(result);
        }

        /// <summary>RunCommand returns false when the executable does not exist.</summary>
        [Fact]
        public void RunCommand_NonExistentExecutable_ReturnsFalse()
        {
            // Arrange
            var runner = new DefaultProcessRunner();
            string exe = "zzz-no-such-exe-" + Guid.NewGuid().ToString("N");

            // Act
            bool result = runner.RunCommand(exe, "--version", false);

            // Assert
            Assert.False(result);
        }

        /// <summary>RunCommand forwards stdout lines to the onOutput callback.</summary>
        [Fact]
        public void RunCommand_OnOutputReceivesStdoutLines()
        {
            // Arrange
            var runner = new DefaultProcessRunner();
            string marker = "hello-stdout";
            var (exe, args) = Echo(marker);
            var outputs = new ConcurrentBag<string>();
            using var outputReceived = new ManualResetEventSlim(false);

            // Act
            bool result = runner.RunCommand(exe, args, false, line =>
            {
                outputs.Add(line);
                if (line.Contains(marker, StringComparison.Ordinal))
                {
                    outputReceived.Set();
                }
            });

            // Assert
            Assert.True(result);
            Assert.True(outputReceived.Wait(TimeSpan.FromSeconds(5)), "Timed out waiting for stdout output.");
            Assert.Contains(outputs, s => s.Contains(marker, StringComparison.Ordinal));
        }

        /// <summary>RunCommand forwards stderr lines to the onOutput callback.</summary>
        [Fact]
        public void RunCommand_OnOutputReceivesStderrLines()
        {
            // Arrange
            var runner = new DefaultProcessRunner();
            string marker = "hello-stderr";
            var (exe, args) = WriteToStderr(marker);
            var outputs = new ConcurrentBag<string>();
            using var outputReceived = new ManualResetEventSlim(false);

            // Act
            bool result = runner.RunCommand(exe, args, false, line =>
            {
                outputs.Add(line);
                if (line.Contains(marker, StringComparison.Ordinal))
                {
                    outputReceived.Set();
                }
            });

            // Assert
            Assert.True(result);
            Assert.True(outputReceived.Wait(TimeSpan.FromSeconds(5)), "Timed out waiting for stderr output.");
            Assert.Contains(outputs, s => s.Contains(marker, StringComparison.Ordinal));
        }

        /// <summary>RunCommand with null onOutput completes without throwing and returns success.</summary>
        [Fact]
        public void RunCommand_OnOutputNull_DoesNotThrow()
        {
            // Arrange
            var runner = new DefaultProcessRunner();

            // Act
            bool result = runner.RunCommand("dotnet", "--version", false, null);

            // Assert
            Assert.True(result);
        }

        /// <summary>RunCommandWithOutput returns non-empty stdout for dotnet --version.</summary>
        [Fact]
        public void RunCommandWithOutput_ReturnsStdoutFromDotnetVersion()
        {
            // Arrange
            var runner = new DefaultProcessRunner();

            // Act
            string output = runner.RunCommandWithOutput("dotnet", "--version").Trim();

            // Assert
            Assert.False(string.IsNullOrWhiteSpace(output));
            Assert.True(char.IsDigit(output[0]));
        }

        /// <summary>RunCommandWithOutput returns empty string for a non-existent executable.</summary>
        [Fact]
        public void RunCommandWithOutput_NonExistentExecutable_ReturnsEmpty()
        {
            // Arrange
            var runner = new DefaultProcessRunner();
            string exe = "zzz-no-such-exe-" + Guid.NewGuid().ToString("N");

            // Act
            string output = runner.RunCommandWithOutput(exe, "--version");

            // Assert
            Assert.Equal(string.Empty, output);
        }

        /// <summary>RunCommandWithOutput returns stdout even when the process exits non-zero.</summary>
        [Fact]
        public void RunCommandWithOutput_ExitNonZero_StillReturnsStdout()
        {
            // Arrange
            var runner = new DefaultProcessRunner();
            string marker = "hello-output";
            string exe;
            string args;

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                exe = "cmd";
                args = $"/c echo {marker} & exit 1";
            }
            else
            {
                exe = "sh";
                args = $"-c \"echo {marker}; exit 1\"";
            }

            // Act
            string output = runner.RunCommandWithOutput(exe, args).Trim();

            // Assert
            Assert.Contains(marker, output);
        }

        /// <summary>RunCommandWithOutput writes standard input and returns echoed output.</summary>
        [Fact]
        public void RunCommandWithOutput_WithStandardInput_EchoesInput()
        {
            // Arrange
            var runner = new DefaultProcessRunner();
            // findstr requires at least one non-empty line to avoid hanging.
            string input = "stdin-echo";
            string exe;
            string args;

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                exe = "findstr";
                args = ".";
            }
            else
            {
                exe = "cat";
                args = string.Empty;
            }

            // Act
            string output = runner.RunCommandWithOutput(exe, args, input);

            // Assert
            Assert.Contains(input, output);
        }

        /// <summary>RunAndCapture returns trimmed stdout for dotnet --version.</summary>
        [Fact]
        public void RunAndCapture_ReturnsTrimmedStdout()
        {
            // Arrange
            var runner = new DefaultProcessRunner();

            // Act
            string output = runner.RunAndCapture("dotnet", "--version");

            // Assert
            Assert.False(string.IsNullOrWhiteSpace(output));
            Assert.True(char.IsDigit(output[0]));
            Assert.Equal(output.Trim(), output);
        }

        /// <summary>RunAndCapture returns empty string for a non-existent executable.</summary>
        [Fact]
        public void RunAndCapture_NonExistentExecutable_ReturnsEmpty()
        {
            // Arrange
            var runner = new DefaultProcessRunner();
            string exe = "zzz-no-such-exe-" + Guid.NewGuid().ToString("N");

            // Act
            string output = runner.RunAndCapture(exe, "--version");

            // Assert
            Assert.Equal(string.Empty, output);
        }

        /// <summary>RunAndCapture does not capture stderr output.</summary>
        [Fact]
        public void RunAndCapture_StderrOnly_ReturnsEmptyOrWhitespace()
        {
            // Arrange
            var runner = new DefaultProcessRunner();
            var (exe, args) = WriteToStderr("stderr-only");

            // Act
            string output = runner.RunAndCapture(exe, args);

            // Assert
            Assert.True(string.IsNullOrWhiteSpace(output));
        }

        /// <summary>RunAndCapture with IEnumerable arguments returns trimmed stdout for dotnet --version.</summary>
        [Fact]
        public void RunAndCapture_EnumerableArgs_ReturnsTrimmedStdout()
        {
            // Arrange
            var runner = new DefaultProcessRunner();

            // Act
            string output = runner.RunAndCapture("dotnet", new[] { "--version" });

            // Assert
            Assert.False(string.IsNullOrWhiteSpace(output));
            Assert.True(char.IsDigit(output[0]));
            Assert.Equal(output.Trim(), output);
        }

        /// <summary>RunAndCapture with IEnumerable arguments returns empty string for a non-existent executable.</summary>
        [Fact]
        public void RunAndCapture_EnumerableArgs_NonExistentExecutable_ReturnsEmpty()
        {
            // Arrange
            var runner = new DefaultProcessRunner();
            string exe = "zzz-no-such-exe-" + Guid.NewGuid().ToString("N");

            // Act
            string output = runner.RunAndCapture(exe, new[] { "--version" });

            // Assert
            Assert.Equal(string.Empty, output);
        }

        /// <summary>RunAndCapture with IEnumerable arguments does not capture stderr output.</summary>
        [Fact]
        public void RunAndCapture_EnumerableArgs_StderrOnly_ReturnsEmptyOrWhitespace()
        {
            // Arrange
            var runner = new DefaultProcessRunner();
            string exe;
            string[] args;
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                exe = "cmd";
                args = new[] { "/c", "echo stderr-only 1>&2" };
            }
            else
            {
                exe = "sh";
                args = new[] { "-c", "echo stderr-only >&2" };
            }

            // Act
            string output = runner.RunAndCapture(exe, args);

            // Assert
            Assert.True(string.IsNullOrWhiteSpace(output));
        }


        private static (string exe, string args) Echo(string text)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return ("cmd", $"/c echo {text}");
            }

            return ("sh", $"-c \"echo {text}\"");
        }

        private static (string exe, string args) WriteToStderr(string text)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                // Use cmd to reliably and quickly write to stderr across environments
                return ("cmd", $"/c echo {text} 1>&2");
            }

            return ("sh", $"-c \"echo {text} >&2\"");
        }

        private static (string exe, string args) ExitNonZero()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return ("cmd", "/c exit 1");
            }

            return ("sh", "-c \"exit 1\"");
        }
    }
}
