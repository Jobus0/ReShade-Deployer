using System;
using System.IO;
using System.IO.Pipes;
using System.Runtime.InteropServices;
using System.Security.Cryptography; // Needed for hashing
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace ReShadeDeployer;

public static class SingleInstanceManager
{
    private static Mutex? _mutex;
    public static Action<string[]>? OnArgumentsReceived;
    
    [DllImport("user32.dll")]
    private static extern bool AllowSetForegroundWindow(int dwProcessId);

    // Generate the ID dynamically based on the EXE path to support multiple installations of the app
    private static string GetUniqueAppId()
    {
        string? exePath = Environment.ProcessPath;

        // Fallback if something goes wrong
        if (string.IsNullOrEmpty(exePath))
            return "ReShadeDeployer" + typeof(App).Assembly.GetName().Version;

        // Cannot use backslashes in Mutex names, so hash the path
        using var hasher = MD5.Create();
        byte[] data = hasher.ComputeHash(Encoding.UTF8.GetBytes(exePath.ToLowerInvariant()));
        
        return Convert.ToHexString(data);
    }

    public static bool IsFirstInstance()
    {
        string appId = GetUniqueAppId();

        _mutex = new Mutex(true, $"Local\\{appId}", out bool createdNew);

        if (!createdNew)
        {
            AllowSetForegroundWindow(-1); // To allow the original instance to take foreground
            return false;
        }

        _ = Task.Run(() => StartNamedPipeServer(appId));
        return true;
    }

    private static async Task StartNamedPipeServer(string appId)
    {
        while (true)
        {
            try
            {
                await using var server = new NamedPipeServerStream(
                    appId,
                    PipeDirection.In,
                    1,
                    PipeTransmissionMode.Byte,
                    PipeOptions.Asynchronous);

                await server.WaitForConnectionAsync();

                using var reader = new StreamReader(server);
                var json = await reader.ReadToEndAsync();

                if (!string.IsNullOrEmpty(json))
                {
                    var args = JsonSerializer.Deserialize<string[]>(json);
                    if (args != null)
                        Application.Current.Dispatcher.Invoke(() => { OnArgumentsReceived?.Invoke(args); });
                }
            }
            catch (Exception) { /* Ignore */ }
        }
    }

    public static void SendArgumentsToFirstInstance(string[] args)
    {
        try
        {
            string appId = GetUniqueAppId();
            using var client = new NamedPipeClientStream(".", appId, PipeDirection.Out);
            client.Connect(1000);

            using var writer = new StreamWriter(client);
            var json = JsonSerializer.Serialize(args);
            writer.Write(json);
            writer.Flush();
        }
        catch (Exception) { }
    }

    public static void Cleanup()
    {
        if (_mutex != null)
        {
            _mutex.ReleaseMutex();
            _mutex.Close();
            _mutex = null;
        }
    }
}