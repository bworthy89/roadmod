using System;

namespace Game.Settings;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class SettingsUITabWarningAttribute : Attribute
{
	public readonly string tab;

	public readonly Type checkType;

	public readonly string checkMethod;

	public SettingsUITabWarningAttribute(string tab, Type checkType, string checkMethod)
	{
		this.tab = tab;
		this.checkType = checkType;
		this.checkMethod = checkMethod;
	}
}
