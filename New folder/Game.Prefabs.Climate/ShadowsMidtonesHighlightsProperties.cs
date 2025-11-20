using System;
using Game.Rendering;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace Game.Prefabs.Climate;

[ComponentMenu("Weather/", new Type[] { typeof(WeatherPrefab) })]
public class ShadowsMidtonesHighlightsProperties : OverrideablePropertiesComponent
{
	public Vector4Parameter m_Shadows = new Vector4Parameter(new Vector4(1f, 1f, 1f, 0f));

	public Vector4Parameter m_Midtones = new Vector4Parameter(new Vector4(1f, 1f, 1f, 0f));

	public Vector4Parameter m_Highlights = new Vector4Parameter(new Vector4(1f, 1f, 1f, 0f));

	[Header("Shadow Limits")]
	public MinFloatParameter m_ShadowsStart = new MinFloatParameter(0f, 0f);

	public MinFloatParameter m_ShadowsEnd = new MinFloatParameter(0.3f, 0f);

	[Header("Highlight Limits")]
	public MinFloatParameter m_HighlightsStart = new MinFloatParameter(0.55f, 0f);

	public MinFloatParameter m_HighlightsEnd = new MinFloatParameter(1f, 0f);

	protected override void OnBindVolumeProperties(Volume volume)
	{
		ShadowsMidtonesHighlights component = null;
		VolumeHelper.GetOrCreateVolumeComponent(volume, ref component);
		m_Shadows = component.shadows;
		m_Midtones = component.midtones;
		m_Highlights = component.highlights;
		m_ShadowsStart = component.shadowsStart;
		m_ShadowsEnd = component.shadowsEnd;
		m_HighlightsStart = component.highlightsStart;
		m_HighlightsEnd = component.highlightsEnd;
	}
}
