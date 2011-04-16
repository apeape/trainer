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

			result = Win32.WriteProcessMemory(this.Handle, (IntPtr)(address), data, size, out written);

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
	}
}
