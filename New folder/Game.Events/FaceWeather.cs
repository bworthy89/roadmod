using Unity.Entities;

namespace Game.Events;

public struct FaceWeather : IComponentData, IQueryTypeParameter
{
	public Entity m_Event;

	public Entity m_Target;

	public float m_Severity;
}
