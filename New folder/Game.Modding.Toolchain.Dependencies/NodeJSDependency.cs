using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using CliWrap;
using CliWrap.Exceptions;
using Colossal;
using Game.SceneFlow;
using Game.Settings;
using Game.UI.Localization;

namespace Game.Modding.Toolchain.Dependencies;

public class NodeJSDependency : BaseDependency
{
	public static readonly System.Version kNodeJSVersion = new System.Version(22, 21, 0);

	public static readonly System.Version kMinNodeJSVersion = new System.Version(18, 0);

	public static readonly string kNodeJSInstallerUrl = $"https://nodejs.org/dist/v{kNodeJSVersion}/node-v{kNodeJSVersion}-{RuntimeInformation.OSArchitecture.ToString().ToLower()}.msi";

	public static readonly string kDefaultInstallationDirectory = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);

	public static readonly string kInstallationFolder = "nodejs";

	private long? m_DownloadSize;

	public override string name => "Node.js";

	public override string icon => "Media/Toolchain/NodeJS.svg";

	public override bool confirmUninstallation => true;

	public string installerPath => Path.Combine(Path.GetFullPath(SharedSettings.instance.modding.downloadDirectory), Path.GetFileName(kNodeJSInstallerUrl));

	public override bool canChangeInstallationDirectory => true;

	public override string installationDirectory
	{
		get
		{
			return Path.GetFullPath(Path.Combine(base.installationDirectory ?? kDefaultInstallationDirectory, kInstallationFolder));
		}
		protected set
		{
			string text = ((Path.GetFileName(value) == kInstallationFolder) ? Path.GetDirectoryName(value) : value);
			base.installationDirectory = text;
		}
	}

	public override LocalizedString installDescr => new LocalizedString("Options.WARN_TOOLCHAIN_INSTALL_NODEJS", null, new Dictionary<string, ILocElement>
	{
		{
			"NODEJS_VERSION",
			LocalizedString.Value(kNodeJSVersion.ToString())
		},
		{
			"HOST",
			LocalizedString.Value(new Uri(kNodeJSInstallerUrl).Host)
		}
	});

	public override LocalizedString uninstallMessage => new LocalizedString("Options.WARN_TOOLCHAIN_NODEJS_UNINSTALL", null, new Dictionary<string, ILocElement> { 
	{
		"NODEJS_VERSION",
		LocalizedString.Value(kNodeJSVersion.ToString())
	} });

	public override string version
	{
		get
		{
			if (base.version == null)
			{
				Task.Run(async () => await GetNodeVersion(GameManager.instance.terminationToken)).Wait();
			}
			return base.version;
		}
		protected set
		{
			base.version = value;
		}
	}

	public NodeJSDependency()
	{
		OverrideInstallationDirectory(kDefaultInstallationDirectory);
	}

	public override async Task<bool> IsInstalled(CancellationToken token)
	{
		return !string.IsNullOrEmpty(await GetNodeVersion(token).ConfigureAwait(continueOnCapturedContext: false));
	}

	public override async Task<bool> IsUpToDate(CancellationToken token)
	{
		if (System.Version.TryParse(await GetNodeVersion(token).ConfigureAwait(continueOnCapturedContext: false), out var result))
		{
			return result >= kMinNodeJSVersion;
		}
		return false;
	}

	public override async Task<bool> NeedDownload(CancellationToken token)
	{
		FileInfo installerFile = new FileInfo(installerPath);
		if (!installerFile.Exists)
		{
			return true;
		}
		long num = await GetDotNetInstallerSize(token).ConfigureAwait(continueOnCapturedContext: false);
		if (installerFile.Length != num)
		{
			await AsyncUtils.DeleteFileAsync(installerPath, token).ConfigureAwait(continueOnCapturedContext: false);
			return true;
		}
		return false;
	}

	private async Task<long> GetDotNetInstallerSize(CancellationToken token)
	{
		m_DownloadSize.GetValueOrDefault();
		if (!m_DownloadSize.HasValue)
		{
			m_DownloadSize = await IToolchainDependency.GetDownloadSizeAsync(kNodeJSInstallerUrl, token).ConfigureAwait(continueOnCapturedContext: false);
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
				m_Size = 104857600L
			});
			if (await NeedDownload(token).ConfigureAwait(continueOnCapturedContext: false))
			{
				List<IToolchainDependency.DiskSpaceRequirements> list = requests;
				list.Add(new IToolchainDependency.DiskSpaceRequirements
				{
					m_Path = installerPath,
					m_Size = await GetDotNetInstallerSize(token).ConfigureAwait(continueOnCapturedContext: false)
				});
			}
		}
		return requests;
	}

	public override Task Download(CancellationToken token)
	{
		return BaseDependency.Download(this, token, kNodeJSInstallerUrl, installerPath, "DownloadingNodeJS");
	}

	public override async Task Install(CancellationToken token)
	{
		token.ThrowIfCancellationRequested();
		string path = installerPath;
		try
		{
			IToolchainDependency.log.DebugFormat("Installing {0}", name);
			base.state = new IToolchainDependency.State(DependencyState.Installing, "InstallingNodeJS");
			if (!LongFile.Exists(path))
			{
				throw new ToolchainException(ToolchainError.Install, this, "Installer not found '" + path + "'");
			}
			Command command = Cli.Wrap("msiexec").WithArguments(new string[5]
			{
				"/i",
				Utility.Escape(path),
				"/passive",
				"/norestart",
				"INSTALLDIR=" + Utility.Escape(installationDirectory)
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
			IToolchainDependency.UpdateProcessEnvVarPathValue();
		}
		catch (ToolchainException)
		{
			throw;
		}
		catch (OperationCanceledException)
		{
			throw;
		}
		catch (CommandExecutionException ex3)
		{
			if (ex3.ExitCode == 1602 || ex3.ExitCode == 1603)
			{
				throw new ToolchainException(ToolchainError.Install, this, "Installation canceled by user");
			}
			throw new ToolchainException(ToolchainError.Install, this, ex3);
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
		try
		{
			IToolchainDependency.log.DebugFormat("Uninstalling {0}", name);
			base.state = new IToolchainDependency.State(DependencyState.Removing, "RemovingNodeJS");
			if (IToolchainDependency.GetUninstaller(new Dictionary<string, string>
			{
				{ "DisplayName", "Node.js" },
				{ "DisplayVersion", version }
			}, out var keyName) == null)
			{
				throw new ToolchainException(ToolchainError.Uninstall, this, "Uninstaller not found", null, isFatal: false);
			}
			Command command = Cli.Wrap("msiexec").WithArguments(new string[4] { "/x", keyName, "/passive", "/norestart" }).WithStandardOutputPipe(PipeTarget.ToDelegate(delegate(string l)
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
		catch (CommandExecutionException ex3)
		{
			if (ex3.ExitCode == 1602 || ex3.ExitCode == 1603)
			{
				throw new ToolchainException(ToolchainError.Install, this, "Uninstallation canceled by user");
			}
			throw new ToolchainException(ToolchainError.Install, this, ex3);
		}
		catch (Exception innerException)
		{
			throw new ToolchainException(ToolchainError.Uninstall, this, innerException);
		}
	}

	private async Task<string> GetNodeVersion(CancellationToken token)
	{
		string installedVersion = string.Empty;
		try
		{
			Command command = Cli.Wrap("node").WithArguments("-v").WithStandardOutputPipe(PipeTarget.ToDelegate(delegate(string l)
			{
				installedVersion = l;
			}))
				.WithStandardErrorPipe(PipeTarget.ToDelegate(delegate(string l)
				{
					IToolchainDependency.log.Warn(l);
				}))
				.WithValidation(CommandResultValidation.None);
			IToolchainDependency.log.TraceFormat("call '{0}'", command);
			await command.ExecuteAsync(token).ConfigureAwait(continueOnCapturedContext: false);
		}
		catch (Win32Exception ex)
		{
			if (ex.ErrorCode != -2147467259)
			{
				IToolchainDependency.log.ErrorFormat(ex, "Failed to get {0} version", name);
			}
		}
		catch (Exception exception)
		{
			IToolchainDependency.log.ErrorFormat(exception, "Failed to get {0} version", name);
		}
		NodeJSDependency nodeJSDependency = this;
		string text;
		if (!installedVersion.StartsWith('v'))
		{
			text = installedVersion;
		}
		else
		{
			string text2 = installedVersion;
			text = text2.Substring(1, text2.Length - 1);
		}
		((BaseDependency)nodeJSDependency).version = text;
		return base.version;
	}

	public override LocalizedString GetLocalizedVersion()
	{
		if (string.IsNullOrEmpty(version))
		{
			return new LocalizedString("Options.WARN_TOOLCHAIN_MIN_VERSION", null, new Dictionary<string, ILocElement> { 
			{
				"MIN_VERSION",
				LocalizedString.Value(kMinNodeJSVersion.ToString())
			} });
		}
		return base.GetLocalizedVersion();
	}
}
