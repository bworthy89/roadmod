using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace Game.Settings;

[SettingsUIAdvanced]
[SettingsUISection("CloudsQualitySettings")]
[SettingsUIDisableByCondition(typeof(CloudsQualitySettings), "IsOptionsDisabled")]
public class CloudsQualitySettings : QualitySetting<CloudsQualitySettings>
{
	private static VolumetricClouds m_VolumetricClouds;

	private static VisualEnvironment m_VisualEnvironment;

	private static CloudLayer m_CloudLayer;

	public bool volumetricCloudsEnabled { get; set; }

	public bool distanceCloudsEnabled { get; set; }

	public bool volumetricCloudsShadows { get; set; }

	public bool distanceCloudsShadows { get; set; }

	private static CloudsQualitySettings highQuality => new CloudsQualitySettings
	{
		volumetricCloudsEnabled = true,
		distanceCloudsEnabled = true,
		volumetricCloudsShadows = true,
		distanceCloudsShadows = true
	};

	private static CloudsQualitySettings mediumQuality => new CloudsQualitySettings
	{
		volumetricCloudsEnabled = true,
		distanceCloudsEnabled = true,
		volumetricCloudsShadows = false,
		distanceCloudsShadows = true
	};

	private static CloudsQualitySettings lowQuality => new CloudsQualitySettings
	{
		volumetricCloudsEnabled = false,
		distanceCloudsEnabled = true,
		volumetricCloudsShadows = false,
		distanceCloudsShadows = false
	};

	private static CloudsQualitySettings disabled => new CloudsQualitySettings
	{
		volumetricCloudsEnabled = false,
		distanceCloudsEnabled = false,
		volumetricCloudsShadows = false,
		distanceCloudsShadows = false
	};

	static CloudsQualitySettings()
	{
		QualitySetting<CloudsQualitySettings>.RegisterSetting(Level.Disabled, disabled);
		QualitySetting<CloudsQualitySettings>.RegisterSetting(Level.Low, lowQuality);
		QualitySetting<CloudsQualitySettings>.RegisterSetting(Level.Medium, mediumQuality);
		QualitySetting<CloudsQualitySettings>.RegisterSetting(Level.High, highQuality);
	}

	public CloudsQualitySettings()
	{
	}

	public CloudsQualitySettings(Level quality, VolumeProfile profile)
	{
		CreateVolumeComponent(profile, ref m_VolumetricClouds);
		CreateVolumeComponent(profile, ref m_VisualEnvironment);
		CreateVolumeComponent(profile, ref m_CloudLayer);
		SetLevel(quality, apply: false);
	}

	public override void Apply()
	{
		base.Apply();
		if (m_VolumetricClouds != null)
		{
			ApplyState(m_VolumetricClouds.enable, volumetricCloudsEnabled);
			ApplyState(m_VolumetricClouds.shadows, volumetricCloudsShadows);
			ApplyState(m_VisualEnvironment.cloudType, distanceCloudsEnabled ? 1 : 0);
			ApplyState(m_CloudLayer.layerA.castShadows, distanceCloudsShadows);
		}
	}
}
