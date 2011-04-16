using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

// code written by rick@gibbed.us

namespace trainer
{
	public class Win32Exception : InvalidOperationException
	{
		public Win32Exception() :
			base()
		{
		}

		public Win32Exception(string message)
			: base(message)
		{
		}

		public Win32Exception(string message, Exception innerException)
			: base(message, innerException)
		{
		}
	}

	public class ProcessMemory
	{
		#region Win32 Imports / Defines
		protected struct Win32
		{
			public static Int32 TRUE = 1;
			public static Int32 FALSE = 0;

			public static IntPtr NULL = (IntPtr)(0);
			public static IntPtr INVALID_HANDLE_VALUE = (IntPtr)(-1);

			public static UInt32 SYNCHRONIZE = 0x00100000;
			public static UInt32 STANDARD_RIGHTS_REQUIRED = 0x000F0000;
			public static UInt32 PROCESS_ALL_ACCESS = STANDARD_RIGHTS_REQUIRED | SYNCHRONIZE | 0xFFF;
			public static UInt32 THREAD_ALL_ACCESS = STANDARD_RIGHTS_REQUIRED | SYNCHRONIZE | 0x3FF;

			[DllImport("kernel32.dll")]
			public static extern UInt32 GetLastError();

			[DllImport("kernel32.dll")]
			public static extern IntPtr OpenProcess(UInt32 dwDesiredAccess, Int32 bInheritHandle, Int32 dwProcessId);

			[DllImport("kernel32.dll")]
			public static extern Int32 ReadProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, [In, Out] byte[] lpBuffer, UInt32 nSize, out UInt32 lpNumberOfBytesRead);

			[DllImport("kernel32.dll")]
			public static extern Int32 WriteProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, [In, Out] byte[] lpBuffer, UInt32 nSize, out UInt32 lpNumberOfBytesWritten);

			[DllImport("kernel32.dll")]
			public static extern IntPtr OpenThread(UInt32 dwDesiredAccess, Int32 bInheritHandle, UInt32 dwThreadId);

			[DllImport("kernel32.dll")]
			public static extern UInt32 SuspendThread(IntPtr hThread);

			[DllImport("kernel32.dll")]
			public static extern UInt32 ResumeThread(IntPtr hThread);

			[DllImport("kernel32.dll")]
			public static extern Int32 CloseHandle(IntPtr hObject);

            [DllImport("kernel32.dll")]
            public static extern bool VirtualProtect(IntPtr lpAddress, UInt32 dwSize, Int32 flNewProtect, out Int32 lpflOldProtect);

            public enum Protection
            {
                PAGE_NOACCESS = 0x01,
                PAGE_READONLY = 0x02,
                PAGE_READWRITE = 0x04,
                PAGE_WRITECOPY = 0x08,
                PAGE_EXECUTE = 0x10,
                PAGE_EXECUTE_READ = 0x20,
                PAGE_EXECUTE_READWRITE = 0x40,
                PAGE_EXECUTE_WRITECOPY = 0x80,
                PAGE_GUARD = 0x100,
                PAGE_NOCACHE = 0x200,
                PAGE_WRITECOMBINE = 0x400
            }

            [DllImport("advapi32.dll", SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool OpenProcessToken(IntPtr ProcessHandle,
                UInt32 DesiredAccess, out IntPtr TokenHandle);

            public static uint STANDARD_RIGHTS_READ = 0x00020000;
            public static uint TOKEN_ASSIGN_PRIMARY = 0x0001;
            public static uint TOKEN_DUPLICATE = 0x0002;
            public static uint TOKEN_IMPERSONATE = 0x0004;
            public static uint TOKEN_QUERY = 0x0008;
            public static uint TOKEN_QUERY_SOURCE = 0x0010;
            public static uint TOKEN_ADJUST_PRIVILEGES = 0x0020;
            public static uint TOKEN_ADJUST_GROUPS = 0x0040;
            public static uint TOKEN_ADJUST_DEFAULT = 0x0080;
            public static uint TOKEN_ADJUST_SESSIONID = 0x0100;
            public static uint TOKEN_READ = (STANDARD_RIGHTS_READ | TOKEN_QUERY);
            public static uint TOKEN_ALL_ACCESS = (STANDARD_RIGHTS_REQUIRED | TOKEN_ASSIGN_PRIMARY |
                TOKEN_DUPLICATE | TOKEN_IMPERSONATE | TOKEN_QUERY | TOKEN_QUERY_SOURCE |
                TOKEN_ADJUST_PRIVILEGES | TOKEN_ADJUST_GROUPS | TOKEN_ADJUST_DEFAULT |
                TOKEN_ADJUST_SESSIONID);

            [DllImport("kernel32.dll", SetLastError = true)]
            public static extern IntPtr GetCurrentProcess();

            [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Auto)]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool LookupPrivilegeValue(string lpSystemName, string lpName,
                out LUID lpLuid);

            public const string SE_ASSIGNPRIMARYTOKEN_NAME = "SeAssignPrimaryTokenPrivilege";

            public const string SE_AUDIT_NAME = "SeAuditPrivilege";

            public const string SE_BACKUP_NAME = "SeBackupPrivilege";

            public const string SE_CHANGE_NOTIFY_NAME = "SeChangeNotifyPrivilege";

            public const string SE_CREATE_GLOBAL_NAME = "SeCreateGlobalPrivilege";

            public const string SE_CREATE_PAGEFILE_NAME = "SeCreatePagefilePrivilege";

            public const string SE_CREATE_PERMANENT_NAME = "SeCreatePermanentPrivilege";

            public const string SE_CREATE_SYMBOLIC_LINK_NAME = "SeCreateSymbolicLinkPrivilege";

            public const string SE_CREATE_TOKEN_NAME = "SeCreateTokenPrivilege";

            public const string SE_DEBUG_NAME = "SeDebugPrivilege";

            public const string SE_ENABLE_DELEGATION_NAME = "SeEnableDelegationPrivilege";

            public const string SE_IMPERSONATE_NAME = "SeImpersonatePrivilege";

            public const string SE_INC_BASE_PRIORITY_NAME = "SeIncreaseBasePriorityPrivilege";

            public const string SE_INCREASE_QUOTA_NAME = "SeIncreaseQuotaPrivilege";

            public const string SE_INC_WORKING_SET_NAME = "SeIncreaseWorkingSetPrivilege";

            public const string SE_LOAD_DRIVER_NAME = "SeLoadDriverPrivilege";

            public const string SE_LOCK_MEMORY_NAME = "SeLockMemoryPrivilege";

            public const string SE_MACHINE_ACCOUNT_NAME = "SeMachineAccountPrivilege";

            public const string SE_MANAGE_VOLUME_NAME = "SeManageVolumePrivilege";

            public const string SE_PROF_SINGLE_PROCESS_NAME = "SeProfileSingleProcessPrivilege";

            public const string SE_RELABEL_NAME = "SeRelabelPrivilege";

            public const string SE_REMOTE_SHUTDOWN_NAME = "SeRemoteShutdownPrivilege";

            public const string SE_RESTORE_NAME = "SeRestorePrivilege";

            public const string SE_SECURITY_NAME = "SeSecurityPrivilege";

            public const string SE_SHUTDOWN_NAME = "SeShutdownPrivilege";

            public const string SE_SYNC_AGENT_NAME = "SeSyncAgentPrivilege";

            public const string SE_SYSTEM_ENVIRONMENT_NAME = "SeSystemEnvironmentPrivilege";

            public const string SE_SYSTEM_PROFILE_NAME = "SeSystemProfilePrivilege";

            public const string SE_SYSTEMTIME_NAME = "SeSystemtimePrivilege";

            public const string SE_TAKE_OWNERSHIP_NAME = "SeTakeOwnershipPrivilege";

            public const string SE_TCB_NAME = "SeTcbPrivilege";

            public const string SE_TIME_ZONE_NAME = "SeTimeZonePrivilege";

            public const string SE_TRUSTED_CREDMAN_ACCESS_NAME = "SeTrustedCredManAccessPrivilege";

            public const string SE_UNDOCK_NAME = "SeUndockPrivilege";

            public const string SE_UNSOLICITED_INPUT_NAME = "SeUnsolicitedInputPrivilege";

            [StructLayout(LayoutKind.Sequential)]
            public struct LUID
            {
                public UInt32 LowPart;
                public Int32 HighPart;
            }

            public const UInt32 SE_PRIVILEGE_ENABLED_BY_DEFAULT = 0x00000001;
            public const UInt32 SE_PRIVILEGE_ENABLED = 0x00000002;
            public const UInt32 SE_PRIVILEGE_REMOVED = 0x00000004;
            public const UInt32 SE_PRIVILEGE_USED_FOR_ACCESS = 0x80000000;

            [StructLayout(LayoutKind.Sequential)]
            public struct TOKEN_PRIVILEGES
            {
                public UInt32 PrivilegeCount;
                public LUID Luid;
                public UInt32 Attributes;
            }

            [StructLayout(LayoutKind.Sequential)]
            public struct LUID_AND_ATTRIBUTES
            {
                public LUID Luid;
                public UInt32 Attributes;
            }

            // Use this signature if you do not want the previous state
            [DllImport("advapi32.dll", SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool AdjustTokenPrivileges(IntPtr TokenHandle,
               [MarshalAs(UnmanagedType.Bool)]bool DisableAllPrivileges,
               ref TOKEN_PRIVILEGES NewState,
               UInt32 Zero,
               IntPtr Null1,
               IntPtr Null2);

		};
		#endregion

		protected Encoding Encoding;
		protected Process Process;
		protected IntPtr Handle;

		public UInt32 MainModuleAddress { get { return (UInt32)this.Process.MainModule.BaseAddress.ToInt32(); } }
		public UInt32 MainModuleSize { get { return (UInt32)this.Process.MainModule.ModuleMemorySize; } }

		public ProcessMemory()
		{
			this.Process = null;
			this.Handle = Win32.NULL;
			this.Encoding = Encoding.GetEncoding(437);
		}

		~ProcessMemory()
		{
			if (this.Handle != Win32.NULL)
			{
				this.Close();
			}
		}

		public bool Open(Process process)
		{
            // get debug priviledges first
            GetDebugPriviledges();

			if (this.Handle != Win32.NULL)
			{
				Win32.CloseHandle(this.Handle);
				this.Process = null;
				this.Handle = Win32.INVALID_HANDLE_VALUE;
			}

			IntPtr theProcess;
			theProcess = Win32.OpenProcess(Win32.PROCESS_ALL_ACCESS, Win32.FALSE, process.Id);

			if (theProcess == Win32.NULL)
			{
				return false;
			}

			this.Process = process;
			this.Handle = theProcess;
			return true;
		}

		public bool Close()
		{
			Int32 result;

			if (this.Handle == Win32.NULL)
			{
				throw new InvalidOperationException("process handle is invalid");
			}

			result = Win32.CloseHandle(this.Handle);

			this.Process = null;
			this.Handle = Win32.NULL;

			return result == Win32.TRUE ? true : false;
		}

		public UInt32 Read(UInt32 address, ref byte[] data)
		{
			return this.Read(address, ref data, (UInt32)data.Length);
		}

		public UInt32 Read(UInt32 address, ref byte[] data, UInt32 size)
		{
			Int32 result;
			UInt32 read;

			if (this.Handle == Win32.NULL)
			{
				throw new InvalidOperationException("process handle is invalid");
			}

			result = Win32.ReadProcessMemory(this.Handle, (IntPtr)(address), data, size, out read);

			if (result == 0)
			{
				throw new Win32Exception("error " + Win32.GetLastError().ToString());
			}

			if (read != size)
			{
				throw new InvalidOperationException("only read " + read.ToString() + " instead of " + size);
			}

			return read;
		}

		public Byte ReadU8(UInt32 address)
		{
			byte[] data = new byte[1];
			this.Read(address, ref data);
			return data[0];
		}

		public SByte ReadS8(UInt32 address)
		{
			byte[] data = new byte[1];
			this.Read(address, ref data);
			return (SByte)data[0];
		}

		public UInt16 ReadU16(UInt32 address)
		{
			byte[] data = new byte[2];
			this.Read(address, ref data);
			return BitConverter.ToUInt16(data, 0);
		}

		public Int16 ReadS16(UInt32 address)
		{
			byte[] data = new byte[2];
			this.Read(address, ref data);
			return BitConverter.ToInt16(data, 0);
		}

		public UInt32 ReadU32(UInt32 address)
		{
			byte[] data = new byte[4];
			this.Read(address, ref data);
			return BitConverter.ToUInt32(data, 0);
		}

		public Int32 ReadS32(UInt32 address)
		{
			byte[] data = new byte[4];
			this.Read(address, ref data);
			return BitConverter.ToInt32(data, 0);
		}

		public UInt64 ReadU64(UInt32 address)
		{
			byte[] data = new byte[8];
			this.Read(address, ref data);
			return BitConverter.ToUInt64(data, 0);
		}

		public Int64 ReadS64(UInt32 address)
		{
			byte[] data = new byte[8];
			this.Read(address, ref data);
			return BitConverter.ToInt64(data, 0);
		}

		public float ReadF32(UInt32 address)
		{
			byte[] data = new byte[4];
			this.Read(address, ref data);
			return BitConverter.ToSingle(data, 0);
		}

		public double ReadF64(UInt32 address)
		{
			byte[] data = new byte[8];
			this.Read(address, ref data);
			return BitConverter.ToDouble(data, 0);
		}

		public string ReadString(UInt32 address)
		{
			UInt32 length = this.ReadU32(address + 20);
			UInt32 capacity = this.ReadU32(address + 24);
			byte[] data;

			data = new byte[length];
			if (capacity < 16)
			{
				this.Read(address + 4, ref data);
			}
			else
			{
				UInt32 offset = this.ReadU32(address + 4);
				this.Read(offset, ref data);
			}

			return this.Encoding.GetString(data);
		}

		public UInt32 Write(UInt32 address, ref byte[] data)
		{
			return this.Write(address, ref data, (UInt32)data.Length);
		}

		public UInt32 Write(UInt32 address, ref byte[] data, UInt32 size)
		{
			Int32 result;
			UInt32 written;

			if (this.Handle == Win32.NULL)
			{
				throw new InvalidOperationException("process handle is invalid");
			}

            // set page to read/write to avoid errors
            int oldProtection1, oldProtection2;
            Win32.VirtualProtect((IntPtr)address, size, (int)Win32.Protection.PAGE_READWRITE, out oldProtection1);
            // check if virtualprotect worked or not
            Win32.VirtualProtect((IntPtr)address, size, (int)Win32.Protection.PAGE_READWRITE, out oldProtection2);
            if (oldProtection2 != (int)Win32.Protection.PAGE_READWRITE)
            {
                Console.WriteLine("Error setting protection on memory");
            }
			result = Win32.WriteProcessMemory(this.Handle, (IntPtr)(address), data, size, out written);
            // restore old protection level
            Win32.VirtualProtect((IntPtr)address, size, oldProtection1, out oldProtection1);

			if (result == 0)
			{
				throw new Win32Exception("error " + Win32.GetLastError().ToString());
			}

			if (written != size)
			{
				throw new InvalidOperationException("only wrote " + written.ToString() + " instead of " + size);
			}

			return written;
		}

		public void WriteU8(UInt32 address, Byte value)
		{
			byte[] data = new byte[] { value };
			Debug.Assert(data.Length == 1);
			this.Write(address, ref data);
		}

		public void WriteS8(UInt32 address, SByte value)
		{
			byte[] data = BitConverter.GetBytes(value);
			Debug.Assert(data.Length == 1);
			this.Write(address, ref data);
		}

		public void WriteU16(UInt32 address, UInt16 value)
		{
			byte[] data = BitConverter.GetBytes(value);
			Debug.Assert(data.Length == 2);
			this.Write(address, ref data);
		}

		public void WriteS16(UInt32 address, Int16 value)
		{
			byte[] data = BitConverter.GetBytes(value);
			Debug.Assert(data.Length == 2);
			this.Write(address, ref data);
		}

		public void WriteU32(UInt32 address, UInt32 value)
		{
			byte[] data = BitConverter.GetBytes(value);
			Debug.Assert(data.Length == 4);
			this.Write(address, ref data);
		}

		public void WriteS32(UInt32 address, Int32 value)
		{
			byte[] data = BitConverter.GetBytes(value);
			Debug.Assert(data.Length == 4);
			this.Write(address, ref data);
		}

		public void WriteU64(UInt32 address, UInt64 value)
		{
			byte[] data = BitConverter.GetBytes(value);
			Debug.Assert(data.Length == 8);
			this.Write(address, ref data);
		}

		public void WriteS64(UInt32 address, Int64 value)
		{
			byte[] data = BitConverter.GetBytes(value);
			Debug.Assert(data.Length == 8);
			this.Write(address, ref data);
		}

		public void WriteF32(UInt32 address, float value)
		{
			byte[] data = BitConverter.GetBytes(value);
			Debug.Assert(data.Length == 4);
			this.Write(address, ref data);
		}

		public void WriteF64(UInt32 address, double value)
		{
			byte[] data = BitConverter.GetBytes(value);
			Debug.Assert(data.Length == 8);
			this.Write(address, ref data);
		}

		protected bool SuspendThread(int id)
		{
			IntPtr handle = Win32.OpenThread(Win32.THREAD_ALL_ACCESS, Win32.FALSE, (uint)id);
			
			if (handle == Win32.NULL)
			{
				return false;
			}

			if (Win32.SuspendThread(handle) == 0xFFFFFFFF)
			{
				return false;
			}

			Win32.CloseHandle(handle);
			return true;
		}

		protected bool ResumeThread(int id)
		{
			IntPtr handle = Win32.OpenThread(Win32.THREAD_ALL_ACCESS, Win32.FALSE, (uint)id);
			
			if (handle == Win32.NULL)
			{
				return false;
			}

			if (Win32.ResumeThread(handle) == 0xFFFFFFFF)
			{
				return false;
			}

			Win32.CloseHandle(handle);
			return true;
		}

		public bool Suspend()
		{
			bool result = true;
			for (int i = 0; i < this.Process.Threads.Count; i++)
			{
				if (this.SuspendThread(this.Process.Threads[i].Id) == false)
				{
					result = false;
				}
			}
			return result;
		}

		public bool Resume()
		{
			bool result = true;
			for (int i = 0; i < this.Process.Threads.Count; i++)
			{
				if (this.ResumeThread(this.Process.Threads[i].Id) == false)
				{
					result = false;
				}
			}
			return result;
		}

        public bool GetDebugPriviledges()
        {
            IntPtr hToken;
            Win32.LUID luidSEDebugNameValue;
            Win32.TOKEN_PRIVILEGES tkpPrivileges;

            if (!Win32.OpenProcessToken(Win32.GetCurrentProcess(), Win32.TOKEN_ADJUST_PRIVILEGES | Win32.TOKEN_QUERY, out hToken))
            {
                Console.WriteLine("OpenProcessToken() failed, error = {0} . SeDebugPrivilege is not available", Marshal.GetLastWin32Error());
                return false;
            }
            else
            {
                Console.WriteLine("OpenProcessToken() successfully");
            }

            if (!Win32.LookupPrivilegeValue(null, Win32.SE_DEBUG_NAME, out luidSEDebugNameValue))
            {
                Console.WriteLine("LookupPrivilegeValue() failed, error = {0} .SeDebugPrivilege is not available", Marshal.GetLastWin32Error());
                Win32.CloseHandle(hToken);
                return false;
            }
            else
            {
                Console.WriteLine("LookupPrivilegeValue() successfully");
            }

            tkpPrivileges.PrivilegeCount = 1;
            tkpPrivileges.Luid = luidSEDebugNameValue;
            tkpPrivileges.Attributes = Win32.SE_PRIVILEGE_ENABLED;

            if (!Win32.AdjustTokenPrivileges(hToken, false, ref tkpPrivileges, 0, IntPtr.Zero, IntPtr.Zero))
            {
                Console.WriteLine("LookupPrivilegeValue() failed, error = {0} .SeDebugPrivilege is not available", Marshal.GetLastWin32Error());
            }
            else
            {
                Console.WriteLine("SeDebugPrivilege is now available");
            }
            Win32.CloseHandle(hToken);

            return true;
        }
	}
}
