using System.Net;
using System.Net.Sockets;
using DzLegal.App;
using DzLegal.App.Services.LocalModels;
using DzLegal.ModelProviders.Ollama;
using DzLegal.Storage;

if (args.Length != 1) throw new ArgumentException("Supply the absolute path to the pinned CPU ollama.exe.");
var root = Path.Combine(AppPaths.Default.Root, "model-setup-checks", Guid.NewGuid().ToString("N"));
using var occupied = new TcpListener(IPAddress.Loopback, 0);
occupied.Start();
var port = ((IPEndPoint)occupied.LocalEndpoint).Port;
using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(25));
var engine = new ManagedOllama(new Uri($"http://127.0.0.1:{port}"), new WindowsEngineLauncher(args[0], root));
var session = await engine.EnsureAsync(timeout.Token);
if (!session.IsManaged || session.Endpoint.Host != "127.0.0.1" || session.Endpoint.Port == port)
    throw new IOException("Managed loopback ownership check failed.");
using (var api = new LocalOllamaApi(session.Endpoint))
{
    if (!await api.IsCompatibleAsync(timeout.Token) || (await api.InstalledAsync(timeout.Token)).Count != 0)
        throw new IOException("Private engine readiness or empty model store check failed.");
}
engine.Dispose();
await Task.Delay(250);
if (System.Net.NetworkInformation.IPGlobalProperties.GetIPGlobalProperties().GetActiveTcpListeners()
    .Any(p => p.Address.Equals(IPAddress.Loopback) && p.Port == session.Endpoint.Port))
    throw new IOException("Owned listener remained bound after disposal.");
Console.WriteLine("PASS: occupied port preserved; private CPU server ready; owned listener closed on disposal.");
using var external = new ManagedOllama(new Uri("http://127.0.0.1:11434"), new WindowsEngineLauncher(args[0], root));
var reused = await external.EnsureAsync(timeout.Token);
if (reused.IsManaged) throw new IOException("Expected the existing pinned Ollama server for this laptop check.");
var choice = new OllamaModelChoice(new OllamaOptions(), null, external);
var status = await choice.ProbeAsync(timeout.Token);
if (status.State != DzLegal.Presentation.ViewModels.ModelState.Ready || choice.Options.Model != "gemma4:e4b" || choice.Options.Think != false)
    throw new IOException("Installed Gemma 4 selection check failed.");
Console.WriteLine("PASS: compatible existing server reused; installed Gemma 4 selected; think=false; no pull or inference.");