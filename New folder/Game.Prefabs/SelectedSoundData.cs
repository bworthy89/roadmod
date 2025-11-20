using Unity.Entities;

namespace Game.Prefabs;

public struct SelectedSoundData : IComponentData, IQueryTypeParameter
{
	public Entity m_selectedSound;
}
