using Colossal.Serialization.Entities;
using Game.Net;
using Game.Vehicles;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Prefabs;

public struct TrainData : IComponentData, IQueryTypeParameter, ISerializable
{
	public TrackTypes m_TrackType;

	public EnergyTypes m_EnergyType;

	public TrainFlags m_TrainFlags;

	public float m_MaxSpeed;

	public float m_Acceleration;

	public float m_Braking;

	public float2 m_Turning;

	public float2 m_BogieOffsets;

	public float2 m_AttachOffsets;

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		TrackTypes trackType = m_TrackType;
		writer.Write((byte)trackType);
		EnergyTypes energyType = m_EnergyType;
		writer.Write((byte)energyType);
		TrainFlags trainFlags = m_TrainFlags;
		writer.Write((byte)trainFlags);
		float maxSpeed = m_MaxSpeed;
		writer.Write(maxSpeed);
		float acceleration = m_Acceleration;
		writer.Write(acceleration);
		float braking = m_Braking;
		writer.Write(braking);
		float2 turning = m_Turning;
		writer.Write(turning);
		float2 bogieOffsets = m_BogieOffsets;
		writer.Write(bogieOffsets);
		float2 attachOffsets = m_AttachOffsets;
		writer.Write(attachOffsets);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		reader.Read(out byte value);
		reader.Read(out byte value2);
		if (reader.context.version >= Version.trainPrefabFlags)
		{
			reader.Read(out byte value3);
			m_TrainFlags = (TrainFlags)value3;
		}
		ref float maxSpeed = ref m_MaxSpeed;
		reader.Read(out maxSpeed);
		ref float acceleration = ref m_Acceleration;
		reader.Read(out acceleration);
		ref float braking = ref m_Braking;
		reader.Read(out braking);
		ref float2 turning = ref m_Turning;
		reader.Read(out turning);
		ref float2 bogieOffsets = ref m_BogieOffsets;
		reader.Read(out bogieOffsets);
		ref float2 attachOffsets = ref m_AttachOffsets;
		reader.Read(out attachOffsets);
		m_TrackType = (TrackTypes)value;
		m_EnergyType = (EnergyTypes)value2;
	}
}
