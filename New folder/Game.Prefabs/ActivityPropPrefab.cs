using System;
using System.Collections.Generic;
using Colossal;
using Colossal.Animations;
using Colossal.IO.AssetDatabase;
using Game.Creatures;
using Game.Rendering;
using Unity.Entities;

namespace Game.Prefabs;

public class ActivityPropPrefab : StaticObjectPrefab
{
	[Serializable]
	public class AnimationInfo
	{
		public string name;

		public AssetReference<AnimationAsset> animationAsset;

		public Colossal.Animations.AnimationType type;

		public int frameCount;

		public int frameRate;

		public int rootMotionBone;

		public CharacterStyle.AnimationMotion[] rootMotion;

		public GenderMask gender = GenderMask.Any;

		public ActivityType activity;

		public AnimationType state;

		[BitMask]
		public ActivityCondition conditions;

		public AnimationPlayback playback;
	}

	public int m_BoneCount;

	public AnimationInfo[] m_Animations;

	public AnimationAsset GetAnimation(int index)
	{
		return AssetDatabase.global.GetAsset<AnimationAsset>(m_Animations[index].animationAsset);
	}

	public override void GetPrefabComponents(HashSet<ComponentType> components)
	{
		base.GetPrefabComponents(components);
		components.Add(ComponentType.ReadWrite<ActivityPropData>());
		components.Add(ComponentType.ReadWrite<AnimationClip>());
		components.Add(ComponentType.ReadWrite<AnimationMotion>());
		components.Add(ComponentType.ReadWrite<RestPoseElement>());
	}

	public override void GetArchetypeComponents(HashSet<ComponentType> components)
	{
		base.GetArchetypeComponents(components);
		components.Add(ComponentType.ReadWrite<ActivityProp>());
	}

	public override void Initialize(EntityManager entityManager, Entity entity)
	{
		base.Initialize(entityManager, entity);
		ActivityPropData componentData = new ActivityPropData
		{
			m_BoneCount = m_BoneCount,
			m_AnimatedPropID = AnimatedPropID.None
		};
		if (m_Meshes != null)
		{
			AnimatedSystem orCreateSystemManaged = entityManager.World.GetOrCreateSystemManaged<AnimatedSystem>();
			for (int i = 0; i < m_Meshes.Length; i++)
			{
				RenderPrefabBase mesh = m_Meshes[i].m_Mesh;
				if (!(mesh == null))
				{
					CharacterProperties component = mesh.GetComponent<CharacterProperties>();
					if (component != null)
					{
						componentData.m_AnimatedPropID = orCreateSystemManaged.GetPropID(component.m_AnimatedPropName);
						break;
					}
				}
			}
		}
		entityManager.SetComponentData(entity, componentData);
	}

	public void CalculateRootMotion(int[] hierarchy, Animation animation, Animation restPose, int infoIndex)
	{
		int[] shapeIndices = new int[1];
		var (rootMotionBone, rootMotion) = CharacterStyle.CalculateRootMotion(hierarchy, animation, restPose, shapeIndices);
		m_Animations[infoIndex].rootMotionBone = rootMotionBone;
		m_Animations[infoIndex].rootMotion = rootMotion;
	}
}
