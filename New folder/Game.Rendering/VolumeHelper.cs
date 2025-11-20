using System.Collections.Generic;
using Colossal;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace Game.Rendering;

public static class VolumeHelper
{
	public const int kQualityVolumePriority = 100;

	public const int kGameVolumePriority = 50;

	public const int kOverrideVolumePriority = 2000;

	public const int kWaterVolumePriority = 5000;

	private const string kSectionName = "======Volumes======";

	private static List<Volume> m_Volumes = new List<Volume>();

	public static void Dispose()
	{
		for (int num = m_Volumes.Count - 1; num >= 0; num--)
		{
			DestroyVolume(m_Volumes[num]);
		}
	}

	private static VolumeProfile CreateVolumeProfile(string overrideName)
	{
		VolumeProfile volumeProfile = ScriptableObject.CreateInstance<VolumeProfile>();
		volumeProfile.name = overrideName + "Profile";
		volumeProfile.hideFlags = HideFlags.DontSave;
		return volumeProfile;
	}

	public static Volume CreateVolume(string name, int priority)
	{
		GameObject gameObject = OrderedGameObjectSpawner.Get("======Volumes======").Create(name);
		gameObject.hideFlags = HideFlags.DontSave;
		Volume component = gameObject.GetComponent<Volume>();
		component.priority = priority;
		component.sharedProfile = CreateVolumeProfile(name);
		m_Volumes.Add(component);
		return component;
	}

	public static void DestroyVolume(Volume volume)
	{
		m_Volumes.Remove(volume);
		if (volume.sharedProfile != null)
		{
			CoreUtils.Destroy(volume.sharedProfile);
		}
		if (volume != null)
		{
			CoreUtils.Destroy(volume.gameObject);
		}
	}

	public static void GetOrCreateVolumeComponent<PT>(Volume volume, ref PT component) where PT : VolumeComponent
	{
		GetOrCreateVolumeComponent(volume.profileRef, ref component);
	}

	public static void GetOrCreateVolumeComponent<PT>(VolumeProfile profile, ref PT component) where PT : VolumeComponent
	{
		if (component == null && !profile.TryGet<PT>(out component))
		{
			component = profile.Add<PT>();
			if (component is VolumeComponentWithQuality volumeComponentWithQuality)
			{
				volumeComponentWithQuality.quality.Override(3);
			}
		}
	}
}
