using System.Runtime.CompilerServices;
using Colossal.Entities;
using Colossal.UI.Binding;
using Game.Common;
using Game.Objects;
using Game.Prefabs;
using Unity.Entities;
using UnityEngine.Scripting;

namespace Game.UI.InGame;

[CompilerGenerated]
public class DestroyedTreeSection : InfoSectionBase
{
	protected override string group => "DestroyedTreeSection";

	private Entity destroyer { get; set; }

	protected override bool displayForDestroyedObjects => true;

	protected override void Reset()
	{
		destroyer = Entity.Null;
	}

	private bool Visible()
	{
		if (base.Destroyed)
		{
			return base.EntityManager.HasComponent<Tree>(selectedEntity);
		}
		return false;
	}

	[Preserve]
	protected override void OnUpdate()
	{
		base.visible = Visible();
	}

	protected override void OnProcess()
	{
		Destroyed componentData = base.EntityManager.GetComponentData<Destroyed>(selectedEntity);
		base.EntityManager.TryGetComponent<PrefabRef>(componentData.m_Event, out var component);
		destroyer = component.m_Prefab;
		m_InfoUISystem.tags.Add(SelectedInfoTags.Destroyed);
	}

	public override void OnWriteProperties(IJsonWriter writer)
	{
		writer.PropertyName("destroyer");
		if (destroyer != Entity.Null)
		{
			PrefabBase prefab = m_PrefabSystem.GetPrefab<PrefabBase>(destroyer);
			writer.Write(prefab.name);
		}
		else
		{
			writer.WriteNull();
		}
	}

	[Preserve]
	public DestroyedTreeSection()
	{
	}
}
