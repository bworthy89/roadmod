using System;
using System.Text;
using ATL;
using cohtml.Net;
using Colossal.Core;
using Colossal.PSI.Common;
using Colossal.UI;
using Game.UI.Menu;
using UnityEngine;

namespace Game.Settings;

public class About : Setting
{
	public const string kName = "About";

	private const string kGameGroup = "Game";

	private const string kContentGroup = "Content";

	[SettingsUISection("kGameGroup")]
	public string gameVersion => Version.current.fullVersion;

	[SettingsUISection("kGameGroup")]
	public string gameConfiguration
	{
		get
		{
			if (!UnityEngine.Debug.isDebugBuild)
			{
				return "Release";
			}
			return "Development";
		}
	}

	public string coreVersion => Colossal.Core.Version.current.fullVersion;

	public string uiVersion => Colossal.UI.Version.current.fullVersion;

	public string unityVersion => Application.unityVersion;

	public string cohtmlVersion => Versioning.Build.ToString();

	public string atlVersion => ATL.Version.getVersion();

	public override void SetDefaults()
	{
	}

	public override AutomaticSettings.SettingPageData GetPageData(string id, bool addPrefix)
	{
		AutomaticSettings.SettingPageData pageData = base.GetPageData(id, addPrefix);
		foreach (IPlatformServiceIntegration platformServiceIntegration in PlatformManager.instance.platformServiceIntegrations)
		{
			StringBuilder stringBuilder = new StringBuilder();
			platformServiceIntegration.LogVersion(stringBuilder);
			string[] array = stringBuilder.ToString().Split(Environment.NewLine);
			int num = 0;
			string[] array2 = array;
			foreach (string line in array2)
			{
				int sep = line.IndexOf(":", StringComparison.Ordinal);
				if (sep != -1)
				{
					AutomaticSettings.ManualProperty property = new AutomaticSettings.ManualProperty(typeof(About), typeof(string), platformServiceIntegration.name)
					{
						canRead = true,
						canWrite = false,
						attributes = 
						{
							(Attribute)new SettingsUIPathAttribute($"{platformServiceIntegration.GetType().Name}{num++}.{platformServiceIntegration.name}"),
							(Attribute)new SettingsUIDisplayNameAttribute((string)null, line.Substring(0, sep))
						},
						getter = (object obj) => line.Substring(sep + 1)
					};
					AutomaticSettings.SettingItemData item = new AutomaticSettings.SettingItemData(AutomaticSettings.WidgetType.StringField, this, property, pageData.prefix);
					pageData["General"].AddItem(item);
				}
			}
		}
		pageData.AddGroup("Content");
		foreach (IDlc dlc in PlatformManager.instance.EnumerateLocalDLCs())
		{
			AutomaticSettings.ManualProperty property2 = new AutomaticSettings.ManualProperty(typeof(About), typeof(string), dlc.internalName)
			{
				canRead = true,
				canWrite = false,
				attributes = 
				{
					(Attribute)new SettingsUIPathAttribute(dlc.internalName),
					(Attribute)new SettingsUIDisplayNameAttribute((string)null, dlc.internalName + GetOwnershipCheckString(dlc)),
					(Attribute)new SettingsUIDescriptionAttribute((string)null, dlc.version.fullVersion + GetOwnershipString(dlc))
				},
				getter = (object obj) => dlc.version.fullVersion
			};
			AutomaticSettings.SettingItemData item2 = new AutomaticSettings.SettingItemData(AutomaticSettings.WidgetType.StringField, this, property2, pageData.prefix)
			{
				simpleGroup = "Content"
			};
			pageData["General"].AddItem(item2);
		}
		return pageData;
		static string GetOwnershipCheckString(IDlc dlc2)
		{
			if (!PlatformManager.instance.IsDlcOwned(dlc2))
			{
				return "*";
			}
			return string.Empty;
		}
		static string GetOwnershipString(IDlc dlc2)
		{
			if (!PlatformManager.instance.IsDlcOwned(dlc2))
			{
				return "\n*The Content is available on disk but not currently owned";
			}
			return string.Empty;
		}
	}
}
