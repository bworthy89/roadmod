using Colossal.Serialization.Entities;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Tools;

public struct LocalTransformCache : IComponentData, IQueryTypeParameter, ISerializable
{
	public float3 m_Position;

	public quaternion m_Rotation;

	public int m_ParentMesh;

	public int m_GroupIndex;

	public int m_Probability;

	public int m_PrefabSubIndex;

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		float3 position = m_Position;
		writer.Write(position);
		quaternion rotation = m_Rotation;
		writer.Write(rotation);
		int parentMesh = m_ParentMesh;
		writer.Write(parentMesh);
		int groupIndex = m_GroupIndex;
		writer.Write(groupIndex);
		int probability = m_Probability;
		writer.Write(probability);
		int prefabSubIndex = m_PrefabSubIndex;
		writer.Write(prefabSubIndex);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		ref float3 position = ref m_Position;
		reader.Read(out position);
		ref quaternion rotation = ref m_Rotation;
		reader.Read(out rotation);
		ref int parentMesh = ref m_ParentMesh;
		reader.Read(out parentMesh);
		ref int groupIndex = ref m_GroupIndex;
		reader.Read(out groupIndex);
		ref int probability = ref m_Probability;
		reader.Read(out probability);
		ref int prefabSubIndex = ref m_PrefabSubIndex;
		reader.Read(out prefabSubIndex);
	}
}
