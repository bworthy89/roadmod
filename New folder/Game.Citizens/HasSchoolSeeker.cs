using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Citizens;

public struct HasSchoolSeeker : IComponentData, IQueryTypeParameter, ISerializable
{
	public Entity m_Seeker;

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		writer.Write(m_Seeker);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		if (reader.context.version >= Version.seekerReferences)
		{
			ref Entity seeker = ref m_Seeker;
			reader.Read(out seeker);
		}
	}
}
