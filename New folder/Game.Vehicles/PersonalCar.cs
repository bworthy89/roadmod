using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Vehicles;

public struct PersonalCar : IComponentData, IQueryTypeParameter, ISerializable
{
	public Entity m_Keeper;

	public PersonalCarFlags m_State;

	public PersonalCar(Entity keeper, PersonalCarFlags state)
	{
		m_Keeper = keeper;
		m_State = state;
	}

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		Entity keeper = m_Keeper;
		writer.Write(keeper);
		PersonalCarFlags state = m_State;
		writer.Write((uint)state);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		ref Entity keeper = ref m_Keeper;
		reader.Read(out keeper);
		reader.Read(out uint value);
		m_State = (PersonalCarFlags)value;
	}
}
