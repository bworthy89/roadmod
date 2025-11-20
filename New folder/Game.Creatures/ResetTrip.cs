using Game.Citizens;
using Game.Economy;
using Unity.Entities;

namespace Game.Creatures;

public struct ResetTrip : IComponentData, IQueryTypeParameter
{
	public Entity m_Creature;

	public Entity m_Source;

	public Entity m_Target;

	public Entity m_DivertTarget;

	public Entity m_NextTarget;

	public Entity m_Arrived;

	public Resource m_TravelResource;

	public Resource m_DivertResource;

	public Resource m_NextResource;

	public ResidentFlags m_ResidentFlags;

	public int m_TravelData;

	public int m_DivertData;

	public int m_NextData;

	public uint m_Delay;

	public Purpose m_TravelPurpose;

	public Purpose m_DivertPurpose;

	public Purpose m_NextPurpose;

	public bool m_HasDivertPath;
}
