using System;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace ThrownTogether
{
    // Windows job ownership includes grandchildren even when an intermediate exits.
    public sealed class BatchJob : IDisposable
    {
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        static extern IntPtr CreateJobObject(IntPtr attributes, string name);
        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool SetInformationJobObject(IntPtr job, int kind, IntPtr info, uint size);
        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool AssignProcessToJobObject(IntPtr job, IntPtr process);
        [DllImport("kernel32.dll")]
        static extern bool CloseHandle(IntPtr handle);
        IntPtr handle;

        public BatchJob()
        {
            if (IntPtr.Size != 8) throw new PlatformNotSupportedException("Use 64-bit PowerShell.");
            handle = CreateJobObject(IntPtr.Zero, null);
            if (handle == IntPtr.Zero) throw new Win32Exception();
            // JOBOBJECT_EXTENDED_LIMIT_INFORMATION, Windows x64 layout.
            var info = Marshal.AllocHGlobal(144);
            try
            {
                Marshal.Copy(new byte[144], 0, info, 144);
                Marshal.WriteInt32(info, 16, 0x2000); // JOB_OBJECT_LIMIT_KILL_ON_JOB_CLOSE
                if (!SetInformationJobObject(handle, 9, info, 144)) throw new Win32Exception();
            }
            catch { Dispose(); throw; }
            finally { Marshal.FreeHGlobal(info); }
        }
        public void Assign(IntPtr process)
        {
            if (!AssignProcessToJobObject(handle, process)) throw new Win32Exception();
        }
        public void Dispose()
        {
            if (handle == IntPtr.Zero) return;
            CloseHandle(handle);
            handle = IntPtr.Zero;
        }
    }
}
