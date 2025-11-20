using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Net;

public struct CoverageServiceType : ISharedComponentData, IQueryTypeParameter, ISerializable
{
	public CoverageService m_Service;

	public CoverageServiceType(CoverageService service)
	{
		m_Service = service;
	}

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		writer.Write((byte)m_Service);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		reader.Read(out byte value);
		m_Service = (CoverageService)value;
	}
}
