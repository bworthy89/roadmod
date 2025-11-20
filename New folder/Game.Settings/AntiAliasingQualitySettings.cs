using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace Game.Settings;

[SettingsUIAdvanced]
[SettingsUISection("AntiAliasingQualitySettings")]
[SettingsUIDisableByCondition(typeof(AntiAliasingQualitySettings), "IsOptionsDisabled")]
public class AntiAliasingQualitySettings : QualitySetting<AntiAliasingQualitySettings>
{
	public enum AntialiasingMethod
	{
		None,
		FXAA,
		SMAA,
		TAA
	}

	private static HDAdditionalCameraData m_GameCamera;

	public AntialiasingMethod antiAliasingMethod { get; set; }

	public HDAdditionalCameraData.SMAAQualityLevel smaaQuality { get; set; }

	public MSAASamples outlinesMSAA { get; set; }

	private static AntiAliasingQualitySettings highQuality => new AntiAliasingQualitySettings
	{
		outlinesMSAA = MSAASamples.MSAA8x,
		antiAliasingMethod = AntialiasingMethod.SMAA,
		smaaQuality = HDAdditionalCameraData.SMAAQualityLevel.High
	};

	private static AntiAliasingQualitySettings mediumQuality => new AntiAliasingQualitySettings
	{
		outlinesMSAA = MSAASamples.MSAA4x,
		antiAliasingMethod = AntialiasingMethod.SMAA,
		smaaQuality = HDAdditionalCameraData.SMAAQualityLevel.Low
	};

	private static AntiAliasingQualitySettings lowQuality => new AntiAliasingQualitySettings
	{
		outlinesMSAA = MSAASamples.MSAA2x,
		antiAliasingMethod = AntialiasingMethod.FXAA
	};

	private static AntiAliasingQualitySettings disabled => new AntiAliasingQualitySettings
	{
		outlinesMSAA = MSAASamples.None,
		antiAliasingMethod = AntialiasingMethod.None
	};

	static AntiAliasingQualitySettings()
	{
		QualitySetting<AntiAliasingQualitySettings>.RegisterMockName(Level.Disabled, "None");
		QualitySetting<AntiAliasingQualitySettings>.RegisterMockName(Level.Low, "FXAA");
		QualitySetting<AntiAliasingQualitySettings>.RegisterMockName(Level.Medium, "LowSMAA");
		QualitySetting<AntiAliasingQualitySettings>.RegisterMockName(Level.High, "HighSMAA");
		QualitySetting<AntiAliasingQualitySettings>.RegisterSetting(Level.Disabled, disabled);
		QualitySetting<AntiAliasingQualitySettings>.RegisterSetting(Level.Low, lowQuality);
		QualitySetting<AntiAliasingQualitySettings>.RegisterSetting(Level.Medium, mediumQuality);
		QualitySetting<AntiAliasingQualitySettings>.RegisterSetting(Level.High, highQuality);
	}

	public AntiAliasingQualitySettings()
	{
	}

	public AntiAliasingQualitySettings(Level quality)
	{
		SetLevel(quality, apply: false);
	}

	private static HDAdditionalCameraData.AntialiasingMode ToAAMode(AntialiasingMethod method)
	{
		return method switch
		{
			AntialiasingMethod.None => HDAdditionalCameraData.AntialiasingMode.None, 
			AntialiasingMethod.FXAA => HDAdditionalCameraData.AntialiasingMode.FastApproximateAntialiasing, 
			AntialiasingMethod.SMAA => HDAdditionalCameraData.AntialiasingMode.SubpixelMorphologicalAntiAliasing, 
			AntialiasingMethod.TAA => HDAdditionalCameraData.AntialiasingMode.TemporalAntialiasing, 
			_ => HDAdditionalCameraData.AntialiasingMode.None, 
		};
	}

	public override void Apply()
	{
		base.Apply();
		if (TryGetGameplayCamera(ref m_GameCamera))
		{
			if (!SharedSettings.instance.graphics.isDlssActive && !SharedSettings.instance.graphics.isFsr2Active)
			{
				m_GameCamera.antialiasing = ToAAMode(antiAliasingMethod);
				m_GameCamera.SMAAQuality = smaaQuality;
			}
			else
			{
				m_GameCamera.antialiasing = HDAdditionalCameraData.AntialiasingMode.None;
			}
		}
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
