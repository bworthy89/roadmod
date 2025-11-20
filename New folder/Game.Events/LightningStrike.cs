using Unity.Entities;
using Unity.Mathematics;

namespace Game.Events;

public struct LightningStrike
{
	public Entity m_HitEntity;

	public float3 m_Position;
}
