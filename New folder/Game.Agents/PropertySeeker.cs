using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Agents;

public struct PropertySeeker : IComponentData, IQueryTypeParameter, ISerializable, IEnableableComponent
{
	public Entity m_TargetProperty;

	public Entity m_BestProperty;

	public float m_BestPropertyScore;

	public uint m_LastPropertySeekFrame;

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		Entity targetProperty = m_TargetProperty;
		writer.Write(targetProperty);
		Entity bestProperty = m_BestProperty;
		writer.Write(bestProperty);
		float bestPropertyScore = m_BestPropertyScore;
		writer.Write(bestPropertyScore);
		uint lastPropertySeekFrame = m_LastPropertySeekFrame;
		writer.Write(lastPropertySeekFrame);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		ref Entity targetProperty = ref m_TargetProperty;
		reader.Read(out targetProperty);
		ref Entity bestProperty = ref m_BestProperty;
		reader.Read(out bestProperty);
		ref float bestPropertyScore = ref m_BestPropertyScore;
		reader.Read(out bestPropertyScore);
		if (reader.context.format.Has(FormatTags.HomelessAndWorkerFix))
		{
			ref uint lastPropertySeekFrame = ref m_LastPropertySeekFrame;
			reader.Read(out lastPropertySeekFrame);
		}
		else
		{
			reader.Read(out byte _);
		}
	}
}
