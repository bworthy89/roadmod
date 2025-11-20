using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Colossal.Mathematics;
using Colossal.Serialization.Entities;
using Game.Common;
using Game.Creatures;
using Game.Net;
using Game.Objects;
using Game.Pathfind;
using Game.Prefabs;
using Game.Routes;
using Game.Serialization;
using Game.Simulation;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Scripting;

namespace Game.Rendering;

[CompilerGenerated]
public class RouteBufferSystem : GameSystemBase, IPreDeserialize
{
	private class ManagedData : IDisposable
	{
		public Material m_Material;

		public ComputeBuffer m_SegmentBuffer;

		public Vector4 m_Size;

		public int m_OriginalRenderQueue;

		public bool m_Updated;

		public void Initialize(RoutePrefab routePrefab)
		{
			if (m_Material != null)
			{
				UnityEngine.Object.Destroy(m_Material);
			}
			m_Material = new Material(routePrefab.m_Material);
			m_Material.name = "Routes (" + routePrefab.name + ")";
			m_OriginalRenderQueue = m_Material.renderQueue;
			m_Size = new Vector4(routePrefab.m_Width, routePrefab.m_Width * 0.25f, routePrefab.m_SegmentLength, 0f);
		}

		public void Dispose()
		{
			if (m_Material != null)
			{
				UnityEngine.Object.Destroy(m_Material);
			}
			if (m_SegmentBuffer != null)
			{
				m_SegmentBuffer.Release();
			}
		}
	}

	private struct NativeData : IDisposable
	{
		public UnsafeList<SegmentData> m_SegmentData;

		public Bounds3 m_Bounds;

		public Entity m_Entity;

		public float m_Length;

		public bool m_Updated;

		public void Initialize(Entity entity)
		{
			m_Entity = entity;
		}

		public void Dispose()
		{
			if (m_SegmentData.IsCreated)
			{
				m_SegmentData.Dispose();
			}
		}
	}

	private struct SegmentData
	{
		public float4x4 m_Curve;

		public float3 m_Position;

		public float3 m_SizeFactor;

		public float2 m_Opacity;

		public float2 m_DividedOpacity;

		public float m_Broken;
	}

	private struct CurveKey : IEquatable<CurveKey>
	{
		public Line3.Segment m_Line;

		public Entity m_Entity;

		public float2 m_Range;

		public bool Equals(CurveKey other)
		{
			if (m_Entity != Entity.Null)
			{
				if (m_Entity.Equals(other.m_Entity))
				{
					return m_Range.Equals(other.m_Range);
				}
				return false;
			}
			if (m_Line.a.Equals(other.m_Line.a))
			{
				return m_Line.b.Equals(other.m_Line.b);
			}
			return false;
		}

		public override int GetHashCode()
		{
			if (m_Entity != Entity.Null)
			{
				return m_Entity.GetHashCode() ^ m_Range.GetHashCode();
			}
			return m_Line.a.GetHashCode() ^ m_Line.b.GetHashCode();
		}
	}

	private struct CurveValue
	{
		public int m_SegmentDataIndex;

		public int m_SharedCount;
	}

	private struct SourceKey : IEquatable<SourceKey>
	{
		public Entity m_Entity;

		public bool m_Forward;

		public bool Equals(SourceKey other)
		{
			if (m_Entity.Equals(other.m_Entity))
			{
				return m_Forward == other.m_Forward;
			}
			return false;
		}

		public override int GetHashCode()
		{
			return m_Entity.GetHashCode() ^ m_Forward.GetHashCode();
		}
	}

	[BurstCompile]
	private struct UpdateBufferJob : IJobParallelFor
	{
		[ReadOnly]
		public EntityStorageInfoLookup m_EntityLookup;

		[ReadOnly]
		public SharedComponentTypeHandle<UpdateFrame> m_UpdateFrameType;

		[ReadOnly]
		public ComponentLookup<Position> m_PositionData;

		[ReadOnly]
		public ComponentLookup<PathSource> m_PathSourceData;

		[ReadOnly]
		public ComponentLookup<LivePath> m_LivePathData;

		[ReadOnly]
		public ComponentLookup<Curve> m_CurveData;

		[ReadOnly]
		public ComponentLookup<Game.Objects.Transform> m_TransformData;

		[ReadOnly]
		public ComponentLookup<Unspawned> m_UnspawnedData;

		[ReadOnly]
		public ComponentLookup<CurrentVehicle> m_CurrentVehicleData;

		[ReadOnly]
		public BufferLookup<RouteWaypoint> m_RouteWaypoints;

		[ReadOnly]
		public BufferLookup<RouteSegment> m_RouteSegments;

		[ReadOnly]
		public BufferLookup<CurveElement> m_CurveElements;

		[ReadOnly]
		public BufferLookup<CurveSource> m_CurveSources;

		[ReadOnly]
		public BufferLookup<TransformFrame> m_TransformFrames;

		[ReadOnly]
		public BufferLookup<PathElement> m_PathElements;

		[ReadOnly]
		public uint m_FrameIndex;

		[ReadOnly]
		public float m_FrameTime;

		[NativeDisableParallelForRestriction]
		public NativeList<NativeData> m_NativeData;

		public void Execute(int index)
		{
			ref NativeData reference = ref m_NativeData.ElementAt(index);
			if (!reference.m_Updated)
			{
				return;
			}
			reference.m_Updated = false;
			reference.m_SegmentData.Clear();
			DynamicBuffer<RouteWaypoint> dynamicBuffer = m_RouteWaypoints[reference.m_Entity];
			DynamicBuffer<RouteSegment> dynamicBuffer2 = m_RouteSegments[reference.m_Entity];
			NativeHashMap<CurveKey, CurveValue> curveMap = default(NativeHashMap<CurveKey, CurveValue>);
			NativeParallelMultiHashMap<SourceKey, float> nativeParallelMultiHashMap = default(NativeParallelMultiHashMap<SourceKey, float>);
			NativeList<int> nativeList = default(NativeList<int>);
			NativeList<float> list = default(NativeList<float>);
			if (m_LivePathData.HasComponent(reference.m_Entity))
			{
				curveMap = new NativeHashMap<CurveKey, CurveValue>(1000, Allocator.Temp);
				nativeParallelMultiHashMap = new NativeParallelMultiHashMap<SourceKey, float>(1000, Allocator.Temp);
				nativeList = new NativeList<int>(1000, Allocator.Temp);
				list = new NativeList<float>(100, Allocator.Temp);
				for (int i = 0; i < dynamicBuffer2.Length; i++)
				{
					Entity segment = dynamicBuffer2[i].m_Segment;
					if (segment == Entity.Null)
					{
						continue;
					}
					DynamicBuffer<CurveSource> dynamicBuffer3 = m_CurveSources[segment];
					for (int j = 0; j < dynamicBuffer3.Length; j++)
					{
						CurveSource curveSource = dynamicBuffer3[j];
						if (curveSource.m_Range.x != curveSource.m_Range.y)
						{
							if (curveSource.m_Range.x != 0f && curveSource.m_Range.x != 1f)
							{
								nativeParallelMultiHashMap.Add(new SourceKey
								{
									m_Entity = curveSource.m_Entity,
									m_Forward = (curveSource.m_Range.y > curveSource.m_Range.x)
								}, curveSource.m_Range.x);
							}
							if (curveSource.m_Range.y != 0f && curveSource.m_Range.y != 1f)
							{
								nativeParallelMultiHashMap.Add(new SourceKey
								{
									m_Entity = curveSource.m_Entity,
									m_Forward = (curveSource.m_Range.y > curveSource.m_Range.x)
								}, curveSource.m_Range.y);
							}
						}
					}
				}
			}
			float num = 0f;
			Bounds3 bounds = new Bounds3(float.MaxValue, float.MinValue);
			for (int k = 0; k < dynamicBuffer2.Length; k++)
			{
				Entity segment2 = dynamicBuffer2[k].m_Segment;
				if (segment2 == Entity.Null)
				{
					continue;
				}
				float broken = 0f;
				if (m_PathElements.TryGetBuffer(segment2, out var bufferData))
				{
					broken = math.select(0f, 1f, bufferData.Length == 0);
				}
				DynamicBuffer<CurveElement> dynamicBuffer4 = m_CurveElements[segment2];
				DynamicBuffer<CurveSource> dynamicBuffer5 = default(DynamicBuffer<CurveSource>);
				float3 y = default(float3);
				float3 y2 = default(float3);
				float3 @float = default(float3);
				float3 float2 = default(float3);
				if (dynamicBuffer4.Length > 0)
				{
					@float = math.normalizesafe(MathUtils.StartTangent(dynamicBuffer4[0].m_Curve));
					float2 = dynamicBuffer4[0].m_Curve.a;
					if (m_PathSourceData.TryGetComponent(segment2, out var componentData))
					{
						dynamicBuffer5 = m_CurveSources[segment2];
						num = 0f;
						if (m_TransformData.TryGetComponent(componentData.m_Entity, out var componentData2) && !m_UnspawnedData.HasComponent(componentData.m_Entity))
						{
							EntityStorageInfo entityStorageInfo = m_EntityLookup[componentData.m_Entity];
							bool flag = false;
							if (entityStorageInfo.Chunk.Has(m_UpdateFrameType) && m_TransformFrames.TryGetBuffer(componentData.m_Entity, out var bufferData2))
							{
								UpdateFrame sharedComponent = entityStorageInfo.Chunk.GetSharedComponent(m_UpdateFrameType);
								ObjectInterpolateSystem.CalculateUpdateFrames(m_FrameIndex, m_FrameTime, sharedComponent.m_Index, out var updateFrame, out var updateFrame2, out var framePosition);
								TransformFrame frame = bufferData2[(int)updateFrame];
								TransformFrame frame2 = bufferData2[(int)updateFrame2];
								componentData2 = ObjectInterpolateSystem.CalculateTransform(frame, frame2, framePosition).ToTransform();
							}
							else if (m_CurrentVehicleData.HasComponent(componentData.m_Entity))
							{
								flag = true;
							}
							if (!flag)
							{
								float3 a = dynamicBuffer4[0].m_Curve.a;
								float3 value = a - componentData2.m_Position;
								value = MathUtils.Normalize(value, value.xz);
								Bezier4x3 curve;
								if (@float.Equals(default(float3)))
								{
									curve = NetUtils.StraightCurve(componentData2.m_Position, a);
									float num2 = (curve.a.y - curve.b.y) / math.max(1f, math.abs(value.y));
									curve.b.y += num2;
									curve.c.y += num2;
									@float = math.normalizesafe(MathUtils.EndTangent(curve));
								}
								else
								{
									float3 float3 = MathUtils.Normalize(@float, @float.xz);
									float3 startTangent = value * (math.dot(float3, value) * 2f) - float3;
									curve = NetUtils.FitCurve(componentData2.m_Position, startTangent, float3, a);
								}
								float num3 = MathUtils.Length(curve);
								float2 offset = new float2(num, num + num3);
								int value2 = AddSegment(ref reference.m_SegmentData, curveMap, curve, Entity.Null, default(float2), offset, new float2(0f, 1f), broken);
								num = offset.y;
								bounds |= MathUtils.Bounds(curve);
								y = @float;
								y2 = float2;
								if (nativeList.IsCreated)
								{
									nativeList.Add(in value2);
								}
							}
						}
					}
				}
				if (!@float.Equals(default(float3)))
				{
					for (int l = 0; l < dynamicBuffer4.Length; l++)
					{
						CurveElement curveElement = dynamicBuffer4[l];
						float3 x = @float;
						float3 x2 = float2;
						float3 float4 = math.normalizesafe(MathUtils.EndTangent(curveElement.m_Curve));
						float3 d = curveElement.m_Curve.d;
						if (l + 1 < dynamicBuffer4.Length)
						{
							@float = math.normalizesafe(MathUtils.StartTangent(dynamicBuffer4[l + 1].m_Curve));
							float2 = dynamicBuffer4[l + 1].m_Curve.a;
						}
						else
						{
							@float = default(float3);
							float2 = default(float3);
						}
						float2 float5 = new float2(math.dot(x, y), math.dot(float4, @float));
						float2 float6 = new float2(math.distancesq(x2, y2), math.distancesq(d, float2));
						float2 float7 = math.select(1f, 0f, (float5 < 0.99f) | (float6 > 0.01f));
						bounds |= MathUtils.Bounds(curveElement.m_Curve);
						y = float4;
						y2 = d;
						CurveSource curveSource2 = default(CurveSource);
						if (dynamicBuffer5.IsCreated && nativeParallelMultiHashMap.IsCreated)
						{
							curveSource2 = dynamicBuffer5[l];
							if (curveSource2.m_Range.x != curveSource2.m_Range.y)
							{
								bool flag2 = curveSource2.m_Range.y > curveSource2.m_Range.x;
								if (nativeParallelMultiHashMap.TryGetFirstValue(new SourceKey
								{
									m_Entity = curveSource2.m_Entity,
									m_Forward = flag2
								}, out var item, out var it))
								{
									do
									{
										list.Add(in item);
									}
									while (nativeParallelMultiHashMap.TryGetNextValue(out item, ref it));
									if (list.Length >= 2)
									{
										list.Sort();
									}
									if (!flag2)
									{
										curveElement.m_Curve = MathUtils.Invert(curveElement.m_Curve);
									}
									if (((curveSource2.m_Range.x != 0f && curveSource2.m_Range.x != 1f) || (curveSource2.m_Range.y != 0f && curveSource2.m_Range.y != 1f)) && m_CurveData.TryGetComponent(curveSource2.m_Entity, out var componentData3))
									{
										if (math.any(curveSource2.m_Offset != 0f))
										{
											curveElement.m_Curve = NetUtils.OffsetCurveLeftSmooth(componentData3.m_Bezier, curveSource2.m_Offset);
										}
										else
										{
											curveElement.m_Curve = componentData3.m_Bezier;
										}
									}
									float2 range = curveSource2.m_Range;
									for (int m = 0; m <= list.Length; m++)
									{
										if (flag2)
										{
											if (m < list.Length)
											{
												range.y = list[m];
												if (range.y <= range.x || range.y >= curveSource2.m_Range.y)
												{
													continue;
												}
											}
											else
											{
												range.y = curveSource2.m_Range.y;
											}
										}
										else if (m < list.Length)
										{
											range.y = list[list.Length - m - 1];
											if (range.y >= range.x || range.y <= curveSource2.m_Range.y)
											{
												continue;
											}
										}
										else
										{
											range.y = curveSource2.m_Range.y;
										}
										Bezier4x3 curve2 = MathUtils.Cut(curveElement.m_Curve, range);
										float num4 = MathUtils.Length(curve2);
										float2 offset2 = new float2(num, num + num4);
										float2 opacity = math.select(float7, 1f, range != curveSource2.m_Range);
										int value3 = AddSegment(ref reference.m_SegmentData, curveMap, curve2, curveSource2.m_Entity, range, offset2, opacity, broken);
										num = offset2.y;
										range.x = range.y;
										nativeList.Add(in value3);
									}
									list.Clear();
									continue;
								}
							}
						}
						float num5 = MathUtils.Length(curveElement.m_Curve);
						float2 offset3 = new float2(num, num + num5);
						int value4 = AddSegment(ref reference.m_SegmentData, curveMap, curveElement.m_Curve, curveSource2.m_Entity, curveSource2.m_Range, offset3, float7, broken);
						num = offset3.y;
						if (nativeList.IsCreated)
						{
							nativeList.Add(in value4);
						}
					}
				}
				if (nativeList.IsCreated)
				{
					nativeList.Add(-1);
				}
			}
			if (nativeList.IsCreated && nativeList.Length > 0)
			{
				bool flag3 = true;
				int num6 = 0;
				while (flag3 && num6 < 10)
				{
					int num7 = nativeList[0];
					flag3 = false;
					if (num6 == 0)
					{
						for (int n = 1; n < nativeList.Length; n++)
						{
							int num8 = nativeList[n];
							if (num7 != -1 && num8 != -1)
							{
								ref SegmentData reference2 = ref reference.m_SegmentData.ElementAt(num7);
								ref SegmentData reference3 = ref reference.m_SegmentData.ElementAt(num8);
								float num9 = math.max(reference2.m_SizeFactor.z, reference3.m_SizeFactor.x);
								flag3 |= num9 != reference2.m_SizeFactor.z || num9 != reference3.m_SizeFactor.x;
								reference2.m_SizeFactor.z = num9;
								reference3.m_SizeFactor.x = num9;
								float y3 = math.select(reference2.m_Opacity.y / reference3.m_Opacity.x, 1f, reference3.m_Opacity.x < 0.5f || reference2.m_Opacity.y > reference3.m_Opacity.x);
								float y4 = math.select(reference3.m_Opacity.x / reference2.m_Opacity.y, 1f, reference2.m_Opacity.y < 0.5f || reference3.m_Opacity.x > reference2.m_Opacity.y);
								reference2.m_DividedOpacity.y = math.min(reference2.m_DividedOpacity.y, y3);
								reference3.m_DividedOpacity.x = math.min(reference3.m_DividedOpacity.x, y4);
							}
							num7 = num8;
						}
						for (int num10 = 0; num10 < reference.m_SegmentData.Length; num10++)
						{
							ref SegmentData reference4 = ref reference.m_SegmentData.ElementAt(num10);
							reference4.m_Opacity = math.saturate(reference4.m_Opacity);
						}
					}
					else
					{
						for (int num11 = 1; num11 < nativeList.Length; num11++)
						{
							int num12 = nativeList[num11];
							if (num7 != -1 && num12 != -1)
							{
								ref SegmentData reference5 = ref reference.m_SegmentData.ElementAt(num7);
								ref SegmentData reference6 = ref reference.m_SegmentData.ElementAt(num12);
								float num13 = math.max(reference5.m_SizeFactor.z, reference6.m_SizeFactor.x);
								flag3 |= num13 != reference5.m_SizeFactor.z || num13 != reference6.m_SizeFactor.x;
								reference5.m_SizeFactor.z = num13;
								reference6.m_SizeFactor.x = num13;
							}
							num7 = num12;
						}
					}
					num6++;
				}
				int num14 = 0;
				for (int num15 = 0; num15 < nativeList.Length; num15++)
				{
					int num16 = nativeList[num15];
					if (num16 == -1)
					{
						float num17 = 0f;
						for (int num18 = num15 - 1; num18 >= num14; num18--)
						{
							num16 = nativeList[num18];
							ref SegmentData reference7 = ref reference.m_SegmentData.ElementAt(num16);
							float num19 = math.min(0f, num17 - reference7.m_Curve.c3.w);
							num17 -= reference7.m_Curve.c3.w - reference7.m_Curve.c0.w;
							reference7.m_Curve.c0.w += num19;
							reference7.m_Curve.c1.w += num19;
							reference7.m_Curve.c2.w += num19;
							reference7.m_Curve.c3.w += num19;
						}
						num14 = num15 + 1;
					}
				}
			}
			for (int num20 = 0; num20 < dynamicBuffer.Length; num20++)
			{
				Entity waypoint = dynamicBuffer[num20].m_Waypoint;
				float3 position = m_PositionData[waypoint].m_Position;
				AddNode(ref reference.m_SegmentData, position, 1f);
				bounds |= position;
			}
			if (curveMap.IsCreated)
			{
				curveMap.Dispose();
			}
			if (nativeParallelMultiHashMap.IsCreated)
			{
				nativeParallelMultiHashMap.Dispose();
			}
			if (nativeList.IsCreated)
			{
				nativeList.Dispose();
			}
			if (list.IsCreated)
			{
				list.Dispose();
			}
			reference.m_Bounds = bounds;
			reference.m_Length = num;
		}

		private int AddSegment(ref UnsafeList<SegmentData> segments, NativeHashMap<CurveKey, CurveValue> curveMap, Bezier4x3 curve, Entity sourceEntity, float2 sourceRange, float2 offset, float2 opacity, float broken)
		{
			float3 @float = math.lerp(curve.a, curve.d, 0.5f);
			SegmentData value = default(SegmentData);
			value.m_Curve = new float4x4
			{
				c0 = new float4(curve.a - @float, offset.x),
				c1 = new float4(curve.b - @float, offset.x + math.distance(curve.a, curve.b)),
				c2 = new float4(curve.c - @float, offset.y - math.distance(curve.c, curve.d)),
				c3 = new float4(curve.d - @float, offset.y)
			};
			value.m_Position = @float;
			value.m_SizeFactor = new float3(1f, 1f, 1f);
			value.m_Opacity = opacity;
			value.m_DividedOpacity = new float2(1f, 1f);
			value.m_Broken = broken;
			int result = -1;
			if (curveMap.IsCreated)
			{
				CurveKey key = new CurveKey
				{
					m_Line = new Line3.Segment(curve.a, curve.d),
					m_Entity = sourceEntity,
					m_Range = sourceRange
				};
				if (curveMap.TryGetValue(key, out var item))
				{
					ref SegmentData reference = ref segments.ElementAt(item.m_SegmentDataIndex);
					item.m_SharedCount++;
					reference.m_SizeFactor = GetSizeFactor(item.m_SharedCount);
					reference.m_Opacity += opacity;
					curveMap[key] = item;
					return item.m_SegmentDataIndex;
				}
				item.m_SharedCount = 1;
				item.m_SegmentDataIndex = segments.Length;
				curveMap.Add(key, item);
				result = item.m_SegmentDataIndex;
			}
			segments.Add(in value);
			return result;
		}

		private float GetSizeFactor(int sharedCount)
		{
			return 4f - 21f / (6f + (float)sharedCount);
		}

		private void AddNode(ref UnsafeList<SegmentData> segments, float3 position, float opacity)
		{
			SegmentData value = default(SegmentData);
			value.m_Curve = new float4x4
			{
				c0 = new float4(0f, 0f, -1f, -1000000f),
				c1 = new float4(0f, 0f, -1f / 3f, -1000000f),
				c2 = new float4(0f, 0f, 1f / 3f, -1000000f),
				c3 = new float4(0f, 0f, 1f, -1000000f)
			};
			value.m_Position = position;
			value.m_SizeFactor = new float3(1f, 1f, 1f);
			value.m_Opacity = new float2(opacity, opacity);
			value.m_DividedOpacity = new float2(1f, 1f);
			value.m_Broken = 0f;
			segments.Add(in value);
		}
	}

	private struct TypeHandle
	{
		[ReadOnly]
		public EntityTypeHandle __Unity_Entities_Entity_TypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Deleted> __Game_Common_Deleted_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Created> __Game_Common_Created_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Applied> __Game_Common_Applied_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<PathUpdated> __Game_Pathfind_PathUpdated_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentTypeHandle;

		public ComponentTypeHandle<RouteBufferIndex> __Game_Rendering_RouteBufferIndex_RW_ComponentTypeHandle;

		[ReadOnly]
		public ComponentLookup<Game.Routes.Segment> __Game_Routes_Segment_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Owner> __Game_Common_Owner_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Deleted> __Game_Common_Deleted_RO_ComponentLookup;

		[ReadOnly]
		public EntityStorageInfoLookup __EntityStorageInfoLookup;

		public SharedComponentTypeHandle<UpdateFrame> __Game_Simulation_UpdateFrame_SharedComponentTypeHandle;

		[ReadOnly]
		public ComponentLookup<Position> __Game_Routes_Position_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PathSource> __Game_Routes_PathSource_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<LivePath> __Game_Routes_LivePath_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Curve> __Game_Net_Curve_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Objects.Transform> __Game_Objects_Transform_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Unspawned> __Game_Objects_Unspawned_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<CurrentVehicle> __Game_Creatures_CurrentVehicle_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<RouteWaypoint> __Game_Routes_RouteWaypoint_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<RouteSegment> __Game_Routes_RouteSegment_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<CurveElement> __Game_Routes_CurveElement_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<CurveSource> __Game_Routes_CurveSource_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<TransformFrame> __Game_Objects_TransformFrame_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<PathElement> __Game_Pathfind_PathElement_RO_BufferLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
			__Game_Common_Deleted_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Deleted>(isReadOnly: true);
			__Game_Common_Created_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Created>(isReadOnly: true);
			__Game_Common_Applied_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Applied>(isReadOnly: true);
			__Game_Pathfind_PathUpdated_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PathUpdated>(isReadOnly: true);
			__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PrefabRef>(isReadOnly: true);
			__Game_Rendering_RouteBufferIndex_RW_ComponentTypeHandle = state.GetComponentTypeHandle<RouteBufferIndex>();
			__Game_Routes_Segment_RO_ComponentLookup = state.GetComponentLookup<Game.Routes.Segment>(isReadOnly: true);
			__Game_Common_Owner_RO_ComponentLookup = state.GetComponentLookup<Owner>(isReadOnly: true);
			__Game_Common_Deleted_RO_ComponentLookup = state.GetComponentLookup<Deleted>(isReadOnly: true);
			__EntityStorageInfoLookup = state.GetEntityStorageInfoLookup();
			__Game_Simulation_UpdateFrame_SharedComponentTypeHandle = state.GetSharedComponentTypeHandle<UpdateFrame>();
			__Game_Routes_Position_RO_ComponentLookup = state.GetComponentLookup<Position>(isReadOnly: true);
			__Game_Routes_PathSource_RO_ComponentLookup = state.GetComponentLookup<PathSource>(isReadOnly: true);
			__Game_Routes_LivePath_RO_ComponentLookup = state.GetComponentLookup<LivePath>(isReadOnly: true);
			__Game_Net_Curve_RO_ComponentLookup = state.GetComponentLookup<Curve>(isReadOnly: true);
			__Game_Objects_Transform_RO_ComponentLookup = state.GetComponentLookup<Game.Objects.Transform>(isReadOnly: true);
			__Game_Objects_Unspawned_RO_ComponentLookup = state.GetComponentLookup<Unspawned>(isReadOnly: true);
			__Game_Creatures_CurrentVehicle_RO_ComponentLookup = state.GetComponentLookup<CurrentVehicle>(isReadOnly: true);
			__Game_Routes_RouteWaypoint_RO_BufferLookup = state.GetBufferLookup<RouteWaypoint>(isReadOnly: true);
			__Game_Routes_RouteSegment_RO_BufferLookup = state.GetBufferLookup<RouteSegment>(isReadOnly: true);
			__Game_Routes_CurveElement_RO_BufferLookup = state.GetBufferLookup<CurveElement>(isReadOnly: true);
			__Game_Routes_CurveSource_RO_BufferLookup = state.GetBufferLookup<CurveSource>(isReadOnly: true);
			__Game_Objects_TransformFrame_RO_BufferLookup = state.GetBufferLookup<TransformFrame>(isReadOnly: true);
			__Game_Pathfind_PathElement_RO_BufferLookup = state.GetBufferLookup<PathElement>(isReadOnly: true);
		}
	}

	private RenderingSystem m_RenderingSystem;

	private PrefabSystem m_PrefabSystem;

	private EntityQuery m_UpdatedRoutesQuery;

	private EntityQuery m_AllRoutesQuery;

	private EntityQuery m_RouteConfigQuery;

	private List<ManagedData> m_ManagedData;

	private NativeList<NativeData> m_NativeData;

	private Stack<int> m_FreeBufferIndices;

	private JobHandle m_BufferDependencies;

	private bool m_Loaded;

	private TypeHandle __TypeHandle;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_RenderingSystem = base.World.GetOrCreateSystemManaged<RenderingSystem>();
		m_PrefabSystem = base.World.GetOrCreateSystemManaged<PrefabSystem>();
		m_UpdatedRoutesQuery = GetEntityQuery(new EntityQueryDesc
		{
			All = new ComponentType[1] { ComponentType.ReadOnly<Route>() },
			Any = new ComponentType[3]
			{
				ComponentType.ReadOnly<Updated>(),
				ComponentType.ReadOnly<LivePath>(),
				ComponentType.ReadOnly<Deleted>()
			}
		}, new EntityQueryDesc
		{
			All = new ComponentType[2]
			{
				ComponentType.ReadOnly<Game.Common.Event>(),
				ComponentType.ReadOnly<PathUpdated>()
			}
		});
		m_AllRoutesQuery = GetEntityQuery(ComponentType.ReadOnly<Route>());
		m_RouteConfigQuery = GetEntityQuery(ComponentType.ReadOnly<RouteConfigurationData>());
	}

	[Preserve]
	protected override void OnDestroy()
	{
		Clear();
		if (m_NativeData.IsCreated)
		{
			m_NativeData.Dispose();
		}
		base.OnDestroy();
	}

	public void PreDeserialize(Context context)
	{
		Clear();
		m_Loaded = true;
	}

	private void Clear()
	{
		if (m_ManagedData != null)
		{
			for (int i = 0; i < m_ManagedData.Count; i++)
			{
				m_ManagedData[i].Dispose();
			}
			m_ManagedData.Clear();
		}
		if (m_FreeBufferIndices != null)
		{
			m_FreeBufferIndices.Clear();
		}
		if (m_NativeData.IsCreated)
		{
			m_BufferDependencies.Complete();
			for (int j = 0; j < m_NativeData.Length; j++)
			{
				m_NativeData.ElementAt(j).Dispose();
			}
			m_NativeData.Clear();
		}
	}

	public unsafe void GetBuffer(int index, out Material material, out ComputeBuffer segmentBuffer, out int originalRenderQueue, out Bounds bounds, out Vector4 size)
	{
		material = null;
		segmentBuffer = null;
		originalRenderQueue = 0;
		bounds = default(Bounds);
		size = default(Vector4);
		if (m_ManagedData == null || index < 0 || index >= m_ManagedData.Count)
		{
			return;
		}
		m_BufferDependencies.Complete();
		ManagedData managedData = m_ManagedData[index];
		ref NativeData reference = ref m_NativeData.ElementAt(index);
		if (managedData.m_Updated)
		{
			managedData.m_Updated = false;
			if (managedData.m_SegmentBuffer != null && managedData.m_SegmentBuffer.count != reference.m_SegmentData.Length)
			{
				managedData.m_SegmentBuffer.Release();
				managedData.m_SegmentBuffer = null;
			}
			if (reference.m_SegmentData.Length > 0)
			{
				if (managedData.m_SegmentBuffer == null)
				{
					managedData.m_SegmentBuffer = new ComputeBuffer(reference.m_SegmentData.Length, sizeof(SegmentData));
					managedData.m_SegmentBuffer.name = "Route segment buffer (" + managedData.m_Material.name + ")";
				}
				NativeArray<SegmentData> data = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<SegmentData>(reference.m_SegmentData.Ptr, reference.m_SegmentData.Length, Allocator.None);
				managedData.m_SegmentBuffer.SetData(data);
			}
			reference.m_SegmentData.Dispose();
		}
		material = managedData.m_Material;
		segmentBuffer = managedData.m_SegmentBuffer;
		originalRenderQueue = managedData.m_OriginalRenderQueue;
		bounds = RenderingUtils.ToBounds(reference.m_Bounds);
		size = managedData.m_Size;
	}

	private bool GetLoaded()
	{
		if (m_Loaded)
		{
			m_Loaded = false;
			return true;
		}
		return false;
	}

	[Preserve]
	protected override void OnUpdate()
	{
		bool loaded = GetLoaded();
		EntityQuery entityQuery = (loaded ? m_AllRoutesQuery : m_UpdatedRoutesQuery);
		if (entityQuery.IsEmptyIgnoreFilter)
		{
			return;
		}
		RoutePrefab routePrefab = null;
		HashSet<Entity> hashSet = null;
		m_BufferDependencies.Complete();
		NativeArray<ArchetypeChunk> nativeArray = entityQuery.ToArchetypeChunkArray(Allocator.TempJob);
		try
		{
			EntityTypeHandle entityTypeHandle = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef);
			ComponentTypeHandle<Deleted> typeHandle = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Common_Deleted_RO_ComponentTypeHandle, ref base.CheckedStateRef);
			ComponentTypeHandle<Created> typeHandle2 = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Common_Created_RO_ComponentTypeHandle, ref base.CheckedStateRef);
			ComponentTypeHandle<Applied> typeHandle3 = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Common_Applied_RO_ComponentTypeHandle, ref base.CheckedStateRef);
			ComponentTypeHandle<PathUpdated> typeHandle4 = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Pathfind_PathUpdated_RO_ComponentTypeHandle, ref base.CheckedStateRef);
			ComponentTypeHandle<PrefabRef> typeHandle5 = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle, ref base.CheckedStateRef);
			ComponentTypeHandle<RouteBufferIndex> typeHandle6 = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Rendering_RouteBufferIndex_RW_ComponentTypeHandle, ref base.CheckedStateRef);
			ComponentLookup<Game.Routes.Segment> componentLookup = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Routes_Segment_RO_ComponentLookup, ref base.CheckedStateRef);
			ComponentLookup<Owner> componentLookup2 = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Common_Owner_RO_ComponentLookup, ref base.CheckedStateRef);
			ComponentLookup<Deleted> componentLookup3 = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Common_Deleted_RO_ComponentLookup, ref base.CheckedStateRef);
			for (int i = 0; i < nativeArray.Length; i++)
			{
				ArchetypeChunk archetypeChunk = nativeArray[i];
				NativeArray<PathUpdated> nativeArray2 = archetypeChunk.GetNativeArray(ref typeHandle4);
				if (nativeArray2.Length != 0)
				{
					for (int j = 0; j < nativeArray2.Length; j++)
					{
						PathUpdated pathUpdated = nativeArray2[j];
						if (componentLookup.HasComponent(pathUpdated.m_Owner) && componentLookup2.HasComponent(pathUpdated.m_Owner) && !componentLookup3.HasComponent(pathUpdated.m_Owner))
						{
							if (hashSet == null)
							{
								hashSet = new HashSet<Entity>();
							}
							hashSet.Add(componentLookup2[pathUpdated.m_Owner].m_Owner);
						}
					}
				}
				else if (archetypeChunk.Has(ref typeHandle))
				{
					NativeArray<RouteBufferIndex> nativeArray3 = archetypeChunk.GetNativeArray(ref typeHandle6);
					if (m_FreeBufferIndices == null)
					{
						m_FreeBufferIndices = new Stack<int>(nativeArray3.Length);
					}
					for (int k = 0; k < nativeArray3.Length; k++)
					{
						RouteBufferIndex value = nativeArray3[k];
						ManagedData managedData = m_ManagedData[value.m_Index];
						ref NativeData reference = ref m_NativeData.ElementAt(value.m_Index);
						managedData.m_Updated = false;
						reference.m_Updated = false;
						m_FreeBufferIndices.Push(value.m_Index);
						value.m_Index = -1;
						nativeArray3[k] = value;
					}
				}
				else if (loaded || (archetypeChunk.Has(ref typeHandle2) && !archetypeChunk.Has(ref typeHandle3)))
				{
					NativeArray<Entity> nativeArray4 = archetypeChunk.GetNativeArray(entityTypeHandle);
					NativeArray<RouteBufferIndex> nativeArray5 = archetypeChunk.GetNativeArray(ref typeHandle6);
					NativeArray<PrefabRef> nativeArray6 = archetypeChunk.GetNativeArray(ref typeHandle5);
					if (m_ManagedData == null)
					{
						m_ManagedData = new List<ManagedData>(nativeArray5.Length);
					}
					if (!m_NativeData.IsCreated)
					{
						m_NativeData = new NativeList<NativeData>(nativeArray5.Length, Allocator.Persistent);
					}
					if (hashSet == null)
					{
						hashSet = new HashSet<Entity>();
					}
					for (int l = 0; l < nativeArray5.Length; l++)
					{
						Entity entity = nativeArray4[l];
						RouteBufferIndex value2 = nativeArray5[l];
						PrefabRef refData = nativeArray6[l];
						if (!m_PrefabSystem.TryGetPrefab<RoutePrefab>(refData, out var prefab))
						{
							RouteConfigurationData singleton = m_RouteConfigQuery.GetSingleton<RouteConfigurationData>();
							if (routePrefab != null)
							{
								prefab = routePrefab;
							}
							else if (m_PrefabSystem.TryGetPrefab<RoutePrefab>(singleton.m_MissingRoutePrefab, out prefab))
							{
								routePrefab = prefab;
							}
						}
						if (m_FreeBufferIndices != null && m_FreeBufferIndices.Count > 0)
						{
							value2.m_Index = m_FreeBufferIndices.Pop();
							ManagedData managedData2 = m_ManagedData[value2.m_Index];
							ref NativeData reference2 = ref m_NativeData.ElementAt(value2.m_Index);
							managedData2.Initialize(prefab);
							reference2.Initialize(entity);
						}
						else
						{
							value2.m_Index = m_ManagedData.Count;
							ManagedData managedData3 = new ManagedData();
							NativeData value3 = default(NativeData);
							managedData3.Initialize(prefab);
							value3.Initialize(entity);
							m_ManagedData.Add(managedData3);
							m_NativeData.Add(in value3);
						}
						nativeArray5[l] = value2;
						hashSet.Add(entity);
					}
				}
				else
				{
					NativeArray<Entity> nativeArray7 = archetypeChunk.GetNativeArray(entityTypeHandle);
					if (hashSet == null)
					{
						hashSet = new HashSet<Entity>();
					}
					for (int m = 0; m < nativeArray7.Length; m++)
					{
						hashSet.Add(nativeArray7[m]);
					}
				}
			}
		}
		finally
		{
			nativeArray.Dispose();
		}
		if (hashSet == null)
		{
			return;
		}
		foreach (Entity item in hashSet)
		{
			RouteBufferIndex componentData = base.EntityManager.GetComponentData<RouteBufferIndex>(item);
			if (componentData.m_Index >= 0)
			{
				ManagedData managedData4 = m_ManagedData[componentData.m_Index];
				ref NativeData reference3 = ref m_NativeData.ElementAt(componentData.m_Index);
				managedData4.m_Updated = true;
				reference3.m_Updated = true;
				if (!reference3.m_SegmentData.IsCreated)
				{
					reference3.m_SegmentData = new UnsafeList<SegmentData>(0, Allocator.Persistent);
				}
			}
		}
		UpdateBufferJob jobData = new UpdateBufferJob
		{
			m_EntityLookup = InternalCompilerInterface.GetEntityStorageInfoLookup(ref __TypeHandle.__EntityStorageInfoLookup, ref base.CheckedStateRef),
			m_UpdateFrameType = InternalCompilerInterface.GetSharedComponentTypeHandle(ref __TypeHandle.__Game_Simulation_UpdateFrame_SharedComponentTypeHandle, ref base.CheckedStateRef),
			m_PositionData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Routes_Position_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PathSourceData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Routes_PathSource_RO_ComponentLookup, ref base.CheckedStateRef),
			m_LivePathData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Routes_LivePath_RO_ComponentLookup, ref base.CheckedStateRef),
			m_CurveData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Curve_RO_ComponentLookup, ref base.CheckedStateRef),
			m_TransformData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Transform_RO_ComponentLookup, ref base.CheckedStateRef),
			m_UnspawnedData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Unspawned_RO_ComponentLookup, ref base.CheckedStateRef),
			m_CurrentVehicleData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Creatures_CurrentVehicle_RO_ComponentLookup, ref base.CheckedStateRef),
			m_RouteWaypoints = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Routes_RouteWaypoint_RO_BufferLookup, ref base.CheckedStateRef),
			m_RouteSegments = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Routes_RouteSegment_RO_BufferLookup, ref base.CheckedStateRef),
			m_CurveElements = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Routes_CurveElement_RO_BufferLookup, ref base.CheckedStateRef),
			m_CurveSources = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Routes_CurveSource_RO_BufferLookup, ref base.CheckedStateRef),
			m_TransformFrames = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Objects_TransformFrame_RO_BufferLookup, ref base.CheckedStateRef),
			m_PathElements = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Pathfind_PathElement_RO_BufferLookup, ref base.CheckedStateRef),
			m_FrameIndex = m_RenderingSystem.frameIndex,
			m_FrameTime = m_RenderingSystem.frameTime,
			m_NativeData = m_NativeData
		};
		m_BufferDependencies = IJobParallelForExtensions.Schedule(jobData, m_NativeData.Length, 1, base.Dependency);
		base.Dependency = m_BufferDependencies;
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
	public RouteBufferSystem()
	{
	}
}
