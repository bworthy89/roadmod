using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace Game.Settings;

[SettingsUIAdvanced]
[SettingsUISection("SSAOQualitySettings")]
[SettingsUIDisableByCondition(typeof(SSAOQualitySettings), "IsOptionsDisabled")]
public class SSAOQualitySettings : QualitySetting<SSAOQualitySettings>
{
	private static ScreenSpaceAmbientOcclusion m_AOComponent;

	[SettingsUIHidden]
	public bool enabled { get; set; }

	[SettingsUISlider(min = 16f, max = 256f, step = 1f, unit = "integer")]
	public int maxPixelRadius { get; set; }

	public bool fullscreen { get; set; }

	[SettingsUISlider(min = 2f, max = 32f, step = 1f, unit = "integer")]
	public int stepCount { get; set; }

	private static SSAOQualitySettings highQuality => new SSAOQualitySettings
	{
		enabled = true,
		stepCount = 16,
		maxPixelRadius = 80,
		fullscreen = true
	};

	private static SSAOQualitySettings mediumQuality => new SSAOQualitySettings
	{
		enabled = true,
		stepCount = 6,
		maxPixelRadius = 40,
		fullscreen = true
	};

	private static SSAOQualitySettings lowQuality => new SSAOQualitySettings
	{
		enabled = true,
		stepCount = 4,
		maxPixelRadius = 32,
		fullscreen = false
	};

	private static SSAOQualitySettings disabled => new SSAOQualitySettings
	{
		enabled = false
	};

	static SSAOQualitySettings()
	{
		QualitySetting<SSAOQualitySettings>.RegisterSetting(Level.Disabled, disabled);
		QualitySetting<SSAOQualitySettings>.RegisterSetting(Level.Low, lowQuality);
		QualitySetting<SSAOQualitySettings>.RegisterSetting(Level.Medium, mediumQuality);
		QualitySetting<SSAOQualitySettings>.RegisterSetting(Level.High, highQuality);
	}

	public SSAOQualitySettings()
	{
	}

	public SSAOQualitySettings(Level quality, VolumeProfile profile)
	{
		CreateVolumeComponent(profile, ref m_AOComponent);
		SetLevel(quality, apply: false);
	}

	public override void Apply()
	{
		base.Apply();
		if (m_AOComponent != null)
		{
			ApplyState(m_AOComponent.intensity, 0f, !enabled);
			ApplyState(m_AOComponent.m_FullResolution, fullscreen);
			ApplyState(m_AOComponent.m_MaximumRadiusInPixels, maxPixelRadius);
			ApplyState(m_AOComponent.m_StepCount, stepCount);
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
