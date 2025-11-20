using System;
using System.Collections.ObjectModel;

namespace Game.Settings;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public class SettingsUIGroupOrderAttribute : Attribute
{
	public readonly ReadOnlyCollection<string> groups;

	public readonly Type checkType;

	public readonly string checkMethod;

	public SettingsUIGroupOrderAttribute(params string[] groups)
	{
		this.groups = new ReadOnlyCollection<string>(groups);
	}

	public SettingsUIGroupOrderAttribute(Type checkType, string checkMethod)
	{
		this.checkType = checkType;
		this.checkMethod = checkMethod;
	}
}
