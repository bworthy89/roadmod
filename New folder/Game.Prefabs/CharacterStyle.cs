using System;
using System.Collections.Generic;
using Colossal;
using Colossal.Animations;
using Colossal.IO.AssetDatabase;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Prefabs;

public class CharacterStyle : PrefabBase
{
	[Serializable]
	public class AnimationMotion
	{
		public float3 startOffset;

		public float3 endOffset;

		public quaternion startRotation;

		public quaternion endRotation;
	}

	[Serializable]
	public class AnimationInfo
	{
		public string name;

		public AssetReference<AnimationAsset> animationAsset;

		public RenderPrefab target;

		public Colossal.Animations.AnimationType type;

		public Colossal.Animations.AnimationLayer layer;

		public int frameCount;

		public int frameRate;

		public int rootMotionBone;

		public AnimationMotion[] rootMotion;

		public ActivityType activity;

		public AnimationType state;

		[BitMask]
		public ActivityCondition conditions;

		public AnimationPlayback playback;
	}

	public int m_ShapeCount;

	public int m_BoneCount;

	public GenderMask m_Gender = GenderMask.Any;

	public AnimationInfo[] m_Animations;

	public override bool ignoreUnlockDependencies => true;

	public AnimationAsset GetAnimation(int index)
	{
		return AssetDatabase.global.GetAsset<AnimationAsset>(m_Animations[index].animationAsset);
	}

	public override void GetDependencies(List<PrefabBase> prefabs)
	{
		base.GetDependencies(prefabs);
		for (int i = 0; i < m_Animations.Length; i++)
		{
			AnimationInfo animationInfo = m_Animations[i];
			if (animationInfo.target != null)
			{
				prefabs.Add(animationInfo.target);
			}
		}
	}

	public override void GetPrefabComponents(HashSet<ComponentType> components)
	{
		base.GetPrefabComponents(components);
		components.Add(ComponentType.ReadWrite<CharacterStyleData>());
		components.Add(ComponentType.ReadWrite<AnimationClip>());
		components.Add(ComponentType.ReadWrite<Game.Prefabs.AnimationMotion>());
		components.Add(ComponentType.ReadWrite<RestPoseElement>());
	}

	public void CalculateRootMotion(int[] hierarchy, Animation animation, Animation restPose, int infoIndex)
	{
		int num = ((animation.shapeIndices.Length <= 1) ? 1 : m_ShapeCount);
		int[] array = new int[num];
		if (num > 1)
		{
			for (int i = 0; i < animation.shapeIndices.Length; i++)
			{
				array[animation.shapeIndices[i]] = i;
			}
		}
		var (rootMotionBone, rootMotion) = CalculateRootMotion(hierarchy, animation, restPose, array);
		m_Animations[infoIndex].rootMotionBone = rootMotionBone;
		m_Animations[infoIndex].rootMotion = rootMotion;
	}

	public static (int, AnimationMotion[]) CalculateRootMotion(int[] hierarchy, Animation animation, Animation restPose, int[] shapeIndices)
	{
		int num = shapeIndices.Length;
		int num2 = 0;
		if (animation.layer == Colossal.Animations.AnimationLayer.BodyLayer)
		{
			for (int i = 0; i < animation.boneIndices.Length; i++)
			{
				if (animation.boneIndices[i] == 1)
				{
					num2 = 1;
					break;
				}
			}
		}
		int[] array = new int[hierarchy.Length];
		AnimationMotion[] array2 = new AnimationMotion[num];
		for (int j = 0; j < array.Length; j++)
		{
			array[j] = -1;
		}
		for (int k = 0; k < animation.boneIndices.Length; k++)
		{
			if (animation.boneIndices[k] < array.Length)
			{
				array[animation.boneIndices[k]] = k;
			}
		}
		int num3 = animation.shapeIndices.Length;
		int num4 = animation.boneIndices.Length;
		for (int l = 0; l < num; l++)
		{
			AnimationMotion animationMotion = (array2[l] = new AnimationMotion());
			int num5 = array[num2];
			int num6 = shapeIndices[l];
			Animation.ElementRaw elementRaw;
			Animation.ElementRaw elementRaw2;
			if (num5 >= 0)
			{
				int num7 = num5 * num3;
				int num8 = num7 + (animation.frameCount - 1) * num4 * num3;
				elementRaw = animation.DecodeElement(num7 + num6);
				elementRaw2 = animation.DecodeElement(num8 + num6);
			}
			else
			{
				int num9 = num2 * restPose.shapeIndices.Length;
				elementRaw = restPose.DecodeElement(num9 + l);
				elementRaw2 = elementRaw;
			}
			for (int num10 = hierarchy[num2]; num10 != -1; num10 = hierarchy[num10])
			{
				num5 = array[num10];
				Animation.ElementRaw elementRaw3;
				Animation.ElementRaw elementRaw4;
				if (num5 >= 0)
				{
					int num11 = num5 * num3;
					int num12 = num11 + (animation.frameCount - 1) * num4 * num3;
					elementRaw3 = animation.DecodeElement(num11 + num6);
					elementRaw4 = animation.DecodeElement(num12 + num6);
				}
				else
				{
					int num13 = num10 * restPose.shapeIndices.Length;
					elementRaw3 = restPose.DecodeElement(num13 + l);
					elementRaw4 = elementRaw3;
				}
				elementRaw.position = elementRaw3.position + math.mul(elementRaw3.rotation, elementRaw.position);
				elementRaw.rotation = math.mul((quaternion)elementRaw3.rotation, (quaternion)elementRaw.rotation).value;
				elementRaw2.position = elementRaw4.position + math.mul(elementRaw4.rotation, elementRaw2.position);
				elementRaw2.rotation = math.mul((quaternion)elementRaw4.rotation, (quaternion)elementRaw2.rotation).value;
			}
			animationMotion.startOffset = elementRaw.position;
			animationMotion.endOffset = elementRaw2.position;
			animationMotion.startRotation = math.normalize((quaternion)elementRaw.rotation);
			animationMotion.endRotation = math.normalize((quaternion)elementRaw2.rotation);
		}
		return (num2, array2);
	}
}
