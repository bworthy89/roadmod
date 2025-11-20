using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Simulation;

public struct CollectedCityServiceFeeData : IBufferElementData, ISerializable
{
	public int m_PlayerResource;

	public float m_Export;

	public float m_Import;

	public float m_Internal;

	public float m_ExportCount;

	public float m_ImportCount;

	public float m_InternalCount;

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		ref int playerResource = ref m_PlayerResource;
		reader.Read(out playerResource);
		ref float export = ref m_Export;
		reader.Read(out export);
		ref float import = ref m_Import;
		reader.Read(out import);
		ref float value = ref m_Internal;
		reader.Read(out value);
		if (reader.context.version < Version.serviceFeeFix)
		{
			reader.Read(out int value2);
			reader.Read(out int value3);
			reader.Read(out int value4);
			m_ExportCount = value2;
			m_ImportCount = value3;
			m_InternalCount = value4;
		}
		else
		{
			ref float exportCount = ref m_ExportCount;
			reader.Read(out exportCount);
			ref float importCount = ref m_ImportCount;
			reader.Read(out importCount);
			ref float internalCount = ref m_InternalCount;
			reader.Read(out internalCount);
		}
	}

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		int playerResource = m_PlayerResource;
		writer.Write(playerResource);
		float export = m_Export;
		writer.Write(export);
		float import = m_Import;
		writer.Write(import);
		float value = m_Internal;
		writer.Write(value);
		float exportCount = m_ExportCount;
		writer.Write(exportCount);
		float importCount = m_ImportCount;
		writer.Write(importCount);
		float internalCount = m_InternalCount;
		writer.Write(internalCount);
	}
}
