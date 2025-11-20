using System.Runtime.CompilerServices;
using Colossal.Collections;
using Colossal.Entities;
using Colossal.Mathematics;
using Game.Buildings;
using Game.City;
using Game.Common;
using Game.Events;
using Game.Net;
using Game.Objects;
using Game.Prefabs;
using Game.Rendering;
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

namespace Game.Simulation;

[CompilerGenerated]
public class WeatherPhenomenonSystem : GameSystemBase
{
	[BurstCompile]
	private struct WeatherPhenomenonJob : IJobChunk
	{
		private struct LightningTargetIterator : INativeQuadTreeIterator<Entity, QuadTreeBoundsXZ>, IUnsafeQuadTreeIterator<Entity, QuadTreeBoundsXZ>
		{
			public Game.Events.WeatherPhenomenon m_WeatherPhenomenon;

			public Entity m_SelectedEntity;

			public float3 m_SelectedPosition;

			public float m_BestDistance;

			public ComponentLookup<Building> m_BuildingData;

			public bool Intersect(QuadTreeBoundsXZ bounds)
			{
				float num = MathUtils.Distance(bounds.m_Bounds.xz, m_WeatherPhenomenon.m_HotspotPosition.xz);
				if (num < m_WeatherPhenomenon.m_HotspotRadius)
				{
					return num * 0.5f - bounds.m_Bounds.max.y < m_BestDistance;
				}
				return false;
			}

			public void Iterate(QuadTreeBoundsXZ bounds, Entity item)
			{
				float2 x = MathUtils.Center(bounds.m_Bounds.xz);
				float num = math.distance(x, m_WeatherPhenomenon.m_HotspotPosition.xz);
				if (!(num >= m_WeatherPhenomenon.m_HotspotRadius))
				{
					num = num * 0.5f - bounds.m_Bounds.max.y;
					if (!(num >= m_BestDistance) && ((bounds.m_Mask & BoundsMask.IsTree) != 0 || m_BuildingData.HasComponent(item)))
					{
						m_SelectedEntity = item;
						m_SelectedPosition = new float3(x.x, bounds.m_Bounds.max.y, x.y);
						m_BestDistance = num;
					}
				}
			}
		}

		private struct EndangeredStaticObjectIterator : INativeQuadTreeIterator<Entity, QuadTreeBoundsXZ>, IUnsafeQuadTreeIterator<Entity, QuadTreeBoundsXZ>
		{
			public int m_JobIndex;

			public uint m_SimulationFrame;

			public float m_DangerSpeed;

			public Entity m_Event;

			public Line2.Segment m_Line;

			public float m_Radius;

			public WeatherPhenomenonData m_WeatherPhenomenonData;

			public ComponentLookup<Building> m_BuildingData;

			public ComponentLookup<Game.Buildings.EmergencyShelter> m_EmergencyShelterData;

			public ComponentLookup<Placeholder> m_PlaceholderData;

			public ComponentLookup<InDanger> m_InDangerData;

			public EntityArchetype m_EndangerArchetype;

			public EntityCommandBuffer.ParallelWriter m_CommandBuffer;

			public bool Intersect(QuadTreeBoundsXZ bounds)
			{
				float2 t;
				return MathUtils.Intersect(MathUtils.Expand(bounds.m_Bounds.xz, m_Radius), m_Line, out t);
			}

			public void Iterate(QuadTreeBoundsXZ bounds, Entity item)
			{
				if (!MathUtils.Intersect(MathUtils.Expand(bounds.m_Bounds.xz, m_Radius), m_Line, out var _))
				{
					return;
				}
				if (!MathUtils.Intersect(bounds.m_Bounds.xz, new Circle2(m_Radius, m_Line.a)))
				{
					float2 value = m_Line.b - m_Line.a;
					if (!MathUtils.TryNormalize(ref value))
					{
						return;
					}
					if (!MathUtils.Intersect(bounds.m_Bounds.xz, new Circle2(m_Radius, m_Line.b)))
					{
						float2 @float = MathUtils.Right(value);
						if (!MathUtils.Intersect(quad: new Quad2(m_Line.a - @float, m_Line.a + @float, m_Line.b + @float, m_Line.b - @float), bounds: bounds.m_Bounds.xz))
						{
							return;
						}
					}
				}
				if (!m_BuildingData.HasComponent(item) || m_PlaceholderData.HasComponent(item))
				{
					return;
				}
				DangerFlags dangerFlags = m_WeatherPhenomenonData.m_DangerFlags;
				if ((dangerFlags & DangerFlags.Evacuate) != 0 && m_EmergencyShelterData.HasComponent(item))
				{
					dangerFlags = (DangerFlags)((uint)dangerFlags & 0xFFFFFFFDu);
					dangerFlags |= DangerFlags.StayIndoors;
				}
				if (m_InDangerData.HasComponent(item))
				{
					InDanger inDanger = m_InDangerData[item];
					if (inDanger.m_EndFrame >= m_SimulationFrame + 64 && (inDanger.m_Event == m_Event || !EventUtils.IsWorse(dangerFlags, inDanger.m_Flags)))
					{
						return;
					}
				}
				float num = 30f + math.max(m_Radius, MathUtils.Distance(bounds.m_Bounds.xz, m_Line.a)) / m_DangerSpeed;
				Entity e = m_CommandBuffer.CreateEntity(m_JobIndex, m_EndangerArchetype);
				m_CommandBuffer.SetComponent(m_JobIndex, e, new Endanger
				{
					m_Event = m_Event,
					m_Target = item,
					m_Flags = dangerFlags,
					m_EndFrame = m_SimulationFrame + 64 + (uint)(num * 60f)
				});
			}
		}

		private struct AffectedStaticObjectIterator : INativeQuadTreeIterator<Entity, QuadTreeBoundsXZ>, IUnsafeQuadTreeIterator<Entity, QuadTreeBoundsXZ>
		{
			public int m_JobIndex;

			public Entity m_Event;

			public Circle2 m_Circle;

			public Game.Events.WeatherPhenomenon m_WeatherPhenomenon;

			public WeatherPhenomenonData m_WeatherPhenomenonData;

			public ComponentLookup<Building> m_BuildingData;

			public ComponentLookup<Transform> m_TransformData;

			public ComponentLookup<Placeholder> m_PlaceholderData;

			public ComponentLookup<Destroyed> m_DestroyedData;

			public ComponentLookup<FacingWeather> m_FacingWeatherData;

			public EntityArchetype m_FaceWeatherArchetype;

			public EntityCommandBuffer.ParallelWriter m_CommandBuffer;

			public bool Intersect(QuadTreeBoundsXZ bounds)
			{
				return MathUtils.Intersect(bounds.m_Bounds.xz, m_Circle);
			}

			public void Iterate(QuadTreeBoundsXZ bounds, Entity item)
			{
				if (!MathUtils.Intersect(bounds.m_Bounds.xz, m_Circle) || !m_BuildingData.HasComponent(item) || m_PlaceholderData.HasComponent(item))
				{
					return;
				}
				float num = 0f;
				if (m_FacingWeatherData.HasComponent(item))
				{
					FacingWeather facingWeather = m_FacingWeatherData[item];
					if (facingWeather.m_Event == m_Event)
					{
						return;
					}
					num = facingWeather.m_Severity;
				}
				if (!m_DestroyedData.HasComponent(item))
				{
					float severity = EventUtils.GetSeverity(m_TransformData[item].m_Position, m_WeatherPhenomenon, m_WeatherPhenomenonData);
					if (severity > num)
					{
						Entity e = m_CommandBuffer.CreateEntity(m_JobIndex, m_FaceWeatherArchetype);
						m_CommandBuffer.SetComponent(m_JobIndex, e, new FaceWeather
						{
							m_Event = m_Event,
							m_Target = item,
							m_Severity = severity
						});
					}
				}
			}
		}

		private struct AffectedNetIterator : INativeQuadTreeIterator<Entity, QuadTreeBoundsXZ>, IUnsafeQuadTreeIterator<Entity, QuadTreeBoundsXZ>
		{
			public int m_JobIndex;

			public Entity m_Event;

			public Circle2 m_Circle;

			public Game.Events.WeatherPhenomenon m_WeatherPhenomenon;

			public TrafficAccidentData m_TrafficAccidentData;

			public Random m_Random;

			public float m_DividedProbability;

			public ComponentLookup<InvolvedInAccident> m_InvolvedInAccidentData;

			public ComponentLookup<Car> m_CarData;

			public ComponentLookup<Bicycle> m_BicycleData;

			public ComponentLookup<Moving> m_MovingData;

			public ComponentLookup<Transform> m_TransformData;

			public BufferLookup<Game.Net.SubLane> m_SubLanes;

			public BufferLookup<LaneObject> m_LaneObjects;

			public EntityArchetype m_EventImpactArchetype;

			public EntityCommandBuffer.ParallelWriter m_CommandBuffer;

			public bool Intersect(QuadTreeBoundsXZ bounds)
			{
				return MathUtils.Intersect(bounds.m_Bounds.xz, m_Circle);
			}

			public void Iterate(QuadTreeBoundsXZ bounds, Entity item)
			{
				if (!MathUtils.Intersect(bounds.m_Bounds.xz, m_Circle) || m_Random.NextFloat(1f) >= m_DividedProbability)
				{
					return;
				}
				Entity entity = TryFindSubject(item, ref m_Random, m_TrafficAccidentData);
				if (entity != Entity.Null)
				{
					float num = math.distance(m_TransformData[entity].m_Position.xz, m_WeatherPhenomenon.m_HotspotPosition.xz);
					float num2 = 4f / 15f;
					float num3 = m_DividedProbability * (m_WeatherPhenomenon.m_HotspotRadius - num) * num2;
					if (!(m_Random.NextFloat(m_WeatherPhenomenon.m_HotspotRadius) >= num3))
					{
						AddImpact(m_JobIndex, m_Event, ref m_Random, entity, m_TrafficAccidentData);
					}
				}
			}

			private Entity TryFindSubject(Entity entity, ref Random random, TrafficAccidentData trafficAccidentData)
			{
				Entity result = Entity.Null;
				int num = 0;
				if (m_SubLanes.HasBuffer(entity))
				{
					DynamicBuffer<Game.Net.SubLane> dynamicBuffer = m_SubLanes[entity];
					for (int i = 0; i < dynamicBuffer.Length; i++)
					{
						Entity subLane = dynamicBuffer[i].m_SubLane;
						if (!m_LaneObjects.HasBuffer(subLane))
						{
							continue;
						}
						DynamicBuffer<LaneObject> dynamicBuffer2 = m_LaneObjects[subLane];
						for (int j = 0; j < dynamicBuffer2.Length; j++)
						{
							Entity laneObject = dynamicBuffer2[j].m_LaneObject;
							if (trafficAccidentData.m_SubjectType == EventTargetType.MovingCar && m_CarData.HasComponent(laneObject) && m_MovingData.HasComponent(laneObject) && !m_BicycleData.HasComponent(laneObject) && !m_InvolvedInAccidentData.HasComponent(laneObject))
							{
								num++;
								if (random.NextInt(num) == num - 1)
								{
									result = laneObject;
								}
							}
						}
					}
				}
				return result;
			}

			private void AddImpact(int jobIndex, Entity eventEntity, ref Random random, Entity target, TrafficAccidentData trafficAccidentData)
			{
				Impact component = new Impact
				{
					m_Event = eventEntity,
					m_Target = target
				};
				if (trafficAccidentData.m_AccidentType == TrafficAccidentType.LoseControl && m_MovingData.HasComponent(target))
				{
					Moving moving = m_MovingData[target];
					component.m_Severity = 5f;
					if (random.NextBool())
					{
						component.m_AngularVelocityDelta.y = -2f;
						component.m_VelocityDelta.xz = component.m_Severity * MathUtils.Left(math.normalizesafe(moving.m_Velocity.xz));
					}
					else
					{
						component.m_AngularVelocityDelta.y = 2f;
						component.m_VelocityDelta.xz = component.m_Severity * MathUtils.Right(math.normalizesafe(moving.m_Velocity.xz));
					}
				}
				Entity e = m_CommandBuffer.CreateEntity(jobIndex, m_EventImpactArchetype);
				m_CommandBuffer.SetComponent(jobIndex, e, component);
			}
		}

		[ReadOnly]
		public uint m_SimulationFrame;

		[ReadOnly]
		public RandomSeed m_RandomSeed;

		[ReadOnly]
		public EntityArchetype m_FaceWeatherArchetype;

		[ReadOnly]
		public EntityArchetype m_ImpactArchetype;

		[ReadOnly]
		public EntityArchetype m_EndangerArchetype;

		[ReadOnly]
		public EntityArchetype m_EventIgniteArchetype;

		[ReadOnly]
		public CellMapData<Wind> m_WindData;

		[ReadOnly]
		public TerrainHeightData m_TerrainHeightData;

		[ReadOnly]
		public WaterSurfaceData<SurfaceWater> m_WaterSurfaceData;

		[ReadOnly]
		public Entity m_City;

		[ReadOnly]
		public NativeQuadTree<Entity, QuadTreeBoundsXZ> m_StaticObjectSearchTree;

		[ReadOnly]
		public NativeQuadTree<Entity, QuadTreeBoundsXZ> m_NetSearchTree;

		public EntityCommandBuffer.ParallelWriter m_CommandBuffer;

		public NativeQueue<LightningStrike>.ParallelWriter m_LightningStrikes;

		[ReadOnly]
		public NativeArray<Entity> m_EarlyDisasterWarningSystems;

		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentTypeHandle<Duration> m_DurationType;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> m_PrefabRefType;

		public ComponentTypeHandle<Game.Events.WeatherPhenomenon> m_WeatherPhenomenonType;

		public BufferTypeHandle<HotspotFrame> m_HotspotFrameType;

		public ComponentTypeHandle<Game.Events.DangerLevel> m_DangerLevelType;

		[ReadOnly]
		public ComponentLookup<Building> m_BuildingData;

		[ReadOnly]
		public ComponentLookup<Game.Buildings.EmergencyShelter> m_EmergencyShelterData;

		[ReadOnly]
		public ComponentLookup<Car> m_CarData;

		[ReadOnly]
		public ComponentLookup<Bicycle> m_BicycleData;

		[ReadOnly]
		public ComponentLookup<Transform> m_TransformData;

		[ReadOnly]
		public ComponentLookup<Moving> m_MovingData;

		[ReadOnly]
		public ComponentLookup<Placeholder> m_PlaceholderData;

		[ReadOnly]
		public ComponentLookup<Destroyed> m_DestroyedData;

		[ReadOnly]
		public ComponentLookup<InvolvedInAccident> m_InvolvedInAccidentData;

		[ReadOnly]
		public ComponentLookup<FacingWeather> m_FacingWeatherData;

		[ReadOnly]
		public ComponentLookup<InDanger> m_InDangerData;

		[ReadOnly]
		public ComponentLookup<PrefabRef> m_PrefabRefData;

		[ReadOnly]
		public ComponentLookup<WeatherPhenomenonData> m_WeatherPhenomenonData;

		[ReadOnly]
		public ComponentLookup<TrafficAccidentData> m_TrafficAccidentData;

		[ReadOnly]
		public ComponentLookup<FireData> m_PrefabFireData;

		[ReadOnly]
		public ComponentLookup<DestructibleObjectData> m_PrefabDestructibleObjectData;

		[ReadOnly]
		public BufferLookup<Game.Net.SubLane> m_SubLanes;

		[ReadOnly]
		public BufferLookup<LaneObject> m_LaneObjects;

		[ReadOnly]
		public BufferLookup<CityModifier> m_CityModifiers;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			float num = 4f / 15f;
			float t = math.pow(0.9f, num);
			int index = (int)((m_SimulationFrame / 16) & 3);
			Random random = m_RandomSeed.GetRandom(unfilteredChunkIndex);
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			NativeArray<Duration> nativeArray2 = chunk.GetNativeArray(ref m_DurationType);
			NativeArray<Game.Events.WeatherPhenomenon> nativeArray3 = chunk.GetNativeArray(ref m_WeatherPhenomenonType);
			NativeArray<PrefabRef> nativeArray4 = chunk.GetNativeArray(ref m_PrefabRefType);
			BufferAccessor<HotspotFrame> bufferAccessor = chunk.GetBufferAccessor(ref m_HotspotFrameType);
			NativeArray<Game.Events.DangerLevel> nativeArray5 = chunk.GetNativeArray(ref m_DangerLevelType);
			for (int i = 0; i < nativeArray3.Length; i++)
			{
				Entity entity = nativeArray[i];
				Duration duration = nativeArray2[i];
				Game.Events.WeatherPhenomenon weatherPhenomenon = nativeArray3[i];
				PrefabRef eventPrefabRef = nativeArray4[i];
				WeatherPhenomenonData weatherPhenomenonData = m_WeatherPhenomenonData[eventPrefabRef.m_Prefab];
				float intensity = weatherPhenomenon.m_Intensity;
				if (duration.m_EndFrame <= m_SimulationFrame)
				{
					weatherPhenomenon.m_Intensity = math.max(0f, weatherPhenomenon.m_Intensity - num * 0.2f);
				}
				else if (duration.m_StartFrame <= m_SimulationFrame)
				{
					weatherPhenomenon.m_Intensity = math.min(1f, weatherPhenomenon.m_Intensity + num * 0.2f);
				}
				float2 @float = Wind.SampleWind(m_WindData, weatherPhenomenon.m_PhenomenonPosition) * 20f;
				if (weatherPhenomenon.m_Intensity != 0f)
				{
					weatherPhenomenon.m_PhenomenonPosition.xz += @float * num;
					weatherPhenomenon.m_PhenomenonPosition.y = WaterUtils.SampleHeight(ref m_WaterSurfaceData, ref m_TerrainHeightData, weatherPhenomenon.m_PhenomenonPosition);
					float num2 = weatherPhenomenon.m_PhenomenonRadius - weatherPhenomenon.m_HotspotRadius;
					float2 float2 = weatherPhenomenon.m_PhenomenonPosition.xz - weatherPhenomenon.m_HotspotPosition.xz;
					float2 start = @float + (float2 + random.NextFloat2(0f - num2, num2)) * weatherPhenomenonData.m_HotspotInstability;
					weatherPhenomenon.m_HotspotVelocity.xz = math.lerp(start, weatherPhenomenon.m_HotspotVelocity.xz, t);
					float num3 = math.length(float2);
					if (num3 >= 0.001f)
					{
						float num4 = (num2 - num3) * weatherPhenomenonData.m_HotspotInstability;
						float num5 = math.dot(float2, @float - weatherPhenomenon.m_HotspotVelocity.xz) / num3;
						weatherPhenomenon.m_HotspotVelocity.xz += float2 * (math.max(0f, num5 - num4) / num3);
					}
					weatherPhenomenon.m_HotspotPosition += weatherPhenomenon.m_HotspotVelocity * num;
					weatherPhenomenon.m_HotspotPosition.y = WaterUtils.SampleHeight(ref m_WaterSurfaceData, ref m_TerrainHeightData, weatherPhenomenon.m_HotspotPosition);
					if (weatherPhenomenonData.m_DamageSeverity != 0f)
					{
						FindAffectedObjects(unfilteredChunkIndex, entity, weatherPhenomenon, weatherPhenomenonData);
					}
					if (weatherPhenomenon.m_LightningTimer != 0f)
					{
						weatherPhenomenon.m_LightningTimer -= num;
						while (weatherPhenomenon.m_LightningTimer <= 0f)
						{
							LightningStrike(ref random, unfilteredChunkIndex, entity, weatherPhenomenon, eventPrefabRef);
							float num6 = random.NextFloat(weatherPhenomenonData.m_LightningInterval.min, weatherPhenomenonData.m_LightningInterval.max);
							if (num6 <= 0f)
							{
								weatherPhenomenon.m_LightningTimer = 0f;
								break;
							}
							weatherPhenomenon.m_LightningTimer += num6;
						}
					}
					if (m_TrafficAccidentData.HasComponent(eventPrefabRef.m_Prefab))
					{
						TrafficAccidentData trafficAccidentData = m_TrafficAccidentData[eventPrefabRef.m_Prefab];
						FindAffectedEdges(unfilteredChunkIndex, ref random, entity, weatherPhenomenon, trafficAccidentData);
					}
				}
				else
				{
					weatherPhenomenon.m_HotspotVelocity = 0f;
				}
				if (bufferAccessor.Length != 0)
				{
					DynamicBuffer<HotspotFrame> dynamicBuffer = bufferAccessor[i];
					dynamicBuffer[index] = new HotspotFrame
					{
						m_Position = weatherPhenomenon.m_HotspotPosition - weatherPhenomenon.m_HotspotVelocity * num * 0.5f,
						m_Velocity = weatherPhenomenon.m_HotspotVelocity
					};
				}
				if (m_SimulationFrame < duration.m_EndFrame && weatherPhenomenonData.m_DangerFlags != 0)
				{
					FindEndangeredObjects(unfilteredChunkIndex, entity, duration, weatherPhenomenon, @float, weatherPhenomenonData);
				}
				if (intensity != 0f != (weatherPhenomenon.m_Intensity != 0f))
				{
					m_CommandBuffer.AddComponent(unfilteredChunkIndex, entity, default(EffectsUpdated));
				}
				bool flag = m_SimulationFrame > duration.m_StartFrame && m_SimulationFrame < duration.m_EndFrame;
				nativeArray5[i] = new Game.Events.DangerLevel(flag ? weatherPhenomenonData.m_DangerLevel : 0f);
				nativeArray3[i] = weatherPhenomenon;
				if (m_SimulationFrame <= duration.m_EndFrame)
				{
					continue;
				}
				m_CommandBuffer.AddComponent(unfilteredChunkIndex, entity, default(Deleted));
				foreach (Entity item in m_EarlyDisasterWarningSystems)
				{
					m_CommandBuffer.AddComponent(unfilteredChunkIndex, item, default(EffectsUpdated));
				}
			}
		}

		private void LightningStrike(ref Random random, int jobIndex, Entity eventEntity, Game.Events.WeatherPhenomenon weatherPhenomenon, PrefabRef eventPrefabRef)
		{
			LightningTargetIterator iterator = new LightningTargetIterator
			{
				m_WeatherPhenomenon = weatherPhenomenon,
				m_BestDistance = float.MaxValue,
				m_BuildingData = m_BuildingData
			};
			m_StaticObjectSearchTree.Iterate(ref iterator);
			if (iterator.m_SelectedEntity != Entity.Null)
			{
				m_LightningStrikes.Enqueue(new LightningStrike
				{
					m_HitEntity = iterator.m_SelectedEntity,
					m_Position = iterator.m_SelectedPosition
				});
			}
			if (!m_PrefabRefData.TryGetComponent(iterator.m_SelectedEntity, out var componentData))
			{
				return;
			}
			bool flag = false;
			if (m_PrefabFireData.TryGetComponent(eventPrefabRef.m_Prefab, out var componentData2))
			{
				float startProbability = componentData2.m_StartProbability;
				if (startProbability > 0.01f)
				{
					flag = random.NextFloat(100f) < startProbability;
				}
			}
			if (flag && m_PrefabDestructibleObjectData.TryGetComponent(componentData, out var componentData3) && componentData3.m_FireHazard == 0f)
			{
				flag = false;
			}
			if (flag)
			{
				Ignite component = new Ignite
				{
					m_Target = iterator.m_SelectedEntity,
					m_Event = eventEntity,
					m_Intensity = componentData2.m_StartIntensity,
					m_RequestFrame = m_SimulationFrame
				};
				Entity e = m_CommandBuffer.CreateEntity(jobIndex, m_EventIgniteArchetype);
				m_CommandBuffer.SetComponent(jobIndex, e, component);
			}
		}

		private void FindEndangeredObjects(int jobIndex, Entity eventEntity, Duration duration, Game.Events.WeatherPhenomenon weatherPhenomenon, float2 wind, WeatherPhenomenonData weatherPhenomenonData)
		{
			float value = 0f;
			DynamicBuffer<CityModifier> modifiers = m_CityModifiers[m_City];
			CityUtils.ApplyModifier(ref value, modifiers, CityModifierType.DisasterWarningTime);
			if (duration.m_StartFrame > m_SimulationFrame)
			{
				value -= (float)(duration.m_StartFrame - m_SimulationFrame) / 60f;
			}
			value = math.max(0f, value);
			EndangeredStaticObjectIterator iterator = new EndangeredStaticObjectIterator
			{
				m_JobIndex = jobIndex,
				m_DangerSpeed = math.length(wind),
				m_SimulationFrame = m_SimulationFrame,
				m_Event = eventEntity,
				m_Line = new Line2.Segment(weatherPhenomenon.m_PhenomenonPosition.xz, weatherPhenomenon.m_PhenomenonPosition.xz + wind * value),
				m_Radius = weatherPhenomenon.m_PhenomenonRadius,
				m_WeatherPhenomenonData = weatherPhenomenonData,
				m_BuildingData = m_BuildingData,
				m_EmergencyShelterData = m_EmergencyShelterData,
				m_PlaceholderData = m_PlaceholderData,
				m_InDangerData = m_InDangerData,
				m_EndangerArchetype = m_EndangerArchetype,
				m_CommandBuffer = m_CommandBuffer
			};
			m_StaticObjectSearchTree.Iterate(ref iterator);
		}

		private void FindAffectedObjects(int jobIndex, Entity eventEntity, Game.Events.WeatherPhenomenon weatherPhenomenon, WeatherPhenomenonData weatherPhenomenonData)
		{
			AffectedStaticObjectIterator iterator = new AffectedStaticObjectIterator
			{
				m_JobIndex = jobIndex,
				m_Event = eventEntity,
				m_Circle = new Circle2(weatherPhenomenon.m_HotspotRadius, weatherPhenomenon.m_HotspotPosition.xz),
				m_WeatherPhenomenon = weatherPhenomenon,
				m_WeatherPhenomenonData = weatherPhenomenonData,
				m_BuildingData = m_BuildingData,
				m_TransformData = m_TransformData,
				m_PlaceholderData = m_PlaceholderData,
				m_DestroyedData = m_DestroyedData,
				m_FacingWeatherData = m_FacingWeatherData,
				m_FaceWeatherArchetype = m_FaceWeatherArchetype,
				m_CommandBuffer = m_CommandBuffer
			};
			m_StaticObjectSearchTree.Iterate(ref iterator);
		}

		private void FindAffectedEdges(int jobIndex, ref Random random, Entity eventEntity, Game.Events.WeatherPhenomenon weatherPhenomenon, TrafficAccidentData trafficAccidentData)
		{
			float dividedProbability = math.sqrt(trafficAccidentData.m_OccurenceProbability * 0.01f);
			AffectedNetIterator iterator = new AffectedNetIterator
			{
				m_JobIndex = jobIndex,
				m_Event = eventEntity,
				m_Circle = new Circle2(weatherPhenomenon.m_HotspotRadius, weatherPhenomenon.m_HotspotPosition.xz),
				m_WeatherPhenomenon = weatherPhenomenon,
				m_TrafficAccidentData = trafficAccidentData,
				m_Random = random,
				m_DividedProbability = dividedProbability,
				m_InvolvedInAccidentData = m_InvolvedInAccidentData,
				m_CarData = m_CarData,
				m_BicycleData = m_BicycleData,
				m_MovingData = m_MovingData,
				m_TransformData = m_TransformData,
				m_SubLanes = m_SubLanes,
				m_LaneObjects = m_LaneObjects,
				m_EventImpactArchetype = m_ImpactArchetype,
				m_CommandBuffer = m_CommandBuffer
			};
			m_NetSearchTree.Iterate(ref iterator);
			random = iterator.m_Random;
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	private struct TypeHandle
	{
		[ReadOnly]
		public EntityTypeHandle __Unity_Entities_Entity_TypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Duration> __Game_Events_Duration_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentTypeHandle;

		public ComponentTypeHandle<Game.Events.WeatherPhenomenon> __Game_Events_WeatherPhenomenon_RW_ComponentTypeHandle;

		public BufferTypeHandle<HotspotFrame> __Game_Events_HotspotFrame_RW_BufferTypeHandle;

		public ComponentTypeHandle<Game.Events.DangerLevel> __Game_Events_DangerLevel_RW_ComponentTypeHandle;

		[ReadOnly]
		public ComponentLookup<Building> __Game_Buildings_Building_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Buildings.EmergencyShelter> __Game_Buildings_EmergencyShelter_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Car> __Game_Vehicles_Car_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Bicycle> __Game_Vehicles_Bicycle_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Transform> __Game_Objects_Transform_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Moving> __Game_Objects_Moving_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Placeholder> __Game_Objects_Placeholder_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Destroyed> __Game_Common_Destroyed_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<InvolvedInAccident> __Game_Events_InvolvedInAccident_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<FacingWeather> __Game_Events_FacingWeather_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<InDanger> __Game_Events_InDanger_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<WeatherPhenomenonData> __Game_Prefabs_WeatherPhenomenonData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<TrafficAccidentData> __Game_Prefabs_TrafficAccidentData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<FireData> __Game_Prefabs_FireData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<DestructibleObjectData> __Game_Prefabs_DestructibleObjectData_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<Game.Net.SubLane> __Game_Net_SubLane_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<LaneObject> __Game_Net_LaneObject_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<CityModifier> __Game_City_CityModifier_RO_BufferLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
			__Game_Events_Duration_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Duration>(isReadOnly: true);
			__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PrefabRef>(isReadOnly: true);
			__Game_Events_WeatherPhenomenon_RW_ComponentTypeHandle = state.GetComponentTypeHandle<Game.Events.WeatherPhenomenon>();
			__Game_Events_HotspotFrame_RW_BufferTypeHandle = state.GetBufferTypeHandle<HotspotFrame>();
			__Game_Events_DangerLevel_RW_ComponentTypeHandle = state.GetComponentTypeHandle<Game.Events.DangerLevel>();
			__Game_Buildings_Building_RO_ComponentLookup = state.GetComponentLookup<Building>(isReadOnly: true);
			__Game_Buildings_EmergencyShelter_RO_ComponentLookup = state.GetComponentLookup<Game.Buildings.EmergencyShelter>(isReadOnly: true);
			__Game_Vehicles_Car_RO_ComponentLookup = state.GetComponentLookup<Car>(isReadOnly: true);
			__Game_Vehicles_Bicycle_RO_ComponentLookup = state.GetComponentLookup<Bicycle>(isReadOnly: true);
			__Game_Objects_Transform_RO_ComponentLookup = state.GetComponentLookup<Transform>(isReadOnly: true);
			__Game_Objects_Moving_RO_ComponentLookup = state.GetComponentLookup<Moving>(isReadOnly: true);
			__Game_Objects_Placeholder_RO_ComponentLookup = state.GetComponentLookup<Placeholder>(isReadOnly: true);
			__Game_Common_Destroyed_RO_ComponentLookup = state.GetComponentLookup<Destroyed>(isReadOnly: true);
			__Game_Events_InvolvedInAccident_RO_ComponentLookup = state.GetComponentLookup<InvolvedInAccident>(isReadOnly: true);
			__Game_Events_FacingWeather_RO_ComponentLookup = state.GetComponentLookup<FacingWeather>(isReadOnly: true);
			__Game_Events_InDanger_RO_ComponentLookup = state.GetComponentLookup<InDanger>(isReadOnly: true);
			__Game_Prefabs_PrefabRef_RO_ComponentLookup = state.GetComponentLookup<PrefabRef>(isReadOnly: true);
			__Game_Prefabs_WeatherPhenomenonData_RO_ComponentLookup = state.GetComponentLookup<WeatherPhenomenonData>(isReadOnly: true);
			__Game_Prefabs_TrafficAccidentData_RO_ComponentLookup = state.GetComponentLookup<TrafficAccidentData>(isReadOnly: true);
			__Game_Prefabs_FireData_RO_ComponentLookup = state.GetComponentLookup<FireData>(isReadOnly: true);
			__Game_Prefabs_DestructibleObjectData_RO_ComponentLookup = state.GetComponentLookup<DestructibleObjectData>(isReadOnly: true);
			__Game_Net_SubLane_RO_BufferLookup = state.GetBufferLookup<Game.Net.SubLane>(isReadOnly: true);
			__Game_Net_LaneObject_RO_BufferLookup = state.GetBufferLookup<LaneObject>(isReadOnly: true);
			__Game_City_CityModifier_RO_BufferLookup = state.GetBufferLookup<CityModifier>(isReadOnly: true);
		}
	}

	private SimulationSystem m_SimulationSystem;

	private WindSystem m_WindSystem;

	private TerrainSystem m_TerrainSystem;

	private WaterSystem m_WaterSystem;

	private CitySystem m_CitySystem;

	private Game.Objects.SearchSystem m_ObjectSearchSystem;

	private Game.Net.SearchSystem m_NetSearchSystem;

	private ClimateRenderSystem m_ClimateRenderSystem;

	private EndFrameBarrier m_EndFrameBarrier;

	private EntityQuery m_PhenomenonQuery;

	private EntityArchetype m_FaceWeatherArchetype;

	private EntityArchetype m_ImpactArchetype;

	private EntityArchetype m_EndangerArchetype;

	private EntityArchetype m_EventIgniteArchetype;

	private EntityQuery m_EDWSBuildingQuery;

	private TypeHandle __TypeHandle;

	public override int GetUpdateInterval(SystemUpdatePhase phase)
	{
		return 16;
	}

	public override int GetUpdateOffset(SystemUpdatePhase phase)
	{
		return 0;
	}

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_SimulationSystem = base.World.GetOrCreateSystemManaged<SimulationSystem>();
		m_WindSystem = base.World.GetOrCreateSystemManaged<WindSystem>();
		m_TerrainSystem = base.World.GetOrCreateSystemManaged<TerrainSystem>();
		m_WaterSystem = base.World.GetOrCreateSystemManaged<WaterSystem>();
		m_CitySystem = base.World.GetOrCreateSystemManaged<CitySystem>();
		m_ObjectSearchSystem = base.World.GetOrCreateSystemManaged<Game.Objects.SearchSystem>();
		m_NetSearchSystem = base.World.GetOrCreateSystemManaged<Game.Net.SearchSystem>();
		m_ClimateRenderSystem = base.World.GetExistingSystemManaged<ClimateRenderSystem>();
		m_EndFrameBarrier = base.World.GetOrCreateSystemManaged<EndFrameBarrier>();
		m_PhenomenonQuery = GetEntityQuery(ComponentType.ReadWrite<Game.Events.WeatherPhenomenon>(), ComponentType.Exclude<Deleted>(), ComponentType.Exclude<Temp>());
		m_FaceWeatherArchetype = base.EntityManager.CreateArchetype(ComponentType.ReadWrite<Game.Common.Event>(), ComponentType.ReadWrite<FaceWeather>());
		m_ImpactArchetype = base.EntityManager.CreateArchetype(ComponentType.ReadWrite<Game.Common.Event>(), ComponentType.ReadWrite<Impact>());
		m_EndangerArchetype = base.EntityManager.CreateArchetype(ComponentType.ReadWrite<Game.Common.Event>(), ComponentType.ReadWrite<Endanger>());
		m_EventIgniteArchetype = base.EntityManager.CreateArchetype(ComponentType.ReadWrite<Game.Common.Event>(), ComponentType.ReadWrite<Ignite>());
		m_EDWSBuildingQuery = GetEntityQuery(ComponentType.ReadOnly<Game.Buildings.EarlyDisasterWarningSystem>());
		RequireForUpdate(m_PhenomenonQuery);
	}

	[Preserve]
	protected override void OnUpdate()
	{
		JobHandle dependencies;
		JobHandle deps;
		JobHandle dependencies2;
		JobHandle dependencies3;
		JobHandle dependencies4;
		WeatherPhenomenonJob jobData = new WeatherPhenomenonJob
		{
			m_SimulationFrame = m_SimulationSystem.frameIndex,
			m_RandomSeed = RandomSeed.Next(),
			m_FaceWeatherArchetype = m_FaceWeatherArchetype,
			m_ImpactArchetype = m_ImpactArchetype,
			m_EndangerArchetype = m_EndangerArchetype,
			m_EventIgniteArchetype = m_EventIgniteArchetype,
			m_WindData = m_WindSystem.GetData(readOnly: true, out dependencies),
			m_TerrainHeightData = m_TerrainSystem.GetHeightData(),
			m_WaterSurfaceData = m_WaterSystem.GetSurfaceData(out deps),
			m_City = m_CitySystem.City,
			m_StaticObjectSearchTree = m_ObjectSearchSystem.GetStaticSearchTree(readOnly: true, out dependencies2),
			m_NetSearchTree = m_NetSearchSystem.GetNetSearchTree(readOnly: true, out dependencies3),
			m_CommandBuffer = m_EndFrameBarrier.CreateCommandBuffer().AsParallelWriter(),
			m_LightningStrikes = m_ClimateRenderSystem.GetLightningStrikeQueue(out dependencies4).AsParallelWriter(),
			m_EarlyDisasterWarningSystems = m_EDWSBuildingQuery.ToEntityArray(Allocator.TempJob),
			m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
			m_DurationType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Events_Duration_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_PrefabRefType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_WeatherPhenomenonType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Events_WeatherPhenomenon_RW_ComponentTypeHandle, ref base.CheckedStateRef),
			m_HotspotFrameType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Events_HotspotFrame_RW_BufferTypeHandle, ref base.CheckedStateRef),
			m_DangerLevelType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Events_DangerLevel_RW_ComponentTypeHandle, ref base.CheckedStateRef),
			m_BuildingData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_Building_RO_ComponentLookup, ref base.CheckedStateRef),
			m_EmergencyShelterData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_EmergencyShelter_RO_ComponentLookup, ref base.CheckedStateRef),
			m_CarData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Vehicles_Car_RO_ComponentLookup, ref base.CheckedStateRef),
			m_BicycleData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Vehicles_Bicycle_RO_ComponentLookup, ref base.CheckedStateRef),
			m_TransformData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Transform_RO_ComponentLookup, ref base.CheckedStateRef),
			m_MovingData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Moving_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PlaceholderData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Placeholder_RO_ComponentLookup, ref base.CheckedStateRef),
			m_DestroyedData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Common_Destroyed_RO_ComponentLookup, ref base.CheckedStateRef),
			m_InvolvedInAccidentData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Events_InvolvedInAccident_RO_ComponentLookup, ref base.CheckedStateRef),
			m_FacingWeatherData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Events_FacingWeather_RO_ComponentLookup, ref base.CheckedStateRef),
			m_InDangerData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Events_InDanger_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabRefData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup, ref base.CheckedStateRef),
			m_WeatherPhenomenonData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_WeatherPhenomenonData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_TrafficAccidentData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_TrafficAccidentData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabFireData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_FireData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabDestructibleObjectData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_DestructibleObjectData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_SubLanes = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Net_SubLane_RO_BufferLookup, ref base.CheckedStateRef),
			m_LaneObjects = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Net_LaneObject_RO_BufferLookup, ref base.CheckedStateRef),
			m_CityModifiers = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_City_CityModifier_RO_BufferLookup, ref base.CheckedStateRef)
		};
		JobHandle jobHandle = JobChunkExtensions.ScheduleParallel(jobData, m_PhenomenonQuery, JobUtils.CombineDependencies(base.Dependency, dependencies, deps, dependencies2, dependencies3, dependencies4));
		jobData.m_EarlyDisasterWarningSystems.Dispose(jobHandle);
		m_ObjectSearchSystem.AddStaticSearchTreeReader(jobHandle);
		m_NetSearchSystem.AddNetSearchTreeReader(jobHandle);
		m_EndFrameBarrier.AddJobHandleForProducer(jobHandle);
		m_WindSystem.AddReader(jobHandle);
		m_TerrainSystem.AddCPUHeightReader(jobHandle);
		m_WaterSystem.AddSurfaceReader(jobHandle);
		m_ClimateRenderSystem.AddLightningStrikeWriter(jobHandle);
		base.Dependency = jobHandle;
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
	public WeatherPhenomenonSystem()
	{
	}
}
