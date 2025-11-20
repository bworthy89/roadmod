using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Prefabs;

[ComponentMenu("Rendering/", new Type[] { typeof(RenderPrefab) })]
public class CharacterProperties : ComponentBase
{
	[Flags]
	public enum BodyPart
	{
		Torso = 1,
		Head = 2,
		Face = 4,
		Legs = 8,
		Feet = 0x10,
		Beard = 0x20,
		Neck = 0x40
	}

	public BodyPart m_BodyParts;

	public string m_CorrectiveAnimationName;

	public string m_AnimatedPropName;

	public CharacterOverlay[] m_Overlays;

	public override void GetDependencies(List<PrefabBase> prefabs)
	{
		base.GetDependencies(prefabs);
		if (m_Overlays != null && m_Overlays.Length != 0)
		{
			for (int i = 0; i < m_Overlays.Length; i++)
			{
				prefabs.Add(m_Overlays[i]);
			}
		}
	}

	public override void GetArchetypeComponents(HashSet<ComponentType> components)
	{
	}

	public override void GetPrefabComponents(HashSet<ComponentType> components)
	{
		if (m_Overlays != null && m_Overlays.Length != 0)
		{
			components.Add(ComponentType.ReadWrite<OverlayElement>());
		}
	}

	public override void LateInitialize(EntityManager entityManager, Entity entity)
	{
		base.LateInitialize(entityManager, entity);
		if (m_Overlays != null && m_Overlays.Length != 0)
		{
			PrefabSystem existingSystemManaged = entityManager.World.GetExistingSystemManaged<PrefabSystem>();
			DynamicBuffer<OverlayElement> buffer = entityManager.GetBuffer<OverlayElement>(entity);
			int num = 0;
			for (int i = 0; i < m_Overlays.Length; i++)
			{
				CharacterOverlay characterOverlay = m_Overlays[i];
				num = math.max(num, characterOverlay.m_Index + 1);
			}
			buffer.Resize(num, NativeArrayOptions.ClearMemory);
			for (int j = 0; j < m_Overlays.Length; j++)
			{
				CharacterOverlay characterOverlay2 = m_Overlays[j];
				buffer[characterOverlay2.m_Index] = new OverlayElement
				{
					m_Overlay = existingSystemManaged.GetEntity(characterOverlay2),
					m_SortOrder = characterOverlay2.m_SortOrder,
					m_SourceRegion = characterOverlay2.GetRegionAsFloat4(characterOverlay2.m_sourceRegion),
					m_TargetRegion = characterOverlay2.GetRegionAsFloat4(characterOverlay2.m_targetRegion)
				};
			}
		}
	}
}
