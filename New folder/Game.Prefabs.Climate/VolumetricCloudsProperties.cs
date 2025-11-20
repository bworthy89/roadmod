using System;
using Game.Rendering;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace Game.Prefabs.Climate;

[ComponentMenu("Weather/", new Type[] { typeof(WeatherPrefab) })]
public class VolumetricCloudsProperties : OverrideablePropertiesComponent
{
	public MinFloatParameter m_BottomAltitude = new MinFloatParameter(1200f, 0.01f);

	public MinFloatParameter m_AltitudeRange = new MinFloatParameter(2000f, 100f);

	public ClampedFloatParameter m_DensityMultiplier = new ClampedFloatParameter(0.4f, 0f, 1f);

	public AnimationCurveParameter m_DensityCurve = new AnimationCurveParameter(new AnimationCurve(new Keyframe(0f, 0f), new Keyframe(0.15f, 1f), new Keyframe(1f, 0.1f)));

	public ClampedFloatParameter m_ShapeFactor = new ClampedFloatParameter(0.9f, 0f, 1f);

	public Vector3Parameter m_ShapeOffset = new Vector3Parameter(Vector3.zero);

	public ClampedFloatParameter m_ErosionFactor = new ClampedFloatParameter(0.8f, 0f, 1f);

	public ClampedFloatParameter m_ErosionOcclusion = new ClampedFloatParameter(0.1f, 0f, 1f);

	public AnimationCurveParameter m_ErosionCurve = new AnimationCurveParameter(new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(0.1f, 0.9f), new Keyframe(1f, 1f)));

	public AnimationCurveParameter m_AmbientOcclusionCurve = new AnimationCurveParameter(new AnimationCurve(new Keyframe(0f, 0f), new Keyframe(0.25f, 0.4f), new Keyframe(1f, 0f)));

	public ClampedFloatParameter m_MultiScattering = new ClampedFloatParameter(0.5f, 0f, 1f);

	protected override void OnBindVolumeProperties(Volume volume)
	{
		VolumetricClouds component = null;
		VolumeHelper.GetOrCreateVolumeComponent(volume, ref component);
		m_BottomAltitude = component.bottomAltitude;
		m_AltitudeRange = component.altitudeRange;
		m_DensityMultiplier = component.densityMultiplier;
		m_DensityCurve = component.densityCurve;
		m_ShapeFactor = component.shapeFactor;
		m_ShapeOffset = component.shapeOffset;
		m_ErosionFactor = component.erosionFactor;
		m_ErosionOcclusion = component.erosionOcclusion;
		m_ErosionCurve = component.erosionCurve;
		m_AmbientOcclusionCurve = component.ambientOcclusionCurve;
		m_MultiScattering = component.multiScattering;
		component.m_CloudPreset.Override(VolumetricClouds.CloudPresets.Custom);
	}
}
