using System.ComponentModel;
using System.Diagnostics;

namespace Oleander.Assembly.Versioning.ExternalProcesses;

internal class ExternalProcess(string exeName, string arguments, string? workingDirectory = null)
{
    private readonly string _workingDirectory = workingDirectory ?? Directory.GetCurrentDirectory();

    public ExternalProcessResult Start()
    {
        var epr = new ExternalProcessResult(exeName, arguments);

        var p = new Process
        {
            StartInfo =
                {
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    FileName = exeName,
                    Arguments = arguments,
                    CreateNoWindow = true,
                    ErrorDialog = false, 
                    WorkingDirectory = this._workingDirectory
                }
        };

        p.EnableRaisingEvents = true;

        try
        {
            if (!p.Start())
            {
                epr.ExitCode = -1;
                epr.StandardErrorOutput = $"Process '{p.MainWindowTitle}' did not start!";
                return epr;
            }
        }
        catch (Win32Exception ex)
        {
            epr.Win32ExitCode = (Win32ExitCodes)ex.NativeErrorCode;
            epr.ExitCode = ex.NativeErrorCode;
            epr.StandardErrorOutput = $"An error occurred while starting the process! ({ex.Message})";
            return epr;
        }
        catch (Exception ex)
        {
            epr.ExitCode = -1;
            epr.StandardErrorOutput = $"An error occurred while starting the process! ({ex.Message})";
            return epr;
        }

        epr.StandardOutput = p.StandardOutput.ReadToEnd();
        epr.StandardErrorOutput = p.StandardError.ReadToEnd();

        if (!p.HasExited && !p.WaitForExit(3000))
        {
            var p1 = p;
            var t = Task.Run(() =>
            {
                try
                {
                    epr.ExitCode = p1.ExitCode;
                    p1.Close();
                    p1.Dispose();
                }
                catch (Exception)
                {
                    //
                }
            });

            if (!t.Wait(1500))
            {
                try
                {
                    epr.ExitCode = -2;
                    epr.StandardErrorOutput = $"Try to kill the process {p.Id} because there is no response!";
                    p.Kill();
                }
                catch (Exception ex)
                {
                    epr.ExitCode = -1;
                    epr.StandardErrorOutput = $"An error occurred while killing the process! ({ex.Message})";
                }
            }

            return epr;
        }

        epr.ExitCode = p.ExitCode;
        p.Close();
        p.Dispose();

        if (epr.ExitCode == 0)
        {
            return epr;
        }

        if (string.IsNullOrWhiteSpace(epr.StandardErrorOutput)) epr.StandardErrorOutput = epr.StandardOutput;

        return epr;
    }
}