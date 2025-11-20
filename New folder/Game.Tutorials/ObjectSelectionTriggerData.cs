using Unity.Entities;

namespace Game.Tutorials;

public struct ObjectSelectionTriggerData : IBufferElementData
{
	public Entity m_Prefab;

	public Entity m_GoToPhase;
}
