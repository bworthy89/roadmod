using System;
using Game.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace Game.Prefabs.Climate;

[ComponentMenu("Weather/", new Type[] { typeof(WeatherPrefab) })]
public class FogProperties : OverrideablePropertiesComponent
{
	protected override void OnBindVolumeProperties(Volume volume)
	{
		Fog component = null;
		VolumeHelper.GetOrCreateVolumeComponent(volume, ref component);
	}
}
