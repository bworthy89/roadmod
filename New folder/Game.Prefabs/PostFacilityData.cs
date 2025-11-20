using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Prefabs;

public struct PostFacilityData : IComponentData, IQueryTypeParameter, ICombineData<PostFacilityData>, ISerializable
{
	public int m_PostVanCapacity;

	public int m_PostTruckCapacity;

	public int m_MailCapacity;

	public int m_SortingRate;

	public void Combine(PostFacilityData otherData)
	{
		m_PostVanCapacity += otherData.m_PostVanCapacity;
		m_PostTruckCapacity += otherData.m_PostTruckCapacity;
		m_MailCapacity += otherData.m_MailCapacity;
		m_SortingRate += otherData.m_SortingRate;
	}

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		int postVanCapacity = m_PostVanCapacity;
		writer.Write(postVanCapacity);
		int postTruckCapacity = m_PostTruckCapacity;
		writer.Write(postTruckCapacity);
		int mailCapacity = m_MailCapacity;
		writer.Write(mailCapacity);
		int sortingRate = m_SortingRate;
		writer.Write(sortingRate);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		ref int postVanCapacity = ref m_PostVanCapacity;
		reader.Read(out postVanCapacity);
		ref int postTruckCapacity = ref m_PostTruckCapacity;
		reader.Read(out postTruckCapacity);
		ref int mailCapacity = ref m_MailCapacity;
		reader.Read(out mailCapacity);
		ref int sortingRate = ref m_SortingRate;
		reader.Read(out sortingRate);
	}
}
