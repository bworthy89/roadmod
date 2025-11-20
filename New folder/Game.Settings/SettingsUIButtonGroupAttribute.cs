using System;

namespace Game.Settings;

[AttributeUsage(AttributeTargets.Property, Inherited = true)]
public class SettingsUIButtonGroupAttribute : Attribute
{
	public readonly string name;

	public SettingsUIButtonGroupAttribute(string name)
	{
		this.name = name;
	}
}
