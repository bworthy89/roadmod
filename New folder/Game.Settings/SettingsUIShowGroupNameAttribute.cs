using System;
using System.Collections.ObjectModel;

namespace Game.Settings;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited = true)]
public class SettingsUIShowGroupNameAttribute : Attribute
{
	public readonly bool showAll;

	public readonly ReadOnlyCollection<string> groups;

	public SettingsUIShowGroupNameAttribute()
	{
		showAll = true;
	}

	public SettingsUIShowGroupNameAttribute(params string[] groups)
	{
		this.groups = new ReadOnlyCollection<string>(groups);
	}
}
