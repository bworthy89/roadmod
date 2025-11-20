using System;
using System.Collections.ObjectModel;

namespace Game.Settings;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public class SettingsUITabOrderAttribute : Attribute
{
	public readonly ReadOnlyCollection<string> tabs;

	public readonly Type checkType;

	public readonly string checkMethod;

	public SettingsUITabOrderAttribute(params string[] tabs)
	{
		this.tabs = new ReadOnlyCollection<string>(tabs);
	}

	public SettingsUITabOrderAttribute(Type checkType, string checkMethod)
	{
		this.checkType = checkType;
		this.checkMethod = checkMethod;
	}
}
