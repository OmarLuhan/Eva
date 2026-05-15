using System.Diagnostics;
using System.Text.Json;

Console.Write("> ");
var input = Console.ReadLine();
if (string.IsNullOrWhiteSpace(input)) return;

var response = await ReadOpcodesponse(input);
if (string.IsNullOrWhiteSpace(response)) return;

Console.WriteLine(response);

using var piper = new Process();
piper.StartInfo = new ProcessStartInfo
{
    FileName = "piper-tts",
    ArgumentList = { "--model", "./piper-voices/es_MX-claude-high.onnx", "--output_raw" },
    RedirectStandardInput = true,
    RedirectStandardOutput = true,
    UseShellExecute = false,
    CreateNoWindow = true
};

using var player = new Process();
player.StartInfo = new ProcessStartInfo
{
    FileName = "ffplay",
    ArgumentList = { "-f", "s16le", "-ar", "22050", "-ch_layout", "mono", "-nodisp", "-autoexit", "-loglevel", "quiet", "-" },
    RedirectStandardInput = true,
    UseShellExecute = false,
    CreateNoWindow = true
};

piper.Start();
player.Start();

await using (var writer = piper.StandardInput)
{
    await writer.WriteLineAsync(response);
}

await piper.StandardOutput.BaseStream.CopyToAsync(player.StandardInput.BaseStream);
player.StandardInput.Close();

await player.WaitForExitAsync();
return;

static async Task<string> ReadOpcodesponse(string message)
{
    var psi = new ProcessStartInfo
    {
        FileName = "opencode",
        ArgumentList = { "run", message, "-s", "ses_1d63e717fffe9Yko62healxMFW", "--format", "json" },
        RedirectStandardOutput = true,
        UseShellExecute = false,
        CreateNoWindow = true
    };

    using var proc = new Process();
    proc.StartInfo = psi;
    proc.Start();

    var sb = new System.Text.StringBuilder();
    while (await proc.StandardOutput.ReadLineAsync() is { } line)
    {
        if (string.IsNullOrWhiteSpace(line)) continue;
        using var doc = JsonDocument.Parse(line);
        var root = doc.RootElement;
        if (!root.TryGetProperty("type", out var type) || type.GetString() != "text") continue;
        var t = root.GetProperty("part").GetProperty("text").GetString();
        if (!string.IsNullOrWhiteSpace(t)) sb.Append(t);
    }

    await proc.WaitForExitAsync();
    return sb.ToString();
}
