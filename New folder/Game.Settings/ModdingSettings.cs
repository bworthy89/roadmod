using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Colossal.IO.AssetDatabase;
using Colossal.Json;
using Colossal.PSI.Environment;
using Game.Modding.Toolchain;
using Game.Modding.Toolchain.Dependencies;
using Game.SceneFlow;
using Game.UI;
using Game.UI.Localization;
using Game.UI.Menu;
using Game.UI.Widgets;

namespace Game.Settings;

[FileLocation("Settings")]
[SettingsUIShowGroupName(new string[] { "Dependencies" })]
[SettingsUIGroupOrder(new string[] { "Disclaimer", "Main", "Dependencies" })]
[SettingsUIPageWarning(typeof(ModdingSettings), "showWarning")]
public class ModdingSettings : Setting
{
	public const string kName = "Modding";

	public const string kDisclaimer = "Disclaimer";

	public const string kMain = "Main";

	public const string kDependencies = "Dependencies";

	[SettingsUIHidden]
	public bool isInstalled { get; set; }

	[SettingsUISection("Main")]
	[SettingsUIButtonGroup("toolchainAction")]
	[SettingsUIHideByCondition(typeof(ModdingSettings), "canNotBeInstalled")]
	[SettingsUIDisableByCondition(typeof(ModdingSettings), "isActionDisabled")]
	[SettingsUISearchHidden]
	public bool installModdingToolchain
	{
		set
		{
			ToolchainDeployment.RunWithUI(DeploymentAction.Install, null, delegate(bool success)
			{
				if (success)
				{
					isInstalled = true;
				}
			});
		}
	}

	[SettingsUISection("Main")]
	[SettingsUIButtonGroup("toolchainAction")]
	[SettingsUIHideByCondition(typeof(ModdingSettings), "canNotBeUninstalled")]
	[SettingsUIDisableByCondition(typeof(ModdingSettings), "isActionDisabled")]
	[SettingsUISearchHidden]
	public bool uninstallModdingToolchain
	{
		set
		{
			ToolchainDeployment.RunWithUI(DeploymentAction.Uninstall, null, delegate(bool success)
			{
				if (success)
				{
					isInstalled = false;
				}
			});
		}
	}

	[SettingsUISection("Main")]
	[SettingsUIButtonGroup("toolchainAction")]
	[SettingsUIHideByCondition(typeof(ModdingSettings), "canNotBeRepaired")]
	[SettingsUIDisableByCondition(typeof(ModdingSettings), "isActionDisabled")]
	[SettingsUISearchHidden]
	public bool repairModdingToolchain
	{
		set
		{
			ToolchainDeployment.RunWithUI(DeploymentAction.Repair, null, delegate(bool success)
			{
				if (success)
				{
					isInstalled = true;
				}
			});
		}
	}

	[SettingsUISection("Main")]
	[SettingsUIButtonGroup("toolchainAction")]
	[SettingsUIHideByCondition(typeof(ModdingSettings), "canNotBeUpdated")]
	[SettingsUIDisableByCondition(typeof(ModdingSettings), "isActionDisabled")]
	[SettingsUISearchHidden]
	public bool updateModdingToolchain
	{
		set
		{
			ToolchainDeployment.RunWithUI(DeploymentAction.Update, null, delegate(bool success)
			{
				if (success)
				{
					isInstalled = true;
				}
			});
		}
	}

	[SettingsUIDeveloper]
	[SettingsUIButtonGroup("envSetVars")]
	[SettingsUIHideByCondition(typeof(ModdingSettings), "disableEnvVarUpdate")]
	[SettingsUIDisableByCondition(typeof(ModdingSettings), "isActionDisabled")]
	[SettingsUIDisplayName(null, "Show env vars")]
	[SettingsUIDescription(null, "Show environment variable currently set values")]
	public bool showEnvVars
	{
		set
		{
			List<string> list = new List<string>();
			foreach (string key in IToolchainDependency.envVars.Keys)
			{
				string environmentVariable = Environment.GetEnvironmentVariable(key, EnvironmentVariableTarget.User);
				if (environmentVariable != null)
				{
					list.Add("**" + key + "** = " + environmentVariable.Replace("\\", "\\\\"));
				}
			}
			MessageDialog dialog = new MessageDialog("envVars", LocalizedString.Value("Values which set to variables"), LocalizedString.Value((list.Count != 0) ? string.Join("\n", list) : "Nothing set"), true, LocalizedString.Id("Common.OK"));
			GameManager.instance.userInterface.appBindings.ShowMessageDialog(dialog, null);
		}
	}

	[SettingsUIDeveloper]
	[SettingsUIButtonGroup("envSetVars")]
	[SettingsUIHideByCondition(typeof(ModdingSettings), "disableEnvVarUpdate")]
	[SettingsUIDisableByCondition(typeof(ModdingSettings), "isActionDisabled")]
	[SettingsUIDisplayName(null, "Show actual values")]
	[SettingsUIDescription(null, "Show actual values which are supposed to be set.")]
	public bool showCurrentValues
	{
		set
		{
			List<string> values = IToolchainDependency.envVars.Select((KeyValuePair<string, string> env) => "**" + env.Key + "** = " + env.Value.Replace("\\", "\\\\")).ToList();
			MessageDialog dialog = new MessageDialog("envVars", LocalizedString.Value("Current values"), LocalizedString.Value(string.Join("\n", values)), true, LocalizedString.Id("Common.OK"));
			GameManager.instance.userInterface.appBindings.ShowMessageDialog(dialog, null);
		}
	}

	[SettingsUIDeveloper]
	[SettingsUIButtonGroup("envVars")]
	[SettingsUIHideByCondition(typeof(ModdingSettings), "disableEnvVarUpdate")]
	[SettingsUIDisableByCondition(typeof(ModdingSettings), "isActionDisabled")]
	[SettingsUIDisplayName(null, "Update env vars")]
	[SettingsUIDescription(null, "Update environment variables with actual values.")]
	public bool updateEnvVars
	{
		set
		{
			ToolchainDependencyManager.UserEnvironmentVariableManager.SetEnvVars(IToolchainDependency.envVars.Keys.ToArray());
			Task.Run((Func<Task<ToolchainDependencyManager.State>>)ToolchainDeployment.dependencyManager.GetCurrentState);
		}
	}

	[SettingsUIDeveloper]
	[SettingsUIButtonGroup("envVars")]
	[SettingsUIHideByCondition(typeof(ModdingSettings), "disableEnvVarUpdate")]
	[SettingsUIDisableByCondition(typeof(ModdingSettings), "isActionDisabled")]
	[SettingsUIDisplayName(null, "Remove env vars")]
	[SettingsUIDescription(null, "Remove environment variable values.")]
	public bool removeEnvVars
	{
		set
		{
			ToolchainDependencyManager.UserEnvironmentVariableManager.RemoveEnvVars();
			Task.Run((Func<Task<ToolchainDependencyManager.State>>)ToolchainDeployment.dependencyManager.GetCurrentState);
		}
	}

	private bool disableEnvVarUpdate => !isInstalled;

	[SettingsUISection("Main")]
	[SettingsUIDirectoryPicker]
	[SettingsUIHideByCondition(typeof(ModdingSettings), "noNeedDownloadPath")]
	[SettingsUIDisableByCondition(typeof(ModdingSettings), "isActionDisabled")]
	[Exclude]
	public string downloadDirectory { get; set; } = EnvPath.kTempDataPath;

	public bool isActionDisabled => ToolchainDeployment.dependencyManager.cachedState.m_Status != ModdingToolStatus.Idle;

	public bool canNotBeInstalled => (DeploymentState)ToolchainDeployment.dependencyManager.cachedState != DeploymentState.NotInstalled;

	public bool canNotBeUninstalled => (DeploymentState)ToolchainDeployment.dependencyManager.cachedState == DeploymentState.NotInstalled;

	public bool canNotBeRepaired => (DeploymentState)ToolchainDeployment.dependencyManager.cachedState != DeploymentState.Invalid;

	public bool canNotBeUpdated => (DeploymentState)ToolchainDeployment.dependencyManager.cachedState != DeploymentState.Outdated;

	public bool noNeedDownloadPath => (DeploymentState)ToolchainDeployment.dependencyManager.cachedState == DeploymentState.Installed;

	public bool showWarning
	{
		get
		{
			if (isInstalled)
			{
				if ((DeploymentState)ToolchainDeployment.dependencyManager.cachedState != DeploymentState.Invalid)
				{
					return (DeploymentState)ToolchainDeployment.dependencyManager.cachedState == DeploymentState.Outdated;
				}
				return true;
			}
			return false;
		}
	}

	public override void SetDefaults()
	{
	}

	public override AutomaticSettings.SettingPageData GetPageData(string id, bool addPrefix)
	{
		AutomaticSettings.SettingPageData pageData = base.GetPageData(id, addPrefix);
		pageData.AddGroup("Disclaimer");
		AutomaticSettings.ManualProperty property = new AutomaticSettings.ManualProperty(typeof(ModdingSettings), typeof(string), "disclaimer")
		{
			canRead = true,
			canWrite = false,
			attributes = 
			{
				(Attribute)new SettingsUIMultilineTextAttribute("Media/Misc/Warning.svg"),
				(Attribute)new SettingsUISearchHiddenAttribute()
			}
		};
		MultilineTextSettingItemData item = new MultilineTextSettingItemData(this, property, pageData.prefix)
		{
			simpleGroup = "Disclaimer"
		};
		pageData["General"].AddItem(item);
		AutomaticSettings.ManualProperty property2 = new AutomaticSettings.ManualProperty(typeof(ModdingSettings), typeof(IToolchainDependency), "ToolchainDeployment")
		{
			canRead = true,
			canWrite = false,
			getter = (object _) => ToolchainDependencyManager.m_MainDependency
		};
		ModdingToolchainSettingItem item2 = new ModdingToolchainSettingItem(this, property2, pageData.prefix)
		{
			simpleGroup = "Main",
			valueVersionAction = ToolchainDependencyManager.m_MainDependency.GetHashCode
		};
		pageData["General"].InsertItem(item2, 0);
		pageData.AddGroup("Dependencies");
		foreach (IToolchainDependency item3 in ToolchainDeployment.dependencyManager)
		{
			pageData["General"].AddItem(GetItem(item3, pageData));
		}
		return pageData;
	}

	private ModdingToolchainSettingItem GetItem(IToolchainDependency dependency, AutomaticSettings.SettingPageData pageData)
	{
		AutomaticSettings.ManualProperty property = new AutomaticSettings.ManualProperty(typeof(ModdingSettings), typeof(IToolchainDependency), dependency.GetType().Name)
		{
			canRead = true,
			canWrite = false,
			getter = (object obj) => dependency
		};
		ModdingToolchainSettingItem moddingToolchainSettingItem = new ModdingToolchainSettingItem(this, property, pageData.prefix)
		{
			simpleGroup = "Dependencies",
			valueVersionAction = dependency.GetHashCode,
			description = dependency.description
		};
		if (dependency.canBeInstalled)
		{
			foreach (DeploymentAction action in Enum.GetValues(typeof(DeploymentAction)))
			{
				if (action != DeploymentAction.None)
				{
					AutomaticSettings.ManualProperty property2 = new AutomaticSettings.ManualProperty(typeof(ModdingSettings), typeof(bool), action.ToString().ToLower() + "ModdingToolchain")
					{
						canRead = false,
						canWrite = true,
						setter = delegate
						{
							ToolchainDeployment.RunWithUI(action, new List<IToolchainDependency> { dependency });
						},
						attributes = { (Attribute)new SettingsUIButtonGroupAttribute(dependency.name + "toolchainAction") }
					};
					AutomaticSettings.SettingItemData item = new AutomaticSettings.SettingItemData(AutomaticSettings.WidgetType.BoolButton, this, property2, pageData.prefix)
					{
						hideAction = () => (dependency.availableActions & action) == 0,
						disableAction = () => isActionDisabled
					};
					moddingToolchainSettingItem.children.Add(item);
				}
			}
		}
		if (dependency.canBeInstalled && dependency.canChangeInstallationDirectory)
		{
			AutomaticSettings.ManualProperty property3 = new AutomaticSettings.ManualProperty(typeof(ModdingSettings), typeof(string), dependency.GetType().Name + ".InstallationDirectory")
			{
				canRead = true,
				canWrite = true,
				getter = (object obj) => dependency.installationDirectory,
				setter = delegate(object obj, object value)
				{
					dependency.OverrideInstallationDirectory((string)value);
				},
				attributes = { (Attribute)new SettingsUIDisplayNameAttribute("ModdingSettings.installationDirectory") }
			};
			AutomaticSettings.SettingItemData item2 = new AutomaticSettings.SettingItemData(AutomaticSettings.WidgetType.DirectoryPicker, this, property3, pageData.prefix)
			{
				disableAction = () => isActionDisabled || dependency.state.m_State == DependencyState.Installed
			};
			moddingToolchainSettingItem.children.Add(item2);
		}
		if (dependency is CombinedDependency combinedDependency)
		{
			moddingToolchainSettingItem.children.AddRange(combinedDependency.dependencies.Select((IToolchainDependency d) => GetItem(d, pageData)));
		}
		return moddingToolchainSettingItem;
	}
}
