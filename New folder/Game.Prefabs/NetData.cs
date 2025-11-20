using Colossal.Serialization.Entities;
using Game.Net;
using Unity.Entities;

namespace Game.Prefabs;

public struct NetData : IComponentData, IQueryTypeParameter, ISerializable
{
	public EntityArchetype m_NodeArchetype;

	public EntityArchetype m_EdgeArchetype;

	public Layer m_RequiredLayers;

	public Layer m_ConnectLayers;

	public Layer m_LocalConnectLayers;

	public CompositionFlags.General m_GeneralFlagMask;

	public CompositionFlags.Side m_SideFlagMask;

	public float m_NodePriority;

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		Layer requiredLayers = m_RequiredLayers;
		writer.Write((uint)requiredLayers);
		Layer connectLayers = m_ConnectLayers;
		writer.Write((uint)connectLayers);
		Layer localConnectLayers = m_LocalConnectLayers;
		writer.Write((uint)localConnectLayers);
		float nodePriority = m_NodePriority;
		writer.Write(nodePriority);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		reader.Read(out uint value);
		reader.Read(out uint value2);
		reader.Read(out uint value3);
		ref float nodePriority = ref m_NodePriority;
		reader.Read(out nodePriority);
		m_RequiredLayers = (Layer)value;
		m_ConnectLayers = (Layer)value2;
		m_LocalConnectLayers = (Layer)value3;
	}
}
