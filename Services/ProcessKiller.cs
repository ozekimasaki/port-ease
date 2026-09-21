using System.ComponentModel;
using System.Diagnostics;

namespace PortBan.Services;

internal static class ProcessKiller
{
    public static void Kill(int pid)
    {
        if (pid <= 0)
            throw new InvalidOperationException("プロセスを特定できないため終了できません。");

        if (pid <= 4)
            throw new InvalidOperationException("システムプロセスは終了できません。");

        if (pid == Environment.ProcessId)
            throw new InvalidOperationException("ポート番自身は、トレイの「終了」から閉じてください。");

        try
        {
            using var process = Process.GetProcessById(pid);
            process.Kill(entireProcessTree: true);
            process.WaitForExit(5000);
        }
        catch (ArgumentException)
        {
            throw new InvalidOperationException("そのプロセスは既に終了しています。");
        }
        catch (Win32Exception ex) when (ex.NativeErrorCode is 5 or 1314)
        {
            throw new InvalidOperationException(DeniedMessage());
        }
        catch (UnauthorizedAccessException)
        {
            throw new InvalidOperationException(DeniedMessage());
        }
        catch (InvalidOperationException)
        {
            throw new InvalidOperationException("プロセスを終了できませんでした。");
        }
    }

    private static string DeniedMessage()
    {
        if (OperatingSystem.IsWindows())
            return "権限がありません。管理者として起動し直してください。";

        return "権限がありません。";
    }
}
