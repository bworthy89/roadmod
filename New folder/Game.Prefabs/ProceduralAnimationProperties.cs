using System;
using System.Collections.Generic;
using Colossal;
using Colossal.IO.AssetDatabase;
using Unity.Entities;
using UnityEngine;

namespace Game.Prefabs;

[ComponentMenu("Rendering/", new Type[] { typeof(RenderPrefab) })]
public class ProceduralAnimationProperties : ComponentBase
{
	[Serializable]
	public class AnimationInfo
	{
		public string name;

		public AssetReference<AnimationAsset> animationAsset;

		public AnimationLayer layer;

		public ClipState state;

		public AnimationPlayback playback;

		public int frameCount;

		public int frameRate;

		public float acceleration;
	}

	[Serializable]
	public class BoneInfo
	{
		public string name;

		public Vector3 position;

		[EulerAngles]
		public Quaternion rotation;

		public Vector3 scale;

		public Matrix4x4 bindPose;

		public int parentId;

		public BoneType m_Type;

		public float m_Speed;

		public float m_Acceleration;

		public int m_ConnectionID;

		public int m_SourceID;
	}

	public BoneInfo[] m_Bones;

	public AnimationInfo[] m_Animations;

	public AnimationAsset GetAnimation(int index)
	{
		return AssetDatabase.global.GetAsset<AnimationAsset>(m_Animations[index].animationAsset);
	}

	public override void GetArchetypeComponents(HashSet<ComponentType> components)
	{
	}

	public override void GetPrefabComponents(HashSet<ComponentType> components)
	{
		components.Add(ComponentType.ReadWrite<ProceduralBone>());
		AnimationInfo[] animations = m_Animations;
		if (animations != null && animations.Length != 0)
		{
			components.Add(ComponentType.ReadWrite<AnimationClip>());
		}
	}
}
