using System;
using System.IO;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Core;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace Jellyfin.Plugin.PersonalMALRatings.Services;

/// <summary>
/// Custom plugin logger that writes to plugin-specific log files
/// </summary>
public static class PluginLogger
{
    private static Logger? _fileLogger;
    private static bool _initialized = false;

    public static void Initialize(string pluginDataPath)
    {
        if (_initialized) return;

        try
        {
            // Create logs directory in plugin data path
            var logsPath = Path.Combine(pluginDataPath, "logs");
            Directory.CreateDirectory(logsPath);

            // Configure Serilog file logger
            _fileLogger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.File(
                    path: Path.Combine(logsPath, "mal-plugin-.log"),
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: 7,
                    outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz}] [{Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}")
                .CreateLogger();

            _initialized = true;
            LogToFile(LogLevel.Information, "PluginLogger", "Plugin file logging initialized at: {LogPath}", logsPath);
        }
        catch (Exception ex)
        {
            // If file logging fails, we'll fall back to standard Jellyfin logging
            System.Diagnostics.Debug.WriteLine($"Failed to initialize plugin file logging: {ex.Message}");
        }
    }

    public static void LogToFile(LogLevel level, string category, string message, params object[] args)
    {
        if (_fileLogger == null) return;

        try
        {
            var formattedMessage = args.Length > 0 ? string.Format(message, args) : message;
            var logMessage = $"[{category}] {formattedMessage}";

            switch (level)
            {
                case LogLevel.Trace:
                case LogLevel.Debug:
                    _fileLogger.Debug(logMessage);
                    break;
                case LogLevel.Information:
                    _fileLogger.Information(logMessage);
                    break;
                case LogLevel.Warning:
                    _fileLogger.Warning(logMessage);
                    break;
                case LogLevel.Error:
                    _fileLogger.Error(logMessage);
                    break;
                case LogLevel.Critical:
                    _fileLogger.Fatal(logMessage);
                    break;
            }
        }
        catch
        {
            // Silently fail - don't break the plugin if logging fails
        }
    }

    public static void LogToFile(LogLevel level, string category, Exception exception, string message, params object[] args)
    {
        if (_fileLogger == null) return;

        try
        {
            var formattedMessage = args.Length > 0 ? string.Format(message, args) : message;
            var logMessage = $"[{category}] {formattedMessage}";

            switch (level)
            {
                case LogLevel.Error:
                    _fileLogger.Error(exception, logMessage);
                    break;
                case LogLevel.Critical:
                    _fileLogger.Fatal(exception, logMessage);
                    break;
                default:
                    _fileLogger.Error(exception, logMessage);
                    break;
            }
        }
        catch
        {
            // Silently fail - don't break the plugin if logging fails
        }
    }

    public static void Dispose()
    {
        _fileLogger?.Dispose();
        _fileLogger = null;
        _initialized = false;
    }
}