using System;
using System.Collections.Generic;
using Unity.Entities;

namespace Game.Prefabs;

[ComponentMenu("Rendering/", new Type[] { typeof(RenderPrefab) })]
public class StackProperties : ComponentBase
{
	public StackDirection m_Direction = StackDirection.Up;

	public StackOrder m_Order = StackOrder.Middle;

	public float m_StartOverlap;

	public float m_EndOverlap;

	public bool m_ForbidScaling;

	public override void GetArchetypeComponents(HashSet<ComponentType> components)
	{
	}

	public override void GetPrefabComponents(HashSet<ComponentType> components)
	{
	}
}
