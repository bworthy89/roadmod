using System.Collections.Generic;
using Colossal.Annotations;
using Unity.Entities;
using UnityEngine;

namespace Game.Prefabs;

public abstract class UITagPrefabBase : PrefabBase
{
	[Tooltip("If set, the override is used as a UI tag instead of the generated tag.")]
	[CanBeNull]
	public string m_Override;

	public override void GetPrefabComponents(HashSet<ComponentType> prefabComponents)
	{
		base.GetPrefabComponents(prefabComponents);
		prefabComponents.Add(ComponentType.ReadWrite<UITagPrefabData>());
	}
}
