using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Objects;

public struct Color : IComponentData, IQueryTypeParameter, IEmptySerializable
{
	public byte m_Index;

	public byte m_Value;

	public bool m_SubColor;

	public Color(byte index, byte value, bool subColor = false)
	{
		m_Index = index;
		m_Value = value;
		m_SubColor = subColor;
	}
}
