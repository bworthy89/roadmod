using System;
using System.Collections.Generic;
using Game.Input;
using Unity.Entities;

namespace Game.Prefabs;

[ComponentMenu("UI/", new Type[] { })]
public class UIShortcut : ComponentBase
{
	public UIInputAction m_Action;

	public override void GetPrefabComponents(HashSet<ComponentType> components)
	{
	}

	public override void GetArchetypeComponents(HashSet<ComponentType> components)
	{
	}
}
