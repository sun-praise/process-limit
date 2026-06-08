using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using ProcessLimit.Models;
using ProcessLimit.Services;

namespace ProcessLimit;

public class MainViewModel : INotifyPropertyChanged, IDisposable
{
    private readonly ProcessMonitorService _monitor;
    private string _logText = string.Empty;
    private string _searchText = string.Empty;
    private ProcessInfo? _selectedProcess;
    private string _newRuleProcessName = string.Empty;
    private long _newRuleMemoryMB = 512;

    public ObservableCollection<ProcessInfo> Processes { get; } = new();
    public ObservableCollection<ProcessRule> Rules { get; } = new();

    public string LogText
    {
        get => _logText;
        set { _logText = value; OnPropertyChanged(); }
    }

    public string SearchText
    {
        get => _searchText;
        set { _searchText = value; OnPropertyChanged(); RefreshProcesses(); }
    }

    public ProcessInfo? SelectedProcess
    {
        get => _selectedProcess;
        set { _selectedProcess = value; OnPropertyChanged(); }
    }

    public string NewRuleProcessName
    {
        get => _newRuleProcessName;
        set { _newRuleProcessName = value; OnPropertyChanged(); }
    }

    public long NewRuleMemoryMB
    {
        get => _newRuleMemoryMB;
        set { _newRuleMemoryMB = value; OnPropertyChanged(); }
    }

    public ICommand RefreshCommand { get; }
    public ICommand AddRuleCommand { get; }
    public ICommand DeleteRuleCommand { get; }
    public ICommand ToggleRuleCommand { get; }
    public ICommand ApplyToSelectedCommand { get; }

    public MainViewModel()
    {
        _monitor = new ProcessMonitorService();
        _monitor.OnLog += msg =>
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                LogText += msg + Environment.NewLine;
                if (LogText.Length > 10000)
                    LogText = LogText[^5000..];
            });
        };

        RefreshCommand = new RelayCommand(_ => RefreshProcesses());
        AddRuleCommand = new RelayCommand(_ => AddRule());
        DeleteRuleCommand = new RelayCommand(DeleteRule);
        ToggleRuleCommand = new RelayCommand(ToggleRule);
        ApplyToSelectedCommand = new RelayCommand(_ => ApplyToSelected());

        LoadRules();
        RefreshProcesses();
        _monitor.Start();
    }

    private ProcessRule? _selectedRule;
    public ProcessRule? SelectedRule
    {
        get => _selectedRule;
        set { _selectedRule = value; OnPropertyChanged(); }
    }

    private void RefreshProcesses()
    {
        var processes = _monitor.GetRunningProcesses();
        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            processes = processes.Where(p =>
                p.ProcessName.Contains(SearchText, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        Application.Current.Dispatcher.Invoke(() =>
        {
            Processes.Clear();
            foreach (var p in processes.Take(200))
                Processes.Add(p);
        });
    }

    private void LoadRules()
    {
        var rules = _monitor.GetRules();
        Rules.Clear();
        foreach (var r in rules) Rules.Add(r);
    }

    private void AddRule()
    {
        if (string.IsNullOrWhiteSpace(NewRuleProcessName) || NewRuleMemoryMB <= 0) return;

        var name = NewRuleProcessName.EndsWith(".exe", StringComparison.OrdinalIgnoreCase)
            ? NewRuleProcessName[..^4]
            : NewRuleProcessName;

        var rule = new ProcessRule
        {
            ProcessName = name,
            MaxMemoryBytes = NewRuleMemoryMB * 1024 * 1024,
            IsEnabled = true
        };

        var existing = Rules.FirstOrDefault(r => r.ProcessName.Equals(name, StringComparison.OrdinalIgnoreCase));
        if (existing != null)
        {
            existing.MaxMemoryBytes = rule.MaxMemoryBytes;
            existing.IsEnabled = true;
        }
        else
        {
            Rules.Add(rule);
        }

        SaveAllRules();
        NewRuleProcessName = string.Empty;
    }

    private void DeleteRule(object? obj)
    {
        if (obj is ProcessRule rule)
        {
            Rules.Remove(rule);
            SaveAllRules();
        }
    }

    private void ToggleRule(object? obj)
    {
        if (obj is ProcessRule rule)
        {
            rule.IsEnabled = !rule.IsEnabled;
            SaveAllRules();
        }
    }

    private void ApplyToSelected()
    {
        if (SelectedProcess == null) return;
        NewRuleProcessName = SelectedProcess.ProcessName;
    }

    private void SaveAllRules()
    {
        _monitor.SaveRules(Rules.ToList());
    }

    public void Dispose()
    {
        _monitor.Dispose();
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

public class RelayCommand : ICommand
{
    private readonly Action<object?> _execute;
    private readonly Predicate<object?>? _canExecute;

    public RelayCommand(Action<object?> execute, Predicate<object?>? canExecute = null)
    {
        _execute = execute;
        _canExecute = canExecute;
    }

    public bool CanExecute(object? parameter) => _canExecute?.Invoke(parameter) ?? true;
    public void Execute(object? parameter) => _execute(parameter);
    public event EventHandler? CanExecuteChanged { add { } remove { } }
}
