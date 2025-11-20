using System;
using System.Collections.Generic;
using Game.Tools;
using Unity.Entities;

namespace Game.Prefabs;

[ComponentMenu("Notifications/", new Type[] { typeof(NotificationIconPrefab) })]
public class ToolError : ComponentBase
{
	public ErrorType m_Error;

	public bool m_TemporaryOnly;

	public bool m_DisableInGame;

	public bool m_DisableInEditor;

	public override void GetPrefabComponents(HashSet<ComponentType> components)
	{
		components.Add(ComponentType.ReadWrite<ToolErrorData>());
	}

	public override void GetArchetypeComponents(HashSet<ComponentType> components)
	{
	}

	public override void Initialize(EntityManager entityManager, Entity entity)
	{
		base.Initialize(entityManager, entity);
		ToolErrorData componentData = default(ToolErrorData);
		componentData.m_Error = m_Error;
		componentData.m_Flags = (ToolErrorFlags)0;
		if (m_TemporaryOnly)
		{
			componentData.m_Flags |= ToolErrorFlags.TemporaryOnly;
		}
		if (m_DisableInGame)
		{
			componentData.m_Flags |= ToolErrorFlags.DisableInGame;
		}
		if (m_DisableInEditor)
		{
			componentData.m_Flags |= ToolErrorFlags.DisableInEditor;
		}
		entityManager.SetComponentData(entity, componentData);
	}
}
