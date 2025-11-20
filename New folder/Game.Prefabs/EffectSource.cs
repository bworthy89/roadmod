using System;
using System.Collections.Generic;
using Colossal.Collections;
using Game.Effects;
using Game.UI.Widgets;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Game.Prefabs;

[ComponentMenu("Effects/", new Type[] { })]
public class EffectSource : ComponentBase
{
	[Serializable]
	public class EffectSettings
	{
		public EffectPrefab m_Effect;

		[InputField]
		[RangeN(-10000f, 10000f, true)]
		public float3 m_PositionOffset;

		public quaternion m_Rotation = quaternion.identity;

		public float3 m_Scale = new float3(1f, 1f, 1f);

		public float m_Intensity = 1f;

		public int m_ParentMesh;

		public int m_AnimationIndex = -1;
	}

	[Serializable]
	public class AnimationProperties
	{
		public float m_Duration;

		public AnimationCurve m_Curve;
	}

	public List<EffectSettings> m_Effects;

	public List<AnimationProperties> m_AnimationCurves;

	public override void GetPrefabComponents(HashSet<ComponentType> components)
	{
		components.Add(ComponentType.ReadWrite<Effect>());
		if (m_AnimationCurves != null && m_AnimationCurves.Count != 0)
		{
			components.Add(ComponentType.ReadWrite<EffectAnimation>());
		}
	}

	public override void GetArchetypeComponents(HashSet<ComponentType> components)
	{
		components.Add(ComponentType.ReadWrite<EnabledEffect>());
	}

	public override void GetDependencies(List<PrefabBase> prefabs)
	{
		base.GetDependencies(prefabs);
		if (m_Effects == null)
		{
			return;
		}
		foreach (EffectSettings effect in m_Effects)
		{
			prefabs.Add(effect.m_Effect);
		}
	}

	public override void LateInitialize(EntityManager entityManager, Entity entity)
	{
		base.LateInitialize(entityManager, entity);
		PrefabSystem existingSystemManaged = entityManager.World.GetExistingSystemManaged<PrefabSystem>();
		if (m_Effects != null)
		{
			DynamicBuffer<Effect> buffer = entityManager.GetBuffer<Effect>(entity);
			buffer.EnsureCapacity(m_Effects.Count);
			for (int i = 0; i < m_Effects.Count; i++)
			{
				EffectSettings effectSettings = m_Effects[i];
				if (!(effectSettings.m_Effect == null))
				{
					if (effectSettings.m_Intensity == 0f)
					{
						effectSettings.m_Intensity = 1f;
					}
					buffer.Add(new Effect
					{
						m_Effect = existingSystemManaged.GetEntity(effectSettings.m_Effect),
						m_Position = effectSettings.m_PositionOffset,
						m_Rotation = effectSettings.m_Rotation,
						m_Scale = effectSettings.m_Scale,
						m_Intensity = effectSettings.m_Intensity,
						m_ParentMesh = effectSettings.m_ParentMesh,
						m_AnimationIndex = effectSettings.m_AnimationIndex
					});
				}
			}
		}
		if (m_AnimationCurves != null && m_AnimationCurves.Count != 0)
		{
			DynamicBuffer<EffectAnimation> buffer2 = entityManager.GetBuffer<EffectAnimation>(entity);
			buffer2.ResizeUninitialized(m_AnimationCurves.Count);
			for (int j = 0; j < m_AnimationCurves.Count; j++)
			{
				AnimationProperties animationProperties = m_AnimationCurves[j];
				buffer2[j] = new EffectAnimation
				{
					m_DurationFrames = (uint)Mathf.RoundToInt(animationProperties.m_Duration * 60f),
					m_AnimationCurve = new AnimationCurve1(animationProperties.m_Curve)
				};
			}
		}
	}
}
