using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace Game.Settings;

[SettingsUIAdvanced]
[SettingsUISection("TerrainQualitySettings")]
[SettingsUIDisableByCondition(typeof(TerrainQualitySettings), "IsOptionsDisabled")]
public class TerrainQualitySettings : QualitySetting<TerrainQualitySettings>
{
	private static TerrainRendering m_TerrainRenderingComponent;

	[SettingsUISlider(min = 2f, max = 5f, step = 1f, unit = "integer")]
	public int finalTessellation { get; set; }

	[SettingsUISlider(min = 4f, max = 64f, step = 1f, unit = "integer")]
	public int targetPatchSize { get; set; }

	private static TerrainQualitySettings highQuality => new TerrainQualitySettings
	{
		finalTessellation = 4,
		targetPatchSize = 12
	};

	private static TerrainQualitySettings mediumQuality => new TerrainQualitySettings
	{
		finalTessellation = 3,
		targetPatchSize = 16
	};

	private static TerrainQualitySettings lowQuality => new TerrainQualitySettings
	{
		finalTessellation = 3,
		targetPatchSize = 24
	};

	public TerrainQualitySettings()
	{
	}

	static TerrainQualitySettings()
	{
		QualitySetting<TerrainQualitySettings>.RegisterSetting(Level.Low, lowQuality);
		QualitySetting<TerrainQualitySettings>.RegisterSetting(Level.Medium, mediumQuality);
		QualitySetting<TerrainQualitySettings>.RegisterSetting(Level.High, highQuality);
	}

	public TerrainQualitySettings(Level quality, VolumeProfile profile)
	{
		CreateVolumeComponent(profile, ref m_TerrainRenderingComponent);
		SetLevel(quality, apply: false);
	}

	public override void Apply()
	{
		base.Apply();
		if (m_TerrainRenderingComponent != null)
		{
			ApplyState(m_TerrainRenderingComponent.finalTessellation, finalTessellation);
			ApplyState(m_TerrainRenderingComponent.targetPatchSize, targetPatchSize);
		}
	}
}
