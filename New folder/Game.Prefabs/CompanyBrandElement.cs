using Unity.Entities;

namespace Game.Prefabs;

[InternalBufferCapacity(0)]
public struct CompanyBrandElement : IBufferElementData
{
	public Entity m_Brand;

	public CompanyBrandElement(Entity brand)
	{
		m_Brand = brand;
	}
}
