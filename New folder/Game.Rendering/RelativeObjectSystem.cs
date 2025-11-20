using System.Runtime.CompilerServices;
using Colossal.Collections;
using Game.Common;
using Game.Creatures;
using Game.Objects;
using Game.Prefabs;
using Game.Simulation;
using Game.Tools;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine.Scripting;

namespace Game.Rendering;

[CompilerGenerated]
public class RelativeObjectSystem : GameSystemBase
{
	[BurstCompile]
	private struct UpdateRelativeTransformDataJob : IJobParallelForDefer
	{
		[ReadOnly]
		public ComponentLookup<Owner> m_OwnerData;

		[ReadOnly]
		public ComponentLookup<CullingInfo> m_CullingInfoData;

		[ReadOnly]
		public ComponentLookup<Relative> m_RelativeData;

		[ReadOnly]
		public ComponentLookup<Transform> m_TransformData;

		[ReadOnly]
		public ComponentLookup<PseudoRandomSeed> m_PseudoRandomSeedData;

		[ReadOnly]
		public ComponentLookup<Human> m_HumanData;

		[ReadOnly]
		public ComponentLookup<CurrentVehicle> m_CurrentVehicleData;

		[ReadOnly]
		public ComponentLookup<ActivityProp> m_ActivityPropData;

		[ReadOnly]
		public ComponentLookup<PrefabRef> m_PrefabRefData;

		[ReadOnly]
		public ComponentLookup<VehicleData> m_PrefabVehicleData;

		[ReadOnly]
		public ComponentLookup<ActivityPropData> m_PrefabActivityPropData;

		[ReadOnly]
		public ComponentLookup<CreatureData> m_PrefabCreatureData;

		[ReadOnly]
		public BufferLookup<Game.Objects.SubObject> m_SubObjects;

		[ReadOnly]
		public BufferLookup<Bone> m_Bones;

		[ReadOnly]
		public BufferLookup<BoneHistory> m_BoneHistories;

		[ReadOnly]
		public BufferLookup<MeshGroup> m_MeshGroups;

		[ReadOnly]
		public BufferLookup<TransformFrame> m_TransformFrames;

		[ReadOnly]
		public BufferLookup<SubMesh> m_SubMeshes;

		[ReadOnly]
		public BufferLookup<SubMeshGroup> m_SubMeshGroups;

		[ReadOnly]
		public BufferLookup<CharacterElement> m_CharacterElements;

		[ReadOnly]
		public BufferLookup<AnimationClip> m_AnimationClips;

		[ReadOnly]
		public BufferLookup<AnimationMotion> m_AnimationMotions;

		[ReadOnly]
		public BufferLookup<ActivityLocationElement> m_ActivityLocations;

		[NativeDisableParallelForRestriction]
		public ComponentLookup<InterpolatedTransform> m_InterpolatedTransformData;

		[NativeDisableParallelForRestriction]
		public BufferLookup<Animated> m_Animateds;

		[NativeDisableParallelForRestriction]
		public BufferLookup<PlaybackLayer> m_PlaybackLayers;

		[ReadOnly]
		public uint m_FrameIndex;

		[ReadOnly]
		public uint m_PrevFrameIndex;

		[ReadOnly]
		public float m_FrameTime;

		[ReadOnly]
		public float m_FrameDelta;

		[ReadOnly]
		public float4 m_LodParameters;

		[ReadOnly]
		public float3 m_CameraPosition;

		[ReadOnly]
		public float3 m_CameraDirection;

		[ReadOnly]
		public RandomSeed m_RandomSeed;

		[ReadOnly]
		public NativeList<PreCullingData> m_CullingData;

		public AnimatedSystem.AnimationData m_AnimationData;

		public void Execute(int index)
		{
			PreCullingData cullingData = m_CullingData[index];
			if ((cullingData.m_Flags & (PreCullingFlags.NearCamera | PreCullingFlags.Temp | PreCullingFlags.InterpolatedTransform | PreCullingFlags.Relative)) != (PreCullingFlags.NearCamera | PreCullingFlags.InterpolatedTransform | PreCullingFlags.Relative))
			{
				return;
			}
			Entity entity;
			if (m_CurrentVehicleData.TryGetComponent(cullingData.m_Entity, out var componentData))
			{
				entity = componentData.m_Vehicle;
			}
			else
			{
				Owner owner = m_OwnerData[cullingData.m_Entity];
				if (m_RelativeData.HasComponent(owner.m_Owner))
				{
					return;
				}
				entity = owner.m_Owner;
			}
			Transform relativeTransform = GetRelativeTransform(m_RelativeData[cullingData.m_Entity], entity, ref m_BoneHistories, ref m_PrefabRefData, ref m_SubMeshes);
			InterpolatedTransform componentData2;
			Transform componentData3;
			Transform transform = (m_InterpolatedTransformData.TryGetComponent(entity, out componentData2) ? ObjectUtils.LocalToWorld(componentData2.ToTransform(), relativeTransform) : ((!m_TransformData.TryGetComponent(entity, out componentData3)) ? relativeTransform : ObjectUtils.LocalToWorld(componentData3, relativeTransform)));
			Random random = m_RandomSeed.GetRandom(index);
			if ((cullingData.m_Flags & PreCullingFlags.Animated) != 0)
			{
				uint updateFrame;
				uint updateFrame2;
				if (m_ActivityPropData.HasComponent(cullingData.m_Entity))
				{
					float framePosition = 0.5f;
					if (m_CullingInfoData.TryGetComponent(entity, out var componentData4) && componentData4.m_CullingIndex != 0)
					{
						PreCullingData preCullingData = m_CullingData[componentData4.m_CullingIndex];
						ObjectInterpolateSystem.CalculateUpdateFrames(m_FrameIndex, m_PrevFrameIndex, m_FrameTime, (uint)preCullingData.m_UpdateFrame, out updateFrame, out updateFrame2, out framePosition, out var _);
					}
					UpdateActivityPropAnimations(cullingData, transform, entity, framePosition);
				}
				else
				{
					ObjectInterpolateSystem.CalculateUpdateFrames(m_FrameIndex, m_PrevFrameIndex, m_FrameTime, (uint)cullingData.m_UpdateFrame, out updateFrame2, out updateFrame, out var framePosition2, out var updateFrameChanged2);
					float updateFrameToSeconds = 4f / 15f;
					float deltaTime = m_FrameDelta / 60f;
					float speedDeltaFactor = math.select(60f / m_FrameDelta, 0f, m_FrameDelta == 0f);
					UpdateInterpolatedAnimations(cullingData, transform, entity, ref random, framePosition2, updateFrameToSeconds, deltaTime, speedDeltaFactor, updateFrameChanged2);
				}
			}
			else
			{
				m_InterpolatedTransformData[cullingData.m_Entity] = new InterpolatedTransform(transform);
			}
			if (m_SubObjects.TryGetBuffer(cullingData.m_Entity, out var bufferData))
			{
				UpdateTransforms(transform, bufferData);
			}
		}

		private float3 GetVelocity(Entity parent)
		{
			if (m_CullingInfoData.TryGetComponent(parent, out var componentData) && componentData.m_CullingIndex != 0 && m_TransformFrames.TryGetBuffer(parent, out var bufferData))
			{
				PreCullingData preCullingData = m_CullingData[componentData.m_CullingIndex];
				ObjectInterpolateSystem.CalculateUpdateFrames(m_FrameIndex, m_PrevFrameIndex, m_FrameTime, (uint)preCullingData.m_UpdateFrame, out var updateFrame, out var updateFrame2, out var framePosition, out var _);
				TransformFrame transformFrame = bufferData[(int)updateFrame];
				TransformFrame transformFrame2 = bufferData[(int)updateFrame2];
				return math.lerp(transformFrame.m_Velocity, transformFrame2.m_Velocity, framePosition);
			}
			return 0f;
		}

		private void UpdateActivityPropAnimations(PreCullingData cullingData, Transform transform, Entity parent, float framePosition)
		{
			bool reset = (cullingData.m_Flags & PreCullingFlags.NearCameraUpdated) != 0;
			m_InterpolatedTransformData[cullingData.m_Entity] = new InterpolatedTransform(transform);
			CullingInfo cullingInfo = m_CullingInfoData[cullingData.m_Entity];
			PrefabRef prefabRef = m_PrefabRefData[cullingData.m_Entity];
			m_PrefabRefData.TryGetComponent(parent, out var componentData);
			DynamicBuffer<Animated> dynamicBuffer = m_Animateds[cullingData.m_Entity];
			m_Animateds.TryGetBuffer(parent, out var bufferData);
			m_MeshGroups.TryGetBuffer(parent, out var bufferData2);
			ActivityPropData activityPropData = m_PrefabActivityPropData[prefabRef.m_Prefab];
			DynamicBuffer<AnimationClip> clips = m_AnimationClips[prefabRef.m_Prefab];
			m_PrefabCreatureData.TryGetComponent(componentData.m_Prefab, out var componentData2);
			m_CharacterElements.TryGetBuffer(componentData.m_Prefab, out var bufferData3);
			float num = RenderingUtils.CalculateMinDistance(cullingInfo.m_Bounds, m_CameraPosition, m_CameraDirection, m_LodParameters);
			int priority = RenderingUtils.CalculateLod(num * num, m_LodParameters) - cullingInfo.m_MinLod;
			for (int i = 0; i < dynamicBuffer.Length; i++)
			{
				Animated animated = dynamicBuffer[i];
				if (animated.m_ClipIndexBody0 != -1)
				{
					AnimationType animationType = AnimationType.None;
					AnimationType animationType2 = AnimationType.None;
					ActivityType activityType = ActivityType.None;
					ActivityType activityType2 = ActivityType.None;
					if (bufferData.IsCreated && i < bufferData.Length && bufferData3.IsCreated)
					{
						Animated animated2 = bufferData[i];
						CollectionUtils.TryGet(bufferData2, i, out var value);
						CharacterElement characterElement = bufferData3[value.m_SubMeshGroup];
						DynamicBuffer<AnimationClip> dynamicBuffer2 = m_AnimationClips[characterElement.m_Style];
						if (animated2.m_ClipIndexBody0 != -1)
						{
							AnimationClip animationClip = dynamicBuffer2[animated2.m_ClipIndexBody0];
							animationType = animationClip.m_Type;
							activityType = animationClip.m_Activity;
						}
						if (animated2.m_ClipIndexBody1 != -1)
						{
							AnimationClip animationClip2 = dynamicBuffer2[animated2.m_ClipIndexBody1];
							animationType2 = animationClip2.m_Type;
							activityType2 = animationClip2.m_Activity;
						}
						animated.m_Time = animated2.m_Time;
					}
					AnimationClip animationClip3 = clips[animated.m_ClipIndexBody0];
					AnimationClip clip;
					if (animationClip3.m_Type != animationType || animationClip3.m_Activity != activityType)
					{
						if (ObjectInterpolateSystem.FindAnimationClip(clips, animationType, activityType, AnimationLayer.Prop, componentData2.m_Gender, (ActivityCondition)0u, out clip, out var index))
						{
							animated.m_ClipIndexBody0 = (byte)index;
						}
						else
						{
							animated.m_ClipIndexBody0 = -1;
						}
					}
					animationClip3 = ((animated.m_ClipIndexBody1 == -1) ? default(AnimationClip) : clips[animated.m_ClipIndexBody1]);
					if (animationClip3.m_Type != animationType2 || animationClip3.m_Activity != activityType2)
					{
						if (animationType2 != AnimationType.None && ObjectInterpolateSystem.FindAnimationClip(clips, animationType2, activityType2, AnimationLayer.Prop, componentData2.m_Gender, (ActivityCondition)0u, out clip, out var index2))
						{
							animated.m_ClipIndexBody1 = (byte)index2;
						}
						else
						{
							animated.m_ClipIndexBody1 = -1;
						}
					}
					if (animated.m_ClipIndexBody0 == -1)
					{
						if (animated.m_ClipIndexBody1 != -1)
						{
							animated.m_ClipIndexBody0 = animated.m_ClipIndexBody1;
							animated.m_ClipIndexBody1 = -1;
							animated.m_Time.x = animated.m_Time.y;
						}
						else
						{
							animated = dynamicBuffer[i];
						}
					}
					m_AnimationData.SetAnimationFrame(prefabRef.m_Prefab, activityPropData.m_RestPoseClipIndex, -1, clips, in animated, ObjectInterpolateSystem.GetUpdateFrameTransition(framePosition), priority, reset);
				}
				dynamicBuffer[i] = animated;
			}
		}

		private void UpdateInterpolatedAnimations(PreCullingData cullingData, Transform transform, Entity parent, ref Random random, float framePosition, float updateFrameToSeconds, float deltaTime, float speedDeltaFactor, int updateFrameChanged)
		{
			bool flag = (cullingData.m_Flags & PreCullingFlags.NearCameraUpdated) != 0;
			ref InterpolatedTransform valueRW = ref m_InterpolatedTransformData.GetRefRW(cullingData.m_Entity).ValueRW;
			PseudoRandomSeed pseudoRandomSeed = m_PseudoRandomSeedData[cullingData.m_Entity];
			PrefabRef prefabRef = m_PrefabRefData[cullingData.m_Entity];
			DynamicBuffer<Animated> dynamicBuffer = m_Animateds[cullingData.m_Entity];
			TransformState state = TransformState.Idle;
			ActivityType activity = ActivityType.Driving;
			AnimatedPropID propID = AnimatedPropID.None;
			float steerAngle = 0f;
			float3 velocity = default(float3);
			if (m_PrefabRefData.TryGetComponent(parent, out var componentData))
			{
				if (m_ActivityLocations.TryGetBuffer(componentData.m_Prefab, out var bufferData) && bufferData.Length != 0)
				{
					propID = bufferData[0].m_PropID;
				}
				if (m_PrefabVehicleData.TryGetComponent(componentData.m_Prefab, out var componentData2) && componentData2.m_SteeringBoneIndex != -1 && m_Bones.TryGetBuffer(parent, out var bufferData2) && bufferData2.Length > componentData2.m_SteeringBoneIndex)
				{
					steerAngle = math.asin(math.mul(bufferData2[componentData2.m_SteeringBoneIndex].m_Rotation, math.up()).x);
				}
				velocity = GetVelocity(parent);
			}
			DynamicBuffer<MeshGroup> bufferData3 = default(DynamicBuffer<MeshGroup>);
			DynamicBuffer<CharacterElement> bufferData4 = default(DynamicBuffer<CharacterElement>);
			int priority = 0;
			if (m_SubMeshGroups.TryGetBuffer(prefabRef.m_Prefab, out var _))
			{
				m_MeshGroups.TryGetBuffer(cullingData.m_Entity, out bufferData3);
				m_CharacterElements.TryGetBuffer(prefabRef.m_Prefab, out bufferData4);
				valueRW = new InterpolatedTransform(transform);
				CullingInfo cullingInfo = m_CullingInfoData[cullingData.m_Entity];
				float num = RenderingUtils.CalculateMinDistance(cullingInfo.m_Bounds, m_CameraPosition, m_CameraDirection, m_LodParameters);
				priority = RenderingUtils.CalculateLod(num * num, m_LodParameters) - cullingInfo.m_MinLod;
			}
			else
			{
				valueRW = new InterpolatedTransform(transform);
			}
			InterpolatedTransform oldTransform = valueRW;
			for (int i = 0; i < dynamicBuffer.Length; i++)
			{
				Animated animated = dynamicBuffer[i];
				if (animated.m_ClipIndexBody0 != -1 && bufferData4.IsCreated)
				{
					CollectionUtils.TryGet(bufferData3, i, out var value);
					CharacterElement characterElement = bufferData4[value.m_SubMeshGroup];
					DynamicBuffer<AnimationClip> clips = m_AnimationClips[characterElement.m_Style];
					UpdateDrivingAnimationBody(cullingData.m_Entity, in characterElement, clips, ref m_HumanData, ref m_AnimationMotions, oldTransform, valueRW, pseudoRandomSeed, ref animated, ref random, velocity, steerAngle, propID, updateFrameToSeconds, speedDeltaFactor, deltaTime, updateFrameChanged, flag);
					ObjectInterpolateSystem.UpdateInterpolatedAnimationFace(cullingData.m_Entity, clips, ref m_HumanData, ref animated, ref random, state, activity, pseudoRandomSeed, deltaTime, updateFrameChanged, flag);
					m_AnimationData.SetAnimationFrame(characterElement.m_Style, characterElement.m_RestPoseClipIndex, characterElement.m_CorrectiveClipIndex, clips, in animated, ObjectInterpolateSystem.GetUpdateFrameTransition(framePosition), priority, flag);
					UpdateVehicleFrameTime(cullingData.m_Entity, clips[animated.m_ClipIndexBody0], animated.m_Time.x);
				}
				dynamicBuffer[i] = animated;
			}
		}

		private void UpdateVehicleFrameTime(Entity entity, AnimationClip clip, float time)
		{
			float num = 0f;
			if (clip.m_Activity == ActivityType.Driving || clip.m_Activity == ActivityType.Biking)
			{
				num = time;
			}
			if (!m_CurrentVehicleData.TryGetComponent(entity, out var componentData) || !m_PlaybackLayers.TryGetBuffer(componentData.m_Vehicle, out var bufferData) || !bufferData.IsCreated)
			{
				return;
			}
			int length = bufferData.Length;
			for (int i = 0; i < length; i++)
			{
				ref PlaybackLayer reference = ref bufferData.ElementAt(i);
				reference.m_RelativeClipTime = num;
				if (num != 0f)
				{
					reference.m_ClipTime = num / clip.m_AnimationLength;
				}
			}
		}

		private void UpdateTransforms(Transform ownerTransform, DynamicBuffer<Game.Objects.SubObject> subObjects)
		{
			for (int i = 0; i < subObjects.Length; i++)
			{
				Entity subObject = subObjects[i].m_SubObject;
				if (m_InterpolatedTransformData.HasComponent(subObject))
				{
					Transform transform = m_RelativeData[subObject].ToTransform();
					Transform transform2 = ObjectUtils.LocalToWorld(ownerTransform, transform);
					m_InterpolatedTransformData[subObject] = new InterpolatedTransform(transform2);
					if (m_SubObjects.HasBuffer(subObject))
					{
						DynamicBuffer<Game.Objects.SubObject> subObjects2 = m_SubObjects[subObject];
						UpdateTransforms(transform2, subObjects2);
					}
				}
			}
		}
	}

	[BurstCompile]
	private struct UpdateQueryTransformDataJob : IJobChunk
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public SharedComponentTypeHandle<UpdateFrame> m_UpdateFrameType;

		[ReadOnly]
		public ComponentTypeHandle<CullingInfo> m_CullingInfoType;

		[ReadOnly]
		public ComponentTypeHandle<Static> m_StaticType;

		[ReadOnly]
		public ComponentTypeHandle<Temp> m_TempType;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> m_PrefabRefType;

		[ReadOnly]
		public BufferTypeHandle<MeshGroup> m_MeshGroupType;

		[ReadOnly]
		public ComponentLookup<Owner> m_OwnerData;

		[ReadOnly]
		public ComponentLookup<Relative> m_RelativeData;

		[ReadOnly]
		public ComponentLookup<CurrentVehicle> m_CurrentVehicleData;

		[ReadOnly]
		public ComponentLookup<PrefabRef> m_PrefabRefData;

		[ReadOnly]
		public ComponentLookup<ActivityPropData> m_PrefabActivityPropData;

		[ReadOnly]
		public BufferLookup<Game.Objects.SubObject> m_SubObjects;

		[ReadOnly]
		public BufferLookup<BoneHistory> m_BoneHistories;

		[ReadOnly]
		public BufferLookup<CharacterElement> m_CharacterElements;

		[ReadOnly]
		public BufferLookup<AnimationClip> m_AnimationClips;

		[ReadOnly]
		public BufferLookup<SubMesh> m_SubMeshes;

		[NativeDisableParallelForRestriction]
		public ComponentLookup<Transform> m_TransformData;

		[NativeDisableParallelForRestriction]
		public ComponentLookup<InterpolatedTransform> m_InterpolatedTransformData;

		[NativeDisableParallelForRestriction]
		public BufferLookup<Animated> m_Animateds;

		[ReadOnly]
		public uint m_FrameIndex;

		[ReadOnly]
		public float m_FrameTime;

		[ReadOnly]
		public float4 m_LodParameters;

		[ReadOnly]
		public float3 m_CameraPosition;

		[ReadOnly]
		public float3 m_CameraDirection;

		[ReadOnly]
		public NativeList<PreCullingData> m_CullingData;

		public AnimatedSystem.AnimationData m_AnimationData;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			NativeArray<Temp> nativeArray2 = chunk.GetNativeArray(ref m_TempType);
			NativeArray<PrefabRef> nativeArray3 = chunk.GetNativeArray(ref m_PrefabRefType);
			NativeArray<CullingInfo> nativeArray4 = chunk.GetNativeArray(ref m_CullingInfoType);
			BufferAccessor<MeshGroup> bufferAccessor = chunk.GetBufferAccessor(ref m_MeshGroupType);
			bool flag = chunk.Has(ref m_StaticType);
			uint updateFrame = 0u;
			uint updateFrame2 = 0u;
			float framePosition = 0f;
			if (chunk.Has(m_UpdateFrameType))
			{
				uint index = chunk.GetSharedComponent(m_UpdateFrameType).m_Index;
				ObjectInterpolateSystem.CalculateUpdateFrames(m_FrameIndex, m_FrameTime, index, out updateFrame, out updateFrame2, out framePosition);
			}
			for (int i = 0; i < nativeArray.Length; i++)
			{
				Entity entity = nativeArray[i];
				Temp temp = nativeArray2[i];
				CullingInfo cullingInfo = nativeArray4[i];
				if (cullingInfo.m_CullingIndex == 0)
				{
					continue;
				}
				PreCullingData preCullingData = m_CullingData[cullingInfo.m_CullingIndex];
				if ((preCullingData.m_Flags & PreCullingFlags.NearCamera) == 0)
				{
					continue;
				}
				if (m_InterpolatedTransformData.HasComponent(entity))
				{
					if ((flag && (temp.m_Original == Entity.Null || (temp.m_Flags & (TempFlags.Create | TempFlags.Modify)) != 0)) || (temp.m_Flags & TempFlags.Dragging) != 0)
					{
						Entity entity2;
						Owner componentData2;
						if (m_CurrentVehicleData.TryGetComponent(entity, out var componentData))
						{
							entity2 = componentData.m_Vehicle;
						}
						else if (m_OwnerData.TryGetComponent(entity, out componentData2))
						{
							if (m_RelativeData.HasComponent(componentData2.m_Owner))
							{
								continue;
							}
							entity2 = componentData2.m_Owner;
						}
						else
						{
							entity2 = Entity.Null;
						}
						Transform transform = m_TransformData[entity];
						if (entity2 != Entity.Null)
						{
							Transform relativeTransform = GetRelativeTransform(m_RelativeData[entity], entity2, ref m_BoneHistories, ref m_PrefabRefData, ref m_SubMeshes);
							transform = (m_InterpolatedTransformData.TryGetComponent(entity2, out var componentData3) ? ObjectUtils.LocalToWorld(componentData3.ToTransform(), relativeTransform) : ((!m_TransformData.TryGetComponent(entity2, out var componentData4)) ? relativeTransform : ObjectUtils.LocalToWorld(componentData4, relativeTransform)));
						}
						m_InterpolatedTransformData[entity] = new InterpolatedTransform(transform);
						if (m_Animateds.HasBuffer(entity))
						{
							PrefabRef prefabRef = nativeArray3[i];
							DynamicBuffer<Animated> dynamicBuffer = m_Animateds[entity];
							ActivityPropData componentData5 = default(ActivityPropData);
							CollectionUtils.TryGet(bufferAccessor, i, out var value);
							DynamicBuffer<CharacterElement> bufferData;
							bool flag2 = m_CharacterElements.TryGetBuffer(prefabRef.m_Prefab, out bufferData);
							bool flag3 = !flag2 && m_PrefabActivityPropData.TryGetComponent(prefabRef.m_Prefab, out componentData5);
							for (int j = 0; j < dynamicBuffer.Length; j++)
							{
								Animated value2 = dynamicBuffer[j];
								if (value2.m_ClipIndexBody0 != -1)
								{
									value2.m_ClipIndexBody0 = 0;
									value2.m_Time = 0f;
									value2.m_MovementSpeed = 0f;
									value2.m_Interpolation = 0f;
									dynamicBuffer[j] = value2;
								}
								if (value2.m_MetaIndex != 0 && (flag2 || flag3))
								{
									Entity entity3;
									int restPoseClipIndex;
									int correctiveClipIndex;
									if (flag2)
									{
										CollectionUtils.TryGet(value, j, out var value3);
										CharacterElement characterElement = bufferData[value3.m_SubMeshGroup];
										entity3 = characterElement.m_Style;
										restPoseClipIndex = characterElement.m_RestPoseClipIndex;
										correctiveClipIndex = characterElement.m_CorrectiveClipIndex;
									}
									else
									{
										entity3 = prefabRef.m_Prefab;
										restPoseClipIndex = componentData5.m_RestPoseClipIndex;
										correctiveClipIndex = -1;
									}
									Animated animated = new Animated
									{
										m_MetaIndex = value2.m_MetaIndex,
										m_ClipIndexBody0 = -1,
										m_ClipIndexBody0I = -1,
										m_ClipIndexBody1 = -1,
										m_ClipIndexBody1I = -1,
										m_ClipIndexFace0 = -1,
										m_ClipIndexFace1 = -1
									};
									DynamicBuffer<AnimationClip> clips = m_AnimationClips[entity3];
									m_AnimationData.SetAnimationFrame(entity3, restPoseClipIndex, correctiveClipIndex, clips, in animated, 0f, -1, reset: true);
								}
							}
						}
						if (m_SubObjects.TryGetBuffer(entity, out var bufferData2))
						{
							UpdateTransforms(transform, bufferData2);
						}
						continue;
					}
					if (m_TransformData.HasComponent(temp.m_Original))
					{
						Transform transform2 = m_TransformData[temp.m_Original];
						m_TransformData[entity] = transform2;
						if (m_InterpolatedTransformData.HasComponent(temp.m_Original))
						{
							m_InterpolatedTransformData[entity] = m_InterpolatedTransformData[temp.m_Original];
						}
						else
						{
							m_InterpolatedTransformData[entity] = new InterpolatedTransform(transform2);
						}
					}
					else
					{
						m_InterpolatedTransformData[entity] = new InterpolatedTransform(m_TransformData[entity]);
					}
				}
				if (!m_Animateds.HasBuffer(entity))
				{
					continue;
				}
				PrefabRef prefabRef2 = nativeArray3[i];
				DynamicBuffer<Animated> dynamicBuffer2 = m_Animateds[entity];
				bool reset = (preCullingData.m_Flags & (PreCullingFlags.NearCameraUpdated | PreCullingFlags.Updated | PreCullingFlags.BatchesUpdated)) != 0;
				float num = RenderingUtils.CalculateMinDistance(cullingInfo.m_Bounds, m_CameraPosition, m_CameraDirection, m_LodParameters);
				int priority = RenderingUtils.CalculateLod(num * num, m_LodParameters) - cullingInfo.m_MinLod;
				ActivityPropData componentData6 = default(ActivityPropData);
				CollectionUtils.TryGet(bufferAccessor, i, out var value4);
				DynamicBuffer<CharacterElement> bufferData3;
				bool flag4 = m_CharacterElements.TryGetBuffer(prefabRef2.m_Prefab, out bufferData3);
				bool flag5 = !flag4 && m_PrefabActivityPropData.TryGetComponent(prefabRef2.m_Prefab, out componentData6);
				if (m_Animateds.TryGetBuffer(temp.m_Original, out var bufferData4) && bufferData4.Length == dynamicBuffer2.Length)
				{
					for (int k = 0; k < dynamicBuffer2.Length; k++)
					{
						Animated animated2 = dynamicBuffer2[k];
						Animated animated3 = bufferData4[k];
						animated2.m_ClipIndexBody0 = animated3.m_ClipIndexBody0;
						animated2.m_ClipIndexBody0I = animated3.m_ClipIndexBody0I;
						animated2.m_ClipIndexBody1 = animated3.m_ClipIndexBody1;
						animated2.m_ClipIndexBody1I = animated3.m_ClipIndexBody1I;
						animated2.m_ClipIndexFace0 = animated3.m_ClipIndexFace0;
						animated2.m_ClipIndexFace1 = animated3.m_ClipIndexFace1;
						animated2.m_Time = animated3.m_Time;
						animated2.m_MovementSpeed = animated3.m_MovementSpeed;
						animated2.m_Interpolation = animated3.m_Interpolation;
						dynamicBuffer2[k] = animated2;
						if (animated2.m_MetaIndex != 0 && (flag4 || flag5))
						{
							Entity entity4;
							int restPoseClipIndex2;
							int correctiveClipIndex2;
							if (flag4)
							{
								CollectionUtils.TryGet(value4, k, out var value5);
								CharacterElement characterElement2 = bufferData3[value5.m_SubMeshGroup];
								entity4 = characterElement2.m_Style;
								restPoseClipIndex2 = characterElement2.m_RestPoseClipIndex;
								correctiveClipIndex2 = characterElement2.m_CorrectiveClipIndex;
							}
							else
							{
								entity4 = prefabRef2.m_Prefab;
								restPoseClipIndex2 = componentData6.m_RestPoseClipIndex;
								correctiveClipIndex2 = -1;
							}
							float num2 = framePosition * framePosition;
							num2 = 3f * num2 - 2f * num2 * framePosition;
							DynamicBuffer<AnimationClip> clips2 = m_AnimationClips[entity4];
							m_AnimationData.SetAnimationFrame(entity4, restPoseClipIndex2, correctiveClipIndex2, clips2, in animated2, num2, priority, reset);
						}
					}
					continue;
				}
				for (int l = 0; l < dynamicBuffer2.Length; l++)
				{
					Animated value6 = dynamicBuffer2[l];
					if (value6.m_ClipIndexBody0 != -1)
					{
						value6.m_ClipIndexBody0 = 0;
						value6.m_Time = 0f;
						value6.m_MovementSpeed = 0f;
						value6.m_Interpolation = 0f;
						dynamicBuffer2[l] = value6;
					}
					if (value6.m_MetaIndex != 0 && (flag4 || flag5))
					{
						Entity entity5;
						int restPoseClipIndex3;
						int correctiveClipIndex3;
						if (flag4)
						{
							CollectionUtils.TryGet(value4, l, out var value7);
							CharacterElement characterElement3 = bufferData3[value7.m_SubMeshGroup];
							entity5 = characterElement3.m_Style;
							restPoseClipIndex3 = characterElement3.m_RestPoseClipIndex;
							correctiveClipIndex3 = characterElement3.m_CorrectiveClipIndex;
						}
						else
						{
							entity5 = prefabRef2.m_Prefab;
							restPoseClipIndex3 = componentData6.m_RestPoseClipIndex;
							correctiveClipIndex3 = -1;
						}
						Animated animated4 = new Animated
						{
							m_MetaIndex = value6.m_MetaIndex,
							m_ClipIndexBody0 = -1,
							m_ClipIndexBody0I = -1,
							m_ClipIndexBody1 = -1,
							m_ClipIndexBody1I = -1,
							m_ClipIndexFace0 = -1,
							m_ClipIndexFace1 = -1
						};
						DynamicBuffer<AnimationClip> clips3 = m_AnimationClips[entity5];
						m_AnimationData.SetAnimationFrame(entity5, restPoseClipIndex3, correctiveClipIndex3, clips3, in animated4, 0f, -1, reset);
					}
				}
			}
		}

		private void UpdateTransforms(Transform ownerTransform, DynamicBuffer<Game.Objects.SubObject> subObjects)
		{
			for (int i = 0; i < subObjects.Length; i++)
			{
				Entity subObject = subObjects[i].m_SubObject;
				if (m_InterpolatedTransformData.HasComponent(subObject))
				{
					Transform transform = m_RelativeData[subObject].ToTransform();
					Transform transform2 = ObjectUtils.LocalToWorld(ownerTransform, transform);
					m_InterpolatedTransformData[subObject] = new InterpolatedTransform(transform2);
					if (m_SubObjects.HasBuffer(subObject))
					{
						DynamicBuffer<Game.Objects.SubObject> subObjects2 = m_SubObjects[subObject];
						UpdateTransforms(transform2, subObjects2);
					}
				}
			}
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	private struct TypeHandle
	{
		[ReadOnly]
		public ComponentLookup<Owner> __Game_Common_Owner_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<CullingInfo> __Game_Rendering_CullingInfo_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Relative> __Game_Objects_Relative_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Transform> __Game_Objects_Transform_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PseudoRandomSeed> __Game_Common_PseudoRandomSeed_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Human> __Game_Creatures_Human_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<CurrentVehicle> __Game_Creatures_CurrentVehicle_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<ActivityProp> __Game_Creatures_ActivityProp_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<VehicleData> __Game_Prefabs_VehicleData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<ActivityPropData> __Game_Prefabs_ActivityPropData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<CreatureData> __Game_Prefabs_CreatureData_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<Game.Objects.SubObject> __Game_Objects_SubObject_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<Bone> __Game_Rendering_Bone_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<BoneHistory> __Game_Rendering_BoneHistory_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<MeshGroup> __Game_Rendering_MeshGroup_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<TransformFrame> __Game_Objects_TransformFrame_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<SubMesh> __Game_Prefabs_SubMesh_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<SubMeshGroup> __Game_Prefabs_SubMeshGroup_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<CharacterElement> __Game_Prefabs_CharacterElement_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<AnimationClip> __Game_Prefabs_AnimationClip_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<AnimationMotion> __Game_Prefabs_AnimationMotion_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<ActivityLocationElement> __Game_Prefabs_ActivityLocationElement_RO_BufferLookup;

		public BufferLookup<PlaybackLayer> __Game_Rendering_PlaybackLayer_RW_BufferLookup;

		public ComponentLookup<InterpolatedTransform> __Game_Rendering_InterpolatedTransform_RW_ComponentLookup;

		public BufferLookup<Animated> __Game_Rendering_Animated_RW_BufferLookup;

		[ReadOnly]
		public EntityTypeHandle __Unity_Entities_Entity_TypeHandle;

		public SharedComponentTypeHandle<UpdateFrame> __Game_Simulation_UpdateFrame_SharedComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<CullingInfo> __Game_Rendering_CullingInfo_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Static> __Game_Objects_Static_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Temp> __Game_Tools_Temp_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentTypeHandle;

		[ReadOnly]
		public BufferTypeHandle<MeshGroup> __Game_Rendering_MeshGroup_RO_BufferTypeHandle;

		public ComponentLookup<Transform> __Game_Objects_Transform_RW_ComponentLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Game_Common_Owner_RO_ComponentLookup = state.GetComponentLookup<Owner>(isReadOnly: true);
			__Game_Rendering_CullingInfo_RO_ComponentLookup = state.GetComponentLookup<CullingInfo>(isReadOnly: true);
			__Game_Objects_Relative_RO_ComponentLookup = state.GetComponentLookup<Relative>(isReadOnly: true);
			__Game_Objects_Transform_RO_ComponentLookup = state.GetComponentLookup<Transform>(isReadOnly: true);
			__Game_Common_PseudoRandomSeed_RO_ComponentLookup = state.GetComponentLookup<PseudoRandomSeed>(isReadOnly: true);
			__Game_Creatures_Human_RO_ComponentLookup = state.GetComponentLookup<Human>(isReadOnly: true);
			__Game_Creatures_CurrentVehicle_RO_ComponentLookup = state.GetComponentLookup<CurrentVehicle>(isReadOnly: true);
			__Game_Creatures_ActivityProp_RO_ComponentLookup = state.GetComponentLookup<ActivityProp>(isReadOnly: true);
			__Game_Prefabs_PrefabRef_RO_ComponentLookup = state.GetComponentLookup<PrefabRef>(isReadOnly: true);
			__Game_Prefabs_VehicleData_RO_ComponentLookup = state.GetComponentLookup<VehicleData>(isReadOnly: true);
			__Game_Prefabs_ActivityPropData_RO_ComponentLookup = state.GetComponentLookup<ActivityPropData>(isReadOnly: true);
			__Game_Prefabs_CreatureData_RO_ComponentLookup = state.GetComponentLookup<CreatureData>(isReadOnly: true);
			__Game_Objects_SubObject_RO_BufferLookup = state.GetBufferLookup<Game.Objects.SubObject>(isReadOnly: true);
			__Game_Rendering_Bone_RO_BufferLookup = state.GetBufferLookup<Bone>(isReadOnly: true);
			__Game_Rendering_BoneHistory_RO_BufferLookup = state.GetBufferLookup<BoneHistory>(isReadOnly: true);
			__Game_Rendering_MeshGroup_RO_BufferLookup = state.GetBufferLookup<MeshGroup>(isReadOnly: true);
			__Game_Objects_TransformFrame_RO_BufferLookup = state.GetBufferLookup<TransformFrame>(isReadOnly: true);
			__Game_Prefabs_SubMesh_RO_BufferLookup = state.GetBufferLookup<SubMesh>(isReadOnly: true);
			__Game_Prefabs_SubMeshGroup_RO_BufferLookup = state.GetBufferLookup<SubMeshGroup>(isReadOnly: true);
			__Game_Prefabs_CharacterElement_RO_BufferLookup = state.GetBufferLookup<CharacterElement>(isReadOnly: true);
			__Game_Prefabs_AnimationClip_RO_BufferLookup = state.GetBufferLookup<AnimationClip>(isReadOnly: true);
			__Game_Prefabs_AnimationMotion_RO_BufferLookup = state.GetBufferLookup<AnimationMotion>(isReadOnly: true);
			__Game_Prefabs_ActivityLocationElement_RO_BufferLookup = state.GetBufferLookup<ActivityLocationElement>(isReadOnly: true);
			__Game_Rendering_PlaybackLayer_RW_BufferLookup = state.GetBufferLookup<PlaybackLayer>();
			__Game_Rendering_InterpolatedTransform_RW_ComponentLookup = state.GetComponentLookup<InterpolatedTransform>();
			__Game_Rendering_Animated_RW_BufferLookup = state.GetBufferLookup<Animated>();
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
			__Game_Simulation_UpdateFrame_SharedComponentTypeHandle = state.GetSharedComponentTypeHandle<UpdateFrame>();
			__Game_Rendering_CullingInfo_RO_ComponentTypeHandle = state.GetComponentTypeHandle<CullingInfo>(isReadOnly: true);
			__Game_Objects_Static_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Static>(isReadOnly: true);
			__Game_Tools_Temp_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Temp>(isReadOnly: true);
			__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PrefabRef>(isReadOnly: true);
			__Game_Rendering_MeshGroup_RO_BufferTypeHandle = state.GetBufferTypeHandle<MeshGroup>(isReadOnly: true);
			__Game_Objects_Transform_RW_ComponentLookup = state.GetComponentLookup<Transform>();
		}
	}

	private RenderingSystem m_RenderingSystem;

	private PreCullingSystem m_PreCullingSystem;

	private AnimatedSystem m_AnimatedSystem;

	private CameraUpdateSystem m_CameraUpdateSystem;

	private BatchDataSystem m_BatchDataSystem;

	private EntityQuery m_RelativeQuery;

	private EntityQuery m_InterpolateQuery;

	private uint m_PrevFrameIndex;

	private TypeHandle __TypeHandle;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_RenderingSystem = base.World.GetOrCreateSystemManaged<RenderingSystem>();
		m_PreCullingSystem = base.World.GetOrCreateSystemManaged<PreCullingSystem>();
		m_AnimatedSystem = base.World.GetOrCreateSystemManaged<AnimatedSystem>();
		m_CameraUpdateSystem = base.World.GetOrCreateSystemManaged<CameraUpdateSystem>();
		m_BatchDataSystem = base.World.GetOrCreateSystemManaged<BatchDataSystem>();
		m_InterpolateQuery = GetEntityQuery(new EntityQueryDesc
		{
			All = new ComponentType[2]
			{
				ComponentType.ReadOnly<Temp>(),
				ComponentType.ReadOnly<Relative>()
			},
			Any = new ComponentType[1] { ComponentType.ReadWrite<InterpolatedTransform>() },
			None = new ComponentType[1] { ComponentType.ReadOnly<Deleted>() }
		});
	}

	[Preserve]
	protected override void OnUpdate()
	{
		JobHandle dependencies;
		NativeList<PreCullingData> cullingData = m_PreCullingSystem.GetCullingData(readOnly: true, out dependencies);
		JobHandle dependencies2;
		AnimatedSystem.AnimationData animationData = m_AnimatedSystem.GetAnimationData(out dependencies2);
		float3 cameraPosition = default(float3);
		float3 cameraDirection = default(float3);
		float4 lodParameters = default(float4);
		if (m_CameraUpdateSystem.TryGetLODParameters(out var lodParameters2))
		{
			cameraPosition = lodParameters2.cameraPosition;
			IGameCameraController activeCameraController = m_CameraUpdateSystem.activeCameraController;
			lodParameters = RenderingUtils.CalculateLodParameters(m_BatchDataSystem.GetLevelOfDetail(m_RenderingSystem.frameLod, activeCameraController), lodParameters2);
			cameraDirection = m_CameraUpdateSystem.activeViewer.forward;
		}
		UpdateRelativeTransformDataJob jobData = new UpdateRelativeTransformDataJob
		{
			m_OwnerData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Common_Owner_RO_ComponentLookup, ref base.CheckedStateRef),
			m_CullingInfoData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Rendering_CullingInfo_RO_ComponentLookup, ref base.CheckedStateRef),
			m_RelativeData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Relative_RO_ComponentLookup, ref base.CheckedStateRef),
			m_TransformData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Transform_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PseudoRandomSeedData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Common_PseudoRandomSeed_RO_ComponentLookup, ref base.CheckedStateRef),
			m_HumanData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Creatures_Human_RO_ComponentLookup, ref base.CheckedStateRef),
			m_CurrentVehicleData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Creatures_CurrentVehicle_RO_ComponentLookup, ref base.CheckedStateRef),
			m_ActivityPropData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Creatures_ActivityProp_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabRefData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabVehicleData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_VehicleData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabActivityPropData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_ActivityPropData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabCreatureData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_CreatureData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_SubObjects = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Objects_SubObject_RO_BufferLookup, ref base.CheckedStateRef),
			m_Bones = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Rendering_Bone_RO_BufferLookup, ref base.CheckedStateRef),
			m_BoneHistories = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Rendering_BoneHistory_RO_BufferLookup, ref base.CheckedStateRef),
			m_MeshGroups = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Rendering_MeshGroup_RO_BufferLookup, ref base.CheckedStateRef),
			m_TransformFrames = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Objects_TransformFrame_RO_BufferLookup, ref base.CheckedStateRef),
			m_SubMeshes = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Prefabs_SubMesh_RO_BufferLookup, ref base.CheckedStateRef),
			m_SubMeshGroups = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Prefabs_SubMeshGroup_RO_BufferLookup, ref base.CheckedStateRef),
			m_CharacterElements = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Prefabs_CharacterElement_RO_BufferLookup, ref base.CheckedStateRef),
			m_AnimationClips = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Prefabs_AnimationClip_RO_BufferLookup, ref base.CheckedStateRef),
			m_AnimationMotions = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Prefabs_AnimationMotion_RO_BufferLookup, ref base.CheckedStateRef),
			m_ActivityLocations = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Prefabs_ActivityLocationElement_RO_BufferLookup, ref base.CheckedStateRef),
			m_PlaybackLayers = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Rendering_PlaybackLayer_RW_BufferLookup, ref base.CheckedStateRef),
			m_InterpolatedTransformData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Rendering_InterpolatedTransform_RW_ComponentLookup, ref base.CheckedStateRef),
			m_Animateds = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Rendering_Animated_RW_BufferLookup, ref base.CheckedStateRef),
			m_PrevFrameIndex = m_PrevFrameIndex,
			m_FrameIndex = m_RenderingSystem.frameIndex,
			m_FrameTime = m_RenderingSystem.frameTime,
			m_FrameDelta = m_RenderingSystem.frameDelta,
			m_LodParameters = lodParameters,
			m_CameraPosition = cameraPosition,
			m_CameraDirection = cameraDirection,
			m_RandomSeed = RandomSeed.Next(),
			m_CullingData = cullingData,
			m_AnimationData = animationData
		};
		JobHandle jobHandle = JobChunkExtensions.ScheduleParallel(new UpdateQueryTransformDataJob
		{
			m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
			m_UpdateFrameType = InternalCompilerInterface.GetSharedComponentTypeHandle(ref __TypeHandle.__Game_Simulation_UpdateFrame_SharedComponentTypeHandle, ref base.CheckedStateRef),
			m_CullingInfoType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Rendering_CullingInfo_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_StaticType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Objects_Static_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_TempType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Tools_Temp_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_PrefabRefType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_MeshGroupType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Rendering_MeshGroup_RO_BufferTypeHandle, ref base.CheckedStateRef),
			m_OwnerData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Common_Owner_RO_ComponentLookup, ref base.CheckedStateRef),
			m_RelativeData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Relative_RO_ComponentLookup, ref base.CheckedStateRef),
			m_CurrentVehicleData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Creatures_CurrentVehicle_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabRefData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabActivityPropData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_ActivityPropData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_SubObjects = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Objects_SubObject_RO_BufferLookup, ref base.CheckedStateRef),
			m_BoneHistories = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Rendering_BoneHistory_RO_BufferLookup, ref base.CheckedStateRef),
			m_CharacterElements = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Prefabs_CharacterElement_RO_BufferLookup, ref base.CheckedStateRef),
			m_AnimationClips = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Prefabs_AnimationClip_RO_BufferLookup, ref base.CheckedStateRef),
			m_SubMeshes = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Prefabs_SubMesh_RO_BufferLookup, ref base.CheckedStateRef),
			m_TransformData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Transform_RW_ComponentLookup, ref base.CheckedStateRef),
			m_InterpolatedTransformData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Rendering_InterpolatedTransform_RW_ComponentLookup, ref base.CheckedStateRef),
			m_Animateds = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Rendering_Animated_RW_BufferLookup, ref base.CheckedStateRef),
			m_FrameIndex = m_RenderingSystem.frameIndex,
			m_FrameTime = m_RenderingSystem.frameTime,
			m_LodParameters = lodParameters,
			m_CameraPosition = cameraPosition,
			m_CameraDirection = cameraDirection,
			m_CullingData = cullingData,
			m_AnimationData = animationData
		}, dependsOn: jobData.Schedule(cullingData, 16, JobHandle.CombineDependencies(base.Dependency, dependencies, dependencies2)), query: m_InterpolateQuery);
		m_PreCullingSystem.AddCullingDataReader(jobHandle);
		m_AnimatedSystem.AddAnimationWriter(jobHandle);
		base.Dependency = jobHandle;
		m_PrevFrameIndex = m_RenderingSystem.frameIndex;
	}

	private static Transform GetRelativeTransform(Relative relative, Entity parent, ref BufferLookup<BoneHistory> boneHistoryLookup, ref ComponentLookup<PrefabRef> prefabRefLookup, ref BufferLookup<SubMesh> subMeshLookup)
	{
		if (relative.m_BoneIndex.y >= 0)
		{
			DynamicBuffer<BoneHistory> dynamicBuffer = boneHistoryLookup[parent];
			if (dynamicBuffer.Length > relative.m_BoneIndex.y)
			{
				float4x4 matrix = dynamicBuffer[relative.m_BoneIndex.y].m_Matrix;
				float3 @float = math.transform(matrix, relative.m_Position);
				float3 forward = math.rotate(matrix, math.forward(relative.m_Rotation));
				float3 up = math.rotate(matrix, math.mul(relative.m_Rotation, math.up()));
				quaternion quaternion = quaternion.LookRotation(forward, up);
				if (relative.m_BoneIndex.z >= 0)
				{
					SubMesh subMesh = subMeshLookup[prefabRefLookup[parent].m_Prefab][relative.m_BoneIndex.z];
					@float = subMesh.m_Position + math.rotate(subMesh.m_Rotation, @float);
					quaternion = math.mul(subMesh.m_Rotation, quaternion);
				}
				return new Transform(@float, quaternion);
			}
		}
		return new Transform(relative.m_Position, relative.m_Rotation);
	}

	public static void UpdateDrivingAnimationBody(Entity entity, in CharacterElement characterElement, DynamicBuffer<AnimationClip> clips, ref ComponentLookup<Human> humanLookup, ref BufferLookup<AnimationMotion> motionLookup, InterpolatedTransform oldTransform, InterpolatedTransform newTransform, PseudoRandomSeed pseudoRandomSeed, ref Animated animated, ref Random random, float3 velocity, float steerAngle, AnimatedPropID propID, float updateFrameToSeconds, float speedDeltaFactor, float deltaTime, int updateFrameChanged, bool instantReset)
	{
		AnimationClip clip = default(AnimationClip);
		AnimationClip clipI = default(AnimationClip);
		AnimationClip clip2 = default(AnimationClip);
		AnimationClip clipI2 = default(AnimationClip);
		if (!instantReset)
		{
			clip = clips[animated.m_ClipIndexBody0];
			if (animated.m_ClipIndexBody0I != -1)
			{
				clipI = clips[animated.m_ClipIndexBody0I];
			}
			if (animated.m_ClipIndexBody1 != -1)
			{
				clip2 = clips[animated.m_ClipIndexBody1];
			}
			if (animated.m_ClipIndexBody1I != -1)
			{
				clipI2 = clips[animated.m_ClipIndexBody1I];
			}
		}
		float3 y = math.forward(newTransform.m_Rotation);
		ActivityType activityType = ((!(math.dot(velocity, y) >= 1f)) ? ActivityType.Standing : ActivityType.Driving);
		if (clip.m_Activity == ActivityType.Driving)
		{
			if (clip2.m_Activity == ActivityType.Driving)
			{
				if (activityType == ActivityType.Standing)
				{
					clip2.m_Activity = ActivityType.Standing;
				}
				else
				{
					clip.m_Activity = ActivityType.Standing;
				}
			}
			else if (clip2.m_Activity == ActivityType.None && activityType == ActivityType.Standing && clip.m_Type != AnimationType.Idle)
			{
				clip.m_Activity = ActivityType.Standing;
			}
		}
		bool flag = updateFrameChanged > 0 && ((clip.m_Activity != ActivityType.None && (clip.m_Activity != activityType || clip.m_Type == AnimationType.Start || clip.m_PropID != propID)) || (clip2.m_Activity != ActivityType.None && (clip2.m_Activity != activityType || clip2.m_Type == AnimationType.Start || clip2.m_PropID != propID)));
		if (flag && clip2.m_Type != AnimationType.None)
		{
			animated.m_ClipIndexBody0 = animated.m_ClipIndexBody1;
			animated.m_ClipIndexBody0I = animated.m_ClipIndexBody1I;
			animated.m_ClipIndexBody1 = -1;
			animated.m_ClipIndexBody1I = -1;
			animated.m_Time.x = animated.m_Time.y;
			animated.m_Time.y = 0f;
			animated.m_MovementSpeed.x = animated.m_MovementSpeed.y;
			animated.m_MovementSpeed.y = 0f;
			clip = clip2;
			clipI = clipI2;
			clip2 = default(AnimationClip);
			clipI2 = default(AnimationClip);
			flag &= clip.m_Activity != activityType;
		}
		if (clip.m_Activity == ActivityType.None || ((clip.m_Activity == ActivityType.Driving || clip.m_Activity == ActivityType.Standing) && clip.m_Type != AnimationType.Start && clip.m_PropID == propID))
		{
			bool num = clip.m_Type == AnimationType.None;
			UpdateDrivingClips(targetActivity: num ? activityType : clip.m_Activity, entity: entity, clip: ref clip, clipI: ref clipI, clipIndex: ref animated.m_ClipIndexBody0, clipIndexI: ref animated.m_ClipIndexBody0I, movementSpeed: ref animated.m_MovementSpeed.x, interpolation: ref animated.m_Interpolation, clips: clips, humanLookup: ref humanLookup, steerAngle: steerAngle, pseudoRandomSeed: pseudoRandomSeed, propID: propID);
			if (num)
			{
				animated.m_Time.x = random.NextFloat(clip.m_AnimationLength);
			}
		}
		if (flag || ((clip2.m_Activity == ActivityType.Driving || clip2.m_Activity == ActivityType.Standing) && clip2.m_Type != AnimationType.Start && clip2.m_PropID == propID))
		{
			bool num2 = clip2.m_Type == AnimationType.None;
			UpdateDrivingClips(targetActivity: num2 ? activityType : clip2.m_Activity, entity: entity, clip: ref clip2, clipI: ref clipI2, clipIndex: ref animated.m_ClipIndexBody1, clipIndexI: ref animated.m_ClipIndexBody1I, movementSpeed: ref animated.m_MovementSpeed.y, interpolation: ref animated.m_Interpolation, clips: clips, humanLookup: ref humanLookup, steerAngle: steerAngle, pseudoRandomSeed: pseudoRandomSeed, propID: propID);
			if (num2)
			{
				if ((clip.m_Activity == ActivityType.Driving || clip.m_Activity == ActivityType.Standing) && clip.m_Type != AnimationType.Start && clip.m_PropID == propID)
				{
					animated.m_Time.y = animated.m_Time.x;
				}
				else
				{
					animated.m_Time.y = random.NextFloat(clip2.m_AnimationLength);
				}
			}
		}
		if (animated.m_ClipIndexBody1 != -1 && animated.m_MovementSpeed.y == 0f)
		{
			animated.m_Time.y += deltaTime;
		}
		if (animated.m_MovementSpeed.x == 0f)
		{
			animated.m_Time.x += deltaTime;
		}
	}

	public static void UpdateDrivingClips(Entity entity, ref AnimationClip clip, ref AnimationClip clipI, ref short clipIndex, ref short clipIndexI, ref float movementSpeed, ref float interpolation, DynamicBuffer<AnimationClip> clips, ref ComponentLookup<Human> humanLookup, float steerAngle, PseudoRandomSeed pseudoRandomSeed, AnimatedPropID propID, ActivityType targetActivity)
	{
		float num = math.abs(steerAngle);
		float num2 = math.radians(1f);
		if (targetActivity == ActivityType.Driving && num > num2)
		{
			AnimationType animationType = ((steerAngle > 0f) ? AnimationType.RightMin : AnimationType.LeftMin);
			if (clipI.m_Type != animationType || clipI.m_Activity != ActivityType.Driving || clipI.m_PropID != propID)
			{
				ActivityCondition activityConditions = ObjectInterpolateSystem.GetActivityConditions(entity, ref humanLookup);
				ObjectInterpolateSystem.FindAnimationClip(clips, animationType, ActivityType.Driving, AnimationLayer.Body, pseudoRandomSeed, propID, activityConditions, out clipI, out var index);
				clipIndexI = (short)index;
			}
			float targetRotation = GetTargetRotation(in clipI, math.radians(10f), num2);
			AnimationType animationType2 = AnimationType.Idle;
			ActivityType activity = targetActivity;
			if (num > targetRotation)
			{
				animationType2 = ((steerAngle > 0f) ? AnimationType.RightMax : AnimationType.LeftMax);
				activity = ActivityType.Driving;
			}
			if (clip.m_Type != animationType2 || clip.m_Activity != targetActivity || clip.m_PropID != propID)
			{
				ActivityCondition activityConditions2 = ObjectInterpolateSystem.GetActivityConditions(entity, ref humanLookup);
				ObjectInterpolateSystem.FindAnimationClip(clips, animationType2, activity, AnimationLayer.Body, pseudoRandomSeed, propID, activityConditions2, out clip, out var index2);
				clipIndex = (short)index2;
				movementSpeed = 0f;
			}
			if (num > targetRotation)
			{
				float targetRotation2 = GetTargetRotation(in clip, math.radians(45f), targetRotation);
				interpolation = math.saturate(1f - (num - targetRotation) / (targetRotation2 - targetRotation));
			}
			else
			{
				interpolation = math.saturate((num - num2) / (targetRotation - num2));
			}
		}
		else
		{
			if (clip.m_Type != AnimationType.Idle || clip.m_Activity != targetActivity || clip.m_PropID != propID)
			{
				ActivityCondition activityConditions3 = ObjectInterpolateSystem.GetActivityConditions(entity, ref humanLookup);
				ObjectInterpolateSystem.FindAnimationClip(clips, AnimationType.Idle, targetActivity, AnimationLayer.Body, pseudoRandomSeed, propID, activityConditions3, out clip, out var index3);
				clipIndex = (short)index3;
				movementSpeed = 0f;
			}
			interpolation = 0f;
			clipIndexI = -1;
			clipI = default(AnimationClip);
		}
	}

	public static float GetTargetRotation(in AnimationClip clip, float def, float prev)
	{
		return math.max(math.select(clip.m_TargetValue, def, clip.m_TargetValue == float.MinValue), prev + math.radians(1f));
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
	public RelativeObjectSystem()
	{
	}
}
