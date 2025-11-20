using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Creatures;

public struct Pet : IComponentData, IQueryTypeParameter, ISerializable
{
	public Entity m_HouseholdPet;

	public PetFlags m_Flags;

	public Pet(Entity householdPet)
	{
		m_HouseholdPet = householdPet;
		m_Flags = PetFlags.None;
	}

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		writer.Write((uint)m_Flags);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		reader.Read(out uint value);
		m_Flags = (PetFlags)value;
	}
}
