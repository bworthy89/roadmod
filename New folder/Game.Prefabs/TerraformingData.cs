using Unity.Entities;

namespace Game.Prefabs;

public struct TerraformingData : IComponentData, IQueryTypeParameter
{
	public TerraformingType m_Type;

	public TerraformingTarget m_Target;
}
