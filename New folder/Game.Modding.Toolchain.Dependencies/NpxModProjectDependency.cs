using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using CliWrap;
using Colossal;

namespace Game.Modding.Toolchain.Dependencies;

public class NpxModProjectDependency : BaseDependency
{
	private const string kProjectName = "UI Mod project template";

	private const string kModuleNamespace = "@colossalorder";

	private const string kModuleName = "create-csii-ui-mod";

	private static readonly string kNpxPackagePath = Path.GetFullPath(Path.Combine(ToolchainDependencyManager.kGameToolingPath, "npx-create-csii-ui-mod"));

	public override Type[] dependsOnInstallation => new Type[1] { typeof(NodeJSDependency) };

	public override Type[] dependsOnUninstallation => new Type[1] { typeof(NodeJSDependency) };

	public override IEnumerable<string> envVariables
	{
		get
		{
			yield return "CSII_PATHSET";
			yield return "CSII_USERDATAPATH";
		}
	}

	public override string name => "UI template";

	public override string icon => "Media/Menu/ColossalLogo.svg";

	private async Task<string> GetGlobalNodeModulePath(CancellationToken token)
	{
		string path = string.Empty;
		List<string> errorText = new List<string>();
		try
		{
			await Cli.Wrap("npm").WithArguments("config get prefix").WithStandardOutputPipe(PipeTarget.ToDelegate(delegate(string l)
			{
				path = l;
			}))
				.WithStandardErrorPipe(PipeTarget.ToDelegate(delegate(string l)
				{
					errorText.Add(l);
				}))
				.WithValidation(CommandResultValidation.None)
				.ExecuteAsync(token)
				.ConfigureAwait(continueOnCapturedContext: false);
		}
		catch (Win32Exception ex)
		{
			if (ex.ErrorCode != -2147467259)
			{
				IToolchainDependency.log.Error(ex, "Failed to get global npm module path");
			}
			if (errorText.Count > 0)
			{
				IToolchainDependency.log.Warn(string.Join('\n', errorText));
			}
		}
		catch (Exception exception)
		{
			IToolchainDependency.log.Error(exception, "Failed to get global npm module path");
			if (errorText.Count > 0)
			{
				IToolchainDependency.log.Warn(string.Join('\n', errorText));
			}
		}
		return path;
	}

	public override async Task<bool> IsInstalled(CancellationToken token)
	{
		string text = await GetGlobalNodeModulePath(token).ConfigureAwait(continueOnCapturedContext: false);
		if (LongDirectory.Exists(text))
		{
			return LongDirectory.Exists(Path.GetFullPath(Path.Combine(text, "node_modules", "@colossalorder")));
		}
		return false;
	}

	public override async Task<bool> IsUpToDate(CancellationToken token)
	{
		string text = await GetGlobalNodeModulePath(token).ConfigureAwait(continueOnCapturedContext: false);
		if (LongDirectory.Exists(text) && LongFile.TryGetSymlinkTarget(Path.Combine(text, "node_modules", "@colossalorder", "create-csii-ui-mod"), out var targetPath))
		{
			return targetPath == kNpxPackagePath;
		}
		return true;
	}

	public override Task<bool> NeedDownload(CancellationToken token)
	{
		return Task.FromResult(result: false);
	}

	public override Task Download(CancellationToken token)
	{
		return Task.CompletedTask;
	}

	public override async Task Install(CancellationToken token)
	{
		token.ThrowIfCancellationRequested();
		try
		{
			IToolchainDependency.log.DebugFormat("Installing {0}", "UI Mod project template");
			base.state = new IToolchainDependency.State(DependencyState.Installing, "InstallingNpxModsTemplate");
			Command command = Cli.Wrap("npm").WithArguments("link").WithWorkingDirectory(kNpxPackagePath)
				.WithStandardOutputPipe(PipeTarget.ToDelegate(delegate(string l)
				{
					IToolchainDependency.log.Trace(l);
				}))
				.WithStandardErrorPipe(PipeTarget.ToDelegate(delegate(string l)
				{
					IToolchainDependency.log.Warn(l);
				}))
				.WithValidation(CommandResultValidation.None);
			IToolchainDependency.log.TraceFormat("call '{0}'", command);
			await command.ExecuteAsync(token).ConfigureAwait(continueOnCapturedContext: false);
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
			throw new ToolchainException(ToolchainError.Install, this, innerException);
		}
	}

	private static async Task DeleteNpxModule(string globalNodeModulePath, string moduleNamespace, string moduleName, CancellationToken token)
	{
		if (string.IsNullOrEmpty(globalNodeModulePath))
		{
			throw new ArgumentException("Directory path cannot be null or empty.", "globalNodeModulePath");
		}
		if (string.IsNullOrEmpty(moduleName))
		{
			throw new ArgumentException("File prefix cannot be null or empty.", "moduleName");
		}
		if (!Directory.Exists(globalNodeModulePath))
		{
			throw new DirectoryNotFoundException("The specified directory was not found: " + globalNodeModulePath);
		}
		string[] files = LongDirectory.GetFiles(globalNodeModulePath, moduleName + "*");
		string[] array = files;
		foreach (string text in array)
		{
			IToolchainDependency.log.DebugFormat("Remove file '{0}'", text);
			await AsyncUtils.DeleteFileAsync(text, token).ConfigureAwait(continueOnCapturedContext: false);
		}
		string text2 = Path.Combine(globalNodeModulePath, "node_modules", moduleNamespace);
		IToolchainDependency.log.DebugFormat("Remove directory '{0}'", text2);
		await AsyncUtils.DeleteDirectoryAsync(text2, recursive: true, token).ConfigureAwait(continueOnCapturedContext: false);
	}

	public override async Task Uninstall(CancellationToken token)
	{
		token.ThrowIfCancellationRequested();
		try
		{
			IToolchainDependency.log.DebugFormat("Deleting {0}", "UI Mod project template");
			base.state = new IToolchainDependency.State(DependencyState.Removing, "RemovingNpxModsTemplate");
			string text = await GetGlobalNodeModulePath(token).ConfigureAwait(continueOnCapturedContext: false);
			if (LongDirectory.Exists(text))
			{
				await DeleteNpxModule(text, "@colossalorder", "create-csii-ui-mod", token);
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

	public override Task<List<IToolchainDependency.DiskSpaceRequirements>> GetRequiredDiskSpace(CancellationToken token)
	{
		return Task.FromResult(new List<IToolchainDependency.DiskSpaceRequirements>());
	}
}
