using System.Runtime.CompilerServices;
using Colossal.Animations;
using Colossal.Collections;
using Colossal.Mathematics;
using Game.Common;
using Game.Rendering;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Mathematics;
using UnityEngine.Scripting;

namespace Game.Prefabs;

[CompilerGenerated]
public class AnimatedPrefabSystem : GameSystemBase
{
	private struct TypeHandle
	{
		[ReadOnly]
		public EntityTypeHandle __Unity_Entities_Entity_TypeHandle;

		public ComponentTypeHandle<CharacterStyleData> __Game_Prefabs_CharacterStyleData_RW_ComponentTypeHandle;

		public ComponentTypeHandle<ActivityPropData> __Game_Prefabs_ActivityPropData_RW_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<PrefabData> __Game_Prefabs_PrefabData_RO_ComponentTypeHandle;

		public BufferTypeHandle<AnimationClip> __Game_Prefabs_AnimationClip_RW_BufferTypeHandle;

		public BufferTypeHandle<AnimationMotion> __Game_Prefabs_AnimationMotion_RW_BufferTypeHandle;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
			__Game_Prefabs_CharacterStyleData_RW_ComponentTypeHandle = state.GetComponentTypeHandle<CharacterStyleData>();
			__Game_Prefabs_ActivityPropData_RW_ComponentTypeHandle = state.GetComponentTypeHandle<ActivityPropData>();
			__Game_Prefabs_PrefabData_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PrefabData>(isReadOnly: true);
			__Game_Prefabs_AnimationClip_RW_BufferTypeHandle = state.GetBufferTypeHandle<AnimationClip>();
			__Game_Prefabs_AnimationMotion_RW_BufferTypeHandle = state.GetBufferTypeHandle<AnimationMotion>();
		}
	}

	private PrefabSystem m_PrefabSystem;

	private AnimatedSystem m_AnimatedSystem;

	private EntityQuery m_PrefabQuery;

	private TypeHandle __TypeHandle;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_PrefabSystem = base.World.GetOrCreateSystemManaged<PrefabSystem>();
		m_AnimatedSystem = base.World.GetOrCreateSystemManaged<AnimatedSystem>();
		m_PrefabQuery = GetEntityQuery(new EntityQueryDesc
		{
			All = new ComponentType[2]
			{
				ComponentType.ReadOnly<Created>(),
				ComponentType.ReadOnly<PrefabData>()
			},
			Any = new ComponentType[2]
			{
				ComponentType.ReadWrite<CharacterStyleData>(),
				ComponentType.ReadWrite<ActivityPropData>()
			}
		});
		RequireForUpdate(m_PrefabQuery);
	}

	[Preserve]
	protected override void OnUpdate()
	{
		NativeArray<ArchetypeChunk> nativeArray = m_PrefabQuery.ToArchetypeChunkArray(Allocator.Temp);
		EntityTypeHandle entityTypeHandle = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef);
		ComponentTypeHandle<CharacterStyleData> typeHandle = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_CharacterStyleData_RW_ComponentTypeHandle, ref base.CheckedStateRef);
		ComponentTypeHandle<ActivityPropData> typeHandle2 = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_ActivityPropData_RW_ComponentTypeHandle, ref base.CheckedStateRef);
		CompleteDependency();
		for (int i = 0; i < nativeArray.Length; i++)
		{
			ArchetypeChunk chunk = nativeArray[i];
			if (chunk.Has(ref typeHandle))
			{
				CharacterStyles(chunk, entityTypeHandle);
			}
			else if (chunk.Has(ref typeHandle2))
			{
				ActivityPropPrefabs(chunk, entityTypeHandle);
			}
		}
		nativeArray.Dispose();
	}

	private void CharacterStyles(ArchetypeChunk chunk, EntityTypeHandle entityType)
	{
		ComponentTypeHandle<PrefabData> typeHandle = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_PrefabData_RO_ComponentTypeHandle, ref base.CheckedStateRef);
		ComponentTypeHandle<CharacterStyleData> typeHandle2 = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_CharacterStyleData_RW_ComponentTypeHandle, ref base.CheckedStateRef);
		BufferTypeHandle<AnimationClip> bufferTypeHandle = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Prefabs_AnimationClip_RW_BufferTypeHandle, ref base.CheckedStateRef);
		BufferTypeHandle<AnimationMotion> bufferTypeHandle2 = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Prefabs_AnimationMotion_RW_BufferTypeHandle, ref base.CheckedStateRef);
		NativeArray<Entity> nativeArray = chunk.GetNativeArray(entityType);
		NativeArray<PrefabData> nativeArray2 = chunk.GetNativeArray(ref typeHandle);
		NativeArray<CharacterStyleData> nativeArray3 = chunk.GetNativeArray(ref typeHandle2);
		BufferAccessor<AnimationClip> bufferAccessor = chunk.GetBufferAccessor(ref bufferTypeHandle);
		BufferAccessor<AnimationMotion> bufferAccessor2 = chunk.GetBufferAccessor(ref bufferTypeHandle2);
		for (int i = 0; i < nativeArray.Length; i++)
		{
			CharacterStyle prefab = m_PrefabSystem.GetPrefab<CharacterStyle>(nativeArray2[i]);
			ref CharacterStyleData reference = ref nativeArray3.ElementAt(i);
			DynamicBuffer<AnimationClip> dynamicBuffer = bufferAccessor[i];
			DynamicBuffer<AnimationMotion> dynamicBuffer2 = bufferAccessor2[i];
			reference.m_ActivityMask = default(ActivityMask);
			reference.m_RestPoseClipIndex = -1;
			int num = prefab.m_Animations.Length;
			int num2 = 0;
			dynamicBuffer.ResizeUninitialized(num);
			for (int j = 0; j < num; j++)
			{
				CharacterStyle.AnimationInfo animationInfo = prefab.m_Animations[j];
				ref AnimationClip reference2 = ref dynamicBuffer.ElementAt(j);
				reference2 = default(AnimationClip);
				reference2.m_InfoIndex = -1;
				reference2.m_RootMotionBone = animationInfo.rootMotionBone;
				switch (animationInfo.layer)
				{
				case Colossal.Animations.AnimationLayer.BodyLayer:
					reference2.m_Layer = AnimationLayer.Body;
					break;
				case Colossal.Animations.AnimationLayer.PropLayer:
					reference2.m_Layer = AnimationLayer.Prop;
					break;
				case Colossal.Animations.AnimationLayer.FacialLayer:
					reference2.m_Layer = AnimationLayer.Facial;
					break;
				case Colossal.Animations.AnimationLayer.CorrectiveLayer:
					reference2.m_Layer = AnimationLayer.Corrective;
					break;
				default:
					reference2.m_Layer = AnimationLayer.None;
					break;
				}
				if (animationInfo.rootMotion != null)
				{
					num2 += animationInfo.rootMotion.Length;
				}
				if (animationInfo.type == Colossal.Animations.AnimationType.RestPose && animationInfo.target == null)
				{
					reference.m_RestPoseClipIndex = j;
				}
				if (animationInfo.target != null && animationInfo.target.TryGet<CharacterProperties>(out var component))
				{
					reference2.m_PropID = m_AnimatedSystem.GetPropID(component.m_AnimatedPropName);
				}
				else
				{
					reference2.m_PropID = m_AnimatedSystem.GetPropID(null);
				}
			}
			dynamicBuffer2.ResizeUninitialized(num2);
			num2 = 0;
			float num3 = float.MaxValue;
			float num4 = 0f;
			for (int k = 0; k < num; k++)
			{
				CharacterStyle.AnimationInfo animationInfo2 = prefab.m_Animations[k];
				ref AnimationClip reference3 = ref dynamicBuffer.ElementAt(k);
				reference3.m_Type = animationInfo2.state;
				reference3.m_Activity = animationInfo2.activity;
				reference3.m_Conditions = animationInfo2.conditions;
				reference3.m_Playback = animationInfo2.playback;
				reference3.m_Gender = prefab.m_Gender;
				reference3.m_TargetValue = float.MinValue;
				reference3.m_VariationCount = 1;
				for (int l = 0; l < k; l++)
				{
					ref AnimationClip reference4 = ref dynamicBuffer.ElementAt(l);
					if (reference3.m_Activity == reference4.m_Activity && reference3.m_Type == reference4.m_Type && reference3.m_Conditions == reference4.m_Conditions && reference3.m_Layer == reference4.m_Layer)
					{
						reference3.m_VariationIndex++;
						reference3.m_VariationCount++;
						reference4.m_VariationCount++;
					}
				}
				if (reference3.m_Playback == AnimationPlayback.RandomLoop || reference3.m_Type == AnimationType.Move || reference3.m_Playback == AnimationPlayback.SyncToRelative)
				{
					reference3.m_AnimationLength = (float)animationInfo2.frameCount / (float)animationInfo2.frameRate;
					reference3.m_FrameRate = animationInfo2.frameRate;
				}
				else
				{
					float num5 = (float)(animationInfo2.frameCount - 1) * (60f / (float)animationInfo2.frameRate);
					num5 = math.max(1f, math.round(num5 / 16f)) * 16f;
					reference3.m_AnimationLength = num5 * (1f / 60f);
					reference3.m_FrameRate = (float)math.max(1, animationInfo2.frameCount - 1) / reference3.m_AnimationLength;
					reference3.m_AnimationLength -= 0.001f;
				}
				if (animationInfo2.rootMotion != null)
				{
					NativeArray<AnimationMotion> subArray = dynamicBuffer2.AsNativeArray().GetSubArray(num2, animationInfo2.rootMotion.Length);
					CleanUpRootMotion(animationInfo2.rootMotion, subArray, animationInfo2.activity);
					reference3.m_MotionRange = new int2(num2, num2 + animationInfo2.rootMotion.Length);
					num2 += animationInfo2.rootMotion.Length;
					if (reference3.m_Type == AnimationType.Move)
					{
						AnimationMotion animationMotion = subArray[0];
						reference3.m_MovementSpeed = math.length(animationMotion.m_EndOffset - animationMotion.m_StartOffset) * reference3.m_FrameRate / (float)math.max(1, animationInfo2.frameCount - 1);
						if ((double)reference3.m_MovementSpeed < 0.001)
						{
							reference3.m_MovementSpeed = (float)math.max(1, animationInfo2.frameCount - 1) / reference3.m_FrameRate * 3.6f;
						}
						if (reference3.m_Conditions == (ActivityCondition)0u)
						{
							switch (reference3.m_Activity)
							{
							case ActivityType.Walking:
								num3 = reference3.m_MovementSpeed;
								break;
							case ActivityType.Running:
								num4 = reference3.m_MovementSpeed;
								break;
							}
						}
					}
				}
				else
				{
					reference3.m_RootMotionBone = -1;
				}
				reference.m_ActivityMask.m_Mask |= new ActivityMask(reference3.m_Activity).m_Mask;
				reference.m_AnimationLayerMask.m_Mask |= new AnimationLayerMask(reference3.m_Layer).m_Mask;
			}
			for (int m = 0; m < dynamicBuffer.Length; m++)
			{
				ref AnimationClip reference5 = ref dynamicBuffer.ElementAt(m);
				if (reference5.m_Layer == AnimationLayer.Body && reference5.m_Type == AnimationType.Move)
				{
					reference5.m_SpeedRange = new Bounds1(0f, float.MaxValue);
					switch (reference5.m_Activity)
					{
					case ActivityType.Walking:
						reference5.m_SpeedRange.max = math.select((num3 + num4) * 0.5f, float.MaxValue, num4 <= num3);
						break;
					case ActivityType.Running:
						reference5.m_SpeedRange.min = math.select((num3 + num4) * 0.5f, 0f, num3 >= num4);
						break;
					}
				}
			}
			reference.m_BoneCount = prefab.m_BoneCount;
			reference.m_ShapeCount = prefab.m_ShapeCount;
		}
	}

	private void ActivityPropPrefabs(ArchetypeChunk chunk, EntityTypeHandle entityType)
	{
		ComponentTypeHandle<PrefabData> typeHandle = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_PrefabData_RO_ComponentTypeHandle, ref base.CheckedStateRef);
		ComponentTypeHandle<ActivityPropData> typeHandle2 = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_ActivityPropData_RW_ComponentTypeHandle, ref base.CheckedStateRef);
		BufferTypeHandle<AnimationClip> bufferTypeHandle = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Prefabs_AnimationClip_RW_BufferTypeHandle, ref base.CheckedStateRef);
		BufferTypeHandle<AnimationMotion> bufferTypeHandle2 = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Prefabs_AnimationMotion_RW_BufferTypeHandle, ref base.CheckedStateRef);
		NativeArray<Entity> nativeArray = chunk.GetNativeArray(entityType);
		NativeArray<PrefabData> nativeArray2 = chunk.GetNativeArray(ref typeHandle);
		NativeArray<ActivityPropData> nativeArray3 = chunk.GetNativeArray(ref typeHandle2);
		BufferAccessor<AnimationClip> bufferAccessor = chunk.GetBufferAccessor(ref bufferTypeHandle);
		BufferAccessor<AnimationMotion> bufferAccessor2 = chunk.GetBufferAccessor(ref bufferTypeHandle2);
		for (int i = 0; i < nativeArray.Length; i++)
		{
			ActivityPropPrefab prefab = m_PrefabSystem.GetPrefab<ActivityPropPrefab>(nativeArray2[i]);
			ref ActivityPropData reference = ref nativeArray3.ElementAt(i);
			DynamicBuffer<AnimationClip> dynamicBuffer = bufferAccessor[i];
			DynamicBuffer<AnimationMotion> dynamicBuffer2 = bufferAccessor2[i];
			reference.m_ActivityMask = default(ActivityMask);
			reference.m_RestPoseClipIndex = -1;
			int num = prefab.m_Animations.Length;
			int num2 = 0;
			dynamicBuffer.ResizeUninitialized(num);
			for (int j = 0; j < num; j++)
			{
				ActivityPropPrefab.AnimationInfo animationInfo = prefab.m_Animations[j];
				ref AnimationClip reference2 = ref dynamicBuffer.ElementAt(j);
				reference2 = default(AnimationClip);
				reference2.m_InfoIndex = -1;
				reference2.m_RootMotionBone = animationInfo.rootMotionBone;
				reference2.m_Layer = AnimationLayer.Prop;
				reference2.m_VariationCount = 1;
				if (animationInfo.rootMotion != null)
				{
					num2 += animationInfo.rootMotion.Length;
				}
				if (animationInfo.type == Colossal.Animations.AnimationType.RestPose)
				{
					reference.m_RestPoseClipIndex = j;
				}
				m_AnimatedSystem.AddPropClip(reference.m_AnimatedPropID, animationInfo.activity, animationInfo.state, animationInfo.gender, nativeArray[i], j);
			}
			dynamicBuffer2.ResizeUninitialized(num2);
			num2 = 0;
			float num3 = float.MaxValue;
			float num4 = 0f;
			for (int k = 0; k < num; k++)
			{
				ActivityPropPrefab.AnimationInfo animationInfo2 = prefab.m_Animations[k];
				ref AnimationClip reference3 = ref dynamicBuffer.ElementAt(k);
				reference3.m_Type = animationInfo2.state;
				reference3.m_Activity = animationInfo2.activity;
				reference3.m_Conditions = animationInfo2.conditions;
				reference3.m_Playback = animationInfo2.playback;
				reference3.m_Gender = animationInfo2.gender;
				reference3.m_TargetValue = float.MinValue;
				if (reference3.m_Playback == AnimationPlayback.RandomLoop || reference3.m_Type == AnimationType.Move)
				{
					reference3.m_AnimationLength = (float)animationInfo2.frameCount / (float)animationInfo2.frameRate;
					reference3.m_FrameRate = animationInfo2.frameRate;
				}
				else
				{
					float num5 = (float)(animationInfo2.frameCount - 1) * (60f / (float)animationInfo2.frameRate);
					num5 = math.max(1f, math.round(num5 / 16f)) * 16f;
					reference3.m_AnimationLength = num5 * (1f / 60f);
					reference3.m_FrameRate = (float)math.max(1, animationInfo2.frameCount - 1) / reference3.m_AnimationLength;
					reference3.m_AnimationLength -= 0.001f;
				}
				if (animationInfo2.rootMotion != null)
				{
					NativeArray<AnimationMotion> subArray = dynamicBuffer2.AsNativeArray().GetSubArray(num2, animationInfo2.rootMotion.Length);
					CleanUpRootMotion(animationInfo2.rootMotion, subArray, animationInfo2.activity);
					reference3.m_MotionRange = new int2(num2, num2 + animationInfo2.rootMotion.Length);
					num2 += animationInfo2.rootMotion.Length;
					if (reference3.m_Type == AnimationType.Move)
					{
						AnimationMotion animationMotion = subArray[0];
						reference3.m_MovementSpeed = math.length(animationMotion.m_EndOffset - animationMotion.m_StartOffset) * reference3.m_FrameRate / (float)math.max(1, animationInfo2.frameCount - 1);
						if ((double)reference3.m_MovementSpeed < 0.001)
						{
							reference3.m_MovementSpeed = (float)math.max(1, animationInfo2.frameCount - 1) / reference3.m_FrameRate * 3.6f;
						}
						if (reference3.m_Conditions == (ActivityCondition)0u)
						{
							switch (reference3.m_Activity)
							{
							case ActivityType.Walking:
								num3 = reference3.m_MovementSpeed;
								break;
							case ActivityType.Running:
								num4 = reference3.m_MovementSpeed;
								break;
							}
						}
					}
				}
				else
				{
					reference3.m_RootMotionBone = -1;
				}
				reference.m_ActivityMask.m_Mask |= new ActivityMask(reference3.m_Activity).m_Mask;
				reference.m_AnimationLayerMask.m_Mask |= new AnimationLayerMask(reference3.m_Layer).m_Mask;
			}
			for (int l = 0; l < dynamicBuffer.Length; l++)
			{
				ref AnimationClip reference4 = ref dynamicBuffer.ElementAt(l);
				if (reference4.m_Layer == AnimationLayer.Body && reference4.m_Type == AnimationType.Move)
				{
					reference4.m_SpeedRange = new Bounds1(0f, float.MaxValue);
					switch (reference4.m_Activity)
					{
					case ActivityType.Walking:
						reference4.m_SpeedRange.max = math.select((num3 + num4) * 0.5f, float.MaxValue, num4 <= num3);
						break;
					case ActivityType.Running:
						reference4.m_SpeedRange.min = math.select((num3 + num4) * 0.5f, 0f, num3 >= num4);
						break;
					}
				}
			}
			reference.m_BoneCount = prefab.m_BoneCount;
			reference.m_ShapeCount = 1;
		}
	}

	public static void CleanUpRootMotion(CharacterStyle.AnimationMotion[] source, NativeArray<AnimationMotion> target, ActivityType activityType)
	{
		for (int i = 0; i < source.Length; i++)
		{
			CharacterStyle.AnimationMotion animationMotion = source[i];
			ref AnimationMotion reference = ref target.ElementAt(i);
			reference.m_StartOffset = animationMotion.startOffset;
			reference.m_EndOffset = animationMotion.endOffset;
			reference.m_StartRotation = animationMotion.startRotation;
			reference.m_EndRotation = animationMotion.endRotation;
			if (i != 0)
			{
				ref AnimationMotion reference2 = ref target.ElementAt(0);
				reference.m_StartOffset -= reference2.m_StartOffset;
				reference.m_StartRotation = math.mul(reference.m_StartRotation, math.inverse(reference2.m_StartRotation));
				reference.m_EndOffset -= reference2.m_EndOffset;
				reference.m_EndRotation = math.mul(reference.m_EndRotation, math.inverse(reference2.m_EndRotation));
			}
			if (activityType != ActivityType.Flying)
			{
				reference.m_StartOffset.y = 0f;
				reference.m_EndOffset.y = 0f;
			}
			float3 forward = math.forward(reference.m_StartRotation);
			float3 forward2 = math.forward(reference.m_EndRotation);
			forward.y = 0f;
			forward2.y = 0f;
			reference.m_StartRotation = quaternion.LookRotationSafe(forward, math.up());
			reference.m_EndRotation = quaternion.LookRotationSafe(forward2, math.up());
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private void __AssignQueries(ref SystemState state)
	{
		new EntityQueryBuilder(Allocator.Temp).Dispose();
	}

	protected override void OnCreateForCompiler()
	{
		base.OnCreateForCompiler();
		__AssignQueries(ref base.CheckedStateRef);
		__TypeHandle.__AssignHandles(ref base.CheckedStateRef);
	}

	[Preserve]
	public AnimatedPrefabSystem()
	{
	}
}
