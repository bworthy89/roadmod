using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Net;

public struct EdgeColor : IComponentData, IQueryTypeParameter, IEmptySerializable
{
	public byte m_Index;

	public byte m_Value0;

	public byte m_Value1;

	public EdgeColor(byte index, byte value0, byte value1)
	{
		m_Index = index;
		m_Value0 = value0;
		m_Value1 = value1;
	}
}
