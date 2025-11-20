using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Objects;

[FormerlySerializedAs("Game.Buildings.SetLevel, Game")]
public struct UnderConstruction : IComponentData, IQueryTypeParameter, ISerializable
{
	public Entity m_NewPrefab;

	public byte m_Progress;

	public byte m_Speed;

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		Entity newPrefab = m_NewPrefab;
		writer.Write(newPrefab);
		byte progress = m_Progress;
		writer.Write(progress);
		byte speed = m_Speed;
		writer.Write(speed);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		ref Entity newPrefab = ref m_NewPrefab;
		reader.Read(out newPrefab);
		if (reader.context.version >= Version.constructionProgress)
		{
			ref byte progress = ref m_Progress;
			reader.Read(out progress);
		}
		else
		{
			m_Progress = byte.MaxValue;
		}
		if (reader.context.version >= Version.constructionSpeed)
		{
			ref byte speed = ref m_Speed;
			reader.Read(out speed);
		}
		else
		{
			m_Speed = 50;
		}
	}
}
