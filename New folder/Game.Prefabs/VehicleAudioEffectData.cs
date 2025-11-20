using Unity.Entities;
using Unity.Mathematics;

namespace Game.Prefabs;

public struct VehicleAudioEffectData : IComponentData, IQueryTypeParameter
{
	public float2 m_SpeedLimits;

	public float2 m_SpeedPitches;

	public float2 m_SpeedVolumes;
}
