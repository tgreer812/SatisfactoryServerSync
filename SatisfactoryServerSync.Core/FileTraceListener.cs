using System.Diagnostics;
using System.Text;

namespace SatisfactoryServerSync.Core;

/// <summary>
/// A trace listener that writes to a file with automatic log rotation
/// </summary>
public class FileTraceListener : TraceListener
{
    private readonly string _logFilePath;
    private readonly object _lock = new object();
    private const int MaxLogFileSizeBytes = 10 * 1024 * 1024; // 10MB
    private const int MaxLogFiles = 5;

    public FileTraceListener(string logFilePath)
    {
        _logFilePath = logFilePath ?? throw new ArgumentNullException(nameof(logFilePath));
        
        // Ensure directory exists
        var directory = Path.GetDirectoryName(_logFilePath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }
    }

    public override void Write(string? message)
    {
        if (message == null) return;
        WriteToFile(message);
    }

    public override void WriteLine(string? message)
    {
        if (message == null) return;
        WriteToFile(message + Environment.NewLine);
    }

    public override void TraceEvent(TraceEventCache? eventCache, string source, TraceEventType eventType, int id, string? message)
    {
        if (message == null) return;
        
        var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
        var formattedMessage = $"[{timestamp}] [{eventType}] [{source}] {message}{Environment.NewLine}";
        WriteToFile(formattedMessage);
    }

    public override void TraceEvent(TraceEventCache? eventCache, string source, TraceEventType eventType, int id, string? format, params object?[]? args)
    {
        if (format == null) return;
        
        var message = args?.Length > 0 ? string.Format(format, args) : format;
        TraceEvent(eventCache, source, eventType, id, message);
    }

    private void WriteToFile(string message)
    {
        lock (_lock)
        {
            try
            {
                // Check if log rotation is needed
                if (File.Exists(_logFilePath))
                {
                    var fileInfo = new FileInfo(_logFilePath);
                    if (fileInfo.Length > MaxLogFileSizeBytes)
                    {
                        RotateLogFiles();
                    }
                }

                File.AppendAllText(_logFilePath, message, Encoding.UTF8);
            }
            catch (Exception ex)
            {                // Write to event log if file logging fails
                try
                {
                    if (OperatingSystem.IsWindows())
                    {
                        EventLog.WriteEntry("SatisfactoryServerSync", 
                            $"Failed to write to log file {_logFilePath}: {ex.Message}", 
                            EventLogEntryType.Warning);
                    }
                }
                catch
                {
                    // If all else fails, write to console
                    Console.WriteLine($"[LOG ERROR] Failed to write to log file: {ex.Message}");
                }
            }
        }
    }

    private void RotateLogFiles()
    {
        try
        {
            var directory = Path.GetDirectoryName(_logFilePath);
            var fileName = Path.GetFileNameWithoutExtension(_logFilePath);
            var extension = Path.GetExtension(_logFilePath);

            // Delete the oldest log file if it exists
            var oldestLogFile = Path.Combine(directory!, $"{fileName}.{MaxLogFiles}{extension}");
            if (File.Exists(oldestLogFile))
            {
                File.Delete(oldestLogFile);
            }

            // Rotate existing log files
            for (int i = MaxLogFiles - 1; i >= 1; i--)
            {
                var sourceFile = Path.Combine(directory!, $"{fileName}.{i}{extension}");
                var targetFile = Path.Combine(directory!, $"{fileName}.{i + 1}{extension}");
                
                if (File.Exists(sourceFile))
                {
                    File.Move(sourceFile, targetFile);
                }
            }

            // Move current log file to .1
            var firstRotatedFile = Path.Combine(directory!, $"{fileName}.1{extension}");
            File.Move(_logFilePath, firstRotatedFile);
        }
        catch (Exception ex)
        {
            // Log rotation failed, but we don't want to stop the application
            Console.WriteLine($"[LOG WARNING] Log rotation failed: {ex.Message}");
        }
    }

    protected override void Dispose(bool disposing)
    {
        // Nothing to dispose for file operations
        base.Dispose(disposing);
    }
}
