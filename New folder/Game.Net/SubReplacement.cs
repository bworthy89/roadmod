using System;
using Colossal.Serialization.Entities;
using Game.Tools;
using Unity.Entities;

namespace Game.Net;

[InternalBufferCapacity(2)]
public struct SubReplacement : IBufferElementData, IEquatable<SubReplacement>, ISerializable
{
	public Entity m_Prefab;

	public SubReplacementType m_Type;

	public SubReplacementSide m_Side;

	public AgeMask m_AgeMask;

	public bool Equals(SubReplacement other)
	{
		if (m_Prefab == other.m_Prefab && m_Type == other.m_Type && m_Side == other.m_Side)
		{
			return m_AgeMask == other.m_AgeMask;
		}
		return false;
	}

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		Entity prefab = m_Prefab;
		writer.Write(prefab);
		SubReplacementType type = m_Type;
		writer.Write((byte)type);
		SubReplacementSide side = m_Side;
		writer.Write((sbyte)side);
		AgeMask ageMask = m_AgeMask;
		writer.Write((byte)ageMask);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		ref Entity prefab = ref m_Prefab;
		reader.Read(out prefab);
		reader.Read(out byte value);
		reader.Read(out sbyte value2);
		reader.Read(out byte value3);
		m_Type = (SubReplacementType)value;
		m_Side = (SubReplacementSide)value2;
		m_AgeMask = (AgeMask)value3;
	}
}
