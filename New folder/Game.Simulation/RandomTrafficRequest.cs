using Colossal.Serialization.Entities;
using Game.Net;
using Game.Vehicles;
using Unity.Entities;

namespace Game.Simulation;

public struct RandomTrafficRequest : IComponentData, IQueryTypeParameter, ISerializable
{
	public Entity m_Target;

	public RoadTypes m_RoadType;

	public TrackTypes m_TrackType;

	public EnergyTypes m_EnergyTypes;

	public SizeClass m_SizeClass;

	public RandomTrafficRequestFlags m_Flags;

	public RandomTrafficRequest(Entity target, RoadTypes roadType, TrackTypes trackType, EnergyTypes energyTypes, SizeClass sizeClass, RandomTrafficRequestFlags flags)
	{
		m_Target = target;
		m_RoadType = roadType;
		m_TrackType = trackType;
		m_EnergyTypes = energyTypes;
		m_SizeClass = sizeClass;
		m_Flags = flags;
	}

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		Entity target = m_Target;
		writer.Write(target);
		RoadTypes roadType = m_RoadType;
		writer.Write((byte)roadType);
		TrackTypes trackType = m_TrackType;
		writer.Write((byte)trackType);
		EnergyTypes energyTypes = m_EnergyTypes;
		writer.Write((byte)energyTypes);
		SizeClass sizeClass = m_SizeClass;
		writer.Write((byte)sizeClass);
		RandomTrafficRequestFlags flags = m_Flags;
		writer.Write((byte)flags);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		ref Entity target = ref m_Target;
		reader.Read(out target);
		if (reader.context.version >= Version.randomTrafficTypes)
		{
			reader.Read(out byte value);
			reader.Read(out byte value2);
			reader.Read(out byte value3);
			reader.Read(out byte value4);
			m_RoadType = (RoadTypes)value;
			m_TrackType = (TrackTypes)value2;
			m_EnergyTypes = (EnergyTypes)value3;
			m_SizeClass = (SizeClass)value4;
		}
		if (reader.context.version >= Version.randomTrafficFlags)
		{
			reader.Read(out byte value5);
			m_Flags = (RandomTrafficRequestFlags)value5;
		}
	}
}
