using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Colossal.Annotations;
using Colossal.Serialization.Entities;
using Colossal.UI.Binding;
using Game.Notifications;
using Game.Prefabs;
using Game.Tools;
using Game.UI.Localization;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using UnityEngine.Scripting;

namespace Game.UI.InGame;

[CompilerGenerated]
public class InfoviewsUISystem : UISystemBase
{
	public readonly struct Infoview : IComparable<Infoview>
	{
		public Entity entity { get; }

		[NotNull]
		public string id { get; }

		[NotNull]
		public string icon { get; }

		public bool locked { get; }

		[NotNull]
		public string uiTag { get; }

		public int group { get; }

		private int priority { get; }

		private bool editor { get; }

		public Infoview(Entity entity, InfoviewPrefab prefab, bool locked)
		{
			this.entity = entity;
			id = prefab.name;
			icon = prefab.m_IconPath;
			this.locked = locked;
			uiTag = prefab.uiTag;
			priority = prefab.m_Priority;
			group = prefab.m_Group;
			editor = prefab.m_Editor;
		}

		public int CompareTo(Infoview other)
		{
			int num = group - other.group;
			int num2 = ((num != 0) ? num : (priority - other.priority));
			if (num2 == 0)
			{
				return string.Compare(id, other.id, StringComparison.Ordinal);
			}
			return num2;
		}

		public void Write(PrefabUISystem prefabUISystem, IJsonWriter writer)
		{
			writer.TypeBegin("infoviews.Infoview");
			writer.PropertyName("entity");
			writer.Write(entity);
			writer.PropertyName("id");
			writer.Write(id);
			writer.PropertyName("icon");
			writer.Write(icon);
			writer.PropertyName("locked");
			writer.Write(locked);
			writer.PropertyName("uiTag");
			writer.Write(uiTag);
			writer.PropertyName("group");
			writer.Write(group);
			writer.PropertyName("editor");
			writer.Write(editor);
			writer.PropertyName("requirements");
			prefabUISystem.BindPrefabRequirements(writer, entity);
			writer.TypeEnd();
		}
	}

	private const string kGroup = "infoviews";

	private ToolSystem m_ToolSystem;

	private PrefabSystem m_PrefabSystem;

	private UnlockSystem m_UnlockSystem;

	private PrefabUISystem m_PrefabUISystem;

	private InfoviewInitializeSystem m_InfoviewInitializeSystem;

	private RawValueBinding m_ActiveView;

	private List<Infoview> m_InfoviewsCache;

	private RawValueBinding m_Infoviews;

	private EntityQuery m_UnlockedInfoviewQuery;

	private bool m_InfoviewChanged;

	public override GameMode gameMode => GameMode.GameOrEditor;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_UnlockedInfoviewQuery = GetEntityQuery(ComponentType.ReadOnly<Unlock>());
		m_ToolSystem = base.World.GetOrCreateSystemManaged<ToolSystem>();
		m_PrefabSystem = base.World.GetOrCreateSystemManaged<PrefabSystem>();
		m_UnlockSystem = base.World.GetOrCreateSystemManaged<UnlockSystem>();
		m_PrefabUISystem = base.World.GetOrCreateSystemManaged<PrefabUISystem>();
		m_InfoviewInitializeSystem = base.World.GetOrCreateSystemManaged<InfoviewInitializeSystem>();
		m_InfoviewsCache = new List<Infoview>();
		AddBinding(new TriggerBinding<Entity>("infoviews", "setActiveInfoview", SetActiveInfoview));
		AddBinding(new TriggerBinding<Entity, bool, int>("infoviews", "setInfomodeActive", SetInfomodeActive));
		AddBinding(m_Infoviews = new RawValueBinding("infoviews", "infoviews", BindInfoviews));
		AddBinding(m_ActiveView = new RawValueBinding("infoviews", "activeInfoview", BindActiveInfoview));
		ToolSystem toolSystem = m_ToolSystem;
		toolSystem.EventInfoviewChanged = (Action<InfoviewPrefab, InfoviewPrefab>)Delegate.Combine(toolSystem.EventInfoviewChanged, new Action<InfoviewPrefab, InfoviewPrefab>(OnInfoviewChanged));
		ToolSystem toolSystem2 = m_ToolSystem;
		toolSystem2.EventInfomodesChanged = (Action)Delegate.Combine(toolSystem2.EventInfomodesChanged, new Action(OnChanged));
	}

	[Preserve]
	protected override void OnDestroy()
	{
		ToolSystem toolSystem = m_ToolSystem;
		toolSystem.EventInfoviewChanged = (Action<InfoviewPrefab, InfoviewPrefab>)Delegate.Remove(toolSystem.EventInfoviewChanged, new Action<InfoviewPrefab, InfoviewPrefab>(OnInfoviewChanged));
		ToolSystem toolSystem2 = m_ToolSystem;
		toolSystem2.EventInfomodesChanged = (Action)Delegate.Remove(toolSystem2.EventInfomodesChanged, new Action(OnChanged));
		base.OnDestroy();
	}

	[Preserve]
	protected override void OnUpdate()
	{
		if (PrefabUtils.HasUnlockedPrefab<InfoviewData>(base.EntityManager, m_UnlockedInfoviewQuery))
		{
			m_Infoviews.Update();
		}
		if (m_InfoviewChanged)
		{
			m_ActiveView.Update();
			m_InfoviewChanged = false;
		}
	}

	protected override void OnGameLoaded(Context serializationContext)
	{
		m_Infoviews.Update();
		m_ActiveView.Update();
	}

	private void OnInfoviewChanged(InfoviewPrefab prefab, InfoviewPrefab oldPrefab)
	{
		m_InfoviewChanged = true;
		EntityQueryBuilder entityQueryBuilder = new EntityQueryBuilder(Allocator.Temp);
		entityQueryBuilder = entityQueryBuilder.WithAll<NotificationIconDisplayData>();
		entityQueryBuilder = entityQueryBuilder.WithOptions(EntityQueryOptions.IgnoreComponentEnabledState);
		NativeArray<Entity> nativeArray = entityQueryBuilder.Build(this).ToEntityArray(Allocator.Temp);
		Entity entity = Entity.Null;
		if (prefab != null && prefab.m_EnableNotificationIcon != null)
		{
			entity = m_PrefabSystem.GetEntity(prefab.m_EnableNotificationIcon);
		}
		Entity entity2 = Entity.Null;
		if (oldPrefab != null && oldPrefab.m_EnableNotificationIcon != null)
		{
			entity2 = m_PrefabSystem.GetEntity(oldPrefab.m_EnableNotificationIcon);
		}
		foreach (Entity item in nativeArray)
		{
			if (item == entity)
			{
				base.EntityManager.SetComponentEnabled<NotificationIconDisplayData>(item, value: true);
			}
			if (item == entity2)
			{
				base.EntityManager.SetComponentEnabled<NotificationIconDisplayData>(item, value: false);
			}
		}
		base.World.GetOrCreateSystemManaged<IconClusterSystem>().RecalculateClusters();
		nativeArray.Dispose();
	}

	private void OnChanged()
	{
		m_InfoviewChanged = true;
	}

	private void BindInfoviews(IJsonWriter writer)
	{
		m_InfoviewsCache.Clear();
		foreach (InfoviewPrefab infoview in m_InfoviewInitializeSystem.infoviews)
		{
			if (infoview.isValid)
			{
				bool locked = m_UnlockSystem.IsLocked(infoview);
				m_InfoviewsCache.Add(new Infoview(m_PrefabSystem.GetEntity(infoview), infoview, locked));
			}
		}
		m_InfoviewsCache.Sort();
		writer.ArrayBegin(m_InfoviewsCache.Count);
		foreach (Infoview item in m_InfoviewsCache)
		{
			item.Write(m_PrefabUISystem, writer);
		}
		writer.ArrayEnd();
	}

	private void BindActiveInfoview(IJsonWriter writer)
	{
		InfoviewPrefab activeInfoview = m_ToolSystem.activeInfoview;
		if (activeInfoview != null)
		{
			Entity entity = m_PrefabSystem.GetEntity(activeInfoview);
			writer.TypeBegin("infoviews.ActiveInfoview");
			writer.PropertyName("entity");
			writer.Write(entity);
			writer.PropertyName("id");
			writer.Write(activeInfoview.name);
			writer.PropertyName("icon");
			writer.Write(activeInfoview.m_IconPath);
			writer.PropertyName("uiTag");
			writer.Write(activeInfoview.uiTag);
			List<InfomodeInfo> infoviewInfomodes = m_ToolSystem.GetInfoviewInfomodes();
			writer.PropertyName("infomodes");
			writer.ArrayBegin(infoviewInfomodes.Count);
			for (int i = 0; i < infoviewInfomodes.Count; i++)
			{
				BindInfomode(writer, infoviewInfomodes[i]);
			}
			writer.ArrayEnd();
			writer.PropertyName("editor");
			writer.Write(activeInfoview.m_Editor);
			writer.TypeEnd();
		}
		else
		{
			writer.WriteNull();
		}
	}

	private void BindInfomode(IJsonWriter writer, InfomodeInfo info)
	{
		Entity entity = m_PrefabSystem.GetEntity(info.m_Mode);
		IColorInfomode colorInfomode = info.m_Mode as IColorInfomode;
		IGradientInfomode gradientInfomode = info.m_Mode as IGradientInfomode;
		writer.TypeBegin("infoviews.Infomode");
		writer.PropertyName("entity");
		writer.Write(entity);
		writer.PropertyName("id");
		writer.Write(info.m_Mode.name);
		writer.PropertyName("uiTag");
		writer.Write(info.m_Mode.uiTag);
		writer.PropertyName("active");
		writer.Write(m_ToolSystem.IsInfomodeActive(info.m_Mode));
		writer.PropertyName("priority");
		writer.Write(info.m_Priority);
		writer.PropertyName("color");
		if (colorInfomode != null)
		{
			writer.Write(colorInfomode.color);
		}
		else if (gradientInfomode != null && gradientInfomode.legendType == GradientLegendType.Fields && !gradientInfomode.lowLabel.HasValue)
		{
			writer.Write(gradientInfomode.lowColor);
		}
		else
		{
			writer.WriteNull();
		}
		writer.PropertyName("gradientLegend");
		if (gradientInfomode != null && gradientInfomode.legendType == GradientLegendType.Gradient)
		{
			BindInfomodeGradientLegend(writer, gradientInfomode);
		}
		else
		{
			writer.WriteNull();
		}
		writer.PropertyName("colorLegends");
		if (gradientInfomode != null && gradientInfomode.legendType == GradientLegendType.Fields)
		{
			BindColorLegends(writer, gradientInfomode);
		}
		else
		{
			writer.WriteEmptyArray();
		}
		writer.PropertyName("type");
		writer.Write(info.m_Mode.infomodeTypeLocaleKey);
		writer.TypeEnd();
	}

	private void BindInfomodeGradientLegend(IJsonWriter writer, IGradientInfomode gradientInfomode)
	{
		writer.TypeBegin("infoviews.InfomodeGradientLegend");
		writer.PropertyName("lowLabel");
		writer.Write(gradientInfomode.lowLabel);
		writer.PropertyName("highLabel");
		writer.Write(gradientInfomode.highLabel);
		writer.PropertyName("gradient");
		writer.TypeBegin("infoviews.Gradient");
		writer.PropertyName("stops");
		writer.ArrayBegin(3u);
		BindGradientStop(writer, 0f, gradientInfomode.lowColor);
		BindGradientStop(writer, 0.5f, gradientInfomode.mediumColor);
		BindGradientStop(writer, 1f, gradientInfomode.highColor);
		writer.ArrayEnd();
		writer.TypeEnd();
		writer.TypeEnd();
	}

	private void BindGradientStop(IJsonWriter writer, float offset, Color color)
	{
		writer.TypeBegin("infoviews.GradientStop");
		writer.PropertyName("offset");
		writer.Write(offset);
		writer.PropertyName("color");
		writer.Write(color);
		writer.TypeEnd();
	}

	private void BindColorLegends(IJsonWriter writer, IGradientInfomode gradientInfomode)
	{
		uint num = 0u;
		if (gradientInfomode.lowLabel.HasValue)
		{
			num++;
		}
		if (gradientInfomode.mediumLabel.HasValue)
		{
			num++;
		}
		if (gradientInfomode.highLabel.HasValue)
		{
			num++;
		}
		writer.ArrayBegin(num);
		if (gradientInfomode.lowLabel.HasValue)
		{
			BindColorLegend(writer, gradientInfomode.lowColor, gradientInfomode.lowLabel.Value);
		}
		if (gradientInfomode.mediumLabel.HasValue)
		{
			BindColorLegend(writer, gradientInfomode.mediumColor, gradientInfomode.mediumLabel.Value);
		}
		if (gradientInfomode.highLabel.HasValue)
		{
			BindColorLegend(writer, gradientInfomode.highColor, gradientInfomode.highLabel.Value);
		}
		writer.ArrayEnd();
	}

	private void BindColorLegend(IJsonWriter writer, Color color, LocalizedString label)
	{
		writer.TypeBegin("infoviews.ColorLegend");
		writer.PropertyName("color");
		writer.Write(color);
		writer.PropertyName("label");
		writer.Write(label);
		writer.TypeEnd();
	}

	public void SetActiveInfoview(Entity entity)
	{
		InfoviewPrefab infoview = null;
		if (base.EntityManager.Exists(entity))
		{
			infoview = m_PrefabSystem.GetPrefab<InfoviewPrefab>(entity);
		}
		m_ToolSystem.infoview = infoview;
	}

	private void SetInfomodeActive(Entity entity, bool active, int priority)
	{
		m_ToolSystem.SetInfomodeActive(entity, active, priority);
	}

	[Preserve]
	public InfoviewsUISystem()
	{
	}
}
