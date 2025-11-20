using System;
using Game.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace Game.Prefabs.Climate;

[ComponentMenu("Weather/", new Type[] { typeof(WeatherPrefab) })]
public class WhiteBalanceProperties : OverrideablePropertiesComponent
{
	public ClampedFloatParameter m_Temperature = new ClampedFloatParameter(0f, -100f, 100f);

	public ClampedFloatParameter m_Tint = new ClampedFloatParameter(0f, -100f, 100f);

	protected override void OnBindVolumeProperties(Volume volume)
	{
		WhiteBalance component = null;
		VolumeHelper.GetOrCreateVolumeComponent(volume, ref component);
		m_Temperature = component.temperature;
		m_Tint = component.tint;
	}
}
