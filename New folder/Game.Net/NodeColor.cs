using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Net;

public struct NodeColor : IComponentData, IQueryTypeParameter, IEmptySerializable
{
	public byte m_Index;

	public byte m_Value;

	public NodeColor(byte index, byte value)
	{
		m_Index = index;
		m_Value = value;
	}
}
