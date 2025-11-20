using System;

namespace Game.Rendering.CinematicCamera;

public class PhotoModeProperty
{
	public enum OverrideControl
	{
		None,
		Checkbox,
		ColorField
	}

	public string id { get; set; }

	public string group { get; set; }

	public Action<float> setValue { get; set; }

	public Func<float> getValue { get; set; }

	public Func<float> min { get; set; }

	public Func<float> max { get; set; }

	public Func<bool> isAvailable { get; set; }

	public Func<bool> isEnabled { get; set; }

	public Action<bool> setEnabled { get; set; }

	public Action reset { get; set; }

	public int fractionDigits { get; set; } = 3;

	public Type enumType { get; set; }

	public OverrideControl overrideControl { get; set; }
}
