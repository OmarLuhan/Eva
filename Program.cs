using System.Diagnostics;

var text = "Hola. Esto es texto a voz neuronal, ejecutado de forma local en Arch Linux.";
const string model = "./piper-voices/es_MX-claude-high.onnx"; 

// Canaliza la salida de Piper directamente al reproductor de audio del sistema
using var piper = new Process();
piper.StartInfo = new ProcessStartInfo
{
    FileName = "piper-tts",
    Arguments = $"--model {model} --output_raw",
    RedirectStandardInput = true,
    RedirectStandardOutput = true,
    UseShellExecute = false,
    CreateNoWindow = true
};

using var aplay = new Process();
aplay.StartInfo = new ProcessStartInfo
{
    FileName = "ffplay",
    Arguments = "-f s16le -ar 22050 -ch_layout mono -nodisp -autoexit -loglevel quiet -",
    RedirectStandardInput = true,
    UseShellExecute = false,
    CreateNoWindow = true
};

piper.Start();
aplay.Start();

// Envía el texto a Piper y cierra el flujo de entrada para procesar
await using (var writer = piper.StandardInput)
{
    await writer.WriteLineAsync(text);
}

// Envía el flujo de audio de Piper hacia las bocinas (aplay)
await piper.StandardOutput.BaseStream.CopyToAsync(aplay.StandardInput.BaseStream);
aplay.StandardInput.Close();

await aplay.WaitForExitAsync();