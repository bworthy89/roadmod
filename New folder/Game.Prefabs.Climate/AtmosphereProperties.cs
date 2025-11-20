using System;
using Game.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace Game.Prefabs.Climate;

[ComponentMenu("Weather/", new Type[] { typeof(WeatherPrefab) })]
public class AtmosphereProperties : OverrideablePropertiesComponent
{
	public MinFloatParameter m_AuroraBorealisEmissionMultiplier = new MinFloatParameter(0f, 0f);

	public MinFloatParameter m_AuroraBorealisSpeedMultiplier = new MinFloatParameter(1f, 0f);

	protected override void OnBindVolumeProperties(Volume volume)
	{
		PhysicallyBasedSky component = null;
		VolumeHelper.GetOrCreateVolumeComponent(volume, ref component);
		m_AuroraBorealisEmissionMultiplier = component.auroraBorealisEmissionMultiplier;
		m_AuroraBorealisSpeedMultiplier = component.auroraBorealisSpeedMultiplier;
	}
}
