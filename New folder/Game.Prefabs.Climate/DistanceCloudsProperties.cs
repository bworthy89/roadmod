using System;
using Game.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace Game.Prefabs.Climate;

[ComponentMenu("Weather/", new Type[] { typeof(WeatherPrefab) })]
public class DistanceCloudsProperties : OverrideablePropertiesComponent
{
	public ClampedFloatParameter m_Opacity = new ClampedFloatParameter(1f, 0f, 1f);

	public ClampedFloatParameter m_CumulusStrength = new ClampedFloatParameter(1f, 0f, 1f);

	public ClampedFloatParameter m_StratusStrength = new ClampedFloatParameter(1f, 0f, 1f);

	public ClampedFloatParameter m_CirrusStrength = new ClampedFloatParameter(1f, 0f, 1f);

	public ClampedFloatParameter m_WispyStrength = new ClampedFloatParameter(1f, 0f, 1f);

	public MinFloatParameter m_Altitude = new MinFloatParameter(2000f, 0f);

	protected override void OnBindVolumeProperties(Volume volume)
	{
		CloudLayer component = null;
		VolumeHelper.GetOrCreateVolumeComponent(volume, ref component);
		m_Opacity = component.opacity;
		m_CumulusStrength = component.layerA.opacityR;
		m_StratusStrength = component.layerA.opacityG;
		m_CirrusStrength = component.layerA.opacityB;
		m_WispyStrength = component.layerA.opacityA;
		m_Altitude = component.layerA.altitude;
	}
}
