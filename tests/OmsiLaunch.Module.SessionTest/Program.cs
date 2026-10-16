using System.Diagnostics;
using System.Text;
using System.Text.Json;

// Test session program. The first stdin line is the context; settings.mode
// picks the behaviour:
//   ready   - notice, progress event, try to start another process, ready;
//             answers omsi-started; on stop writes stopped.txt in its data
//             directory and exits 0
//   slow    - never reports ready (prepare timeout)
//   crash   - exits 3 before ready
//   linger  - ready, then ignores stop (it is ended by the job)
var input = new StreamReader(Console.OpenStandardInput(), new UTF8Encoding(false));
var output = new StreamWriter(Console.OpenStandardOutput(), new UTF8Encoding(false)) { AutoFlush = true, NewLine = "\n" };
void Send(object value) => output.WriteLine(JsonSerializer.Serialize(value));

var first = input.ReadLine();
if (first is null) return 2;
using var context = JsonDocument.Parse(first);
var root = context.RootElement;
var settings = root.GetProperty("settings");
var mode = settings.TryGetProperty("mode", out var value) ? value.GetString() : "ready";
var data = root.GetProperty("data_directory").GetString()!;

if (mode == "crash") return 3;
Send(new { type = "notice", data = new { title = "Test\u0007notice", text = "first open of " + root.GetProperty("world").GetProperty("map").GetString() } });
Send(new { type = "event", name = "progress", data = new { percent = "100", schema = root.GetProperty("schema").GetString(), channel = root.GetProperty("channel").GetString() } });
// The job allows no second process.
string spawn;
try { using var child = Process.Start(new ProcessStartInfo("cmd.exe", "/c exit 0") { UseShellExecute = false, CreateNoWindow = true }); spawn = child is null ? "refused" : "started"; }
catch (Exception) { spawn = "refused"; }
Send(new { type = "event", name = "spawn", data = new { result = spawn } });
if (mode == "slow") { Thread.Sleep(Timeout.Infinite); return 0; }
Send(new { type = "ready" });

string? line;
while ((line = input.ReadLine()) is not null)
{
    using var message = JsonDocument.Parse(line);
    var type = message.RootElement.GetProperty("type").GetString();
    if (type == "omsi-started") Send(new { type = "event", name = "omsi", data = new { pid = message.RootElement.GetProperty("pid").GetInt32().ToString(System.Globalization.CultureInfo.InvariantCulture) } });
    if (type == "stop")
    {
        if (mode == "linger") { Thread.Sleep(Timeout.Infinite); return 0; }
        File.WriteAllText(Path.Combine(data, "stopped.txt"), "stopped");
        return 0;
    }
}
return 0;
