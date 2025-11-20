using System;
using UnityEngine;

namespace Game.Settings;

public static class GraphicsSettingsExtensions
{
	public static CursorLockMode ToUnityCursorMode(this GraphicsSettings.CursorMode mode)
	{
		return mode switch
		{
			GraphicsSettings.CursorMode.Free => CursorLockMode.None, 
			GraphicsSettings.CursorMode.ConfinedToWindow => CursorLockMode.Confined, 
			_ => throw new ArgumentException($"Unsupported cursor mode: {mode}", "mode"), 
		};
	}
}
