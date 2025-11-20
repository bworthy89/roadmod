using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Game.Settings;

public static class ScreenHelper
{
	private static List<DisplayInfo> m_DisplayInfos;

	public static ScreenResolution currentResolution
	{
		get
		{
			Resolution resolution = Screen.currentResolution;
			if (!Screen.fullScreen)
			{
				return new ScreenResolution
				{
					width = Screen.width,
					height = Screen.height,
					refreshRate = resolution.refreshRateRatio
				};
			}
			return new ScreenResolution(resolution);
		}
	}

	private static ScreenResolution[] resolutions { get; set; }

	private static ScreenResolution[] simpleResolutions { get; set; }

	public static DisplayMode currentDisplayMode => Screen.fullScreenMode switch
	{
		FullScreenMode.ExclusiveFullScreen => DisplayMode.Fullscreen, 
		FullScreenMode.FullScreenWindow => DisplayMode.FullscreenWindow, 
		FullScreenMode.Windowed => DisplayMode.Window, 
		_ => DisplayMode.Window, 
	};

	public static ScreenResolution[] GetAvailableResolutions(bool all)
	{
		if (!all)
		{
			return simpleResolutions;
		}
		return resolutions;
	}

	static ScreenHelper()
	{
		m_DisplayInfos = new List<DisplayInfo>();
		RebuildResolutions();
	}

	public static void RebuildResolutions()
	{
		resolutions = (from x in new List<ScreenResolution>(Screen.resolutions.Select((Resolution r) => new ScreenResolution(r)))
			orderby x.width descending, x.height descending, x.refreshRate.value descending
			select x).ToArray();
		simpleResolutions = (from r in resolutions
			group r by (width: r.width, height: r.height, refreshRate: (int)Math.Round(r.refreshRate.value)) into g
			select g.Aggregate(g.First(), (ScreenResolution a, ScreenResolution b) => (!(b.refreshRateDelta < a.refreshRateDelta)) ? a : b)).ToArray();
	}

	public static ScreenResolution GetClosestAvailable(ScreenResolution sample, bool all)
	{
		ScreenResolution screenResolution = default(ScreenResolution);
		ScreenResolution[] availableResolutions = GetAvailableResolutions(all);
		for (int i = 0; i < availableResolutions.Length; i++)
		{
			ScreenResolution screenResolution2 = availableResolutions[i];
			if (Math.Abs(screenResolution2.width - sample.width) + Math.Abs(screenResolution2.height - sample.height) < Math.Abs(screenResolution.width - sample.width) + Math.Abs(screenResolution.height - sample.height))
			{
				screenResolution = screenResolution2;
			}
			else if (screenResolution2.width == screenResolution.width && screenResolution2.height == screenResolution.height)
			{
				RefreshRate refreshRate = screenResolution2.refreshRate;
				if (Math.Abs(refreshRate.value - sample.refreshRate.value) < Math.Abs(screenResolution.refreshRate.value - sample.refreshRate.value))
				{
					screenResolution = screenResolution2;
				}
			}
		}
		ScreenResolution result = ((screenResolution.width > 0 && screenResolution.height > 0) ? screenResolution : sample);
		result.Sanitize();
		return result;
	}

	public static bool HideAdditionalResolutionOption()
	{
		return simpleResolutions.Length == resolutions.Length;
	}

	public static bool HasMultipleDisplay()
	{
		m_DisplayInfos.Clear();
		Screen.GetDisplayLayout(m_DisplayInfos);
		return m_DisplayInfos.Count > 1;
	}

	public static FullScreenMode GetFullscreenMode(DisplayMode displayMode)
	{
		return displayMode switch
		{
			DisplayMode.Fullscreen => FullScreenMode.ExclusiveFullScreen, 
			DisplayMode.FullscreenWindow => FullScreenMode.FullScreenWindow, 
			DisplayMode.Window => FullScreenMode.Windowed, 
			_ => FullScreenMode.Windowed, 
		};
	}
}
