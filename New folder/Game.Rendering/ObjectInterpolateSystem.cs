using System;
using System.Runtime.CompilerServices;
using Colossal.Collections;
using Colossal.Entities;
using Colossal.Mathematics;
using Game.Buildings;
using Game.Common;
using Game.Creatures;
using Game.Effects;
using Game.Net;
using Game.Notifications;
using Game.Objects;
using Game.Prefabs;
using Game.Simulation;
using Game.Tools;
using Game.Vehicles;
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
public class ObjectInterpolateSystem : GameSystemBase
{
	[BurstCompile]
	private struct UpdateTransformDataJob : IJobParallelForDefer
	{
		[ReadOnly]
		public ComponentLookup<Owner> m_OwnerData;

		[ReadOnly]
		public ComponentLookup<PseudoRandomSeed> m_PseudoRandomSeedData;

		[ReadOnly]
		public ComponentLookup<CullingInfo> m_CullingInfoData;

		[ReadOnly]
		public ComponentLookup<PointOfInterest> m_PointOfInterestData;

		[ReadOnly]
		public ComponentLookup<Destroyed> m_DestroyedData;

		[ReadOnly]
		public ComponentLookup<Attachment> m_AttachmentData;

		[ReadOnly]
		public ComponentLookup<Transform> m_TransformData;

		[ReadOnly]
		public ComponentLookup<TrafficLight> m_TrafficLightData;

		[ReadOnly]
		public BufferLookup<Efficiency> m_BuildingEfficiencyData;

		[ReadOnly]
		public ComponentLookup<ElectricityConsumer> m_BuildingElectricityConsumer;

		[ReadOnly]
		public ComponentLookup<Game.Buildings.ExtractorFacility> m_BuildingExtractorFacility;

		[ReadOnly]
		public ComponentLookup<Building> m_BuildingData;

		[ReadOnly]
		public ComponentLookup<Vehicle> m_VehicleData;

		[ReadOnly]
		public ComponentLookup<ParkedCar> m_ParkedCarData;

		[ReadOnly]
		public ComponentLookup<ParkedTrain> m_ParkedTrainData;

		[ReadOnly]
		public ComponentLookup<Car> m_CarData;

		[ReadOnly]
		public ComponentLookup<CarTrailer> m_CarTrailerData;

		[ReadOnly]
		public ComponentLookup<Controller> m_ControllerData;

		[ReadOnly]
		public ComponentLookup<Watercraft> m_WatercraftData;

		[ReadOnly]
		public ComponentLookup<Human> m_HumanData;

		[ReadOnly]
		public ComponentLookup<CurrentVehicle> m_CurrentVehicleData;

		[ReadOnly]
		public ComponentLookup<Curve> m_CurveData;

		[ReadOnly]
		public ComponentLookup<Moving> m_MovingData;

		[ReadOnly]
		public ComponentLookup<PrefabRef> m_PrefabRefData;

		[ReadOnly]
		public ComponentLookup<SwayingData> m_PrefabSwayingData;

		[ReadOnly]
		public ComponentLookup<ObjectGeometryData> m_PrefabObjectGeometryData;

		[ReadOnly]
		public ComponentLookup<UtilityLaneData> m_PrefabUtilityLaneData;

		[ReadOnly]
		public ComponentLookup<CarData> m_PrefabCarData;

		[ReadOnly]
		public BufferLookup<TransformFrame> m_TransformFrames;

		[ReadOnly]
		public BufferLookup<MeshGroup> m_MeshGroups;

		[ReadOnly]
		public BufferLookup<EnabledEffect> m_EffectInstances;

		[ReadOnly]
		public BufferLookup<AnimationClip> m_AnimationClips;

		[ReadOnly]
		public BufferLookup<AnimationMotion> m_AnimationMotions;

		[ReadOnly]
		public BufferLookup<ProceduralBone> m_ProceduralBones;

		[ReadOnly]
		public BufferLookup<ProceduralLight> m_ProceduralLights;

		[ReadOnly]
		public BufferLookup<LightAnimation> m_LightAnimations;

		[ReadOnly]
		public BufferLookup<SubMesh> m_SubMeshes;

		[ReadOnly]
		public BufferLookup<SubMeshGroup> m_SubMeshGroups;

		[ReadOnly]
		public BufferLookup<CharacterElement> m_CharacterElements;

		[ReadOnly]
		public BufferLookup<ActivityLocationElement> m_ActivityLocations;

		[ReadOnly]
		public BufferLookup<Passenger> m_Passengers;

		[NativeDisableParallelForRestriction]
		public ComponentLookup<InterpolatedTransform> m_InterpolatedTransformData;

		[NativeDisableParallelForRestriction]
		public ComponentLookup<Swaying> m_SwayingData;

		[NativeDisableParallelForRestriction]
		public BufferLookup<Skeleton> m_Skeletons;

		[NativeDisableParallelForRestriction]
		public BufferLookup<Emissive> m_Emissives;

		[NativeDisableParallelForRestriction]
		public BufferLookup<Animated> m_Animateds;

		[NativeDisableParallelForRestriction]
		public BufferLookup<Bone> m_Bones;

		[NativeDisableParallelForRestriction]
		public BufferLookup<Momentum> m_Momentums;

		[NativeDisableParallelForRestriction]
		public BufferLookup<PlaybackLayer> m_PlaybackLayers;

		[NativeDisableParallelForRestriction]
		public BufferLookup<LightState> m_Lights;

		[ReadOnly]
		public uint m_FrameIndex;

		[ReadOnly]
		public uint m_PrevFrameIndex;

		[ReadOnly]
		public float m_FrameTime;

		[ReadOnly]
		public float m_FrameDelta;

		[ReadOnly]
		public float m_TimeOfDay;

		[ReadOnly]
		public float4 m_LodParameters;

		[ReadOnly]
		public float3 m_CameraPosition;

		[ReadOnly]
		public float3 m_CameraDirection;

		[ReadOnly]
		public RandomSeed m_RandomSeed;

		[ReadOnly]
		public CellMapData<Wind> m_WindData;

		[ReadOnly]
		public NativeQuadTree<Entity, QuadTreeBoundsXZ> m_LaneSearchTree;

		[ReadOnly]
		public NativeList<PreCullingData> m_CullingData;

		[ReadOnly]
		public NativeList<EnabledEffectData> m_EnabledData;

		[ReadOnly]
		public WaterSurfaceData<SurfaceWater> m_WaterSurfaceData;

		[ReadOnly]
		public WaterSurfaceData<SurfaceWater> m_WaterVelocityData;

		[ReadOnly]
		public WaterRenderSurfaceData m_WaterRenderSurfaceData;

		[ReadOnly]
		public TerrainHeightData m_TerrainHeightData;

		public NativeQueue<WaterHeightRequest>.ParallelWriter m_requestedWaterHeightsWriter;

		public AnimatedSystem.AnimationData m_AnimationData;

		public void Execute(int index)
		{
			PreCullingData cullingData = m_CullingData[index];
			if ((cullingData.m_Flags & (PreCullingFlags.InterpolatedTransform | PreCullingFlags.Animated | PreCullingFlags.Skeleton | PreCullingFlags.Emissive)) != 0 && (cullingData.m_Flags & (PreCullingFlags.NearCamera | PreCullingFlags.Temp | PreCullingFlags.Relative)) == PreCullingFlags.NearCamera)
			{
				Unity.Mathematics.Random random = m_RandomSeed.GetRandom(index);
				if (m_TransformFrames.TryGetBuffer(cullingData.m_Entity, out var bufferData))
				{
					UpdateInterpolatedTransforms(cullingData, bufferData, ref random);
				}
				else
				{
					UpdateStaticAnimations(cullingData, ref random);
				}
			}
		}

		private void UpdateStaticAnimations(PreCullingData cullingData, ref Unity.Mathematics.Random random)
		{
			float num = m_FrameDelta / 60f;
			float speedDeltaFactor = math.select(60f / m_FrameDelta, 0f, m_FrameDelta == 0f);
			bool flag = (cullingData.m_Flags & PreCullingFlags.NearCameraUpdated) != 0;
			Transform transform = m_TransformData[cullingData.m_Entity];
			PrefabRef prefabRef = m_PrefabRefData[cullingData.m_Entity];
			if ((cullingData.m_Flags & PreCullingFlags.InterpolatedTransform) != 0)
			{
				ref InterpolatedTransform valueRW = ref m_InterpolatedTransformData.GetRefRW(cullingData.m_Entity).ValueRW;
				if (m_DestroyedData.TryGetComponent(cullingData.m_Entity, out var componentData) && m_PrefabObjectGeometryData.TryGetComponent(prefabRef.m_Prefab, out var componentData2))
				{
					quaternion q = m_PseudoRandomSeedData[cullingData.m_Entity].GetRandom(PseudoRandomSeed.kCollapse).NextQuaternionRotation();
					float collapseTime = BuildingUtils.GetCollapseTime(componentData2.m_Size.y);
					float num2 = ((!flag) ? BuildingUtils.GetCollapseTime(transform.m_Position.y - valueRW.m_Position.y) : (collapseTime + componentData.m_Cleared));
					num2 = math.max(0f, num2 + num);
					valueRW.m_Position = transform.m_Position;
					valueRW.m_Position.y -= BuildingUtils.GetCollapseHeight(num2);
					valueRW.m_Rotation = math.slerp(transform.m_Rotation, q, num2 / (10f + collapseTime * 10f));
				}
				else if (m_SwayingData.HasComponent(cullingData.m_Entity) && m_PrefabObjectGeometryData.TryGetComponent(prefabRef.m_Prefab, out componentData2))
				{
					ref Swaying valueRW2 = ref m_SwayingData.GetRefRW(cullingData.m_Entity).ValueRW;
					InterpolatedTransform oldTransform = valueRW;
					valueRW.m_Position = transform.m_Position;
					valueRW.m_Rotation = transform.m_Rotation;
					UpdateWaterSwaying(componentData2, cullingData.m_Entity.Index, default(float3), oldTransform, ref valueRW, ref valueRW2, num, speedDeltaFactor, 0.9f, flag);
				}
				else
				{
					valueRW = new InterpolatedTransform(transform);
				}
			}
			if ((cullingData.m_Flags & (PreCullingFlags.Skeleton | PreCullingFlags.Emissive)) == 0)
			{
				return;
			}
			Entity owner = Entity.Null;
			if (m_OwnerData.TryGetComponent(cullingData.m_Entity, out var componentData3))
			{
				owner = componentData3.m_Owner;
			}
			float2 wind = float.NaN;
			float2 efficiency = -1f;
			float num3 = -1f;
			if (m_VehicleData.HasComponent(cullingData.m_Entity))
			{
				efficiency = math.select(1f, 0f, m_ParkedCarData.HasComponent(cullingData.m_Entity) || m_ParkedTrainData.HasComponent(cullingData.m_Entity));
				num3 = math.select(1f, 0f, m_DestroyedData.HasComponent(cullingData.m_Entity));
			}
			else
			{
				if (m_BuildingEfficiencyData.TryGetBuffer(cullingData.m_Entity, out var bufferData))
				{
					efficiency = GetEfficiency(bufferData);
				}
				num3 = ((!m_BuildingElectricityConsumer.TryGetComponent(cullingData.m_Entity, out var componentData4)) ? efficiency.x : math.select(0f, 1f, componentData4.electricityConnected));
			}
			float working = -1f;
			Game.Objects.TrafficLightState trafficLightState = Game.Objects.TrafficLightState.None;
			if (m_TrafficLightData.TryGetComponent(cullingData.m_Entity, out var componentData5))
			{
				trafficLightState = componentData5.m_State;
			}
			DynamicBuffer<SubMesh> dynamicBuffer = m_SubMeshes[prefabRef.m_Prefab];
			if ((cullingData.m_Flags & PreCullingFlags.Skeleton) != 0)
			{
				DynamicBuffer<Skeleton> dynamicBuffer2 = m_Skeletons[cullingData.m_Entity];
				DynamicBuffer<Bone> bones = m_Bones[cullingData.m_Entity];
				m_Momentums.TryGetBuffer(cullingData.m_Entity, out var bufferData2);
				int priority = 0;
				int i = 0;
				if (m_PlaybackLayers.TryGetBuffer(cullingData.m_Entity, out var bufferData3))
				{
					CullingInfo cullingInfo = m_CullingInfoData[cullingData.m_Entity];
					float num4 = RenderingUtils.CalculateMinDistance(cullingInfo.m_Bounds, m_CameraPosition, m_CameraDirection, m_LodParameters);
					priority = RenderingUtils.CalculateLod(num4 * num4, m_LodParameters) - cullingInfo.m_MinLod;
				}
				for (int j = 0; j < dynamicBuffer2.Length; j++)
				{
					ref Skeleton reference = ref dynamicBuffer2.ElementAt(j);
					if (reference.m_BufferAllocation.Empty)
					{
						continue;
					}
					SubMesh subMesh = dynamicBuffer[j];
					DynamicBuffer<ProceduralBone> proceduralBones = m_ProceduralBones[subMesh.m_SubMesh];
					Transform transform2 = transform;
					if ((subMesh.m_Flags & SubMeshFlags.HasTransform) != 0)
					{
						transform2 = ObjectUtils.LocalToWorld(transform, subMesh.m_Position, subMesh.m_Rotation);
					}
					DynamicBuffer<AnimationClip> bufferData4 = default(DynamicBuffer<AnimationClip>);
					if (bufferData3.IsCreated)
					{
						int num5 = ((j != dynamicBuffer2.Length - 1) ? dynamicBuffer2[j + 1].m_LayerOffset : bufferData3.Length);
						if (m_AnimationClips.TryGetBuffer(subMesh.m_SubMesh, out bufferData4))
						{
							for (; i < num5; i++)
							{
								ref PlaybackLayer playbackLayer = ref bufferData3.ElementAt(i);
								AnimateStaticLayer(bufferData4, ref playbackLayer, num, cullingData.m_Entity, owner, flag, ref random, ref efficiency);
								m_AnimationData.RequireAnimation(subMesh.m_SubMesh, bufferData4, in playbackLayer, priority);
							}
						}
						i = num5;
					}
					for (int k = 0; k < proceduralBones.Length; k++)
					{
						AnimateStaticBone(proceduralBones, bufferData4, bones, bufferData2, bufferData3, transform2, prefabRef, ref reference, k, num, cullingData.m_Entity, owner, flag, trafficLightState, ref random, ref wind, ref efficiency, ref num3, ref working);
					}
				}
			}
			if ((cullingData.m_Flags & PreCullingFlags.Emissive) == 0)
			{
				return;
			}
			PseudoRandomSeed pseudoRandomSeed = m_PseudoRandomSeedData[cullingData.m_Entity];
			DynamicBuffer<Emissive> dynamicBuffer3 = m_Emissives[cullingData.m_Entity];
			DynamicBuffer<LightState> lights = m_Lights[cullingData.m_Entity];
			m_EffectInstances.TryGetBuffer(cullingData.m_Entity, out var bufferData5);
			CarFlags carFlags = (CarFlags)0u;
			if (m_CarData.TryGetComponent(cullingData.m_Entity, out var componentData6))
			{
				carFlags = componentData6.m_Flags;
			}
			Unity.Mathematics.Random random2 = pseudoRandomSeed.GetRandom(PseudoRandomSeed.kLightState);
			bool isBuildingActive = false;
			if (m_BuildingData.TryGetComponent(cullingData.m_Entity, out var componentData7))
			{
				isBuildingActive = !BuildingUtils.CheckOption(componentData7, BuildingOption.Inactive);
			}
			for (int l = 0; l < dynamicBuffer3.Length; l++)
			{
				ref Emissive reference2 = ref dynamicBuffer3.ElementAt(l);
				if (!reference2.m_BufferAllocation.Empty)
				{
					SubMesh subMesh2 = dynamicBuffer[l];
					DynamicBuffer<ProceduralLight> proceduralLights = m_ProceduralLights[subMesh2.m_SubMesh];
					m_LightAnimations.TryGetBuffer(subMesh2.m_SubMesh, out var bufferData6);
					for (int m = 0; m < proceduralLights.Length; m++)
					{
						AnimateStaticLight(proceduralLights, bufferData6, lights, isBuildingActive, ref reference2, m, num, owner, flag, random2, trafficLightState, carFlags, bufferData5, ref num3);
					}
				}
			}
		}

		private void UpdateWaterSwaying(ObjectGeometryData objectGeometryData, int entityID, float3 frameVelocity, InterpolatedTransform oldTransform, ref InterpolatedTransform newTransform, ref Swaying swaying, float deltaTime, float speedDeltaFactor, float inertiaFactor, bool instantReset)
		{
			float3 @float = MathUtils.Size(objectGeometryData.m_Bounds);
			float3 float2 = math.max(0.01f, @float * @float);
			float2.xz = 12f / (float2.yy + float2.xz);
			float2.y = 1f;
			float2 *= inertiaFactor;
			@float *= 0.5f;
			SwayingData swayingData = new SwayingData
			{
				m_VelocityFactors = float2 * new float3(@float.y, 1f, @float.y),
				m_DampingFactors = 0.02f,
				m_MaxPosition = new float3(MathF.PI / 2f, 1000f, MathF.PI / 2f),
				m_SpringFactors = float2 * new float3(@float.x, 1f, @float.z) * 50f
			};
			float2 float3 = WaterUtils.SampleVelocity(ref m_WaterVelocityData, newTransform.m_Position);
			float3 /= math.max(1f, math.cmax(@float));
			float waterDepth;
			float num2;
			if (math.any(@float.xz >= 0.2f))
			{
				float2 float4 = objectGeometryData.m_Size.xz * 0.4f;
				float3 float5 = newTransform.m_Position + math.rotate(newTransform.m_Rotation, new float3(0f - float4.x, 0f, 0f - float4.y));
				float3 float6 = newTransform.m_Position + math.rotate(newTransform.m_Rotation, new float3(float4.x, 0f, 0f - float4.y));
				float3 float7 = newTransform.m_Position + math.rotate(newTransform.m_Rotation, new float3(0f - float4.x, 0f, float4.y));
				float3 float8 = newTransform.m_Position + math.rotate(newTransform.m_Rotation, new float3(float4.x, 0f, float4.y));
				m_requestedWaterHeightsWriter.Enqueue(new WaterHeightRequest(entityID, 0, float5));
				m_requestedWaterHeightsWriter.Enqueue(new WaterHeightRequest(entityID, 1, float6));
				m_requestedWaterHeightsWriter.Enqueue(new WaterHeightRequest(entityID, 2, float7));
				m_requestedWaterHeightsWriter.Enqueue(new WaterHeightRequest(entityID, 3, float8));
				float4 float9 = default(float4);
				float4 x = default(float4);
				WaterUtils.SampleHeight(ref m_WaterSurfaceData, ref m_TerrainHeightData, float5, out float9.x, out x.x, out waterDepth);
				WaterUtils.SampleHeight(ref m_WaterSurfaceData, ref m_TerrainHeightData, float6, out float9.y, out x.y, out waterDepth);
				WaterUtils.SampleHeight(ref m_WaterSurfaceData, ref m_TerrainHeightData, float7, out float9.z, out x.z, out waterDepth);
				WaterUtils.SampleHeight(ref m_WaterSurfaceData, ref m_TerrainHeightData, float8, out float9.w, out x.w, out waterDepth);
				if (m_WaterRenderSurfaceData.GetWaterRenderHeight(entityID, 0, out var height))
				{
					x.x = height;
				}
				if (m_WaterRenderSurfaceData.GetWaterRenderHeight(entityID, 1, out height))
				{
					x.y = height;
				}
				if (m_WaterRenderSurfaceData.GetWaterRenderHeight(entityID, 2, out height))
				{
					x.z = height;
				}
				if (m_WaterRenderSurfaceData.GetWaterRenderHeight(entityID, 3, out height))
				{
					x.w = height;
				}
				float num = math.max(0f, objectGeometryData.m_Bounds.min.y * -0.75f);
				x = math.max(x, float9 + num);
				num2 = math.csum(x) * 0.25f;
				x -= new float4(float5.y, float6.y, float7.y, float8.y);
				float3 v = new float3
				{
					xz = x.yz + x.ww - x.xx - x.zy
				};
				v.xz *= swayingData.m_SpringFactors.xz / swayingData.m_VelocityFactors.xz * (MathF.PI / 80f);
				float3 -= math.mul(newTransform.m_Rotation, v).xz;
			}
			else
			{
				m_requestedWaterHeightsWriter.Enqueue(new WaterHeightRequest(entityID, 0, newTransform.m_Position));
				WaterUtils.SampleHeight(ref m_WaterSurfaceData, ref m_TerrainHeightData, newTransform.m_Position, out var terrainHeight, out num2, out waterDepth);
				if (m_WaterRenderSurfaceData.GetWaterRenderHeight(entityID, 0, out var height2))
				{
					num2 = height2;
				}
				float num3 = math.max(0f, objectGeometryData.m_Bounds.min.y * -0.75f);
				num2 = math.max(num2, terrainHeight + num3);
			}
			swaying.m_LastVelocity.xz += float3 * deltaTime;
			float num4 = ((float)(m_FrameIndex & 0xFFFF) + m_FrameTime) * (1f / 60f);
			float num5 = math.sqrt(math.max(1f, math.length(@float)));
			num2 += noise.pnoise(new float3(num4, newTransform.m_Position.z, newTransform.m_Position.x) * num5, 1092.2667f) * 0.25f;
			float3.x += noise.pnoise(new float3(newTransform.m_Position.x, num4, newTransform.m_Position.z) * num5, 1092.2667f);
			float3.y += noise.pnoise(new float3(newTransform.m_Position.z, newTransform.m_Position.x, num4) * num5, 1092.2667f);
			if (instantReset)
			{
				newTransform.m_Position.y = num2;
				swaying.m_LastVelocity = frameVelocity - new float3(float3.x, 0f, float3.y);
				swaying.m_SwayVelocity = 0f;
				swaying.m_SwayPosition = 0f;
			}
			else
			{
				newTransform.m_Position.y = num2;
				oldTransform.m_Position.xz += float3 * deltaTime;
				swaying.m_SwayPosition.y = oldTransform.m_Position.y - num2;
				UpdateSwaying(swayingData, oldTransform, ref newTransform, ref swaying, deltaTime, speedDeltaFactor, localSway: false, out var _, out waterDepth);
			}
		}

		private void AnimateLayer(DynamicBuffer<AnimationClip> animationClips, ref PlaybackLayer playbackLayer, float deltaTime, Entity entity, bool instantReset, ref Unity.Mathematics.Random random)
		{
			int num = -1;
			int num2 = -1;
			float num3 = 0f;
			AnimationClip animationClip;
			for (int i = 0; i < animationClips.Length; i++)
			{
				animationClip = animationClips[i];
				if (animationClip.m_Layer - 5 != (AnimationLayer)playbackLayer.m_LayerIndex)
				{
					continue;
				}
				int num4 = 0;
				float num5 = 1f;
				if (animationClip.m_ClipState == ClipState.Driving)
				{
					Moving componentData;
					if (animationClip.m_Playback == AnimationPlayback.SyncToRelative)
					{
						num4 = math.select(0, 1, playbackLayer.m_RelativeClipTime != 0f);
						num5 = math.select(0, 2, playbackLayer.m_RelativeClipTime != 0f);
					}
					else if (m_MovingData.TryGetComponent(entity, out componentData))
					{
						bool test = !componentData.m_Velocity.Equals(float3.zero);
						num4 = math.select(0, 1, test);
						num5 = math.select(0, 1, test);
					}
					else
					{
						num4 = 0;
						num5 = 0f;
					}
				}
				if (num4 > num2)
				{
					num = i;
					num2 = num4;
					num3 = num5;
				}
			}
			animationClip = default(AnimationClip);
			if (num >= 0)
			{
				animationClip = animationClips[num];
			}
			if (instantReset)
			{
				playbackLayer.m_ClipIndex = (short)num;
				if (animationClip.m_Playback == AnimationPlayback.SyncToRelative)
				{
					if (playbackLayer.m_RelativeClipTime != 0f)
					{
						playbackLayer.m_ClipTime = playbackLayer.m_RelativeClipTime;
					}
				}
				else
				{
					playbackLayer.m_ClipTime = random.NextFloat(animationClip.m_AnimationLength);
				}
				playbackLayer.m_PlaySpeed = num3;
			}
			else if (playbackLayer.m_ClipIndex != num)
			{
				playbackLayer.m_ClipIndex = (short)num;
				switch (animationClip.m_Playback)
				{
				case AnimationPlayback.RandomLoop:
					playbackLayer.m_ClipTime = random.NextFloat(animationClip.m_AnimationLength);
					break;
				case AnimationPlayback.HalfLoop:
					playbackLayer.m_ClipTime = math.select(0f, animationClip.m_AnimationLength * 0.5f, random.NextBool());
					break;
				case AnimationPlayback.SyncToRelative:
					if (playbackLayer.m_RelativeClipTime != 0f)
					{
						playbackLayer.m_ClipTime = playbackLayer.m_RelativeClipTime;
					}
					break;
				default:
					playbackLayer.m_ClipTime = 0f;
					break;
				}
				if (animationClip.m_Acceleration != 0f)
				{
					playbackLayer.m_PlaySpeed = math.min(math.abs(animationClip.m_Acceleration * deltaTime), num3);
				}
				else
				{
					playbackLayer.m_PlaySpeed = num3;
				}
			}
			else
			{
				if (playbackLayer.m_ClipIndex < 0)
				{
					return;
				}
				playbackLayer.m_ClipTime += playbackLayer.m_PlaySpeed * deltaTime;
				if (animationClip.m_Acceleration != 0f)
				{
					float num6 = math.abs(animationClip.m_Acceleration * deltaTime);
					if (num3 > playbackLayer.m_PlaySpeed)
					{
						playbackLayer.m_PlaySpeed = math.min(playbackLayer.m_PlaySpeed + num6, num3);
					}
					else
					{
						playbackLayer.m_PlaySpeed = math.max(playbackLayer.m_PlaySpeed - num6, num3);
					}
				}
				else
				{
					playbackLayer.m_PlaySpeed = num3;
				}
			}
		}

		private void AnimateInterpolatedBone(DynamicBuffer<ProceduralBone> proceduralBones, DynamicBuffer<AnimationClip> animationClips, DynamicBuffer<Bone> bones, DynamicBuffer<Momentum> momentums, DynamicBuffer<PlaybackLayer> playbackLayers, InterpolatedTransform oldTransform, InterpolatedTransform newTransform, PrefabRef prefabRef, ref Skeleton skeleton, quaternion swayRotation, float swayOffset, float steeringRadius, float pivotOffset, int index, float deltaTime, Entity entity, bool instantReset, ref Unity.Mathematics.Random random)
		{
			ProceduralBone proceduralBone = proceduralBones[index];
			Momentum momentum = default(Momentum);
			int index2 = skeleton.m_BoneOffset + index;
			ref Bone reference = ref bones.ElementAt(index2);
			if (momentums.IsCreated)
			{
				momentums.ElementAt(index2);
			}
			BoneType type = proceduralBone.m_Type;
			if ((uint)(type - 35) <= 7u)
			{
				if (playbackLayers.IsCreated)
				{
					ref PlaybackLayer reference2 = ref playbackLayers.ElementAt((int)(skeleton.m_LayerOffset + (proceduralBone.m_Type - 35)));
					if (m_AnimationData.GetBoneTransform(animationClips, reference2.m_ClipIndex, proceduralBone.m_BindIndex, reference2.m_ClipTime, out var bonePosition, out var boneRotation))
					{
						skeleton.m_CurrentUpdated |= !reference.m_Position.Equals(bonePosition) || !reference.m_Rotation.Equals(boneRotation);
						reference.m_Position = bonePosition;
						reference.m_Rotation = boneRotation;
					}
				}
			}
			else
			{
				ObjectInterpolateSystem.AnimateInterpolatedBone(proceduralBones, bones, momentums, oldTransform, newTransform, prefabRef, ref skeleton, swayRotation, swayOffset, steeringRadius, pivotOffset, index, deltaTime, entity, instantReset, m_FrameIndex, m_FrameTime, ref random, ref m_PointOfInterestData, ref m_CurveData, ref m_PrefabRefData, ref m_PrefabUtilityLaneData, ref m_PrefabObjectGeometryData, ref m_LaneSearchTree);
			}
		}

		private void AnimateStaticLayer(DynamicBuffer<AnimationClip> animationClips, ref PlaybackLayer playbackLayer, float deltaTime, Entity entity, Entity owner, bool instantReset, ref Unity.Mathematics.Random random, ref float2 efficiency)
		{
			int num = -1;
			int num2 = -1;
			float num3 = 0f;
			AnimationClip animationClip;
			for (int i = 0; i < animationClips.Length; i++)
			{
				animationClip = animationClips[i];
				if (animationClip.m_Layer - 5 == (AnimationLayer)playbackLayer.m_LayerIndex)
				{
					int num4 = 0;
					float num5 = 0f;
					switch (animationClip.m_ClipState)
					{
					case ClipState.Operating:
						RequireEfficiency(ref efficiency, owner);
						num4 = math.select(0, 1, efficiency.x > 0f);
						num5 = efficiency.x;
						break;
					case ClipState.Driving:
						num4 = 0;
						num5 = 0f;
						break;
					}
					if (num4 > num2)
					{
						num = i;
						num2 = num4;
						num3 = num5;
					}
				}
			}
			animationClip = default(AnimationClip);
			if (num >= 0)
			{
				animationClip = animationClips[num];
			}
			if (instantReset)
			{
				playbackLayer.m_ClipIndex = (short)num;
				if (animationClip.m_Playback == AnimationPlayback.SyncToRelative)
				{
					if (playbackLayer.m_RelativeClipTime != 0f)
					{
						playbackLayer.m_ClipTime = playbackLayer.m_RelativeClipTime;
					}
				}
				else
				{
					playbackLayer.m_ClipTime = random.NextFloat(animationClip.m_AnimationLength);
				}
				playbackLayer.m_PlaySpeed = num3;
			}
			else if (playbackLayer.m_ClipIndex != num)
			{
				playbackLayer.m_ClipIndex = (short)num;
				switch (animationClip.m_Playback)
				{
				case AnimationPlayback.RandomLoop:
					playbackLayer.m_ClipTime = random.NextFloat(animationClip.m_AnimationLength);
					break;
				case AnimationPlayback.HalfLoop:
					playbackLayer.m_ClipTime = math.select(0f, animationClip.m_AnimationLength * 0.5f, random.NextBool());
					break;
				case AnimationPlayback.SyncToRelative:
					if (playbackLayer.m_RelativeClipTime != 0f)
					{
						playbackLayer.m_ClipTime = playbackLayer.m_RelativeClipTime;
					}
					break;
				default:
					playbackLayer.m_ClipTime = 0f;
					break;
				}
				if (animationClip.m_Acceleration != 0f)
				{
					playbackLayer.m_PlaySpeed = math.min(math.abs(animationClip.m_Acceleration * deltaTime), num3);
				}
				else
				{
					playbackLayer.m_PlaySpeed = num3;
				}
			}
			else
			{
				if (playbackLayer.m_ClipIndex < 0)
				{
					return;
				}
				playbackLayer.m_ClipTime += playbackLayer.m_PlaySpeed * deltaTime;
				if (animationClip.m_Acceleration != 0f)
				{
					float num6 = math.abs(animationClip.m_Acceleration * deltaTime);
					if (num3 > playbackLayer.m_PlaySpeed)
					{
						playbackLayer.m_PlaySpeed = math.min(playbackLayer.m_PlaySpeed + num6, num3);
					}
					else
					{
						playbackLayer.m_PlaySpeed = math.max(playbackLayer.m_PlaySpeed - num6, num3);
					}
				}
				else
				{
					playbackLayer.m_PlaySpeed = num3;
				}
			}
		}

		private void AnimateStaticBone(DynamicBuffer<ProceduralBone> proceduralBones, DynamicBuffer<AnimationClip> animationClips, DynamicBuffer<Bone> bones, DynamicBuffer<Momentum> momentums, DynamicBuffer<PlaybackLayer> playbackLayers, Transform transform, PrefabRef prefabRef, ref Skeleton skeleton, int index, float deltaTime, Entity entity, Entity owner, bool instantReset, Game.Objects.TrafficLightState trafficLightState, ref Unity.Mathematics.Random random, ref float2 wind, ref float2 efficiency, ref float electricity, ref float working)
		{
			ProceduralBone proceduralBone = proceduralBones[index];
			Momentum momentum = default(Momentum);
			int index2 = skeleton.m_BoneOffset + index;
			ref Bone reference = ref bones.ElementAt(index2);
			ref Momentum momentum2 = ref momentum;
			if (momentums.IsCreated)
			{
				momentum2 = ref momentums.ElementAt(index2);
			}
			switch (proceduralBone.m_Type)
			{
			case BoneType.LookAtDirection:
			{
				RequireEfficiency(ref efficiency, owner);
				if (m_PointOfInterestData.TryGetComponent(entity, out var componentData3) && componentData3.m_IsValid)
				{
					quaternion q2 = LocalToWorld(proceduralBones, bones, transform, skeleton, proceduralBone.m_ParentIndex, proceduralBone.m_Rotation);
					float3 float3 = math.mul(v: componentData3.m_Position - transform.m_Position, q: math.inverse(q2));
					float3 = math.select(float3, -float3, proceduralBone.m_Speed < 0f);
					float targetSpeed7 = math.abs(proceduralBone.m_Speed) * efficiency.x;
					AnimateRotatingBoneY(proceduralBone, ref skeleton, ref reference, ref momentum2, ref random, float3.xz, targetSpeed7, deltaTime, instantReset);
				}
				else
				{
					AnimateRotatingBoneY(proceduralBone, ref skeleton, ref reference, ref momentum2, ref random, new float2(0f, 1f), 0f, deltaTime, instantReset);
				}
				break;
			}
			case BoneType.WindTurbineRotation:
			{
				RequireWind(ref wind, transform);
				RequireEfficiency(ref efficiency, owner);
				float targetSpeed2 = proceduralBone.m_Speed * math.length(wind) * efficiency.y;
				AnimateRotatingBoneY(proceduralBone, ref skeleton, ref reference, ref momentum2, ref random, targetSpeed2, deltaTime, instantReset);
				break;
			}
			case BoneType.WindSpeedRotation:
			{
				RequireWind(ref wind, transform);
				float targetSpeed5 = proceduralBone.m_Speed * math.length(wind);
				AnimateRotatingBoneY(proceduralBone, ref skeleton, ref reference, ref momentum2, ref random, targetSpeed5, deltaTime, instantReset);
				break;
			}
			case BoneType.PoweredRotation:
			{
				RequireElectricity(ref electricity, owner);
				float targetSpeed10 = proceduralBone.m_Speed * electricity;
				AnimateRotatingBoneY(proceduralBone, ref skeleton, ref reference, ref momentum2, ref random, targetSpeed10, deltaTime, instantReset);
				break;
			}
			case BoneType.OperatingRotation:
			{
				RequireEfficiency(ref efficiency, owner);
				float targetSpeed9 = proceduralBone.m_Speed * efficiency.x;
				AnimateRotatingBoneY(proceduralBone, ref skeleton, ref reference, ref momentum2, ref random, targetSpeed9, deltaTime, instantReset);
				break;
			}
			case BoneType.WorkingRotation:
			{
				RequireEfficiency(ref efficiency, owner);
				RequireWorking(ref working, entity);
				float targetSpeed3 = proceduralBone.m_Speed * efficiency.x * working;
				AnimateRotatingBoneX(proceduralBone, ref skeleton, ref reference, ref momentum2, ref random, targetSpeed3, deltaTime, instantReset);
				break;
			}
			case BoneType.TrafficBarrierDirection:
			{
				float2 targetDir = math.select(new float2(math.select(-1f, 1f, proceduralBone.m_Speed < 0f), 0f), new float2(0f, 1f), (trafficLightState & (Game.Objects.TrafficLightState.Red | Game.Objects.TrafficLightState.Yellow)) == Game.Objects.TrafficLightState.Red);
				float targetSpeed6 = math.abs(proceduralBone.m_Speed);
				AnimateRotatingBoneZ(proceduralBone, ref skeleton, ref reference, ref momentum2, ref random, targetDir, targetSpeed6, deltaTime, instantReset);
				break;
			}
			case BoneType.LookAtRotation:
			case BoneType.LookAtRotationSide:
			{
				RequireEfficiency(ref efficiency, owner);
				if (m_PointOfInterestData.TryGetComponent(entity, out var componentData) && componentData.m_IsValid)
				{
					float3 position = proceduralBone.m_Position;
					quaternion rotation = proceduralBone.m_Rotation;
					LocalToWorld(proceduralBones, bones, transform, skeleton, proceduralBone.m_ParentIndex, ref position, ref rotation);
					float3 v = componentData.m_Position - position;
					v = math.mul(math.inverse(rotation), v);
					v.xz = math.select(v.xz, MathUtils.Right(v.xz), proceduralBone.m_Type == BoneType.LookAtRotationSide);
					v = math.select(v, -v, proceduralBone.m_Speed < 0f);
					float targetSpeed = math.abs(proceduralBone.m_Speed) * efficiency.x;
					AnimateRotatingBoneY(proceduralBone, ref skeleton, ref reference, ref momentum2, ref random, v.xz, targetSpeed, deltaTime, instantReset);
				}
				else
				{
					AnimateRotatingBoneY(proceduralBone, ref skeleton, ref reference, ref momentum2, ref random, math.forward().xz, 0f, deltaTime, instantReset);
				}
				break;
			}
			case BoneType.LengthwiseLookAtRotation:
			{
				RequireEfficiency(ref efficiency, owner);
				if (m_PointOfInterestData.TryGetComponent(entity, out var componentData5) && componentData5.m_IsValid)
				{
					float3 position4 = proceduralBone.m_Position;
					quaternion rotation4 = proceduralBone.m_Rotation;
					LocalToWorld(proceduralBones, bones, transform, skeleton, proceduralBone.m_ParentIndex, ref position4, ref rotation4);
					float3 v4 = componentData5.m_Position - position4;
					v4 = math.mul(math.inverse(rotation4), v4);
					v4 = math.select(v4, -v4, proceduralBone.m_Speed < 0f);
					float targetSpeed11 = math.abs(proceduralBone.m_Speed) * efficiency.x;
					AnimateRotatingBoneZ(proceduralBone, ref skeleton, ref reference, ref momentum2, ref random, v4.xy, targetSpeed11, deltaTime, instantReset);
				}
				else
				{
					AnimateRotatingBoneZ(proceduralBone, ref skeleton, ref reference, ref momentum2, ref random, math.up().xy, 0f, deltaTime, instantReset);
				}
				break;
			}
			case BoneType.LookAtAim:
			case BoneType.LookAtAimForward:
			{
				RequireEfficiency(ref efficiency, entity);
				if (m_PointOfInterestData.TryGetComponent(entity, out var componentData2) && componentData2.m_IsValid)
				{
					float3 position2 = proceduralBone.m_Position;
					quaternion rotation2 = proceduralBone.m_Rotation;
					LookAtLocalToWorld(proceduralBones, bones, transform, skeleton, componentData2, proceduralBone.m_ParentIndex, ref position2, ref rotation2);
					float3 v2 = componentData2.m_Position - position2;
					v2 = math.mul(math.inverse(rotation2), v2);
					v2.yz = math.select(v2.yz, MathUtils.Left(v2.yz), proceduralBone.m_Type == BoneType.LookAtAimForward);
					v2 = math.select(v2, -v2, proceduralBone.m_Speed < 0f);
					float targetSpeed4 = math.abs(proceduralBone.m_Speed) * efficiency.x;
					AnimateRotatingBoneX(proceduralBone, ref skeleton, ref reference, ref momentum2, ref random, v2.yz, targetSpeed4, deltaTime, instantReset);
				}
				else
				{
					AnimateRotatingBoneX(proceduralBone, ref skeleton, ref reference, ref momentum2, ref random, math.up().yz, 0f, deltaTime, instantReset);
				}
				break;
			}
			case BoneType.FixedRotation:
			{
				ProceduralBone proceduralBone3 = proceduralBones[proceduralBone.m_ParentIndex];
				Bone bone2 = bones.ElementAt(skeleton.m_BoneOffset + proceduralBone.m_ParentIndex);
				quaternion quaternion2 = math.mul(math.inverse(LocalToObject(proceduralBones, bones, skeleton, proceduralBone3.m_ParentIndex, bone2.m_Rotation)), proceduralBone.m_ObjectRotation);
				skeleton.m_CurrentUpdated |= !reference.m_Rotation.Equals(quaternion2);
				reference.m_Rotation = quaternion2;
				break;
			}
			case BoneType.TimeRotation:
			{
				RequireElectricity(ref electricity, owner);
				float num2 = m_TimeOfDay * proceduralBone.m_Speed;
				float angle2;
				if (instantReset)
				{
					angle2 = math.select(random.NextFloat(-MathF.PI, MathF.PI), num2, electricity != 0f);
				}
				else
				{
					float2 float4 = math.normalizesafe(math.mul(math.mul(math.inverse(proceduralBone.m_Rotation), reference.m_Rotation), math.right()).xz);
					angle2 = math.atan2(0f - float4.y, float4.x);
					float num3 = math.abs(deltaTime) * electricity;
					angle2 += math.clamp(MathUtils.RotationAngle(angle2, num2), 0f - num3, num3);
				}
				quaternion quaternion4 = math.mul(proceduralBone.m_Rotation, quaternion.RotateY(angle2));
				skeleton.m_CurrentUpdated |= !reference.m_Rotation.Equals(quaternion4);
				reference.m_Rotation = quaternion4;
				break;
			}
			case BoneType.LookAtMovementX:
			case BoneType.LookAtMovementY:
			case BoneType.LookAtMovementZ:
			{
				RequireEfficiency(ref efficiency, owner);
				float3 v3 = math.select(0f, 1f, new bool3(proceduralBone.m_Type == BoneType.LookAtMovementX, proceduralBone.m_Type == BoneType.LookAtMovementY, proceduralBone.m_Type == BoneType.LookAtMovementZ));
				float3 moveDirection = math.rotate(proceduralBone.m_Rotation, v3);
				if (m_PointOfInterestData.TryGetComponent(entity, out var componentData4) && componentData4.m_IsValid)
				{
					float3 position3 = proceduralBone.m_Position;
					quaternion rotation3 = proceduralBone.m_Rotation;
					LookAtLocalToWorld(proceduralBones, bones, transform, skeleton, componentData4, proceduralBone.m_ParentIndex, ref position3, ref rotation3);
					float3 y = math.rotate(rotation3, v3);
					float num = math.dot(componentData4.m_Position - position3, y);
					num = math.select(num, 0f - num, proceduralBone.m_Speed < 0f);
					float targetSpeed8 = math.abs(proceduralBone.m_Speed) * efficiency.x;
					AnimateMovingBone(proceduralBone, ref skeleton, ref reference, ref momentum2, moveDirection, num, targetSpeed8, deltaTime, instantReset);
				}
				else
				{
					AnimateMovingBone(proceduralBone, ref skeleton, ref reference, ref momentum2, moveDirection, 0f, 0f, deltaTime, instantReset);
				}
				break;
			}
			case BoneType.PantographRotation:
				AnimatePantographBone(proceduralBones, bones, transform, prefabRef, proceduralBone, ref skeleton, ref reference, index, deltaTime, active: false, instantReset, ref m_CurveData, ref m_PrefabRefData, ref m_PrefabUtilityLaneData, ref m_PrefabObjectGeometryData, ref m_LaneSearchTree);
				break;
			case BoneType.RotationXFromMovementY:
				if (proceduralBone.m_SourceIndex >= 0)
				{
					ProceduralBone proceduralBone5 = proceduralBones[proceduralBone.m_SourceIndex];
					float angle = (bones.ElementAt(skeleton.m_BoneOffset + proceduralBone.m_SourceIndex).m_Position.y - proceduralBone5.m_Position.y) * proceduralBone.m_Speed;
					quaternion quaternion3 = math.mul(proceduralBone.m_Rotation, quaternion.RotateX(angle));
					skeleton.m_CurrentUpdated |= !reference.m_Rotation.Equals(quaternion3);
					reference.m_Rotation = quaternion3;
				}
				break;
			case BoneType.ScaledMovement:
				if (proceduralBone.m_SourceIndex >= 0)
				{
					ProceduralBone proceduralBone4 = proceduralBones[proceduralBone.m_SourceIndex];
					float3 @float = bones.ElementAt(skeleton.m_BoneOffset + proceduralBone.m_SourceIndex).m_Position - proceduralBone4.m_Position;
					float3 float2 = proceduralBone.m_Position + @float * proceduralBone.m_Speed;
					skeleton.m_CurrentUpdated |= !reference.m_Position.Equals(float2);
					reference.m_Position = float2;
				}
				break;
			case BoneType.PlaybackLayer0:
			case BoneType.PlaybackLayer1:
			case BoneType.PlaybackLayer2:
			case BoneType.PlaybackLayer3:
			case BoneType.PlaybackLayer4:
			case BoneType.PlaybackLayer5:
			case BoneType.PlaybackLayer6:
			case BoneType.PlaybackLayer7:
				if (playbackLayers.IsCreated)
				{
					ref PlaybackLayer reference2 = ref playbackLayers.ElementAt((int)(skeleton.m_LayerOffset + (proceduralBone.m_Type - 35)));
					if (m_AnimationData.GetBoneTransform(animationClips, reference2.m_ClipIndex, proceduralBone.m_BindIndex, reference2.m_ClipTime, out var bonePosition, out var boneRotation))
					{
						skeleton.m_CurrentUpdated |= !reference.m_Position.Equals(bonePosition) || !reference.m_Rotation.Equals(boneRotation);
						reference.m_Position = bonePosition;
						reference.m_Rotation = boneRotation;
					}
				}
				break;
			case BoneType.ScaledRotation:
				if (proceduralBone.m_SourceIndex >= 0)
				{
					ProceduralBone proceduralBone2 = proceduralBones[proceduralBone.m_SourceIndex];
					Bone bone = bones.ElementAt(skeleton.m_BoneOffset + proceduralBone.m_SourceIndex);
					quaternion q = math.mul(math.inverse(proceduralBone2.m_Rotation), bone.m_Rotation);
					quaternion quaternion = math.mul(proceduralBone.m_Rotation, math.slerp(quaternion.identity, q, proceduralBone.m_Speed));
					skeleton.m_CurrentUpdated |= !reference.m_Rotation.Equals(quaternion);
					reference.m_Rotation = quaternion;
				}
				break;
			case BoneType.RollingTire:
			case BoneType.SteeringTire:
			case BoneType.SuspensionMovement:
			case BoneType.SteeringRotation:
			case BoneType.SuspensionRotation:
			case BoneType.FixedTire:
			case BoneType.DebugMovement:
			case BoneType.VehicleConnection:
			case BoneType.TrainBogie:
			case BoneType.RollingRotation:
			case BoneType.PropellerRotation:
			case BoneType.PropellerAngle:
			case BoneType.SteeringSuspension:
				break;
			}
		}

		private void AnimateStaticLight(DynamicBuffer<ProceduralLight> proceduralLights, DynamicBuffer<LightAnimation> lightAnimations, DynamicBuffer<LightState> lights, bool isBuildingActive, ref Emissive emissive, int index, float deltaTime, Entity owner, bool instantReset, Unity.Mathematics.Random pseudoRandom, Game.Objects.TrafficLightState trafficLightState, CarFlags carFlags, DynamicBuffer<EnabledEffect> effects, ref float electricity)
		{
			ProceduralLight proceduralLight = proceduralLights[index];
			int index2 = emissive.m_LightOffset + index;
			ref LightState light = ref lights.ElementAt(index2);
			switch (proceduralLight.m_Purpose)
			{
			case EmissiveProperties.Purpose.TrafficLight_Red:
				AnimateTrafficLight(proceduralLight, lightAnimations, pseudoRandom, ref emissive, ref light, deltaTime, instantReset, (trafficLightState & Game.Objects.TrafficLightState.Red) != 0, (trafficLightState & Game.Objects.TrafficLightState.Flashing) != 0);
				break;
			case EmissiveProperties.Purpose.TrafficLight_Yellow:
				AnimateTrafficLight(proceduralLight, lightAnimations, pseudoRandom, ref emissive, ref light, deltaTime, instantReset, (trafficLightState & Game.Objects.TrafficLightState.Yellow) != 0, (trafficLightState & Game.Objects.TrafficLightState.Flashing) != 0);
				break;
			case EmissiveProperties.Purpose.TrafficLight_Green:
				AnimateTrafficLight(proceduralLight, lightAnimations, pseudoRandom, ref emissive, ref light, deltaTime, instantReset, (trafficLightState & Game.Objects.TrafficLightState.Green) != 0, (trafficLightState & Game.Objects.TrafficLightState.Flashing) != 0);
				break;
			case EmissiveProperties.Purpose.PedestrianLight_Stop:
			{
				Game.Objects.TrafficLightState trafficLightState3 = (Game.Objects.TrafficLightState)((int)trafficLightState >> 4);
				AnimateTrafficLight(proceduralLight, lightAnimations, pseudoRandom, ref emissive, ref light, deltaTime, instantReset, (trafficLightState3 & Game.Objects.TrafficLightState.Red) != 0, (trafficLightState3 & Game.Objects.TrafficLightState.Flashing) != 0);
				break;
			}
			case EmissiveProperties.Purpose.PedestrianLight_Walk:
			{
				Game.Objects.TrafficLightState trafficLightState2 = (Game.Objects.TrafficLightState)((int)trafficLightState >> 4);
				AnimateTrafficLight(proceduralLight, lightAnimations, pseudoRandom, ref emissive, ref light, deltaTime, instantReset, (trafficLightState2 & Game.Objects.TrafficLightState.Green) != 0, (trafficLightState2 & Game.Objects.TrafficLightState.Flashing) != 0);
				break;
			}
			case EmissiveProperties.Purpose.RailCrossing_Stop:
				AnimateTrafficLight(proceduralLight, lightAnimations, pseudoRandom, ref emissive, ref light, deltaTime, instantReset, (trafficLightState & (Game.Objects.TrafficLightState.Red | Game.Objects.TrafficLightState.Yellow)) != 0, (trafficLightState & Game.Objects.TrafficLightState.Flashing) != 0);
				break;
			case EmissiveProperties.Purpose.NeonSign:
			case EmissiveProperties.Purpose.DecorativeLight:
			{
				RequireElectricity(ref electricity, owner);
				float targetIntensity3 = AnimateIntensity(proceduralLight, lightAnimations, pseudoRandom, m_FrameIndex, m_FrameTime, electricity);
				AnimateLight(proceduralLight, ref emissive, ref light, deltaTime, targetIntensity3, instantReset);
				break;
			}
			case EmissiveProperties.Purpose.Emergency1:
			case EmissiveProperties.Purpose.Emergency2:
			case EmissiveProperties.Purpose.Emergency3:
			case EmissiveProperties.Purpose.Emergency4:
			case EmissiveProperties.Purpose.Emergency5:
			case EmissiveProperties.Purpose.Emergency6:
			case EmissiveProperties.Purpose.RearAlarmLights:
			case EmissiveProperties.Purpose.FrontAlarmLightsLeft:
			case EmissiveProperties.Purpose.FrontAlarmLightsRight:
			case EmissiveProperties.Purpose.Warning1:
			case EmissiveProperties.Purpose.Warning2:
			case EmissiveProperties.Purpose.Emergency7:
			case EmissiveProperties.Purpose.Emergency8:
			case EmissiveProperties.Purpose.Emergency9:
			case EmissiveProperties.Purpose.Emergency10:
			case EmissiveProperties.Purpose.AntiCollisionLightsRed:
			case EmissiveProperties.Purpose.AntiCollisionLightsWhite:
			{
				float targetIntensity6 = 0f;
				if ((carFlags & (CarFlags.Emergency | CarFlags.Warning)) != 0)
				{
					targetIntensity6 = AnimateIntensity(proceduralLight, lightAnimations, pseudoRandom, m_FrameIndex, m_FrameTime, 1f);
				}
				AnimateLight(proceduralLight, ref emissive, ref light, deltaTime, targetIntensity6, instantReset);
				break;
			}
			case EmissiveProperties.Purpose.TaxiSign:
			{
				float targetIntensity5 = 0f;
				if ((carFlags & CarFlags.Sign) != 0)
				{
					targetIntensity5 = AnimateIntensity(proceduralLight, lightAnimations, pseudoRandom, m_FrameIndex, m_FrameTime, 1f);
				}
				AnimateLight(proceduralLight, ref emissive, ref light, deltaTime, targetIntensity5, instantReset);
				break;
			}
			case EmissiveProperties.Purpose.CollectionLights:
			case EmissiveProperties.Purpose.WorkLights:
			{
				float targetIntensity4 = math.select(0f, 1f, (carFlags & CarFlags.Sign) != 0);
				AnimateLight(proceduralLight, ref emissive, ref light, deltaTime, targetIntensity4, instantReset);
				break;
			}
			case EmissiveProperties.Purpose.DaytimeRunningLight:
			case EmissiveProperties.Purpose.Headlight_HighBeam:
			case EmissiveProperties.Purpose.Headlight_LowBeam:
			case EmissiveProperties.Purpose.TurnSignalLeft:
			case EmissiveProperties.Purpose.TurnSignalRight:
			case EmissiveProperties.Purpose.RearLight:
			case EmissiveProperties.Purpose.BrakeLight:
			case EmissiveProperties.Purpose.ReverseLight:
			case EmissiveProperties.Purpose.Clearance:
			case EmissiveProperties.Purpose.DaytimeRunningLightLeft:
			case EmissiveProperties.Purpose.DaytimeRunningLightRight:
			case EmissiveProperties.Purpose.SignalGroup1:
			case EmissiveProperties.Purpose.SignalGroup2:
			case EmissiveProperties.Purpose.SignalGroup3:
			case EmissiveProperties.Purpose.SignalGroup4:
			case EmissiveProperties.Purpose.SignalGroup5:
			case EmissiveProperties.Purpose.SignalGroup6:
			case EmissiveProperties.Purpose.SignalGroup7:
			case EmissiveProperties.Purpose.SignalGroup8:
			case EmissiveProperties.Purpose.SignalGroup9:
			case EmissiveProperties.Purpose.SignalGroup10:
			case EmissiveProperties.Purpose.SignalGroup11:
			case EmissiveProperties.Purpose.Interior1:
			case EmissiveProperties.Purpose.DaytimeRunningLightAlt:
			case EmissiveProperties.Purpose.Dashboard:
			case EmissiveProperties.Purpose.Clearance2:
			case EmissiveProperties.Purpose.MarkerLights:
			case EmissiveProperties.Purpose.BrakeAndTurnSignalLeft:
			case EmissiveProperties.Purpose.BrakeAndTurnSignalRight:
			case EmissiveProperties.Purpose.TaxiLights:
			case EmissiveProperties.Purpose.LandingLights:
			case EmissiveProperties.Purpose.WingInspectionLights:
			case EmissiveProperties.Purpose.LogoLights:
			case EmissiveProperties.Purpose.PositionLightLeft:
			case EmissiveProperties.Purpose.PositionLightRight:
			case EmissiveProperties.Purpose.PositionLights:
			case EmissiveProperties.Purpose.SearchLightsFront:
			case EmissiveProperties.Purpose.SearchLights360:
			case EmissiveProperties.Purpose.NumberLight:
				AnimateLight(proceduralLight, ref emissive, ref light, deltaTime, 0f, instantReset);
				break;
			case EmissiveProperties.Purpose.EffectSource:
				if (effects.IsCreated)
				{
					float targetIntensity2 = 0f;
					int num = 0;
					if (effects.Length > num)
					{
						EnabledEffect enabledEffect = effects[num];
						targetIntensity2 = math.select(0f, 1f, (m_EnabledData[enabledEffect.m_EnabledIndex].m_Flags & EnabledEffectFlags.IsEnabled) != 0);
					}
					AnimateLight(proceduralLight, ref emissive, ref light, deltaTime, targetIntensity2, instantReset);
				}
				break;
			case EmissiveProperties.Purpose.BuildingActive:
			{
				if (!isBuildingActive)
				{
					AnimateLight(proceduralLight, ref emissive, ref light, deltaTime, 0f, instantReset);
					break;
				}
				RequireElectricity(ref electricity, owner);
				float targetIntensity = AnimateIntensity(proceduralLight, lightAnimations, pseudoRandom, m_FrameIndex, m_FrameTime, electricity);
				AnimateLight(proceduralLight, ref emissive, ref light, deltaTime, targetIntensity, instantReset);
				break;
			}
			case EmissiveProperties.Purpose.Interior2:
			case EmissiveProperties.Purpose.BoardingLightLeft:
			case EmissiveProperties.Purpose.BoardingLightRight:
				break;
			}
		}

		private void AnimateTrafficLight(ProceduralLight proceduralLight, DynamicBuffer<LightAnimation> lightAnimations, Unity.Mathematics.Random pseudoRandom, ref Emissive emissive, ref LightState light, float deltaTime, bool instantReset, bool on, bool flashing)
		{
			float num = math.select(0f, 1f, on);
			if (flashing)
			{
				num = AnimateIntensity(proceduralLight, lightAnimations, pseudoRandom, m_FrameIndex, m_FrameTime, num);
			}
			AnimateLight(proceduralLight, ref emissive, ref light, deltaTime, num, instantReset);
		}

		private void RequireWind(ref float2 wind, Transform transform)
		{
			if (math.isnan(wind.x))
			{
				wind = Wind.SampleWind(m_WindData, transform.m_Position);
			}
		}

		private void RequireEfficiency(ref float2 efficiency, Entity owner)
		{
			if (efficiency.x >= 0f)
			{
				return;
			}
			if (m_BuildingEfficiencyData.TryGetBuffer(owner, out var bufferData))
			{
				efficiency = GetEfficiency(bufferData);
				return;
			}
			Owner componentData;
			while (m_OwnerData.TryGetComponent(owner, out componentData))
			{
				owner = componentData.m_Owner;
				if (m_BuildingEfficiencyData.TryGetBuffer(owner, out bufferData))
				{
					efficiency = GetEfficiency(bufferData);
					return;
				}
			}
			if (m_AttachmentData.TryGetComponent(owner, out var componentData2) && m_BuildingEfficiencyData.TryGetBuffer(componentData2.m_Attached, out bufferData))
			{
				efficiency = GetEfficiency(bufferData);
			}
			else
			{
				efficiency = 1f;
			}
		}

		private float2 GetEfficiency(DynamicBuffer<Efficiency> buffer)
		{
			float2 @float = 1f;
			float2 y = default(float2);
			foreach (Efficiency item in buffer)
			{
				y.x = item.m_Efficiency;
				y.y = math.select(1f, item.m_Efficiency, item.m_Factor != EfficiencyFactor.Fire);
				@float *= math.max(0f, y);
			}
			return math.select(0f, math.max(0.01f, math.round(100f * @float) * 0.01f), @float > 0f);
		}

		private void RequireElectricity(ref float electricity, Entity owner)
		{
			if (electricity >= 0f)
			{
				return;
			}
			electricity = 1f;
			if (m_BuildingElectricityConsumer.TryGetComponent(owner, out var componentData))
			{
				electricity = math.select(0f, 1f, componentData.electricityConnected);
				return;
			}
			if (m_BuildingEfficiencyData.TryGetBuffer(owner, out var bufferData))
			{
				electricity = BuildingUtils.GetEfficiency(bufferData);
			}
			Owner componentData2;
			while (m_OwnerData.TryGetComponent(owner, out componentData2))
			{
				owner = componentData2.m_Owner;
				if (m_BuildingElectricityConsumer.TryGetComponent(owner, out componentData))
				{
					electricity = math.select(0f, 1f, componentData.electricityConnected);
					return;
				}
				if (m_BuildingEfficiencyData.TryGetBuffer(owner, out bufferData))
				{
					electricity = BuildingUtils.GetEfficiency(bufferData);
				}
			}
			if (m_AttachmentData.TryGetComponent(owner, out var componentData3))
			{
				if (m_BuildingElectricityConsumer.TryGetComponent(componentData3.m_Attached, out componentData))
				{
					electricity = math.select(0f, 1f, componentData.electricityConnected);
				}
				else if (m_BuildingEfficiencyData.TryGetBuffer(componentData3.m_Attached, out bufferData))
				{
					electricity = BuildingUtils.GetEfficiency(bufferData);
				}
			}
		}

		private void UpdateInterpolatedTransforms(PreCullingData cullingData, DynamicBuffer<TransformFrame> transformFrames, ref Unity.Mathematics.Random random)
		{
			CalculateUpdateFrames(m_FrameIndex, m_PrevFrameIndex, m_FrameTime, (uint)cullingData.m_UpdateFrame, out var updateFrame, out var updateFrame2, out var framePosition, out var updateFrameChanged);
			float updateFrameToSeconds = 4f / 15f;
			float deltaTime = m_FrameDelta / 60f;
			float speedDeltaFactor = math.select(60f / m_FrameDelta, 0f, m_FrameDelta == 0f);
			if ((cullingData.m_Flags & PreCullingFlags.Animated) != 0)
			{
				UpdateInterpolatedAnimations(cullingData, transformFrames, ref random, updateFrame, updateFrame2, framePosition, updateFrameToSeconds, deltaTime, speedDeltaFactor, updateFrameChanged);
			}
			else if ((cullingData.m_Flags & PreCullingFlags.Skeleton) != 0)
			{
				UpdateInterpolatedAnimations(cullingData, transformFrames, updateFrame, updateFrame2, framePosition, updateFrameToSeconds, deltaTime, speedDeltaFactor, ref random);
			}
			else
			{
				UpdateInterpolatedTransforms(cullingData, transformFrames, updateFrame, updateFrame2, framePosition, updateFrameToSeconds, deltaTime, speedDeltaFactor);
			}
		}

		private void RequireWorking(ref float working, Entity owner)
		{
			if (!(working >= 0f))
			{
				if (m_BuildingExtractorFacility.TryGetComponent(owner, out var componentData))
				{
					working = math.select(0f, 1f, (componentData.m_Flags & ExtractorFlags.Working) != 0);
				}
				else
				{
					working = 1f;
				}
			}
		}

		private void UpdateInterpolatedTransforms(PreCullingData cullingData, DynamicBuffer<TransformFrame> frames, uint updateFrame1, uint updateFrame2, float framePosition, float updateFrameToSeconds, float deltaTime, float speedDeltaFactor)
		{
			ref InterpolatedTransform valueRW = ref m_InterpolatedTransformData.GetRefRW(cullingData.m_Entity).ValueRW;
			InterpolatedTransform oldTransform = valueRW;
			TransformFrame frame = frames[(int)updateFrame1];
			TransformFrame frame2 = frames[(int)updateFrame2];
			valueRW = CalculateTransform(frame, frame2, framePosition);
			if (!m_SwayingData.HasComponent(cullingData.m_Entity))
			{
				return;
			}
			ref Swaying valueRW2 = ref m_SwayingData.GetRefRW(cullingData.m_Entity).ValueRW;
			PrefabRef prefabRef = m_PrefabRefData[cullingData.m_Entity];
			bool flag = (cullingData.m_Flags & PreCullingFlags.NearCameraUpdated) != 0;
			SwayingData componentData2;
			if (m_WatercraftData.HasComponent(cullingData.m_Entity))
			{
				if (m_PrefabObjectGeometryData.TryGetComponent(prefabRef.m_Prefab, out var componentData))
				{
					float3 frameVelocity = math.lerp(frame.m_Velocity, frame2.m_Velocity, framePosition);
					UpdateWaterSwaying(componentData, cullingData.m_Entity.Index, frameVelocity, oldTransform, ref valueRW, ref valueRW2, deltaTime, speedDeltaFactor, 0.7f, flag);
				}
			}
			else if (flag)
			{
				valueRW2.m_LastVelocity = math.lerp(frame.m_Velocity, frame2.m_Velocity, framePosition);
				valueRW2.m_SwayVelocity = 0f;
				valueRW2.m_SwayPosition = 0f;
			}
			else if (m_PrefabSwayingData.TryGetComponent(prefabRef.m_Prefab, out componentData2))
			{
				UpdateSwaying(componentData2, oldTransform, ref valueRW, ref valueRW2, deltaTime, speedDeltaFactor, localSway: true, out var _, out var _);
			}
		}

		private void UpdateInterpolatedAnimations(PreCullingData cullingData, DynamicBuffer<TransformFrame> frames, uint updateFrame1, uint updateFrame2, float framePosition, float updateFrameToSeconds, float deltaTime, float speedDeltaFactor, ref Unity.Mathematics.Random random)
		{
			bool flag = false;
			bool flag2 = false;
			if (m_ControllerData.TryGetComponent(cullingData.m_Entity, out var componentData))
			{
				flag = componentData.m_Controller != Entity.Null;
				if (m_CarTrailerData.HasComponent(cullingData.m_Entity))
				{
					if (m_CullingInfoData.TryGetComponent(componentData.m_Controller, out var componentData2) && componentData2.m_CullingIndex != 0 && (m_CullingData[componentData2.m_CullingIndex].m_Flags & PreCullingFlags.NearCamera) != 0)
					{
						return;
					}
					flag2 = true;
				}
			}
			flag2 |= (cullingData.m_Flags & PreCullingFlags.NearCameraUpdated) != 0;
			ref InterpolatedTransform valueRW = ref m_InterpolatedTransformData.GetRefRW(cullingData.m_Entity).ValueRW;
			InterpolatedTransform interpolatedTransform = valueRW;
			PrefabRef prefabRef = m_PrefabRefData[cullingData.m_Entity];
			TransformFrame frame = frames[(int)updateFrame1];
			TransformFrame frame2 = frames[(int)updateFrame2];
			valueRW = CalculateTransform(frame, frame2, framePosition);
			quaternion swayRotation = quaternion.identity;
			float swayOffset = 0f;
			if (m_SwayingData.HasComponent(cullingData.m_Entity))
			{
				ref Swaying valueRW2 = ref m_SwayingData.GetRefRW(cullingData.m_Entity).ValueRW;
				SwayingData componentData4;
				if (m_WatercraftData.HasComponent(cullingData.m_Entity))
				{
					if (m_PrefabObjectGeometryData.TryGetComponent(prefabRef.m_Prefab, out var componentData3))
					{
						float3 frameVelocity = math.lerp(frame.m_Velocity, frame2.m_Velocity, framePosition);
						UpdateWaterSwaying(componentData3, cullingData.m_Entity.Index, frameVelocity, interpolatedTransform, ref valueRW, ref valueRW2, deltaTime, speedDeltaFactor, 0.7f, flag2);
					}
				}
				else if (flag2)
				{
					valueRW2.m_LastVelocity = math.lerp(frame.m_Velocity, frame2.m_Velocity, framePosition);
					valueRW2.m_SwayVelocity = 0f;
					valueRW2.m_SwayPosition = 0f;
				}
				else if (m_PrefabSwayingData.TryGetComponent(prefabRef.m_Prefab, out componentData4))
				{
					UpdateSwaying(componentData4, interpolatedTransform, ref valueRW, ref valueRW2, deltaTime, speedDeltaFactor, localSway: true, out swayRotation, out swayOffset);
				}
			}
			DynamicBuffer<SubMesh> dynamicBuffer = m_SubMeshes[prefabRef.m_Prefab];
			if ((cullingData.m_Flags & PreCullingFlags.Skeleton) != 0)
			{
				DynamicBuffer<Skeleton> dynamicBuffer2 = m_Skeletons[cullingData.m_Entity];
				DynamicBuffer<Bone> bones = m_Bones[cullingData.m_Entity];
				m_Momentums.TryGetBuffer(cullingData.m_Entity, out var bufferData);
				int priority = 0;
				int i = 0;
				if (m_PlaybackLayers.TryGetBuffer(cullingData.m_Entity, out var bufferData2))
				{
					CullingInfo cullingInfo = m_CullingInfoData[cullingData.m_Entity];
					float num = RenderingUtils.CalculateMinDistance(cullingInfo.m_Bounds, m_CameraPosition, m_CameraDirection, m_LodParameters);
					priority = RenderingUtils.CalculateLod(num * num, m_LodParameters) - cullingInfo.m_MinLod;
				}
				for (int j = 0; j < dynamicBuffer2.Length; j++)
				{
					ref Skeleton reference = ref dynamicBuffer2.ElementAt(j);
					if (reference.m_BufferAllocation.Empty)
					{
						continue;
					}
					SubMesh subMesh = dynamicBuffer[j];
					DynamicBuffer<ProceduralBone> proceduralBones = m_ProceduralBones[subMesh.m_SubMesh];
					InterpolatedTransform oldTransform = interpolatedTransform;
					InterpolatedTransform newTransform = valueRW;
					if ((subMesh.m_Flags & SubMeshFlags.HasTransform) != 0)
					{
						oldTransform = ObjectUtils.LocalToWorld(interpolatedTransform, subMesh.m_Position, subMesh.m_Rotation);
						newTransform = ObjectUtils.LocalToWorld(valueRW, subMesh.m_Position, subMesh.m_Rotation);
					}
					DynamicBuffer<AnimationClip> bufferData3 = default(DynamicBuffer<AnimationClip>);
					if (bufferData2.IsCreated)
					{
						int num2 = ((j != dynamicBuffer2.Length - 1) ? dynamicBuffer2[j + 1].m_LayerOffset : bufferData2.Length);
						if (m_AnimationClips.TryGetBuffer(subMesh.m_SubMesh, out bufferData3))
						{
							for (; i < num2; i++)
							{
								ref PlaybackLayer playbackLayer = ref bufferData2.ElementAt(i);
								AnimateLayer(bufferData3, ref playbackLayer, deltaTime, cullingData.m_Entity, flag2, ref random);
								m_AnimationData.RequireAnimation(subMesh.m_SubMesh, bufferData3, in playbackLayer, priority);
							}
						}
						i = num2;
					}
					float steeringRadius = 0f;
					if (m_PrefabCarData.TryGetComponent(prefabRef.m_Prefab, out var componentData5))
					{
						steeringRadius = CalculateSteeringRadius(proceduralBones, bones, oldTransform, newTransform, ref reference, componentData5);
					}
					for (int k = 0; k < proceduralBones.Length; k++)
					{
						AnimateInterpolatedBone(proceduralBones, bufferData3, bones, bufferData, bufferData2, oldTransform, newTransform, prefabRef, ref reference, swayRotation, swayOffset, steeringRadius, componentData5.m_PivotOffset, k, deltaTime, cullingData.m_Entity, flag2, ref random);
					}
				}
			}
			if ((cullingData.m_Flags & PreCullingFlags.Emissive) == 0)
			{
				return;
			}
			if (!flag || !m_PseudoRandomSeedData.TryGetComponent(componentData.m_Controller, out var componentData6))
			{
				componentData6 = m_PseudoRandomSeedData[cullingData.m_Entity];
			}
			DynamicBuffer<Emissive> dynamicBuffer3 = m_Emissives[cullingData.m_Entity];
			DynamicBuffer<LightState> lights = m_Lights[cullingData.m_Entity];
			Unity.Mathematics.Random random2 = componentData6.GetRandom(PseudoRandomSeed.kLightState);
			for (int l = 0; l < dynamicBuffer3.Length; l++)
			{
				ref Emissive reference2 = ref dynamicBuffer3.ElementAt(l);
				if (!reference2.m_BufferAllocation.Empty)
				{
					SubMesh subMesh2 = dynamicBuffer[l];
					DynamicBuffer<ProceduralLight> proceduralLights = m_ProceduralLights[subMesh2.m_SubMesh];
					m_LightAnimations.TryGetBuffer(subMesh2.m_SubMesh, out var bufferData4);
					for (int m = 0; m < proceduralLights.Length; m++)
					{
						AnimateInterpolatedLight(proceduralLights, bufferData4, lights, valueRW.m_Flags, random2, ref reference2, m, m_FrameIndex, m_FrameTime, deltaTime, flag2);
					}
				}
			}
		}

		private void UpdateInterpolatedAnimations(PreCullingData cullingData, DynamicBuffer<TransformFrame> frames, ref Unity.Mathematics.Random random, uint updateFrame1, uint updateFrame2, float framePosition, float updateFrameToSeconds, float deltaTime, float speedDeltaFactor, int updateFrameChanged)
		{
			bool flag = (cullingData.m_Flags & PreCullingFlags.NearCameraUpdated) != 0;
			ref InterpolatedTransform valueRW = ref m_InterpolatedTransformData.GetRefRW(cullingData.m_Entity).ValueRW;
			InterpolatedTransform oldTransform = valueRW;
			TransformFrame transformFrame = frames[(int)updateFrame1];
			TransformFrame transformFrame2 = frames[(int)updateFrame2];
			if ((cullingData.m_Flags & PreCullingFlags.Animated) != 0)
			{
				PseudoRandomSeed pseudoRandomSeed = m_PseudoRandomSeedData[cullingData.m_Entity];
				PrefabRef prefabRef = m_PrefabRefData[cullingData.m_Entity];
				DynamicBuffer<Animated> dynamicBuffer = m_Animateds[cullingData.m_Entity];
				TransformState state = TransformState.Default;
				ActivityType activity = ActivityType.None;
				DynamicBuffer<MeshGroup> bufferData = default(DynamicBuffer<MeshGroup>);
				DynamicBuffer<CharacterElement> bufferData2 = default(DynamicBuffer<CharacterElement>);
				int priority = 0;
				if (m_SubMeshGroups.TryGetBuffer(prefabRef.m_Prefab, out var _))
				{
					m_MeshGroups.TryGetBuffer(cullingData.m_Entity, out bufferData);
					m_CharacterElements.TryGetBuffer(prefabRef.m_Prefab, out bufferData2);
					valueRW = CalculateTransform(transformFrame, transformFrame2, framePosition);
					CullingInfo cullingInfo = m_CullingInfoData[cullingData.m_Entity];
					float num = RenderingUtils.CalculateMinDistance(cullingInfo.m_Bounds, m_CameraPosition, m_CameraDirection, m_LodParameters);
					priority = RenderingUtils.CalculateLod(num * num, m_LodParameters) - cullingInfo.m_MinLod;
				}
				else
				{
					valueRW = CalculateTransform(transformFrame, transformFrame2, framePosition);
				}
				for (int i = 0; i < dynamicBuffer.Length; i++)
				{
					Animated animated = dynamicBuffer[i];
					if (animated.m_ClipIndexBody0 != -1 && bufferData2.IsCreated)
					{
						CollectionUtils.TryGet(bufferData, i, out var value);
						CharacterElement characterElement = bufferData2[value.m_SubMeshGroup];
						DynamicBuffer<AnimationClip> clips = m_AnimationClips[characterElement.m_Style];
						UpdateInterpolatedAnimationBody(cullingData.m_Entity, in characterElement, clips, ref m_HumanData, ref m_CurrentVehicleData, ref m_PrefabRefData, ref m_ActivityLocations, ref m_AnimationMotions, oldTransform, valueRW, pseudoRandomSeed, ref animated, ref random, transformFrame, transformFrame2, framePosition, updateFrameToSeconds, speedDeltaFactor, deltaTime, updateFrameChanged, flag);
						UpdateInterpolatedAnimationFace(cullingData.m_Entity, clips, ref m_HumanData, ref animated, ref random, state, activity, pseudoRandomSeed, deltaTime, updateFrameChanged, flag);
						m_AnimationData.SetAnimationFrame(characterElement.m_Style, characterElement.m_RestPoseClipIndex, characterElement.m_CorrectiveClipIndex, clips, in animated, GetUpdateFrameTransition(framePosition), priority, flag);
					}
					dynamicBuffer[i] = animated;
				}
			}
			else
			{
				valueRW = CalculateTransform(transformFrame, transformFrame2, framePosition);
			}
		}
	}

	[BurstCompile]
	private struct UpdateTrailerTransformDataJob : IJobParallelForDefer
	{
		[ReadOnly]
		public ComponentLookup<PointOfInterest> m_PointOfInterestData;

		[ReadOnly]
		public ComponentLookup<PseudoRandomSeed> m_PseudoRandomSeedData;

		[ReadOnly]
		public ComponentLookup<Transform> m_TransformData;

		[ReadOnly]
		public ComponentLookup<Curve> m_CurveData;

		[ReadOnly]
		public ComponentLookup<Car> m_CarData;

		[ReadOnly]
		public ComponentLookup<Train> m_TrainData;

		[ReadOnly]
		public ComponentLookup<ParkedTrain> m_ParkedTrainData;

		[ReadOnly]
		public ComponentLookup<PrefabRef> m_PrefabRefData;

		[ReadOnly]
		public ComponentLookup<SwayingData> m_PrefabSwayingData;

		[ReadOnly]
		public ComponentLookup<CarData> m_PrefabCarData;

		[ReadOnly]
		public ComponentLookup<CarTractorData> m_PrefabCarTractorData;

		[ReadOnly]
		public ComponentLookup<CarTrailerData> m_PrefabCarTrailerData;

		[ReadOnly]
		public ComponentLookup<ObjectGeometryData> m_PrefabObjectGeometryData;

		[ReadOnly]
		public ComponentLookup<UtilityLaneData> m_PrefabUtilityLaneData;

		[ReadOnly]
		public BufferLookup<TransformFrame> m_TransformFrames;

		[ReadOnly]
		public BufferLookup<LayoutElement> m_LayoutElements;

		[ReadOnly]
		public BufferLookup<TrainBogieFrame> m_BogieFrames;

		[ReadOnly]
		public BufferLookup<ProceduralBone> m_ProceduralBones;

		[ReadOnly]
		public BufferLookup<ProceduralLight> m_ProceduralLights;

		[ReadOnly]
		public BufferLookup<LightAnimation> m_LightAnimations;

		[ReadOnly]
		public BufferLookup<SubMesh> m_SubMeshes;

		[NativeDisableParallelForRestriction]
		public ComponentLookup<InterpolatedTransform> m_InterpolatedTransformData;

		[NativeDisableParallelForRestriction]
		public ComponentLookup<Swaying> m_SwayingData;

		[NativeDisableParallelForRestriction]
		public BufferLookup<Skeleton> m_Skeletons;

		[NativeDisableParallelForRestriction]
		public BufferLookup<Emissive> m_Emissive;

		[NativeDisableParallelForRestriction]
		public BufferLookup<Bone> m_Bones;

		[NativeDisableParallelForRestriction]
		public BufferLookup<Momentum> m_Momentums;

		[NativeDisableParallelForRestriction]
		public BufferLookup<LightState> m_LightStates;

		[ReadOnly]
		public uint m_FrameIndex;

		[ReadOnly]
		public float m_FrameTime;

		[ReadOnly]
		public float m_FrameDelta;

		[ReadOnly]
		public RandomSeed m_RandomSeed;

		[ReadOnly]
		public NativeQuadTree<Entity, QuadTreeBoundsXZ> m_LaneSearchTree;

		[ReadOnly]
		public NativeList<PreCullingData> m_CullingData;

		public void Execute(int index)
		{
			PreCullingData cullingData = m_CullingData[index];
			if ((cullingData.m_Flags & (PreCullingFlags.NearCamera | PreCullingFlags.Temp | PreCullingFlags.VehicleLayout | PreCullingFlags.Relative)) != (PreCullingFlags.NearCamera | PreCullingFlags.VehicleLayout))
			{
				return;
			}
			Unity.Mathematics.Random random = m_RandomSeed.GetRandom(index);
			if (m_TransformFrames.TryGetBuffer(cullingData.m_Entity, out var _))
			{
				if (m_CarData.HasComponent(cullingData.m_Entity))
				{
					UpdateInterpolatedCarTrailers(cullingData, ref random);
				}
				else
				{
					UpdateInterpolatedLayoutAnimations(cullingData);
				}
			}
			else if (m_ParkedTrainData.HasComponent(cullingData.m_Entity))
			{
				UpdateStaticLayoutAnimations(cullingData);
			}
		}

		private void UpdateInterpolatedCarTrailers(PreCullingData cullingData, ref Unity.Mathematics.Random random)
		{
			CalculateUpdateFrames(m_FrameIndex, m_FrameTime, (uint)cullingData.m_UpdateFrame, out var updateFrame, out var updateFrame2, out var framePosition);
			float deltaTime = m_FrameDelta / 60f;
			float speedDeltaFactor = math.select(60f / m_FrameDelta, 0f, m_FrameDelta == 0f);
			PrefabRef prefabRef = m_PrefabRefData[cullingData.m_Entity];
			DynamicBuffer<LayoutElement> dynamicBuffer = m_LayoutElements[cullingData.m_Entity];
			if (dynamicBuffer.Length <= 1)
			{
				return;
			}
			InterpolatedTransform interpolatedTransform = m_InterpolatedTransformData[cullingData.m_Entity];
			PseudoRandomSeed pseudoRandomSeed = m_PseudoRandomSeedData[cullingData.m_Entity];
			CarTractorData carTractorData = m_PrefabCarTractorData[prefabRef.m_Prefab];
			bool flag = (cullingData.m_Flags & PreCullingFlags.NearCameraUpdated) != 0;
			for (int i = 1; i < dynamicBuffer.Length; i++)
			{
				Entity vehicle = dynamicBuffer[i].m_Vehicle;
				InterpolatedTransform interpolatedTransform2 = m_InterpolatedTransformData[vehicle];
				PrefabRef prefabRef2 = m_PrefabRefData[vehicle];
				DynamicBuffer<TransformFrame> dynamicBuffer2 = m_TransformFrames[vehicle];
				CarData carData = m_PrefabCarData[prefabRef2.m_Prefab];
				CarTrailerData carTrailerData = m_PrefabCarTrailerData[prefabRef2.m_Prefab];
				TransformFrame frame = dynamicBuffer2[(int)updateFrame];
				TransformFrame frame2 = dynamicBuffer2[(int)updateFrame2];
				InterpolatedTransform newTransform = CalculateTransform(frame, frame2, framePosition);
				switch (carTrailerData.m_MovementType)
				{
				case TrailerMovementType.Free:
				{
					float3 @float = interpolatedTransform.m_Position + math.rotate(interpolatedTransform.m_Rotation, carTractorData.m_AttachPosition);
					float3 float2 = newTransform.m_Position + math.rotate(newTransform.m_Rotation, new float3(carTrailerData.m_AttachPosition.xy, carData.m_PivotOffset));
					newTransform.m_Rotation = interpolatedTransform.m_Rotation;
					float3 value = @float - float2;
					if (MathUtils.TryNormalize(ref value))
					{
						newTransform.m_Rotation = quaternion.LookRotationSafe(value, math.up());
					}
					newTransform.m_Position = @float - math.rotate(newTransform.m_Rotation, carTrailerData.m_AttachPosition);
					break;
				}
				case TrailerMovementType.Locked:
					newTransform.m_Position = interpolatedTransform.m_Position;
					newTransform.m_Position -= math.rotate(newTransform.m_Rotation, carTrailerData.m_AttachPosition);
					newTransform.m_Position += math.rotate(interpolatedTransform.m_Rotation, carTractorData.m_AttachPosition);
					newTransform.m_Rotation = interpolatedTransform.m_Rotation;
					break;
				}
				quaternion swayRotation = quaternion.identity;
				float swayOffset = 0f;
				if (m_SwayingData.HasComponent(vehicle))
				{
					Swaying swaying = m_SwayingData[vehicle];
					SwayingData componentData;
					if (flag)
					{
						swaying.m_LastVelocity = math.lerp(frame.m_Velocity, frame2.m_Velocity, framePosition);
						swaying.m_SwayVelocity = 0f;
						swaying.m_SwayPosition = 0f;
					}
					else if (m_PrefabSwayingData.TryGetComponent(prefabRef2.m_Prefab, out componentData))
					{
						componentData.m_MaxPosition.z = 0f;
						UpdateSwaying(componentData, interpolatedTransform2, ref newTransform, ref swaying, deltaTime, speedDeltaFactor, localSway: true, out swayRotation, out swayOffset);
					}
					m_SwayingData[vehicle] = swaying;
				}
				if (m_Skeletons.HasBuffer(vehicle))
				{
					DynamicBuffer<Skeleton> dynamicBuffer3 = m_Skeletons[vehicle];
					DynamicBuffer<Bone> bones = m_Bones[vehicle];
					DynamicBuffer<SubMesh> dynamicBuffer4 = m_SubMeshes[prefabRef2.m_Prefab];
					DynamicBuffer<Momentum> momentums = default(DynamicBuffer<Momentum>);
					if (m_Momentums.HasBuffer(vehicle))
					{
						momentums = m_Momentums[vehicle];
					}
					for (int j = 0; j < dynamicBuffer3.Length; j++)
					{
						ref Skeleton reference = ref dynamicBuffer3.ElementAt(j);
						if (!reference.m_BufferAllocation.Empty)
						{
							SubMesh subMesh = dynamicBuffer4[j];
							DynamicBuffer<ProceduralBone> proceduralBones = m_ProceduralBones[subMesh.m_SubMesh];
							InterpolatedTransform oldTransform = interpolatedTransform2;
							InterpolatedTransform newTransform2 = newTransform;
							if ((subMesh.m_Flags & SubMeshFlags.HasTransform) != 0)
							{
								oldTransform = ObjectUtils.LocalToWorld(interpolatedTransform2, subMesh.m_Position, subMesh.m_Rotation);
								newTransform2 = ObjectUtils.LocalToWorld(newTransform, subMesh.m_Position, subMesh.m_Rotation);
							}
							float steeringRadius = 0f;
							if (m_PrefabCarData.TryGetComponent(prefabRef.m_Prefab, out var componentData2))
							{
								steeringRadius = CalculateSteeringRadius(proceduralBones, bones, oldTransform, newTransform2, ref reference, componentData2);
							}
							for (int k = 0; k < proceduralBones.Length; k++)
							{
								AnimateInterpolatedBone(proceduralBones, bones, momentums, oldTransform, newTransform2, prefabRef, ref reference, swayRotation, swayOffset, steeringRadius, componentData2.m_PivotOffset, k, deltaTime, vehicle, flag, m_FrameIndex, m_FrameTime, ref random, ref m_PointOfInterestData, ref m_CurveData, ref m_PrefabRefData, ref m_PrefabUtilityLaneData, ref m_PrefabObjectGeometryData, ref m_LaneSearchTree);
							}
						}
					}
				}
				if (m_Emissive.HasBuffer(vehicle))
				{
					DynamicBuffer<Emissive> dynamicBuffer5 = m_Emissive[vehicle];
					DynamicBuffer<LightState> lights = m_LightStates[vehicle];
					DynamicBuffer<SubMesh> dynamicBuffer6 = m_SubMeshes[prefabRef2.m_Prefab];
					Unity.Mathematics.Random random2 = pseudoRandomSeed.GetRandom(PseudoRandomSeed.kLightState);
					for (int l = 0; l < dynamicBuffer5.Length; l++)
					{
						ref Emissive reference2 = ref dynamicBuffer5.ElementAt(l);
						if (!reference2.m_BufferAllocation.Empty)
						{
							SubMesh subMesh2 = dynamicBuffer6[l];
							DynamicBuffer<ProceduralLight> proceduralLights = m_ProceduralLights[subMesh2.m_SubMesh];
							m_LightAnimations.TryGetBuffer(subMesh2.m_SubMesh, out var bufferData);
							for (int m = 0; m < proceduralLights.Length; m++)
							{
								AnimateInterpolatedLight(proceduralLights, bufferData, lights, newTransform.m_Flags, random2, ref reference2, m, m_FrameIndex, m_FrameTime, deltaTime, flag);
							}
						}
					}
				}
				m_InterpolatedTransformData[vehicle] = newTransform;
				if (i != dynamicBuffer.Length - 1)
				{
					interpolatedTransform = newTransform;
					carTractorData = m_PrefabCarTractorData[prefabRef2.m_Prefab];
				}
			}
		}

		private void UpdateInterpolatedLayoutAnimations(PreCullingData cullingData)
		{
			CalculateUpdateFrames(m_FrameIndex, m_FrameTime, (uint)cullingData.m_UpdateFrame, out var updateFrame, out var updateFrame2, out var _);
			DynamicBuffer<LayoutElement> dynamicBuffer = m_LayoutElements[cullingData.m_Entity];
			if (dynamicBuffer.Length == 0)
			{
				return;
			}
			Entity entity = dynamicBuffer[0].m_Vehicle;
			PrefabRef prefabRef = m_PrefabRefData[entity];
			InterpolatedTransform prevTransform = default(InterpolatedTransform);
			InterpolatedTransform interpolatedTransform = m_InterpolatedTransformData[entity];
			ObjectGeometryData prevGeometryData = default(ObjectGeometryData);
			ObjectGeometryData objectGeometryData = m_PrefabObjectGeometryData[prefabRef.m_Prefab];
			bool prevReversed = false;
			bool flag = false;
			if (m_TrainData.TryGetComponent(entity, out var componentData))
			{
				flag = (componentData.m_Flags & Game.Vehicles.TrainFlags.Reversed) != 0;
			}
			for (int i = 0; i < dynamicBuffer.Length; i++)
			{
				Entity entity2 = default(Entity);
				PrefabRef prefabRef2 = default(PrefabRef);
				InterpolatedTransform interpolatedTransform2 = default(InterpolatedTransform);
				ObjectGeometryData objectGeometryData2 = default(ObjectGeometryData);
				bool flag2 = false;
				if (i < dynamicBuffer.Length - 1)
				{
					entity2 = dynamicBuffer[i + 1].m_Vehicle;
					prefabRef2 = m_PrefabRefData[entity2];
					interpolatedTransform2 = m_InterpolatedTransformData[entity2];
					objectGeometryData2 = m_PrefabObjectGeometryData[prefabRef2.m_Prefab];
					if (m_TrainData.TryGetComponent(entity2, out componentData))
					{
						flag2 = (componentData.m_Flags & Game.Vehicles.TrainFlags.Reversed) != 0;
					}
				}
				if (m_Skeletons.HasBuffer(entity))
				{
					DynamicBuffer<Skeleton> dynamicBuffer2 = m_Skeletons[entity];
					DynamicBuffer<Bone> bones = m_Bones[entity];
					DynamicBuffer<SubMesh> dynamicBuffer3 = m_SubMeshes[prefabRef.m_Prefab];
					TrainBogieFrame bogieFrame = default(TrainBogieFrame);
					TrainBogieFrame bogieFrame2 = default(TrainBogieFrame);
					if (m_BogieFrames.TryGetBuffer(entity, out var bufferData))
					{
						bogieFrame = bufferData[(int)updateFrame];
						bogieFrame2 = bufferData[(int)updateFrame2];
					}
					for (int j = 0; j < dynamicBuffer2.Length; j++)
					{
						ref Skeleton reference = ref dynamicBuffer2.ElementAt(j);
						if (!reference.m_BufferAllocation.Empty)
						{
							SubMesh subMesh = dynamicBuffer3[j];
							DynamicBuffer<ProceduralBone> proceduralBones = m_ProceduralBones[subMesh.m_SubMesh];
							for (int k = 0; k < proceduralBones.Length; k++)
							{
								AnimateInterpolatedLayoutBone(proceduralBones, bones, prevTransform, interpolatedTransform, interpolatedTransform2, prevGeometryData, objectGeometryData, objectGeometryData2, bogieFrame, bogieFrame2, prevReversed, flag, flag2, ref reference, k);
							}
						}
					}
				}
				prevTransform = interpolatedTransform;
				prevGeometryData = objectGeometryData;
				prevReversed = flag;
				entity = entity2;
				prefabRef = prefabRef2;
				interpolatedTransform = interpolatedTransform2;
				objectGeometryData = objectGeometryData2;
				flag = flag2;
			}
		}

		private void UpdateStaticLayoutAnimations(PreCullingData cullingData)
		{
			DynamicBuffer<LayoutElement> dynamicBuffer = m_LayoutElements[cullingData.m_Entity];
			if (dynamicBuffer.Length == 0)
			{
				return;
			}
			Entity entity = dynamicBuffer[0].m_Vehicle;
			PrefabRef prefabRef = m_PrefabRefData[entity];
			Transform prevTransform = default(Transform);
			Transform transform = m_TransformData[entity];
			ObjectGeometryData prevGeometryData = default(ObjectGeometryData);
			ObjectGeometryData objectGeometryData = m_PrefabObjectGeometryData[prefabRef.m_Prefab];
			bool prevReversed = false;
			bool flag = false;
			if (m_TrainData.TryGetComponent(entity, out var componentData))
			{
				flag = (componentData.m_Flags & Game.Vehicles.TrainFlags.Reversed) != 0;
			}
			for (int i = 0; i < dynamicBuffer.Length; i++)
			{
				Entity entity2 = default(Entity);
				PrefabRef prefabRef2 = default(PrefabRef);
				Transform transform2 = default(Transform);
				ObjectGeometryData objectGeometryData2 = default(ObjectGeometryData);
				bool flag2 = false;
				if (i < dynamicBuffer.Length - 1)
				{
					entity2 = dynamicBuffer[i + 1].m_Vehicle;
					prefabRef2 = m_PrefabRefData[entity2];
					transform2 = m_TransformData[entity2];
					objectGeometryData2 = m_PrefabObjectGeometryData[prefabRef2.m_Prefab];
					if (m_TrainData.TryGetComponent(entity2, out componentData))
					{
						flag2 = (componentData.m_Flags & Game.Vehicles.TrainFlags.Reversed) != 0;
					}
				}
				if (m_Skeletons.HasBuffer(entity))
				{
					ParkedTrain parkedTrain = m_ParkedTrainData[entity];
					DynamicBuffer<Skeleton> dynamicBuffer2 = m_Skeletons[entity];
					DynamicBuffer<Bone> bones = m_Bones[entity];
					DynamicBuffer<SubMesh> dynamicBuffer3 = m_SubMeshes[prefabRef.m_Prefab];
					for (int j = 0; j < dynamicBuffer2.Length; j++)
					{
						ref Skeleton reference = ref dynamicBuffer2.ElementAt(j);
						if (!reference.m_BufferAllocation.Empty)
						{
							SubMesh subMesh = dynamicBuffer3[j];
							DynamicBuffer<ProceduralBone> proceduralBones = m_ProceduralBones[subMesh.m_SubMesh];
							for (int k = 0; k < proceduralBones.Length; k++)
							{
								AnimateStaticLayoutBone(proceduralBones, bones, prevTransform, transform, transform2, parkedTrain, prevGeometryData, objectGeometryData, objectGeometryData2, prevReversed, flag, flag2, ref reference, k);
							}
						}
					}
				}
				prevTransform = transform;
				prevGeometryData = objectGeometryData;
				prevReversed = flag;
				entity = entity2;
				prefabRef = prefabRef2;
				transform = transform2;
				objectGeometryData = objectGeometryData2;
				flag = flag2;
			}
		}

		private void AnimateInterpolatedLayoutBone(DynamicBuffer<ProceduralBone> proceduralBones, DynamicBuffer<Bone> bones, InterpolatedTransform prevTransform, InterpolatedTransform curTransform, InterpolatedTransform nextTransform, ObjectGeometryData prevGeometryData, ObjectGeometryData curGeometryData, ObjectGeometryData nextGeometryData, TrainBogieFrame bogieFrame1, TrainBogieFrame bogieFrame2, bool prevReversed, bool curReversed, bool nextReversed, ref Skeleton skeleton, int index)
		{
			ProceduralBone proceduralBone = proceduralBones[index];
			int index2 = skeleton.m_BoneOffset + index;
			ref Bone bone = ref bones.ElementAt(index2);
			switch (proceduralBone.m_Type)
			{
			case BoneType.VehicleConnection:
				AnimateVehicleConnectionBone(proceduralBone, ref skeleton, ref bone, prevGeometryData, curGeometryData, nextGeometryData, prevTransform.ToTransform(), curTransform.ToTransform(), nextTransform.ToTransform(), prevReversed, curReversed, nextReversed);
				break;
			case BoneType.TrainBogie:
			{
				float num = 2f;
				Entity entity;
				Entity entity2;
				if (proceduralBone.m_ObjectPosition.z >= 0f == curReversed)
				{
					entity = bogieFrame1.m_RearLane;
					entity2 = bogieFrame2.m_RearLane;
				}
				else
				{
					entity = bogieFrame1.m_FrontLane;
					entity2 = bogieFrame2.m_FrontLane;
				}
				float3 position = ObjectUtils.LocalToWorld(curTransform.ToTransform(), new float3(0f, 0f, proceduralBone.m_ObjectPosition.z));
				float3 position2 = default(float3);
				float3 tangent = default(float3);
				if (m_CurveData.TryGetComponent(entity, out var componentData))
				{
					float t;
					float num2 = MathUtils.Distance(componentData.m_Bezier, position, out t);
					if (num2 < num)
					{
						position2 = MathUtils.Position(componentData.m_Bezier, t);
						tangent = MathUtils.Tangent(componentData.m_Bezier, t);
						num = num2;
					}
				}
				if (entity != entity2 && m_CurveData.TryGetComponent(entity2, out var componentData2))
				{
					float t2;
					float num3 = MathUtils.Distance(componentData2.m_Bezier, position, out t2);
					if (num3 < num)
					{
						position2 = MathUtils.Position(componentData2.m_Bezier, t2);
						tangent = MathUtils.Tangent(componentData2.m_Bezier, t2);
						num = num3;
					}
				}
				AnimateTrainBogieBone(curTransform.ToTransform(), proceduralBone, ref skeleton, ref bone, position2, tangent, num != 2f);
				break;
			}
			}
		}

		private void AnimateStaticLayoutBone(DynamicBuffer<ProceduralBone> proceduralBones, DynamicBuffer<Bone> bones, Transform prevTransform, Transform curTransform, Transform nextTransform, ParkedTrain parkedTrain, ObjectGeometryData prevGeometryData, ObjectGeometryData curGeometryData, ObjectGeometryData nextGeometryData, bool prevReversed, bool curReversed, bool nextReversed, ref Skeleton skeleton, int index)
		{
			ProceduralBone proceduralBone = proceduralBones[index];
			int index2 = skeleton.m_BoneOffset + index;
			ref Bone bone = ref bones.ElementAt(index2);
			switch (proceduralBone.m_Type)
			{
			case BoneType.VehicleConnection:
				AnimateVehicleConnectionBone(proceduralBone, ref skeleton, ref bone, prevGeometryData, curGeometryData, nextGeometryData, prevTransform, curTransform, nextTransform, prevReversed, curReversed, nextReversed);
				break;
			case BoneType.TrainBogie:
			{
				Entity entity;
				float t;
				if (proceduralBone.m_ObjectPosition.z >= 0f == curReversed)
				{
					entity = parkedTrain.m_RearLane;
					t = parkedTrain.m_CurvePosition.y;
				}
				else
				{
					entity = parkedTrain.m_FrontLane;
					t = parkedTrain.m_CurvePosition.x;
				}
				float3 position = default(float3);
				float3 tangent = default(float3);
				Curve componentData;
				bool flag = m_CurveData.TryGetComponent(entity, out componentData);
				if (flag)
				{
					position = MathUtils.Position(componentData.m_Bezier, t);
					tangent = MathUtils.Tangent(componentData.m_Bezier, t);
				}
				AnimateTrainBogieBone(curTransform, proceduralBone, ref skeleton, ref bone, position, tangent, flag);
				break;
			}
			}
		}

		private void AnimateVehicleConnectionBone(ProceduralBone proceduralBone, ref Skeleton skeleton, ref Bone bone, ObjectGeometryData prevGeometryData, ObjectGeometryData curGeometryData, ObjectGeometryData nextGeometryData, Transform prevTransform, Transform curTransform, Transform nextTransform, bool prevReversed, bool curReversed, bool nextReversed)
		{
			quaternion quaternion = math.inverse(curTransform.m_Rotation);
			float3 @float;
			quaternion quaternion2;
			float3 float2;
			if (proceduralBone.m_ObjectPosition.z >= 0f == curReversed)
			{
				if (nextGeometryData.m_Bounds.max.z == nextGeometryData.m_Bounds.min.z)
				{
					skeleton.m_CurrentUpdated |= !bone.m_Position.Equals(proceduralBone.m_Position) | !bone.m_Rotation.Equals(proceduralBone.m_Rotation);
					bone.m_Position = proceduralBone.m_Position;
					bone.m_Rotation = proceduralBone.m_Rotation;
					return;
				}
				@float = new float3(proceduralBone.m_Position.xy, math.select(curGeometryData.m_Bounds.min.z, curGeometryData.m_Bounds.max.z, curReversed));
				float2 = math.rotate(v: new float3(proceduralBone.m_Position.xy, math.select(nextGeometryData.m_Bounds.max.z, nextGeometryData.m_Bounds.min.z, nextReversed)), q: nextTransform.m_Rotation);
				float2 = math.rotate(quaternion, float2 + (nextTransform.m_Position - curTransform.m_Position));
				quaternion2 = math.mul(quaternion, nextTransform.m_Rotation);
				if (nextReversed != curReversed)
				{
					quaternion2 = math.mul(quaternion2, quaternion.RotateY(MathF.PI));
				}
			}
			else
			{
				if (prevGeometryData.m_Bounds.max.z == prevGeometryData.m_Bounds.min.z)
				{
					skeleton.m_CurrentUpdated |= !bone.m_Position.Equals(proceduralBone.m_Position) | !bone.m_Rotation.Equals(proceduralBone.m_Rotation);
					bone.m_Position = proceduralBone.m_Position;
					bone.m_Rotation = proceduralBone.m_Rotation;
					return;
				}
				@float = new float3(proceduralBone.m_Position.xy, math.select(curGeometryData.m_Bounds.max.z, curGeometryData.m_Bounds.min.z, curReversed));
				float2 = math.rotate(v: new float3(proceduralBone.m_Position.xy, math.select(prevGeometryData.m_Bounds.min.z, prevGeometryData.m_Bounds.max.z, prevReversed)), q: prevTransform.m_Rotation);
				float2 = math.rotate(quaternion, float2 + (prevTransform.m_Position - curTransform.m_Position));
				quaternion2 = math.mul(quaternion, prevTransform.m_Rotation);
				if (prevReversed != curReversed)
				{
					quaternion2 = math.mul(quaternion2, quaternion.RotateY(MathF.PI));
				}
			}
			float num = math.sign(@float.z) * math.distance(@float, float2) * 0.25f;
			@float.z += num;
			float2 += math.rotate(quaternion2, new float3(0f, 0f, 0f - num));
			float3 float3 = (@float + float2) * 0.5f;
			quaternion quaternion3 = math.slerp(quaternion.identity, quaternion2, 0.5f);
			skeleton.m_CurrentUpdated |= !bone.m_Position.Equals(float3) | !bone.m_Rotation.Equals(quaternion3);
			bone.m_Position = float3;
			bone.m_Rotation = quaternion3;
		}

		private void AnimateTrainBogieBone(Transform transform, ProceduralBone proceduralBone, ref Skeleton skeleton, ref Bone bone, float3 position, float3 tangent, bool positionValid)
		{
			float3 @float = proceduralBone.m_Position;
			quaternion quaternion = proceduralBone.m_Rotation;
			if (positionValid)
			{
				quaternion quaternion2 = math.inverse(transform.m_Rotation);
				@float = math.rotate(quaternion2, position - transform.m_Position);
				@float.y += proceduralBone.m_Position.y;
				tangent = math.select(test: math.dot(tangent, math.forward(math.mul(transform.m_Rotation, proceduralBone.m_ObjectRotation))) < 0f, falseValue: tangent, trueValue: -tangent);
				quaternion = math.mul(quaternion2, quaternion.LookRotationSafe(tangent, math.up()));
			}
			skeleton.m_CurrentUpdated |= !bone.m_Position.Equals(@float) | !bone.m_Rotation.Equals(quaternion);
			bone.m_Position = @float;
			bone.m_Rotation = quaternion;
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
		public ComponentTypeHandle<Swaying> m_SwayingType;

		[ReadOnly]
		public ComponentTypeHandle<Static> m_StaticType;

		[ReadOnly]
		public ComponentTypeHandle<Stopped> m_StoppedType;

		[ReadOnly]
		public ComponentTypeHandle<Temp> m_TempType;

		[ReadOnly]
		public ComponentTypeHandle<Game.Tools.Animation> m_AnimationType;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> m_PrefabRefType;

		[ReadOnly]
		public BufferTypeHandle<TransformFrame> m_TransformFrameType;

		[ReadOnly]
		public BufferTypeHandle<MeshGroup> m_MeshGroupType;

		[ReadOnly]
		public BufferTypeHandle<EnabledEffect> m_EffectInstancesType;

		[ReadOnly]
		public BufferTypeHandle<IconElement> m_IconElementType;

		[ReadOnly]
		public BufferLookup<SubMesh> m_SubMeshes;

		[ReadOnly]
		public BufferLookup<ProceduralLight> m_ProceduralLights;

		[ReadOnly]
		public BufferLookup<CharacterElement> m_CharacterElements;

		[ReadOnly]
		public BufferLookup<AnimationClip> m_AnimationClips;

		[NativeDisableParallelForRestriction]
		public ComponentLookup<Transform> m_TransformData;

		[NativeDisableParallelForRestriction]
		public ComponentLookup<InterpolatedTransform> m_InterpolatedTransformData;

		[NativeDisableParallelForRestriction]
		public BufferLookup<Animated> m_Animateds;

		[NativeDisableParallelForRestriction]
		public BufferLookup<Skeleton> m_Skeletons;

		[NativeDisableParallelForRestriction]
		public BufferLookup<Emissive> m_Emissives;

		[NativeDisableParallelForRestriction]
		public BufferLookup<Bone> m_Bones;

		[NativeDisableParallelForRestriction]
		public BufferLookup<LightState> m_Lights;

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

		[ReadOnly]
		public NativeList<EnabledEffectData> m_EnabledData;

		[ReadOnly]
		public WaterSurfaceData<SurfaceWater> m_WaterSurfaceData;

		[ReadOnly]
		public TerrainHeightData m_TerrainHeightData;

		public AnimatedSystem.AnimationData m_AnimationData;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			NativeArray<Temp> nativeArray2 = chunk.GetNativeArray(ref m_TempType);
			NativeArray<CullingInfo> nativeArray3 = chunk.GetNativeArray(ref m_CullingInfoType);
			uint updateFrame = 0u;
			uint updateFrame2 = 0u;
			float framePosition = 0f;
			if (chunk.Has(m_UpdateFrameType))
			{
				uint index = chunk.GetSharedComponent(m_UpdateFrameType).m_Index;
				CalculateUpdateFrames(m_FrameIndex, m_FrameTime, index, out updateFrame, out updateFrame2, out framePosition);
			}
			if (nativeArray2.Length != 0)
			{
				NativeArray<Game.Tools.Animation> nativeArray4 = chunk.GetNativeArray(ref m_AnimationType);
				NativeArray<PrefabRef> nativeArray5 = chunk.GetNativeArray(ref m_PrefabRefType);
				BufferAccessor<MeshGroup> bufferAccessor = chunk.GetBufferAccessor(ref m_MeshGroupType);
				BufferAccessor<EnabledEffect> bufferAccessor2 = chunk.GetBufferAccessor(ref m_EffectInstancesType);
				bool flag = chunk.Has(ref m_SwayingType);
				bool flag2 = chunk.Has(ref m_StaticType);
				bool flag3 = chunk.Has(ref m_StoppedType);
				bool flag4 = chunk.Has(ref m_IconElementType);
				for (int i = 0; i < nativeArray.Length; i++)
				{
					Entity entity = nativeArray[i];
					Temp temp = nativeArray2[i];
					if (!flag4)
					{
						CullingInfo cullingInfo = nativeArray3[i];
						if (cullingInfo.m_CullingIndex == 0 || (m_CullingData[cullingInfo.m_CullingIndex].m_Flags & PreCullingFlags.NearCamera) == 0)
						{
							continue;
						}
					}
					if (m_InterpolatedTransformData.HasComponent(entity))
					{
						if (((flag2 || flag3) && (temp.m_Original == Entity.Null || (temp.m_Flags & (TempFlags.Create | TempFlags.Modify)) != 0)) || (temp.m_Flags & TempFlags.Dragging) != 0)
						{
							Game.Tools.Animation value;
							Transform transform = ((!CollectionUtils.TryGet(nativeArray4, i, out value)) ? m_TransformData[entity] : value.ToTransform());
							if (flag)
							{
								transform.m_Position.y = WaterUtils.SampleHeight(ref m_WaterSurfaceData, ref m_TerrainHeightData, transform.m_Position);
							}
							m_InterpolatedTransformData[entity] = new InterpolatedTransform(transform);
							if (!m_Animateds.HasBuffer(entity))
							{
								continue;
							}
							PrefabRef prefabRef = nativeArray5[i];
							DynamicBuffer<Animated> dynamicBuffer = m_Animateds[entity];
							CollectionUtils.TryGet(bufferAccessor, i, out var value2);
							m_CharacterElements.TryGetBuffer(prefabRef.m_Prefab, out var bufferData);
							for (int j = 0; j < dynamicBuffer.Length; j++)
							{
								Animated value3 = dynamicBuffer[j];
								if (value3.m_ClipIndexBody0 != -1)
								{
									value3.m_ClipIndexBody0 = 0;
									value3.m_Time = 0f;
									value3.m_MovementSpeed = 0f;
									value3.m_Interpolation = 0f;
									dynamicBuffer[j] = value3;
								}
								if (value3.m_MetaIndex != 0 && bufferData.IsCreated)
								{
									CollectionUtils.TryGet(value2, j, out var value4);
									CharacterElement characterElement = bufferData[value4.m_SubMeshGroup];
									Animated animated = new Animated
									{
										m_MetaIndex = value3.m_MetaIndex,
										m_ClipIndexBody0 = -1,
										m_ClipIndexBody0I = -1,
										m_ClipIndexBody1 = -1,
										m_ClipIndexBody1I = -1,
										m_ClipIndexFace0 = -1,
										m_ClipIndexFace1 = -1
									};
									DynamicBuffer<AnimationClip> clips = m_AnimationClips[characterElement.m_Style];
									m_AnimationData.SetAnimationFrame(characterElement.m_Style, characterElement.m_RestPoseClipIndex, characterElement.m_CorrectiveClipIndex, clips, in animated, 0f, -1, reset: true);
								}
							}
							continue;
						}
						if (m_TransformData.TryGetComponent(temp.m_Original, out var componentData))
						{
							m_TransformData[entity] = componentData;
							if (m_InterpolatedTransformData.TryGetComponent(temp.m_Original, out var componentData2))
							{
								m_InterpolatedTransformData[entity] = componentData2;
							}
							else
							{
								m_InterpolatedTransformData[entity] = new InterpolatedTransform(componentData);
							}
						}
						else
						{
							m_InterpolatedTransformData[entity] = new InterpolatedTransform(m_TransformData[entity]);
						}
					}
					if (m_Animateds.HasBuffer(entity))
					{
						PrefabRef prefabRef2 = nativeArray5[i];
						DynamicBuffer<Animated> dynamicBuffer2 = m_Animateds[entity];
						bool reset = false;
						CullingInfo cullingInfo2 = nativeArray3[i];
						if (cullingInfo2.m_CullingIndex != 0)
						{
							reset = (m_CullingData[cullingInfo2.m_CullingIndex].m_Flags & (PreCullingFlags.NearCameraUpdated | PreCullingFlags.Updated | PreCullingFlags.BatchesUpdated)) != 0;
						}
						float num = RenderingUtils.CalculateMinDistance(cullingInfo2.m_Bounds, m_CameraPosition, m_CameraDirection, m_LodParameters);
						int priority = RenderingUtils.CalculateLod(num * num, m_LodParameters) - cullingInfo2.m_MinLod;
						CollectionUtils.TryGet(bufferAccessor, i, out var value5);
						m_CharacterElements.TryGetBuffer(prefabRef2.m_Prefab, out var bufferData2);
						if (m_Animateds.TryGetBuffer(temp.m_Original, out var bufferData3) && bufferData3.Length == dynamicBuffer2.Length)
						{
							for (int k = 0; k < dynamicBuffer2.Length; k++)
							{
								Animated animated2 = dynamicBuffer2[k];
								Animated animated3 = bufferData3[k];
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
								if (animated2.m_MetaIndex != 0 && bufferData2.IsCreated)
								{
									CollectionUtils.TryGet(value5, k, out var value6);
									CharacterElement characterElement2 = bufferData2[value6.m_SubMeshGroup];
									float num2 = framePosition * framePosition;
									num2 = 3f * num2 - 2f * num2 * framePosition;
									DynamicBuffer<AnimationClip> clips2 = m_AnimationClips[characterElement2.m_Style];
									m_AnimationData.SetAnimationFrame(characterElement2.m_Style, characterElement2.m_RestPoseClipIndex, characterElement2.m_CorrectiveClipIndex, clips2, in animated2, num2, priority, reset);
								}
							}
						}
						else
						{
							for (int l = 0; l < dynamicBuffer2.Length; l++)
							{
								Animated value7 = dynamicBuffer2[l];
								if (value7.m_ClipIndexBody0 != -1)
								{
									value7.m_ClipIndexBody0 = 0;
									value7.m_Time = 0f;
									value7.m_MovementSpeed = 0f;
									value7.m_Interpolation = 0f;
									dynamicBuffer2[l] = value7;
								}
								if (value7.m_MetaIndex != 0 && bufferData2.IsCreated)
								{
									CollectionUtils.TryGet(value5, l, out var value8);
									CharacterElement characterElement3 = bufferData2[value8.m_SubMeshGroup];
									Animated animated4 = new Animated
									{
										m_MetaIndex = value7.m_MetaIndex,
										m_ClipIndexBody0 = -1,
										m_ClipIndexBody0I = -1,
										m_ClipIndexBody1 = -1,
										m_ClipIndexBody1I = -1,
										m_ClipIndexFace0 = -1,
										m_ClipIndexFace1 = -1
									};
									DynamicBuffer<AnimationClip> clips3 = m_AnimationClips[characterElement3.m_Style];
									m_AnimationData.SetAnimationFrame(characterElement3.m_Style, characterElement3.m_RestPoseClipIndex, characterElement3.m_CorrectiveClipIndex, clips3, in animated4, 0f, -1, reset);
								}
							}
						}
					}
					if (m_Bones.HasBuffer(entity))
					{
						DynamicBuffer<Skeleton> dynamicBuffer3 = m_Skeletons[entity];
						DynamicBuffer<Bone> dynamicBuffer4 = m_Bones[entity];
						if (m_Bones.HasBuffer(temp.m_Original))
						{
							DynamicBuffer<Bone> dynamicBuffer5 = m_Bones[temp.m_Original];
							if (dynamicBuffer4.Length == dynamicBuffer5.Length)
							{
								for (int m = 0; m < dynamicBuffer3.Length; m++)
								{
									dynamicBuffer3.ElementAt(m).m_CurrentUpdated = true;
								}
								for (int n = 0; n < dynamicBuffer4.Length; n++)
								{
									dynamicBuffer4[n] = dynamicBuffer5[n];
								}
							}
						}
					}
					if (!m_Lights.HasBuffer(entity))
					{
						continue;
					}
					DynamicBuffer<Emissive> dynamicBuffer6 = m_Emissives[entity];
					DynamicBuffer<LightState> dynamicBuffer7 = m_Lights[entity];
					DynamicBuffer<EnabledEffect> dynamicBuffer8 = default(DynamicBuffer<EnabledEffect>);
					if (bufferAccessor2.Length != 0)
					{
						dynamicBuffer8 = bufferAccessor2[i];
					}
					PrefabRef prefabRef3 = nativeArray5[i];
					DynamicBuffer<SubMesh> dynamicBuffer9 = m_SubMeshes[prefabRef3.m_Prefab];
					DynamicBuffer<LightState> dynamicBuffer10 = default(DynamicBuffer<LightState>);
					bool flag5 = false;
					if (m_Lights.HasBuffer(temp.m_Original))
					{
						dynamicBuffer10 = m_Lights[temp.m_Original];
						flag5 = dynamicBuffer10.Length == dynamicBuffer7.Length;
					}
					for (int num3 = 0; num3 < dynamicBuffer6.Length; num3++)
					{
						ref Emissive reference = ref dynamicBuffer6.ElementAt(num3);
						if (reference.m_BufferAllocation.Empty)
						{
							continue;
						}
						reference.m_Updated = true;
						SubMesh subMesh = dynamicBuffer9[num3];
						DynamicBuffer<ProceduralLight> dynamicBuffer11 = m_ProceduralLights[subMesh.m_SubMesh];
						for (int num4 = 0; num4 < dynamicBuffer11.Length; num4++)
						{
							int index2 = reference.m_LightOffset + num4;
							ProceduralLight proceduralLight = dynamicBuffer11[num4];
							ref LightState reference2 = ref dynamicBuffer7.ElementAt(index2);
							if (proceduralLight.m_Purpose == EmissiveProperties.Purpose.EffectSource)
							{
								if (dynamicBuffer8.IsCreated)
								{
									reference2.m_Intensity = 0f;
									int num5 = 0;
									if (dynamicBuffer8.Length > num5)
									{
										EnabledEffect enabledEffect = dynamicBuffer8[num5];
										reference2.m_Intensity = math.select(0f, 1f, (m_EnabledData[enabledEffect.m_EnabledIndex].m_Flags & EnabledEffectFlags.IsEnabled) != 0);
									}
								}
							}
							else if (flag5)
							{
								reference2 = dynamicBuffer10[index2];
							}
						}
					}
				}
				return;
			}
			BufferAccessor<TransformFrame> bufferAccessor3 = chunk.GetBufferAccessor(ref m_TransformFrameType);
			for (int num6 = 0; num6 < nativeArray.Length; num6++)
			{
				CullingInfo cullingInfo3 = nativeArray3[num6];
				if (cullingInfo3.m_CullingIndex == 0 || (m_CullingData[cullingInfo3.m_CullingIndex].m_Flags & PreCullingFlags.NearCamera) == 0)
				{
					DynamicBuffer<TransformFrame> dynamicBuffer12 = bufferAccessor3[num6];
					TransformFrame frame = dynamicBuffer12[(int)updateFrame];
					TransformFrame frame2 = dynamicBuffer12[(int)updateFrame2];
					m_InterpolatedTransformData[nativeArray[num6]] = CalculateTransform(frame, frame2, framePosition);
				}
			}
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	private struct CatenaryIterator : INativeQuadTreeIterator<Entity, QuadTreeBoundsXZ>, IUnsafeQuadTreeIterator<Entity, QuadTreeBoundsXZ>
	{
		public Bounds3 m_Bounds;

		public Line3.Segment m_Line;

		public float3 m_Result;

		public float m_Default;

		public ComponentLookup<Curve> m_CurveData;

		public ComponentLookup<PrefabRef> m_PrefabRefData;

		public ComponentLookup<UtilityLaneData> m_PrefabUtilityLaneData;

		public bool Intersect(QuadTreeBoundsXZ bounds)
		{
			return MathUtils.Intersect(m_Bounds, bounds.m_Bounds);
		}

		public void Iterate(QuadTreeBoundsXZ bounds, Entity item)
		{
			if (MathUtils.Intersect(m_Bounds, bounds.m_Bounds))
			{
				PrefabRef prefabRef = m_PrefabRefData[item];
				if (m_PrefabUtilityLaneData.TryGetComponent(prefabRef.m_Prefab, out var componentData) && (componentData.m_UtilityTypes & (UtilityTypes.LowVoltageLine | UtilityTypes.Catenary)) != UtilityTypes.None)
				{
					Curve curve = m_CurveData[item];
					MathUtils.Distance(curve.m_Bezier, MathUtils.Position(m_Line, 0.5f), out var t);
					float3 position = MathUtils.Position(curve.m_Bezier, t);
					float num = math.max(0f, MathUtils.Distance(m_Line, position, out t) - m_Default * 0.5f);
					float num2 = t * m_Default * 2f;
					float3 trueValue = new float3(num2, num, num2 + num);
					m_Result = math.select(m_Result, trueValue, trueValue.z < m_Result.z);
				}
			}
		}
	}

	private struct TypeHandle
	{
		[ReadOnly]
		public ComponentLookup<Owner> __Game_Common_Owner_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PseudoRandomSeed> __Game_Common_PseudoRandomSeed_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<CullingInfo> __Game_Rendering_CullingInfo_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PointOfInterest> __Game_Common_PointOfInterest_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Destroyed> __Game_Common_Destroyed_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Attachment> __Game_Objects_Attachment_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Transform> __Game_Objects_Transform_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<TrafficLight> __Game_Objects_TrafficLight_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<Efficiency> __Game_Buildings_Efficiency_RO_BufferLookup;

		[ReadOnly]
		public ComponentLookup<ElectricityConsumer> __Game_Buildings_ElectricityConsumer_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Buildings.ExtractorFacility> __Game_Buildings_ExtractorFacility_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Building> __Game_Buildings_Building_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Vehicle> __Game_Vehicles_Vehicle_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<ParkedCar> __Game_Vehicles_ParkedCar_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<ParkedTrain> __Game_Vehicles_ParkedTrain_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Car> __Game_Vehicles_Car_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<CarTrailer> __Game_Vehicles_CarTrailer_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Controller> __Game_Vehicles_Controller_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Watercraft> __Game_Vehicles_Watercraft_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Human> __Game_Creatures_Human_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<CurrentVehicle> __Game_Creatures_CurrentVehicle_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Curve> __Game_Net_Curve_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Moving> __Game_Objects_Moving_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<SwayingData> __Game_Prefabs_SwayingData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<ObjectGeometryData> __Game_Prefabs_ObjectGeometryData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<UtilityLaneData> __Game_Prefabs_UtilityLaneData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<CarData> __Game_Prefabs_CarData_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<TransformFrame> __Game_Objects_TransformFrame_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<MeshGroup> __Game_Rendering_MeshGroup_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<EnabledEffect> __Game_Effects_EnabledEffect_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<AnimationClip> __Game_Prefabs_AnimationClip_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<AnimationMotion> __Game_Prefabs_AnimationMotion_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<ProceduralBone> __Game_Prefabs_ProceduralBone_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<ProceduralLight> __Game_Prefabs_ProceduralLight_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<LightAnimation> __Game_Prefabs_LightAnimation_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<SubMesh> __Game_Prefabs_SubMesh_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<SubMeshGroup> __Game_Prefabs_SubMeshGroup_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<CharacterElement> __Game_Prefabs_CharacterElement_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<ActivityLocationElement> __Game_Prefabs_ActivityLocationElement_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<Passenger> __Game_Vehicles_Passenger_RO_BufferLookup;

		public ComponentLookup<InterpolatedTransform> __Game_Rendering_InterpolatedTransform_RW_ComponentLookup;

		public ComponentLookup<Swaying> __Game_Rendering_Swaying_RW_ComponentLookup;

		public BufferLookup<Skeleton> __Game_Rendering_Skeleton_RW_BufferLookup;

		public BufferLookup<Emissive> __Game_Rendering_Emissive_RW_BufferLookup;

		public BufferLookup<Animated> __Game_Rendering_Animated_RW_BufferLookup;

		public BufferLookup<Bone> __Game_Rendering_Bone_RW_BufferLookup;

		public BufferLookup<Momentum> __Game_Rendering_Momentum_RW_BufferLookup;

		public BufferLookup<PlaybackLayer> __Game_Rendering_PlaybackLayer_RW_BufferLookup;

		public BufferLookup<LightState> __Game_Rendering_LightState_RW_BufferLookup;

		[ReadOnly]
		public ComponentLookup<Train> __Game_Vehicles_Train_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<CarTractorData> __Game_Prefabs_CarTractorData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<CarTrailerData> __Game_Prefabs_CarTrailerData_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<LayoutElement> __Game_Vehicles_LayoutElement_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<TrainBogieFrame> __Game_Vehicles_TrainBogieFrame_RO_BufferLookup;

		[ReadOnly]
		public EntityTypeHandle __Unity_Entities_Entity_TypeHandle;

		public SharedComponentTypeHandle<UpdateFrame> __Game_Simulation_UpdateFrame_SharedComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<CullingInfo> __Game_Rendering_CullingInfo_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Swaying> __Game_Rendering_Swaying_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Static> __Game_Objects_Static_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Stopped> __Game_Objects_Stopped_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Temp> __Game_Tools_Temp_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Game.Tools.Animation> __Game_Tools_Animation_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentTypeHandle;

		[ReadOnly]
		public BufferTypeHandle<TransformFrame> __Game_Objects_TransformFrame_RO_BufferTypeHandle;

		[ReadOnly]
		public BufferTypeHandle<MeshGroup> __Game_Rendering_MeshGroup_RO_BufferTypeHandle;

		[ReadOnly]
		public BufferTypeHandle<EnabledEffect> __Game_Effects_EnabledEffect_RO_BufferTypeHandle;

		[ReadOnly]
		public BufferTypeHandle<IconElement> __Game_Notifications_IconElement_RO_BufferTypeHandle;

		public ComponentLookup<Transform> __Game_Objects_Transform_RW_ComponentLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Game_Common_Owner_RO_ComponentLookup = state.GetComponentLookup<Owner>(isReadOnly: true);
			__Game_Common_PseudoRandomSeed_RO_ComponentLookup = state.GetComponentLookup<PseudoRandomSeed>(isReadOnly: true);
			__Game_Rendering_CullingInfo_RO_ComponentLookup = state.GetComponentLookup<CullingInfo>(isReadOnly: true);
			__Game_Common_PointOfInterest_RO_ComponentLookup = state.GetComponentLookup<PointOfInterest>(isReadOnly: true);
			__Game_Common_Destroyed_RO_ComponentLookup = state.GetComponentLookup<Destroyed>(isReadOnly: true);
			__Game_Objects_Attachment_RO_ComponentLookup = state.GetComponentLookup<Attachment>(isReadOnly: true);
			__Game_Objects_Transform_RO_ComponentLookup = state.GetComponentLookup<Transform>(isReadOnly: true);
			__Game_Objects_TrafficLight_RO_ComponentLookup = state.GetComponentLookup<TrafficLight>(isReadOnly: true);
			__Game_Buildings_Efficiency_RO_BufferLookup = state.GetBufferLookup<Efficiency>(isReadOnly: true);
			__Game_Buildings_ElectricityConsumer_RO_ComponentLookup = state.GetComponentLookup<ElectricityConsumer>(isReadOnly: true);
			__Game_Buildings_ExtractorFacility_RO_ComponentLookup = state.GetComponentLookup<Game.Buildings.ExtractorFacility>(isReadOnly: true);
			__Game_Buildings_Building_RO_ComponentLookup = state.GetComponentLookup<Building>(isReadOnly: true);
			__Game_Vehicles_Vehicle_RO_ComponentLookup = state.GetComponentLookup<Vehicle>(isReadOnly: true);
			__Game_Vehicles_ParkedCar_RO_ComponentLookup = state.GetComponentLookup<ParkedCar>(isReadOnly: true);
			__Game_Vehicles_ParkedTrain_RO_ComponentLookup = state.GetComponentLookup<ParkedTrain>(isReadOnly: true);
			__Game_Vehicles_Car_RO_ComponentLookup = state.GetComponentLookup<Car>(isReadOnly: true);
			__Game_Vehicles_CarTrailer_RO_ComponentLookup = state.GetComponentLookup<CarTrailer>(isReadOnly: true);
			__Game_Vehicles_Controller_RO_ComponentLookup = state.GetComponentLookup<Controller>(isReadOnly: true);
			__Game_Vehicles_Watercraft_RO_ComponentLookup = state.GetComponentLookup<Watercraft>(isReadOnly: true);
			__Game_Creatures_Human_RO_ComponentLookup = state.GetComponentLookup<Human>(isReadOnly: true);
			__Game_Creatures_CurrentVehicle_RO_ComponentLookup = state.GetComponentLookup<CurrentVehicle>(isReadOnly: true);
			__Game_Net_Curve_RO_ComponentLookup = state.GetComponentLookup<Curve>(isReadOnly: true);
			__Game_Objects_Moving_RO_ComponentLookup = state.GetComponentLookup<Moving>(isReadOnly: true);
			__Game_Prefabs_SwayingData_RO_ComponentLookup = state.GetComponentLookup<SwayingData>(isReadOnly: true);
			__Game_Prefabs_PrefabRef_RO_ComponentLookup = state.GetComponentLookup<PrefabRef>(isReadOnly: true);
			__Game_Prefabs_ObjectGeometryData_RO_ComponentLookup = state.GetComponentLookup<ObjectGeometryData>(isReadOnly: true);
			__Game_Prefabs_UtilityLaneData_RO_ComponentLookup = state.GetComponentLookup<UtilityLaneData>(isReadOnly: true);
			__Game_Prefabs_CarData_RO_ComponentLookup = state.GetComponentLookup<CarData>(isReadOnly: true);
			__Game_Objects_TransformFrame_RO_BufferLookup = state.GetBufferLookup<TransformFrame>(isReadOnly: true);
			__Game_Rendering_MeshGroup_RO_BufferLookup = state.GetBufferLookup<MeshGroup>(isReadOnly: true);
			__Game_Effects_EnabledEffect_RO_BufferLookup = state.GetBufferLookup<EnabledEffect>(isReadOnly: true);
			__Game_Prefabs_AnimationClip_RO_BufferLookup = state.GetBufferLookup<AnimationClip>(isReadOnly: true);
			__Game_Prefabs_AnimationMotion_RO_BufferLookup = state.GetBufferLookup<AnimationMotion>(isReadOnly: true);
			__Game_Prefabs_ProceduralBone_RO_BufferLookup = state.GetBufferLookup<ProceduralBone>(isReadOnly: true);
			__Game_Prefabs_ProceduralLight_RO_BufferLookup = state.GetBufferLookup<ProceduralLight>(isReadOnly: true);
			__Game_Prefabs_LightAnimation_RO_BufferLookup = state.GetBufferLookup<LightAnimation>(isReadOnly: true);
			__Game_Prefabs_SubMesh_RO_BufferLookup = state.GetBufferLookup<SubMesh>(isReadOnly: true);
			__Game_Prefabs_SubMeshGroup_RO_BufferLookup = state.GetBufferLookup<SubMeshGroup>(isReadOnly: true);
			__Game_Prefabs_CharacterElement_RO_BufferLookup = state.GetBufferLookup<CharacterElement>(isReadOnly: true);
			__Game_Prefabs_ActivityLocationElement_RO_BufferLookup = state.GetBufferLookup<ActivityLocationElement>(isReadOnly: true);
			__Game_Vehicles_Passenger_RO_BufferLookup = state.GetBufferLookup<Passenger>(isReadOnly: true);
			__Game_Rendering_InterpolatedTransform_RW_ComponentLookup = state.GetComponentLookup<InterpolatedTransform>();
			__Game_Rendering_Swaying_RW_ComponentLookup = state.GetComponentLookup<Swaying>();
			__Game_Rendering_Skeleton_RW_BufferLookup = state.GetBufferLookup<Skeleton>();
			__Game_Rendering_Emissive_RW_BufferLookup = state.GetBufferLookup<Emissive>();
			__Game_Rendering_Animated_RW_BufferLookup = state.GetBufferLookup<Animated>();
			__Game_Rendering_Bone_RW_BufferLookup = state.GetBufferLookup<Bone>();
			__Game_Rendering_Momentum_RW_BufferLookup = state.GetBufferLookup<Momentum>();
			__Game_Rendering_PlaybackLayer_RW_BufferLookup = state.GetBufferLookup<PlaybackLayer>();
			__Game_Rendering_LightState_RW_BufferLookup = state.GetBufferLookup<LightState>();
			__Game_Vehicles_Train_RO_ComponentLookup = state.GetComponentLookup<Train>(isReadOnly: true);
			__Game_Prefabs_CarTractorData_RO_ComponentLookup = state.GetComponentLookup<CarTractorData>(isReadOnly: true);
			__Game_Prefabs_CarTrailerData_RO_ComponentLookup = state.GetComponentLookup<CarTrailerData>(isReadOnly: true);
			__Game_Vehicles_LayoutElement_RO_BufferLookup = state.GetBufferLookup<LayoutElement>(isReadOnly: true);
			__Game_Vehicles_TrainBogieFrame_RO_BufferLookup = state.GetBufferLookup<TrainBogieFrame>(isReadOnly: true);
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
			__Game_Simulation_UpdateFrame_SharedComponentTypeHandle = state.GetSharedComponentTypeHandle<UpdateFrame>();
			__Game_Rendering_CullingInfo_RO_ComponentTypeHandle = state.GetComponentTypeHandle<CullingInfo>(isReadOnly: true);
			__Game_Rendering_Swaying_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Swaying>(isReadOnly: true);
			__Game_Objects_Static_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Static>(isReadOnly: true);
			__Game_Objects_Stopped_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Stopped>(isReadOnly: true);
			__Game_Tools_Temp_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Temp>(isReadOnly: true);
			__Game_Tools_Animation_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Game.Tools.Animation>(isReadOnly: true);
			__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PrefabRef>(isReadOnly: true);
			__Game_Objects_TransformFrame_RO_BufferTypeHandle = state.GetBufferTypeHandle<TransformFrame>(isReadOnly: true);
			__Game_Rendering_MeshGroup_RO_BufferTypeHandle = state.GetBufferTypeHandle<MeshGroup>(isReadOnly: true);
			__Game_Effects_EnabledEffect_RO_BufferTypeHandle = state.GetBufferTypeHandle<EnabledEffect>(isReadOnly: true);
			__Game_Notifications_IconElement_RO_BufferTypeHandle = state.GetBufferTypeHandle<IconElement>(isReadOnly: true);
			__Game_Objects_Transform_RW_ComponentLookup = state.GetComponentLookup<Transform>();
		}
	}

	private RenderingSystem m_RenderingSystem;

	private PreCullingSystem m_PreCullingSystem;

	private Game.Net.SearchSystem m_NetSearchSystem;

	private EffectControlSystem m_EffectControlSystem;

	private WindSystem m_WindSystem;

	private AnimatedSystem m_AnimatedSystem;

	private CameraUpdateSystem m_CameraUpdateSystem;

	private BatchDataSystem m_BatchDataSystem;

	private WaterSystem m_WaterSystem;

	private WaterRenderSystem m_WaterRenderSystem;

	private TerrainSystem m_TerrainSystem;

	private EntityQuery m_InterpolateQuery;

	private uint m_PrevFrameIndex;

	private TypeHandle __TypeHandle;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_RenderingSystem = base.World.GetOrCreateSystemManaged<RenderingSystem>();
		m_PreCullingSystem = base.World.GetOrCreateSystemManaged<PreCullingSystem>();
		m_NetSearchSystem = base.World.GetOrCreateSystemManaged<Game.Net.SearchSystem>();
		m_EffectControlSystem = base.World.GetOrCreateSystemManaged<EffectControlSystem>();
		m_WindSystem = base.World.GetOrCreateSystemManaged<WindSystem>();
		m_AnimatedSystem = base.World.GetOrCreateSystemManaged<AnimatedSystem>();
		m_CameraUpdateSystem = base.World.GetOrCreateSystemManaged<CameraUpdateSystem>();
		m_BatchDataSystem = base.World.GetOrCreateSystemManaged<BatchDataSystem>();
		m_WaterSystem = base.World.GetOrCreateSystemManaged<WaterSystem>();
		m_WaterRenderSystem = base.World.GetOrCreateSystemManaged<WaterRenderSystem>();
		m_TerrainSystem = base.World.GetOrCreateSystemManaged<TerrainSystem>();
		m_InterpolateQuery = GetEntityQuery(new EntityQueryDesc
		{
			All = new ComponentType[1] { ComponentType.ReadOnly<Temp>() },
			Any = new ComponentType[4]
			{
				ComponentType.ReadWrite<InterpolatedTransform>(),
				ComponentType.ReadWrite<Animated>(),
				ComponentType.ReadWrite<Bone>(),
				ComponentType.ReadWrite<LightState>()
			},
			None = new ComponentType[2]
			{
				ComponentType.ReadOnly<Deleted>(),
				ComponentType.ReadOnly<Relative>()
			}
		}, new EntityQueryDesc
		{
			All = new ComponentType[4]
			{
				ComponentType.ReadOnly<IconElement>(),
				ComponentType.ReadWrite<InterpolatedTransform>(),
				ComponentType.ReadOnly<UpdateFrame>(),
				ComponentType.ReadOnly<TransformFrame>()
			},
			None = new ComponentType[2]
			{
				ComponentType.ReadOnly<Deleted>(),
				ComponentType.ReadOnly<Relative>()
			}
		});
	}

	[Preserve]
	protected override void OnUpdate()
	{
		JobHandle dependencies;
		NativeQuadTree<Entity, QuadTreeBoundsXZ> laneSearchTree = m_NetSearchSystem.GetLaneSearchTree(readOnly: true, out dependencies);
		JobHandle dependencies2;
		NativeList<PreCullingData> cullingData = m_PreCullingSystem.GetCullingData(readOnly: true, out dependencies2);
		JobHandle dependencies3;
		NativeList<EnabledEffectData> enabledData = m_EffectControlSystem.GetEnabledData(readOnly: true, out dependencies3);
		JobHandle dependencies4;
		AnimatedSystem.AnimationData animationData = m_AnimatedSystem.GetAnimationData(out dependencies4);
		JobHandle deps;
		WaterSurfaceData<SurfaceWater> surfaceData = m_WaterSystem.GetSurfaceData(out deps);
		JobHandle deps2;
		WaterSurfaceData<SurfaceWater> velocitiesSurfaceData = m_WaterSystem.GetVelocitiesSurfaceData(out deps2);
		TerrainHeightData heightData = m_TerrainSystem.GetHeightData();
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
		JobHandle dependencies5;
		UpdateTransformDataJob jobData = new UpdateTransformDataJob
		{
			m_OwnerData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Common_Owner_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PseudoRandomSeedData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Common_PseudoRandomSeed_RO_ComponentLookup, ref base.CheckedStateRef),
			m_CullingInfoData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Rendering_CullingInfo_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PointOfInterestData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Common_PointOfInterest_RO_ComponentLookup, ref base.CheckedStateRef),
			m_DestroyedData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Common_Destroyed_RO_ComponentLookup, ref base.CheckedStateRef),
			m_AttachmentData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Attachment_RO_ComponentLookup, ref base.CheckedStateRef),
			m_TransformData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Transform_RO_ComponentLookup, ref base.CheckedStateRef),
			m_TrafficLightData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_TrafficLight_RO_ComponentLookup, ref base.CheckedStateRef),
			m_BuildingEfficiencyData = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Buildings_Efficiency_RO_BufferLookup, ref base.CheckedStateRef),
			m_BuildingElectricityConsumer = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_ElectricityConsumer_RO_ComponentLookup, ref base.CheckedStateRef),
			m_BuildingExtractorFacility = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_ExtractorFacility_RO_ComponentLookup, ref base.CheckedStateRef),
			m_BuildingData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_Building_RO_ComponentLookup, ref base.CheckedStateRef),
			m_VehicleData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Vehicles_Vehicle_RO_ComponentLookup, ref base.CheckedStateRef),
			m_ParkedCarData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Vehicles_ParkedCar_RO_ComponentLookup, ref base.CheckedStateRef),
			m_ParkedTrainData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Vehicles_ParkedTrain_RO_ComponentLookup, ref base.CheckedStateRef),
			m_CarData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Vehicles_Car_RO_ComponentLookup, ref base.CheckedStateRef),
			m_CarTrailerData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Vehicles_CarTrailer_RO_ComponentLookup, ref base.CheckedStateRef),
			m_ControllerData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Vehicles_Controller_RO_ComponentLookup, ref base.CheckedStateRef),
			m_WatercraftData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Vehicles_Watercraft_RO_ComponentLookup, ref base.CheckedStateRef),
			m_HumanData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Creatures_Human_RO_ComponentLookup, ref base.CheckedStateRef),
			m_CurrentVehicleData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Creatures_CurrentVehicle_RO_ComponentLookup, ref base.CheckedStateRef),
			m_CurveData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Curve_RO_ComponentLookup, ref base.CheckedStateRef),
			m_MovingData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Moving_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabSwayingData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_SwayingData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabRefData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabObjectGeometryData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_ObjectGeometryData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabUtilityLaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_UtilityLaneData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabCarData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_CarData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_TransformFrames = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Objects_TransformFrame_RO_BufferLookup, ref base.CheckedStateRef),
			m_MeshGroups = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Rendering_MeshGroup_RO_BufferLookup, ref base.CheckedStateRef),
			m_EffectInstances = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Effects_EnabledEffect_RO_BufferLookup, ref base.CheckedStateRef),
			m_AnimationClips = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Prefabs_AnimationClip_RO_BufferLookup, ref base.CheckedStateRef),
			m_AnimationMotions = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Prefabs_AnimationMotion_RO_BufferLookup, ref base.CheckedStateRef),
			m_ProceduralBones = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Prefabs_ProceduralBone_RO_BufferLookup, ref base.CheckedStateRef),
			m_ProceduralLights = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Prefabs_ProceduralLight_RO_BufferLookup, ref base.CheckedStateRef),
			m_LightAnimations = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Prefabs_LightAnimation_RO_BufferLookup, ref base.CheckedStateRef),
			m_SubMeshes = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Prefabs_SubMesh_RO_BufferLookup, ref base.CheckedStateRef),
			m_SubMeshGroups = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Prefabs_SubMeshGroup_RO_BufferLookup, ref base.CheckedStateRef),
			m_CharacterElements = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Prefabs_CharacterElement_RO_BufferLookup, ref base.CheckedStateRef),
			m_ActivityLocations = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Prefabs_ActivityLocationElement_RO_BufferLookup, ref base.CheckedStateRef),
			m_Passengers = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Vehicles_Passenger_RO_BufferLookup, ref base.CheckedStateRef),
			m_InterpolatedTransformData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Rendering_InterpolatedTransform_RW_ComponentLookup, ref base.CheckedStateRef),
			m_SwayingData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Rendering_Swaying_RW_ComponentLookup, ref base.CheckedStateRef),
			m_Skeletons = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Rendering_Skeleton_RW_BufferLookup, ref base.CheckedStateRef),
			m_Emissives = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Rendering_Emissive_RW_BufferLookup, ref base.CheckedStateRef),
			m_Animateds = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Rendering_Animated_RW_BufferLookup, ref base.CheckedStateRef),
			m_Bones = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Rendering_Bone_RW_BufferLookup, ref base.CheckedStateRef),
			m_Momentums = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Rendering_Momentum_RW_BufferLookup, ref base.CheckedStateRef),
			m_PlaybackLayers = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Rendering_PlaybackLayer_RW_BufferLookup, ref base.CheckedStateRef),
			m_Lights = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Rendering_LightState_RW_BufferLookup, ref base.CheckedStateRef),
			m_PrevFrameIndex = m_PrevFrameIndex,
			m_FrameIndex = m_RenderingSystem.frameIndex,
			m_FrameTime = m_RenderingSystem.frameTime,
			m_FrameDelta = m_RenderingSystem.frameDelta,
			m_TimeOfDay = m_RenderingSystem.timeOfDay,
			m_LodParameters = lodParameters,
			m_CameraPosition = cameraPosition,
			m_CameraDirection = cameraDirection,
			m_RandomSeed = RandomSeed.Next(),
			m_WindData = m_WindSystem.GetData(readOnly: true, out dependencies5),
			m_LaneSearchTree = laneSearchTree,
			m_CullingData = cullingData,
			m_EnabledData = enabledData,
			m_AnimationData = animationData,
			m_WaterSurfaceData = surfaceData,
			m_WaterVelocityData = velocitiesSurfaceData,
			m_WaterRenderSurfaceData = m_WaterRenderSystem.GetRenderSurfaceData(),
			m_requestedWaterHeightsWriter = m_WaterRenderSystem.m_RequestedPositions.AsParallelWriter(),
			m_TerrainHeightData = heightData
		};
		UpdateTrailerTransformDataJob jobData2 = new UpdateTrailerTransformDataJob
		{
			m_PointOfInterestData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Common_PointOfInterest_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PseudoRandomSeedData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Common_PseudoRandomSeed_RO_ComponentLookup, ref base.CheckedStateRef),
			m_TransformData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Transform_RO_ComponentLookup, ref base.CheckedStateRef),
			m_CurveData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Curve_RO_ComponentLookup, ref base.CheckedStateRef),
			m_CarData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Vehicles_Car_RO_ComponentLookup, ref base.CheckedStateRef),
			m_TrainData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Vehicles_Train_RO_ComponentLookup, ref base.CheckedStateRef),
			m_ParkedTrainData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Vehicles_ParkedTrain_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabRefData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabSwayingData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_SwayingData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabCarData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_CarData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabCarTractorData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_CarTractorData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabCarTrailerData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_CarTrailerData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabObjectGeometryData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_ObjectGeometryData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabUtilityLaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_UtilityLaneData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_TransformFrames = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Objects_TransformFrame_RO_BufferLookup, ref base.CheckedStateRef),
			m_LayoutElements = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Vehicles_LayoutElement_RO_BufferLookup, ref base.CheckedStateRef),
			m_BogieFrames = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Vehicles_TrainBogieFrame_RO_BufferLookup, ref base.CheckedStateRef),
			m_ProceduralBones = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Prefabs_ProceduralBone_RO_BufferLookup, ref base.CheckedStateRef),
			m_ProceduralLights = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Prefabs_ProceduralLight_RO_BufferLookup, ref base.CheckedStateRef),
			m_LightAnimations = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Prefabs_LightAnimation_RO_BufferLookup, ref base.CheckedStateRef),
			m_SubMeshes = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Prefabs_SubMesh_RO_BufferLookup, ref base.CheckedStateRef),
			m_InterpolatedTransformData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Rendering_InterpolatedTransform_RW_ComponentLookup, ref base.CheckedStateRef),
			m_SwayingData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Rendering_Swaying_RW_ComponentLookup, ref base.CheckedStateRef),
			m_Skeletons = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Rendering_Skeleton_RW_BufferLookup, ref base.CheckedStateRef),
			m_Emissive = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Rendering_Emissive_RW_BufferLookup, ref base.CheckedStateRef),
			m_Bones = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Rendering_Bone_RW_BufferLookup, ref base.CheckedStateRef),
			m_Momentums = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Rendering_Momentum_RW_BufferLookup, ref base.CheckedStateRef),
			m_LightStates = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Rendering_LightState_RW_BufferLookup, ref base.CheckedStateRef),
			m_FrameIndex = m_RenderingSystem.frameIndex,
			m_FrameTime = m_RenderingSystem.frameTime,
			m_FrameDelta = m_RenderingSystem.frameDelta,
			m_RandomSeed = RandomSeed.Next(),
			m_LaneSearchTree = laneSearchTree,
			m_CullingData = cullingData
		};
		UpdateQueryTransformDataJob jobData3 = new UpdateQueryTransformDataJob
		{
			m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
			m_UpdateFrameType = InternalCompilerInterface.GetSharedComponentTypeHandle(ref __TypeHandle.__Game_Simulation_UpdateFrame_SharedComponentTypeHandle, ref base.CheckedStateRef),
			m_CullingInfoType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Rendering_CullingInfo_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_SwayingType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Rendering_Swaying_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_StaticType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Objects_Static_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_StoppedType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Objects_Stopped_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_TempType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Tools_Temp_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_AnimationType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Tools_Animation_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_PrefabRefType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_TransformFrameType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Objects_TransformFrame_RO_BufferTypeHandle, ref base.CheckedStateRef),
			m_MeshGroupType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Rendering_MeshGroup_RO_BufferTypeHandle, ref base.CheckedStateRef),
			m_EffectInstancesType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Effects_EnabledEffect_RO_BufferTypeHandle, ref base.CheckedStateRef),
			m_IconElementType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Notifications_IconElement_RO_BufferTypeHandle, ref base.CheckedStateRef),
			m_SubMeshes = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Prefabs_SubMesh_RO_BufferLookup, ref base.CheckedStateRef),
			m_ProceduralLights = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Prefabs_ProceduralLight_RO_BufferLookup, ref base.CheckedStateRef),
			m_CharacterElements = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Prefabs_CharacterElement_RO_BufferLookup, ref base.CheckedStateRef),
			m_AnimationClips = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Prefabs_AnimationClip_RO_BufferLookup, ref base.CheckedStateRef),
			m_TransformData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Transform_RW_ComponentLookup, ref base.CheckedStateRef),
			m_InterpolatedTransformData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Rendering_InterpolatedTransform_RW_ComponentLookup, ref base.CheckedStateRef),
			m_Animateds = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Rendering_Animated_RW_BufferLookup, ref base.CheckedStateRef),
			m_Skeletons = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Rendering_Skeleton_RW_BufferLookup, ref base.CheckedStateRef),
			m_Emissives = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Rendering_Emissive_RW_BufferLookup, ref base.CheckedStateRef),
			m_Bones = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Rendering_Bone_RW_BufferLookup, ref base.CheckedStateRef),
			m_Lights = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Rendering_LightState_RW_BufferLookup, ref base.CheckedStateRef),
			m_FrameIndex = m_RenderingSystem.frameIndex,
			m_FrameTime = m_RenderingSystem.frameTime,
			m_LodParameters = lodParameters,
			m_CameraPosition = cameraPosition,
			m_CameraDirection = cameraDirection,
			m_CullingData = cullingData,
			m_EnabledData = enabledData,
			m_AnimationData = animationData,
			m_WaterSurfaceData = surfaceData,
			m_TerrainHeightData = heightData
		};
		JobHandle jobHandle = jobData.Schedule(cullingData, 16, JobUtils.CombineDependencies(base.Dependency, dependencies, dependencies2, dependencies3, dependencies5, dependencies4, deps, deps2));
		JobHandle jobHandle2 = jobData2.Schedule(cullingData, 16, jobHandle);
		JobHandle jobHandle3 = JobChunkExtensions.ScheduleParallel(jobData3, m_InterpolateQuery, jobHandle2);
		m_WaterRenderSystem.AddHeightReader(jobHandle);
		m_WindSystem.AddReader(jobHandle);
		m_NetSearchSystem.AddLaneSearchTreeReader(jobHandle2);
		m_PreCullingSystem.AddCullingDataReader(jobHandle3);
		m_EffectControlSystem.AddEnabledDataReader(jobHandle3);
		m_AnimatedSystem.AddAnimationWriter(jobHandle3);
		m_WaterSystem.AddSurfaceReader(jobHandle3);
		m_WaterSystem.AddVelocitySurfaceReader(jobHandle);
		m_TerrainSystem.AddCPUHeightReader(jobHandle3);
		base.Dependency = jobHandle3;
		m_PrevFrameIndex = m_RenderingSystem.frameIndex;
	}

	private static void UpdateSwaying(SwayingData swayingData, InterpolatedTransform oldTransform, ref InterpolatedTransform newTransform, ref Swaying swaying, float deltaTime, float speedDeltaFactor, bool localSway, out quaternion swayRotation, out float swayOffset)
	{
		if (deltaTime != 0f)
		{
			float3 position = oldTransform.m_Position;
			if (localSway)
			{
				position -= math.mul(oldTransform.m_Rotation, new float3(0f, swaying.m_SwayPosition.y, 0f));
			}
			else
			{
				position.y -= swaying.m_SwayPosition.y;
			}
			float3 @float = (newTransform.m_Position - position) * speedDeltaFactor;
			float3 v = @float - swaying.m_LastVelocity;
			v = math.mul(math.inverse(newTransform.m_Rotation), v);
			swaying.m_SwayVelocity += v * swayingData.m_VelocityFactors - swaying.m_SwayPosition * swayingData.m_SpringFactors * deltaTime;
			swaying.m_SwayVelocity *= math.pow(swayingData.m_DampingFactors, deltaTime);
			swaying.m_SwayPosition += swaying.m_SwayVelocity * deltaTime;
			swaying.m_SwayVelocity = math.select(swaying.m_SwayVelocity, 0f, ((swaying.m_SwayPosition >= swayingData.m_MaxPosition) & (swaying.m_SwayVelocity >= 0f)) | ((swaying.m_SwayPosition <= -swayingData.m_MaxPosition) & (swaying.m_SwayVelocity <= 0f)));
			swaying.m_SwayPosition = math.clamp(swaying.m_SwayPosition, -swayingData.m_MaxPosition, swayingData.m_MaxPosition);
			swaying.m_LastVelocity = @float;
		}
		float2 value = swaying.m_SwayPosition.xz;
		if (MathUtils.TryNormalize(ref value))
		{
			swayRotation = quaternion.AxisAngle(new float3(0f - value.y, 0f, value.x), math.length(swaying.m_SwayPosition.xz));
			newTransform.m_Rotation = math.mul(newTransform.m_Rotation, swayRotation);
			swayRotation = math.inverse(swayRotation);
		}
		else
		{
			swayRotation = quaternion.identity;
		}
		if (localSway)
		{
			newTransform.m_Position += math.mul(newTransform.m_Rotation, new float3(0f, swaying.m_SwayPosition.y, 0f));
		}
		else
		{
			newTransform.m_Position.y += swaying.m_SwayPosition.y;
		}
		swayOffset = 0f - swaying.m_SwayPosition.y;
	}

	private static float CalculateSteeringRadius(DynamicBuffer<ProceduralBone> proceduralBones, DynamicBuffer<Bone> bones, InterpolatedTransform oldTransform, InterpolatedTransform newTransform, ref Skeleton skeleton, CarData carData)
	{
		float num = float.PositiveInfinity;
		float num2 = -1f;
		float num3 = 0f;
		for (int i = 0; i < proceduralBones.Length; i++)
		{
			ProceduralBone proceduralBone = proceduralBones[i];
			int index = skeleton.m_BoneOffset + i;
			ref Bone reference = ref bones.ElementAt(index);
			BoneType type = proceduralBone.m_Type;
			ProceduralBone proceduralBone2;
			float3 @float;
			float3 float2;
			float num4;
			if (type != BoneType.SteeringTire)
			{
				if (type != BoneType.SteeringRotation)
				{
					if (type != BoneType.SteeringSuspension || !FindChildBone(proceduralBones, i, out var childIndex))
					{
						continue;
					}
					proceduralBone2 = proceduralBones[childIndex];
					if (FindChildBone(proceduralBones, childIndex, out var childIndex2))
					{
						proceduralBone2 = proceduralBones[childIndex2];
					}
					@float = ObjectUtils.LocalToWorld(oldTransform.ToTransform(), proceduralBone2.m_ObjectPosition);
					float2 = ObjectUtils.LocalToWorld(newTransform.ToTransform(), proceduralBone2.m_ObjectPosition);
					num4 = math.asin(math.mul(math.mul(math.inverse(proceduralBone.m_Rotation), reference.m_Rotation), math.left()).z);
				}
				else
				{
					if (!FindChildBone(proceduralBones, i, out var childIndex3))
					{
						continue;
					}
					proceduralBone2 = proceduralBones[childIndex3];
					if (FindChildBone(proceduralBones, childIndex3, out var childIndex4))
					{
						proceduralBone2 = proceduralBones[childIndex4];
					}
					@float = ObjectUtils.LocalToWorld(oldTransform.ToTransform(), proceduralBone2.m_ObjectPosition);
					float2 = ObjectUtils.LocalToWorld(newTransform.ToTransform(), proceduralBone2.m_ObjectPosition);
					num4 = math.asin(math.mul(math.mul(math.inverse(proceduralBone.m_Rotation), reference.m_Rotation), math.right()).y);
				}
			}
			else
			{
				@float = ObjectUtils.LocalToWorld(oldTransform.ToTransform(), proceduralBone.m_ObjectPosition);
				float2 = ObjectUtils.LocalToWorld(newTransform.ToTransform(), proceduralBone.m_ObjectPosition);
				proceduralBone2 = proceduralBone;
				num4 = math.asin(math.mul(math.mul(math.inverse(proceduralBone.m_Rotation), reference.m_Rotation), math.left()).z);
			}
			float2 x = new float2(proceduralBone2.m_ObjectPosition.x, proceduralBone2.m_ObjectPosition.z - carData.m_PivotOffset);
			x.y *= 0.5f;
			num3 = math.max(num3, math.csum(math.abs(x)));
			float3 x2 = float2 - @float;
			float3 float3 = math.mul(newTransform.m_Rotation, math.right());
			float3 float4 = math.forward(newTransform.m_Rotation);
			float num5 = math.dot(x2, float4);
			float num6 = math.dot(x2, float3);
			num5 += math.select(0.001f, -0.001f, num5 < 0f);
			float3 float5 = math.normalizesafe(float4 * num5 + float3 * num6);
			float5 = math.select(float5, -float5, num5 < 0f);
			float num7 = math.abs(math.dot(x2, float5));
			if (!(num7 <= num2))
			{
				num2 = num7;
				float num8 = math.asin(math.dot(float3, float5));
				float num9 = num7 / math.max(0.01f, proceduralBone2.m_ObjectPosition.y * 2f);
				num4 += math.clamp(num8 - num4, 0f - num9, num9);
				num = (proceduralBone2.m_ObjectPosition.z - carData.m_PivotOffset) / math.tan(num4) + proceduralBone2.m_ObjectPosition.x;
			}
		}
		num = math.select(num, num3, num < num3 && num >= 0f);
		return math.select(num, 0f - num3, num > 0f - num3 && num < 0f);
	}

	private static void AnimateInterpolatedBone(DynamicBuffer<ProceduralBone> proceduralBones, DynamicBuffer<Bone> bones, DynamicBuffer<Momentum> momentums, InterpolatedTransform oldTransform, InterpolatedTransform newTransform, PrefabRef prefabRef, ref Skeleton skeleton, quaternion swayRotation, float swayOffset, float steeringRadius, float pivotOffset, int index, float deltaTime, Entity entity, bool instantReset, uint frameIndex, float frameTime, ref Unity.Mathematics.Random random, ref ComponentLookup<PointOfInterest> pointOfInterests, ref ComponentLookup<Curve> curveDatas, ref ComponentLookup<PrefabRef> prefabRefDatas, ref ComponentLookup<UtilityLaneData> prefabUtilityLaneDatas, ref ComponentLookup<ObjectGeometryData> prefabObjectGeometryDatas, ref NativeQuadTree<Entity, QuadTreeBoundsXZ> laneSearchTree)
	{
		ProceduralBone proceduralBone = proceduralBones[index];
		Momentum momentum = default(Momentum);
		int index2 = skeleton.m_BoneOffset + index;
		ref Bone reference = ref bones.ElementAt(index2);
		ref Momentum momentum2 = ref momentum;
		if (momentums.IsCreated)
		{
			momentum2 = ref momentums.ElementAt(index2);
		}
		switch (proceduralBone.m_Type)
		{
		case BoneType.RollingTire:
		{
			float3 float6 = ObjectUtils.LocalToWorld(oldTransform.ToTransform(), proceduralBone.m_ObjectPosition);
			float3 x3 = ObjectUtils.LocalToWorld(newTransform.ToTransform(), proceduralBone.m_ObjectPosition) - float6;
			float3 y3 = math.forward(newTransform.m_Rotation);
			float num9 = math.dot(x3, y3) / math.max(0.01f, proceduralBone.m_ObjectPosition.y);
			float2 yz = math.forward(math.mul(math.inverse(proceduralBone.m_Rotation), reference.m_Rotation)).yz;
			float angle2 = num9 - math.atan2(yz.x, yz.y);
			float3 float7 = math.mul(swayRotation, proceduralBone.m_Position);
			float7.y += swayOffset;
			quaternion quaternion3 = math.mul(proceduralBone.m_Rotation, quaternion.RotateX(angle2));
			skeleton.m_CurrentUpdated |= !reference.m_Position.Equals(float7) | !reference.m_Rotation.Equals(quaternion3);
			reference.m_Position = float7;
			reference.m_Rotation = quaternion3;
			break;
		}
		case BoneType.SteeringTire:
		{
			float3 float18 = ObjectUtils.LocalToWorld(oldTransform.ToTransform(), proceduralBone.m_ObjectPosition);
			float3 x10 = ObjectUtils.LocalToWorld(newTransform.ToTransform(), proceduralBone.m_ObjectPosition) - float18;
			float3 float19 = math.mul(newTransform.m_Rotation, math.right());
			float3 float20 = math.forward(newTransform.m_Rotation);
			float num22 = math.dot(x10, float20);
			float num23 = math.dot(x10, float19);
			num22 += math.select(0.001f, -0.001f, num22 < 0f);
			float3 float21 = math.normalizesafe(float20 * num22 + float19 * num23);
			float21 = math.select(float21, -float21, num22 < 0f);
			float num24 = math.dot(x10, float21) / math.max(0.01f, proceduralBone.m_ObjectPosition.y);
			quaternion q = math.mul(math.inverse(proceduralBone.m_Rotation), reference.m_Rotation);
			float3 float22 = math.forward(q);
			float num25 = math.length(float22.xz);
			float angle6 = num24 - math.atan2(x: math.select(num25, 0f - num25, float22.z < 0f), y: float22.y);
			float num27;
			if (steeringRadius == 0f)
			{
				float num26 = math.asin(math.dot(float19, float21));
				num27 = math.asin(math.mul(q, math.left()).z);
				float num28 = math.length(x10) / math.max(0.01f, proceduralBone.m_ObjectPosition.y);
				num27 += math.clamp(num26 - num27, 0f - num28, num28);
			}
			else
			{
				num27 = math.atan((proceduralBone.m_ObjectPosition.z - pivotOffset) / (steeringRadius - proceduralBone.m_ObjectPosition.x));
			}
			float3 float23 = math.mul(swayRotation, proceduralBone.m_Position);
			float23.y += swayOffset;
			quaternion quaternion9 = math.mul(proceduralBone.m_Rotation, math.mul(quaternion.RotateY(num27), quaternion.RotateX(angle6)));
			skeleton.m_CurrentUpdated |= !reference.m_Position.Equals(float23) | !reference.m_Rotation.Equals(quaternion9);
			reference.m_Position = float23;
			reference.m_Rotation = quaternion9;
			break;
		}
		case BoneType.SuspensionMovement:
		{
			if (FindChildBone(proceduralBones, index, out var childIndex3))
			{
				ProceduralBone proceduralBone3 = proceduralBones[childIndex3];
				float3 position5 = proceduralBone.m_Position;
				position5.z += math.mul(swayRotation, proceduralBone3.m_ObjectPosition).y - proceduralBone3.m_ObjectPosition.y;
				position5.z += swayOffset;
				skeleton.m_CurrentUpdated |= !reference.m_Position.Equals(position5);
				reference.m_Position = position5;
			}
			break;
		}
		case BoneType.SteeringRotation:
		{
			if (FindChildBone(proceduralBones, index, out var childIndex4))
			{
				ProceduralBone proceduralBone4 = proceduralBones[childIndex4];
				if (FindChildBone(proceduralBones, childIndex4, out var childIndex5))
				{
					proceduralBone4 = proceduralBones[childIndex5];
				}
				float num20;
				if (steeringRadius == 0f)
				{
					float3 float14 = ObjectUtils.LocalToWorld(oldTransform.ToTransform(), proceduralBone4.m_ObjectPosition);
					float3 x9 = ObjectUtils.LocalToWorld(newTransform.ToTransform(), proceduralBone4.m_ObjectPosition) - float14;
					float3 float15 = math.mul(newTransform.m_Rotation, math.right());
					float3 float16 = math.forward(newTransform.m_Rotation);
					float num17 = math.dot(x9, float16);
					float num18 = math.dot(x9, float15);
					num17 += math.select(0.001f, -0.001f, num17 < 0f);
					float3 float17 = math.normalizesafe(float16 * num17 + float15 * num18);
					float17 = math.select(float17, -float17, num17 < 0f);
					float num19 = math.asin(math.dot(float15, float17));
					num20 = math.asin(math.mul(math.mul(math.inverse(proceduralBone.m_Rotation), reference.m_Rotation), math.right()).y);
					float num21 = math.length(x9) / math.max(0.01f, proceduralBone4.m_ObjectPosition.y);
					num20 += math.clamp(num19 - num20, 0f - num21, num21);
				}
				else
				{
					num20 = math.atan((proceduralBone4.m_ObjectPosition.z - pivotOffset) / (steeringRadius - proceduralBone4.m_ObjectPosition.x));
				}
				quaternion quaternion7 = math.mul(proceduralBone.m_Rotation, quaternion.RotateZ(num20));
				skeleton.m_CurrentUpdated |= !reference.m_Rotation.Equals(quaternion7);
				reference.m_Rotation = quaternion7;
			}
			break;
		}
		case BoneType.SuspensionRotation:
		{
			if (FindChildBone(proceduralBones, index, out var childIndex6))
			{
				ProceduralBone proceduralBone5 = proceduralBones[childIndex6];
				float angle5 = 0f - math.atan((math.mul(swayRotation, proceduralBone5.m_ObjectPosition).y - proceduralBone5.m_ObjectPosition.y + swayOffset) / proceduralBone5.m_Position.z);
				quaternion quaternion8 = math.mul(proceduralBone.m_Rotation, quaternion.RotateX(angle5));
				skeleton.m_CurrentUpdated |= !reference.m_Rotation.Equals(quaternion8);
				reference.m_Rotation = quaternion8;
			}
			break;
		}
		case BoneType.FixedRotation:
		{
			ProceduralBone proceduralBone6 = proceduralBones[proceduralBone.m_ParentIndex];
			Bone bone = bones.ElementAt(skeleton.m_BoneOffset + proceduralBone.m_ParentIndex);
			quaternion quaternion10 = math.mul(math.inverse(LocalToObject(proceduralBones, bones, skeleton, proceduralBone6.m_ParentIndex, bone.m_Rotation)), proceduralBone.m_ObjectRotation);
			skeleton.m_CurrentUpdated |= !reference.m_Rotation.Equals(quaternion10);
			reference.m_Rotation = quaternion10;
			break;
		}
		case BoneType.FixedTire:
		{
			float3 float13 = ObjectUtils.LocalToWorld(oldTransform.ToTransform(), proceduralBone.m_ObjectPosition);
			float3 x7 = ObjectUtils.LocalToWorld(newTransform.ToTransform(), proceduralBone.m_ObjectPosition) - float13;
			float3 x8 = math.rotate(LocalToWorld(proceduralBones, bones, newTransform.ToTransform(), skeleton, proceduralBone.m_ParentIndex, proceduralBone.m_Rotation), math.right());
			float3 y6 = math.rotate(newTransform.m_Rotation, math.up());
			float3 y7 = math.normalizesafe(math.cross(x8, y6));
			float num16 = math.dot(x7, y7) / math.max(0.01f, proceduralBone.m_ObjectPosition.y);
			float2 yz3 = math.forward(math.mul(math.inverse(proceduralBone.m_Rotation), reference.m_Rotation)).yz;
			float angle4 = num16 - math.atan2(yz3.x, yz3.y);
			quaternion quaternion6 = math.mul(proceduralBone.m_Rotation, quaternion.RotateX(angle4));
			skeleton.m_CurrentUpdated |= !reference.m_Rotation.Equals(quaternion6);
			reference.m_Rotation = quaternion6;
			break;
		}
		case BoneType.DebugMovement:
		{
			float3 position3 = proceduralBone.m_Position;
			float num8 = ((float)(frameIndex & 0xFF) + frameTime) * (3f / 128f);
			if (num8 < 1f)
			{
				position3.x += math.smoothstep(0f, 1f, num8);
			}
			else if (num8 < 2f)
			{
				position3.x += math.smoothstep(2f, 1f, num8);
			}
			else if (num8 < 3f)
			{
				position3.y += math.smoothstep(2f, 3f, num8);
			}
			else if (num8 < 4f)
			{
				position3.y += math.smoothstep(4f, 3f, num8);
			}
			else if (num8 < 5f)
			{
				position3.z += math.smoothstep(4f, 5f, num8);
			}
			else
			{
				position3.z += math.smoothstep(6f, 5f, num8);
			}
			skeleton.m_CurrentUpdated |= !reference.m_Position.Equals(position3);
			reference.m_Position = position3;
			break;
		}
		case BoneType.RollingRotation:
		{
			float3 float8 = ObjectUtils.LocalToWorld(oldTransform.ToTransform(), proceduralBone.m_ObjectPosition);
			float3 x4 = ObjectUtils.LocalToWorld(newTransform.ToTransform(), proceduralBone.m_ObjectPosition) - float8;
			float3 x5 = math.rotate(LocalToWorld(proceduralBones, bones, newTransform.ToTransform(), skeleton, proceduralBone.m_ParentIndex, proceduralBone.m_Rotation), math.right());
			float3 y4 = math.rotate(newTransform.m_Rotation, math.up());
			float3 y5 = math.normalizesafe(math.cross(x5, y4));
			float num10 = math.dot(x4, y5) * proceduralBone.m_Speed;
			float2 yz2 = math.forward(math.mul(math.inverse(proceduralBone.m_Rotation), reference.m_Rotation)).yz;
			float angle3 = num10 - math.atan2(yz2.x, yz2.y);
			quaternion quaternion4 = math.mul(proceduralBone.m_Rotation, quaternion.RotateX(angle3));
			skeleton.m_CurrentUpdated |= !reference.m_Rotation.Equals(quaternion4);
			reference.m_Rotation = quaternion4;
			break;
		}
		case BoneType.PropellerRotation:
		{
			float3 @float = ObjectUtils.LocalToWorld(oldTransform.ToTransform(), proceduralBone.m_ObjectPosition);
			float3 x = ObjectUtils.LocalToWorld(newTransform.ToTransform(), proceduralBone.m_ObjectPosition) - @float;
			float3 y = math.rotate(LocalToWorld(proceduralBones, bones, newTransform.ToTransform(), skeleton, proceduralBone.m_ParentIndex, proceduralBone.m_Rotation), math.up());
			float num = math.dot(x, y) * proceduralBone.m_Speed;
			float2 xz = math.forward(math.mul(math.inverse(proceduralBone.m_Rotation), reference.m_Rotation)).xz;
			float angle = num + math.atan2(xz.x, xz.y);
			quaternion quaternion = math.mul(proceduralBone.m_Rotation, quaternion.RotateY(angle));
			skeleton.m_CurrentUpdated |= !reference.m_Rotation.Equals(quaternion);
			reference.m_Rotation = quaternion;
			break;
		}
		case BoneType.PoweredRotation:
		case BoneType.OperatingRotation:
		{
			float speed = proceduralBone.m_Speed;
			AnimateRotatingBoneY(proceduralBone, ref skeleton, ref reference, ref momentum2, ref random, speed, deltaTime, instantReset);
			break;
		}
		case BoneType.PropellerAngle:
		{
			float3 float2 = ObjectUtils.LocalToWorld(oldTransform.ToTransform(), proceduralBone.m_ObjectPosition);
			float3 x2 = ObjectUtils.LocalToWorld(newTransform.ToTransform(), proceduralBone.m_ObjectPosition) - float2;
			float3 float3 = math.mul(newTransform.m_Rotation, math.right());
			float3 float4 = math.forward(newTransform.m_Rotation);
			float num2 = math.dot(x2, float4);
			float num3 = math.dot(x2, float3);
			num2 += math.select(0.001f, -0.001f, num2 < 0f);
			float3 y2 = math.normalizesafe(float4 * num2 + float3 * num3);
			float num4 = math.atan2(math.dot(float3, y2), math.dot(float4, y2));
			float3 float5 = math.mul(reference.m_Rotation, math.forward());
			float num5 = math.atan2(float5.x, float5.z);
			float num6 = math.length(x2) * proceduralBone.m_Speed;
			float num7 = num4 - num5;
			num7 = math.select(num7, num7 - MathF.PI, num7 > MathF.PI);
			num7 = math.select(num7, num7 + MathF.PI, num7 < -MathF.PI);
			num5 += math.clamp(num7, 0f - num6, num6);
			quaternion quaternion2 = math.mul(quaternion.RotateY(num5), proceduralBone.m_Rotation);
			skeleton.m_CurrentUpdated |= !reference.m_Rotation.Equals(quaternion2);
			reference.m_Rotation = quaternion2;
			break;
		}
		case BoneType.PantographRotation:
		{
			bool active = (newTransform.m_Flags & TransformFlags.Pantograph) != 0;
			AnimatePantographBone(proceduralBones, bones, newTransform.ToTransform(), prefabRef, proceduralBone, ref skeleton, ref reference, index, deltaTime, active, instantReset, ref curveDatas, ref prefabRefDatas, ref prefabUtilityLaneDatas, ref prefabObjectGeometryDatas, ref laneSearchTree);
			break;
		}
		case BoneType.SteeringSuspension:
		{
			if (FindChildBone(proceduralBones, index, out var childIndex))
			{
				ProceduralBone proceduralBone2 = proceduralBones[childIndex];
				if (FindChildBone(proceduralBones, childIndex, out var childIndex2))
				{
					proceduralBone2 = proceduralBones[childIndex2];
				}
				float num14;
				if (steeringRadius == 0f)
				{
					float3 float9 = ObjectUtils.LocalToWorld(oldTransform.ToTransform(), proceduralBone2.m_ObjectPosition);
					float3 x6 = ObjectUtils.LocalToWorld(newTransform.ToTransform(), proceduralBone2.m_ObjectPosition) - float9;
					float3 float10 = math.mul(newTransform.m_Rotation, math.right());
					float3 float11 = math.forward(newTransform.m_Rotation);
					float num11 = math.dot(x6, float11);
					float num12 = math.dot(x6, float10);
					num11 += math.select(0.001f, -0.001f, num11 < 0f);
					float3 float12 = math.normalizesafe(float11 * num11 + float10 * num12);
					float12 = math.select(float12, -float12, num11 < 0f);
					float num13 = math.asin(math.dot(float10, float12));
					num14 = math.asin(math.mul(math.mul(math.inverse(proceduralBone.m_Rotation), reference.m_Rotation), math.left()).z);
					float num15 = math.length(x6) / math.max(0.01f, proceduralBone2.m_ObjectPosition.y);
					num14 += math.clamp(num13 - num14, 0f - num15, num15);
				}
				else
				{
					num14 = math.atan((proceduralBone2.m_ObjectPosition.z - pivotOffset) / (steeringRadius - proceduralBone2.m_ObjectPosition.x));
				}
				quaternion quaternion5 = math.mul(proceduralBone.m_Rotation, quaternion.RotateY(num14));
				float3 position4 = proceduralBone.m_Position;
				position4.z += math.mul(swayRotation, proceduralBone2.m_ObjectPosition).y - proceduralBone2.m_ObjectPosition.y;
				position4.z += swayOffset;
				skeleton.m_CurrentUpdated |= !reference.m_Rotation.Equals(quaternion5) | !reference.m_Position.Equals(position4);
				reference.m_Rotation = quaternion5;
				reference.m_Position = position4;
			}
			break;
		}
		case BoneType.LookAtRotation:
		case BoneType.LookAtRotationSide:
		{
			if (pointOfInterests.TryGetComponent(entity, out var componentData2) && componentData2.m_IsValid)
			{
				float3 position2 = proceduralBone.m_Position;
				quaternion rotation2 = proceduralBone.m_Rotation;
				LocalToWorld(proceduralBones, bones, newTransform.ToTransform(), skeleton, proceduralBone.m_ParentIndex, ref position2, ref rotation2);
				float3 v2 = componentData2.m_Position - position2;
				v2 = math.mul(math.inverse(rotation2), v2);
				v2.xz = math.select(v2.xz, MathUtils.Right(v2.xz), proceduralBone.m_Type == BoneType.LookAtRotationSide);
				v2 = math.select(v2, -v2, proceduralBone.m_Speed < 0f);
				float targetSpeed2 = math.abs(proceduralBone.m_Speed);
				AnimateRotatingBoneY(proceduralBone, ref skeleton, ref reference, ref momentum2, ref random, v2.xz, targetSpeed2, deltaTime, instantReset);
			}
			else
			{
				AnimateRotatingBoneY(proceduralBone, ref skeleton, ref reference, ref momentum2, ref random, 0f, deltaTime, instantReset);
			}
			break;
		}
		case BoneType.LookAtAim:
		case BoneType.LookAtAimForward:
		{
			if (pointOfInterests.TryGetComponent(entity, out var componentData) && componentData.m_IsValid)
			{
				float3 position = proceduralBone.m_Position;
				quaternion rotation = proceduralBone.m_Rotation;
				LookAtLocalToWorld(proceduralBones, bones, newTransform.ToTransform(), skeleton, componentData, proceduralBone.m_ParentIndex, ref position, ref rotation);
				float3 v = componentData.m_Position - position;
				v = math.mul(math.inverse(rotation), v);
				v.yz = math.select(v.yz, MathUtils.Left(v.yz), proceduralBone.m_Type == BoneType.LookAtAimForward);
				v = math.select(v, -v, proceduralBone.m_Speed < 0f);
				float targetSpeed = math.abs(proceduralBone.m_Speed);
				AnimateRotatingBoneX(proceduralBone, ref skeleton, ref reference, ref momentum2, ref random, v.yz, targetSpeed, deltaTime, instantReset);
			}
			else
			{
				AnimateRotatingBoneX(proceduralBone, ref skeleton, ref reference, ref momentum2, ref random, 0f, deltaTime, instantReset);
			}
			break;
		}
		case BoneType.TrafficBarrierDirection:
		case BoneType.VehicleConnection:
		case BoneType.TrainBogie:
		case BoneType.LengthwiseLookAtRotation:
		case BoneType.WorkingRotation:
		case BoneType.TimeRotation:
		case BoneType.LookAtMovementX:
		case BoneType.LookAtMovementY:
		case BoneType.LookAtMovementZ:
		case BoneType.RotationXFromMovementY:
		case BoneType.ScaledMovement:
			break;
		}
	}

	private static void AnimatePantographBone(DynamicBuffer<ProceduralBone> proceduralBones, DynamicBuffer<Bone> bones, Transform transform, PrefabRef prefabRef, ProceduralBone proceduralBone, ref Skeleton skeleton, ref Bone bone, int index, float deltaTime, bool active, bool instantReset, ref ComponentLookup<Curve> curveDatas, ref ComponentLookup<PrefabRef> prefabRefDatas, ref ComponentLookup<UtilityLaneData> prefabUtilityLaneDatas, ref ComponentLookup<ObjectGeometryData> prefabObjectGeometryDatas, ref NativeQuadTree<Entity, QuadTreeBoundsXZ> laneSearchTree)
	{
		ProceduralBone proceduralBone2 = proceduralBones[proceduralBone.m_ParentIndex];
		quaternion quaternion2;
		int childIndex;
		if (proceduralBone2.m_Type == BoneType.PantographRotation)
		{
			Bone bone2 = bones.ElementAt(skeleton.m_BoneOffset + proceduralBone.m_ParentIndex);
			quaternion quaternion = math.mul(math.inverse(proceduralBone2.m_Rotation), bone2.m_Rotation);
			quaternion.value.x = 0f - quaternion.value.x;
			quaternion2 = math.mul(math.mul(quaternion, quaternion), proceduralBone.m_Rotation);
		}
		else if (FindChildBone(proceduralBones, index, out childIndex))
		{
			float num = 0f;
			if (active)
			{
				ProceduralBone proceduralBone3 = proceduralBones[childIndex];
				ObjectGeometryData objectGeometryData = prefabObjectGeometryDatas[prefabRef.m_Prefab];
				float3 objectPosition = proceduralBone.m_ObjectPosition;
				objectPosition.y = objectGeometryData.m_Bounds.max.y;
				objectPosition = ObjectUtils.LocalToWorld(transform, objectPosition);
				float num2 = math.length(proceduralBone3.m_Position.yz);
				if (proceduralBone3.m_Type == BoneType.PantographRotation && FindChildBone(proceduralBones, childIndex, out var childIndex2))
				{
					num2 += math.length(proceduralBones[childIndex2].m_Position.yz);
				}
				float defaultHeight = num2 * 0.38268343f;
				float num3 = FindCatenaryHeight(objectPosition, transform.m_Rotation, defaultHeight, ref curveDatas, ref prefabRefDatas, ref prefabUtilityLaneDatas, ref laneSearchTree);
				num = math.asin(math.min(0.9f, num3 / math.max(num2, 0.001f)));
				num = math.select(num, 0f - num, proceduralBone3.m_Position.z > 0f);
			}
			float2 yz = math.forward(math.mul(math.inverse(proceduralBone.m_Rotation), bone.m_Rotation)).yz;
			float num4 = 0f - math.atan2(yz.x, yz.y);
			float num5 = proceduralBone.m_Speed * deltaTime;
			num = math.select(math.clamp(num, num4 - num5, num4 + num5), num, instantReset);
			quaternion2 = math.mul(proceduralBone.m_Rotation, quaternion.RotateX(num));
		}
		else
		{
			quaternion2 = bone.m_Rotation;
		}
		skeleton.m_CurrentUpdated |= !bone.m_Rotation.Equals(quaternion2);
		bone.m_Rotation = quaternion2;
	}

	private static float FindCatenaryHeight(float3 position, quaternion rotation, float defaultHeight, ref ComponentLookup<Curve> curveDatas, ref ComponentLookup<PrefabRef> prefabRefDatas, ref ComponentLookup<UtilityLaneData> prefabUtilityLaneDatas, ref NativeQuadTree<Entity, QuadTreeBoundsXZ> laneSearchTree)
	{
		Line3.Segment line = new Line3.Segment(position, position + math.mul(rotation, new float3(0f, defaultHeight * 2f, 0f)));
		float3 @float = MathUtils.Position(line, 0.5f);
		CatenaryIterator iterator = new CatenaryIterator
		{
			m_Bounds = new Bounds3(@float - defaultHeight, @float + defaultHeight),
			m_Line = line,
			m_Result = 1000f,
			m_Default = defaultHeight,
			m_CurveData = curveDatas,
			m_PrefabRefData = prefabRefDatas,
			m_PrefabUtilityLaneData = prefabUtilityLaneDatas
		};
		laneSearchTree.Iterate(ref iterator);
		curveDatas = iterator.m_CurveData;
		prefabRefDatas = iterator.m_PrefabRefData;
		prefabUtilityLaneDatas = iterator.m_PrefabUtilityLaneData;
		return math.lerp(iterator.m_Result.x, defaultHeight, math.min(1f, iterator.m_Result.y / (defaultHeight * 0.5f)));
	}

	private static bool FindChildBone(DynamicBuffer<ProceduralBone> proceduralBones, int index, out int childIndex)
	{
		for (int i = 0; i < proceduralBones.Length; i++)
		{
			if (proceduralBones[i].m_ParentIndex == index)
			{
				childIndex = i;
				return true;
			}
		}
		childIndex = -1;
		return false;
	}

	private static void AnimateMovingBone(ProceduralBone proceduralBone, ref Skeleton skeleton, ref Bone bone, ref Momentum momentum, float3 moveDirection, float targetOffset, float targetSpeed, float deltaTime, bool instantReset)
	{
		float3 position = proceduralBone.m_Position;
		if (instantReset)
		{
			position += moveDirection * targetOffset;
			momentum.m_Momentum = 0f;
		}
		else
		{
			float num = math.dot(bone.m_Position - position, moveDirection);
			float num2 = targetOffset - num;
			targetSpeed = math.select(targetSpeed, 0f - targetSpeed, num2 < 0f);
			float num3 = math.sqrt(math.abs(num2 * proceduralBone.m_Acceleration));
			targetSpeed = math.clamp(targetSpeed, 0f - num3, num3);
			float valueToClamp = targetSpeed - momentum.m_Momentum;
			float num4 = math.abs(deltaTime * proceduralBone.m_Acceleration);
			momentum.m_Momentum += math.clamp(valueToClamp, 0f - num4, num4);
			position += moveDirection * (num + momentum.m_Momentum * deltaTime);
		}
		skeleton.m_CurrentUpdated |= !bone.m_Position.Equals(position);
		bone.m_Position = position;
	}

	private static void AnimateRotatingBoneY(ProceduralBone proceduralBone, ref Skeleton skeleton, ref Bone bone, ref Momentum momentum, ref Unity.Mathematics.Random random, float2 targetDir, float targetSpeed, float deltaTime, bool instantReset)
	{
		float angle;
		if (instantReset)
		{
			angle = ((!MathUtils.TryNormalize(ref targetDir)) ? random.NextFloat(-MathF.PI, MathF.PI) : MathUtils.RotationAngleSignedRight(math.forward().xz, targetDir));
			momentum.m_Momentum = 0f;
		}
		else
		{
			float2 value = math.forward(math.mul(math.inverse(proceduralBone.m_Rotation), bone.m_Rotation)).xz;
			if (MathUtils.TryNormalize(ref value) && MathUtils.TryNormalize(ref targetDir))
			{
				float num = MathUtils.RotationAngleSignedRight(value, targetDir);
				targetSpeed = math.select(targetSpeed, 0f - targetSpeed, num < 0f);
				float num2 = math.sqrt(math.abs(num * proceduralBone.m_Acceleration));
				targetSpeed = math.clamp(targetSpeed, 0f - num2, num2);
			}
			else
			{
				targetSpeed = 0f;
			}
			float valueToClamp = targetSpeed - momentum.m_Momentum;
			float num3 = math.abs(deltaTime * proceduralBone.m_Acceleration);
			momentum.m_Momentum += math.clamp(valueToClamp, 0f - num3, num3);
			angle = math.atan2(value.x, value.y) + momentum.m_Momentum * deltaTime;
		}
		quaternion quaternion = math.mul(proceduralBone.m_Rotation, quaternion.RotateY(angle));
		skeleton.m_CurrentUpdated |= !bone.m_Rotation.Equals(quaternion);
		bone.m_Rotation = quaternion;
	}

	private static void AnimateRotatingBoneZ(ProceduralBone proceduralBone, ref Skeleton skeleton, ref Bone bone, ref Momentum momentum, ref Unity.Mathematics.Random random, float2 targetDir, float targetSpeed, float deltaTime, bool instantReset)
	{
		float angle;
		if (instantReset)
		{
			angle = ((!MathUtils.TryNormalize(ref targetDir)) ? random.NextFloat(-MathF.PI, MathF.PI) : MathUtils.RotationAngleSignedLeft(math.up().xy, targetDir));
			momentum.m_Momentum = 0f;
		}
		else
		{
			float2 value = math.rotate(math.mul(math.inverse(proceduralBone.m_Rotation), bone.m_Rotation), math.up()).xy;
			if (MathUtils.TryNormalize(ref value) && MathUtils.TryNormalize(ref targetDir))
			{
				float num = MathUtils.RotationAngleSignedLeft(value, targetDir);
				targetSpeed = math.select(targetSpeed, 0f - targetSpeed, num < 0f);
				float num2 = math.sqrt(math.abs(num * proceduralBone.m_Acceleration));
				targetSpeed = math.clamp(targetSpeed, 0f - num2, num2);
			}
			else
			{
				targetSpeed = 0f;
			}
			float valueToClamp = targetSpeed - momentum.m_Momentum;
			float num3 = math.abs(deltaTime * proceduralBone.m_Acceleration);
			momentum.m_Momentum += math.clamp(valueToClamp, 0f - num3, num3);
			angle = math.atan2(0f - value.x, value.y) + momentum.m_Momentum * deltaTime;
		}
		quaternion quaternion = math.mul(proceduralBone.m_Rotation, quaternion.RotateZ(angle));
		skeleton.m_CurrentUpdated |= !bone.m_Rotation.Equals(quaternion);
		bone.m_Rotation = quaternion;
	}

	private static void AnimateRotatingBoneX(ProceduralBone proceduralBone, ref Skeleton skeleton, ref Bone bone, ref Momentum momentum, ref Unity.Mathematics.Random random, float2 targetDir, float targetSpeed, float deltaTime, bool instantReset)
	{
		float angle;
		if (instantReset)
		{
			angle = ((!MathUtils.TryNormalize(ref targetDir)) ? random.NextFloat(-MathF.PI, MathF.PI) : MathUtils.RotationAngleSignedLeft(math.up().yz, targetDir));
			momentum.m_Momentum = 0f;
		}
		else
		{
			float2 value = math.rotate(math.mul(math.inverse(proceduralBone.m_Rotation), bone.m_Rotation), math.up()).yz;
			if (MathUtils.TryNormalize(ref value) && MathUtils.TryNormalize(ref targetDir))
			{
				float num = MathUtils.RotationAngleSignedLeft(value, targetDir);
				targetSpeed = math.select(targetSpeed, 0f - targetSpeed, num < 0f);
				float num2 = math.sqrt(math.abs(num * proceduralBone.m_Acceleration));
				targetSpeed = math.clamp(targetSpeed, 0f - num2, num2);
			}
			else
			{
				targetSpeed = 0f;
			}
			float valueToClamp = targetSpeed - momentum.m_Momentum;
			float num3 = math.abs(deltaTime * proceduralBone.m_Acceleration);
			momentum.m_Momentum += math.clamp(valueToClamp, 0f - num3, num3);
			angle = math.atan2(value.y, value.x) + momentum.m_Momentum * deltaTime;
		}
		quaternion quaternion = math.mul(proceduralBone.m_Rotation, quaternion.RotateX(angle));
		skeleton.m_CurrentUpdated |= !bone.m_Rotation.Equals(quaternion);
		bone.m_Rotation = quaternion;
	}

	private static void AnimateRotatingBoneY(ProceduralBone proceduralBone, ref Skeleton skeleton, ref Bone bone, ref Momentum momentum, ref Unity.Mathematics.Random random, float targetSpeed, float deltaTime, bool instantReset)
	{
		float angle;
		if (instantReset)
		{
			momentum.m_Momentum = targetSpeed;
			angle = random.NextFloat(-MathF.PI, MathF.PI);
		}
		else
		{
			float valueToClamp = targetSpeed - momentum.m_Momentum;
			float num = math.abs(deltaTime * proceduralBone.m_Acceleration);
			momentum.m_Momentum += math.clamp(valueToClamp, 0f - num, num);
			float2 xz = math.forward(math.mul(math.inverse(proceduralBone.m_Rotation), bone.m_Rotation)).xz;
			angle = math.atan2(xz.x, xz.y) + momentum.m_Momentum * deltaTime;
		}
		quaternion quaternion = math.mul(proceduralBone.m_Rotation, quaternion.RotateY(angle));
		skeleton.m_CurrentUpdated |= !bone.m_Rotation.Equals(quaternion);
		bone.m_Rotation = quaternion;
	}

	private static void AnimateRotatingBoneZ(ProceduralBone proceduralBone, ref Skeleton skeleton, ref Bone bone, ref Momentum momentum, ref Unity.Mathematics.Random random, float targetSpeed, float deltaTime, bool instantReset)
	{
		float angle;
		if (instantReset)
		{
			momentum.m_Momentum = targetSpeed;
			angle = random.NextFloat(-MathF.PI, MathF.PI);
		}
		else
		{
			float valueToClamp = targetSpeed - momentum.m_Momentum;
			float num = math.abs(deltaTime * proceduralBone.m_Acceleration);
			momentum.m_Momentum += math.clamp(valueToClamp, 0f - num, num);
			float2 xy = math.rotate(math.mul(math.inverse(proceduralBone.m_Rotation), bone.m_Rotation), math.up()).xy;
			angle = math.atan2(0f - xy.x, xy.y) + momentum.m_Momentum * deltaTime;
		}
		quaternion quaternion = math.mul(proceduralBone.m_Rotation, quaternion.RotateZ(angle));
		skeleton.m_CurrentUpdated |= !bone.m_Rotation.Equals(quaternion);
		bone.m_Rotation = quaternion;
	}

	private static void AnimateRotatingBoneX(ProceduralBone proceduralBone, ref Skeleton skeleton, ref Bone bone, ref Momentum momentum, ref Unity.Mathematics.Random random, float targetSpeed, float deltaTime, bool instantReset)
	{
		float angle;
		if (instantReset)
		{
			momentum.m_Momentum = targetSpeed;
			angle = random.NextFloat(-MathF.PI, MathF.PI);
		}
		else
		{
			float valueToClamp = targetSpeed - momentum.m_Momentum;
			float num = math.abs(deltaTime * proceduralBone.m_Acceleration);
			momentum.m_Momentum += math.clamp(valueToClamp, 0f - num, num);
			float2 yz = math.rotate(math.mul(math.inverse(proceduralBone.m_Rotation), bone.m_Rotation), math.up()).yz;
			angle = math.atan2(yz.y, yz.x) + momentum.m_Momentum * deltaTime;
		}
		quaternion quaternion = math.mul(proceduralBone.m_Rotation, quaternion.RotateX(angle));
		skeleton.m_CurrentUpdated |= !bone.m_Rotation.Equals(quaternion);
		bone.m_Rotation = quaternion;
	}

	private static void AnimateInterpolatedLight(DynamicBuffer<ProceduralLight> proceduralLights, DynamicBuffer<LightAnimation> lightAnimations, DynamicBuffer<LightState> lights, TransformFlags transformFlags, Unity.Mathematics.Random pseudoRandom, ref Emissive emissive, int index, uint frame, float frameTime, float deltaTime, bool instantReset)
	{
		ProceduralLight proceduralLight = proceduralLights[index];
		int index2 = emissive.m_LightOffset + index;
		ref LightState light = ref lights.ElementAt(index2);
		switch (proceduralLight.m_Purpose)
		{
		case EmissiveProperties.Purpose.DaytimeRunningLight:
		case EmissiveProperties.Purpose.DaytimeRunningLightAlt:
			AnimateLight(proceduralLight, ref emissive, ref light, deltaTime, 1f, instantReset);
			break;
		case EmissiveProperties.Purpose.RearLight:
		{
			float targetIntensity5 = math.select(0f, 1f, (transformFlags & TransformFlags.RearLights) != 0);
			AnimateLight(proceduralLight, ref emissive, ref light, deltaTime, targetIntensity5, instantReset);
			break;
		}
		case EmissiveProperties.Purpose.Headlight_LowBeam:
		case EmissiveProperties.Purpose.TaxiLights:
		case EmissiveProperties.Purpose.SearchLightsFront:
		{
			float targetIntensity13 = math.select(0f, 1f, (transformFlags & TransformFlags.MainLights) != 0);
			AnimateLight(proceduralLight, ref emissive, ref light, deltaTime, targetIntensity13, instantReset);
			break;
		}
		case EmissiveProperties.Purpose.Headlight_HighBeam:
		case EmissiveProperties.Purpose.LandingLights:
		{
			float targetIntensity4 = math.select(0f, 1f, (transformFlags & TransformFlags.ExtraLights) != 0);
			AnimateLight(proceduralLight, ref emissive, ref light, deltaTime, targetIntensity4, instantReset);
			break;
		}
		case EmissiveProperties.Purpose.TurnSignalLeft:
		{
			float targetIntensity10 = 0f;
			if ((transformFlags & TransformFlags.TurningLeft) != 0)
			{
				targetIntensity10 = AnimateIntensity(proceduralLight, lightAnimations, pseudoRandom, frame, frameTime, 1f);
			}
			AnimateLight(proceduralLight, ref emissive, ref light, deltaTime, targetIntensity10, instantReset);
			break;
		}
		case EmissiveProperties.Purpose.TurnSignalRight:
		{
			float targetIntensity14 = 0f;
			if ((transformFlags & TransformFlags.TurningRight) != 0)
			{
				targetIntensity14 = AnimateIntensity(proceduralLight, lightAnimations, pseudoRandom, frame, frameTime, 1f);
			}
			AnimateLight(proceduralLight, ref emissive, ref light, deltaTime, targetIntensity14, instantReset);
			break;
		}
		case EmissiveProperties.Purpose.BrakeLight:
		{
			float targetIntensity8 = math.select(0f, 1f, (transformFlags & TransformFlags.Braking) != 0);
			AnimateLight(proceduralLight, ref emissive, ref light, deltaTime, targetIntensity8, instantReset);
			break;
		}
		case EmissiveProperties.Purpose.DaytimeRunningLightLeft:
		{
			float targetIntensity7 = math.select(1f, 0f, (transformFlags & TransformFlags.TurningLeft) != 0);
			AnimateLight(proceduralLight, ref emissive, ref light, deltaTime, targetIntensity7, instantReset);
			break;
		}
		case EmissiveProperties.Purpose.DaytimeRunningLightRight:
		{
			float targetIntensity9 = math.select(1f, 0f, (transformFlags & TransformFlags.TurningRight) != 0);
			AnimateLight(proceduralLight, ref emissive, ref light, deltaTime, targetIntensity9, instantReset);
			break;
		}
		case EmissiveProperties.Purpose.BrakeAndTurnSignalLeft:
		{
			float targetIntensity6 = math.select(0f, 1f, (transformFlags & TransformFlags.Braking) != 0);
			if ((transformFlags & TransformFlags.TurningLeft) != 0)
			{
				targetIntensity6 = AnimateIntensity(proceduralLight, lightAnimations, pseudoRandom, frame, frameTime, 1f);
			}
			AnimateLight(proceduralLight, ref emissive, ref light, deltaTime, targetIntensity6, instantReset);
			break;
		}
		case EmissiveProperties.Purpose.BrakeAndTurnSignalRight:
		{
			float targetIntensity16 = math.select(0f, 1f, (transformFlags & TransformFlags.Braking) != 0);
			if ((transformFlags & TransformFlags.TurningRight) != 0)
			{
				targetIntensity16 = AnimateIntensity(proceduralLight, lightAnimations, pseudoRandom, frame, frameTime, 1f);
			}
			AnimateLight(proceduralLight, ref emissive, ref light, deltaTime, targetIntensity16, instantReset);
			break;
		}
		case EmissiveProperties.Purpose.ReverseLight:
		{
			float targetIntensity15 = math.select(0f, 1f, (transformFlags & TransformFlags.Reversing) != 0);
			AnimateLight(proceduralLight, ref emissive, ref light, deltaTime, targetIntensity15, instantReset);
			break;
		}
		case EmissiveProperties.Purpose.Emergency1:
		case EmissiveProperties.Purpose.Emergency2:
		case EmissiveProperties.Purpose.Emergency3:
		case EmissiveProperties.Purpose.Emergency4:
		case EmissiveProperties.Purpose.Emergency5:
		case EmissiveProperties.Purpose.Emergency6:
		case EmissiveProperties.Purpose.RearAlarmLights:
		case EmissiveProperties.Purpose.FrontAlarmLightsLeft:
		case EmissiveProperties.Purpose.FrontAlarmLightsRight:
		case EmissiveProperties.Purpose.Warning1:
		case EmissiveProperties.Purpose.Warning2:
		case EmissiveProperties.Purpose.Emergency7:
		case EmissiveProperties.Purpose.Emergency8:
		case EmissiveProperties.Purpose.Emergency9:
		case EmissiveProperties.Purpose.Emergency10:
		case EmissiveProperties.Purpose.AntiCollisionLightsRed:
		case EmissiveProperties.Purpose.AntiCollisionLightsWhite:
		{
			float targetIntensity12 = 0f;
			if ((transformFlags & TransformFlags.WarningLights) != 0)
			{
				targetIntensity12 = AnimateIntensity(proceduralLight, lightAnimations, pseudoRandom, frame, frameTime, 1f);
			}
			AnimateLight(proceduralLight, ref emissive, ref light, deltaTime, targetIntensity12, instantReset);
			break;
		}
		case EmissiveProperties.Purpose.CollectionLights:
		case EmissiveProperties.Purpose.TaxiSign:
		case EmissiveProperties.Purpose.WorkLights:
		case EmissiveProperties.Purpose.SearchLights360:
		{
			float targetIntensity11 = math.select(0f, 1f, (transformFlags & TransformFlags.WorkLights) != 0);
			AnimateLight(proceduralLight, ref emissive, ref light, deltaTime, targetIntensity11, instantReset);
			break;
		}
		case EmissiveProperties.Purpose.SignalGroup1:
		case EmissiveProperties.Purpose.SignalGroup2:
		case EmissiveProperties.Purpose.SignalGroup3:
		case EmissiveProperties.Purpose.SignalGroup4:
		case EmissiveProperties.Purpose.SignalGroup5:
		case EmissiveProperties.Purpose.SignalGroup6:
		case EmissiveProperties.Purpose.SignalGroup7:
		case EmissiveProperties.Purpose.SignalGroup8:
		case EmissiveProperties.Purpose.SignalGroup9:
		case EmissiveProperties.Purpose.SignalGroup10:
		case EmissiveProperties.Purpose.SignalGroup11:
		{
			int num = (int)(proceduralLight.m_Purpose - 12);
			SignalGroupMask signalGroupMask = (SignalGroupMask)(1 << num);
			float targetIntensity3 = 0f;
			if ((transformFlags & (TransformFlags.SignalAnimation1 | TransformFlags.SignalAnimation2)) != 0)
			{
				int num2 = 0;
				num2 |= (((transformFlags & TransformFlags.SignalAnimation1) != 0) ? 1 : 0);
				num2 |= (((transformFlags & TransformFlags.SignalAnimation2) != 0) ? 2 : 0);
				num2--;
				targetIntensity3 = AnimateIntensity(signalGroupMask, num2, lightAnimations, pseudoRandom, frame, frameTime, 1f);
			}
			AnimateLight(proceduralLight, ref emissive, ref light, deltaTime, targetIntensity3, instantReset);
			break;
		}
		case EmissiveProperties.Purpose.NeonSign:
		case EmissiveProperties.Purpose.DecorativeLight:
		{
			float targetIntensity2 = AnimateIntensity(proceduralLight, lightAnimations, pseudoRandom, frame, frameTime, 1f);
			AnimateLight(proceduralLight, ref emissive, ref light, deltaTime, targetIntensity2, instantReset);
			break;
		}
		case EmissiveProperties.Purpose.BoardingLightLeft:
		{
			float y2 = math.select(1f, 0f, (transformFlags & TransformFlags.BoardingLeft) != 0);
			AnimateLight(proceduralLight, ref emissive, ref light, deltaTime, new float2(1f, y2), instantReset);
			break;
		}
		case EmissiveProperties.Purpose.BoardingLightRight:
		{
			float y = math.select(1f, 0f, (transformFlags & TransformFlags.BoardingRight) != 0);
			AnimateLight(proceduralLight, ref emissive, ref light, deltaTime, new float2(1f, y), instantReset);
			break;
		}
		case EmissiveProperties.Purpose.Interior1:
		case EmissiveProperties.Purpose.Interior2:
		{
			float targetIntensity = math.select(0f, 0.003f, (transformFlags & TransformFlags.InteriorLights) != 0);
			AnimateLight(proceduralLight, ref emissive, ref light, deltaTime, targetIntensity, instantReset);
			break;
		}
		case EmissiveProperties.Purpose.Clearance:
		case EmissiveProperties.Purpose.Dashboard:
		case EmissiveProperties.Purpose.Clearance2:
		case EmissiveProperties.Purpose.MarkerLights:
		case EmissiveProperties.Purpose.WingInspectionLights:
		case EmissiveProperties.Purpose.LogoLights:
		case EmissiveProperties.Purpose.PositionLightLeft:
		case EmissiveProperties.Purpose.PositionLightRight:
		case EmissiveProperties.Purpose.PositionLights:
		case EmissiveProperties.Purpose.NumberLight:
			AnimateLight(proceduralLight, ref emissive, ref light, deltaTime, 1f, instantReset);
			break;
		case EmissiveProperties.Purpose.TrafficLight_Red:
		case EmissiveProperties.Purpose.TrafficLight_Yellow:
		case EmissiveProperties.Purpose.TrafficLight_Green:
		case EmissiveProperties.Purpose.PedestrianLight_Stop:
		case EmissiveProperties.Purpose.PedestrianLight_Walk:
		case EmissiveProperties.Purpose.RailCrossing_Stop:
			break;
		}
	}

	private static float AnimateIntensity(ProceduralLight proceduralLight, DynamicBuffer<LightAnimation> lightAnimations, Unity.Mathematics.Random pseudoRandom, uint frame, float frameTime, float intensity)
	{
		if (proceduralLight.m_AnimationIndex >= 0 && lightAnimations.IsCreated)
		{
			LightAnimation lightAnimation = lightAnimations[proceduralLight.m_AnimationIndex];
			float num = (float)((frame + pseudoRandom.NextUInt(lightAnimation.m_DurationFrames)) % lightAnimation.m_DurationFrames) + frameTime;
			intensity *= lightAnimation.m_AnimationCurve.Evaluate(num / (float)lightAnimation.m_DurationFrames);
		}
		return intensity;
	}

	private static float AnimateIntensity(SignalGroupMask signalGroupMask, int signalAnimationIndex, DynamicBuffer<LightAnimation> lightAnimations, Unity.Mathematics.Random pseudoRandom, uint frame, float frameTime, float intensity)
	{
		if (signalAnimationIndex >= 0 && lightAnimations.IsCreated)
		{
			LightAnimation lightAnimation = lightAnimations[signalAnimationIndex];
			float num = (float)((frame + pseudoRandom.NextUInt(lightAnimation.m_DurationFrames)) % lightAnimation.m_DurationFrames) + frameTime;
			intensity *= lightAnimation.m_SignalAnimation.Evaluate(signalGroupMask, num / (float)lightAnimation.m_DurationFrames);
		}
		return intensity;
	}

	public static void AnimateLight(ProceduralLight proceduralLight, ref Emissive emissive, ref LightState light, float deltaTime, float targetIntensity, bool instantReset)
	{
		float num = math.abs(deltaTime) * proceduralLight.m_ResponseSpeed;
		float num2 = math.select(math.clamp(targetIntensity, light.m_Intensity - num, light.m_Intensity + num), targetIntensity, instantReset);
		emissive.m_Updated |= light.m_Intensity != num2;
		light.m_Intensity = num2;
	}

	public static void AnimateLight(ProceduralLight proceduralLight, ref Emissive emissive, ref LightState light, float deltaTime, float2 target, bool instantReset)
	{
		float2 @float = new float2(light.m_Intensity, light.m_Color);
		float num = math.abs(deltaTime) * proceduralLight.m_ResponseSpeed;
		float2 float2 = math.select(math.clamp(target, @float - num, @float + num), target, instantReset);
		emissive.m_Updated |= math.any(float2 != @float);
		light.m_Intensity = float2.x;
		light.m_Color = float2.y;
	}

	public static void UpdateInterpolatedAnimationBody(Entity entity, in CharacterElement characterElement, DynamicBuffer<AnimationClip> clips, ref ComponentLookup<Human> humanLookup, ref ComponentLookup<CurrentVehicle> currentVehicleLookup, ref ComponentLookup<PrefabRef> prefabRefLookup, ref BufferLookup<ActivityLocationElement> activityLocationLookup, ref BufferLookup<AnimationMotion> motionLookup, InterpolatedTransform oldTransform, InterpolatedTransform newTransform, PseudoRandomSeed pseudoRandomSeed, ref Animated animated, ref Unity.Mathematics.Random random, TransformFrame frame0, TransformFrame frame1, float framePosition, float updateFrameToSeconds, float speedDeltaFactor, float deltaTime, int updateFrameChanged, bool instantReset)
	{
		float3 y = math.forward(newTransform.m_Rotation);
		float num = math.dot(newTransform.m_Position - oldTransform.m_Position, y);
		if (instantReset)
		{
			AnimationClip clip = clips[animated.m_ClipIndexBody0];
			ActivityCondition activityConditions = GetActivityConditions(entity, ref humanLookup);
			ActivityType activity = (ActivityType)frame1.m_Activity;
			AnimatedPropID propID = GetPropID(entity, activity, ref currentVehicleLookup, ref prefabRefLookup, ref activityLocationLookup);
			GetClipType(clip, frame1.m_State, activityConditions, num, speedDeltaFactor, out var type, ref activity);
			FindAnimationClip(clips, type, activity, AnimationLayer.Body, pseudoRandomSeed, propID, activityConditions, out clip, out var index);
			animated.m_ClipIndexBody0 = (short)index;
			animated.m_ClipIndexBody0I = -1;
			animated.m_ClipIndexBody1 = -1;
			animated.m_ClipIndexBody1I = -1;
			animated.m_MovementSpeed = new float2(GetMovementSpeed(in characterElement, in clip, ref motionLookup), 0f);
			animated.m_Time.xy = 0f;
			if (animated.m_MovementSpeed.x != 0f || frame1.m_State == TransformState.Idle)
			{
				animated.m_Time.x = random.NextFloat(clip.m_AnimationLength);
			}
		}
		else if (updateFrameChanged > 0)
		{
			AnimationClip clip2;
			if (animated.m_ClipIndexBody1 != -1)
			{
				clip2 = clips[animated.m_ClipIndexBody1];
				animated.m_ClipIndexBody0 = animated.m_ClipIndexBody1;
				animated.m_ClipIndexBody0I = animated.m_ClipIndexBody1I;
				animated.m_Time.x = animated.m_Time.y;
				animated.m_MovementSpeed.x = animated.m_MovementSpeed.y;
			}
			else
			{
				clip2 = clips[animated.m_ClipIndexBody0];
			}
			ActivityCondition activityConditions2 = GetActivityConditions(entity, ref humanLookup);
			ActivityType activity2 = (ActivityType)frame1.m_Activity;
			AnimatedPropID propID2 = GetPropID(entity, activity2, ref currentVehicleLookup, ref prefabRefLookup, ref activityLocationLookup);
			GetClipType(clip2, frame1.m_State, activityConditions2, num, speedDeltaFactor, out var type2, ref activity2);
			animated.m_ClipIndexBody1 = -1;
			animated.m_ClipIndexBody1I = -1;
			animated.m_MovementSpeed.y = 0f;
			animated.m_Time.y = 0f;
			if (clip2.m_Type != type2 || clip2.m_Activity != activity2 || (clip2.m_PropID != propID2 && propID2 != AnimatedPropID.Any))
			{
				float animationLength = clip2.m_AnimationLength;
				if (FindAnimationClip(clips, type2, activity2, AnimationLayer.Body, pseudoRandomSeed, propID2, activityConditions2, out clip2, out var index2))
				{
					animated.m_ClipIndexBody1 = (short)index2;
					animated.m_ClipIndexBody1I = -1;
					animated.m_MovementSpeed.y = GetMovementSpeed(in characterElement, in clip2, ref motionLookup);
					animated.m_Time.y = GetInitialTime(ref random, in clip2, animated.m_MovementSpeed.y, animationLength, animated.m_MovementSpeed.x, animated.m_Time.x);
				}
			}
		}
		else if (updateFrameChanged < 0)
		{
			AnimationClip clip3 = clips[animated.m_ClipIndexBody0];
			ActivityCondition activityConditions3 = GetActivityConditions(entity, ref humanLookup);
			ActivityType activity3 = (ActivityType)frame0.m_Activity;
			AnimatedPropID propID3 = GetPropID(entity, activity3, ref currentVehicleLookup, ref prefabRefLookup, ref activityLocationLookup);
			GetClipType(clip3, frame0.m_State, activityConditions3, num, speedDeltaFactor, out var type3, ref activity3);
			animated.m_ClipIndexBody1 = -1;
			animated.m_ClipIndexBody1I = -1;
			animated.m_Time.y = 0f;
			animated.m_MovementSpeed.y = 0f;
			if (clip3.m_Type != type3 || clip3.m_Activity != activity3 || (clip3.m_PropID != propID3 && propID3 != AnimatedPropID.Any))
			{
				float animationLength2 = clip3.m_AnimationLength;
				if (FindAnimationClip(clips, type3, activity3, AnimationLayer.Body, pseudoRandomSeed, propID3, activityConditions3, out clip3, out var index3))
				{
					animated.m_ClipIndexBody1 = animated.m_ClipIndexBody0;
					animated.m_ClipIndexBody1I = animated.m_ClipIndexBody0I;
					animated.m_MovementSpeed.y = animated.m_MovementSpeed.x;
					animated.m_Time.y = animated.m_Time.x;
					animated.m_ClipIndexBody0 = (short)index3;
					animated.m_ClipIndexBody0I = -1;
					animated.m_MovementSpeed.x = GetMovementSpeed(in characterElement, in clip3, ref motionLookup);
					animated.m_Time.x = GetInitialTime(ref random, in clip3, animated.m_MovementSpeed.x, animationLength2, animated.m_MovementSpeed.y, animated.m_Time.y);
				}
			}
		}
		if (animated.m_ClipIndexBody1 != -1)
		{
			if (math.all(animated.m_MovementSpeed != 0f))
			{
				SynchronizeMovementTime(clips, ref animated, num, framePosition);
				return;
			}
			if (animated.m_MovementSpeed.y != 0f)
			{
				animated.m_Time.y += num / animated.m_MovementSpeed.y;
			}
			else if (clips[animated.m_ClipIndexBody1].m_Type == Game.Prefabs.AnimationType.Idle)
			{
				animated.m_Time.y += deltaTime;
			}
			else
			{
				animated.m_Time.y = ((float)(int)frame1.m_StateTimer + framePosition - 1f) * updateFrameToSeconds;
			}
		}
		if (animated.m_MovementSpeed.x != 0f)
		{
			animated.m_Time.x += num / animated.m_MovementSpeed.x;
		}
		else if (clips[animated.m_ClipIndexBody0].m_Type == Game.Prefabs.AnimationType.Idle)
		{
			animated.m_Time.x += deltaTime;
		}
		else
		{
			animated.m_Time.x = ((float)(int)frame0.m_StateTimer + framePosition) * updateFrameToSeconds;
		}
	}

	public static float GetUpdateFrameTransition(float framePosition)
	{
		float num = framePosition * framePosition;
		return 3f * num - 2f * num * framePosition;
	}

	public static void SynchronizeMovementTime(DynamicBuffer<AnimationClip> clips, ref Animated animated, float movementDelta, float framePosition)
	{
		AnimationClip animationClip = clips[animated.m_ClipIndexBody0];
		AnimationClip animationClip2 = clips[animated.m_ClipIndexBody1];
		float2 @float = new float2(animationClip.m_AnimationLength, animationClip2.m_AnimationLength);
		float2 float2 = movementDelta / (animated.m_MovementSpeed * @float);
		animated.m_Time.xy += math.lerp(float2.x, float2.y, framePosition) * @float;
	}

	public static float GetInitialTime(ref Unity.Mathematics.Random random, in AnimationClip clip, float movementSpeed, float prevClipLength, float prevMovementSpeed, float prevTime)
	{
		if (movementSpeed != 0f && prevMovementSpeed != 0f && prevClipLength != 0f)
		{
			return prevTime / prevClipLength * clip.m_AnimationLength;
		}
		return clip.m_Playback switch
		{
			AnimationPlayback.RandomLoop => random.NextFloat(clip.m_AnimationLength), 
			AnimationPlayback.HalfLoop => math.select(0f, clip.m_AnimationLength * 0.5f, random.NextBool()), 
			_ => 0f, 
		};
	}

	public static void UpdateInterpolatedAnimationFace(Entity entity, DynamicBuffer<AnimationClip> clips, ref ComponentLookup<Human> humanLookup, ref Animated animated, ref Unity.Mathematics.Random random, TransformState state, ActivityType activity, PseudoRandomSeed pseudoRandomSeed, float deltaTime, int updateFrameChanged, bool instantReset)
	{
		if (instantReset)
		{
			ActivityCondition activityConditions = GetActivityConditions(entity, ref humanLookup);
			FindAnimationClip(clips, Game.Prefabs.AnimationType.Idle, ActivityType.None, AnimationLayer.Facial, pseudoRandomSeed, AnimatedPropID.None, activityConditions, out var clip, out var index);
			animated.m_ClipIndexFace0 = (short)index;
			animated.m_Time.z = random.NextFloat(clip.m_AnimationLength);
		}
		else if (updateFrameChanged > 0)
		{
			AnimationClip clip2;
			if (animated.m_ClipIndexFace1 != -1)
			{
				clip2 = clips[animated.m_ClipIndexFace1];
				animated.m_ClipIndexFace0 = animated.m_ClipIndexFace1;
				animated.m_Time.z = animated.m_Time.w;
			}
			else
			{
				clip2 = clips[animated.m_ClipIndexFace0];
			}
			ActivityCondition activityConditions2 = GetActivityConditions(entity, ref humanLookup);
			animated.m_ClipIndexFace1 = -1;
			animated.m_Time.w = 0f;
			if (((clip2.m_Conditions ^ activityConditions2) & (ActivityCondition.Angry | ActivityCondition.Sad | ActivityCondition.Happy | ActivityCondition.Waiting)) != 0 && FindAnimationClip(clips, Game.Prefabs.AnimationType.Idle, ActivityType.None, AnimationLayer.Facial, pseudoRandomSeed, AnimatedPropID.None, activityConditions2, out clip2, out var index2))
			{
				animated.m_ClipIndexFace1 = (short)index2;
				animated.m_Time.w = random.NextFloat(clip2.m_AnimationLength);
			}
		}
		else if (updateFrameChanged < 0)
		{
			AnimationClip clip3 = clips[animated.m_ClipIndexFace0];
			ActivityCondition activityConditions3 = GetActivityConditions(entity, ref humanLookup);
			animated.m_ClipIndexFace1 = -1;
			animated.m_Time.w = 0f;
			if (((clip3.m_Conditions ^ activityConditions3) & (ActivityCondition.Angry | ActivityCondition.Sad | ActivityCondition.Happy | ActivityCondition.Waiting)) != 0 && FindAnimationClip(clips, Game.Prefabs.AnimationType.Idle, ActivityType.None, AnimationLayer.Facial, pseudoRandomSeed, AnimatedPropID.None, activityConditions3, out clip3, out var index3))
			{
				animated.m_ClipIndexFace1 = animated.m_ClipIndexFace0;
				animated.m_Time.w = animated.m_Time.z;
				animated.m_ClipIndexFace0 = (short)index3;
				animated.m_Time.z = random.NextFloat(clip3.m_AnimationLength);
			}
		}
		animated.m_Time.zw += deltaTime;
		animated.m_Time.w = 0f;
	}

	public static void CalculateUpdateFrames(uint simulationFrameIndex, float simulationFrameTime, uint updateFrameIndex, out uint updateFrame1, out uint updateFrame2, out float framePosition)
	{
		uint num = simulationFrameIndex - updateFrameIndex - 32;
		updateFrame1 = (num >> 4) & 3;
		updateFrame2 = (updateFrame1 + 1) & 3;
		framePosition = ((float)(num & 0xF) + simulationFrameTime) * 0.0625f;
	}

	public static void CalculateUpdateFrames(uint simulationFrameIndex, uint prevSimulationFrameIndex, float simulationFrameTime, uint updateFrameIndex, out uint updateFrame1, out uint updateFrame2, out float framePosition, out int updateFrameChanged)
	{
		uint num = simulationFrameIndex - updateFrameIndex - 32;
		uint num2 = prevSimulationFrameIndex - updateFrameIndex - 32;
		updateFrame1 = num >> 4;
		uint num3 = num2 >> 4;
		updateFrameChanged = math.select(0, math.select(-1, 1, updateFrame1 > num3), updateFrame1 != num3);
		updateFrame1 &= 3u;
		updateFrame2 = (updateFrame1 + 1) & 3;
		framePosition = ((float)(num & 0xF) + simulationFrameTime) * 0.0625f;
	}

	public static InterpolatedTransform CalculateTransform(TransformFrame frame1, TransformFrame frame2, float framePosition)
	{
		Bezier4x3 curve = new Bezier4x3(frame1.m_Position, frame1.m_Position + frame1.m_Velocity * (4f / 45f), frame2.m_Position - frame2.m_Velocity * (4f / 45f), frame2.m_Position);
		InterpolatedTransform result = default(InterpolatedTransform);
		result.m_Position = MathUtils.Position(curve, framePosition);
		result.m_Rotation = math.slerp(frame1.m_Rotation, frame2.m_Rotation, framePosition);
		result.m_Flags = ((framePosition >= 0.5f) ? frame2.m_Flags : frame1.m_Flags);
		return result;
	}

	private static quaternion LocalToWorld(DynamicBuffer<ProceduralBone> proceduralBones, DynamicBuffer<Bone> bones, Transform transform, Skeleton skeleton, int index, quaternion rotation)
	{
		while (index >= 0)
		{
			rotation = math.mul(bones[skeleton.m_BoneOffset + index].m_Rotation, rotation);
			index = proceduralBones[index].m_ParentIndex;
		}
		return math.mul(transform.m_Rotation, rotation);
	}

	private static quaternion LocalToObject(DynamicBuffer<ProceduralBone> proceduralBones, DynamicBuffer<Bone> bones, Skeleton skeleton, int index, quaternion rotation)
	{
		while (index >= 0)
		{
			rotation = math.mul(bones[skeleton.m_BoneOffset + index].m_Rotation, rotation);
			index = proceduralBones[index].m_ParentIndex;
		}
		return rotation;
	}

	private static float3 LocalToWorld(DynamicBuffer<ProceduralBone> proceduralBones, DynamicBuffer<Bone> bones, Transform transform, Skeleton skeleton, int index, float3 position)
	{
		while (index >= 0)
		{
			Bone bone = bones[skeleton.m_BoneOffset + index];
			position = bone.m_Position + math.mul(bone.m_Rotation, position);
			index = proceduralBones[index].m_ParentIndex;
		}
		return transform.m_Position + math.mul(transform.m_Rotation, position);
	}

	private static void LocalToWorld(DynamicBuffer<ProceduralBone> proceduralBones, DynamicBuffer<Bone> bones, Transform transform, Skeleton skeleton, int index, ref float3 position, ref quaternion rotation)
	{
		while (index >= 0)
		{
			Bone bone = bones[skeleton.m_BoneOffset + index];
			position = bone.m_Position + math.mul(bone.m_Rotation, position);
			rotation = math.mul(bone.m_Rotation, rotation);
			index = proceduralBones[index].m_ParentIndex;
		}
		position = transform.m_Position + math.mul(transform.m_Rotation, position);
		rotation = math.mul(transform.m_Rotation, rotation);
	}

	private static void LookAtLocalToWorld(DynamicBuffer<ProceduralBone> proceduralBones, DynamicBuffer<Bone> bones, Transform transform, Skeleton skeleton, PointOfInterest pointOfInterest, int parentIndex, ref float3 position, ref quaternion rotation)
	{
		ProceduralBone proceduralBone = proceduralBones[parentIndex];
		if (proceduralBone.m_Type == BoneType.LookAtRotation || proceduralBone.m_Type == BoneType.LookAtRotationSide)
		{
			float3 position2 = proceduralBone.m_Position;
			quaternion rotation2 = proceduralBone.m_Rotation;
			LocalToWorld(proceduralBones, bones, transform, skeleton, proceduralBone.m_ParentIndex, ref position2, ref rotation2);
			float3 v = pointOfInterest.m_Position - position2;
			v = math.mul(math.inverse(rotation2), v);
			v.xz = math.select(v.xz, MathUtils.Right(v.xz), proceduralBone.m_Type == BoneType.LookAtRotationSide);
			float2 value = math.select(v, -v, proceduralBone.m_Speed < 0f).xz;
			if (MathUtils.TryNormalize(ref value))
			{
				float angle = MathUtils.RotationAngleSignedRight(math.forward().xz, value);
				rotation2 = math.mul(rotation2, quaternion.RotateY(angle));
			}
			position = position2 + math.mul(rotation2, position);
			rotation = math.mul(rotation2, rotation);
		}
		else if (proceduralBone.m_Type == BoneType.LengthwiseLookAtRotation)
		{
			float3 position3 = proceduralBone.m_Position;
			quaternion rotation3 = proceduralBone.m_Rotation;
			LocalToWorld(proceduralBones, bones, transform, skeleton, proceduralBone.m_ParentIndex, ref position3, ref rotation3);
			float3 v2 = pointOfInterest.m_Position - position3;
			v2 = math.mul(math.inverse(rotation3), v2);
			float2 value2 = math.select(v2, -v2, proceduralBone.m_Speed < 0f).xy;
			if (MathUtils.TryNormalize(ref value2))
			{
				float angle2 = MathUtils.RotationAngleSignedLeft(math.up().xy, value2);
				rotation3 = math.mul(rotation3, quaternion.RotateZ(angle2));
			}
			position = position3 + math.mul(rotation3, position);
			rotation = math.mul(rotation3, rotation);
		}
		else
		{
			LocalToWorld(proceduralBones, bones, transform, skeleton, parentIndex, ref position, ref rotation);
		}
	}

	public static bool FindAnimationClip(DynamicBuffer<AnimationClip> clips, Game.Prefabs.AnimationType type, ActivityType activity, AnimationLayer animationLayer, PseudoRandomSeed pseudoRandomSeed, AnimatedPropID propID, ActivityCondition conditions, out AnimationClip clip, out int index)
	{
		int num = int.MaxValue;
		clip = clips[0];
		index = 0;
		for (int i = 0; i < clips.Length; i++)
		{
			AnimationClip animationClip = clips[i];
			if (animationClip.m_Type == type && animationClip.m_Activity == activity && animationClip.m_Layer == animationLayer && (animationClip.m_PropID == propID || propID == AnimatedPropID.Any) && (!(propID == AnimatedPropID.Any) || animationClip.m_VariationCount <= 1 || pseudoRandomSeed.GetRandom((uint)PseudoRandomSeed.kAnimationVariation ^ (uint)activity).NextInt(animationClip.m_VariationCount) == animationClip.m_VariationIndex))
			{
				ActivityCondition activityCondition = animationClip.m_Conditions ^ conditions;
				if (activityCondition == (ActivityCondition)0u)
				{
					clip = animationClip;
					index = i;
					return true;
				}
				int num2 = math.countbits((uint)activityCondition);
				if (num2 < num)
				{
					num = num2;
					clip = animationClip;
					index = i;
				}
			}
		}
		return num != int.MaxValue;
	}

	public static bool FindAnimationClip(DynamicBuffer<AnimationClip> clips, Game.Prefabs.AnimationType type, ActivityType activity, AnimationLayer animationLayer, GenderMask gender, ActivityCondition conditions, out AnimationClip clip, out int index)
	{
		int num = int.MaxValue;
		clip = clips[0];
		index = 0;
		for (int i = 0; i < clips.Length; i++)
		{
			AnimationClip animationClip = clips[i];
			if (animationClip.m_Type == type && animationClip.m_Activity == activity && animationClip.m_Layer == animationLayer && (animationClip.m_Gender & gender) == gender)
			{
				ActivityCondition activityCondition = animationClip.m_Conditions ^ conditions;
				if (activityCondition == (ActivityCondition)0u)
				{
					clip = animationClip;
					index = i;
					return true;
				}
				int num2 = math.countbits((uint)activityCondition);
				if (num2 < num)
				{
					num = num2;
					clip = animationClip;
					index = i;
				}
			}
		}
		return num != int.MaxValue;
	}

	public static float GetMovementSpeed(in CharacterElement characterElement, in AnimationClip clip, ref BufferLookup<AnimationMotion> motionLookup)
	{
		if (clip.m_Type == Game.Prefabs.AnimationType.Move && clip.m_MotionRange.y > clip.m_MotionRange.x + 1)
		{
			DynamicBuffer<AnimationMotion> motions = motionLookup[characterElement.m_Style];
			AnimationMotion motion = motions[clip.m_MotionRange.x];
			AddMotionOffset(ref motion, motions, clip.m_MotionRange, characterElement.m_ShapeWeights.m_Weight0);
			AddMotionOffset(ref motion, motions, clip.m_MotionRange, characterElement.m_ShapeWeights.m_Weight1);
			AddMotionOffset(ref motion, motions, clip.m_MotionRange, characterElement.m_ShapeWeights.m_Weight2);
			AddMotionOffset(ref motion, motions, clip.m_MotionRange, characterElement.m_ShapeWeights.m_Weight3);
			AddMotionOffset(ref motion, motions, clip.m_MotionRange, characterElement.m_ShapeWeights.m_Weight4);
			AddMotionOffset(ref motion, motions, clip.m_MotionRange, characterElement.m_ShapeWeights.m_Weight5);
			AddMotionOffset(ref motion, motions, clip.m_MotionRange, characterElement.m_ShapeWeights.m_Weight6);
			AddMotionOffset(ref motion, motions, clip.m_MotionRange, characterElement.m_ShapeWeights.m_Weight7);
			float num = clip.m_AnimationLength * clip.m_FrameRate;
			float num2 = clip.m_FrameRate / math.max(1f, num - 1f);
			return math.length(motion.m_EndOffset - motion.m_StartOffset) * num2;
		}
		return clip.m_MovementSpeed;
	}

	private static void AddMotionOffset(ref AnimationMotion motion, DynamicBuffer<AnimationMotion> motions, int2 range, BlendWeight weight)
	{
		AnimationMotion animationMotion = motions[range.x + weight.m_Index + 1];
		motion.m_StartOffset += animationMotion.m_StartOffset * weight.m_Weight;
		motion.m_EndOffset += animationMotion.m_EndOffset * weight.m_Weight;
	}

	public static AnimatedPropID GetPropID(Entity entity, ActivityType activity, ref ComponentLookup<CurrentVehicle> currentVehicleLookup, ref ComponentLookup<PrefabRef> prefabRefLookup, ref BufferLookup<ActivityLocationElement> activityLocationLookup)
	{
		AnimatedPropID result = AnimatedPropID.Any;
		switch (activity)
		{
		case ActivityType.None:
			result = AnimatedPropID.None;
			break;
		case ActivityType.Standing:
		case ActivityType.Enter:
		case ActivityType.Exit:
		{
			result = AnimatedPropID.None;
			if (currentVehicleLookup.TryGetComponent(entity, out var componentData) && prefabRefLookup.TryGetComponent(componentData.m_Vehicle, out var componentData2) && activityLocationLookup.TryGetBuffer(componentData2.m_Prefab, out var bufferData) && bufferData.Length != 0)
			{
				result = bufferData[0].m_PropID;
			}
			break;
		}
		}
		return result;
	}

	public static ActivityCondition GetActivityConditions(Entity entity, ref ComponentLookup<Human> humanLookup)
	{
		if (humanLookup.TryGetComponent(entity, out var componentData))
		{
			return CreatureUtils.GetConditions(componentData);
		}
		return (ActivityCondition)0u;
	}

	public static void GetClipType(AnimationClip clip, TransformState state, ActivityCondition condition, float movementDelta, float speedDeltaFactor, out Game.Prefabs.AnimationType type, ref ActivityType activity)
	{
		switch (state)
		{
		case TransformState.Move:
			type = Game.Prefabs.AnimationType.Move;
			if (activity == ActivityType.None)
			{
				switch (clip.m_Activity)
				{
				case ActivityType.Walking:
				{
					float num2 = math.abs(movementDelta * speedDeltaFactor);
					activity = ((speedDeltaFactor != 0f && num2 > clip.m_SpeedRange.max) ? ActivityType.Running : ActivityType.Walking);
					break;
				}
				case ActivityType.Running:
				{
					float num = math.abs(movementDelta * speedDeltaFactor);
					activity = ((speedDeltaFactor != 0f && num < clip.m_SpeedRange.min) ? ActivityType.Walking : ActivityType.Running);
					break;
				}
				default:
					activity = ActivityType.Walking;
					break;
				}
			}
			break;
		case TransformState.Start:
			type = Game.Prefabs.AnimationType.Start;
			break;
		case TransformState.End:
			type = Game.Prefabs.AnimationType.End;
			break;
		case TransformState.Action:
		case TransformState.Done:
			type = Game.Prefabs.AnimationType.Action;
			break;
		default:
			type = Game.Prefabs.AnimationType.Idle;
			if (activity == ActivityType.None)
			{
				activity = (((condition & ActivityCondition.Collapsed) == 0) ? ActivityType.Standing : ActivityType.GroundLaying);
			}
			break;
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
	public ObjectInterpolateSystem()
	{
	}
}
