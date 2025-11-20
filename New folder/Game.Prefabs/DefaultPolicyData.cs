using Unity.Entities;

namespace Game.Prefabs;

[InternalBufferCapacity(0)]
public struct DefaultPolicyData : IBufferElementData
{
	public Entity m_Policy;

	public DefaultPolicyData(Entity policy)
	{
		m_Policy = policy;
	}
}
