using Colossal.Serialization.Entities;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Objects;

public struct Relative : IComponentData, IQueryTypeParameter, ISerializable
{
	public float3 m_Position;

	public quaternion m_Rotation;

	public int3 m_BoneIndex;

	public Relative(Transform localTransform, int3 boneIndex)
	{
		m_Position = localTransform.m_Position;
		m_Rotation = localTransform.m_Rotation;
		m_BoneIndex = boneIndex;
	}

	public Transform ToTransform()
	{
		return new Transform(m_Position, m_Rotation);
	}

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		float3 position = m_Position;
		writer.Write(position);
		quaternion rotation = m_Rotation;
		writer.Write(rotation);
		int x = m_BoneIndex.x;
		writer.Write(x);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		ref float3 position = ref m_Position;
		reader.Read(out position);
		ref quaternion rotation = ref m_Rotation;
		reader.Read(out rotation);
		if (reader.context.version >= Version.boneRelativeObjects)
		{
			ref int x = ref m_BoneIndex.x;
			reader.Read(out x);
		}
		m_BoneIndex.yz = -1;
	}
}
