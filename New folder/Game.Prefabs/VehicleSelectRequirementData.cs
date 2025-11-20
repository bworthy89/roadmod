using Game.City;
using Unity.Entities;

namespace Game.Prefabs;

public struct VehicleSelectRequirementData
{
	public struct Chunk
	{
		internal EnabledMask m_LockedMask;

		internal BufferAccessor<ObjectRequirementElement> m_ObjectRequirements;
	}

	private ComponentTypeHandle<Locked> m_LockedType;

	private BufferTypeHandle<ObjectRequirementElement> m_ObjectRequirementType;

	private ComponentLookup<ThemeData> m_ThemeData;

	private Entity m_DefaultTheme;

	public VehicleSelectRequirementData(SystemBase system)
	{
		m_LockedType = system.GetComponentTypeHandle<Locked>(isReadOnly: true);
		m_ObjectRequirementType = system.GetBufferTypeHandle<ObjectRequirementElement>(isReadOnly: true);
		m_ThemeData = system.GetComponentLookup<ThemeData>(isReadOnly: true);
		m_DefaultTheme = default(Entity);
	}

	public void Update(SystemBase system, CityConfigurationSystem cityConfigurationSystem)
	{
		m_LockedType.Update(system);
		m_ObjectRequirementType.Update(system);
		m_ThemeData.Update(system);
		m_DefaultTheme = cityConfigurationSystem.defaultTheme;
	}

	public Chunk GetChunk(ArchetypeChunk chunk)
	{
		return new Chunk
		{
			m_LockedMask = chunk.GetEnabledMask(ref m_LockedType),
			m_ObjectRequirements = chunk.GetBufferAccessor(ref m_ObjectRequirementType)
		};
	}

	public bool CheckRequirements(ref Chunk chunk, int index, bool ignoreTheme = false)
	{
		if (chunk.m_LockedMask.EnableBit.IsValid && chunk.m_LockedMask[index])
		{
			return false;
		}
		if (chunk.m_ObjectRequirements.Length != 0)
		{
			DynamicBuffer<ObjectRequirementElement> dynamicBuffer = chunk.m_ObjectRequirements[index];
			int num = -1;
			bool flag = true;
			for (int i = 0; i < dynamicBuffer.Length; i++)
			{
				ObjectRequirementElement objectRequirementElement = dynamicBuffer[i];
				if (objectRequirementElement.m_Group != num)
				{
					if (!flag)
					{
						break;
					}
					num = objectRequirementElement.m_Group;
					flag = false;
				}
				flag |= m_DefaultTheme == objectRequirementElement.m_Requirement || (ignoreTheme && m_ThemeData.HasComponent(objectRequirementElement.m_Requirement));
			}
			if (!flag)
			{
				return false;
			}
		}
		return true;
	}
}
