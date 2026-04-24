
namespace SharpSim;

public static class LogHandler
{
    public enum LogLevel
    {
        Info = 0, Debug, Warn, Error
    }
    public delegate void LogMessageHandler(string message);
    public static event LogMessageHandler? LogInfoHandle = null;
    public static event LogMessageHandler? LogDebugHandle = null;
    public static event LogMessageHandler? LogWarnHandle = null;
    public static event LogMessageHandler? LogErrorHandle = null;

    public static void Info(string msg) => AddLog(LogLevel.Info, msg);
    public static void Debug(string msg) => AddLog(LogLevel.Debug, msg);
    public static void Warn(string msg) => AddLog(LogLevel.Warn, msg);
    public static void Error(string msg) => AddLog(LogLevel.Error, msg);

    private static void AddLog(LogLevel level, string msg)
    {
        string prefix = $"{"[" + level.ToString() + "]",-7} ({DateTime.Now.ToString("hh:mm:ss.ff")}) ";
        switch (level)
        {
            case LogLevel.Info:
                LogInfoHandle?.Invoke(msg);
                break;
            case LogLevel.Debug:
                LogDebugHandle?.Invoke(msg);
                break;
            case LogLevel.Warn:
                LogWarnHandle?.Invoke(msg);
                break;
            case LogLevel.Error:
                LogErrorHandle?.Invoke(msg);
                break;
        }
    }
}

