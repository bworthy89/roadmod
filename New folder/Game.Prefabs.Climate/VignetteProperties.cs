using System;
using Game.Rendering;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace Game.Prefabs.Climate;

[ComponentMenu("Weather/", new Type[] { typeof(WeatherPrefab) })]
public class VignetteProperties : OverrideablePropertiesComponent
{
	public ColorParameter m_Color = new ColorParameter(Color.black, hdr: false, showAlpha: false, showEyeDropper: true);

	public Vector2Parameter m_Center = new Vector2Parameter(new Vector2(0.5f, 0.5f));

	public ClampedFloatParameter m_Intensity = new ClampedFloatParameter(0f, 0f, 1f);

	public ClampedFloatParameter m_Smoothness = new ClampedFloatParameter(0.2f, 0.01f, 1f);

	public ClampedFloatParameter m_Roundness = new ClampedFloatParameter(1f, 0f, 1f);

	public BoolParameter m_Rounded = new BoolParameter(value: false);

	protected override void OnBindVolumeProperties(Volume volume)
	{
		Vignette component = null;
		VolumeHelper.GetOrCreateVolumeComponent(volume, ref component);
		m_Color = component.color;
		m_Center = component.center;
		m_Intensity = component.intensity;
		m_Smoothness = component.smoothness;
		m_Roundness = component.roundness;
		m_Rounded = component.rounded;
		component.mode.Override(VignetteMode.Procedural);
	}
}
