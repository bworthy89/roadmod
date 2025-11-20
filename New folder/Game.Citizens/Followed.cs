using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Citizens;

public struct Followed : IComponentData, IQueryTypeParameter, ISerializable
{
	public uint m_Priority;

	public bool m_StartedFollowingAsChild;

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		uint priority = m_Priority;
		writer.Write(priority);
		bool startedFollowingAsChild = m_StartedFollowingAsChild;
		writer.Write(startedFollowingAsChild);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		if (reader.context.version >= Version.localizationIndex)
		{
			ref uint priority = ref m_Priority;
			reader.Read(out priority);
		}
		if (reader.context.version >= Version.stalkerAchievement)
		{
			ref bool startedFollowingAsChild = ref m_StartedFollowingAsChild;
			reader.Read(out startedFollowingAsChild);
		}
	}
}
