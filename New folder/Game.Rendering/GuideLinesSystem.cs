using System.Runtime.CompilerServices;
using Colossal.Collections;
using Colossal.Entities;
using Colossal.Mathematics;
using Game.Areas;
using Game.Buildings;
using Game.Common;
using Game.Net;
using Game.Objects;
using Game.Prefabs;
using Game.Routes;
using Game.Simulation;
using Game.Tools;
using Game.Zones;
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
public class GuideLinesSystem : GameSystemBase
{
	public struct TooltipInfo
	{
		public TooltipType m_Type;

		public float3 m_Position;

		public float m_Value;

		public TooltipInfo(TooltipType type, float3 position, float value)
		{
			m_Type = type;
			m_Position = position;
			m_Value = value;
		}
	}

	public enum TooltipType
	{
		Angle,
		Length
	}

	[BurstCompile]
	private struct WaterToolGuideLinesJobLegacy : IJob
	{
		[ReadOnly]
		public ComponentTypeHandle<Game.Simulation.WaterSourceData> m_WaterSourceDataType;

		[ReadOnly]
		public ComponentTypeHandle<Game.Objects.Transform> m_TransformType;

		[ReadOnly]
		public ComponentTypeHandle<Temp> m_TempType;

		[ReadOnly]
		public ComponentLookup<GuideLineSettingsData> m_GuideLineSettingsData;

		[ReadOnly]
		public BufferLookup<WaterSourceColorElement> m_WaterSourceColors;

		[ReadOnly]
		public WaterToolSystem.Attribute m_Attribute;

		[ReadOnly]
		public float3 m_PositionOffset;

		[ReadOnly]
		public float3 m_CameraRight;

		[ReadOnly]
		public TerrainHeightData m_TerrainHeightData;

		[ReadOnly]
		public NativeList<ArchetypeChunk> m_WaterSourceChunks;

		[ReadOnly]
		public Entity m_GuideLineSettingsEntity;

		public OverlayRenderSystem.Buffer m_OverlayBuffer;

		public void Execute()
		{
			GuideLineSettingsData guideLineSettingsData = m_GuideLineSettingsData[m_GuideLineSettingsEntity];
			DynamicBuffer<WaterSourceColorElement> dynamicBuffer = m_WaterSourceColors[m_GuideLineSettingsEntity];
			for (int i = 0; i < m_WaterSourceChunks.Length; i++)
			{
				ArchetypeChunk archetypeChunk = m_WaterSourceChunks[i];
				NativeArray<Game.Simulation.WaterSourceData> nativeArray = archetypeChunk.GetNativeArray(ref m_WaterSourceDataType);
				NativeArray<Game.Objects.Transform> nativeArray2 = archetypeChunk.GetNativeArray(ref m_TransformType);
				bool flag = archetypeChunk.Has(ref m_TempType);
				for (int j = 0; j < nativeArray.Length; j++)
				{
					Game.Simulation.WaterSourceData waterSourceData = nativeArray[j];
					Game.Objects.Transform transform = nativeArray2[j];
					int valueToClamp = waterSourceData.m_ConstantDepth + 3;
					WaterSourceColorElement waterSourceColorElement = dynamicBuffer[math.clamp(valueToClamp, 0, dynamicBuffer.Length - 1)];
					float3 @float = math.forward(transform.m_Rotation);
					float num = math.max(1f, waterSourceData.m_Radius);
					float num2 = num * 0.02f;
					float3 position = transform.m_Position;
					if (waterSourceData.m_ConstantDepth > 0)
					{
						position.y = m_PositionOffset.y + waterSourceData.m_Height;
					}
					else
					{
						position.y = TerrainUtils.SampleHeight(ref m_TerrainHeightData, position) + waterSourceData.m_Height;
					}
					if (flag)
					{
						waterSourceColorElement.m_Fill.a = waterSourceColorElement.m_Fill.a * 0.5f + 0.5f;
						waterSourceColorElement.m_Outline.a = waterSourceColorElement.m_Outline.a * 0.5f + 0.5f;
					}
					m_OverlayBuffer.DrawCircle(waterSourceColorElement.m_Outline, waterSourceColorElement.m_Fill, num2, (OverlayRenderSystem.StyleFlags)0, @float.xz, position, num * 2f);
					m_OverlayBuffer.DrawCircle(waterSourceColorElement.m_ProjectedOutline, waterSourceColorElement.m_ProjectedFill, num2, OverlayRenderSystem.StyleFlags.Projected, @float.xz, position, num * 2f);
					if (!flag)
					{
						continue;
					}
					switch (m_Attribute)
					{
					case WaterToolSystem.Attribute.Location:
					{
						float2 value2 = m_CameraRight.xz;
						if (MathUtils.TryNormalize(ref value2))
						{
							Line3.Segment line = new Line3.Segment(position, position);
							line.a.xz -= value2 * (num * 0.5f);
							line.b.xz += value2 * (num * 0.5f);
							float dashLength = num * 0.5f - num2;
							m_OverlayBuffer.DrawDashedLine(guideLineSettingsData.m_HighPriorityColor, guideLineSettingsData.m_HighPriorityColor, 0f, (OverlayRenderSystem.StyleFlags)0, line, num2, dashLength, num2);
							float2 float4 = MathUtils.Left(value2);
							line = new Line3.Segment(position, position);
							line.a.xz -= float4 * (num * 0.5f);
							line.b.xz += float4 * (num * 0.5f);
							m_OverlayBuffer.DrawDashedLine(guideLineSettingsData.m_HighPriorityColor, guideLineSettingsData.m_HighPriorityColor, 0f, (OverlayRenderSystem.StyleFlags)0, line, num2, dashLength, num2);
						}
						break;
					}
					case WaterToolSystem.Attribute.Radius:
					{
						float2 value3 = m_CameraRight.xz;
						if (MathUtils.TryNormalize(ref value3))
						{
							float2 float5 = MathUtils.Left(value3);
							float2 float6 = (value3 + float5) * 0.70710677f;
							float3 startPos2 = position;
							float3 startTangent2 = default(float3);
							float3 endPos2 = position;
							float3 endTangent2 = default(float3);
							startPos2.xz += float6 * (num - num2 * 0.5f);
							startTangent2.xz = MathUtils.Right(float6);
							endPos2.xz += MathUtils.Right(float6) * (num - num2 * 0.5f);
							endTangent2.xz = -float6;
							Bezier4x3 curve2 = NetUtils.FitCurve(startPos2, startTangent2, endTangent2, endPos2);
							m_OverlayBuffer.DrawCurve(guideLineSettingsData.m_HighPriorityColor, guideLineSettingsData.m_HighPriorityColor, 0f, (OverlayRenderSystem.StyleFlags)0, curve2, num2);
							curve2.a.xz = 2f * position.xz - curve2.a.xz;
							curve2.b.xz = 2f * position.xz - curve2.b.xz;
							curve2.c.xz = 2f * position.xz - curve2.c.xz;
							curve2.d.xz = 2f * position.xz - curve2.d.xz;
							m_OverlayBuffer.DrawCurve(guideLineSettingsData.m_HighPriorityColor, guideLineSettingsData.m_HighPriorityColor, 0f, (OverlayRenderSystem.StyleFlags)0, curve2, num2);
						}
						break;
					}
					case WaterToolSystem.Attribute.Rate:
					case WaterToolSystem.Attribute.Height:
					{
						float2 value = m_CameraRight.xz;
						if (MathUtils.TryNormalize(ref value))
						{
							float2 float2 = MathUtils.Left(value);
							float2 float3 = (value + float2) * 0.70710677f;
							float3 startPos = position;
							float3 startTangent = default(float3);
							float3 endPos = position;
							float3 endTangent = default(float3);
							startPos.xz += float3 * (num - num2 * 0.5f);
							startTangent.xz = MathUtils.Left(float3);
							endPos.xz += MathUtils.Left(float3) * (num - num2 * 0.5f);
							endTangent.xz = -float3;
							Bezier4x3 curve = NetUtils.FitCurve(startPos, startTangent, endTangent, endPos);
							m_OverlayBuffer.DrawCurve(guideLineSettingsData.m_HighPriorityColor, guideLineSettingsData.m_HighPriorityColor, 0f, (OverlayRenderSystem.StyleFlags)0, curve, num2);
							curve.a.xz = 2f * position.xz - curve.a.xz;
							curve.b.xz = 2f * position.xz - curve.b.xz;
							curve.c.xz = 2f * position.xz - curve.c.xz;
							curve.d.xz = 2f * position.xz - curve.d.xz;
							m_OverlayBuffer.DrawCurve(guideLineSettingsData.m_HighPriorityColor, guideLineSettingsData.m_HighPriorityColor, 0f, (OverlayRenderSystem.StyleFlags)0, curve, num2);
						}
						break;
					}
					}
				}
			}
		}
	}

	[BurstCompile]
	private struct WaterToolGuideLinesJob : IJob
	{
		[ReadOnly]
		public ComponentTypeHandle<Game.Simulation.WaterSourceData> m_WaterSourceDataType;

		[ReadOnly]
		public ComponentTypeHandle<Game.Objects.Transform> m_TransformType;

		[ReadOnly]
		public ComponentTypeHandle<Temp> m_TempType;

		[ReadOnly]
		public ComponentLookup<GuideLineSettingsData> m_GuideLineSettingsData;

		[ReadOnly]
		public BufferLookup<WaterSourceColorElement> m_WaterSourceColors;

		[ReadOnly]
		public WaterToolSystem.Attribute m_Attribute;

		[ReadOnly]
		public float3 m_PositionOffset;

		[ReadOnly]
		public float3 m_CameraRight;

		[ReadOnly]
		public Bounds3 m_WorldBounds;

		[ReadOnly]
		public TerrainHeightData m_TerrainHeightData;

		[ReadOnly]
		public NativeList<ArchetypeChunk> m_WaterSourceChunks;

		[ReadOnly]
		public Entity m_GuideLineSettingsEntity;

		[ReadOnly]
		public bool m_showNames;

		public OverlayRenderSystem.Buffer m_OverlayBuffer;

		public void Execute()
		{
			GuideLineSettingsData guideLineSettingsData = m_GuideLineSettingsData[m_GuideLineSettingsEntity];
			DynamicBuffer<WaterSourceColorElement> dynamicBuffer = m_WaterSourceColors[m_GuideLineSettingsEntity];
			for (int i = 0; i < m_WaterSourceChunks.Length; i++)
			{
				ArchetypeChunk archetypeChunk = m_WaterSourceChunks[i];
				NativeArray<Game.Simulation.WaterSourceData> nativeArray = archetypeChunk.GetNativeArray(ref m_WaterSourceDataType);
				NativeArray<Game.Objects.Transform> nativeArray2 = archetypeChunk.GetNativeArray(ref m_TransformType);
				bool flag = archetypeChunk.Has(ref m_TempType);
				Bounds3 bounds = TerrainUtils.GetBounds(ref m_TerrainHeightData);
				for (int j = 0; j < nativeArray.Length; j++)
				{
					Game.Simulation.WaterSourceData waterSourceData = nativeArray[j];
					Game.Objects.Transform transform = nativeArray2[j];
					WaterSourceColorElement waterSourceColorElement = dynamicBuffer[0];
					float3 @float = math.forward(transform.m_Rotation);
					float num = math.max(1f, waterSourceData.m_Radius);
					float num2 = num * 0.02f;
					float3 position = transform.m_Position;
					position.y = TerrainUtils.SampleHeight(ref m_TerrainHeightData, position) + waterSourceData.m_Height;
					bool flag2 = MathUtils.Intersect(bounds.xz, position.xz);
					if (((0u | ((!MathUtils.Intersect(bounds.xz, position.xz + new float2(num, 0f))) ? 1u : 0u) | ((!MathUtils.Intersect(bounds.xz, position.xz + new float2(0f, num))) ? 1u : 0u) | ((!MathUtils.Intersect(bounds.xz, position.xz + new float2(0f - num, 0f))) ? 1u : 0u) | ((!MathUtils.Intersect(bounds.xz, position.xz + new float2(0f, 0f - num))) ? 1u : 0u)) & (flag2 ? 1u : 0u)) != 0)
					{
						waterSourceColorElement = dynamicBuffer[2];
					}
					else if (!flag2)
					{
						waterSourceColorElement = dynamicBuffer[1];
					}
					if (flag)
					{
						waterSourceColorElement.m_Fill.a = waterSourceColorElement.m_Fill.a * 0.5f + 0.5f;
						waterSourceColorElement.m_Outline.a = waterSourceColorElement.m_Outline.a * 0.5f + 0.5f;
					}
					float3 position2 = position;
					position2.y -= waterSourceData.m_Height * 0.5f;
					m_OverlayBuffer.DrawCustomMesh(waterSourceColorElement.m_Fill, position2, waterSourceData.m_Height, num, OverlayRenderSystem.CustomMeshType.Cylinder);
					m_OverlayBuffer.DrawCircle(waterSourceColorElement.m_Outline, waterSourceColorElement.m_Fill, num2, OverlayRenderSystem.StyleFlags.DepthFadeBelow, @float.xz, position, num * 2f);
					m_OverlayBuffer.DrawCircle(waterSourceColorElement.m_ProjectedOutline, waterSourceColorElement.m_ProjectedFill, num2, OverlayRenderSystem.StyleFlags.Projected, @float.xz, position, num * 2f);
					if (m_showNames)
					{
						m_OverlayBuffer.DrawText(waterSourceData.SourceNameId, position + new float3(0f, 20f, 0f), cameraFace: true);
					}
					float2 value = m_CameraRight.xz;
					if (!flag)
					{
						continue;
					}
					switch (m_Attribute)
					{
					case WaterToolSystem.Attribute.Location:
						if (MathUtils.TryNormalize(ref value))
						{
							Line3.Segment line = new Line3.Segment(position, position);
							line.a.xz -= value * (num * 0.5f);
							line.b.xz += value * (num * 0.5f);
							float dashLength = num * 0.5f - num2;
							m_OverlayBuffer.DrawDashedLine(guideLineSettingsData.m_HighPriorityColor, guideLineSettingsData.m_HighPriorityColor, 0f, (OverlayRenderSystem.StyleFlags)0, line, num2, dashLength, num2);
							float2 float5 = MathUtils.Left(value);
							line = new Line3.Segment(position, position);
							line.a.xz -= float5 * (num * 0.5f);
							line.b.xz += float5 * (num * 0.5f);
							m_OverlayBuffer.DrawDashedLine(guideLineSettingsData.m_HighPriorityColor, guideLineSettingsData.m_HighPriorityColor, 0f, (OverlayRenderSystem.StyleFlags)0, line, num2, dashLength, num2);
						}
						break;
					case WaterToolSystem.Attribute.Radius:
						if (MathUtils.TryNormalize(ref value))
						{
							float2 float6 = MathUtils.Left(value);
							float2 float7 = (value + float6) * 0.70710677f;
							float3 startPos = position;
							float3 startTangent = default(float3);
							float3 endPos = position;
							float3 endTangent = default(float3);
							startPos.xz += float7 * (num - num2 * 0.5f);
							startTangent.xz = MathUtils.Right(float7);
							endPos.xz += MathUtils.Right(float7) * (num - num2 * 0.5f);
							endTangent.xz = -float7;
							Bezier4x3 curve = NetUtils.FitCurve(startPos, startTangent, endTangent, endPos);
							m_OverlayBuffer.DrawCurve(guideLineSettingsData.m_HighPriorityColor, guideLineSettingsData.m_HighPriorityColor, 0f, (OverlayRenderSystem.StyleFlags)0, curve, num2);
							curve.a.xz = 2f * position.xz - curve.a.xz;
							curve.b.xz = 2f * position.xz - curve.b.xz;
							curve.c.xz = 2f * position.xz - curve.c.xz;
							curve.d.xz = 2f * position.xz - curve.d.xz;
							m_OverlayBuffer.DrawCurve(guideLineSettingsData.m_HighPriorityColor, guideLineSettingsData.m_HighPriorityColor, 0f, (OverlayRenderSystem.StyleFlags)0, curve, num2);
						}
						break;
					case WaterToolSystem.Attribute.Height:
						if (MathUtils.TryNormalize(ref value))
						{
							float2 float2 = MathUtils.Left(value);
							float2 float3 = position.xz + float2 * num;
							float3 position3 = new float3(float3.x, position.y, float3.y);
							Quaternion rot = Quaternion.LookRotation(new Vector3(float2.x, 0f, float2.y), Vector3.up);
							float num3 = math.lerp(4f, 40f, math.saturate((num - 30f) / 2000f));
							m_OverlayBuffer.DrawCustomMesh(guideLineSettingsData.m_HighPriorityColor, position3, num3, num3, OverlayRenderSystem.CustomMeshType.Arrow, rot);
							m_OverlayBuffer.DrawCustomMesh(guideLineSettingsData.m_HighPriorityColor, position3, 0f - num3, num3, OverlayRenderSystem.CustomMeshType.Arrow, rot);
							float3 = position.xz - float2 * num;
							position3 = new float3(float3.x, position.y, float3.y);
							m_OverlayBuffer.DrawCustomMesh(guideLineSettingsData.m_HighPriorityColor, position3, num3, num3, OverlayRenderSystem.CustomMeshType.Arrow, rot);
							m_OverlayBuffer.DrawCustomMesh(guideLineSettingsData.m_HighPriorityColor, position3, 0f - num3, num3, OverlayRenderSystem.CustomMeshType.Arrow, rot);
							float3 float4 = MathUtils.Size(m_WorldBounds);
							float3 position4 = new float3(0f, position3.y, 0f);
							m_OverlayBuffer.DrawCustomMesh(guideLineSettingsData.m_LowPriorityColor, position4, float4.x, float4.z, OverlayRenderSystem.CustomMeshType.Plane);
						}
						break;
					}
				}
			}
		}
	}

	[BurstCompile]
	private struct ObjectToolGuideLinesJob : IJob
	{
		[ReadOnly]
		public ComponentTypeHandle<CreationDefinition> m_CreationDefinitionType;

		[ReadOnly]
		public ComponentTypeHandle<ObjectDefinition> m_ObjectDefinitionType;

		[ReadOnly]
		public ComponentTypeHandle<OwnerDefinition> m_OwnerDefinitionType;

		[ReadOnly]
		public ComponentTypeHandle<NetCourse> m_NetCourseType;

		[ReadOnly]
		public ComponentLookup<Game.Objects.Transform> m_TransformData;

		[ReadOnly]
		public ComponentLookup<ObjectGeometryData> m_PrefabObjectGeometryData;

		[ReadOnly]
		public ComponentLookup<PlaceableObjectData> m_PrefabPlaceableObjectData;

		[ReadOnly]
		public ComponentLookup<ServiceUpgradeData> m_PrefabServiceUpgradeData;

		[ReadOnly]
		public ComponentLookup<BuildingData> m_PrefabBuildingData;

		[ReadOnly]
		public ComponentLookup<LotData> m_PrefabLotData;

		[ReadOnly]
		public ComponentLookup<PlaceableNetData> m_PrefabPlaceableNetData;

		[ReadOnly]
		public ComponentLookup<NetGeometryData> m_PrefabNetGeometryData;

		[ReadOnly]
		public BufferLookup<Game.Prefabs.SubArea> m_PrefabSubAreas;

		[ReadOnly]
		public BufferLookup<Game.Prefabs.SubNet> m_PrefabSubNets;

		[ReadOnly]
		public NativeList<ArchetypeChunk> m_DefinitionChunks;

		[ReadOnly]
		public NativeList<ControlPoint> m_ControlPoints;

		[ReadOnly]
		public NativeList<SubSnapPoint> m_SubSnapPoints;

		[ReadOnly]
		public NativeList<NetToolSystem.UpgradeState> m_NetUpgradeState;

		[ReadOnly]
		public TerrainHeightData m_TerrainHeightData;

		[ReadOnly]
		public WaterSurfaceData<SurfaceWater> m_WaterSurfaceData;

		[ReadOnly]
		public GuideLineSettingsData m_GuideLineSettingsData;

		[ReadOnly]
		public ObjectToolSystem.Mode m_Mode;

		[ReadOnly]
		public ObjectToolSystem.State m_State;

		[ReadOnly]
		public Entity m_Prefab;

		[ReadOnly]
		public float m_DistanceScale;

		public NativeList<bool> m_AngleSides;

		public NativeList<TooltipInfo> m_Tooltips;

		public OverlayRenderSystem.Buffer m_OverlayBuffer;

		public void Execute()
		{
			bool flag = m_NetUpgradeState.Length != 0;
			NativeParallelHashSet<float> nativeParallelHashSet = default(NativeParallelHashSet<float>);
			int angleIndex = 0;
			if (!flag && m_State != ObjectToolSystem.State.Adding && m_State != ObjectToolSystem.State.Removing && (m_Mode == ObjectToolSystem.Mode.Line || m_Mode == ObjectToolSystem.Mode.Curve))
			{
				DrawControlPoints();
			}
			Line3.Segment line = default(Line3.Segment);
			for (int i = 0; i < m_DefinitionChunks.Length; i++)
			{
				ArchetypeChunk archetypeChunk = m_DefinitionChunks[i];
				NativeArray<CreationDefinition> nativeArray = archetypeChunk.GetNativeArray(ref m_CreationDefinitionType);
				if (flag)
				{
					NativeArray<NetCourse> nativeArray2 = archetypeChunk.GetNativeArray(ref m_NetCourseType);
					for (int j = 0; j < nativeArray2.Length; j++)
					{
						CreationDefinition creationDefinition = nativeArray[j];
						NetCourse netCourse = nativeArray2[j];
						if ((creationDefinition.m_Flags & CreationFlags.Permanent) == 0 && m_PrefabNetGeometryData.TryGetComponent(creationDefinition.m_Prefab, out var componentData))
						{
							DrawNetCourse(m_OverlayBuffer, netCourse, ref m_TerrainHeightData, ref m_WaterSurfaceData, componentData, m_GuideLineSettingsData);
						}
					}
					continue;
				}
				NativeArray<ObjectDefinition> nativeArray3 = archetypeChunk.GetNativeArray(ref m_ObjectDefinitionType);
				NativeArray<OwnerDefinition> nativeArray4 = archetypeChunk.GetNativeArray(ref m_OwnerDefinitionType);
				for (int k = 0; k < nativeArray3.Length; k++)
				{
					CreationDefinition creationDefinition2 = nativeArray[k];
					ObjectDefinition objectDefinition = nativeArray3[k];
					if ((creationDefinition2.m_Flags & CreationFlags.Permanent) != 0)
					{
						continue;
					}
					if (m_PrefabPlaceableObjectData.TryGetComponent(creationDefinition2.m_Prefab, out var componentData2) && (componentData2.m_Flags & Game.Objects.PlacementFlags.Hovering) != Game.Objects.PlacementFlags.None)
					{
						ObjectGeometryData objectGeometryData = m_PrefabObjectGeometryData[creationDefinition2.m_Prefab];
						float x = MathUtils.Center(objectGeometryData.m_Bounds.x);
						float width = MathUtils.Size(objectGeometryData.m_Bounds.x);
						line.a = ObjectUtils.LocalToWorld(objectDefinition.m_Position, objectDefinition.m_Rotation, new float3(x, 0f, objectGeometryData.m_Bounds.min.z));
						line.b = ObjectUtils.LocalToWorld(objectDefinition.m_Position, objectDefinition.m_Rotation, new float3(x, 0f, objectGeometryData.m_Bounds.max.z));
						m_OverlayBuffer.DrawLine(m_GuideLineSettingsData.m_MediumPriorityColor, m_GuideLineSettingsData.m_MediumPriorityColor, 0f, OverlayRenderSystem.StyleFlags.Projected, line, width, 0.02f);
					}
					if (m_PrefabSubAreas.TryGetBuffer(creationDefinition2.m_Prefab, out var bufferData))
					{
						for (int l = 0; l < bufferData.Length; l++)
						{
							Game.Prefabs.SubArea subArea = bufferData[l];
							if (m_PrefabLotData.TryGetComponent(subArea.m_Prefab, out var componentData3) && componentData3.m_MaxRadius > 0f)
							{
								if (!nativeParallelHashSet.IsCreated)
								{
									nativeParallelHashSet = new NativeParallelHashSet<float>(10, Allocator.Temp);
								}
								if (nativeParallelHashSet.Add(componentData3.m_MaxRadius))
								{
									DrawAreaRange(m_OverlayBuffer, objectDefinition.m_Rotation, objectDefinition.m_Position, componentData3);
								}
							}
						}
						if (nativeParallelHashSet.IsCreated)
						{
							nativeParallelHashSet.Clear();
						}
					}
					if (m_PrefabServiceUpgradeData.TryGetComponent(creationDefinition2.m_Prefab, out var componentData4) && componentData4.m_MaxPlacementDistance != 0f && CollectionUtils.TryGet(nativeArray4, k, out var value) && m_PrefabBuildingData.TryGetComponent(value.m_Prefab, out var componentData5) && m_PrefabBuildingData.TryGetComponent(creationDefinition2.m_Prefab, out var componentData6))
					{
						DrawUpgradeRange(m_OverlayBuffer, value.m_Rotation, value.m_Position, m_GuideLineSettingsData, componentData5, componentData6, componentData4);
					}
					DrawSubSnapPoints(creationDefinition2.m_Prefab, objectDefinition.m_Position, objectDefinition.m_Rotation, ref angleIndex);
				}
			}
			if (m_Mode == ObjectToolSystem.Mode.Stamp)
			{
				for (int m = 0; m < m_ControlPoints.Length; m++)
				{
					ControlPoint controlPoint = m_ControlPoints[m];
					DrawSubSnapPoints(m_Prefab, controlPoint.m_Position, controlPoint.m_Rotation, ref angleIndex);
				}
			}
			if (nativeParallelHashSet.IsCreated)
			{
				nativeParallelHashSet.Dispose();
			}
		}

		private void DrawControlPoints()
		{
			int angleIndex = 0;
			Line3.Segment prevLine = default(Line3.Segment);
			float3 prevPoint = -1000000f;
			float num = m_DistanceScale;
			float num2 = num * 0.125f;
			float num3 = num * 4f;
			if (m_ControlPoints.Length >= 2)
			{
				Line3.Segment line = new Line3.Segment(m_ControlPoints[0].m_Position, m_ControlPoints[1].m_Position);
				float num4 = MathUtils.Length(line.xz);
				if (num4 > num2 * 7f)
				{
					float2 @float = (line.b.xz - line.a.xz) / num4;
					float2 leftDir = default(float2);
					float2 rightDir = default(float2);
					float2 leftDir2 = default(float2);
					float2 rightDir2 = default(float2);
					int bestLeft = 181;
					int bestRight = 181;
					int bestLeft2 = 181;
					int bestRight2 = 181;
					float2 float2 = default(float2);
					Game.Objects.Transform componentData;
					if (!m_ControlPoints[0].m_Direction.Equals(default(float2)))
					{
						float2 = m_ControlPoints[0].m_Direction;
					}
					else if (m_TransformData.TryGetComponent(m_ControlPoints[0].m_OriginalEntity, out componentData))
					{
						float2 = math.forward(componentData.m_Rotation).xz;
					}
					if (!float2.Equals(default(float2)))
					{
						CheckDirection(@float, float2, ref leftDir, ref rightDir, ref bestLeft, ref bestRight, ref leftDir2, ref rightDir2, ref bestLeft2, ref bestRight2);
						CheckDirection(@float, MathUtils.Right(float2), ref leftDir, ref rightDir, ref bestLeft, ref bestRight, ref leftDir2, ref rightDir2, ref bestLeft2, ref bestRight2);
						CheckDirection(@float, -float2, ref leftDir, ref rightDir, ref bestLeft, ref bestRight, ref leftDir2, ref rightDir2, ref bestLeft2, ref bestRight2);
						CheckDirection(@float, MathUtils.Left(float2), ref leftDir, ref rightDir, ref bestLeft, ref bestRight, ref leftDir2, ref rightDir2, ref bestLeft2, ref bestRight2);
					}
					bool value = bestRight < bestLeft;
					if (bestLeft == bestRight && m_AngleSides.Length > angleIndex)
					{
						value = m_AngleSides[angleIndex];
					}
					if (bestLeft == 180 && bestRight == 180)
					{
						if (value)
						{
							bestLeft = 181;
						}
						else
						{
							bestRight = 181;
						}
					}
					else
					{
						if (bestLeft2 <= 180 && bestRight2 <= 180)
						{
							if (bestLeft2 < bestRight2 || (bestLeft2 == bestRight2 && value))
							{
								bestRight2 = 181;
							}
							else
							{
								bestLeft2 = 181;
							}
						}
						if (bestLeft2 <= 180)
						{
							leftDir = leftDir2;
							bestLeft = bestLeft2;
						}
						else if (bestRight2 <= 180)
						{
							rightDir = rightDir2;
							bestRight = bestRight2;
						}
					}
					if (bestLeft <= 180)
					{
						Line3.Segment segment = new Line3.Segment(line.a, line.a);
						segment.a.xz += leftDir * math.min(num4, num3);
						m_OverlayBuffer.DrawLine(m_GuideLineSettingsData.m_HighPriorityColor, segment, num2);
						GuideLinesSystem.DrawAngleIndicator(m_OverlayBuffer, m_Tooltips, m_GuideLineSettingsData, segment, line, -leftDir, -@float, math.min(num4, num3) * 0.5f, num2, bestLeft, angleSide: false);
					}
					if (bestRight <= 180)
					{
						Line3.Segment segment2 = new Line3.Segment(line.a, line.a);
						segment2.a.xz += rightDir * math.min(num4, num3);
						m_OverlayBuffer.DrawLine(m_GuideLineSettingsData.m_HighPriorityColor, segment2, num2);
						GuideLinesSystem.DrawAngleIndicator(m_OverlayBuffer, m_Tooltips, m_GuideLineSettingsData, segment2, line, -rightDir, -@float, math.min(num4, num3) * 0.5f, num2, bestRight, angleSide: true);
					}
					if (m_AngleSides.Length > angleIndex)
					{
						m_AngleSides[angleIndex] = value;
					}
					else
					{
						while (m_AngleSides.Length <= angleIndex)
						{
							m_AngleSides.Add(in value);
						}
					}
				}
				angleIndex++;
			}
			if (m_ControlPoints.Length >= 2)
			{
				ControlPoint controlPoint = m_ControlPoints[0];
				for (int i = 1; i < m_ControlPoints.Length; i++)
				{
					ControlPoint controlPoint2 = m_ControlPoints[i];
					float num5 = (float)Mathf.RoundToInt(math.distance(controlPoint.m_Position.xz, controlPoint2.m_Position.xz) * 2f) / 2f;
					if (num5 > 0f)
					{
						m_Tooltips.Add(new TooltipInfo(TooltipType.Length, (controlPoint.m_Position + controlPoint2.m_Position) * 0.5f, num5));
					}
					controlPoint = controlPoint2;
				}
			}
			if (m_ControlPoints.Length >= 3 && !m_ControlPoints[0].m_Position.xz.Equals(m_ControlPoints[1].m_Position.xz) && !m_ControlPoints[2].m_Position.xz.Equals(m_ControlPoints[1].m_Position.xz))
			{
				float3 value2 = m_ControlPoints[1].m_Position - m_ControlPoints[0].m_Position;
				float3 value3 = m_ControlPoints[2].m_Position - m_ControlPoints[1].m_Position;
				value2 = MathUtils.Normalize(value2, value2.xz);
				value3 = MathUtils.Normalize(value3, value3.xz);
				Bezier4x3 input = NetUtils.FitCurve(m_ControlPoints[0].m_Position, value2, value3, m_ControlPoints[2].m_Position);
				float t = NetUtils.FindMiddleTangentPos(input.xz, new float2(0f, 1f));
				MathUtils.Divide(input, out var output, out var output2, t);
				m_OverlayBuffer.DrawCurve(m_GuideLineSettingsData.m_MediumPriorityColor, m_GuideLineSettingsData.m_MediumPriorityColor, 0f, OverlayRenderSystem.StyleFlags.Projected, output, num2, new float2(1f, 0f));
				m_OverlayBuffer.DrawCurve(m_GuideLineSettingsData.m_MediumPriorityColor, m_GuideLineSettingsData.m_MediumPriorityColor, 0f, OverlayRenderSystem.StyleFlags.Projected, output2, num2, new float2(0f, 1f));
			}
			else if (m_ControlPoints.Length >= 2 && !m_ControlPoints[0].m_Position.xz.Equals(m_ControlPoints[m_ControlPoints.Length - 1].m_Position.xz))
			{
				Line3.Segment line2 = new Line3.Segment(m_ControlPoints[0].m_Position, m_ControlPoints[m_ControlPoints.Length - 1].m_Position);
				m_OverlayBuffer.DrawLine(m_GuideLineSettingsData.m_MediumPriorityColor, m_GuideLineSettingsData.m_MediumPriorityColor, 0f, OverlayRenderSystem.StyleFlags.Projected, line2, num2, 1f);
			}
			else if (m_ControlPoints.Length >= 1)
			{
				float3 position = m_ControlPoints[0].m_Position;
				m_OverlayBuffer.DrawCircle(m_GuideLineSettingsData.m_MediumPriorityColor, m_GuideLineSettingsData.m_MediumPriorityColor, 0f, OverlayRenderSystem.StyleFlags.Projected, new float2(0f, 1f), position, num2);
			}
			for (int j = 0; j < m_ControlPoints.Length; j++)
			{
				ControlPoint controlPoint3 = m_ControlPoints[j];
				if (j > 0)
				{
					ControlPoint point = m_ControlPoints[j - 1];
					DrawControlPointLine(point, controlPoint3, num2, num3, ref angleIndex, ref prevLine);
				}
				DrawControlPoint(controlPoint3, num2, ref prevPoint);
			}
		}

		private void DrawControlPoint(ControlPoint point, float lineWidth, ref float3 prevPoint)
		{
			if (math.distancesq(prevPoint, point.m_Position) > 0.01f)
			{
				m_OverlayBuffer.DrawCircle(m_GuideLineSettingsData.m_HighPriorityColor, point.m_Position, lineWidth * 5f);
			}
			prevPoint = point.m_Position;
		}

		private void DrawControlPointLine(ControlPoint point1, ControlPoint point2, float lineWidth, float lineLength, ref int angleIndex, ref Line3.Segment prevLine)
		{
			Line3.Segment segment = new Line3.Segment(point1.m_Position, point2.m_Position);
			float num = math.distance(point1.m_Position.xz, point2.m_Position.xz);
			if (num > lineWidth * 8f)
			{
				float3 @float = (segment.b - segment.a) * (lineWidth * 4f / num);
				Line3.Segment line = new Line3.Segment(segment.a + @float, segment.b - @float);
				m_OverlayBuffer.DrawDashedLine(m_GuideLineSettingsData.m_HighPriorityColor, line, lineWidth * 3f, lineWidth * 5f, lineWidth * 3f);
			}
			DrawAngleIndicator(prevLine, segment, lineWidth, lineLength, angleIndex++);
			prevLine = segment;
		}

		private void DrawAngleIndicator(Line3.Segment line1, Line3.Segment line2, float lineWidth, float lineLength, int angleIndex)
		{
			bool value = true;
			if (m_AngleSides.Length > angleIndex)
			{
				value = m_AngleSides[angleIndex];
			}
			float num = math.distance(line1.a.xz, line1.b.xz);
			float num2 = math.distance(line2.a.xz, line2.b.xz);
			if (num > lineWidth * 7f && num2 > lineWidth * 7f)
			{
				float2 @float = (line1.b.xz - line1.a.xz) / num;
				float2 float2 = (line2.a.xz - line2.b.xz) / num2;
				float size = math.min(lineLength, math.min(num, num2)) * 0.5f;
				int num3 = Mathf.RoundToInt(math.degrees(math.acos(math.clamp(math.dot(@float, float2), -1f, 1f))));
				if (num3 < 180)
				{
					value = math.dot(MathUtils.Right(@float), float2) < 0f;
				}
				GuideLinesSystem.DrawAngleIndicator(m_OverlayBuffer, m_Tooltips, m_GuideLineSettingsData, line1, line2, @float, float2, size, lineWidth, num3, value);
			}
			if (m_AngleSides.Length > angleIndex)
			{
				m_AngleSides[angleIndex] = value;
				return;
			}
			while (m_AngleSides.Length <= angleIndex)
			{
				m_AngleSides.Add(in value);
			}
		}

		private void DrawSubSnapPoints(Entity prefab, float3 position, quaternion rotation, ref int angleIndex)
		{
			if (!m_PrefabSubNets.TryGetBuffer(prefab, out var bufferData))
			{
				return;
			}
			for (int i = 0; i < bufferData.Length; i++)
			{
				Game.Prefabs.SubNet subNet = bufferData[i];
				if (subNet.m_Snapping.x)
				{
					float3 pos = ObjectUtils.LocalToWorld(position, rotation, subNet.m_Curve.a);
					float2 tangent = math.select(default(float2), math.normalizesafe(math.mul(rotation, MathUtils.StartTangent(subNet.m_Curve)).xz), subNet.m_NodeIndex.y != subNet.m_NodeIndex.x);
					DrawSubSnapPoints(subNet.m_Prefab, pos, tangent, ref angleIndex);
				}
				if (subNet.m_Snapping.y)
				{
					float3 pos2 = ObjectUtils.LocalToWorld(position, rotation, subNet.m_Curve.d);
					float2 tangent2 = math.normalizesafe(math.mul(rotation, -MathUtils.EndTangent(subNet.m_Curve)).xz);
					DrawSubSnapPoints(subNet.m_Prefab, pos2, tangent2, ref angleIndex);
				}
			}
		}

		private void DrawSubSnapPoints(Entity prefab, float3 pos, float2 tangent, ref int angleIndex)
		{
			float2 leftDir = default(float2);
			float2 rightDir = default(float2);
			float2 leftDir2 = default(float2);
			float2 rightDir2 = default(float2);
			int bestLeft = 181;
			int bestRight = 181;
			int bestLeft2 = 181;
			int bestRight2 = 181;
			for (int i = 0; i < m_SubSnapPoints.Length; i++)
			{
				SubSnapPoint subSnapPoint = m_SubSnapPoints[i];
				if (!(math.distancesq(pos.xz, subSnapPoint.m_Position.xz) >= 0.01f))
				{
					CheckDirection(tangent, subSnapPoint.m_Tangent, ref leftDir, ref rightDir, ref bestLeft, ref bestRight, ref leftDir2, ref rightDir2, ref bestLeft2, ref bestRight2);
				}
			}
			float num = 1f;
			if (m_PrefabPlaceableNetData.TryGetComponent(prefab, out var componentData))
			{
				num = math.min(componentData.m_SnapDistance, 16f);
			}
			float num2 = num * 0.125f;
			float num3 = num * 4f;
			bool value = bestRight < bestLeft;
			if (bestLeft == bestRight && m_AngleSides.Length > angleIndex)
			{
				value = m_AngleSides[angleIndex];
			}
			if (bestLeft == 180 && bestRight == 180)
			{
				if (value)
				{
					bestLeft = 181;
				}
				else
				{
					bestRight = 181;
				}
			}
			else
			{
				if (bestLeft2 <= 180 && bestRight2 <= 180)
				{
					if (bestLeft2 < bestRight2 || (bestLeft2 == bestRight2 && value))
					{
						bestRight2 = 181;
					}
					else
					{
						bestLeft2 = 181;
					}
				}
				if (bestLeft2 <= 180)
				{
					leftDir = leftDir2;
					bestLeft = bestLeft2;
				}
				else if (bestRight2 <= 180)
				{
					rightDir = rightDir2;
					bestRight = bestRight2;
				}
			}
			if (bestLeft <= 180 || bestRight <= 180)
			{
				if (tangent.Equals(default(float2)))
				{
					UnityEngine.Color highPriorityColor = m_GuideLineSettingsData.m_HighPriorityColor;
					highPriorityColor.a = 0f;
					m_OverlayBuffer.DrawCircle(m_GuideLineSettingsData.m_HighPriorityColor, highPriorityColor, num2, (OverlayRenderSystem.StyleFlags)0, new float2(0f, 1f), pos, num3 * 0.5f);
				}
				else
				{
					Line3.Segment segment = new Line3.Segment(pos, pos);
					segment.b.xz += tangent * num3;
					m_OverlayBuffer.DrawLine(m_GuideLineSettingsData.m_HighPriorityColor, segment, num2);
					if (bestLeft <= 180)
					{
						Line3.Segment segment2 = new Line3.Segment(pos, pos);
						segment2.a.xz += leftDir * num3;
						m_OverlayBuffer.DrawLine(m_GuideLineSettingsData.m_HighPriorityColor, segment2, num2);
						GuideLinesSystem.DrawAngleIndicator(m_OverlayBuffer, m_Tooltips, m_GuideLineSettingsData, segment2, segment, -leftDir, -tangent, num3 * 0.5f, num2, bestLeft, angleSide: false);
					}
					if (bestRight <= 180)
					{
						Line3.Segment segment3 = new Line3.Segment(pos, pos);
						segment3.a.xz += rightDir * num3;
						m_OverlayBuffer.DrawLine(m_GuideLineSettingsData.m_HighPriorityColor, segment3, num2);
						GuideLinesSystem.DrawAngleIndicator(m_OverlayBuffer, m_Tooltips, m_GuideLineSettingsData, segment3, segment, -rightDir, -tangent, num3 * 0.5f, num2, bestRight, angleSide: true);
					}
				}
			}
			if (m_AngleSides.Length > angleIndex)
			{
				m_AngleSides[angleIndex] = value;
			}
			else
			{
				while (m_AngleSides.Length <= angleIndex)
				{
					m_AngleSides.Add(in value);
				}
			}
			angleIndex++;
		}
	}

	[BurstCompile]
	private struct AreaToolGuideLinesJob : IJob
	{
		[ReadOnly]
		public ComponentTypeHandle<CreationDefinition> m_CreationDefinitionType;

		[ReadOnly]
		public BufferTypeHandle<Game.Areas.Node> m_NodeType;

		[ReadOnly]
		public ComponentLookup<Owner> m_OwnerData;

		[ReadOnly]
		public ComponentLookup<Game.Objects.Transform> m_TransformData;

		[ReadOnly]
		public ComponentLookup<PrefabRef> m_PrefabRefData;

		[ReadOnly]
		public ComponentLookup<AreaGeometryData> m_PrefabGeometryData;

		[ReadOnly]
		public ComponentLookup<LotData> m_PrefabLotData;

		[ReadOnly]
		public BufferLookup<Game.Areas.Node> m_Nodes;

		[ReadOnly]
		public NativeList<ArchetypeChunk> m_DefinitionChunks;

		[ReadOnly]
		public NativeList<ControlPoint> m_ControlPoints;

		[ReadOnly]
		public NativeList<ControlPoint> m_MoveStartPositions;

		[ReadOnly]
		public AreaToolSystem.State m_State;

		[ReadOnly]
		public Entity m_Prefab;

		[ReadOnly]
		public GuideLineSettingsData m_GuideLineSettingsData;

		public NativeList<bool> m_AngleSides;

		public NativeList<TooltipInfo> m_Tooltips;

		public OverlayRenderSystem.Buffer m_OverlayBuffer;

		public void Execute()
		{
			if (m_ControlPoints.Length <= 0)
			{
				return;
			}
			switch (m_State)
			{
			case AreaToolSystem.State.Default:
			{
				for (int n = 0; n < m_DefinitionChunks.Length; n++)
				{
					ArchetypeChunk archetypeChunk3 = m_DefinitionChunks[n];
					NativeArray<CreationDefinition> nativeArray3 = archetypeChunk3.GetNativeArray(ref m_CreationDefinitionType);
					BufferAccessor<Game.Areas.Node> bufferAccessor3 = archetypeChunk3.GetBufferAccessor(ref m_NodeType);
					for (int num = 0; num < bufferAccessor3.Length; num++)
					{
						CreationDefinition creationDefinition3 = nativeArray3[num];
						if ((creationDefinition3.m_Flags & CreationFlags.Permanent) != 0 || !m_PrefabRefData.HasComponent(creationDefinition3.m_Original) || !m_OwnerData.HasComponent(creationDefinition3.m_Original))
						{
							continue;
						}
						PrefabRef prefabRef2 = m_PrefabRefData[creationDefinition3.m_Original];
						Owner owner2 = m_OwnerData[creationDefinition3.m_Original];
						if (m_PrefabLotData.HasComponent(prefabRef2.m_Prefab) && m_TransformData.HasComponent(owner2.m_Owner))
						{
							LotData lotData2 = m_PrefabLotData[prefabRef2.m_Prefab];
							Game.Objects.Transform transform2 = m_TransformData[owner2.m_Owner];
							if (!(lotData2.m_MaxRadius <= 0f))
							{
								DrawAreaRange(m_OverlayBuffer, transform2.m_Rotation, transform2.m_Position, lotData2);
							}
						}
					}
				}
				ControlPoint controlPoint3 = m_ControlPoints[0];
				if (m_Nodes.HasBuffer(controlPoint3.m_OriginalEntity) && math.any(controlPoint3.m_ElementIndex >= 0))
				{
					PrefabRef prefabRef3 = m_PrefabRefData[controlPoint3.m_OriginalEntity];
					AreaGeometryData areaGeometryData3 = m_PrefabGeometryData[prefabRef3.m_Prefab];
					m_OverlayBuffer.DrawCircle(m_GuideLineSettingsData.m_HighPriorityColor, controlPoint3.m_Position, areaGeometryData3.m_SnapDistance * 0.5f);
					break;
				}
				for (int num2 = 0; num2 < m_DefinitionChunks.Length; num2++)
				{
					ArchetypeChunk archetypeChunk4 = m_DefinitionChunks[num2];
					NativeArray<CreationDefinition> nativeArray4 = archetypeChunk4.GetNativeArray(ref m_CreationDefinitionType);
					BufferAccessor<Game.Areas.Node> bufferAccessor4 = archetypeChunk4.GetBufferAccessor(ref m_NodeType);
					for (int num3 = 0; num3 < bufferAccessor4.Length; num3++)
					{
						CreationDefinition creationDefinition4 = nativeArray4[num3];
						DynamicBuffer<Game.Areas.Node> dynamicBuffer2 = bufferAccessor4[num3];
						if ((creationDefinition4.m_Flags & CreationFlags.Permanent) == 0 && m_PrefabRefData.HasComponent(creationDefinition4.m_Original) && dynamicBuffer2.Length != 0)
						{
							PrefabRef prefabRef4 = m_PrefabRefData[creationDefinition4.m_Original];
							if (m_PrefabGeometryData.HasComponent(prefabRef4.m_Prefab))
							{
								AreaGeometryData areaGeometryData4 = m_PrefabGeometryData[prefabRef4.m_Prefab];
								Game.Areas.Node node2 = dynamicBuffer2[0];
								m_OverlayBuffer.DrawCircle(m_GuideLineSettingsData.m_HighPriorityColor, node2.m_Position, areaGeometryData4.m_SnapDistance * 0.5f);
							}
						}
					}
				}
				break;
			}
			case AreaToolSystem.State.Create:
			{
				for (int j = 0; j < m_DefinitionChunks.Length; j++)
				{
					ArchetypeChunk archetypeChunk = m_DefinitionChunks[j];
					NativeArray<CreationDefinition> nativeArray = archetypeChunk.GetNativeArray(ref m_CreationDefinitionType);
					BufferAccessor<Game.Areas.Node> bufferAccessor = archetypeChunk.GetBufferAccessor(ref m_NodeType);
					for (int k = 0; k < bufferAccessor.Length; k++)
					{
						CreationDefinition creationDefinition = nativeArray[k];
						if ((creationDefinition.m_Flags & CreationFlags.Permanent) == 0 && m_PrefabLotData.HasComponent(creationDefinition.m_Prefab) && m_OwnerData.HasComponent(creationDefinition.m_Original))
						{
							LotData lotData = m_PrefabLotData[creationDefinition.m_Prefab];
							Owner owner = m_OwnerData[creationDefinition.m_Original];
							if (!(lotData.m_MaxRadius <= 0f) && m_TransformData.HasComponent(owner.m_Owner))
							{
								Game.Objects.Transform transform = m_TransformData[owner.m_Owner];
								DrawAreaRange(m_OverlayBuffer, transform.m_Rotation, transform.m_Position, lotData);
							}
						}
					}
				}
				for (int l = 0; l < m_DefinitionChunks.Length; l++)
				{
					ArchetypeChunk archetypeChunk2 = m_DefinitionChunks[l];
					NativeArray<CreationDefinition> nativeArray2 = archetypeChunk2.GetNativeArray(ref m_CreationDefinitionType);
					BufferAccessor<Game.Areas.Node> bufferAccessor2 = archetypeChunk2.GetBufferAccessor(ref m_NodeType);
					for (int m = 0; m < bufferAccessor2.Length; m++)
					{
						CreationDefinition creationDefinition2 = nativeArray2[m];
						if ((creationDefinition2.m_Flags & CreationFlags.Permanent) == 0 && m_PrefabGeometryData.HasComponent(creationDefinition2.m_Prefab))
						{
							DynamicBuffer<Game.Areas.Node> dynamicBuffer = bufferAccessor2[m];
							if (dynamicBuffer.Length != 0)
							{
								AreaGeometryData areaGeometryData2 = m_PrefabGeometryData[creationDefinition2.m_Prefab];
								Game.Areas.Node node = dynamicBuffer[dynamicBuffer.Length - 1];
								m_OverlayBuffer.DrawCircle(m_GuideLineSettingsData.m_HighPriorityColor, node.m_Position, areaGeometryData2.m_SnapDistance * 0.5f);
							}
						}
					}
				}
				DrawAngles();
				break;
			}
			case AreaToolSystem.State.Modify:
			case AreaToolSystem.State.Remove:
			{
				for (int i = 0; i < m_MoveStartPositions.Length; i++)
				{
					ControlPoint controlPoint = m_MoveStartPositions[i];
					if (m_PrefabRefData.TryGetComponent(controlPoint.m_OriginalEntity, out var componentData) && m_OwnerData.TryGetComponent(controlPoint.m_OriginalEntity, out var componentData2) && m_PrefabLotData.TryGetComponent(componentData.m_Prefab, out var componentData3) && m_TransformData.TryGetComponent(componentData2.m_Owner, out var componentData4) && componentData3.m_MaxRadius > 0f)
					{
						DrawAreaRange(m_OverlayBuffer, componentData4.m_Rotation, componentData4.m_Position, componentData3);
					}
				}
				if (m_MoveStartPositions.Length != 0)
				{
					ControlPoint other = m_MoveStartPositions[0];
					if (m_Nodes.HasBuffer(other.m_OriginalEntity) && math.any(other.m_ElementIndex >= 0))
					{
						PrefabRef prefabRef = m_PrefabRefData[other.m_OriginalEntity];
						AreaGeometryData areaGeometryData = m_PrefabGeometryData[prefabRef.m_Prefab];
						ControlPoint controlPoint2 = m_ControlPoints[0];
						if (controlPoint2.Equals(default(ControlPoint)) || controlPoint2.Equals(other))
						{
							m_OverlayBuffer.DrawCircle(m_GuideLineSettingsData.m_HighPriorityColor, other.m_Position, areaGeometryData.m_SnapDistance * 0.5f);
						}
						else
						{
							m_OverlayBuffer.DrawCircle(m_GuideLineSettingsData.m_HighPriorityColor, controlPoint2.m_Position, areaGeometryData.m_SnapDistance * 0.5f);
						}
					}
				}
				DrawAngles();
				break;
			}
			}
		}

		private void DrawAngles()
		{
			int num;
			switch (m_State)
			{
			default:
				return;
			case AreaToolSystem.State.Create:
				num = math.select(0, 1, m_ControlPoints.Length >= 2);
				break;
			case AreaToolSystem.State.Modify:
				num = m_MoveStartPositions.Length * 2;
				break;
			}
			if (!m_PrefabGeometryData.HasComponent(m_Prefab))
			{
				return;
			}
			float num2 = math.min(m_PrefabGeometryData[m_Prefab].m_SnapDistance, 16f);
			float num3 = num2 * 0.125f;
			float y = num2 * 4f;
			for (int i = 0; i < num; i++)
			{
				float2 leftDir = default(float2);
				float2 rightDir = default(float2);
				float2 leftDir2 = default(float2);
				float2 rightDir2 = default(float2);
				int bestLeft = 181;
				int bestRight = 181;
				int bestLeft2 = 181;
				int bestRight2 = 181;
				Line3.Segment line;
				float num4;
				float2 @float;
				if (m_State == AreaToolSystem.State.Create)
				{
					line = new Line3.Segment(m_ControlPoints[m_ControlPoints.Length - 2].m_Position, m_ControlPoints[m_ControlPoints.Length - 1].m_Position);
					num4 = MathUtils.Length(line.xz);
					@float = (line.b.xz - line.a.xz) / num4;
					if (m_ControlPoints.Length >= 3)
					{
						ControlPoint controlPoint = m_ControlPoints[m_ControlPoints.Length - 3];
						ControlPoint controlPoint2 = m_ControlPoints[m_ControlPoints.Length - 2];
						float2 checkDir = math.normalizesafe(controlPoint.m_Position.xz - controlPoint2.m_Position.xz);
						if (!checkDir.Equals(default(float2)))
						{
							CheckDirection(@float, checkDir, ref leftDir, ref rightDir, ref bestLeft, ref bestRight, ref leftDir2, ref rightDir2, ref bestLeft2, ref bestRight2);
						}
					}
					if (bestLeft > 180 && bestRight > 180)
					{
						ControlPoint controlPoint3 = m_ControlPoints[m_ControlPoints.Length - 2];
						if (!controlPoint3.m_Direction.Equals(default(float2)))
						{
							CheckDirection(@float, controlPoint3.m_Direction, ref leftDir, ref rightDir, ref bestLeft, ref bestRight, ref leftDir2, ref rightDir2, ref bestLeft2, ref bestRight2);
							CheckDirection(@float, MathUtils.Right(controlPoint3.m_Direction), ref leftDir, ref rightDir, ref bestLeft, ref bestRight, ref leftDir2, ref rightDir2, ref bestLeft2, ref bestRight2);
							CheckDirection(@float, -controlPoint3.m_Direction, ref leftDir, ref rightDir, ref bestLeft, ref bestRight, ref leftDir2, ref rightDir2, ref bestLeft2, ref bestRight2);
							CheckDirection(@float, MathUtils.Left(controlPoint3.m_Direction), ref leftDir, ref rightDir, ref bestLeft, ref bestRight, ref leftDir2, ref rightDir2, ref bestLeft2, ref bestRight2);
						}
					}
				}
				else
				{
					ControlPoint controlPoint4 = m_MoveStartPositions[i >> 1];
					if (!m_Nodes.HasBuffer(controlPoint4.m_OriginalEntity) || !math.any(controlPoint4.m_ElementIndex >= 0))
					{
						continue;
					}
					DynamicBuffer<Game.Areas.Node> dynamicBuffer = m_Nodes[controlPoint4.m_OriginalEntity];
					int2 @int;
					if ((i & 1) == 0)
					{
						@int = math.select(controlPoint4.m_ElementIndex.x + new int2(-1, -2), controlPoint4.m_ElementIndex.y + new int2(0, -1), controlPoint4.m_ElementIndex.y >= 0);
						@int = math.select(@int, @int + dynamicBuffer.Length, @int < 0);
					}
					else
					{
						@int = math.select(controlPoint4.m_ElementIndex.x + new int2(1, 2), controlPoint4.m_ElementIndex.y + new int2(1, 2), controlPoint4.m_ElementIndex.y >= 0);
						@int = math.select(@int, @int - dynamicBuffer.Length, @int >= dynamicBuffer.Length);
					}
					float3 position = m_ControlPoints[0].m_Position;
					float3 position2 = dynamicBuffer[@int.x].m_Position;
					float3 position3 = dynamicBuffer[@int.y].m_Position;
					line = new Line3.Segment(position2, position);
					num4 = MathUtils.Length(line.xz);
					@float = (line.b.xz - line.a.xz) / num4;
					float2 checkDir2 = math.normalizesafe(position3.xz - position2.xz);
					if (!checkDir2.Equals(default(float2)))
					{
						CheckDirection(@float, checkDir2, ref leftDir, ref rightDir, ref bestLeft, ref bestRight, ref leftDir2, ref rightDir2, ref bestLeft2, ref bestRight2);
					}
				}
				bool value = bestRight < bestLeft;
				if (bestLeft == bestRight && m_AngleSides.Length > i)
				{
					value = m_AngleSides[i];
				}
				if (bestLeft == 180 && bestRight == 180)
				{
					if (value)
					{
						bestLeft = 181;
					}
					else
					{
						bestRight = 181;
					}
				}
				else
				{
					if (bestLeft2 <= 180 && bestRight2 <= 180)
					{
						if (bestLeft2 < bestRight2 || (bestLeft2 == bestRight2 && value))
						{
							bestRight2 = 181;
						}
						else
						{
							bestLeft2 = 181;
						}
					}
					if (bestLeft2 <= 180)
					{
						leftDir = leftDir2;
						bestLeft = bestLeft2;
					}
					else if (bestRight2 <= 180)
					{
						rightDir = rightDir2;
						bestRight = bestRight2;
					}
				}
				if (bestLeft <= 180 || bestRight <= 180)
				{
					Line3.Segment line2 = new Line3.Segment(line.a, line.a);
					line2.a.xz += @float * math.min(num4, y);
					m_OverlayBuffer.DrawLine(m_GuideLineSettingsData.m_HighPriorityColor, line2, num3);
				}
				if (bestLeft <= 180)
				{
					Line3.Segment segment = new Line3.Segment(line.a, line.a);
					segment.a.xz += leftDir * math.min(num4, y);
					m_OverlayBuffer.DrawLine(m_GuideLineSettingsData.m_HighPriorityColor, segment, num3);
					DrawAngleIndicator(m_OverlayBuffer, m_Tooltips, m_GuideLineSettingsData, segment, line, -leftDir, -@float, math.min(num4, y) * 0.5f, num3, bestLeft, angleSide: false);
				}
				if (bestRight <= 180)
				{
					Line3.Segment segment2 = new Line3.Segment(line.a, line.a);
					segment2.a.xz += rightDir * math.min(num4, y);
					m_OverlayBuffer.DrawLine(m_GuideLineSettingsData.m_HighPriorityColor, segment2, num3);
					DrawAngleIndicator(m_OverlayBuffer, m_Tooltips, m_GuideLineSettingsData, segment2, line, -rightDir, -@float, math.min(num4, y) * 0.5f, num3, bestRight, angleSide: true);
				}
				if (m_AngleSides.Length > i)
				{
					m_AngleSides[i] = value;
					continue;
				}
				while (m_AngleSides.Length <= i)
				{
					m_AngleSides.Add(in value);
				}
			}
		}
	}

	[BurstCompile]
	private struct SelectionToolGuideLinesJob : IJob
	{
		[ReadOnly]
		public SelectionToolSystem.State m_State;

		[ReadOnly]
		public SelectionType m_SelectionType;

		[ReadOnly]
		public bool m_SelectionQuadIsValid;

		[ReadOnly]
		public Quad3 m_SelectionQuad;

		[ReadOnly]
		public GuideLineSettingsData m_GuideLineSettingsData;

		public OverlayRenderSystem.Buffer m_OverlayBuffer;

		public void Execute()
		{
			SelectionToolSystem.State state = m_State;
			if ((uint)(state - 1) <= 1u && m_SelectionQuadIsValid)
			{
				float num = m_SelectionType switch
				{
					SelectionType.ServiceDistrict => AreaUtils.GetMinNodeDistance(Game.Areas.AreaType.District) * 2f, 
					SelectionType.MapTiles => AreaUtils.GetMinNodeDistance(Game.Areas.AreaType.MapTile) * 2f, 
					_ => 16f, 
				};
				float width = num * 0.125f;
				float dashLength = num * 0.7f;
				float gapLength = num * 0.3f;
				m_OverlayBuffer.DrawDashedLine(m_GuideLineSettingsData.m_HighPriorityColor, m_SelectionQuad.ab, width, dashLength, gapLength, 1f);
				m_OverlayBuffer.DrawDashedLine(m_GuideLineSettingsData.m_HighPriorityColor, m_SelectionQuad.ad, width, dashLength, gapLength, 1f);
				m_OverlayBuffer.DrawDashedLine(m_GuideLineSettingsData.m_HighPriorityColor, m_SelectionQuad.bc, width, dashLength, gapLength, 1f);
				m_OverlayBuffer.DrawDashedLine(m_GuideLineSettingsData.m_HighPriorityColor, m_SelectionQuad.dc, width, dashLength, gapLength, 1f);
			}
		}
	}

	[BurstCompile]
	private struct ZoneToolGuideLinesJob : IJob
	{
		[ReadOnly]
		public ComponentTypeHandle<CreationDefinition> m_CreationDefinitionType;

		[ReadOnly]
		public ComponentTypeHandle<Zoning> m_ZoningType;

		[ReadOnly]
		public NativeList<ArchetypeChunk> m_DefinitionChunks;

		[ReadOnly]
		public GuideLineSettingsData m_GuideLineSettingsData;

		public OverlayRenderSystem.Buffer m_OverlayBuffer;

		public void Execute()
		{
			for (int i = 0; i < m_DefinitionChunks.Length; i++)
			{
				ArchetypeChunk archetypeChunk = m_DefinitionChunks[i];
				NativeArray<CreationDefinition> nativeArray = archetypeChunk.GetNativeArray(ref m_CreationDefinitionType);
				NativeArray<Zoning> nativeArray2 = archetypeChunk.GetNativeArray(ref m_ZoningType);
				for (int j = 0; j < nativeArray2.Length; j++)
				{
					CreationDefinition creationDefinition = nativeArray[j];
					Zoning zoning = nativeArray2[j];
					if ((creationDefinition.m_Flags & CreationFlags.Permanent) == 0 && (zoning.m_Flags & ZoningFlags.Marquee) != 0)
					{
						float3 a = zoning.m_Position.a;
						float3 b = zoning.m_Position.b;
						float3 c = zoning.m_Position.c;
						float3 d = zoning.m_Position.d;
						float width = 1f;
						float dashLength = 5.6f;
						float gapLength = 2.4f;
						m_OverlayBuffer.DrawDashedLine(m_GuideLineSettingsData.m_HighPriorityColor, new Line3.Segment(a, b), width, dashLength, gapLength, 1f);
						m_OverlayBuffer.DrawDashedLine(m_GuideLineSettingsData.m_HighPriorityColor, new Line3.Segment(b, c), width, dashLength, gapLength, 1f);
						m_OverlayBuffer.DrawDashedLine(m_GuideLineSettingsData.m_HighPriorityColor, new Line3.Segment(c, d), width, dashLength, gapLength, 1f);
						m_OverlayBuffer.DrawDashedLine(m_GuideLineSettingsData.m_HighPriorityColor, new Line3.Segment(d, a), width, dashLength, gapLength, 1f);
					}
				}
			}
		}
	}

	[BurstCompile]
	private struct RouteToolGuideLinesJob : IJob
	{
		[ReadOnly]
		public ComponentTypeHandle<CreationDefinition> m_CreationDefinitionType;

		[ReadOnly]
		public BufferTypeHandle<WaypointDefinition> m_WaypointDefinitionType;

		[ReadOnly]
		public ComponentLookup<Route> m_RouteData;

		[ReadOnly]
		public ComponentLookup<PrefabRef> m_PrefabRefData;

		[ReadOnly]
		public ComponentLookup<RouteData> m_PrefabRouteData;

		[ReadOnly]
		public NativeList<ArchetypeChunk> m_DefinitionChunks;

		[ReadOnly]
		public NativeList<ControlPoint> m_ControlPoints;

		[ReadOnly]
		public ControlPoint m_MoveStartPosition;

		[ReadOnly]
		public RouteToolSystem.State m_State;

		[ReadOnly]
		public GuideLineSettingsData m_GuideLineSettingsData;

		public OverlayRenderSystem.Buffer m_OverlayBuffer;

		public void Execute()
		{
			if (m_ControlPoints.Length <= 0)
			{
				return;
			}
			switch (m_State)
			{
			case RouteToolSystem.State.Default:
			{
				ControlPoint controlPoint2 = m_ControlPoints[0];
				if (m_RouteData.HasComponent(controlPoint2.m_OriginalEntity) && math.any(controlPoint2.m_ElementIndex >= 0))
				{
					PrefabRef prefabRef2 = m_PrefabRefData[controlPoint2.m_OriginalEntity];
					RouteData routeData3 = m_PrefabRouteData[prefabRef2.m_Prefab];
					if (controlPoint2.m_ElementIndex.x >= 0)
					{
						m_OverlayBuffer.DrawCircle(m_GuideLineSettingsData.m_HighPriorityColor, controlPoint2.m_Position, routeData3.m_Width * 1.7777778f);
					}
					else
					{
						m_OverlayBuffer.DrawCircle(m_GuideLineSettingsData.m_HighPriorityColor, controlPoint2.m_Position, routeData3.m_Width * (8f / 9f));
					}
					break;
				}
				for (int k = 0; k < m_DefinitionChunks.Length; k++)
				{
					ArchetypeChunk archetypeChunk2 = m_DefinitionChunks[k];
					NativeArray<CreationDefinition> nativeArray2 = archetypeChunk2.GetNativeArray(ref m_CreationDefinitionType);
					BufferAccessor<WaypointDefinition> bufferAccessor2 = archetypeChunk2.GetBufferAccessor(ref m_WaypointDefinitionType);
					for (int l = 0; l < bufferAccessor2.Length; l++)
					{
						CreationDefinition creationDefinition2 = nativeArray2[l];
						if ((creationDefinition2.m_Flags & CreationFlags.Permanent) == 0 && m_PrefabRouteData.HasComponent(creationDefinition2.m_Prefab))
						{
							DynamicBuffer<WaypointDefinition> dynamicBuffer2 = bufferAccessor2[l];
							if (dynamicBuffer2.Length != 0)
							{
								RouteData routeData4 = m_PrefabRouteData[creationDefinition2.m_Prefab];
								WaypointDefinition waypointDefinition2 = dynamicBuffer2[0];
								m_OverlayBuffer.DrawCircle(m_GuideLineSettingsData.m_HighPriorityColor, waypointDefinition2.m_Position, routeData4.m_Width * 1.7777778f);
							}
						}
					}
				}
				break;
			}
			case RouteToolSystem.State.Create:
			{
				for (int i = 0; i < m_DefinitionChunks.Length; i++)
				{
					ArchetypeChunk archetypeChunk = m_DefinitionChunks[i];
					NativeArray<CreationDefinition> nativeArray = archetypeChunk.GetNativeArray(ref m_CreationDefinitionType);
					BufferAccessor<WaypointDefinition> bufferAccessor = archetypeChunk.GetBufferAccessor(ref m_WaypointDefinitionType);
					for (int j = 0; j < bufferAccessor.Length; j++)
					{
						CreationDefinition creationDefinition = nativeArray[j];
						if ((creationDefinition.m_Flags & CreationFlags.Permanent) == 0 && m_PrefabRouteData.HasComponent(creationDefinition.m_Prefab))
						{
							DynamicBuffer<WaypointDefinition> dynamicBuffer = bufferAccessor[j];
							if (dynamicBuffer.Length != 0)
							{
								RouteData routeData2 = m_PrefabRouteData[creationDefinition.m_Prefab];
								WaypointDefinition waypointDefinition = dynamicBuffer[dynamicBuffer.Length - 1];
								m_OverlayBuffer.DrawCircle(m_GuideLineSettingsData.m_HighPriorityColor, waypointDefinition.m_Position, routeData2.m_Width * 1.7777778f);
							}
						}
					}
				}
				break;
			}
			case RouteToolSystem.State.Modify:
			case RouteToolSystem.State.Remove:
			{
				if (!m_RouteData.HasComponent(m_MoveStartPosition.m_OriginalEntity) || !math.any(m_MoveStartPosition.m_ElementIndex >= 0))
				{
					break;
				}
				PrefabRef prefabRef = m_PrefabRefData[m_MoveStartPosition.m_OriginalEntity];
				RouteData routeData = m_PrefabRouteData[prefabRef.m_Prefab];
				ControlPoint controlPoint = m_ControlPoints[m_ControlPoints.Length - 1];
				if (controlPoint.Equals(default(ControlPoint)) || controlPoint.Equals(m_MoveStartPosition))
				{
					if (m_MoveStartPosition.m_ElementIndex.x >= 0)
					{
						m_OverlayBuffer.DrawCircle(m_GuideLineSettingsData.m_HighPriorityColor, m_MoveStartPosition.m_Position, routeData.m_Width * 1.7777778f);
					}
					else
					{
						m_OverlayBuffer.DrawCircle(m_GuideLineSettingsData.m_HighPriorityColor, m_MoveStartPosition.m_Position, routeData.m_Width * (8f / 9f));
					}
				}
				else
				{
					m_OverlayBuffer.DrawCircle(m_GuideLineSettingsData.m_HighPriorityColor, controlPoint.m_Position, routeData.m_Width * 1.7777778f);
				}
				break;
			}
			}
		}
	}

	[BurstCompile]
	private struct NetToolGuideLinesJob : IJob
	{
		private struct SnapDir
		{
			public float2 m_Direction;

			public float2 m_Height;

			public float2 m_Factor;
		}

		[ReadOnly]
		public ComponentTypeHandle<CreationDefinition> m_CreationDefinitionType;

		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentTypeHandle<NetCourse> m_NetCourseType;

		[ReadOnly]
		public ComponentTypeHandle<Game.Net.Node> m_NodeType;

		[ReadOnly]
		public ComponentTypeHandle<Temp> m_TempType;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> m_PrefabRefType;

		[ReadOnly]
		public BufferTypeHandle<ConnectedEdge> m_ConnectedEdgeType;

		[ReadOnly]
		public ComponentLookup<Edge> m_EdgeData;

		[ReadOnly]
		public ComponentLookup<Game.Net.Node> m_NodeData;

		[ReadOnly]
		public ComponentLookup<Curve> m_CurveData;

		[ReadOnly]
		public ComponentLookup<Game.Objects.Transform> m_TransformData;

		[ReadOnly]
		public ComponentLookup<Temp> m_TempData;

		[ReadOnly]
		public ComponentLookup<PrefabRef> m_PrefabRefData;

		[ReadOnly]
		public ComponentLookup<NetGeometryData> m_NetGeometryData;

		[ReadOnly]
		public ComponentLookup<PlaceableNetData> m_PlaceableNetData;

		[ReadOnly]
		public ComponentLookup<RoadData> m_RoadData;

		[ReadOnly]
		public ComponentLookup<ElectricityConnectionData> m_ElectricityConnectionData;

		[ReadOnly]
		public ComponentLookup<WaterPipeConnectionData> m_WaterPipeConnectionData;

		[ReadOnly]
		public ComponentLookup<ResourceConnectionData> m_ResourceConnectionData;

		[ReadOnly]
		public BufferLookup<ConnectedEdge> m_ConnectedEdges;

		[ReadOnly]
		public BufferLookup<ConnectedNode> m_ConnectedNodes;

		[ReadOnly]
		public NativeList<ArchetypeChunk> m_DefinitionChunks;

		[NativeDisableContainerSafetyRestriction]
		[ReadOnly]
		public NativeList<ArchetypeChunk> m_MarkerNodeChunks;

		[NativeDisableContainerSafetyRestriction]
		[ReadOnly]
		public NativeList<ArchetypeChunk> m_TempNodeChunks;

		[ReadOnly]
		public NativeList<ControlPoint> m_ControlPoints;

		[ReadOnly]
		public NativeList<SnapLine> m_SnapLines;

		[ReadOnly]
		public TerrainHeightData m_TerrainHeightData;

		[ReadOnly]
		public WaterSurfaceData<SurfaceWater> m_WaterSurfaceData;

		[ReadOnly]
		public NetToolSystem.Mode m_Mode;

		[ReadOnly]
		public Game.Prefabs.ElectricityConnection.Voltage m_HighlightVoltage;

		[ReadOnly]
		public bool2 m_HighlightWater;

		[ReadOnly]
		public bool m_HighResourceLine;

		[ReadOnly]
		public Entity m_Prefab;

		[ReadOnly]
		public GuideLineSettingsData m_GuideLineSettingsData;

		public NativeList<bool> m_AngleSides;

		public NativeList<TooltipInfo> m_Tooltips;

		public OverlayRenderSystem.Buffer m_OverlayBuffer;

		public void Execute()
		{
			DrawZones();
			DrawCourses();
			if (m_Mode != NetToolSystem.Mode.Replace)
			{
				DrawSnapLines();
				DrawControlPoints();
				DrawNodeConnections(out var lastNodePosition, out var lastNodeWidth);
				DrawMarkers(lastNodePosition, lastNodeWidth);
			}
		}

		private void DrawNodeConnections(out float3 lastNodePosition, out float lastNodeWidth)
		{
			lastNodePosition = float.MinValue;
			lastNodeWidth = 0f;
			if (!m_TempNodeChunks.IsCreated)
			{
				return;
			}
			for (int i = 0; i < m_TempNodeChunks.Length; i++)
			{
				ArchetypeChunk archetypeChunk = m_TempNodeChunks[i];
				NativeArray<Entity> nativeArray = archetypeChunk.GetNativeArray(m_EntityType);
				NativeArray<Game.Net.Node> nativeArray2 = archetypeChunk.GetNativeArray(ref m_NodeType);
				NativeArray<Temp> nativeArray3 = archetypeChunk.GetNativeArray(ref m_TempType);
				NativeArray<PrefabRef> nativeArray4 = archetypeChunk.GetNativeArray(ref m_PrefabRefType);
				BufferAccessor<ConnectedEdge> bufferAccessor = archetypeChunk.GetBufferAccessor(ref m_ConnectedEdgeType);
				for (int j = 0; j < nativeArray.Length; j++)
				{
					Entity entity = nativeArray[j];
					Game.Net.Node node = nativeArray2[j];
					Temp temp = nativeArray3[j];
					PrefabRef prefabRef = nativeArray4[j];
					DynamicBuffer<ConnectedEdge> dynamicBuffer = bufferAccessor[j];
					if ((temp.m_Flags & TempFlags.IsLast) == 0 || !m_NetGeometryData.TryGetComponent(prefabRef.m_Prefab, out var componentData))
					{
						continue;
					}
					UnityEngine.Color positiveFeedbackColor = m_GuideLineSettingsData.m_PositiveFeedbackColor;
					positiveFeedbackColor.a *= 0.1f;
					float num = componentData.m_DefaultWidth + 12f;
					float outlineWidth = (math.sqrt(num + 1f) - 1f) * 0.2f;
					if (m_PrefabRefData.TryGetComponent(temp.m_Original, out var componentData2))
					{
						if (!CheckConnectionType(componentData2))
						{
							continue;
						}
						if (m_ControlPoints.Length == 2)
						{
							bool flag = false;
							for (int k = 0; k < dynamicBuffer.Length; k++)
							{
								if (m_TempData.TryGetComponent(dynamicBuffer[k].m_Edge, out var componentData3) && (componentData3.m_Flags & TempFlags.Essential) != 0)
								{
									flag = true;
								}
							}
							if (!flag)
							{
								continue;
							}
						}
						m_OverlayBuffer.DrawCircle(m_GuideLineSettingsData.m_PositiveFeedbackColor, positiveFeedbackColor, outlineWidth, (OverlayRenderSystem.StyleFlags)0, new float2(0f, 1f), node.m_Position, num);
						continue;
					}
					if (dynamicBuffer.Length == 0)
					{
						lastNodePosition = node.m_Position;
						lastNodeWidth = componentData.m_DefaultWidth;
					}
					for (int l = 0; l < dynamicBuffer.Length; l++)
					{
						Edge edge = m_EdgeData[dynamicBuffer[l].m_Edge];
						bool2 x = new bool2(edge.m_Start == entity, edge.m_End == entity);
						if (!m_ConnectedNodes.TryGetBuffer(dynamicBuffer[l].m_Edge, out var bufferData))
						{
							continue;
						}
						for (int m = 0; m < bufferData.Length; m++)
						{
							ConnectedNode connectedNode = bufferData[m];
							float3 position;
							if (math.any(x))
							{
								if ((x.x ? (connectedNode.m_CurvePosition > 0.5f) : (connectedNode.m_CurvePosition < 0.5f)) || !m_TempData.TryGetComponent(dynamicBuffer[l].m_Edge, out var componentData4) || (componentData4.m_Flags & TempFlags.Essential) == 0)
								{
									continue;
								}
								position = m_NodeData[connectedNode.m_Node].m_Position;
								componentData2 = m_PrefabRefData[connectedNode.m_Node];
							}
							else
							{
								if (!(connectedNode.m_Node == entity))
								{
									continue;
								}
								position = MathUtils.Position(m_CurveData[dynamicBuffer[l].m_Edge].m_Bezier, connectedNode.m_CurvePosition);
								componentData2 = m_PrefabRefData[dynamicBuffer[l].m_Edge];
							}
							if (CheckConnectionType(componentData2))
							{
								m_OverlayBuffer.DrawCircle(m_GuideLineSettingsData.m_PositiveFeedbackColor, positiveFeedbackColor, outlineWidth, (OverlayRenderSystem.StyleFlags)0, new float2(0f, 1f), position, num);
							}
						}
					}
				}
			}
		}

		private bool CheckConnectionType(PrefabRef prefabRef)
		{
			ResourceConnectionData componentData3;
			if (m_HighlightVoltage != Game.Prefabs.ElectricityConnection.Voltage.Invalid)
			{
				if (!m_ElectricityConnectionData.TryGetComponent(prefabRef.m_Prefab, out var componentData))
				{
					return false;
				}
				if (componentData.m_Voltage != m_HighlightVoltage)
				{
					return false;
				}
			}
			else if (math.any(m_HighlightWater))
			{
				if (!m_WaterPipeConnectionData.TryGetComponent(prefabRef.m_Prefab, out var componentData2))
				{
					return false;
				}
				if (!math.any((new int2(componentData2.m_FreshCapacity, componentData2.m_SewageCapacity) > 0) & m_HighlightWater))
				{
					return false;
				}
			}
			else if (m_HighResourceLine && !m_ResourceConnectionData.TryGetComponent(prefabRef.m_Prefab, out componentData3))
			{
				return false;
			}
			return true;
		}

		private void DrawMarkers(float3 lastNodePosition, float lastNodeWidth)
		{
			if (!m_MarkerNodeChunks.IsCreated)
			{
				return;
			}
			for (int i = 0; i < m_MarkerNodeChunks.Length; i++)
			{
				ArchetypeChunk archetypeChunk = m_MarkerNodeChunks[i];
				NativeArray<Game.Net.Node> nativeArray = archetypeChunk.GetNativeArray(ref m_NodeType);
				NativeArray<PrefabRef> nativeArray2 = archetypeChunk.GetNativeArray(ref m_PrefabRefType);
				BufferAccessor<ConnectedEdge> bufferAccessor = archetypeChunk.GetBufferAccessor(ref m_ConnectedEdgeType);
				for (int j = 0; j < nativeArray.Length; j++)
				{
					Game.Net.Node node = nativeArray[j];
					PrefabRef prefabRef = nativeArray2[j];
					if (bufferAccessor[j].Length == 0 && CheckConnectionType(prefabRef))
					{
						NetGeometryData componentData;
						if (node.m_Position.xz.Equals(lastNodePosition.xz))
						{
							UnityEngine.Color positiveFeedbackColor = m_GuideLineSettingsData.m_PositiveFeedbackColor;
							positiveFeedbackColor.a *= 0.1f;
							float num = lastNodeWidth + 12f;
							float outlineWidth = (math.sqrt(num + 1f) - 1f) * 0.2f;
							m_OverlayBuffer.DrawCircle(m_GuideLineSettingsData.m_PositiveFeedbackColor, positiveFeedbackColor, outlineWidth, (OverlayRenderSystem.StyleFlags)0, new float2(0f, 1f), node.m_Position, num);
						}
						else if (m_NetGeometryData.TryGetComponent(prefabRef.m_Prefab, out componentData))
						{
							UnityEngine.Color mediumPriorityColor = m_GuideLineSettingsData.m_MediumPriorityColor;
							mediumPriorityColor.a *= 0.1f;
							float defaultWidth = componentData.m_DefaultWidth;
							float outlineWidth2 = (math.sqrt(defaultWidth + 1f) - 1f) * 0.3f;
							m_OverlayBuffer.DrawCircle(m_GuideLineSettingsData.m_MediumPriorityColor, mediumPriorityColor, outlineWidth2, (OverlayRenderSystem.StyleFlags)0, new float2(0f, 1f), node.m_Position, defaultWidth);
						}
					}
				}
			}
		}

		private void DrawZones()
		{
			for (int i = 0; i < m_DefinitionChunks.Length; i++)
			{
				ArchetypeChunk archetypeChunk = m_DefinitionChunks[i];
				NativeArray<CreationDefinition> nativeArray = archetypeChunk.GetNativeArray(ref m_CreationDefinitionType);
				NativeArray<NetCourse> nativeArray2 = archetypeChunk.GetNativeArray(ref m_NetCourseType);
				for (int j = 0; j < nativeArray2.Length; j++)
				{
					NetCourse netCourse = nativeArray2[j];
					CreationDefinition creationDefinition = nativeArray[j];
					if ((creationDefinition.m_Flags & (CreationFlags.Permanent | CreationFlags.Upgrade)) != 0 || !m_RoadData.HasComponent(creationDefinition.m_Prefab) || !m_NetGeometryData.HasComponent(creationDefinition.m_Prefab))
					{
						continue;
					}
					NetGeometryData netGeometryData = m_NetGeometryData[creationDefinition.m_Prefab];
					if (m_RoadData[creationDefinition.m_Prefab].m_ZoneBlockPrefab == Entity.Null)
					{
						continue;
					}
					float2 @float = math.max(math.max(math.cmin(netCourse.m_StartPosition.m_Elevation), math.cmin(netCourse.m_EndPosition.m_Elevation)), netCourse.m_Elevation);
					float2 float2 = math.min(math.min(math.cmax(netCourse.m_StartPosition.m_Elevation), math.cmax(netCourse.m_EndPosition.m_Elevation)), netCourse.m_Elevation);
					bool2 x = (@float < netGeometryData.m_ElevationLimit) & (float2 > 0f - netGeometryData.m_ElevationLimit);
					x.x &= (netCourse.m_StartPosition.m_Flags & CoursePosFlags.IsLeft) != 0;
					x.y &= (netCourse.m_StartPosition.m_Flags & CoursePosFlags.IsRight) != 0;
					if (!math.any(x))
					{
						continue;
					}
					float num = ((float)ZoneUtils.GetCellWidth(netGeometryData.m_DefaultWidth) * 0.5f + 6f) * 8f - 1f;
					bool num2 = (netCourse.m_StartPosition.m_Flags & (CoursePosFlags.IsFirst | CoursePosFlags.IsLast)) != 0;
					bool flag = (netCourse.m_EndPosition.m_Flags & (CoursePosFlags.IsFirst | CoursePosFlags.IsLast)) != 0;
					bool flag2 = netCourse.m_Length > 0.1f;
					if (num2)
					{
						if ((netCourse.m_StartPosition.m_Flags & CoursePosFlags.IsGrid) != 0)
						{
							if ((netCourse.m_StartPosition.m_Flags & (CoursePosFlags.IsFirst | CoursePosFlags.IsParallel)) == CoursePosFlags.IsFirst || (netCourse.m_StartPosition.m_Flags & (CoursePosFlags.IsLast | CoursePosFlags.IsParallel)) == (CoursePosFlags.IsLast | CoursePosFlags.IsParallel))
							{
								DrawZoneCircle(netCourse.m_StartPosition, num, start: true, x.x, x.y);
							}
						}
						else
						{
							DrawZoneCircle(netCourse.m_StartPosition, num, fullStart: true, !flag2, x.x, x.y);
						}
					}
					if (!flag2)
					{
						continue;
					}
					if (flag)
					{
						if ((netCourse.m_EndPosition.m_Flags & CoursePosFlags.IsGrid) != 0)
						{
							if ((netCourse.m_EndPosition.m_Flags & (CoursePosFlags.IsFirst | CoursePosFlags.IsParallel)) == CoursePosFlags.IsFirst || (netCourse.m_EndPosition.m_Flags & (CoursePosFlags.IsLast | CoursePosFlags.IsParallel)) == (CoursePosFlags.IsLast | CoursePosFlags.IsParallel))
							{
								DrawZoneCircle(netCourse.m_EndPosition, num, start: false, x.x, x.y);
							}
						}
						else
						{
							DrawZoneCircle(netCourse.m_EndPosition, num, fullStart: false, fullEnd: true, x.x, x.y);
						}
					}
					float2 float3 = new float2(netCourse.m_StartPosition.m_CourseDelta, netCourse.m_EndPosition.m_CourseDelta);
					Bezier4x3 source = MathUtils.Cut(netCourse.m_Curve, new float2(float3.x, math.lerp(float3.x, float3.y, 0.5f)));
					Bezier4x3 source2 = MathUtils.Cut(netCourse.m_Curve, new float2(math.lerp(float3.x, float3.y, 0.5f), float3.y));
					if (x.x)
					{
						if (GetOffsetCurve(source, num, out var result))
						{
							m_OverlayBuffer.DrawCurve(m_GuideLineSettingsData.m_LowPriorityColor, m_GuideLineSettingsData.m_LowPriorityColor, 0f, OverlayRenderSystem.StyleFlags.Projected, result, 2f);
						}
						if (GetOffsetCurve(source2, num, out var result2))
						{
							m_OverlayBuffer.DrawCurve(m_GuideLineSettingsData.m_LowPriorityColor, m_GuideLineSettingsData.m_LowPriorityColor, 0f, OverlayRenderSystem.StyleFlags.Projected, result2, 2f);
						}
					}
					if (x.y)
					{
						if (GetOffsetCurve(source, 0f - num, out var result3))
						{
							m_OverlayBuffer.DrawCurve(m_GuideLineSettingsData.m_LowPriorityColor, m_GuideLineSettingsData.m_LowPriorityColor, 0f, OverlayRenderSystem.StyleFlags.Projected, result3, 2f);
						}
						if (GetOffsetCurve(source2, 0f - num, out var result4))
						{
							m_OverlayBuffer.DrawCurve(m_GuideLineSettingsData.m_LowPriorityColor, m_GuideLineSettingsData.m_LowPriorityColor, 0f, OverlayRenderSystem.StyleFlags.Projected, result4, 2f);
						}
					}
				}
			}
		}

		private void DrawZoneCircle(CoursePos coursePos, float offset, bool start, bool left, bool right)
		{
			Bezier4x3 curve = NetUtils.CircleCurve(coursePos.m_Position, coursePos.m_Rotation, 0f - offset, 0f - offset);
			Bezier4x3 curve2 = NetUtils.CircleCurve(coursePos.m_Position, coursePos.m_Rotation, offset, 0f - offset);
			Bezier4x3 curve3 = NetUtils.CircleCurve(coursePos.m_Position, coursePos.m_Rotation, offset, offset);
			Bezier4x3 curve4 = NetUtils.CircleCurve(coursePos.m_Position, coursePos.m_Rotation, 0f - offset, offset);
			if (start)
			{
				if (left)
				{
					m_OverlayBuffer.DrawCurve(m_GuideLineSettingsData.m_LowPriorityColor, m_GuideLineSettingsData.m_LowPriorityColor, 0f, OverlayRenderSystem.StyleFlags.Projected, curve, 2f);
				}
				if (right)
				{
					m_OverlayBuffer.DrawCurve(m_GuideLineSettingsData.m_LowPriorityColor, m_GuideLineSettingsData.m_LowPriorityColor, 0f, OverlayRenderSystem.StyleFlags.Projected, curve2, 2f);
				}
			}
			else
			{
				if (right)
				{
					m_OverlayBuffer.DrawCurve(m_GuideLineSettingsData.m_LowPriorityColor, m_GuideLineSettingsData.m_LowPriorityColor, 0f, OverlayRenderSystem.StyleFlags.Projected, curve3, 2f);
				}
				if (left)
				{
					m_OverlayBuffer.DrawCurve(m_GuideLineSettingsData.m_LowPriorityColor, m_GuideLineSettingsData.m_LowPriorityColor, 0f, OverlayRenderSystem.StyleFlags.Projected, curve4, 2f);
				}
			}
		}

		private void DrawZoneCircle(CoursePos coursePos, float offset, bool fullStart, bool fullEnd, bool left, bool right)
		{
			Bezier4x3 curve = NetUtils.CircleCurve(coursePos.m_Position, coursePos.m_Rotation, 0f - offset, 0f - offset);
			Bezier4x3 curve2 = NetUtils.CircleCurve(coursePos.m_Position, coursePos.m_Rotation, offset, 0f - offset);
			Bezier4x3 curve3 = NetUtils.CircleCurve(coursePos.m_Position, coursePos.m_Rotation, offset, offset);
			Bezier4x3 curve4 = NetUtils.CircleCurve(coursePos.m_Position, coursePos.m_Rotation, 0f - offset, offset);
			if (fullStart)
			{
				if (left)
				{
					m_OverlayBuffer.DrawCurve(m_GuideLineSettingsData.m_LowPriorityColor, m_GuideLineSettingsData.m_LowPriorityColor, 0f, OverlayRenderSystem.StyleFlags.Projected, curve, 2f);
				}
				if (right)
				{
					m_OverlayBuffer.DrawCurve(m_GuideLineSettingsData.m_LowPriorityColor, m_GuideLineSettingsData.m_LowPriorityColor, 0f, OverlayRenderSystem.StyleFlags.Projected, curve2, 2f);
				}
			}
			else
			{
				if (left)
				{
					curve = MathUtils.Cut(curve, new float2(0.25f, 1f));
					m_OverlayBuffer.DrawCurve(m_GuideLineSettingsData.m_VeryLowPriorityColor, m_GuideLineSettingsData.m_VeryLowPriorityColor, 0f, OverlayRenderSystem.StyleFlags.Projected, curve, 2f);
				}
				if (right)
				{
					curve2 = MathUtils.Cut(curve2, new float2(0.25f, 1f));
					m_OverlayBuffer.DrawCurve(m_GuideLineSettingsData.m_VeryLowPriorityColor, m_GuideLineSettingsData.m_VeryLowPriorityColor, 0f, OverlayRenderSystem.StyleFlags.Projected, curve2, 2f);
				}
			}
			if (fullEnd)
			{
				if (right)
				{
					m_OverlayBuffer.DrawCurve(m_GuideLineSettingsData.m_LowPriorityColor, m_GuideLineSettingsData.m_LowPriorityColor, 0f, OverlayRenderSystem.StyleFlags.Projected, curve3, 2f);
				}
				if (left)
				{
					m_OverlayBuffer.DrawCurve(m_GuideLineSettingsData.m_LowPriorityColor, m_GuideLineSettingsData.m_LowPriorityColor, 0f, OverlayRenderSystem.StyleFlags.Projected, curve4, 2f);
				}
				return;
			}
			if (right)
			{
				curve3 = MathUtils.Cut(curve3, new float2(0.25f, 1f));
				m_OverlayBuffer.DrawCurve(m_GuideLineSettingsData.m_VeryLowPriorityColor, m_GuideLineSettingsData.m_VeryLowPriorityColor, 0f, OverlayRenderSystem.StyleFlags.Projected, curve3, 2f);
			}
			if (left)
			{
				curve4 = MathUtils.Cut(curve4, new float2(0.25f, 1f));
				m_OverlayBuffer.DrawCurve(m_GuideLineSettingsData.m_VeryLowPriorityColor, m_GuideLineSettingsData.m_VeryLowPriorityColor, 0f, OverlayRenderSystem.StyleFlags.Projected, curve4, 2f);
			}
		}

		private bool GetOffsetCurve(Bezier4x3 source, float offset, out Bezier4x3 result)
		{
			result = NetUtils.OffsetCurveLeftSmooth(source, offset);
			return math.dot(source.d.xz - source.a.xz, result.d.xz - result.a.xz) > 0f;
		}

		private void DrawSnapLines()
		{
			float num = 1f;
			if (m_PlaceableNetData.HasComponent(m_Prefab))
			{
				num = math.min(m_PlaceableNetData[m_Prefab].m_SnapDistance, 16f);
			}
			float num2 = num * 0.125f;
			float num3 = num * 4f;
			if (m_Mode == NetToolSystem.Mode.Replace || m_ControlPoints.Length < 1)
			{
				return;
			}
			ControlPoint controlPoint = m_ControlPoints[m_ControlPoints.Length - 1];
			NativeList<SnapDir> nativeList = new NativeList<SnapDir>(4, Allocator.Temp);
			bool flag = false;
			for (int i = 0; i < m_SnapLines.Length; i++)
			{
				SnapLine snapLine = m_SnapLines[i];
				if ((snapLine.m_Flags & SnapLineFlags.Hidden) != 0 || !(NetUtils.ExtendedDistance(snapLine.m_Curve.xz, controlPoint.m_Position.xz, out var t) < 0.1f))
				{
					continue;
				}
				NetUtils.ExtendedPositionAndTangent(snapLine.m_Curve, t, out var position, out var tangent);
				tangent = MathUtils.Normalize(tangent, tangent.xz);
				position.y = controlPoint.m_Position.y;
				float3 value = position - snapLine.m_Curve.a;
				SnapDir value2 = new SnapDir
				{
					m_Direction = tangent.xz,
					m_Height = MathUtils.Normalize(value, value.xz).y
				};
				if ((snapLine.m_Flags & SnapLineFlags.GuideLine) != 0)
				{
					float y = math.dot(tangent.xz, value.xz);
					value2.m_Factor.x = math.max(0f, y);
					value2.m_Factor.y = 1000000f;
					flag = true;
				}
				else if ((snapLine.m_Flags & SnapLineFlags.Secondary) != 0)
				{
					value2.m_Factor = 1000000f;
				}
				else
				{
					flag = true;
				}
				int num4 = 0;
				while (true)
				{
					if (num4 < nativeList.Length)
					{
						SnapDir value3 = nativeList[num4];
						switch (Mathf.RoundToInt(math.degrees(math.acos(math.clamp(math.dot(value2.m_Direction, value3.m_Direction), -1f, 1f)))))
						{
						case 0:
							if (value2.m_Factor.x < value3.m_Factor.x)
							{
								value3.m_Factor.x = value2.m_Factor.x;
								value3.m_Height.x = value2.m_Height.x;
							}
							if (value2.m_Factor.y < value3.m_Factor.y)
							{
								value3.m_Factor.y = value2.m_Factor.y;
								value3.m_Height.y = value2.m_Height.y;
							}
							nativeList[num4] = value3;
							break;
						case 180:
							if (value2.m_Factor.y < value3.m_Factor.x)
							{
								value3.m_Factor.x = value2.m_Factor.y;
								value3.m_Height.x = 0f - value2.m_Height.y;
							}
							if (value2.m_Factor.x < value3.m_Factor.y)
							{
								value3.m_Factor.y = value2.m_Factor.x;
								value3.m_Height.y = 0f - value2.m_Height.x;
							}
							nativeList[num4] = value3;
							break;
						default:
							goto IL_0370;
						}
					}
					else
					{
						nativeList.Add(in value2);
					}
					break;
					IL_0370:
					num4++;
				}
			}
			for (int j = 0; j < nativeList.Length; j++)
			{
				SnapDir snapDir = nativeList[j];
				if (!flag || !math.all(snapDir.m_Factor == 1000000f))
				{
					float3 @float = new float3(snapDir.m_Direction.x, 0f, snapDir.m_Direction.y);
					Line3.Segment line = new Line3.Segment(controlPoint.m_Position - @float * num3, controlPoint.m_Position + @float * num3);
					m_OverlayBuffer.DrawLine(m_GuideLineSettingsData.m_HighPriorityColor, line, num2);
					float2 float2 = math.min(num2 / math.abs(snapDir.m_Height), num3);
					float2 = MathUtils.Snap(float2 - snapDir.m_Factor, num2 * 4f) + snapDir.m_Factor;
					if (snapDir.m_Factor.x > float2.x + num2 * 3f && snapDir.m_Factor.x < 1000000f)
					{
						float3 float3 = new float3(snapDir.m_Direction.x, snapDir.m_Height.x, snapDir.m_Direction.y);
						Line3.Segment line2 = new Line3.Segment(controlPoint.m_Position - float3 * (snapDir.m_Factor.x + num2), controlPoint.m_Position - float3 * (float2.x + num2));
						m_OverlayBuffer.DrawDashedLine(m_GuideLineSettingsData.m_HighPriorityColor, line2, num2, num2 * 2f, num2 * 2f);
					}
					if (snapDir.m_Factor.y > float2.y + num2 * 3f && snapDir.m_Factor.y < 1000000f)
					{
						float3 float4 = new float3(snapDir.m_Direction.x, snapDir.m_Height.y, snapDir.m_Direction.y);
						Line3.Segment line3 = new Line3.Segment(controlPoint.m_Position + float4 * (snapDir.m_Factor.y + num2), controlPoint.m_Position + float4 * (float2.y + num2));
						m_OverlayBuffer.DrawDashedLine(m_GuideLineSettingsData.m_HighPriorityColor, line3, num2, num2 * 2f, num2 * 2f);
					}
				}
			}
			nativeList.Dispose();
		}

		private void DrawControlPoints()
		{
			int angleIndex = 0;
			Line3.Segment prevLine = default(Line3.Segment);
			float3 prevPoint = -1000000f;
			float num = 1f;
			if (m_PlaceableNetData.HasComponent(m_Prefab))
			{
				num = math.min(m_PlaceableNetData[m_Prefab].m_SnapDistance, 16f);
			}
			float num2 = num * 0.125f;
			float num3 = num * 4f;
			if (m_Mode != NetToolSystem.Mode.Replace && m_ControlPoints.Length >= 2)
			{
				Line3.Segment line = new Line3.Segment(m_ControlPoints[0].m_Position, m_ControlPoints[1].m_Position);
				float num4 = MathUtils.Length(line.xz);
				if (num4 > num2 * 7f)
				{
					float2 @float = (line.b.xz - line.a.xz) / num4;
					float2 leftDir = default(float2);
					float2 rightDir = default(float2);
					float2 leftDir2 = default(float2);
					float2 rightDir2 = default(float2);
					int bestLeft = 181;
					int bestRight = 181;
					int bestLeft2 = 181;
					int bestRight2 = 181;
					for (int i = 0; i < m_DefinitionChunks.Length; i++)
					{
						ArchetypeChunk archetypeChunk = m_DefinitionChunks[i];
						NativeArray<CreationDefinition> nativeArray = archetypeChunk.GetNativeArray(ref m_CreationDefinitionType);
						NativeArray<NetCourse> nativeArray2 = archetypeChunk.GetNativeArray(ref m_NetCourseType);
						for (int j = 0; j < nativeArray2.Length; j++)
						{
							CreationDefinition creationDefinition = nativeArray[j];
							NetCourse netCourse = nativeArray2[j];
							if ((creationDefinition.m_Flags & CreationFlags.Permanent) != 0 || (netCourse.m_StartPosition.m_Flags & (CoursePosFlags.IsFirst | CoursePosFlags.IsParallel)) != CoursePosFlags.IsFirst)
							{
								continue;
							}
							if (m_ConnectedEdges.HasBuffer(netCourse.m_StartPosition.m_Entity))
							{
								DynamicBuffer<ConnectedEdge> dynamicBuffer = m_ConnectedEdges[netCourse.m_StartPosition.m_Entity];
								for (int k = 0; k < dynamicBuffer.Length; k++)
								{
									Entity edge = dynamicBuffer[k].m_Edge;
									Edge edge2 = m_EdgeData[edge];
									Curve curve = m_CurveData[edge];
									if (edge2.m_Start == netCourse.m_StartPosition.m_Entity)
									{
										CheckDirection(@float, MathUtils.StartTangent(curve.m_Bezier).xz, ref leftDir, ref rightDir, ref bestLeft, ref bestRight, ref leftDir2, ref rightDir2, ref bestLeft2, ref bestRight2);
									}
									else if (edge2.m_End == netCourse.m_StartPosition.m_Entity)
									{
										CheckDirection(@float, -MathUtils.EndTangent(curve.m_Bezier).xz, ref leftDir, ref rightDir, ref bestLeft, ref bestRight, ref leftDir2, ref rightDir2, ref bestLeft2, ref bestRight2);
									}
								}
							}
							else if (m_CurveData.HasComponent(netCourse.m_StartPosition.m_Entity))
							{
								float3 float2 = MathUtils.Tangent(m_CurveData[netCourse.m_StartPosition.m_Entity].m_Bezier, netCourse.m_StartPosition.m_SplitPosition);
								CheckDirection(@float, float2.xz, ref leftDir, ref rightDir, ref bestLeft, ref bestRight, ref leftDir2, ref rightDir2, ref bestLeft2, ref bestRight2);
								CheckDirection(@float, -float2.xz, ref leftDir, ref rightDir, ref bestLeft, ref bestRight, ref leftDir2, ref rightDir2, ref bestLeft2, ref bestRight2);
							}
						}
					}
					if (bestLeft > 180 && bestRight > 180)
					{
						float2 float3 = default(float2);
						if (!m_ControlPoints[0].m_Direction.Equals(default(float2)))
						{
							float3 = m_ControlPoints[0].m_Direction;
						}
						else if (m_TransformData.HasComponent(m_ControlPoints[0].m_OriginalEntity))
						{
							float3 = math.forward(m_TransformData[m_ControlPoints[0].m_OriginalEntity].m_Rotation).xz;
						}
						if (!float3.Equals(default(float2)))
						{
							CheckDirection(@float, float3, ref leftDir, ref rightDir, ref bestLeft, ref bestRight, ref leftDir2, ref rightDir2, ref bestLeft2, ref bestRight2);
							CheckDirection(@float, MathUtils.Right(float3), ref leftDir, ref rightDir, ref bestLeft, ref bestRight, ref leftDir2, ref rightDir2, ref bestLeft2, ref bestRight2);
							CheckDirection(@float, -float3, ref leftDir, ref rightDir, ref bestLeft, ref bestRight, ref leftDir2, ref rightDir2, ref bestLeft2, ref bestRight2);
							CheckDirection(@float, MathUtils.Left(float3), ref leftDir, ref rightDir, ref bestLeft, ref bestRight, ref leftDir2, ref rightDir2, ref bestLeft2, ref bestRight2);
						}
					}
					bool value = bestRight < bestLeft;
					if (bestLeft == bestRight && m_AngleSides.Length > angleIndex)
					{
						value = m_AngleSides[angleIndex];
					}
					if (bestLeft == 180 && bestRight == 180)
					{
						if (value)
						{
							bestLeft = 181;
						}
						else
						{
							bestRight = 181;
						}
					}
					else
					{
						if (bestLeft2 <= 180 && bestRight2 <= 180)
						{
							if (bestLeft2 < bestRight2 || (bestLeft2 == bestRight2 && value))
							{
								bestRight2 = 181;
							}
							else
							{
								bestLeft2 = 181;
							}
						}
						if (bestLeft2 <= 180)
						{
							leftDir = leftDir2;
							bestLeft = bestLeft2;
						}
						else if (bestRight2 <= 180)
						{
							rightDir = rightDir2;
							bestRight = bestRight2;
						}
					}
					if (bestLeft <= 180)
					{
						Line3.Segment segment = new Line3.Segment(line.a, line.a);
						segment.a.xz += leftDir * math.min(num4, num3);
						m_OverlayBuffer.DrawLine(m_GuideLineSettingsData.m_HighPriorityColor, segment, num2);
						GuideLinesSystem.DrawAngleIndicator(m_OverlayBuffer, m_Tooltips, m_GuideLineSettingsData, segment, line, -leftDir, -@float, math.min(num4, num3) * 0.5f, num2, bestLeft, angleSide: false);
					}
					if (bestRight <= 180)
					{
						Line3.Segment segment2 = new Line3.Segment(line.a, line.a);
						segment2.a.xz += rightDir * math.min(num4, num3);
						m_OverlayBuffer.DrawLine(m_GuideLineSettingsData.m_HighPriorityColor, segment2, num2);
						GuideLinesSystem.DrawAngleIndicator(m_OverlayBuffer, m_Tooltips, m_GuideLineSettingsData, segment2, line, -rightDir, -@float, math.min(num4, num3) * 0.5f, num2, bestRight, angleSide: true);
					}
					if (m_AngleSides.Length > angleIndex)
					{
						m_AngleSides[angleIndex] = value;
					}
					else
					{
						while (m_AngleSides.Length <= angleIndex)
						{
							m_AngleSides.Add(in value);
						}
					}
				}
				angleIndex++;
			}
			if (m_Mode == NetToolSystem.Mode.Continuous && m_ControlPoints.Length >= 3)
			{
				ControlPoint controlPoint = m_ControlPoints[0];
				ControlPoint controlPoint2 = m_ControlPoints[m_ControlPoints.Length - 2];
				ControlPoint controlPoint3 = m_ControlPoints[m_ControlPoints.Length - 1];
				if (math.dot(controlPoint3.m_Direction, controlPoint2.m_Direction) <= -0.01f)
				{
					float3 startTangent = new float3(controlPoint2.m_Direction.x, 0f, controlPoint2.m_Direction.y);
					float3 float4 = new float3(controlPoint3.m_Direction.x, 0f, controlPoint3.m_Direction.y);
					float num5 = math.dot(math.normalizesafe(controlPoint3.m_Position.xz - controlPoint.m_Position.xz), controlPoint2.m_Direction);
					Bezier4x3 curve2;
					if (num5 <= -0.01f)
					{
						float3 endPos = controlPoint3.m_Position + float4 * num5;
						curve2 = NetUtils.FitCurve(controlPoint.m_Position, startTangent, float4, endPos);
						curve2.d = controlPoint3.m_Position;
					}
					else
					{
						curve2 = NetUtils.FitCurve(controlPoint.m_Position, startTangent, float4, controlPoint3.m_Position);
					}
					Line2 line2 = default(Line2);
					line2.a = MathUtils.Position(curve2, 0.5f).xz;
					line2.b = line2.a + MathUtils.Tangent(curve2, 0.5f).xz;
					Line2 line3 = new Line2(controlPoint.m_Position.xz, controlPoint.m_Position.xz + controlPoint2.m_Direction);
					Line2 line4 = new Line2(controlPoint3.m_Position.xz, controlPoint3.m_Position.xz - controlPoint3.m_Direction);
					ControlPoint controlPoint4 = controlPoint2;
					if (MathUtils.Intersect(line3, line2, out var t))
					{
						controlPoint2.m_Position.xz = MathUtils.Position(line3, t.x);
					}
					if (MathUtils.Intersect(line4, line2, out var t2))
					{
						controlPoint4.m_Position.xz = MathUtils.Position(line4, t2.x);
					}
					DrawControlPoint(controlPoint, num2, ref prevPoint);
					DrawControlPointLine(controlPoint, controlPoint2, num2, num3, ref angleIndex, ref prevLine);
					DrawControlPoint(controlPoint2, num2, ref prevPoint);
					DrawControlPointLine(controlPoint2, controlPoint4, num2, num3, ref angleIndex, ref prevLine);
					DrawControlPoint(controlPoint4, num2, ref prevPoint);
					DrawControlPointLine(controlPoint4, controlPoint3, num2, num3, ref angleIndex, ref prevLine);
					DrawControlPoint(controlPoint3, num2, ref prevPoint);
					return;
				}
			}
			if (m_Mode != NetToolSystem.Mode.Replace && m_ControlPoints.Length >= 3)
			{
				ControlPoint controlPoint5 = m_ControlPoints[0];
				int num6 = 0;
				for (int l = 1; l < m_ControlPoints.Length; l++)
				{
					ControlPoint controlPoint6 = m_ControlPoints[l];
					int num7 = Mathf.RoundToInt(math.distance(controlPoint5.m_Position.xz, controlPoint6.m_Position.xz));
					num6 += math.select(0, 1, num7 > 0);
					controlPoint5 = controlPoint6;
				}
				if (num6 >= 2)
				{
					controlPoint5 = m_ControlPoints[0];
					for (int m = 1; m < m_ControlPoints.Length; m++)
					{
						ControlPoint controlPoint7 = m_ControlPoints[m];
						float num8 = (float)Mathf.RoundToInt(math.distance(controlPoint5.m_Position.xz, controlPoint7.m_Position.xz) * 2f) / 2f;
						if (num8 > 0f)
						{
							m_Tooltips.Add(new TooltipInfo(TooltipType.Length, (controlPoint5.m_Position + controlPoint7.m_Position) * 0.5f, num8));
						}
						controlPoint5 = controlPoint7;
					}
				}
			}
			int num9 = 0;
			int num10 = m_ControlPoints.Length - 1;
			if (m_Mode == NetToolSystem.Mode.Replace)
			{
				num9 = 1;
				num10 = m_ControlPoints.Length - 2;
			}
			for (int n = num9; n <= num10; n++)
			{
				ControlPoint controlPoint8 = m_ControlPoints[n];
				if (n > num9)
				{
					ControlPoint point = m_ControlPoints[n - 1];
					DrawControlPointLine(point, controlPoint8, num2, num3, ref angleIndex, ref prevLine);
				}
				DrawControlPoint(controlPoint8, num2, ref prevPoint);
			}
		}

		private void DrawControlPoint(ControlPoint point, float lineWidth, ref float3 prevPoint)
		{
			if (math.distancesq(prevPoint, point.m_Position) > 0.01f)
			{
				m_OverlayBuffer.DrawCircle(m_GuideLineSettingsData.m_HighPriorityColor, point.m_Position, lineWidth * 5f);
			}
			prevPoint = point.m_Position;
		}

		private void DrawControlPointLine(ControlPoint point1, ControlPoint point2, float lineWidth, float lineLength, ref int angleIndex, ref Line3.Segment prevLine)
		{
			Line3.Segment segment = new Line3.Segment(point1.m_Position, point2.m_Position);
			float num = math.distance(point1.m_Position.xz, point2.m_Position.xz);
			if (num > lineWidth * 8f)
			{
				float3 @float = (segment.b - segment.a) * (lineWidth * 4f / num);
				Line3.Segment line = new Line3.Segment(segment.a + @float, segment.b - @float);
				m_OverlayBuffer.DrawDashedLine(m_GuideLineSettingsData.m_HighPriorityColor, line, lineWidth * 3f, lineWidth * 5f, lineWidth * 3f);
			}
			DrawAngleIndicator(prevLine, segment, lineWidth, lineLength, angleIndex++);
			prevLine = segment;
		}

		private void DrawCourses()
		{
			for (int i = 0; i < m_DefinitionChunks.Length; i++)
			{
				ArchetypeChunk archetypeChunk = m_DefinitionChunks[i];
				NativeArray<CreationDefinition> nativeArray = archetypeChunk.GetNativeArray(ref m_CreationDefinitionType);
				NativeArray<NetCourse> nativeArray2 = archetypeChunk.GetNativeArray(ref m_NetCourseType);
				for (int j = 0; j < nativeArray2.Length; j++)
				{
					NetCourse netCourse = nativeArray2[j];
					CreationDefinition creationDefinition = nativeArray[j];
					if ((creationDefinition.m_Flags & CreationFlags.Permanent) == 0 && m_NetGeometryData.TryGetComponent(creationDefinition.m_Prefab, out var componentData))
					{
						DrawNetCourse(m_OverlayBuffer, netCourse, ref m_TerrainHeightData, ref m_WaterSurfaceData, componentData, m_GuideLineSettingsData);
					}
				}
			}
		}

		private void DrawAngleIndicator(Line3.Segment line1, Line3.Segment line2, float lineWidth, float lineLength, int angleIndex)
		{
			bool value = true;
			if (m_AngleSides.Length > angleIndex)
			{
				value = m_AngleSides[angleIndex];
			}
			float num = math.distance(line1.a.xz, line1.b.xz);
			float num2 = math.distance(line2.a.xz, line2.b.xz);
			if (num > lineWidth * 7f && num2 > lineWidth * 7f)
			{
				float2 @float = (line1.b.xz - line1.a.xz) / num;
				float2 float2 = (line2.a.xz - line2.b.xz) / num2;
				float size = math.min(lineLength, math.min(num, num2)) * 0.5f;
				int num3 = Mathf.RoundToInt(math.degrees(math.acos(math.clamp(math.dot(@float, float2), -1f, 1f))));
				if (num3 < 180)
				{
					value = math.dot(MathUtils.Right(@float), float2) < 0f;
				}
				GuideLinesSystem.DrawAngleIndicator(m_OverlayBuffer, m_Tooltips, m_GuideLineSettingsData, line1, line2, @float, float2, size, lineWidth, num3, value);
			}
			if (m_AngleSides.Length > angleIndex)
			{
				m_AngleSides[angleIndex] = value;
				return;
			}
			while (m_AngleSides.Length <= angleIndex)
			{
				m_AngleSides.Add(in value);
			}
		}
	}

	private struct TypeHandle
	{
		[ReadOnly]
		public ComponentTypeHandle<CreationDefinition> __Game_Tools_CreationDefinition_RO_ComponentTypeHandle;

		[ReadOnly]
		public EntityTypeHandle __Unity_Entities_Entity_TypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<NetCourse> __Game_Tools_NetCourse_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Game.Net.Node> __Game_Net_Node_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Temp> __Game_Tools_Temp_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentTypeHandle;

		[ReadOnly]
		public BufferTypeHandle<ConnectedEdge> __Game_Net_ConnectedEdge_RO_BufferTypeHandle;

		[ReadOnly]
		public ComponentLookup<Edge> __Game_Net_Edge_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Net.Node> __Game_Net_Node_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Curve> __Game_Net_Curve_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Objects.Transform> __Game_Objects_Transform_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Temp> __Game_Tools_Temp_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<NetGeometryData> __Game_Prefabs_NetGeometryData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PlaceableNetData> __Game_Prefabs_PlaceableNetData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<RoadData> __Game_Prefabs_RoadData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<ElectricityConnectionData> __Game_Prefabs_ElectricityConnectionData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<WaterPipeConnectionData> __Game_Prefabs_WaterPipeConnectionData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<ResourceConnectionData> __Game_Prefabs_ResourceConnectionData_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<ConnectedEdge> __Game_Net_ConnectedEdge_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<ConnectedNode> __Game_Net_ConnectedNode_RO_BufferLookup;

		[ReadOnly]
		public BufferTypeHandle<WaypointDefinition> __Game_Routes_WaypointDefinition_RO_BufferTypeHandle;

		[ReadOnly]
		public ComponentLookup<Route> __Game_Routes_Route_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<RouteData> __Game_Prefabs_RouteData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentTypeHandle<Zoning> __Game_Tools_Zoning_RO_ComponentTypeHandle;

		[ReadOnly]
		public BufferTypeHandle<Game.Areas.Node> __Game_Areas_Node_RO_BufferTypeHandle;

		[ReadOnly]
		public ComponentLookup<Owner> __Game_Common_Owner_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<AreaGeometryData> __Game_Prefabs_AreaGeometryData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<LotData> __Game_Prefabs_LotData_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<Game.Areas.Node> __Game_Areas_Node_RO_BufferLookup;

		[ReadOnly]
		public ComponentTypeHandle<ObjectDefinition> __Game_Tools_ObjectDefinition_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<OwnerDefinition> __Game_Tools_OwnerDefinition_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentLookup<ObjectGeometryData> __Game_Prefabs_ObjectGeometryData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PlaceableObjectData> __Game_Prefabs_PlaceableObjectData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<ServiceUpgradeData> __Game_Prefabs_ServiceUpgradeData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<BuildingData> __Game_Prefabs_BuildingData_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<Game.Prefabs.SubArea> __Game_Prefabs_SubArea_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<Game.Prefabs.SubNet> __Game_Prefabs_SubNet_RO_BufferLookup;

		[ReadOnly]
		public ComponentTypeHandle<Game.Simulation.WaterSourceData> __Game_Simulation_WaterSourceData_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Game.Objects.Transform> __Game_Objects_Transform_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentLookup<GuideLineSettingsData> __Game_Prefabs_GuideLineSettingsData_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<WaterSourceColorElement> __Game_Prefabs_WaterSourceColorElement_RO_BufferLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Game_Tools_CreationDefinition_RO_ComponentTypeHandle = state.GetComponentTypeHandle<CreationDefinition>(isReadOnly: true);
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
			__Game_Tools_NetCourse_RO_ComponentTypeHandle = state.GetComponentTypeHandle<NetCourse>(isReadOnly: true);
			__Game_Net_Node_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Game.Net.Node>(isReadOnly: true);
			__Game_Tools_Temp_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Temp>(isReadOnly: true);
			__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PrefabRef>(isReadOnly: true);
			__Game_Net_ConnectedEdge_RO_BufferTypeHandle = state.GetBufferTypeHandle<ConnectedEdge>(isReadOnly: true);
			__Game_Net_Edge_RO_ComponentLookup = state.GetComponentLookup<Edge>(isReadOnly: true);
			__Game_Net_Node_RO_ComponentLookup = state.GetComponentLookup<Game.Net.Node>(isReadOnly: true);
			__Game_Net_Curve_RO_ComponentLookup = state.GetComponentLookup<Curve>(isReadOnly: true);
			__Game_Objects_Transform_RO_ComponentLookup = state.GetComponentLookup<Game.Objects.Transform>(isReadOnly: true);
			__Game_Tools_Temp_RO_ComponentLookup = state.GetComponentLookup<Temp>(isReadOnly: true);
			__Game_Prefabs_PrefabRef_RO_ComponentLookup = state.GetComponentLookup<PrefabRef>(isReadOnly: true);
			__Game_Prefabs_NetGeometryData_RO_ComponentLookup = state.GetComponentLookup<NetGeometryData>(isReadOnly: true);
			__Game_Prefabs_PlaceableNetData_RO_ComponentLookup = state.GetComponentLookup<PlaceableNetData>(isReadOnly: true);
			__Game_Prefabs_RoadData_RO_ComponentLookup = state.GetComponentLookup<RoadData>(isReadOnly: true);
			__Game_Prefabs_ElectricityConnectionData_RO_ComponentLookup = state.GetComponentLookup<ElectricityConnectionData>(isReadOnly: true);
			__Game_Prefabs_WaterPipeConnectionData_RO_ComponentLookup = state.GetComponentLookup<WaterPipeConnectionData>(isReadOnly: true);
			__Game_Prefabs_ResourceConnectionData_RO_ComponentLookup = state.GetComponentLookup<ResourceConnectionData>(isReadOnly: true);
			__Game_Net_ConnectedEdge_RO_BufferLookup = state.GetBufferLookup<ConnectedEdge>(isReadOnly: true);
			__Game_Net_ConnectedNode_RO_BufferLookup = state.GetBufferLookup<ConnectedNode>(isReadOnly: true);
			__Game_Routes_WaypointDefinition_RO_BufferTypeHandle = state.GetBufferTypeHandle<WaypointDefinition>(isReadOnly: true);
			__Game_Routes_Route_RO_ComponentLookup = state.GetComponentLookup<Route>(isReadOnly: true);
			__Game_Prefabs_RouteData_RO_ComponentLookup = state.GetComponentLookup<RouteData>(isReadOnly: true);
			__Game_Tools_Zoning_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Zoning>(isReadOnly: true);
			__Game_Areas_Node_RO_BufferTypeHandle = state.GetBufferTypeHandle<Game.Areas.Node>(isReadOnly: true);
			__Game_Common_Owner_RO_ComponentLookup = state.GetComponentLookup<Owner>(isReadOnly: true);
			__Game_Prefabs_AreaGeometryData_RO_ComponentLookup = state.GetComponentLookup<AreaGeometryData>(isReadOnly: true);
			__Game_Prefabs_LotData_RO_ComponentLookup = state.GetComponentLookup<LotData>(isReadOnly: true);
			__Game_Areas_Node_RO_BufferLookup = state.GetBufferLookup<Game.Areas.Node>(isReadOnly: true);
			__Game_Tools_ObjectDefinition_RO_ComponentTypeHandle = state.GetComponentTypeHandle<ObjectDefinition>(isReadOnly: true);
			__Game_Tools_OwnerDefinition_RO_ComponentTypeHandle = state.GetComponentTypeHandle<OwnerDefinition>(isReadOnly: true);
			__Game_Prefabs_ObjectGeometryData_RO_ComponentLookup = state.GetComponentLookup<ObjectGeometryData>(isReadOnly: true);
			__Game_Prefabs_PlaceableObjectData_RO_ComponentLookup = state.GetComponentLookup<PlaceableObjectData>(isReadOnly: true);
			__Game_Prefabs_ServiceUpgradeData_RO_ComponentLookup = state.GetComponentLookup<ServiceUpgradeData>(isReadOnly: true);
			__Game_Prefabs_BuildingData_RO_ComponentLookup = state.GetComponentLookup<BuildingData>(isReadOnly: true);
			__Game_Prefabs_SubArea_RO_BufferLookup = state.GetBufferLookup<Game.Prefabs.SubArea>(isReadOnly: true);
			__Game_Prefabs_SubNet_RO_BufferLookup = state.GetBufferLookup<Game.Prefabs.SubNet>(isReadOnly: true);
			__Game_Simulation_WaterSourceData_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Game.Simulation.WaterSourceData>(isReadOnly: true);
			__Game_Objects_Transform_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Game.Objects.Transform>(isReadOnly: true);
			__Game_Prefabs_GuideLineSettingsData_RO_ComponentLookup = state.GetComponentLookup<GuideLineSettingsData>(isReadOnly: true);
			__Game_Prefabs_WaterSourceColorElement_RO_BufferLookup = state.GetBufferLookup<WaterSourceColorElement>(isReadOnly: true);
		}
	}

	private EntityQuery m_DefinitionQuery;

	private EntityQuery m_MarkerNodeQuery;

	private EntityQuery m_TempNodeQuery;

	private EntityQuery m_WaterSourceQuery;

	private EntityQuery m_RenderingSettingsQuery;

	private TerrainSystem m_TerrainSystem;

	private WaterSystem m_WaterSystem;

	private ToolSystem m_ToolSystem;

	private NetToolSystem m_NetToolSystem;

	private RouteToolSystem m_RouteToolSystem;

	private ZoneToolSystem m_ZoneToolSystem;

	private SelectionToolSystem m_SelectionToolSystem;

	private AreaToolSystem m_AreaToolSystem;

	private ObjectToolSystem m_ObjectToolSystem;

	private WaterToolSystem m_WaterToolSystem;

	private OverlayRenderSystem m_OverlayRenderSystem;

	private CameraUpdateSystem m_CameraUpdateSystem;

	private PrefabSystem m_PrefabSystem;

	private ToolRaycastSystem m_ToolRaycastSystem;

	private NativeList<bool> m_AngleSides;

	private NativeList<TooltipInfo> m_Tooltips;

	private JobHandle m_TooltipDeps;

	private TypeHandle __TypeHandle;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_TerrainSystem = base.World.GetOrCreateSystemManaged<TerrainSystem>();
		m_WaterSystem = base.World.GetOrCreateSystemManaged<WaterSystem>();
		m_ToolSystem = base.World.GetOrCreateSystemManaged<ToolSystem>();
		m_NetToolSystem = base.World.GetOrCreateSystemManaged<NetToolSystem>();
		m_RouteToolSystem = base.World.GetOrCreateSystemManaged<RouteToolSystem>();
		m_ZoneToolSystem = base.World.GetOrCreateSystemManaged<ZoneToolSystem>();
		m_SelectionToolSystem = base.World.GetOrCreateSystemManaged<SelectionToolSystem>();
		m_AreaToolSystem = base.World.GetOrCreateSystemManaged<AreaToolSystem>();
		m_ObjectToolSystem = base.World.GetOrCreateSystemManaged<ObjectToolSystem>();
		m_WaterToolSystem = base.World.GetOrCreateSystemManaged<WaterToolSystem>();
		m_OverlayRenderSystem = base.World.GetOrCreateSystemManaged<OverlayRenderSystem>();
		m_CameraUpdateSystem = base.World.GetOrCreateSystemManaged<CameraUpdateSystem>();
		m_ToolRaycastSystem = base.World.GetOrCreateSystemManaged<ToolRaycastSystem>();
		m_PrefabSystem = base.World.GetOrCreateSystemManaged<PrefabSystem>();
		m_DefinitionQuery = GetEntityQuery(new EntityQueryDesc
		{
			All = new ComponentType[1] { ComponentType.ReadOnly<CreationDefinition>() },
			Any = new ComponentType[5]
			{
				ComponentType.ReadOnly<NetCourse>(),
				ComponentType.ReadOnly<WaypointDefinition>(),
				ComponentType.ReadOnly<Zoning>(),
				ComponentType.ReadOnly<Game.Areas.Node>(),
				ComponentType.ReadOnly<ObjectDefinition>()
			},
			None = new ComponentType[0]
		});
		m_TempNodeQuery = GetEntityQuery(ComponentType.ReadOnly<Game.Net.Node>(), ComponentType.ReadOnly<Temp>(), ComponentType.Exclude<Deleted>());
		m_MarkerNodeQuery = GetEntityQuery(ComponentType.ReadOnly<Game.Net.Marker>(), ComponentType.ReadOnly<Orphan>(), ComponentType.ReadOnly<Game.Net.Node>(), ComponentType.Exclude<Hidden>(), ComponentType.Exclude<Temp>(), ComponentType.Exclude<Deleted>());
		m_WaterSourceQuery = GetEntityQuery(ComponentType.ReadOnly<Game.Simulation.WaterSourceData>(), ComponentType.Exclude<PrefabRef>(), ComponentType.Exclude<Hidden>(), ComponentType.Exclude<Deleted>());
		m_RenderingSettingsQuery = GetEntityQuery(ComponentType.ReadOnly<GuideLineSettingsData>());
		m_AngleSides = new NativeList<bool>(4, Allocator.Persistent);
		m_Tooltips = new NativeList<TooltipInfo>(8, Allocator.Persistent);
	}

	[Preserve]
	protected override void OnDestroy()
	{
		m_AngleSides.Dispose();
		m_Tooltips.Dispose();
		base.OnDestroy();
	}

	public NativeList<TooltipInfo> GetTooltips(out JobHandle dependencies)
	{
		dependencies = m_TooltipDeps;
		return m_Tooltips;
	}

	[Preserve]
	protected override void OnUpdate()
	{
		m_Tooltips.Clear();
		bool flag = (m_ToolRaycastSystem.raycastFlags & (RaycastFlags.DebugDisable | RaycastFlags.UIDisable)) != 0;
		if (m_ToolSystem.activeTool == m_NetToolSystem)
		{
			if (flag)
			{
				return;
			}
			JobHandle outJobHandle;
			JobHandle dependencies;
			JobHandle dependencies2;
			JobHandle deps;
			JobHandle dependencies3;
			NetToolGuideLinesJob jobData = new NetToolGuideLinesJob
			{
				m_CreationDefinitionType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Tools_CreationDefinition_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
				m_NetCourseType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Tools_NetCourse_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_NodeType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Net_Node_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_TempType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Tools_Temp_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_PrefabRefType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_ConnectedEdgeType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Net_ConnectedEdge_RO_BufferTypeHandle, ref base.CheckedStateRef),
				m_EdgeData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Edge_RO_ComponentLookup, ref base.CheckedStateRef),
				m_NodeData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Node_RO_ComponentLookup, ref base.CheckedStateRef),
				m_CurveData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Curve_RO_ComponentLookup, ref base.CheckedStateRef),
				m_TransformData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Transform_RO_ComponentLookup, ref base.CheckedStateRef),
				m_TempData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Tools_Temp_RO_ComponentLookup, ref base.CheckedStateRef),
				m_PrefabRefData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup, ref base.CheckedStateRef),
				m_NetGeometryData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_NetGeometryData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_PlaceableNetData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PlaceableNetData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_RoadData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_RoadData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_ElectricityConnectionData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_ElectricityConnectionData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_WaterPipeConnectionData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_WaterPipeConnectionData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_ResourceConnectionData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_ResourceConnectionData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_ConnectedEdges = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Net_ConnectedEdge_RO_BufferLookup, ref base.CheckedStateRef),
				m_ConnectedNodes = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Net_ConnectedNode_RO_BufferLookup, ref base.CheckedStateRef),
				m_DefinitionChunks = m_DefinitionQuery.ToArchetypeChunkListAsync(Allocator.TempJob, out outJobHandle),
				m_ControlPoints = m_NetToolSystem.GetControlPoints(out dependencies),
				m_SnapLines = m_NetToolSystem.GetSnapLines(out dependencies2),
				m_TerrainHeightData = m_TerrainSystem.GetHeightData(),
				m_WaterSurfaceData = m_WaterSystem.GetSurfaceData(out deps),
				m_Mode = m_NetToolSystem.actualMode,
				m_HighlightVoltage = Game.Prefabs.ElectricityConnection.Voltage.Invalid,
				m_HighlightWater = false,
				m_HighResourceLine = false,
				m_Prefab = ((m_NetToolSystem.prefab != null) ? m_PrefabSystem.GetEntity(m_NetToolSystem.prefab) : Entity.Null),
				m_GuideLineSettingsData = m_RenderingSettingsQuery.GetSingleton<GuideLineSettingsData>(),
				m_AngleSides = m_AngleSides,
				m_Tooltips = m_Tooltips,
				m_OverlayBuffer = m_OverlayRenderSystem.GetBuffer(out dependencies3)
			};
			JobHandle jobHandle = JobUtils.CombineDependencies(dependencies, dependencies2, deps, dependencies3);
			if (m_NetToolSystem.prefab is PowerLinePrefab)
			{
				if (m_NetToolSystem.prefab.TryGet<Game.Prefabs.ElectricityConnection>(out var component))
				{
					jobData.m_MarkerNodeChunks = m_MarkerNodeQuery.ToArchetypeChunkListAsync(Allocator.TempJob, out var outJobHandle2);
					jobData.m_TempNodeChunks = m_TempNodeQuery.ToArchetypeChunkListAsync(Allocator.TempJob, out var outJobHandle3);
					jobData.m_HighlightVoltage = component.m_Voltage;
					jobHandle = JobHandle.CombineDependencies(jobHandle, outJobHandle2, outJobHandle3);
				}
			}
			else if (m_NetToolSystem.prefab is PipelinePrefab)
			{
				jobData.m_MarkerNodeChunks = m_MarkerNodeQuery.ToArchetypeChunkListAsync(Allocator.TempJob, out var outJobHandle4);
				jobData.m_TempNodeChunks = m_TempNodeQuery.ToArchetypeChunkListAsync(Allocator.TempJob, out var outJobHandle5);
				jobHandle = JobHandle.CombineDependencies(jobHandle, outJobHandle4, outJobHandle5);
				if (m_NetToolSystem.prefab.TryGet<Game.Prefabs.WaterPipeConnection>(out var component2))
				{
					jobData.m_HighlightWater.x = component2.m_FreshCapacity > 0;
					jobData.m_HighlightWater.y = component2.m_SewageCapacity > 0;
				}
				else
				{
					jobData.m_HighResourceLine = true;
				}
			}
			JobHandle jobHandle2 = IJobExtensions.Schedule(jobData, JobHandle.CombineDependencies(base.Dependency, outJobHandle, jobHandle));
			if (jobData.m_MarkerNodeChunks.IsCreated)
			{
				jobData.m_MarkerNodeChunks.Dispose(jobHandle2);
			}
			if (jobData.m_TempNodeChunks.IsCreated)
			{
				jobData.m_TempNodeChunks.Dispose(jobHandle2);
			}
			jobData.m_DefinitionChunks.Dispose(jobHandle2);
			m_TerrainSystem.AddCPUHeightReader(jobHandle2);
			m_WaterSystem.AddSurfaceReader(jobHandle2);
			m_OverlayRenderSystem.AddBufferWriter(jobHandle2);
			m_TooltipDeps = jobHandle2;
			base.Dependency = jobHandle2;
		}
		else if (m_ToolSystem.activeTool == m_RouteToolSystem)
		{
			if (!flag)
			{
				JobHandle outJobHandle6;
				JobHandle dependencies4;
				JobHandle dependencies5;
				RouteToolGuideLinesJob jobData2 = new RouteToolGuideLinesJob
				{
					m_CreationDefinitionType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Tools_CreationDefinition_RO_ComponentTypeHandle, ref base.CheckedStateRef),
					m_WaypointDefinitionType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Routes_WaypointDefinition_RO_BufferTypeHandle, ref base.CheckedStateRef),
					m_RouteData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Routes_Route_RO_ComponentLookup, ref base.CheckedStateRef),
					m_PrefabRefData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup, ref base.CheckedStateRef),
					m_PrefabRouteData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_RouteData_RO_ComponentLookup, ref base.CheckedStateRef),
					m_DefinitionChunks = m_DefinitionQuery.ToArchetypeChunkListAsync(Allocator.TempJob, out outJobHandle6),
					m_ControlPoints = m_RouteToolSystem.GetControlPoints(out dependencies4),
					m_MoveStartPosition = m_RouteToolSystem.moveStartPosition,
					m_State = m_RouteToolSystem.state,
					m_GuideLineSettingsData = m_RenderingSettingsQuery.GetSingleton<GuideLineSettingsData>(),
					m_OverlayBuffer = m_OverlayRenderSystem.GetBuffer(out dependencies5)
				};
				JobHandle job = JobHandle.CombineDependencies(dependencies4, dependencies5);
				JobHandle jobHandle3 = IJobExtensions.Schedule(jobData2, JobHandle.CombineDependencies(base.Dependency, outJobHandle6, job));
				jobData2.m_DefinitionChunks.Dispose(jobHandle3);
				m_OverlayRenderSystem.AddBufferWriter(jobHandle3);
				base.Dependency = jobHandle3;
			}
		}
		else if (m_ToolSystem.activeTool == m_ZoneToolSystem)
		{
			if (!flag)
			{
				JobHandle outJobHandle7;
				JobHandle dependencies6;
				ZoneToolGuideLinesJob jobData3 = new ZoneToolGuideLinesJob
				{
					m_CreationDefinitionType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Tools_CreationDefinition_RO_ComponentTypeHandle, ref base.CheckedStateRef),
					m_ZoningType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Tools_Zoning_RO_ComponentTypeHandle, ref base.CheckedStateRef),
					m_DefinitionChunks = m_DefinitionQuery.ToArchetypeChunkListAsync(Allocator.TempJob, out outJobHandle7),
					m_GuideLineSettingsData = m_RenderingSettingsQuery.GetSingleton<GuideLineSettingsData>(),
					m_OverlayBuffer = m_OverlayRenderSystem.GetBuffer(out dependencies6)
				};
				JobHandle jobHandle4 = IJobExtensions.Schedule(jobData3, JobHandle.CombineDependencies(base.Dependency, outJobHandle7, dependencies6));
				jobData3.m_DefinitionChunks.Dispose(jobHandle4);
				m_OverlayRenderSystem.AddBufferWriter(jobHandle4);
				base.Dependency = jobHandle4;
			}
		}
		else if (m_ToolSystem.activeTool == m_SelectionToolSystem)
		{
			if (!flag)
			{
				Quad3 quad;
				bool selectionQuad = m_SelectionToolSystem.GetSelectionQuad(out quad);
				JobHandle dependencies7;
				JobHandle jobHandle5 = IJobExtensions.Schedule(new SelectionToolGuideLinesJob
				{
					m_State = m_SelectionToolSystem.state,
					m_SelectionType = m_SelectionToolSystem.selectionType,
					m_SelectionQuadIsValid = selectionQuad,
					m_SelectionQuad = quad,
					m_GuideLineSettingsData = m_RenderingSettingsQuery.GetSingleton<GuideLineSettingsData>(),
					m_OverlayBuffer = m_OverlayRenderSystem.GetBuffer(out dependencies7)
				}, JobHandle.CombineDependencies(base.Dependency, dependencies7));
				m_OverlayRenderSystem.AddBufferWriter(jobHandle5);
				base.Dependency = jobHandle5;
			}
		}
		else if (m_ToolSystem.activeTool == m_AreaToolSystem)
		{
			if (!flag)
			{
				JobHandle outJobHandle8;
				NativeList<ControlPoint> moveStartPositions;
				JobHandle dependencies8;
				JobHandle dependencies9;
				AreaToolGuideLinesJob jobData4 = new AreaToolGuideLinesJob
				{
					m_CreationDefinitionType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Tools_CreationDefinition_RO_ComponentTypeHandle, ref base.CheckedStateRef),
					m_NodeType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Areas_Node_RO_BufferTypeHandle, ref base.CheckedStateRef),
					m_OwnerData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Common_Owner_RO_ComponentLookup, ref base.CheckedStateRef),
					m_TransformData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Transform_RO_ComponentLookup, ref base.CheckedStateRef),
					m_PrefabRefData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup, ref base.CheckedStateRef),
					m_PrefabGeometryData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_AreaGeometryData_RO_ComponentLookup, ref base.CheckedStateRef),
					m_PrefabLotData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_LotData_RO_ComponentLookup, ref base.CheckedStateRef),
					m_Nodes = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Areas_Node_RO_BufferLookup, ref base.CheckedStateRef),
					m_DefinitionChunks = m_DefinitionQuery.ToArchetypeChunkListAsync(Allocator.TempJob, out outJobHandle8),
					m_ControlPoints = m_AreaToolSystem.GetControlPoints(out moveStartPositions, out dependencies8),
					m_MoveStartPositions = moveStartPositions,
					m_State = m_AreaToolSystem.state,
					m_Prefab = ((m_AreaToolSystem.prefab != null) ? m_PrefabSystem.GetEntity(m_AreaToolSystem.prefab) : Entity.Null),
					m_GuideLineSettingsData = m_RenderingSettingsQuery.GetSingleton<GuideLineSettingsData>(),
					m_AngleSides = m_AngleSides,
					m_Tooltips = m_Tooltips,
					m_OverlayBuffer = m_OverlayRenderSystem.GetBuffer(out dependencies9)
				};
				JobHandle job2 = JobHandle.CombineDependencies(dependencies8, dependencies9);
				JobHandle jobHandle6 = IJobExtensions.Schedule(jobData4, JobHandle.CombineDependencies(base.Dependency, outJobHandle8, job2));
				jobData4.m_DefinitionChunks.Dispose(jobHandle6);
				m_OverlayRenderSystem.AddBufferWriter(jobHandle6);
				m_TooltipDeps = jobHandle6;
				base.Dependency = jobHandle6;
			}
		}
		else if (m_ToolSystem.activeTool == m_ObjectToolSystem)
		{
			if (!flag)
			{
				JobHandle outJobHandle9;
				JobHandle dependencies10;
				JobHandle dependencies11;
				JobHandle deps2;
				JobHandle dependencies12;
				JobHandle dependencies13;
				ObjectToolGuideLinesJob jobData5 = new ObjectToolGuideLinesJob
				{
					m_CreationDefinitionType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Tools_CreationDefinition_RO_ComponentTypeHandle, ref base.CheckedStateRef),
					m_ObjectDefinitionType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Tools_ObjectDefinition_RO_ComponentTypeHandle, ref base.CheckedStateRef),
					m_OwnerDefinitionType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Tools_OwnerDefinition_RO_ComponentTypeHandle, ref base.CheckedStateRef),
					m_NetCourseType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Tools_NetCourse_RO_ComponentTypeHandle, ref base.CheckedStateRef),
					m_TransformData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Transform_RO_ComponentLookup, ref base.CheckedStateRef),
					m_PrefabObjectGeometryData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_ObjectGeometryData_RO_ComponentLookup, ref base.CheckedStateRef),
					m_PrefabPlaceableObjectData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PlaceableObjectData_RO_ComponentLookup, ref base.CheckedStateRef),
					m_PrefabServiceUpgradeData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_ServiceUpgradeData_RO_ComponentLookup, ref base.CheckedStateRef),
					m_PrefabBuildingData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_BuildingData_RO_ComponentLookup, ref base.CheckedStateRef),
					m_PrefabLotData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_LotData_RO_ComponentLookup, ref base.CheckedStateRef),
					m_PrefabPlaceableNetData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PlaceableNetData_RO_ComponentLookup, ref base.CheckedStateRef),
					m_PrefabNetGeometryData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_NetGeometryData_RO_ComponentLookup, ref base.CheckedStateRef),
					m_PrefabSubAreas = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Prefabs_SubArea_RO_BufferLookup, ref base.CheckedStateRef),
					m_PrefabSubNets = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Prefabs_SubNet_RO_BufferLookup, ref base.CheckedStateRef),
					m_DefinitionChunks = m_DefinitionQuery.ToArchetypeChunkListAsync(Allocator.TempJob, out outJobHandle9),
					m_ControlPoints = m_ObjectToolSystem.GetControlPoints(out dependencies10),
					m_SubSnapPoints = m_ObjectToolSystem.GetSubSnapPoints(out dependencies11),
					m_TerrainHeightData = m_TerrainSystem.GetHeightData(),
					m_WaterSurfaceData = m_WaterSystem.GetSurfaceData(out deps2),
					m_NetUpgradeState = m_ObjectToolSystem.GetNetUpgradeStates(out dependencies12),
					m_GuideLineSettingsData = m_RenderingSettingsQuery.GetSingleton<GuideLineSettingsData>(),
					m_Mode = m_ObjectToolSystem.actualMode,
					m_State = m_ObjectToolSystem.state,
					m_Prefab = ((m_ObjectToolSystem.prefab != null) ? m_PrefabSystem.GetEntity(m_ObjectToolSystem.prefab) : Entity.Null),
					m_DistanceScale = m_ObjectToolSystem.distanceScale,
					m_AngleSides = m_AngleSides,
					m_Tooltips = m_Tooltips,
					m_OverlayBuffer = m_OverlayRenderSystem.GetBuffer(out dependencies13)
				};
				JobHandle jobHandle7 = IJobExtensions.Schedule(jobData5, JobUtils.CombineDependencies(base.Dependency, outJobHandle9, dependencies10, dependencies11, deps2, dependencies12, dependencies13));
				jobData5.m_DefinitionChunks.Dispose(jobHandle7);
				m_TerrainSystem.AddCPUHeightReader(jobHandle7);
				m_WaterSystem.AddSurfaceReader(jobHandle7);
				m_OverlayRenderSystem.AddBufferWriter(jobHandle7);
				m_TooltipDeps = jobHandle7;
				base.Dependency = jobHandle7;
			}
		}
		else if (m_ToolSystem.activeTool == m_WaterToolSystem)
		{
			float3 cameraRight = default(float3);
			if (m_CameraUpdateSystem.TryGetViewer(out var viewer))
			{
				cameraRight = viewer.right;
			}
			TerrainHeightData data = m_TerrainSystem.GetHeightData();
			Bounds3 editorCameraBounds = TerrainUtils.GetEditorCameraBounds(m_TerrainSystem, ref data);
			if (m_WaterSystem.UseLegacyWaterSources)
			{
				JobHandle outJobHandle10;
				JobHandle dependencies14;
				WaterToolGuideLinesJobLegacy jobData6 = new WaterToolGuideLinesJobLegacy
				{
					m_WaterSourceDataType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Simulation_WaterSourceData_RO_ComponentTypeHandle, ref base.CheckedStateRef),
					m_TransformType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Objects_Transform_RO_ComponentTypeHandle, ref base.CheckedStateRef),
					m_TempType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Tools_Temp_RO_ComponentTypeHandle, ref base.CheckedStateRef),
					m_GuideLineSettingsData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_GuideLineSettingsData_RO_ComponentLookup, ref base.CheckedStateRef),
					m_WaterSourceColors = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Prefabs_WaterSourceColorElement_RO_BufferLookup, ref base.CheckedStateRef),
					m_Attribute = m_WaterToolSystem.attribute,
					m_PositionOffset = m_TerrainSystem.positionOffset,
					m_CameraRight = cameraRight,
					m_TerrainHeightData = m_TerrainSystem.GetHeightData(),
					m_WaterSourceChunks = m_WaterSourceQuery.ToArchetypeChunkListAsync(Allocator.TempJob, out outJobHandle10),
					m_GuideLineSettingsEntity = m_RenderingSettingsQuery.GetSingletonEntity(),
					m_OverlayBuffer = m_OverlayRenderSystem.GetBuffer(out dependencies14)
				};
				JobHandle jobHandle8 = IJobExtensions.Schedule(jobData6, JobHandle.CombineDependencies(base.Dependency, outJobHandle10, dependencies14));
				jobData6.m_WaterSourceChunks.Dispose(jobHandle8);
				m_OverlayRenderSystem.AddBufferWriter(jobHandle8);
				base.Dependency = jobHandle8;
			}
			else
			{
				JobHandle outJobHandle11;
				JobHandle dependencies15;
				WaterToolGuideLinesJob jobData7 = new WaterToolGuideLinesJob
				{
					m_WaterSourceDataType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Simulation_WaterSourceData_RO_ComponentTypeHandle, ref base.CheckedStateRef),
					m_TransformType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Objects_Transform_RO_ComponentTypeHandle, ref base.CheckedStateRef),
					m_TempType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Tools_Temp_RO_ComponentTypeHandle, ref base.CheckedStateRef),
					m_GuideLineSettingsData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_GuideLineSettingsData_RO_ComponentLookup, ref base.CheckedStateRef),
					m_WaterSourceColors = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Prefabs_WaterSourceColorElement_RO_BufferLookup, ref base.CheckedStateRef),
					m_Attribute = m_WaterToolSystem.attribute,
					m_PositionOffset = m_TerrainSystem.positionOffset,
					m_CameraRight = cameraRight,
					m_TerrainHeightData = data,
					m_WorldBounds = editorCameraBounds,
					m_WaterSourceChunks = m_WaterSourceQuery.ToArchetypeChunkListAsync(Allocator.TempJob, out outJobHandle11),
					m_GuideLineSettingsEntity = m_RenderingSettingsQuery.GetSingletonEntity(),
					m_OverlayBuffer = m_OverlayRenderSystem.GetBuffer(out dependencies15),
					m_showNames = m_WaterToolSystem.m_showSourceNames
				};
				JobHandle jobHandle9 = IJobExtensions.Schedule(jobData7, JobHandle.CombineDependencies(base.Dependency, outJobHandle11, dependencies15));
				jobData7.m_WaterSourceChunks.Dispose(jobHandle9);
				m_OverlayRenderSystem.AddBufferWriter(jobHandle9);
				base.Dependency = jobHandle9;
			}
		}
	}

	private static void CheckDirection(float2 startDir, float2 checkDir, ref float2 leftDir, ref float2 rightDir, ref int bestLeft, ref int bestRight, ref float2 leftDir2, ref float2 rightDir2, ref int bestLeft2, ref int bestRight2)
	{
		if (!MathUtils.TryNormalize(ref checkDir))
		{
			return;
		}
		int num = Mathf.RoundToInt(math.degrees(math.acos(math.clamp(math.dot(startDir, checkDir), -1f, 1f))));
		if (num == 0)
		{
			return;
		}
		bool num2 = math.dot(MathUtils.Right(startDir), checkDir) > 0f;
		if (num2 || num == 180)
		{
			if (num < bestRight)
			{
				rightDir = checkDir;
				bestRight = num;
			}
			if ((num == 90 || num == 180) && num < bestRight2)
			{
				rightDir2 = checkDir;
				bestRight2 = num;
			}
		}
		if (!num2 || num == 180)
		{
			if (num < bestLeft)
			{
				leftDir = checkDir;
				bestLeft = num;
			}
			if ((num == 90 || num == 180) && num < bestLeft2)
			{
				leftDir2 = checkDir;
				bestLeft2 = num;
			}
		}
	}

	private static void DrawAngleIndicator(OverlayRenderSystem.Buffer buffer, NativeList<TooltipInfo> tooltips, GuideLineSettingsData guideLineSettings, Line3.Segment line1, Line3.Segment line2, float2 dir1, float2 dir2, float size, float lineWidth, int angle, bool angleSide)
	{
		if (angle == 180)
		{
			float2 @float = (angleSide ? MathUtils.Right(dir1) : MathUtils.Left(dir1));
			float2 float2 = (angleSide ? MathUtils.Right(dir2) : MathUtils.Left(dir2));
			float3 b = line1.b;
			b.xz -= dir1 * size;
			float3 b2 = line1.b;
			float3 b3 = line1.b;
			b2.xz += @float * (size - lineWidth * 0.5f) - dir1 * size;
			b3.xz += @float * size - dir1 * (size + lineWidth * 0.5f);
			float3 a = line2.a;
			float3 a2 = line2.a;
			a.xz -= float2 * size + dir2 * (size + lineWidth * 0.5f);
			a2.xz -= float2 * (size - lineWidth * 0.5f) + dir2 * size;
			float3 a3 = line2.a;
			a3.xz -= dir2 * size;
			buffer.DrawLine(guideLineSettings.m_HighPriorityColor, new Line3.Segment(b, b2), lineWidth);
			buffer.DrawLine(guideLineSettings.m_HighPriorityColor, new Line3.Segment(b3, a), lineWidth);
			buffer.DrawLine(guideLineSettings.m_HighPriorityColor, new Line3.Segment(a2, a3), lineWidth);
			float3 b4 = line1.b;
			b4.xz += @float * (size * 1.5f);
			tooltips.Add(new TooltipInfo(TooltipType.Angle, b4, angle));
		}
		else if (angle > 90)
		{
			float2 float3 = math.normalize(dir1 + dir2);
			float3 b5 = line1.b;
			b5.xz -= dir1 * size;
			float3 startTangent = new float3
			{
				xz = (angleSide ? MathUtils.Right(dir1) : MathUtils.Left(dir1))
			};
			float3 b6 = line1.b;
			b6.xz -= float3 * size;
			float3 float4 = new float3
			{
				xz = (angleSide ? MathUtils.Right(float3) : MathUtils.Left(float3))
			};
			float3 a4 = line2.a;
			a4.xz -= dir2 * size;
			float3 endTangent = new float3
			{
				xz = (angleSide ? MathUtils.Right(dir2) : MathUtils.Left(dir2))
			};
			buffer.DrawCurve(guideLineSettings.m_HighPriorityColor, NetUtils.FitCurve(b5, startTangent, float4, b6), lineWidth);
			buffer.DrawCurve(guideLineSettings.m_HighPriorityColor, NetUtils.FitCurve(b6, float4, endTangent, a4), lineWidth);
			float3 b7 = line1.b;
			b7.xz -= float3 * (size * 1.5f);
			tooltips.Add(new TooltipInfo(TooltipType.Angle, b7, angle));
		}
		else if (angle == 90)
		{
			float3 b8 = line1.b;
			b8.xz -= dir1 * size;
			float3 b9 = line1.b;
			float3 b10 = line1.b;
			b9.xz -= dir2 * (size - lineWidth * 0.5f) + dir1 * size;
			b10.xz -= dir2 * size + dir1 * (size + lineWidth * 0.5f);
			float3 a5 = line2.a;
			a5.xz -= dir2 * size;
			buffer.DrawLine(guideLineSettings.m_HighPriorityColor, new Line3.Segment(b8, b9), lineWidth);
			buffer.DrawLine(guideLineSettings.m_HighPriorityColor, new Line3.Segment(b10, a5), lineWidth);
			float3 b11 = line1.b;
			b11.xz -= math.normalizesafe(dir1 + dir2) * (size * 1.5f);
			tooltips.Add(new TooltipInfo(TooltipType.Angle, b11, angle));
		}
		else if (angle > 0)
		{
			float3 b12 = line1.b;
			b12.xz -= dir1 * size;
			float3 startTangent2 = new float3
			{
				xz = (angleSide ? MathUtils.Right(dir1) : MathUtils.Left(dir1))
			};
			float3 a6 = line2.a;
			a6.xz -= dir2 * size;
			float3 endTangent2 = new float3
			{
				xz = (angleSide ? MathUtils.Right(dir2) : MathUtils.Left(dir2))
			};
			buffer.DrawCurve(guideLineSettings.m_HighPriorityColor, NetUtils.FitCurve(b12, startTangent2, endTangent2, a6), lineWidth);
			float3 b13 = line1.b;
			b13.xz -= math.normalizesafe(dir1 + dir2) * (size * 1.5f);
			tooltips.Add(new TooltipInfo(TooltipType.Angle, b13, angle));
		}
	}

	private static void DrawAreaRange(OverlayRenderSystem.Buffer buffer, quaternion rotation, float3 position, LotData lotData)
	{
		float3 @float = math.forward(rotation);
		UnityEngine.Color color = lotData.m_RangeColor;
		UnityEngine.Color fillColor = color;
		fillColor.a = 0f;
		OverlayRenderSystem.StyleFlags styleFlags = ((!lotData.m_OnWater) ? OverlayRenderSystem.StyleFlags.Projected : ((OverlayRenderSystem.StyleFlags)0));
		buffer.DrawCircle(color, fillColor, lotData.m_MaxRadius * 0.02f, styleFlags, @float.xz, position, lotData.m_MaxRadius * 2f);
	}

	private static void DrawUpgradeRange(OverlayRenderSystem.Buffer buffer, quaternion rotation, float3 position, GuideLineSettingsData guideLineSettings, BuildingData ownerBuildingData, BuildingData buildingData, ServiceUpgradeData serviceUpgradeData)
	{
		UnityEngine.Color lowPriorityColor = guideLineSettings.m_LowPriorityColor;
		UnityEngine.Color fillColor = lowPriorityColor;
		fillColor.a = 0f;
		BuildingUtils.CalculateUpgradeRangeValues(rotation, ownerBuildingData, buildingData, serviceUpgradeData, out var forward, out var width, out var length, out var roundness, out var circular);
		roundness *= 2f;
		length -= roundness;
		roundness /= width;
		if (circular)
		{
			buffer.DrawCircle(lowPriorityColor, fillColor, width * 0.01f, OverlayRenderSystem.StyleFlags.Projected, forward.xz, position, width);
		}
		else
		{
			buffer.DrawLine(line: new Line3.Segment(position - forward * (length * 0.5f), position + forward * (length * 0.5f)), outlineColor: lowPriorityColor, fillColor: fillColor, outlineWidth: width * 0.01f, styleFlags: OverlayRenderSystem.StyleFlags.Projected, width: width, roundness: roundness);
		}
	}

	private static void DrawNetCourse(OverlayRenderSystem.Buffer buffer, NetCourse netCourse, OverlayRenderSystem.StyleFlags styleFlags, NetGeometryData netGeometryData, GuideLineSettingsData guideLineSettings)
	{
		math.select(0f, 1f, new bool2((netCourse.m_StartPosition.m_Flags & (CoursePosFlags.IsFirst | CoursePosFlags.IsLast)) != 0, (netCourse.m_EndPosition.m_Flags & (CoursePosFlags.IsFirst | CoursePosFlags.IsLast)) != 0));
	}

	private static void DrawNetCourse(OverlayRenderSystem.Buffer buffer, NetCourse netCourse, ref TerrainHeightData terrainHeightData, ref WaterSurfaceData<SurfaceWater> waterSurfaceData, NetGeometryData netGeometryData, GuideLineSettingsData guideLineSettings)
	{
		float2 trueValue = math.select(0f, 1f, new bool2((netCourse.m_StartPosition.m_Flags & (CoursePosFlags.IsFirst | CoursePosFlags.IsLast)) != 0, (netCourse.m_EndPosition.m_Flags & (CoursePosFlags.IsFirst | CoursePosFlags.IsLast)) != 0));
		if (netCourse.m_Length < 0.01f)
		{
			OverlayRenderSystem.StyleFlags styleFlags = OverlayRenderSystem.StyleFlags.Projected;
			if (WaterUtils.SampleDepth(ref waterSurfaceData, netCourse.m_StartPosition.m_Position) >= 0.2f)
			{
				netCourse.m_StartPosition.m_Position.y = WaterUtils.SampleHeight(ref waterSurfaceData, ref terrainHeightData, netCourse.m_StartPosition.m_Position);
				styleFlags = (OverlayRenderSystem.StyleFlags)0;
			}
			buffer.DrawCircle(guideLineSettings.m_MediumPriorityColor, guideLineSettings.m_MediumPriorityColor, 0f, styleFlags, new float2(0f, 1f), netCourse.m_StartPosition.m_Position, netGeometryData.m_DefaultWidth);
			return;
		}
		float2 @float = new float2(netCourse.m_StartPosition.m_CourseDelta, netCourse.m_EndPosition.m_CourseDelta);
		float num = netCourse.m_Length * math.abs(@float.y - @float.x);
		int num2 = math.max(1, (int)(num * 0.0625f));
		int num3 = 0;
		float num4 = 1f / (float)num2;
		float3 float2 = new float3(@float.x, math.lerp(@float.x, @float.y, num4), 0f);
		bool3 @bool = new bool3(WaterUtils.SampleDepth(ref waterSurfaceData, MathUtils.Position(netCourse.m_Curve, float2.x)) >= 0.2f, WaterUtils.SampleDepth(ref waterSurfaceData, MathUtils.Position(netCourse.m_Curve, float2.y)) >= 0.2f, z: false);
		for (int i = 1; i <= num2; i++)
		{
			bool flag = i == num2;
			if (!flag)
			{
				float2.z = math.lerp(@float.x, @float.y, (float)(i + 1) * num4);
				@bool.z = WaterUtils.SampleDepth(ref waterSurfaceData, MathUtils.Position(netCourse.m_Curve, float2.z)) >= 0.2f;
			}
			if (flag || math.any(@bool.xy != @bool.yz))
			{
				bool flag2 = i - num3 << 1 > num2;
				OverlayRenderSystem.StyleFlags styleFlags2 = ((!math.any(@bool.xy)) ? OverlayRenderSystem.StyleFlags.Projected : ((OverlayRenderSystem.StyleFlags)0));
				Bezier4x3 curve;
				Bezier4x3 curve2;
				if (flag2)
				{
					curve = MathUtils.Cut(netCourse.m_Curve, new float2(float2.x, math.lerp(float2.x, float2.y, 0.5f)));
					curve2 = MathUtils.Cut(netCourse.m_Curve, new float2(math.lerp(float2.x, float2.y, 0.5f), float2.y));
				}
				else
				{
					curve = MathUtils.Cut(netCourse.m_Curve, float2.xy);
					curve2 = default(Bezier4x3);
				}
				curve.a.y = WaterUtils.SampleHeight(ref waterSurfaceData, ref terrainHeightData, curve.a);
				curve.b.y = WaterUtils.SampleHeight(ref waterSurfaceData, ref terrainHeightData, curve.b);
				curve.c.y = WaterUtils.SampleHeight(ref waterSurfaceData, ref terrainHeightData, curve.c);
				curve.d.y = WaterUtils.SampleHeight(ref waterSurfaceData, ref terrainHeightData, curve.d);
				curve.b.y = math.lerp(curve.a.y, curve.d.y, 1f / 3f);
				curve.c.y = math.lerp(curve.a.y, curve.d.y, 2f / 3f);
				if (flag2)
				{
					curve2.a.y = WaterUtils.SampleHeight(ref waterSurfaceData, ref terrainHeightData, curve2.a);
					curve2.b.y = WaterUtils.SampleHeight(ref waterSurfaceData, ref terrainHeightData, curve2.b);
					curve2.c.y = WaterUtils.SampleHeight(ref waterSurfaceData, ref terrainHeightData, curve2.c);
					curve2.d.y = WaterUtils.SampleHeight(ref waterSurfaceData, ref terrainHeightData, curve2.d);
					curve2.b.y = math.lerp(curve2.a.y, curve2.d.y, 1f / 3f);
					curve2.c.y = math.lerp(curve2.a.y, curve2.d.y, 2f / 3f);
				}
				buffer.DrawCurve(guideLineSettings.m_MediumPriorityColor, guideLineSettings.m_MediumPriorityColor, 0f, styleFlags2, curve, netGeometryData.m_DefaultWidth, math.select(0f, trueValue, new bool2(num3 == 0, !flag2 && flag)));
				if (flag2)
				{
					buffer.DrawCurve(guideLineSettings.m_MediumPriorityColor, guideLineSettings.m_MediumPriorityColor, 0f, styleFlags2, curve2, netGeometryData.m_DefaultWidth, new float2(0f, math.select(0f, trueValue.y, flag)));
				}
				num3 = i;
				float2.x = float2.y;
				@bool.x = @bool.y;
			}
			float2.y = float2.z;
			@bool.y = @bool.z;
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
	public GuideLinesSystem()
	{
	}
}
