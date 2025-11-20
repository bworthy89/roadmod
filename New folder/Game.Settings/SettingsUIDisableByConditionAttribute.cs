using System;

namespace Game.Settings;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Property, Inherited = true)]
public class SettingsUIDisableByConditionAttribute : Attribute
{
	public readonly Type checkType;

	public readonly string checkMethod;

	public readonly bool invert;

	public SettingsUIDisableByConditionAttribute(Type checkType, string checkMethod)
	{
		this.checkType = checkType;
		this.checkMethod = checkMethod;
	}

	public SettingsUIDisableByConditionAttribute(Type checkType, string checkMethod, bool invert)
	{
		this.checkType = checkType;
		this.checkMethod = checkMethod;
		this.invert = invert;
	}
}
