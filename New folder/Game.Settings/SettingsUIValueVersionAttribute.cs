using System;

namespace Game.Settings;

[AttributeUsage(AttributeTargets.Property, Inherited = true)]
public class SettingsUIValueVersionAttribute : Attribute
{
	public readonly Type versionGetterType;

	public readonly string versionGetterMethod;

	public SettingsUIValueVersionAttribute(Type versionGetterType, string versionGetterMethod)
	{
		this.versionGetterType = versionGetterType;
		this.versionGetterMethod = versionGetterMethod;
	}
}
