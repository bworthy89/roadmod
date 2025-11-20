using Unity.Entities;

namespace Game.Events;

public struct Endanger : IComponentData, IQueryTypeParameter
{
	public Entity m_Event;

	public Entity m_Target;

	public DangerFlags m_Flags;

	public uint m_EndFrame;
}
