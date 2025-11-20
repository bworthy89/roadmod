using Unity.Entities;

namespace Game.Prefabs;

public struct WaterwayComposition : IComponentData, IQueryTypeParameter
{
	public float m_SpeedLimit;
}
