using System;
using Game.Rendering;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace Game.Prefabs.Climate;

[ComponentMenu("Weather/", new Type[] { typeof(WeatherPrefab) })]
public class ColorAdjustmentsProperties : OverrideablePropertiesComponent
{
	public FloatParameter m_PostExposure = new FloatParameter(0f);

	public ClampedFloatParameter m_Contrast = new ClampedFloatParameter(0f, -100f, 100f);

	public ColorParameter m_ColorFilter = new ColorParameter(Color.white, hdr: true, showAlpha: false, showEyeDropper: true);

	public ClampedFloatParameter m_HueShift = new ClampedFloatParameter(0f, -180f, 180f);

	public ClampedFloatParameter m_Saturation = new ClampedFloatParameter(0f, -100f, 100f);

	protected override void OnBindVolumeProperties(Volume volume)
	{
		ColorAdjustments component = null;
		VolumeHelper.GetOrCreateVolumeComponent(volume, ref component);
		m_PostExposure = component.postExposure;
		m_Contrast = component.contrast;
		m_ColorFilter = component.colorFilter;
		m_HueShift = component.hueShift;
		m_Saturation = component.saturation;
	}
}
