using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using CliWrap;
using Colossal;
using Game.Settings;
using Game.UI.Localization;
using Microsoft.Win32;
using UnityEngine;

namespace Game.Modding.Toolchain.Dependencies;

public class UnityDependency : BaseDependency
{
	public const string kUnityInstallerUrl = "https://download.unity3d.com/download_unity/7670c08855a9/Windows64EditorInstaller/UnitySetup64-2022.3.62f2.exe";

	public static readonly string sUnityVersion = Application.unityVersion;

	public static readonly string kDefaultInstallationDirectory = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);

	public static readonly string kInstallationFolder = "Unity " + sUnityVersion;

	private long? m_DownloadSize;

	private static string m_CachedUnityInstallationDirectory;

	public override string name => "Unity editor";

	public override string icon => "Media/Toolchain/Unity.svg";

	public override string version
	{
		get
		{
			return sUnityVersion;
		}
		protected set
		{
		}
	}

	public static string unityInstallationDirectory
	{
		get
		{
			string path = "SOFTWARE\\Unity Technologies\\Installer\\Unity " + sUnityVersion + "\\";
			string path2 = "SOFTWARE\\WOW6432Node\\Microsoft\\Windows\\CurrentVersion\\Uninstall\\Unity " + sUnityVersion + "\\";
			int depth;
			if (TryGetRegistryKeyValue(Registry.CurrentUser, path, "Location x64", out var value))
			{
				depth = 0;
			}
			else if (TryGetRegistryKeyValue(Registry.LocalMachine, path, "Location x64", out value))
			{
				depth = 0;
			}
			else if (TryGetRegistryKeyValue(Registry.LocalMachine, path2, "UninstallString", out value))
			{
				depth = 2;
			}
			else
			{
				if (!TryGetRegistryKeyValue(Registry.LocalMachine, path2, "DisplayIcon", out value))
				{
					return string.Empty;
				}
				depth = 2;
			}
			if (TryGetParentPath(value, depth, out m_CachedUnityInstallationDirectory))
			{
				return m_CachedUnityInstallationDirectory;
			}
			return string.Empty;
		}
	}

	public static string unityExePath
	{
		get
		{
			if (unityInstallationDirectory != null)
			{
				return Path.Combine(unityInstallationDirectory, "Editor", "Unity.exe");
			}
			return string.Empty;
		}
	}

	public static string unityUninstallerExePath
	{
		get
		{
			if (unityInstallationDirectory != null)
			{
				return Path.Combine(unityInstallationDirectory, "Editor", "Uninstall.exe");
			}
			return string.Empty;
		}
	}

	public string unityInstallerPath => Path.Combine(Path.GetFullPath(SharedSettings.instance.modding.downloadDirectory), Path.GetFileName("https://download.unity3d.com/download_unity/7670c08855a9/Windows64EditorInstaller/UnitySetup64-2022.3.62f2.exe"));

	public override bool canChangeInstallationDirectory => true;

	public override string installationDirectory
	{
		get
		{
			if (base.state.m_State == DependencyState.Installed)
			{
				return m_CachedUnityInstallationDirectory;
			}
			return Path.GetFullPath(Path.Combine(base.installationDirectory ?? kDefaultInstallationDirectory, kInstallationFolder));
		}
		protected set
		{
			string text = ((Path.GetFileName(value) == kInstallationFolder) ? Path.GetDirectoryName(value) : value);
			base.installationDirectory = text;
		}
	}

	public override bool confirmUninstallation => true;

	public override LocalizedString installDescr => new LocalizedString("Options.WARN_TOOLCHAIN_INSTALL_UNITY", null, new Dictionary<string, ILocElement>
	{
		{
			"UNITY_VERSION",
			LocalizedString.Value(sUnityVersion)
		},
		{
			"HOST",
			LocalizedString.Value(new Uri("https://download.unity3d.com/download_unity/7670c08855a9/Windows64EditorInstaller/UnitySetup64-2022.3.62f2.exe").Host)
		}
	});

	public override LocalizedString uninstallMessage => new LocalizedString("Options.WARN_TOOLCHAIN_UNITY_UNINSTALL", null, new Dictionary<string, ILocElement> { 
	{
		"UNITY_VERSION",
		LocalizedString.Value(sUnityVersion)
	} });

	public UnityDependency()
	{
		OverrideInstallationDirectory(null);
	}

	public override Task<bool> IsInstalled(CancellationToken token)
	{
		string text = unityExePath;
		return Task.FromResult(!string.IsNullOrEmpty(text) && LongFile.Exists(text));
	}

	public override Task<bool> IsUpToDate(CancellationToken token)
	{
		return IsInstalled(token);
	}

	public override async Task<bool> NeedDownload(CancellationToken token)
	{
		FileInfo installerFile = new FileInfo(unityInstallerPath);
		if (!installerFile.Exists)
		{
			return true;
		}
		long num = await GetUnityInstallerSize(token).ConfigureAwait(continueOnCapturedContext: false);
		if (installerFile.Length != num)
		{
			await AsyncUtils.DeleteFileAsync(unityInstallerPath, token).ConfigureAwait(continueOnCapturedContext: false);
			return true;
		}
		return false;
	}

	private async Task<long> GetUnityInstallerSize(CancellationToken token)
	{
		m_DownloadSize.GetValueOrDefault();
		if (!m_DownloadSize.HasValue)
		{
			m_DownloadSize = await IToolchainDependency.GetDownloadSizeAsync("https://download.unity3d.com/download_unity/7670c08855a9/Windows64EditorInstaller/UnitySetup64-2022.3.62f2.exe", token).ConfigureAwait(continueOnCapturedContext: false);
		}
		return m_DownloadSize.Value;
	}

	public override async Task<List<IToolchainDependency.DiskSpaceRequirements>> GetRequiredDiskSpace(CancellationToken token)
	{
		List<IToolchainDependency.DiskSpaceRequirements> requests = new List<IToolchainDependency.DiskSpaceRequirements>();
		if (!(await IsInstalled(token).ConfigureAwait(continueOnCapturedContext: false)))
		{
			requests.Add(new IToolchainDependency.DiskSpaceRequirements
			{
				m_Path = installationDirectory,
				m_Size = 6442450944L
			});
			if (await NeedDownload(token).ConfigureAwait(continueOnCapturedContext: false))
			{
				List<IToolchainDependency.DiskSpaceRequirements> list = requests;
				list.Add(new IToolchainDependency.DiskSpaceRequirements
				{
					m_Path = unityInstallerPath,
					m_Size = await GetUnityInstallerSize(token).ConfigureAwait(continueOnCapturedContext: false)
				});
			}
		}
		return requests;
	}

	public override Task Download(CancellationToken token)
	{
		return BaseDependency.Download(this, token, "https://download.unity3d.com/download_unity/7670c08855a9/Windows64EditorInstaller/UnitySetup64-2022.3.62f2.exe", unityInstallerPath, "DownloadingUnity");
	}

	public override async Task Install(CancellationToken token)
	{
		token.ThrowIfCancellationRequested();
		string path = unityInstallerPath;
		try
		{
			IToolchainDependency.log.InfoFormat("Installing Unity to directory '{0}'", installationDirectory);
			base.state = new IToolchainDependency.State(DependencyState.Installing, "InstallingUnity");
			if (!LongFile.Exists(path))
			{
				throw new ToolchainException(ToolchainError.Install, this, "Installer not found '" + path + "'");
			}
			Command command = Cli.Wrap(path).WithArguments(new string[2]
			{
				"/S",
				"/D=" + Utility.Escape(installationDirectory)
			}, escape: false).WithStandardOutputPipe(PipeTarget.ToDelegate(delegate(string l)
			{
				IToolchainDependency.log.Trace(l);
			}))
				.WithStandardErrorPipe(PipeTarget.ToDelegate(delegate(string l)
				{
					IToolchainDependency.log.Error(l);
				}))
				.WithUseShellExecute(useShellExecute: true);
			IToolchainDependency.log.TraceFormat("call '{0}'", command);
			await command.ExecuteAsync(token).ConfigureAwait(continueOnCapturedContext: false);
		}
		catch (ToolchainException)
		{
			throw;
		}
		catch (OperationCanceledException)
		{
			throw;
		}
		catch (Exception innerException)
		{
			throw new ToolchainException(ToolchainError.Install, this, innerException);
		}
		finally
		{
			IToolchainDependency.log.DebugFormat("Remove file '{0}'", path);
			await AsyncUtils.DeleteFileAsync(path, token).ConfigureAwait(continueOnCapturedContext: false);
		}
	}

	public override async Task Uninstall(CancellationToken token)
	{
		token.ThrowIfCancellationRequested();
		try
		{
			IToolchainDependency.log.Debug("Uninstalling Unity");
			base.state = new IToolchainDependency.State(DependencyState.Removing, "RemovingUnity");
			string text = unityUninstallerExePath;
			string unityPath = unityInstallationDirectory;
			if (string.IsNullOrEmpty(text) || !LongFile.Exists(text))
			{
				throw new Exception("Unity uninstaller path is incorrect or does not exist: " + text);
			}
			Command command = Cli.Wrap(text).WithArguments(new string[1] { "/S" }).WithStandardOutputPipe(PipeTarget.ToDelegate(delegate(string l)
			{
				IToolchainDependency.log.Trace(l);
			}))
				.WithStandardErrorPipe(PipeTarget.ToDelegate(delegate(string l)
				{
					IToolchainDependency.log.Error(l);
				}))
				.WithUseShellExecute(useShellExecute: true);
			IToolchainDependency.log.TraceFormat("call '{0}'", command);
			await command.ExecuteAsync(token).ConfigureAwait(continueOnCapturedContext: false);
			if (!string.IsNullOrEmpty(unityPath))
			{
				IToolchainDependency.log.DebugFormat("Wait for directory removing '{0}'", unityPath);
				await AsyncUtils.WaitForAction(() => !LongDirectory.Exists(unityPath), token).ConfigureAwait(continueOnCapturedContext: false);
			}
		}
		catch (OperationCanceledException)
		{
			throw;
		}
		catch (ToolchainException)
		{
			throw;
		}
		catch (Exception innerException)
		{
			throw new ToolchainException(ToolchainError.Uninstall, this, innerException);
		}
	}

	private static bool TryGetRegistryKeyValue(RegistryKey registry, string path, string key, out string value)
	{
		value = null;
		RegistryKey registryKey = null;
		try
		{
			registryKey = registry.OpenSubKey(path);
			if (registryKey != null)
			{
				object value2 = registryKey.GetValue(key);
				if (value2 != null)
				{
					value = value2.ToString();
					return true;
				}
			}
		}
		catch (Exception exception)
		{
			IToolchainDependency.log.Error(exception, "Failed checking registry key " + registry.Name + "\\" + path + key);
		}
		finally
		{
			registryKey?.Dispose();
		}
		return false;
	}

	private static bool TryGetParentPath(string path, int depth, out string parentPath)
	{
		parentPath = null;
		if (string.IsNullOrEmpty(path))
		{
			return false;
		}
		DirectoryInfo directoryInfo = new DirectoryInfo(path);
		for (int i = 0; i < depth; i++)
		{
			directoryInfo = directoryInfo.Parent;
			if (directoryInfo == null)
			{
				return false;
			}
		}
		if (directoryInfo.Exists)
		{
			parentPath = directoryInfo.FullName;
			return true;
		}
		return false;
	}
}
