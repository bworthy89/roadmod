using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using CliWrap;
using Colossal;
using Colossal.Core;
using Colossal.IO;
using Game.UI.Localization;

namespace Game.Modding.Toolchain.Dependencies;

public class ProjectTemplateDependency : BaseDependency
{
	private const string kProjectName = "C# Mod project template";

	private const string kPropsFile = "Mod.props";

	private const string kTargetsFile = "Mod.targets";

	private static readonly string kPropsFileSource = ToolchainDependencyManager.kGameToolingPath + "/Mod.props";

	private static readonly string kTargetsFileSource = ToolchainDependencyManager.kGameToolingPath + "/Mod.targets";

	private static readonly string kPropsFileDeploy = ToolchainDependencyManager.kUserToolingPath + "/Mod.props";

	private static readonly string kTargetsFileDeploy = ToolchainDependencyManager.kUserToolingPath + "/Mod.targets";

	private const string kTemplatePackageId = "ColossalOrder.ModTemplate";

	private const string kTemplateId = "csiimod";

	private static readonly string kTemplatePackageFile = "ColossalOrder.ModTemplate.1.0.0.nupkg";

	private static readonly string kTemplatePackageSource = Path.Combine(ToolchainDependencyManager.kGameToolingPath, kTemplatePackageFile);

	private static readonly string kTemplatePackageInstallation = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".templateengine", "packages", kTemplatePackageFile);

	public override string name => "C# template";

	public override string icon => "Media/Menu/ColossalLogo.svg";

	public override LocalizedString installDescr => LocalizedString.Id("Options.WARN_TOOLCHAIN_INSTALL_PROJECT_TEMPLATE");

	public override Type[] dependsOnInstallation => new Type[1] { typeof(DotNetDependency) };

	public override Type[] dependsOnUninstallation => new Type[1] { typeof(DotNetDependency) };

	public override IEnumerable<string> envVariables
	{
		get
		{
			yield return "CSII_PATHSET";
			yield return "CSII_INSTALLATIONPATH";
			yield return "CSII_USERDATAPATH";
			yield return "CSII_TOOLPATH";
			yield return "CSII_LOCALMODSPATH";
			yield return "CSII_UNITYMODPROJECTPATH";
			yield return "CSII_UNITYVERSION";
			yield return "CSII_ENTITIESVERSION";
			yield return "CSII_MODPOSTPROCESSORPATH";
			yield return "CSII_MODPUBLISHERPATH";
			yield return "CSII_MANAGEDPATH";
			yield return "CSII_PDXCACHEPATH";
			yield return "CSII_PDXMODSPATH";
			yield return "CSII_ASSEMBLYSEARCHPATH";
			yield return "CSII_MSCORLIBPATH";
		}
	}

	public override async Task<bool> IsInstalled(CancellationToken token)
	{
		_ = 1;
		try
		{
			if (!LongFile.Exists(kPropsFileDeploy) || !LongFile.Exists(kTargetsFileDeploy))
			{
				return false;
			}
			System.Version version = await DotNetDependency.GetDotnetVersion(token).ConfigureAwait(continueOnCapturedContext: false);
			if (version.Major < 6)
			{
				return false;
			}
			List<string> errorText = new List<string>();
			CommandResult obj = await Cli.Wrap("dotnet").WithArguments((version.Major == 6) ? "new --list csiimod" : "new list csiimod --verbosity q").WithStandardErrorPipe(PipeTarget.ToDelegate(delegate(string l)
			{
				errorText.Add(l);
			}))
				.WithValidation(CommandResultValidation.None)
				.ExecuteAsync(token)
				.ConfigureAwait(continueOnCapturedContext: false);
			if (errorText.Count > 0)
			{
				IToolchainDependency.log.Warn(string.Join('\n', errorText));
			}
			if (obj.ExitCode != 0)
			{
				return false;
			}
			return true;
		}
		catch (Exception exception)
		{
			IToolchainDependency.log.Error(exception, "Error during mod template check");
			return false;
		}
	}

	public override Task<bool> IsUpToDate(CancellationToken token)
	{
		try
		{
			if (CalculateCache(kPropsFileSource) != CalculateCache(kPropsFileDeploy))
			{
				return Task.FromResult(result: false);
			}
			if (CalculateCache(kTargetsFileSource) != CalculateCache(kTargetsFileDeploy))
			{
				return Task.FromResult(result: false);
			}
			if (CalculateCache(kTemplatePackageSource) != CalculateCache(kTemplatePackageInstallation))
			{
				return Task.FromResult(result: false);
			}
			return Task.FromResult(result: true);
		}
		catch (Exception exception)
		{
			IToolchainDependency.log.Error(exception, "Error during mod template check");
			return Task.FromResult(result: false);
		}
		static ulong CalculateCache(string file)
		{
			if (!LongFile.Exists(file))
			{
				return 0uL;
			}
			byte[] data = LongFile.ReadAllBytes(file);
			return new Crc(new CrcParameters(64, 4823603603198064275uL, 0uL, 0uL, reflectIn: false, reflectOut: false)).CalculateAsNumeric(data);
		}
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
			IToolchainDependency.log.DebugFormat("Installing {0}", "C# Mod project template");
			base.state = new IToolchainDependency.State(DependencyState.Installing, "InstallingModTemplate");
			IToolchainDependency.log.DebugFormat("Copy mod template properties file '{0}' to '{1}'", kPropsFileSource, kPropsFileDeploy);
			IOUtils.EnsureDirectory(Path.GetDirectoryName(kPropsFileDeploy));
			await AsyncUtils.CopyFileAsync(kPropsFileSource, kPropsFileDeploy, overwrite: true, token).ConfigureAwait(continueOnCapturedContext: false);
			IToolchainDependency.log.DebugFormat("Copy mod template targets file '{0}' to '{1}'", kTargetsFileSource, kTargetsFileDeploy);
			IOUtils.EnsureDirectory(Path.GetDirectoryName(kTargetsFileDeploy));
			await AsyncUtils.CopyFileAsync(kTargetsFileSource, kTargetsFileDeploy, overwrite: true, token).ConfigureAwait(continueOnCapturedContext: false);
			IToolchainDependency.log.DebugFormat("Install mod template package '{0}'", kTemplatePackageSource);
			System.Version dotnetVersion = await DotNetDependency.GetDotnetVersion(token).ConfigureAwait(continueOnCapturedContext: false);
			if (dotnetVersion.Major < 6)
			{
				throw new ToolchainException(ToolchainError.Install, this, ".net6.0 is required");
			}
			List<string> errorText = new List<string>();
			Command command = Cli.Wrap("dotnet").WithArguments((dotnetVersion.Major != 6) ? new string[5] { "new", "uninstall", "ColossalOrder.ModTemplate", "--verbosity", "q" } : new string[3] { "new", "--uninstall", "ColossalOrder.ModTemplate" }).WithStandardOutputPipe(PipeTarget.ToDelegate(delegate(string l)
			{
				IToolchainDependency.log.Trace(l);
			}))
				.WithStandardErrorPipe(PipeTarget.ToDelegate(delegate(string l)
				{
					errorText.Add(l);
				}))
				.WithValidation(CommandResultValidation.None);
			IToolchainDependency.log.TraceFormat("call '{0}'", command);
			await command.ExecuteAsync(token).ConfigureAwait(continueOnCapturedContext: false);
			if (errorText.Count > 0)
			{
				IToolchainDependency.log.Warn(string.Join('\n', errorText));
			}
			errorText.Clear();
			Command command2 = Cli.Wrap("dotnet").WithArguments((dotnetVersion.Major != 6) ? new string[6] { "new", "install", kTemplatePackageSource, "--force", "--verbosity", "q" } : new string[4] { "new", "--install", kTemplatePackageSource, "--force" }).WithStandardOutputPipe(PipeTarget.ToDelegate(delegate(string l)
			{
				IToolchainDependency.log.Trace(l);
			}))
				.WithStandardErrorPipe(PipeTarget.ToDelegate(delegate(string l)
				{
					errorText.Add(l);
				}))
				.WithValidation(CommandResultValidation.None);
			IToolchainDependency.log.TraceFormat("call '{0}'", command2);
			CommandResult commandResult = await command2.ExecuteAsync(token).ConfigureAwait(continueOnCapturedContext: false);
			if (errorText.Count > 0)
			{
				IToolchainDependency.log.Warn(string.Join('\n', errorText));
			}
			if (commandResult.ExitCode != 0)
			{
				throw new ToolchainException(ToolchainError.Install, this, $"Mod template package not installed: code {commandResult.ExitCode}");
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
			throw new ToolchainException(ToolchainError.Install, this, innerException);
		}
	}

	public override async Task Uninstall(CancellationToken token)
	{
		token.ThrowIfCancellationRequested();
		try
		{
			base.state = new IToolchainDependency.State(DependencyState.Removing, "RemovingProjectTemplate");
			IToolchainDependency.log.DebugFormat("Removing {0}", "C# Mod project template");
			IToolchainDependency.log.DebugFormat("Remove file '{0}'", kPropsFileDeploy);
			await AsyncUtils.DeleteFileAsync(kPropsFileDeploy, token).ConfigureAwait(continueOnCapturedContext: false);
			IToolchainDependency.log.DebugFormat("Remove file '{0}'", kTargetsFileDeploy);
			await AsyncUtils.DeleteFileAsync(kTargetsFileDeploy, token).ConfigureAwait(continueOnCapturedContext: false);
			System.Version version = await DotNetDependency.GetDotnetVersion(token).ConfigureAwait(continueOnCapturedContext: false);
			if (version.Major < 6)
			{
				throw new ToolchainException(ToolchainError.Uninstall, this, ".net6.0 is required");
			}
			Command command = Cli.Wrap("dotnet").WithArguments((version.Major != 6) ? new string[3] { "new", "uninstall", "ColossalOrder.ModTemplate" } : new string[3] { "new", "--uninstall", "ColossalOrder.ModTemplate" }).WithStandardOutputPipe(PipeTarget.ToDelegate(delegate(string l)
			{
				IToolchainDependency.log.Trace(l);
			}))
				.WithStandardErrorPipe(PipeTarget.ToDelegate(delegate(string l)
				{
					IToolchainDependency.log.Warn(l);
				}))
				.WithValidation(CommandResultValidation.None);
			IToolchainDependency.log.TraceFormat("call '{0}'", command);
			CommandResult commandResult = await command.ExecuteAsync(token).ConfigureAwait(continueOnCapturedContext: false);
			if (commandResult.ExitCode != 0)
			{
				throw new ToolchainException(ToolchainError.Uninstall, this, $"Mod template package not removed: code {commandResult.ExitCode}");
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
		return Task.FromResult(new List<IToolchainDependency.DiskSpaceRequirements>
		{
			new IToolchainDependency.DiskSpaceRequirements
			{
				m_Path = kPropsFileDeploy,
				m_Size = new FileInfo(kPropsFileSource).Length
			},
			new IToolchainDependency.DiskSpaceRequirements
			{
				m_Path = kTargetsFileDeploy,
				m_Size = new FileInfo(kTargetsFileSource).Length
			},
			new IToolchainDependency.DiskSpaceRequirements
			{
				m_Path = kTemplatePackageInstallation,
				m_Size = new FileInfo(kTemplatePackageSource).Length
			}
		});
	}
}
