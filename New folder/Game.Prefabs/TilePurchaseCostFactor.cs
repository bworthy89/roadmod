using Unity.Entities;

namespace Game.Prefabs;

public struct TilePurchaseCostFactor : IComponentData, IQueryTypeParameter
{
	public float m_Amount;

	public TilePurchaseCostFactor(float amount)
	{
		m_Amount = amount;
	}
}
