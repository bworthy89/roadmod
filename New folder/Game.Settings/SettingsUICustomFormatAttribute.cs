using System;

namespace Game.Settings;

[AttributeUsage(AttributeTargets.Property, Inherited = true)]
public class SettingsUICustomFormatAttribute : Attribute
{
	public int fractionDigits;

	public bool separateThousands = true;

	public float maxValueWithFraction = 100f;

	public bool signed;
}
