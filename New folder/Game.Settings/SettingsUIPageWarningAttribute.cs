using System;

namespace Game.Settings;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public class SettingsUIPageWarningAttribute : Attribute
{
	public readonly Type checkType;

	public readonly string checkMethod;

	public SettingsUIPageWarningAttribute(Type checkType, string checkMethod)
	{
		this.checkType = checkType;
		this.checkMethod = checkMethod;
	}
}
