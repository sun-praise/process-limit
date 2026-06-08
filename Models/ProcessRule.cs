namespace ProcessLimit.Models;

public class ProcessRule
{
    public string ProcessName { get; set; } = string.Empty;
    public long MaxMemoryBytes { get; set; }
    public bool IsEnabled { get; set; } = true;

    public string MaxMemoryDisplay
    {
        get
        {
            if (MaxMemoryBytes <= 0) return "无限制";
            if (MaxMemoryBytes >= 1024 * 1024 * 1024)
                return $"{MaxMemoryBytes / (1024.0 * 1024 * 1024):F1} GB";
            return $"{MaxMemoryBytes / (1024.0 * 1024):F0} MB";
        }
    }
}
