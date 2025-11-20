using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace Game.Settings;

[SettingsUIAdvanced]
[SettingsUISection("MotionBlurQualitySettings")]
[SettingsUIDisableByCondition(typeof(MotionBlurQualitySettings), "IsOptionsDisabled")]
public class MotionBlurQualitySettings : QualitySetting<MotionBlurQualitySettings>
{
	private static MotionBlur m_MotionBlurComponent;

	[SettingsUIHidden]
	public bool enabled { get; set; }

	public int sampleCount { get; set; }

	private static MotionBlurQualitySettings highQuality => new MotionBlurQualitySettings
	{
		enabled = true,
		sampleCount = 12
	};

	private static MotionBlurQualitySettings mediumQuality => new MotionBlurQualitySettings
	{
		enabled = true,
		sampleCount = 8
	};

	private static MotionBlurQualitySettings lowQuality => new MotionBlurQualitySettings
	{
		enabled = true,
		sampleCount = 4
	};

	private static MotionBlurQualitySettings disabled => new MotionBlurQualitySettings
	{
		enabled = false
	};

	static MotionBlurQualitySettings()
	{
		QualitySetting<MotionBlurQualitySettings>.RegisterSetting(Level.Disabled, disabled);
		QualitySetting<MotionBlurQualitySettings>.RegisterSetting(Level.Low, lowQuality);
		QualitySetting<MotionBlurQualitySettings>.RegisterSetting(Level.Medium, mediumQuality);
		QualitySetting<MotionBlurQualitySettings>.RegisterSetting(Level.High, highQuality);
	}

	public MotionBlurQualitySettings()
	{
	}

	public MotionBlurQualitySettings(Level quality, VolumeProfile profile)
	{
		CreateVolumeComponent(profile, ref m_MotionBlurComponent);
		SetLevel(quality, apply: false);
	}

	public override void Apply()
	{
		base.Apply();
		if (m_MotionBlurComponent != null)
		{
			ApplyState(m_MotionBlurComponent.intensity, 0f, !enabled);
			ApplyState(m_MotionBlurComponent.m_SampleCount, sampleCount);
		}
	}

	public override bool IsOptionsDisabled()
	{
		if (!IsOptionFullyDisabled())
		{
			return !enabled;
		}
		return true;
	}
}
