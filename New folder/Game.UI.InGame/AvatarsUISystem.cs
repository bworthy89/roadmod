using System.Runtime.CompilerServices;
using Colossal.Annotations;
using Colossal.Entities;
using Colossal.UI.Binding;
using Game.Common;
using Game.Companies;
using Game.Prefabs;
using JetBrains.Annotations;
using Unity.Entities;
using UnityEngine;
using UnityEngine.Scripting;

namespace Game.UI.InGame;

[CompilerGenerated]
public class AvatarsUISystem : UISystemBase
{
	private const string kGroup = "avatars";

	private const int kIconSize = 32;

	private PrefabSystem m_PrefabSystem;

	private NameSystem m_NameSystem;

	private EntityQuery m_ColorsQuery;

	[UsedImplicitly]
	private RawMapBinding<Entity> m_AvatarsBinding;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_PrefabSystem = base.World.GetOrCreateSystemManaged<PrefabSystem>();
		m_NameSystem = base.World.GetOrCreateSystemManaged<NameSystem>();
		m_ColorsQuery = GetEntityQuery(ComponentType.ReadOnly<UIAvatarColorData>());
		AddBinding(m_AvatarsBinding = new RawMapBinding<Entity>("avatars", "avatarsMap", BindAvatar));
		RequireForUpdate(m_ColorsQuery);
	}

	[Preserve]
	protected override void OnUpdate()
	{
	}

	private void BindAvatar(IJsonWriter writer, Entity entity)
	{
		writer.TypeBegin("avatars.AvatarData");
		writer.PropertyName("picture");
		writer.Write(GetPicture(entity));
		writer.PropertyName("name");
		m_NameSystem.BindName(writer, entity);
		Color32 color = GetColor(entity);
		writer.PropertyName("color");
		writer.Write(color);
		writer.TypeEnd();
	}

	private Color32 GetColor(Entity entity)
	{
		DynamicBuffer<UIAvatarColorData> singletonBuffer = m_ColorsQuery.GetSingletonBuffer<UIAvatarColorData>();
		int randomIndex = GetRandomIndex(entity);
		if (randomIndex < 0)
		{
			return singletonBuffer[0].m_Color;
		}
		return singletonBuffer[randomIndex % singletonBuffer.Length].m_Color;
	}

	[Colossal.Annotations.CanBeNull]
	private string GetPicture(Entity entity)
	{
		if (base.EntityManager.TryGetComponent<CompanyData>(entity, out var component))
		{
			entity = component.m_Brand;
		}
		if (base.EntityManager.TryGetComponent<PrefabData>(entity, out var component2) && m_PrefabSystem.TryGetPrefab<PrefabBase>(component2, out var prefab))
		{
			string icon = ImageSystem.GetIcon(prefab);
			if (icon != null)
			{
				return icon;
			}
			if (prefab is ChirperAccount chirperAccount && chirperAccount.m_InfoView != null && chirperAccount.m_InfoView.m_IconPath != null)
			{
				return chirperAccount.m_InfoView.m_IconPath;
			}
			if (prefab is BrandPrefab brandPrefab)
			{
				return $"{brandPrefab.thumbnailUrl}?width={32}&height={32}";
			}
		}
		return null;
	}

	private int GetRandomIndex(Entity entity)
	{
		if (base.EntityManager.TryGetBuffer(entity, isReadOnly: true, out DynamicBuffer<RandomLocalizationIndex> buffer) && buffer.Length > 0)
		{
			return buffer[0].m_Index;
		}
		return 0;
	}

	[Preserve]
	public AvatarsUISystem()
	{
	}
}
