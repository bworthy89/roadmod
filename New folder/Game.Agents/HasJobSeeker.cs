using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Agents;

public struct HasJobSeeker : IComponentData, IQueryTypeParameter, ISerializable, IEnableableComponent
{
	public Entity m_Seeker;

	public uint m_LastJobSeekFrameIndex;

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		Entity seeker = m_Seeker;
		writer.Write(seeker);
		uint lastJobSeekFrameIndex = m_LastJobSeekFrameIndex;
		writer.Write(lastJobSeekFrameIndex);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		if (reader.context.version >= Version.seekerReferences)
		{
			ref Entity seeker = ref m_Seeker;
			reader.Read(out seeker);
		}
		if (reader.context.version >= Version.findJobOptimize)
		{
			ref uint lastJobSeekFrameIndex = ref m_LastJobSeekFrameIndex;
			reader.Read(out lastJobSeekFrameIndex);
		}
	}
}
