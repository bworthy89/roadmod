using Colossal.Mathematics;
using Colossal.Serialization.Entities;
using Game.Buildings;
using Unity.Entities;

namespace Game.Prefabs;

[InternalBufferCapacity(0)]
public struct LocalModifierData : IBufferElementData, ISerializable
{
	public LocalModifierType m_Type;

	public ModifierValueMode m_Mode;

	public ModifierRadiusCombineMode m_RadiusCombineMode;

	public Bounds1 m_Delta;

	public Bounds1 m_Radius;

	public LocalModifierData(LocalModifierType type, ModifierValueMode mode, ModifierRadiusCombineMode radiusMode, Bounds1 delta, Bounds1 radius)
	{
		m_Type = type;
		m_Mode = mode;
		m_RadiusCombineMode = radiusMode;
		m_Delta = delta;
		m_Radius = radius;
	}

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		float min = m_Delta.min;
		writer.Write(min);
		float max = m_Delta.max;
		writer.Write(max);
		float min2 = m_Radius.min;
		writer.Write(min2);
		float max2 = m_Radius.max;
		writer.Write(max2);
		byte value = (byte)m_Type;
		writer.Write(value);
		byte value2 = (byte)m_Mode;
		writer.Write(value2);
		byte value3 = (byte)m_RadiusCombineMode;
		writer.Write(value3);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		ref float min = ref m_Delta.min;
		reader.Read(out min);
		ref float max = ref m_Delta.max;
		reader.Read(out max);
		ref float min2 = ref m_Radius.min;
		reader.Read(out min2);
		ref float max2 = ref m_Radius.max;
		reader.Read(out max2);
		reader.Read(out byte value);
		reader.Read(out byte value2);
		reader.Read(out byte value3);
		m_Type = (LocalModifierType)value;
		m_Mode = (ModifierValueMode)value2;
		m_RadiusCombineMode = (ModifierRadiusCombineMode)value3;
	}
}
