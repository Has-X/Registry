using System.IO.Pipes;
using System.Text;

namespace Registry_App;

/// <summary>Forwards .reg shell activations to the first interactive instance.</summary>
internal static class RegistryFileActivationRouter
{
    private const string PipeName = "Chromatic.Registry.RegFileActivation.v1";
    private static int _started;

    public static bool TryForwardToExistingInstance(string path)
    {
        try
        {
            using var client = new NamedPipeClientStream(".", PipeName, PipeDirection.Out);
            client.Connect(250);
            using var writer = new StreamWriter(client, new UTF8Encoding(false), leaveOpen: false);
            writer.Write(path);
            writer.Flush();
            return true;
        }
        catch (TimeoutException)
        {
            return false;
        }
        catch (IOException)
        {
            return false;
        }
    }

    public static void Start(Action<string> activationReceived)
    {
        if (Interlocked.Exchange(ref _started, 1) != 0)
        {
            return;
        }

        _ = Task.Run(async () =>
        {
            while (true)
            {
                using var server = new NamedPipeServerStream(PipeName, PipeDirection.In, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
                await server.WaitForConnectionAsync().ConfigureAwait(false);
                using var reader = new StreamReader(server, Encoding.UTF8, detectEncodingFromByteOrderMarks: false, leaveOpen: false);
                var path = await reader.ReadToEndAsync().ConfigureAwait(false);
                if (!string.IsNullOrWhiteSpace(path))
                {
                    activationReceived(path);
                }
            }
        });
    }
}
