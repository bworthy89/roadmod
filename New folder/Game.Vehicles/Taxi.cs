using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Vehicles;

public struct Taxi : IComponentData, IQueryTypeParameter, ISerializable
{
	public Entity m_TargetRequest;

	public TaxiFlags m_State;

	public float m_PathElementTime;

	public float m_StartDistance;

	public float m_MaxBoardingDistance;

	public float m_MinWaitingDistance;

	public int m_ExtraPathElementCount;

	public ushort m_NextStartingFee;

	public ushort m_CurrentFee;

	public Taxi(TaxiFlags flags)
	{
		m_TargetRequest = Entity.Null;
		m_State = flags;
		m_PathElementTime = 0f;
		m_StartDistance = 0f;
		m_MaxBoardingDistance = 0f;
		m_MinWaitingDistance = 0f;
		m_ExtraPathElementCount = 0;
		m_NextStartingFee = 0;
		m_CurrentFee = 0;
	}

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		Entity targetRequest = m_TargetRequest;
		writer.Write(targetRequest);
		TaxiFlags state = m_State;
		writer.Write((uint)state);
		float pathElementTime = m_PathElementTime;
		writer.Write(pathElementTime);
		int extraPathElementCount = m_ExtraPathElementCount;
		writer.Write(extraPathElementCount);
		float startDistance = m_StartDistance;
		writer.Write(startDistance);
		ushort nextStartingFee = m_NextStartingFee;
		writer.Write(nextStartingFee);
		ushort currentFee = m_CurrentFee;
		writer.Write(currentFee);
		float maxBoardingDistance = m_MaxBoardingDistance;
		writer.Write(maxBoardingDistance);
		float minWaitingDistance = m_MinWaitingDistance;
		writer.Write(minWaitingDistance);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		if (reader.context.version >= Version.reverseServiceRequests)
		{
			ref Entity targetRequest = ref m_TargetRequest;
			reader.Read(out targetRequest);
		}
		reader.Read(out uint value);
		ref float pathElementTime = ref m_PathElementTime;
		reader.Read(out pathElementTime);
		ref int extraPathElementCount = ref m_ExtraPathElementCount;
		reader.Read(out extraPathElementCount);
		m_State = (TaxiFlags)value;
		if (reader.context.version >= Version.taxiFee)
		{
			ref float startDistance = ref m_StartDistance;
			reader.Read(out startDistance);
			ref ushort nextStartingFee = ref m_NextStartingFee;
			reader.Read(out nextStartingFee);
			ref ushort currentFee = ref m_CurrentFee;
			reader.Read(out currentFee);
		}
		if (reader.context.version >= Version.roadPatchImprovements)
		{
			ref float maxBoardingDistance = ref m_MaxBoardingDistance;
			reader.Read(out maxBoardingDistance);
			ref float minWaitingDistance = ref m_MinWaitingDistance;
			reader.Read(out minWaitingDistance);
		}
	}
}
