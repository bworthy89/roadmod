using System.Collections.Generic;
using System.Linq;
using Colossal.IO.AssetDatabase;
using Game.Input;

namespace Game.Settings;

[FileLocation("Settings")]
public class KeybindingSettings : Setting
{
	private bool m_IsDefault;

	[SettingsUIHidden]
	public List<ProxyBinding> bindings
	{
		get
		{
			return InputManager.instance.GetBindings(m_IsDefault ? InputManager.PathType.Original : InputManager.PathType.Effective, InputManager.BindingOptions.OnlyRebound | InputManager.BindingOptions.OnlyBuiltIn).ToList();
		}
		set
		{
			if (!m_IsDefault)
			{
				InputManager.instance.SetBindings(value, out var _);
			}
		}
	}

	public KeybindingSettings(bool isDefault = false)
	{
		m_IsDefault = isDefault;
	}

	public override void SetDefaults()
	{
	}
}
