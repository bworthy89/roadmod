using Unity.Collections;
using Unity.Entities;

namespace Game.Prefabs;

public struct ServiceUpkeepData : IBufferElementData, ICombineBuffer<ServiceUpkeepData>
{
	public ResourceStack m_Upkeep;

	public bool m_ScaleWithUsage;

	public void Combine(NativeList<ServiceUpkeepData> result)
	{
		for (int i = 0; i < result.Length; i++)
		{
			ref ServiceUpkeepData reference = ref result.ElementAt(i);
			if (reference.m_Upkeep.m_Resource == m_Upkeep.m_Resource && reference.m_ScaleWithUsage == m_ScaleWithUsage)
			{
				reference.m_Upkeep.m_Amount += m_Upkeep.m_Amount;
				return;
			}
		}
		result.Add(in this);
	}

	public ServiceUpkeepData ApplyServiceUsage(float scale)
	{
		return new ServiceUpkeepData
		{
			m_Upkeep = new ResourceStack
			{
				m_Amount = (int)((float)m_Upkeep.m_Amount * scale),
				m_Resource = m_Upkeep.m_Resource
			},
			m_ScaleWithUsage = m_ScaleWithUsage
		};
	}
}
