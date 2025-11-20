using System;
using Unity.Entities;

namespace Game.Prefabs.Modes;

[ComponentMenu("Modes/Mode Component/", new Type[] { })]
public class SchoolMode : LocalModePrefab
{
	[Serializable]
	public class ModeData
	{
		public PrefabBase m_Prefab;

		public float m_StudentCapacityMultifier;

		public float m_GraduationModifier;

		public sbyte m_StudentWellbeing;

		public sbyte m_StudentHealth;
	}

	public ModeData[] m_ModeDatas;

	public override void RecordChanges(EntityManager entityManager, PrefabSystem prefabSystem)
	{
		for (int i = 0; i < m_ModeDatas.Length; i++)
		{
			School component = m_ModeDatas[i].m_Prefab.GetComponent<School>();
			if (component == null)
			{
				ComponentBase.baseLog.Critical($"Target not found {this}");
				continue;
			}
			Entity entity = prefabSystem.GetEntity(component.prefab);
			entityManager.GetComponentData<SchoolData>(entity);
		}
	}

	public override void ApplyModeData(EntityManager entityManager, PrefabSystem prefabSystem)
	{
		for (int i = 0; i < m_ModeDatas.Length; i++)
		{
			ModeData modeData = m_ModeDatas[i];
			School component = modeData.m_Prefab.GetComponent<School>();
			if (component == null)
			{
				ComponentBase.baseLog.Critical($"Target not found {this}");
				continue;
			}
			Entity entity = prefabSystem.GetEntity(component.prefab);
			SchoolData componentData = entityManager.GetComponentData<SchoolData>(entity);
			componentData.m_StudentCapacity = (int)((float)componentData.m_StudentCapacity * modeData.m_StudentCapacityMultifier);
			componentData.m_GraduationModifier = modeData.m_GraduationModifier;
			componentData.m_StudentWellbeing = modeData.m_StudentWellbeing;
			componentData.m_StudentHealth = modeData.m_StudentHealth;
			entityManager.SetComponentData(entity, componentData);
		}
	}

	public override void RestoreDefaultData(EntityManager entityManager, PrefabSystem prefabSystem)
	{
		for (int i = 0; i < m_ModeDatas.Length; i++)
		{
			School component = m_ModeDatas[i].m_Prefab.GetComponent<School>();
			if (component == null)
			{
				ComponentBase.baseLog.Critical($"Target not found {this}");
				continue;
			}
			Entity entity = prefabSystem.GetEntity(component.prefab);
			SchoolData componentData = entityManager.GetComponentData<SchoolData>(entity);
			componentData.m_StudentCapacity = component.m_StudentCapacity;
			componentData.m_GraduationModifier = component.m_GraduationModifier;
			componentData.m_StudentWellbeing = component.m_StudentWellbeing;
			componentData.m_StudentHealth = component.m_StudentHealth;
			entityManager.SetComponentData(entity, componentData);
		}
	}
}
