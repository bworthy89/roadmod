using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace Game.Settings;

[SettingsUIAdvanced]
[SettingsUISection("SSRQualitySettings")]
[SettingsUIDisableByCondition(typeof(SSRQualitySettings), "IsOptionsDisabled")]
public class SSRQualitySettings : QualitySetting<SSRQualitySettings>
{
	private static ScreenSpaceReflection m_SSRComponent;

	[SettingsUIHidden]
	public bool enabled { get; set; }

	public bool enabledTransparent { get; set; }

	[SettingsUISlider(min = 1f, max = 128f, step = 1f, unit = "integer")]
	public int maxRaySteps { get; set; }

	private static SSRQualitySettings highQuality => new SSRQualitySettings
	{
		enabled = true,
		enabledTransparent = true,
		maxRaySteps = 64
	};

	private static SSRQualitySettings mediumQuality => new SSRQualitySettings
	{
		enabled = true,
		enabledTransparent = true,
		maxRaySteps = 32
	};

	private static SSRQualitySettings lowQuality => new SSRQualitySettings
	{
		enabled = true,
		enabledTransparent = false,
		maxRaySteps = 16
	};

	private static SSRQualitySettings disabled => new SSRQualitySettings
	{
		enabled = false,
		enabledTransparent = false
	};

	static SSRQualitySettings()
	{
		QualitySetting<SSRQualitySettings>.RegisterSetting(Level.Disabled, disabled);
		QualitySetting<SSRQualitySettings>.RegisterSetting(Level.Low, lowQuality);
		QualitySetting<SSRQualitySettings>.RegisterSetting(Level.Medium, mediumQuality);
		QualitySetting<SSRQualitySettings>.RegisterSetting(Level.High, highQuality);
	}

	public SSRQualitySettings()
	{
	}

	public SSRQualitySettings(Level quality, VolumeProfile profile)
	{
		CreateVolumeComponent(profile, ref m_SSRComponent);
		SetLevel(quality, apply: false);
	}

	public override void Apply()
	{
		base.Apply();
		if (m_SSRComponent != null)
		{
			ApplyState(m_SSRComponent.enabled, enabled);
			ApplyState(m_SSRComponent.enabledTransparent, enabled && enabledTransparent);
			ApplyState(m_SSRComponent.m_RayMaxIterations, maxRaySteps);
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
