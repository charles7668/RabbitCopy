using System.Diagnostics;

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

    public Task StartCopy(string srcDir, string destDir, IEnumerable<string> files, RoboCopyOptions options,
        CancellationToken cancellationToken)
    {
        var fileArgs = string.Join(" ", files.Select(f => $"\"{f}\""));
        var roboCopyArgs = options.ToArgsString();
        ProcessStartInfo startInfo = new()
        {
            FileName = "robocopy",
            Arguments = $"\"{srcDir}\" \"{destDir}\" {fileArgs} {roboCopyArgs}",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        var proc = new Process
        {
            StartInfo = startInfo
        };
        proc.OutputDataReceived += (_, args) =>
        {
            if (!string.IsNullOrWhiteSpace(args.Data))
                OnOutputReceive?.Invoke(args.Data);
        };
        proc.ErrorDataReceived += (_, args) =>
        {
            if (!string.IsNullOrWhiteSpace(args.Data))
                OnErrorReceive?.Invoke(args.Data);
        };
        proc.Start();
        Task.Factory.StartNew(() =>
        {
            while (!proc.HasExited)
            {
                if (cancellationToken.IsCancellationRequested)
                    proc.Kill();
                Thread.Sleep(100);
            }
        }, TaskCreationOptions.LongRunning);
        proc.BeginOutputReadLine();
        proc.BeginErrorReadLine();
        proc.WaitForExit();
        return Task.CompletedTask;
    }
}