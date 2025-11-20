using System.Collections.Generic;
using System.Linq;
using Game.Modding.Toolchain;
using Game.Reflection;
using Game.Settings;
using Game.UI.Widgets;

namespace Game.UI.Menu;

public class ModdingToolchainSettingItem : AutomaticSettings.SettingItemData
{
	public List<AutomaticSettings.SettingItemData> children { get; } = new List<AutomaticSettings.SettingItemData>();

	public ModdingToolchainSettingItem(Setting setting, AutomaticSettings.IProxyProperty property, string prefix)
		: base(AutomaticSettings.WidgetType.None, setting, property, prefix)
	{
	}

	protected override IWidget GetWidget()
	{
		return new ModdingToolchainDependency
		{
			path = base.path,
			displayName = base.displayName,
			description = base.description,
			displayNameAction = base.dispayNameAction,
			descriptionAction = base.descriptionAction,
			accessor = new DelegateAccessor<IToolchainDependency>(() => (IToolchainDependency)base.property.GetValue(base.setting)),
			valueVersion = base.valueVersionAction,
			disabled = base.disableAction,
			hidden = base.hideAction,
			children = (from c in children
				select c.widget into w
				where w != null
				select w).ToArray()
		};
	}
}
