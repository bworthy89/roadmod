using System;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace Game.Debug;

internal static class WinConsole
{
	private const uint GENERIC_WRITE = 1073741824u;

	private const uint GENERIC_READ = 2147483648u;

	private const uint FILE_SHARE_READ = 1u;

	private const uint FILE_SHARE_WRITE = 2u;

	private const uint OPEN_EXISTING = 3u;

	private const uint FILE_ATTRIBUTE_NORMAL = 128u;

	private const uint ERROR_ACCESS_DENIED = 5u;

	private const uint ATTACH_PARRENT = uint.MaxValue;

	public static void Initialize(bool alwaysCreateNewConsole = true)
	{
		bool flag = true;
		if (alwaysCreateNewConsole || (AttachConsole(uint.MaxValue) == 0 && (long)Marshal.GetLastWin32Error() != 5))
		{
			flag = AllocConsole() != 0;
		}
		if (flag)
		{
			InitializeOutStream();
		}
	}

	private static void InitializeOutStream()
	{
		FileStream fileStream = CreateFileStream("CONOUT$", 1073741824u, 2u, FileAccess.Write);
		if (fileStream != null)
		{
			StreamWriter obj = new StreamWriter(fileStream)
			{
				AutoFlush = true
			};
			Console.SetOut(obj);
			Console.SetError(obj);
		}
	}

	private static void InitializeInStream()
	{
		FileStream fileStream = CreateFileStream("CONIN$", 2147483648u, 1u, FileAccess.Read);
		if (fileStream != null)
		{
			Console.SetIn(new StreamReader(fileStream));
		}
	}

	private static FileStream CreateFileStream(string name, uint win32DesiredAccess, uint win32ShareMode, FileAccess dotNetFileAccess)
	{
		SafeFileHandle safeFileHandle = new SafeFileHandle(CreateFileW(name, win32DesiredAccess, win32ShareMode, IntPtr.Zero, 3u, 128u, IntPtr.Zero), ownsHandle: true);
		if (!safeFileHandle.IsInvalid)
		{
			return new FileStream(safeFileHandle, dotNetFileAccess);
		}
		return null;
	}

	[DllImport("kernel32.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Auto, SetLastError = true)]
	private static extern int AllocConsole();

	[DllImport("kernel32.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Auto, SetLastError = true)]
	private static extern uint AttachConsole(uint dwProcessId);

	[DllImport("kernel32.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Auto, SetLastError = true)]
	private static extern IntPtr CreateFileW(string lpFileName, uint dwDesiredAccess, uint dwShareMode, IntPtr lpSecurityAttributes, uint dwCreationDisposition, uint dwFlagsAndAttributes, IntPtr hTemplateFile);
}
