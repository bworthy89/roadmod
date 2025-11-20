using Game.Rendering.Utilities;
using UnityEngine;

namespace Game.Settings;

[SettingsUIAdvanced]
[SettingsUISection("DynamicResolutionScaleSettings")]
[SettingsUIDisableByCondition(typeof(DynamicResolutionScaleSettings), "IsOptionsDisabled")]
public class DynamicResolutionScaleSettings : QualitySetting<DynamicResolutionScaleSettings>
{
	private static Camera m_Camera;

	[SettingsUIHidden]
	public bool enabled { get; set; }

	public bool isAdaptive { get; set; }

	public AdaptiveDynamicResolutionScale.DynResUpscaleFilter upscaleFilter { get; set; }

	[SettingsUISlider(min = 50f, max = 100f, step = 1f, unit = "percentageSingleFraction", scalarMultiplier = 100f)]
	public float minScale { get; set; }

	private static DynamicResolutionScaleSettings constantQuality => new DynamicResolutionScaleSettings
	{
		enabled = true,
		isAdaptive = false,
		upscaleFilter = AdaptiveDynamicResolutionScale.DynResUpscaleFilter.EdgeAdaptiveScaling,
		minScale = 0.5f
	};

	private static DynamicResolutionScaleSettings automaticQuality => new DynamicResolutionScaleSettings
	{
		enabled = true,
		isAdaptive = true,
		upscaleFilter = AdaptiveDynamicResolutionScale.DynResUpscaleFilter.EdgeAdaptiveScaling,
		minScale = 0.5f
	};

	private static DynamicResolutionScaleSettings disabledQuality => new DynamicResolutionScaleSettings
	{
		enabled = false
	};

	public DynamicResolutionScaleSettings()
	{
	}

	static DynamicResolutionScaleSettings()
	{
		QualitySetting<DynamicResolutionScaleSettings>.RegisterMockName(Level.Low, "Constant");
		QualitySetting<DynamicResolutionScaleSettings>.RegisterMockName(Level.Medium, "Automatic");
		QualitySetting<DynamicResolutionScaleSettings>.RegisterMockName(Level.High, "Disabled");
		QualitySetting<DynamicResolutionScaleSettings>.RegisterSetting(Level.Low, constantQuality);
		QualitySetting<DynamicResolutionScaleSettings>.RegisterSetting(Level.Medium, automaticQuality);
		QualitySetting<DynamicResolutionScaleSettings>.RegisterSetting(Level.High, disabledQuality);
	}

	public DynamicResolutionScaleSettings(Level quality)
	{
		SetLevel(quality, apply: false);
	}

	public override void Apply()
	{
		base.Apply();
		if (TryGetGameplayCamera(ref m_Camera))
		{
			AdaptiveDynamicResolutionScale.instance.SetParams(enabled, isAdaptive, minScale, upscaleFilter, m_Camera);
		}
	}

	public override bool IsOptionsDisabled()
	{
		if (enabled)
		{
			return IsOptionFullyDisabled();
		}
		return true;
	}

	public override bool IsOptionFullyDisabled()
	{
		if (!base.IsOptionFullyDisabled() && !SharedSettings.instance.graphics.isDlssActive)
		{
			return SharedSettings.instance.graphics.isFsr2Active;
		}
		return true;
	}
}
