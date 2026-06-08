using System.Diagnostics;
using System.Runtime.InteropServices;
using ProcessLimit.Helpers;
using ProcessLimit.Models;

namespace ProcessLimit.Services;

public class JobObjectService
{
    private readonly Dictionary<string, IntPtr> _jobHandles = new(StringComparer.OrdinalIgnoreCase);
    private readonly object _lock = new();

    public bool ApplyLimit(Process process, ProcessRule rule)
    {
        if (process == null || process.HasExited) return false;

        lock (_lock)
        {
            try
            {
                var jobHandle = GetOrCreateJob(rule);

                var processHandle = NativeMethods.OpenProcess(
                    NativeMethods.ProcessAccessFlags.ProcessSetQuota |
                    NativeMethods.ProcessAccessFlags.Terminate,
                    false, process.Id);

                if (processHandle == IntPtr.Zero) return false;

                var result = NativeMethods.AssignProcessToJobObject(jobHandle, processHandle);
                NativeMethods.CloseHandle(processHandle);

                return result;
            }
            catch
            {
                return false;
            }
        }
    }

    private IntPtr GetOrCreateJob(ProcessRule rule)
    {
        var key = $"{rule.ProcessName}_{rule.MaxMemoryBytes}";

        if (_jobHandles.TryGetValue(key, out var existingHandle) && existingHandle != IntPtr.Zero)
            return existingHandle;

        var jobName = $"ProcessLimit_{rule.ProcessName}_{Guid.NewGuid():N}";
        var jobHandle = NativeMethods.CreateJobObjectW(IntPtr.Zero, jobName);

        if (jobHandle == IntPtr.Zero)
            throw new InvalidOperationException($"无法创建 Job Object，错误码: {Marshal.GetLastWin32Error()}");

        var info = new NativeMethods.JOBOBJECT_EXTENDED_LIMIT_INFORMATION();
        info.BasicLimitInformation.LimitFlags = NativeMethods.JOB_OBJECT_LIMIT_PROCESS_MEMORY;
        info.ProcessMemoryLimit = (UIntPtr)rule.MaxMemoryBytes;

        var size = (uint)Marshal.SizeOf<NativeMethods.JOBOBJECT_EXTENDED_LIMIT_INFORMATION>();
        var success = NativeMethods.SetInformationJobObject(
            jobHandle,
            NativeMethods.JOBOBJECTINFOCLASS.ExtendedLimitInformation,
            ref info, size);

        if (!success)
        {
            NativeMethods.CloseHandle(jobHandle);
            throw new InvalidOperationException($"无法设置 Job Object 限制，错误码: {Marshal.GetLastWin32Error()}");
        }

        _jobHandles[key] = jobHandle;
        return jobHandle;
    }

    public void CleanupAll()
    {
        lock (_lock)
        {
            foreach (var handle in _jobHandles.Values)
            {
                if (handle != IntPtr.Zero)
                    NativeMethods.CloseHandle(handle);
            }
            _jobHandles.Clear();
        }
    }

    public void RemoveJob(string key)
    {
        lock (_lock)
        {
            if (_jobHandles.TryGetValue(key, out var handle))
            {
                if (handle != IntPtr.Zero)
                    NativeMethods.CloseHandle(handle);
                _jobHandles.Remove(key);
            }
        }
    }
}
