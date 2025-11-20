using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using CliWrap;
using CliWrap.EventStream;
using Colossal;
using Colossal.IO;
using Colossal.Json;
using Colossal.Win32;
using Game.UI.Localization;
using Mono.Options;

namespace Game.Modding.Toolchain.Dependencies;

public class UnityModProjectDependency : BaseDependency
{
	public const string kProjectName = "UnityModsProject";

	public const string kProjectVersionTxt = "ProjectSettings/ProjectVersion.txt";

	public const string kProjectSettingsAsset = "ProjectSettings/ProjectSettings.asset";

	public const string kProjectPackageManifest = "Packages/manifest.json";

	public const string kProjectPackageLock = "Packages/packages-lock.json";

	public static readonly string kProjectUnzipPath = ToolchainDependencyManager.kUserToolingPath + "/UnityModsProject";

	public static readonly string kProjectZipPath = ToolchainDependencyManager.kGameToolingPath + "/UnityModsProject.zip";

	public static readonly string kModProjectsUnityVersionPath = kProjectUnzipPath + "/ProjectSettings/ProjectVersion.txt";

	public static readonly string kModProjectsVersionPath = kProjectUnzipPath + "/ProjectSettings/ProjectSettings.asset";

	public static readonly string kModProjectPackages = kProjectUnzipPath + "/Packages/packages-lock.json";

	public static bool isUnityOpened => IsUnityOpenWithModsProject(kProjectUnzipPath);

	public override string name => "Unity Mod Project";

	public override string icon => "Media/Menu/ColossalLogo.svg";

	public override string version
	{
		get
		{
			if (LongFile.Exists(kModProjectsVersionPath) && base.version == null)
			{
				version = ReadYAMLVersion(LongFile.ReadAllLines(kModProjectsVersionPath)).shortVersion;
			}
			return base.version;
		}
		protected set
		{
			base.version = value;
		}
	}

	public override IEnumerable<string> envVariables
	{
		get
		{
			yield return "CSII_PATHSET";
			yield return "CSII_UNITYMODPROJECTPATH";
			yield return "CSII_UNITYVERSION";
			yield return "CSII_ENTITIESVERSION";
			yield return "CSII_ASSEMBLYSEARCHPATH";
		}
	}

	public override Type[] dependsOnInstallation => new Type[2]
	{
		typeof(UnityDependency),
		typeof(UnityLicenseDependency)
	};

	public override LocalizedString installDescr => LocalizedString.Id("Options.WARN_TOOLCHAIN_INSTALL_MOD_PROJECT");

	public override Task<bool> IsInstalled(CancellationToken token)
	{
		return Task.FromResult(LongDirectory.Exists(kProjectUnzipPath + "/Library") && LongFile.Exists(kModProjectsUnityVersionPath));
	}

	public override Task<bool> IsUpToDate(CancellationToken token)
	{
		try
		{
			Colossal.Version obj = ReadUnityProjectVersion(kModProjectsUnityVersionPath);
			Colossal.Version version = new Colossal.Version(UnityDependency.sUnityVersion);
			Colossal.Version version2 = ReadYAMLVersion(LongFile.ReadAllLines(kModProjectsVersionPath));
			Colossal.Version version3 = ReadYAMLVersion(ZipUtilities.ExtractAllLines(kProjectZipPath, "ProjectSettings/ProjectSettings.asset"));
			if (obj < version || version2 < version3)
			{
				return Task.FromResult(result: false);
			}
			if (!LongFile.Exists(kModProjectPackages))
			{
				return Task.FromResult(result: false);
			}
			Variant variant = JSON.Load(LongFile.ReadAllText(kModProjectPackages));
			Variant variant2 = JSON.Load(ZipUtilities.ExtractAllText(kProjectZipPath, "Packages/manifest.json"));
			ProxyObject proxyObject = variant.TryGet("dependencies") as ProxyObject;
			ProxyObject proxyObject2 = variant2.TryGet("dependencies") as ProxyObject;
			if (proxyObject == null || proxyObject2 == null)
			{
				return Task.FromResult(result: false);
			}
			foreach (KeyValuePair<string, Variant> item in (IEnumerable<KeyValuePair<string, Variant>>)proxyObject2)
			{
				if (!proxyObject.TryGetValue(item.Key, out var variant3))
				{
					return Task.FromResult(result: false);
				}
				if (!(variant3 is ProxyObject proxyObject3))
				{
					return Task.FromResult(result: false);
				}
				if (!proxyObject3.TryGetValue("version", out var variant4) || !variant4.Equals(item.Value))
				{
					return Task.FromResult(result: false);
				}
			}
			return Task.FromResult(result: true);
		}
		catch (Exception exception)
		{
			IToolchainDependency.log.Error(exception, "Error during up-to-date check");
			return Task.FromResult(result: false);
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
		string zipPath = kProjectZipPath;
		string unzipPath = kProjectUnzipPath;
		try
		{
			if (isUnityOpened)
			{
				IToolchainDependency.log.Debug("Waiting for close Unity");
				base.state = new IToolchainDependency.State(DependencyState.Installing, "WaitingUnityClose");
				await AsyncUtils.WaitForAction(() => !isUnityOpened, token).ConfigureAwait(continueOnCapturedContext: false);
			}
			token.ThrowIfCancellationRequested();
			IToolchainDependency.log.DebugFormat("Deploy Mods project from '{0}' to '{1}'", zipPath, unzipPath);
			base.state = new IToolchainDependency.State(DependencyState.Installing, "InstallingModProject");
			if (LongDirectory.Exists(unzipPath))
			{
				IToolchainDependency.log.DebugFormat("Remove directory '{0}'", unzipPath);
				await AsyncUtils.DeleteDirectoryAsync(unzipPath, recursive: true, token).ConfigureAwait(continueOnCapturedContext: false);
			}
			IToolchainDependency.log.DebugFormat("Unzip archive '{0}' to '{1}'", zipPath, unzipPath);
			await Task.Run(delegate
			{
				ZipUtilities.Unzip(zipPath, unzipPath);
			}, token).ConfigureAwait(continueOnCapturedContext: false);
			string unityExePath = UnityDependency.unityExePath;
			if (string.IsNullOrEmpty(unityExePath) || !LongFile.Exists(unityExePath))
			{
				throw new ToolchainException(ToolchainError.Install, this, "Unity installation path is incorrect or does not exist: " + unityExePath);
			}
			IToolchainDependency.log.DebugFormat("Launching Unity '{0}'", unityExePath);
			CliWrap.Command command = Cli.Wrap(unityExePath).WithArguments(new string[5] { "-projectPath", unzipPath, "-logFile", "-", "-quit" });
			IToolchainDependency.log.TraceFormat("call '{0}'", command);
			await foreach (CommandEvent item in command.ListenAsync(token).ConfigureAwait(continueOnCapturedContext: false))
			{
				if (!(item is StandardOutputCommandEvent standardOutputCommandEvent))
				{
					if (item is StandardErrorCommandEvent standardErrorCommandEvent && !standardErrorCommandEvent.Text.StartsWith("debugger-agent: Unable to listen on"))
					{
						IToolchainDependency.log.Error(standardErrorCommandEvent.Text);
					}
				}
				else
				{
					IToolchainDependency.log.Trace(standardOutputCommandEvent.Text);
				}
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
			IToolchainDependency.log.Debug("Deleting Mods project");
			base.state = new IToolchainDependency.State(DependencyState.Installing, "RemovingModProject");
			string text = kProjectUnzipPath;
			if (LongDirectory.Exists(text))
			{
				IToolchainDependency.log.DebugFormat("Remove directory '{0}'", text);
				await AsyncUtils.DeleteDirectoryAsync(text, recursive: true, token).ConfigureAwait(continueOnCapturedContext: false);
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
				m_Path = kProjectUnzipPath,
				m_Size = 1073741824L
			}
		});
	}

	private static Colossal.Version ReadUnityProjectVersion(string path)
	{
		return new Colossal.Version(LongFile.ReadAllLines(path)[0].Split(':')[1].Trim());
	}

	private static Colossal.Version ReadYAMLVersion(IEnumerable<string> lines)
	{
		foreach (string line in lines)
		{
			if (line.Contains("bundleVersion:"))
			{
				return new Colossal.Version(line.Split(':')[1].Trim());
			}
		}
		throw new Exception();
	}

	private static bool IsUnityOpenWithModsProject(string projectPath)
	{
		try
		{
			Process[] processesByName = Process.GetProcessesByName("unity");
			for (int i = 0; i < processesByName.Length; i++)
			{
				string parameterValue;
				int num = ProcessCommandLine.Retrieve(processesByName[i], out parameterValue);
				if (num == 0)
				{
					string openProjectPath = string.Empty;
					new OptionSet().Add("projectpath=", "", delegate(string option)
					{
						openProjectPath = option;
					}).Parse(ProcessCommandLine.CommandLineToArgs(parameterValue));
					if (!string.IsNullOrEmpty(openProjectPath) && Path.GetFullPath(openProjectPath) == Path.GetFullPath(projectPath))
					{
						return true;
					}
				}
				else
				{
					IToolchainDependency.log.DebugFormat("Unable to get command line ({0}): {1}", num, ProcessCommandLine.ErrorToString(num));
				}
			}
		}
		catch (Exception exception)
		{
			IToolchainDependency.log.Warn(exception);
		}
		return false;
	}
}
