using Godot;
using LiteEntitySystem;
using LiteNetLib;
using System;
using System.Runtime.CompilerServices;

public static class Debug
{
    public class Logger : ILogger, INetLogger
    {
        private static readonly Lazy<Logger> _lazy = new Lazy<Logger>(() => new Logger());
        public static Logger Instance => _lazy.Value;
        private Logger() { }

        void ILogger.Log(string log)
        {
            GD.Print(log);
        }

        void ILogger.LogError(string log)
        {
            GD.Print(log);
        }

        void ILogger.LogWarning(string log)
        {
            GD.Print(log);
        }

        void INetLogger.WriteNet(NetLogLevel level, string str, params object[] args)
        {
            GD.Print($"{level}, {str}, {args}");
        }
    }

    public static void Log(string message, [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0)
    {
#if DEBUG
        GD.Print($"{filePath}:{lineNumber}: {message}");
#endif
    }
    public static void Assert(bool condition, string message = null)
    {
#if DEBUG
        if (!condition)
        {
            GD.PrintErr("Assertion failed: " + message);
            throw new ApplicationException(message);
        }
#endif
    }

    internal static void Error(string message)
    {
#if DEBUG
        GD.PrintErr(message);
#endif
    }

}