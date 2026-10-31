using System.Diagnostics;
using System.IO;
using System.Text;

namespace RabbitCopy.RoboCopyModule;

public class RoboCopy
{
    public RoboCopy()
    {
    }

    public RoboCopy(Action<string>? onOutputReceive, Action<string>? onErrorReceive)
    {
        OnOutputReceive = onOutputReceive;
        OnErrorReceive = onErrorReceive;
    }

    private Action<string>? OnOutputReceive { get; }
    private Action<string>? OnErrorReceive { get; }

    public async Task StartCopy(string srcDir, string destDir, IEnumerable<string> files, RoboCopyOptions options,
        CancellationToken cancellationToken)
    {
        var fileArgs = string.Join(" ", files.Select(f => $"\"{f}\""));
        var roboCopyArgs = options.ToArgsString();
        var logPath = Path.Combine(Path.GetTempPath(), $"RabbitCopy-{Guid.NewGuid():N}.log");
        ProcessStartInfo startInfo = new()
        {
            FileName = "robocopy",
            Arguments = $"\"{srcDir}\" \"{destDir}\" {fileArgs} {roboCopyArgs} /UNILOG:\"{logPath}\"",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            WorkingDirectory = AppContext.BaseDirectory
        };
        using var proc = new Process
        {
            StartInfo = startInfo
        };
        proc.ErrorDataReceived += (_, args) =>
        {
            if (!string.IsNullOrWhiteSpace(args.Data))
                OnErrorReceive?.Invoke(args.Data);
        };
        proc.Start();
        using var cancellationRegistration = cancellationToken.Register(() =>
        {
            try
            {
                if (!proc.HasExited)
                    proc.Kill(true);
            }
            catch (InvalidOperationException)
            {
                // The process may have already exited between HasExited and Kill.
            }
        });
        // Drain stdout so a full pipe cannot block RoboCopy.
        proc.BeginOutputReadLine();
        proc.BeginErrorReadLine();
        try
        {
            await ReadLogUntilExitAsync(proc, logPath, cancellationToken);
            await proc.WaitForExitAsync();
            proc.WaitForExit(); // Flush asynchronous stderr callbacks.
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            await proc.WaitForExitAsync();
            throw;
        }
        finally
        {
            try { File.Delete(logPath); }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }
    }
    private async Task ReadLogUntilExitAsync(Process proc, string logPath, CancellationToken cancellationToken)
    {
        StreamReader? reader = null;
        var pending = string.Empty;
        var buffer = new char[4096];
        DateTime? missingSince = null;
        try
        {
            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (reader == null)
                {
                    try
                    {
                        reader = new StreamReader(new FileStream(logPath, FileMode.Open, FileAccess.Read,
                            FileShare.ReadWrite | FileShare.Delete), Encoding.Unicode, true);
                    }
                    catch (FileNotFoundException)
                    {
                        // A failed RoboCopy invocation may never create a log.
                        if (proc.HasExited)
                        {
                            missingSince ??= DateTime.UtcNow;
                            if (DateTime.UtcNow - missingSince >= TimeSpan.FromSeconds(1)) break;
                        }
                    }
                    catch (IOException)
                    {
                        // Retry sharing conflicts even after the process exits.
                    }
                }

                // Snapshot before reading: an exit during the read requires another drain.
                var exited = proc.HasExited;
                if (reader != null)
                {
                    int count;
                    while ((count = await reader.ReadAsync(buffer.AsMemory(), cancellationToken)) > 0)
                    {
                        var result = SplitLines(pending, new string(buffer, 0, count), false);
                        pending = result.Pending;
                        foreach (var line in result.Lines) OnOutputReceive?.Invoke(line);
                    }
                    if (exited)
                    {
                        var result = SplitLines(pending, string.Empty, true);
                        foreach (var line in result.Lines) OnOutputReceive?.Invoke(line);
                        break;
                    }
                }
                await Task.Delay(100, cancellationToken);
            }
        }
        finally
        {
            reader?.Dispose();
        }
    }

    internal static (string Pending, List<string> Lines) SplitLines(string pending, string chunk, bool flush)
    {
        var text = pending + chunk;
        var lines = new List<string>();
        var start = 0;
        for (var i = 0; i < text.Length; i++)
        {
            if (text[i] != '\r' && text[i] != '\n') continue;
            var line = text.Substring(start, i - start);
            if (!string.IsNullOrWhiteSpace(line)) lines.Add(line);
            start = i + 1;
        }
        var remainder = text.Substring(start);
        if (flush)
        {
            if (!string.IsNullOrWhiteSpace(remainder)) lines.Add(remainder);
            remainder = string.Empty;
        }
        return (remainder, lines);
    }
}
