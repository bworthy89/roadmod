using Colossal.Serialization.Entities;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Tools;

public struct EditorContainer : IComponentData, IQueryTypeParameter, ISerializable
{
	public Entity m_Prefab;

	public float3 m_Scale;

	public float m_Intensity;

	public int m_GroupIndex;

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		Entity prefab = m_Prefab;
		writer.Write(prefab);
		float3 scale = m_Scale;
		writer.Write(scale);
		float intensity = m_Intensity;
		writer.Write(intensity);
		int groupIndex = m_GroupIndex;
		writer.Write(groupIndex);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		ref Entity prefab = ref m_Prefab;
		reader.Read(out prefab);
		ref float3 scale = ref m_Scale;
		reader.Read(out scale);
		ref float intensity = ref m_Intensity;
		reader.Read(out intensity);
		if (reader.context.version >= Version.editorContainerGroupIndex)
		{
			ref int groupIndex = ref m_GroupIndex;
			reader.Read(out groupIndex);
		}
		else
		{
			m_GroupIndex = -1;
		}
	}
}
