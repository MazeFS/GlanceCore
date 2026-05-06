namespace GlanceCore.Core;

using System;
using System.Runtime.InteropServices;
using System.Diagnostics;

public static class MemoryOptimizer
{
    // English: EmptyWorkingSet is physically located in psapi.dll
    [DllImport("psapi.dll", SetLastError = true)]
    private static extern bool EmptyWorkingSet(IntPtr hProcess);

    // English: Alternative method from kernel32 to limit working set size
    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool SetProcessWorkingSetSize(IntPtr hProcess, IntPtr minimumWorkingSetSize, IntPtr maximumWorkingSetSize);

    public static void Trim()
    {
        try
        {
            // 1. Standard .NET Garbage Collection
            GC.Collect(2, GCCollectionMode.Forced, true);
            GC.WaitForPendingFinalizers();

            // 2. Native Windows memory trimming
            IntPtr currentProcess = Process.GetCurrentProcess().Handle;

            // Try PSAPI method first
            if (!EmptyWorkingSet(currentProcess))
            {
                // Fallback to kernel32 method if PSAPI fails
                SetProcessWorkingSetSize(currentProcess, (IntPtr)(-1), (IntPtr)(-1));
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Memory Trimming failed: {ex.Message}");
        }
    }
}