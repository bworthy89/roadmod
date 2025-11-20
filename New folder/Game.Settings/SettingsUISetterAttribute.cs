using System;

namespace Game.Settings;

[AttributeUsage(AttributeTargets.Property, Inherited = true)]
public class SettingsUISetterAttribute : Attribute
{
	public readonly Type setterType;

	public readonly string setterMethod;

	public SettingsUISetterAttribute(Type setterType, string setterMethod)
	{
		this.setterType = setterType;
		this.setterMethod = setterMethod;
	}
}
