using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Simulation;

public struct ElectricityFlowNode : IComponentData, IQueryTypeParameter, IEmptySerializable
{
	public int m_Index;
}
