using System.Diagnostics;
using System.Text;
using System.Text.Json;
using WinHome.Interfaces;
using WinHome.Models.Plugins;

namespace WinHome.Services.Plugins
{
    public class PluginRunner : IPluginRunner
    {
        private readonly ILogger _logger;
        private readonly IRuntimeResolver _runtimeResolver;

        public PluginRunner(ILogger logger, IRuntimeResolver runtimeResolver)
        {
            _logger = logger;
            _runtimeResolver = runtimeResolver;
        }

        public async Task<PluginResult> ExecuteAsync(PluginManifest plugin, string command, object? args, object? context)
        {
            var (fileName, arguments) = BuildProcessStartInfo(plugin);
            
            var request = new PluginRequest
            {
                Command = command,
                Args = args,
                Context = context
            };

            var startInfo = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                WorkingDirectory = plugin.DirectoryPath,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            // Inject Environment Variables if needed
            startInfo.Environment["WINHOME_PLUGIN_NAME"] = plugin.Name;

            using var process = new Process { StartInfo = startInfo };
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            
            try
            {
                _logger.LogInfo($"[PluginRunner] Starting {plugin.Name}: {fileName} {arguments}");
                if (!process.Start())
                {
                    return new PluginResult { Success = false, Error = "Failed to start plugin process." };
                }

                // Handling Stderr asynchronously to avoid deadlocks and log immediately
                process.ErrorDataReceived += (sender, e) => 
                {
                    if (!string.IsNullOrEmpty(e.Data)) _logger.LogWarning($"[{plugin.Name}][STDERR] {e.Data}");
                };
                process.BeginErrorReadLine();

                // 1. Send Request
                string jsonRequest = JsonSerializer.Serialize(request);
                await process.StandardInput.WriteLineAsync(jsonRequest.AsMemory(), cts.Token);
                process.StandardInput.Close(); // Close Stdin to signal EOF to the plugin if it reads until EOF

                // 2. Read Response with Size Limit (10MB) and Timeout
                var outputBuilder = new StringBuilder();
                char[] buffer = new char[4096];
                int totalRead = 0;
                int maxBytes = 10 * 1024 * 1024; // 10 MB

                while (true) 
                {
                    int read = await process.StandardOutput.ReadAsync(buffer.AsMemory(), cts.Token);
                    if (read == 0) break;
                    
                    totalRead += read;
                    if (totalRead > maxBytes) 
                    {
                        process.Kill();
                        throw new InvalidOperationException("Plugin output exceeded size limit (10MB).");
                    }
                    
                    outputBuilder.Append(buffer, 0, read);
                }
                
                await process.WaitForExitAsync(cts.Token);

                if (process.ExitCode != 0)
                {
                    return new PluginResult { Success = false, Error = $"Plugin process exited with code {process.ExitCode}" };
                }

                string output = outputBuilder.ToString();

                if (string.IsNullOrWhiteSpace(output))
                {
                    return new PluginResult { Success = false, Error = "Plugin returned empty response." };
                }

                try
                {
                    // Attempt robust parsing: Find the last line that looks like JSON
                    var lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                    string jsonLine = lines.LastOrDefault(l => l.StartsWith("{") && l.EndsWith("}")) ?? output;

                    var result = JsonSerializer.Deserialize<PluginResult>(jsonLine);
                    return result ?? new PluginResult { Success = false, Error = "Deserialized null result." };
                }
                catch (JsonException ex)
                {
                    return new PluginResult { Success = false, Error = $"Invalid JSON response: {ex.Message}. Output: {output}" };
                }
            }
            catch (OperationCanceledException)
            {
                try { process.Kill(); } catch { }
                return new PluginResult { Success = false, Error = "Plugin timed out after 30 seconds." };
            }
            catch (Exception ex)
            {
                try { if (!process.HasExited) process.Kill(); } catch { }
                return new PluginResult { Success = false, Error = $"Runner Exception: {ex.Message}" };
            }
        }

        private (string FileName, string Arguments) BuildProcessStartInfo(PluginManifest plugin)
        {
            string mainPath = Path.Combine(plugin.DirectoryPath, plugin.Main);

            switch (plugin.Type.ToLower())
            {
                case "python":
                    // uv run --quiet script.py
                    var uvPath = _runtimeResolver.Resolve("uv");
                    return (uvPath, $"run --quiet \"{mainPath}\"");
                
                case "typescript":
                case "javascript":
                    // bun run script.ts
                    var bunPath = _runtimeResolver.Resolve("bun");
                    return (bunPath, $"run \"{mainPath}\"");
                
                case "executable":
                    return (mainPath, "");

                default:
                    throw new NotSupportedException($"Plugin type '{plugin.Type}' is not supported.");
            }
        }
    }
}