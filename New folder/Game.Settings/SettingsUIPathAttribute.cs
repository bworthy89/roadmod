using System;

namespace Game.Settings;

[AttributeUsage(AttributeTargets.Property, Inherited = true)]
public class SettingsUIPathAttribute : Attribute
{
	public readonly string path;

	public SettingsUIPathAttribute(string overridePath)
	{
		path = overridePath;
	}
}
