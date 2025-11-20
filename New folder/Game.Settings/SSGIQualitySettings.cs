using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace Game.Settings;

[SettingsUIAdvanced]
[SettingsUISection("SSGIQualitySettings")]
[SettingsUIDisableByCondition(typeof(SSGIQualitySettings), "IsOptionsDisabled")]
public class SSGIQualitySettings : QualitySetting<SSGIQualitySettings>
{
	private static GlobalIllumination m_SSGIComponent;

	[SettingsUIHidden]
	public bool enabled { get; set; }

	public bool fullscreen { get; set; }

	[SettingsUISlider(min = 16f, max = 128f, step = 1f, unit = "integer")]
	public int raySteps { get; set; }

	[SettingsUISlider(min = 0.001f, max = 1f, step = 0.1f, unit = "floatSingleFraction")]
	public float denoiserRadius { get; set; }

	public bool halfResolutionPass { get; set; }

	public bool secondDenoiserPass { get; set; }

	[SettingsUISlider(min = 0f, max = 0.5f, step = 0.01f, unit = "floatSingleFraction")]
	public float depthBufferThickness { get; set; }

	private static SSGIQualitySettings highQuality => new SSGIQualitySettings
	{
		enabled = true,
		fullscreen = false,
		raySteps = 128,
		denoiserRadius = 0.5f,
		depthBufferThickness = 0.001f,
		halfResolutionPass = false,
		secondDenoiserPass = true
	};

	private static SSGIQualitySettings mediumQuality => new SSGIQualitySettings
	{
		enabled = true,
		fullscreen = false,
		raySteps = 64,
		denoiserRadius = 0.5f,
		depthBufferThickness = 0.001f,
		halfResolutionPass = false,
		secondDenoiserPass = true
	};

	private static SSGIQualitySettings lowQuality => new SSGIQualitySettings
	{
		enabled = true,
		fullscreen = false,
		raySteps = 32,
		denoiserRadius = 0.75f,
		depthBufferThickness = 0.001f,
		halfResolutionPass = true,
		secondDenoiserPass = true
	};

	private static SSGIQualitySettings disabled => new SSGIQualitySettings
	{
		enabled = false
	};

	static SSGIQualitySettings()
	{
		QualitySetting<SSGIQualitySettings>.RegisterSetting(Level.Disabled, disabled);
		QualitySetting<SSGIQualitySettings>.RegisterSetting(Level.Low, lowQuality);
		QualitySetting<SSGIQualitySettings>.RegisterSetting(Level.Medium, mediumQuality);
		QualitySetting<SSGIQualitySettings>.RegisterSetting(Level.High, highQuality);
	}

	public SSGIQualitySettings()
	{
	}

	public SSGIQualitySettings(Level quality, VolumeProfile profile)
	{
		CreateVolumeComponent(profile, ref m_SSGIComponent);
		SetLevel(quality, apply: false);
	}

	public override void Apply()
	{
		base.Apply();
		if (m_SSGIComponent != null)
		{
			ApplyState(m_SSGIComponent.enable, enabled);
			ApplyState(m_SSGIComponent.fullResolutionSS, fullscreen);
			ApplyState(m_SSGIComponent.m_MaxRaySteps, raySteps);
			ApplyState(m_SSGIComponent.m_DenoiserRadiusSS, denoiserRadius);
			ApplyState(m_SSGIComponent.depthBufferThickness, depthBufferThickness);
			ApplyState(m_SSGIComponent.m_HalfResolutionDenoiserSS, halfResolutionPass);
			ApplyState(m_SSGIComponent.m_SecondDenoiserPassSS, secondDenoiserPass);
		}
	}

	public override bool IsOptionsDisabled()
	{
		if (!base.disableSetting)
		{
			return !enabled;
		}
		return true;
	}
}
