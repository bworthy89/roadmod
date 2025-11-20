using System;

namespace Game.Settings;

[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public class SettingsUIWarningAttribute : Attribute
{
	public readonly Type checkType;

	public readonly string checkMethod;

	public SettingsUIWarningAttribute(Type checkType, string checkMethod)
	{
		this.checkType = checkType;
		this.checkMethod = checkMethod;
	}
}
