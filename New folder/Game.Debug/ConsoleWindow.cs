using System;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace Game.Debug;

public class ConsoleWindow
{
	private const uint ATTACH_PARENT_PROCESS = uint.MaxValue;

	private const uint ERROR_ACCESS_DENIED = 5u;

	private const uint GENERIC_WRITE = 1073741824u;

	private const uint GENERIC_READ = 2147483648u;

	private const uint FILE_SHARE_READ = 1u;

	private const uint FILE_SHARE_WRITE = 2u;

	private const uint OPEN_EXISTING = 3u;

	private const uint FILE_ATTRIBUTE_NORMAL = 128u;

	private const int SC_CLOSE = 61536;

	private const int MF_BYCOMMAND = 0;

	private TextWriter m_OldOutput;

	private TextWriter m_OldError;

	private StreamWriter m_Writer;

	private const uint STD_OUTPUT_HANDLE = 4294967285u;

	private const uint ENABLE_VIRTUAL_TERMINAL_PROCESSING = 4u;

	public const ushort FOREGROUND_BLUE = 1;

	public const ushort FOREGROUND_GREEN = 2;

	public const ushort FOREGROUND_RED = 4;

	public const ushort FOREGROUND_INTENSITY = 8;

	public const ushort BACKGROUND_BLUE = 16;

	public const ushort BACKGROUND_GREEN = 32;

	public const ushort BACKGROUND_RED = 64;

	public const ushort BACKGROUND_INTENSITY = 128;

	private static void EnableVirtualTerminal()
	{
		IntPtr stdHandle = GetStdHandle(4294967285u);
		if (stdHandle != IntPtr.Zero && GetConsoleMode(stdHandle, out var lpMode))
		{
			SetConsoleMode(stdHandle, lpMode | 4);
		}
	}

	private static void EnableVirtualTerminal(IntPtr handle)
	{
		if (handle != IntPtr.Zero && GetConsoleMode(handle, out var lpMode))
		{
			SetConsoleMode(handle, lpMode | 4);
		}
	}

	public ConsoleWindow(string title, bool attachConsole = false)
	{
		bool flag = true;
		if (attachConsole)
		{
			if (!AttachConsole(uint.MaxValue) && (long)Marshal.GetLastWin32Error() != 5)
			{
				flag = AllocConsole();
			}
			if (flag)
			{
				SetTitle(title);
				DeleteMenu(GetSystemMenu(GetConsoleWindow(), bRevert: false), 61536u, 0u);
			}
		}
		if (flag)
		{
			m_OldOutput = Console.Out;
			m_OldError = Console.Error;
			EnableVirtualTerminal(InitializeOutStream());
		}
	}

	public static void SetColor(ushort color)
	{
		SetConsoleTextAttribute(GetStdHandle(4294967285u), color);
	}

	private IntPtr InitializeOutStream()
	{
		FileStream fileStream = CreateFileStream("CONOUT$", 3221225472u, 2u, FileAccess.Write);
		if (fileStream != null)
		{
			m_Writer = new StreamWriter(fileStream)
			{
				AutoFlush = true
			};
			Console.SetOut(m_Writer);
			Console.SetError(m_Writer);
			return fileStream.SafeFileHandle.DangerousGetHandle();
		}
		return IntPtr.Zero;
	}

	private void InitializeInStream()
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

	public void Dispose()
	{
		Console.SetOut(m_OldOutput);
		Console.SetError(m_OldError);
		m_Writer.Dispose();
		FreeConsole();
	}

	public void SetTitle(string strName)
	{
		SetConsoleTitle(strName);
	}

	[DllImport("kernel32.dll", SetLastError = true)]
	private static extern bool GetConsoleMode(IntPtr hConsoleHandle, out uint lpMode);

	[DllImport("kernel32.dll")]
	private static extern bool SetConsoleMode(IntPtr hConsoleHandle, uint dwMode);

	[DllImport("kernel32.dll")]
	private static extern IntPtr GetConsoleWindow();

	[DllImport("user32.dll")]
	private static extern IntPtr GetSystemMenu(IntPtr hWnd, bool bRevert);

	[DllImport("user32.dll")]
	private static extern bool DeleteMenu(IntPtr hMenu, uint uPosition, uint uFlags);

	[DllImport("kernel32.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Auto, SetLastError = true)]
	private static extern bool AllocConsole();

	[DllImport("kernel32.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Auto, SetLastError = true)]
	private static extern bool AttachConsole(uint dwProcessId);

	[DllImport("kernel32.dll", SetLastError = true)]
	private static extern bool FreeConsole();

	[DllImport("kernel32.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Auto, SetLastError = true)]
	private static extern IntPtr GetStdHandle(uint nStdHandle);

	[DllImport("kernel32.dll")]
	private static extern void SetStdHandle(uint nStdHandle, IntPtr handle);

	[DllImport("kernel32.dll")]
	private static extern bool SetConsoleTitle(string lpConsoleTitle);

	[DllImport("kernel32.dll", SetLastError = true)]
	private static extern bool SetConsoleTextAttribute(IntPtr hConsoleOutput, ushort attributes);

	[DllImport("kernel32.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Auto, SetLastError = true)]
	private static extern IntPtr CreateFileW(string lpFileName, uint dwDesiredAccess, uint dwShareMode, IntPtr lpSecurityAttributes, uint dwCreationDisposition, uint dwFlagsAndAttributes, IntPtr hTemplateFile);
}
