using Unity.Entities;

namespace Game.Prefabs;

public struct FireAudioEffectData : IComponentData, IQueryTypeParameter
{
	public Entity m_FireAudioEffectEntity;
}
