using Colossal.Serialization.Entities;
using Game.Prefabs;
using Unity.Entities;

namespace Game.Simulation;

public struct PoliceEmergencyRequest : IComponentData, IQueryTypeParameter, ISerializable
{
	public Entity m_Site;

	public Entity m_Target;

	public float m_Priority;

	public PolicePurpose m_Purpose;

	public PoliceEmergencyRequest(Entity site, Entity target, float priority, PolicePurpose purpose)
	{
		m_Site = site;
		m_Target = target;
		m_Priority = priority;
		m_Purpose = purpose;
	}

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		Entity site = m_Site;
		writer.Write(site);
		Entity target = m_Target;
		writer.Write(target);
		float priority = m_Priority;
		writer.Write(priority);
		PolicePurpose purpose = m_Purpose;
		writer.Write((int)purpose);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		ref Entity site = ref m_Site;
		reader.Read(out site);
		ref Entity target = ref m_Target;
		reader.Read(out target);
		ref float priority = ref m_Priority;
		reader.Read(out priority);
		if (reader.context.version >= Version.policeImprovement3)
		{
			reader.Read(out int value);
			m_Purpose = (PolicePurpose)value;
		}
		else
		{
			m_Purpose = PolicePurpose.Patrol | PolicePurpose.Emergency;
		}
	}
}
