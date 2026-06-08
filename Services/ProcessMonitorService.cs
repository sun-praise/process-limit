using System.Diagnostics;
using ProcessLimit.Models;

namespace ProcessLimit.Services;

public class ProcessMonitorService : IDisposable
{
    private readonly JobObjectService _jobObjectService;
    private readonly ConfigService _configService;
    private readonly HashSet<int> _assignedPids = new();
    private readonly object _lock = new();
    private System.Threading.Timer? _timer;
    private List<ProcessRule> _rules = new();

    public event Action<string>? OnLog;

    public ProcessMonitorService()
    {
        _jobObjectService = new JobObjectService();
        _configService = new ConfigService();
    }

    public void Start()
    {
        _rules = _configService.LoadRules();
        _timer = new System.Threading.Timer(MonitorCallback, null, TimeSpan.Zero, TimeSpan.FromSeconds(3));
        Log("监控服务已启动");
    }

    public void Stop()
    {
        _timer?.Dispose();
        _timer = null;
        _jobObjectService.CleanupAll();
        _assignedPids.Clear();
        Log("监控服务已停止");
    }

    public void ReloadRules()
    {
        lock (_lock)
        {
            _rules = _configService.LoadRules();
            _assignedPids.Clear();
            _jobObjectService.CleanupAll();
        }
        Log("规则已重新加载");
    }

    public List<ProcessRule> GetRules() => _configService.LoadRules();

    public void SaveRules(List<ProcessRule> rules)
    {
        _configService.SaveRules(rules);
        lock (_lock) { _rules = new List<ProcessRule>(rules); }
        _assignedPids.Clear();
        _jobObjectService.CleanupAll();
        Log("规则已保存并重新应用");
    }

    public List<ProcessInfo> GetRunningProcesses()
    {
        var processes = Process.GetProcesses();
        var result = new List<ProcessInfo>();

        var processGroups = processes
            .GroupBy(p => p.ProcessName, StringComparer.OrdinalIgnoreCase)
            .OrderBy(g => g.Key);

        foreach (var group in processGroups)
        {
            try
            {
                var totalMemory = group.Sum(p => { try { return p.WorkingSet64; } catch { return 0L; } });
                var matchedRule = _rules.FirstOrDefault(r =>
                    r.ProcessName.Equals(group.Key, StringComparison.OrdinalIgnoreCase) && r.IsEnabled);

                result.Add(new ProcessInfo
                {
                    ProcessName = group.Key,
                    InstanceCount = group.Count(),
                    TotalMemoryBytes = totalMemory,
                    Rule = matchedRule,
                    IsLimited = matchedRule != null
                });
            }
            catch { }
        }

        return result;
    }

    private void MonitorCallback(object? state)
    {
        try
        {
            var activeRules = _rules.Where(r => r.IsEnabled).ToList();
            if (activeRules.Count == 0) return;

            foreach (var rule in activeRules)
            {
                var processes = Process.GetProcessesByName(rule.ProcessName);
                foreach (var process in processes)
                {
                    try
                    {
                        if (process.HasExited) continue;

                        lock (_lock)
                        {
                            if (_assignedPids.Contains(process.Id)) continue;
                        }

                        var success = _jobObjectService.ApplyLimit(process, rule);
                        if (success)
                        {
                            lock (_lock) { _assignedPids.Add(process.Id); }
                            Log($"已为 {rule.ProcessName} (PID:{process.Id}) 设置内存限制: {rule.MaxMemoryDisplay}");
                        }
                    }
                    catch { }
                }
            }
        }
        catch { }
    }

    private void Log(string message)
    {
        var timestamp = DateTime.Now.ToString("HH:mm:ss");
        OnLog?.Invoke($"[{timestamp}] {message}");
    }

    public void Dispose()
    {
        Stop();
    }
}

public class ProcessInfo
{
    public string ProcessName { get; set; } = string.Empty;
    public int InstanceCount { get; set; }
    public long TotalMemoryBytes { get; set; }
    public ProcessRule? Rule { get; set; }
    public bool IsLimited { get; set; }

    public string MemoryDisplay
    {
        get
        {
            if (TotalMemoryBytes >= 1024 * 1024 * 1024)
                return $"{TotalMemoryBytes / (1024.0 * 1024 * 1024):F1} GB";
            return $"{TotalMemoryBytes / (1024.0 * 1024):F1} MB";
        }
    }
}
