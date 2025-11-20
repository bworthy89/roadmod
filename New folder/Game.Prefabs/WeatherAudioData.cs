using System;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Prefabs;

[Serializable]
public struct WeatherAudioData : IComponentData, IQueryTypeParameter
{
	public float m_StartRaininess;

	public float m_RainAudioIntensity;

	public float m_RainFadeInSpeed;

	public float m_RainFadeOutSpeed;

	public float m_RainVolumeDeclineExponent;

	public float m_RainVolumeMaxZoomPercentage;

	public float m_LightningSoundSpeed;

	public float2 m_MinMaxRaininessPitch;

	public Entity m_RainAudio;

	public float m_WaterAudioIntensity;

	public float m_WaterFadeSpeed;

	public int m_WaterAudioEnabledZoom;

	public int m_WaterAudioNearDistance;

	public Entity m_WaterAmbientAudio;

	public Entity m_LightningAudio;
}
