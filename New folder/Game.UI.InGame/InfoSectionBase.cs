using System.Collections.Generic;
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
public abstract class InfoSectionBase : UISystemBase, ISectionSource, IJsonWritable
{
	protected bool m_Dirty;

	protected NameSystem m_NameSystem;

	protected PrefabSystem m_PrefabSystem;

	protected EndFrameBarrier m_EndFrameBarrier;

	protected SelectedInfoUISystem m_InfoUISystem;

	public override GameMode gameMode => GameMode.Game;

	public bool visible { get; protected set; }

	protected virtual bool displayForDestroyedObjects => false;

	protected virtual bool displayForOutsideConnections => false;

	protected virtual bool displayForUnderConstruction => false;

	protected virtual bool displayForUpgrades => false;

	protected abstract string group { get; }

	protected List<string> tooltipKeys { get; set; }

	protected List<string> tooltipTags { get; set; }

	protected virtual Entity selectedEntity => m_InfoUISystem.selectedEntity;

	protected virtual Entity selectedPrefab => m_InfoUISystem.selectedPrefab;

	protected bool Destroyed => base.EntityManager.HasComponent<Destroyed>(selectedEntity);

	protected bool OutsideConnection => base.EntityManager.HasComponent<Game.Objects.OutsideConnection>(selectedEntity);

	protected bool UnderConstruction
	{
		get
		{
			if (base.EntityManager.TryGetComponent<UnderConstruction>(selectedEntity, out var component))
			{
				return component.m_NewPrefab == Entity.Null;
			}
			return false;
		}
	}

	protected bool Upgrade => base.EntityManager.HasComponent<ServiceUpgradeData>(selectedPrefab);

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		tooltipKeys = new List<string>();
		tooltipTags = new List<string>();
		m_NameSystem = base.World.GetOrCreateSystemManaged<NameSystem>();
		m_PrefabSystem = base.World.GetOrCreateSystemManaged<PrefabSystem>();
		m_EndFrameBarrier = base.World.GetOrCreateSystemManaged<EndFrameBarrier>();
		m_InfoUISystem = base.World.GetOrCreateSystemManaged<SelectedInfoUISystem>();
	}

	protected abstract void Reset();

	protected abstract void OnProcess();

	public abstract void OnWriteProperties(IJsonWriter writer);

	public void RequestUpdate()
	{
		m_Dirty = true;
	}

	private bool Visible()
	{
		if (visible && (!Destroyed || displayForDestroyedObjects) && (!OutsideConnection || displayForOutsideConnections) && (!UnderConstruction || displayForUnderConstruction))
		{
			if (Upgrade)
			{
				return displayForUpgrades;
			}
			return true;
		}
		return false;
	}

	protected virtual void OnPreUpdate()
	{
	}

	public void PerformUpdate()
	{
		OnPreUpdate();
		if (m_Dirty)
		{
			m_Dirty = false;
			tooltipKeys.Clear();
			tooltipTags.Clear();
			Reset();
			Update();
			if (Visible())
			{
				OnProcess();
			}
		}
	}

	public void Write(IJsonWriter writer)
	{
		if (Visible())
		{
			writer.TypeBegin(GetType().FullName);
			writer.PropertyName("group");
			writer.Write(group);
			writer.PropertyName("tooltipKeys");
			writer.ArrayBegin(tooltipKeys.Count);
			for (int i = 0; i < tooltipKeys.Count; i++)
			{
				writer.Write(tooltipKeys[i]);
			}
			writer.ArrayEnd();
			writer.PropertyName("tooltipTags");
			writer.ArrayBegin(tooltipTags.Count);
			for (int j = 0; j < tooltipTags.Count; j++)
			{
				writer.Write(tooltipTags[j]);
			}
			writer.ArrayEnd();
			OnWriteProperties(writer);
			writer.TypeEnd();
		}
		else
		{
			writer.WriteNull();
		}
	}

	protected bool TryGetComponentWithUpgrades<T>(Entity entity, Entity prefab, out T data) where T : unmanaged, IComponentData, ICombineData<T>
	{
		return UpgradeUtils.TryGetCombinedComponent<T>(base.EntityManager, entity, prefab, out data);
	}

	[Preserve]
	protected InfoSectionBase()
	{
	}
}
