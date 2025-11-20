using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using CliWrap;
using Colossal;
using Game.UI.Localization;

namespace Game.Modding.Toolchain.Dependencies;

public class UnityLicenseDependency : BaseDependency
{
	private static readonly string kSerialBasedLicenseFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Unity", "Unity_lic.ulf");

	private static readonly string kNamedUserLicenseFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Unity", "licenses", "UnityEntitlementLicense.xml");

	public override string name => "Unity license";

	public override string icon => "Media/Toolchain/Unity.svg";

	public override bool confirmUninstallation => true;

	public override LocalizedString installDescr => LocalizedString.Id("Options.WARN_TOOLCHAIN_INSTALL_UNITY_LICENSE");

	public override LocalizedString uninstallMessage => LocalizedString.Id("Options.WARN_TOOLCHAIN_UNITY_LICENSE_RETURN");

	public override Type[] dependsOnInstallation => new Type[1] { typeof(UnityDependency) };

	public bool licenseExists
	{
		get
		{
			if (!File.Exists(kSerialBasedLicenseFile))
			{
				return File.Exists(kNamedUserLicenseFile);
			}
			return true;
		}
	}

	public override Task<bool> IsInstalled(CancellationToken token)
	{
		return Task.FromResult(licenseExists);
	}

	public override Task<bool> IsUpToDate(CancellationToken token)
	{
		return Task.FromResult(result: true);
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
			IToolchainDependency.log.Debug("Waiting for Unity license");
			base.state = new IToolchainDependency.State(DependencyState.Installing, "WaitingUnityLicense");
			string unityExePath = UnityDependency.unityExePath;
			if (string.IsNullOrEmpty(unityExePath) || !LongFile.Exists(unityExePath))
			{
				throw new Exception("Unity installation path is incorrect or does not exist: " + unityExePath);
			}
			Command command = Cli.Wrap(unityExePath).WithArguments(new string[3]
			{
				"-projectPath",
				UnityModProjectDependency.kProjectUnzipPath,
				"-quit"
			}).WithStandardOutputPipe(PipeTarget.ToDelegate(delegate(string l)
			{
				IToolchainDependency.log.Trace(l);
			}))
				.WithStandardErrorPipe(PipeTarget.ToDelegate(delegate(string l)
				{
					IToolchainDependency.log.Error(l);
				}))
				.WithValidation(CommandResultValidation.None);
			IToolchainDependency.log.TraceFormat("call '{0}'", command);
			command.ExecuteAsync(token);
			IToolchainDependency.log.Debug("Waiting for Unity license");
			await AsyncUtils.WaitForAction(() => licenseExists, token).ConfigureAwait(continueOnCapturedContext: false);
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
			if (LongFile.Exists(kSerialBasedLicenseFile))
			{
				IToolchainDependency.log.Debug("Return Unity license");
				base.state = new IToolchainDependency.State(DependencyState.Removing, "ReturningUnityLicense");
				string unityExePath = UnityDependency.unityExePath;
				if (string.IsNullOrEmpty(unityExePath) || !LongFile.Exists(unityExePath))
				{
					throw new Exception("Unity installation path is incorrect or does not exist: " + unityExePath);
				}
				Command command = Cli.Wrap(unityExePath).WithArguments(new string[2] { "-returnlicense", "-quit" }).WithStandardOutputPipe(PipeTarget.ToDelegate(delegate(string l)
				{
					IToolchainDependency.log.Trace(l);
				}))
					.WithStandardErrorPipe(PipeTarget.ToDelegate(delegate(string l)
					{
						IToolchainDependency.log.Error(l);
					}));
				IToolchainDependency.log.TraceFormat("call '{0}'", command);
				await command.ExecuteAsync(token).ConfigureAwait(continueOnCapturedContext: false);
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

	public override LocalizedString GetLocalizedState(bool includeProgress)
	{
		return base.state.m_State switch
		{
			DependencyState.Installed => LocalizedString.Id("Options.STATE_TOOLCHAIN[Activated]"), 
			DependencyState.Installing => LocalizedString.Id("Options.STATE_TOOLCHAIN[WaitingForActivation]"), 
			DependencyState.NotInstalled => LocalizedString.Id("Options.STATE_TOOLCHAIN[NotActivated]"), 
			DependencyState.Removing => LocalizedString.Id("Options.STATE_TOOLCHAIN[Returning]"), 
			_ => IToolchainDependency.GetLocalizedState(base.state, includeProgress), 
		};
	}
}
