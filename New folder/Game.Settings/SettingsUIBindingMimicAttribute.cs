using System;

namespace Game.Settings;

[AttributeUsage(AttributeTargets.Property)]
public class SettingsUIBindingMimicAttribute : Attribute
{
	public readonly string map;

	public readonly string action;

	public SettingsUIBindingMimicAttribute(string map, string action)
	{
		this.map = map;
		this.action = action;
	}
}
