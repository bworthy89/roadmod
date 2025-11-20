using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Triggers;

public struct Chirp : IComponentData, IQueryTypeParameter, ISerializable
{
	public Entity m_Sender;

	public uint m_CreationFrame;

	public uint m_Likes;

	public uint m_TargetLikes;

	public uint m_InactiveFrame;

	public int m_ViralFactor;

	public float m_ContinuousFactor;

	public ChirpFlags m_Flags;

	public Chirp(Entity sender, uint creationFrame)
	{
		m_Sender = sender;
		m_CreationFrame = creationFrame;
		m_Likes = 0u;
		m_Flags = (ChirpFlags)0;
		m_TargetLikes = 0u;
		m_InactiveFrame = 0u;
		m_ViralFactor = 1;
		m_ContinuousFactor = 0.2f;
	}

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		Entity sender = m_Sender;
		writer.Write(sender);
		uint creationFrame = m_CreationFrame;
		writer.Write(creationFrame);
		uint likes = m_Likes;
		writer.Write(likes);
		ChirpFlags flags = m_Flags;
		writer.Write((byte)flags);
		uint targetLikes = m_TargetLikes;
		writer.Write(targetLikes);
		uint inactiveFrame = m_InactiveFrame;
		writer.Write(inactiveFrame);
		int viralFactor = m_ViralFactor;
		writer.Write(viralFactor);
		float continuousFactor = m_ContinuousFactor;
		writer.Write(continuousFactor);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		ref Entity sender = ref m_Sender;
		reader.Read(out sender);
		ref uint creationFrame = ref m_CreationFrame;
		reader.Read(out creationFrame);
		if (reader.context.version >= Version.chirpLikes)
		{
			ref uint likes = ref m_Likes;
			reader.Read(out likes);
			reader.Read(out byte value);
			m_Flags = (ChirpFlags)value;
		}
		if (reader.context.version >= Version.randomChirpLikes)
		{
			ref uint targetLikes = ref m_TargetLikes;
			reader.Read(out targetLikes);
			ref uint inactiveFrame = ref m_InactiveFrame;
			reader.Read(out inactiveFrame);
			ref int viralFactor = ref m_ViralFactor;
			reader.Read(out viralFactor);
		}
		if (reader.context.version >= Version.continuousChirpLikes)
		{
			ref float continuousFactor = ref m_ContinuousFactor;
			reader.Read(out continuousFactor);
		}
	}
}
