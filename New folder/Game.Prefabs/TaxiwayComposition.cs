using Unity.Entities;

namespace Game.Prefabs;

public struct TaxiwayComposition : IComponentData, IQueryTypeParameter
{
	public float m_SpeedLimit;

	public TaxiwayFlags m_Flags;
}
