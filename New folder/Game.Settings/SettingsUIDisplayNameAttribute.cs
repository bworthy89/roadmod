using System;

namespace Game.Settings;

[AttributeUsage(AttributeTargets.Enum | AttributeTargets.Property, Inherited = true)]
public class SettingsUIDisplayNameAttribute : Attribute
{
	public readonly string id;

	public readonly string value;

	public readonly Type getterType;

	public readonly string getterMethod;

	public SettingsUIDisplayNameAttribute(string overrideId = null, string overrideValue = null)
	{
		id = overrideId;
		value = overrideValue;
	}

	public SettingsUIDisplayNameAttribute(Type getterType, string getterMethod)
	{
		this.getterType = getterType;
		this.getterMethod = getterMethod;
	}
}
