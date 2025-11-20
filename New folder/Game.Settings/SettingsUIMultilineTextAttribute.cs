using System;

namespace Game.Settings;

[AttributeUsage(AttributeTargets.Property, Inherited = true)]
public class SettingsUIMultilineTextAttribute : Attribute
{
	public readonly string icon;

	public SettingsUIMultilineTextAttribute(string icon = null)
	{
		this.icon = icon;
	}
}
