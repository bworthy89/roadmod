using System.Runtime.CompilerServices;
using Colossal;
using Colossal.Mathematics;
using Game.Areas;
using Game.Buildings;
using Game.Common;
using Game.Creatures;
using Game.Net;
using Game.Objects;
using Game.Prefabs;
using Game.Rendering;
using Game.Tools;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Scripting;

namespace Game.Debug;

[CompilerGenerated]
public class ObjectDebugSystem : BaseDebugSystem
{
	[BurstCompile]
	private struct ObjectGizmoJob : IJobChunk
	{
		[ReadOnly]
		public bool m_GeometryOption;

		[ReadOnly]
		public bool m_MarkerOption;

		[ReadOnly]
		public bool m_PivotOption;

		[ReadOnly]
		public bool m_OutlineOption;

		[ReadOnly]
		public bool m_InterpolatedOption;

		[ReadOnly]
		public bool m_NetConnectionOption;

		[ReadOnly]
		public bool m_GroupConnectionOption;

		[ReadOnly]
		public bool m_DistrictOption;

		[ReadOnly]
		public bool m_LotHeightOption;

		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentTypeHandle<Game.Objects.Transform> m_TransformType;

		[ReadOnly]
		public ComponentTypeHandle<Stack> m_StackType;

		[ReadOnly]
		public ComponentTypeHandle<Attached> m_AttachedType;

		[ReadOnly]
		public ComponentTypeHandle<ObjectGeometry> m_ObjectGeometryType;

		[ReadOnly]
		public ComponentTypeHandle<Game.Objects.Marker> m_MarkerType;

		[ReadOnly]
		public ComponentTypeHandle<Game.Objects.SpawnLocation> m_SpawnLocationType;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> m_PrefabRefType;

		[ReadOnly]
		public ComponentTypeHandle<InterpolatedTransform> m_InterpolatedTransformType;

		[ReadOnly]
		public ComponentTypeHandle<Temp> m_TempType;

		[ReadOnly]
		public ComponentTypeHandle<CurrentVehicle> m_CurrentVehicleType;

		[ReadOnly]
		public ComponentTypeHandle<GroupMember> m_GroupMemberType;

		[ReadOnly]
		public ComponentTypeHandle<CurrentDistrict> m_CurrentDistrictType;

		[ReadOnly]
		public ComponentTypeHandle<Building> m_BuildingType;

		[ReadOnly]
		public ComponentTypeHandle<Game.Buildings.Lot> m_BuildingLotType;

		[ReadOnly]
		public ComponentLookup<ObjectGeometryData> m_PrefabGeometryData;

		[ReadOnly]
		public ComponentLookup<StackData> m_PrefabStackData;

		[ReadOnly]
		public ComponentLookup<BuildingData> m_PrefabBuildingData;

		[ReadOnly]
		public ComponentLookup<BuildingExtensionData> m_PrefabBuildingExtensionData;

		[ReadOnly]
		public ComponentLookup<PrefabRef> m_PrefabRefData;

		[ReadOnly]
		public ComponentLookup<Game.Objects.Transform> m_TransformData;

		[ReadOnly]
		public ComponentLookup<Curve> m_NetCurveData;

		[ReadOnly]
		public ComponentLookup<Geometry> m_AreaGeometryData;

		[ReadOnly]
		public ComponentLookup<CurrentVehicle> m_CurrentVehicleData;

		[ReadOnly]
		public Entity m_Selected;

		public GizmoBatcher m_GizmoBatcher;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			int num;
			int num2;
			if (m_Selected != Entity.Null)
			{
				num = (num2 = -1);
				NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
				for (int i = 0; i < nativeArray.Length; i++)
				{
					if (nativeArray[i] == m_Selected)
					{
						num = i;
						num2 = i + 1;
						break;
					}
				}
				if (num == -1)
				{
					return;
				}
			}
			else
			{
				num = 0;
				num2 = chunk.Count;
			}
			UnityEngine.Color pivotColor;
			UnityEngine.Color outlineColor;
			if (chunk.Has(ref m_TempType))
			{
				pivotColor = UnityEngine.Color.blue;
				outlineColor = UnityEngine.Color.blue;
			}
			else
			{
				pivotColor = UnityEngine.Color.cyan;
				outlineColor = UnityEngine.Color.white;
			}
			NativeArray<Game.Objects.Transform> nativeArray2 = chunk.GetNativeArray(ref m_TransformType);
			NativeArray<Stack> nativeArray3 = chunk.GetNativeArray(ref m_StackType);
			NativeArray<PrefabRef> nativeArray4 = chunk.GetNativeArray(ref m_PrefabRefType);
			NativeArray<CurrentVehicle> nativeArray5 = chunk.GetNativeArray(ref m_CurrentVehicleType);
			if (nativeArray5.Length != 0)
			{
				NativeArray<GroupMember> nativeArray6 = chunk.GetNativeArray(ref m_GroupMemberType);
				if (!m_GeometryOption && (!m_GroupConnectionOption || nativeArray6.Length == 0))
				{
					return;
				}
				NativeArray<Entity> nativeArray7 = chunk.GetNativeArray(m_EntityType);
				for (int j = num; j < num2; j++)
				{
					Entity entity = nativeArray7[j];
					Game.Objects.Transform transform = nativeArray2[j];
					PrefabRef prefabRef = nativeArray4[j];
					CurrentVehicle currentVehicle = nativeArray5[j];
					ObjectGeometryData prefabObjectData = m_PrefabGeometryData[prefabRef.m_Prefab];
					GetVehicleTransform(entity, currentVehicle, prefabObjectData, ref transform);
					if (m_GeometryOption)
					{
						if (nativeArray3.Length != 0 && m_PrefabStackData.TryGetComponent(prefabRef.m_Prefab, out var componentData))
						{
							DrawObject(transform, nativeArray3[j], prefabObjectData, componentData, pivotColor, outlineColor);
						}
						else
						{
							DrawObject(transform, prefabObjectData, pivotColor, outlineColor);
						}
					}
					if (!m_GroupConnectionOption || nativeArray6.Length == 0)
					{
						continue;
					}
					GroupMember groupMember = nativeArray6[j];
					if (m_TransformData.HasComponent(groupMember.m_Leader))
					{
						Game.Objects.Transform transform2 = m_TransformData[groupMember.m_Leader];
						if (m_CurrentVehicleData.HasComponent(groupMember.m_Leader))
						{
							CurrentVehicle currentVehicle2 = m_CurrentVehicleData[groupMember.m_Leader];
							PrefabRef prefabRef2 = m_PrefabRefData[groupMember.m_Leader];
							ObjectGeometryData prefabObjectData2 = m_PrefabGeometryData[prefabRef2.m_Prefab];
							GetVehicleTransform(groupMember.m_Leader, currentVehicle2, prefabObjectData2, ref transform2);
						}
						m_GizmoBatcher.DrawLine(transform.m_Position, transform2.m_Position, UnityEngine.Color.green);
					}
					else
					{
						m_GizmoBatcher.DrawWireNode(transform.m_Position, 2f, UnityEngine.Color.red);
					}
				}
				return;
			}
			NativeArray<Attached> nativeArray8 = chunk.GetNativeArray(ref m_AttachedType);
			if (chunk.Has(ref m_ObjectGeometryType))
			{
				if (m_GeometryOption)
				{
					NativeArray<InterpolatedTransform> nativeArray9 = chunk.GetNativeArray(ref m_InterpolatedTransformType);
					if (nativeArray9.Length != 0 && m_InterpolatedOption)
					{
						for (int k = num; k < num2; k++)
						{
							Game.Objects.Transform transform3 = nativeArray2[k];
							InterpolatedTransform interpolatedTransform = nativeArray9[k];
							PrefabRef prefabRef3 = nativeArray4[k];
							ObjectGeometryData prefabObjectData3 = m_PrefabGeometryData[prefabRef3.m_Prefab];
							if (nativeArray3.Length != 0 && m_PrefabStackData.TryGetComponent(prefabRef3.m_Prefab, out var componentData2))
							{
								DrawObject(transform3, nativeArray3[k], prefabObjectData3, componentData2, pivotColor, outlineColor);
								DrawObject(interpolatedTransform.ToTransform(), nativeArray3[k], prefabObjectData3, componentData2, UnityEngine.Color.green, UnityEngine.Color.green);
							}
							else
							{
								DrawObject(transform3, prefabObjectData3, pivotColor, outlineColor);
								DrawObject(interpolatedTransform.ToTransform(), prefabObjectData3, UnityEngine.Color.green, UnityEngine.Color.green);
							}
						}
					}
					else if (nativeArray8.Length != 0)
					{
						for (int l = num; l < num2; l++)
						{
							Game.Objects.Transform transform4 = nativeArray2[l];
							Attached attached = nativeArray8[l];
							PrefabRef prefabRef4 = nativeArray4[l];
							ObjectGeometryData prefabObjectData4 = m_PrefabGeometryData[prefabRef4.m_Prefab];
							UnityEngine.Color color = ((attached.m_Parent == Entity.Null) ? UnityEngine.Color.red : ((!m_PrefabRefData.HasComponent(attached.m_Parent)) ? UnityEngine.Color.yellow : UnityEngine.Color.green));
							if (m_NetConnectionOption && GetAttachPosition(attached, out var attachPosition))
							{
								m_GizmoBatcher.DrawLine(transform4.m_Position, attachPosition, color);
								m_GizmoBatcher.DrawWireNode(attachPosition, 1f, color);
							}
							if (nativeArray3.Length != 0 && m_PrefabStackData.TryGetComponent(prefabRef4.m_Prefab, out var componentData3))
							{
								DrawObject(transform4, nativeArray3[l], prefabObjectData4, componentData3, color, outlineColor);
							}
							else
							{
								DrawObject(transform4, prefabObjectData4, color, outlineColor);
							}
						}
					}
					else
					{
						for (int m = num; m < num2; m++)
						{
							Game.Objects.Transform transform5 = nativeArray2[m];
							PrefabRef prefabRef5 = nativeArray4[m];
							ObjectGeometryData prefabObjectData5 = m_PrefabGeometryData[prefabRef5.m_Prefab];
							if (nativeArray3.Length != 0 && m_PrefabStackData.TryGetComponent(prefabRef5.m_Prefab, out var componentData4))
							{
								DrawObject(transform5, nativeArray3[m], prefabObjectData5, componentData4, pivotColor, outlineColor);
							}
							else
							{
								DrawObject(transform5, prefabObjectData5, pivotColor, outlineColor);
							}
						}
					}
				}
			}
			else if (chunk.Has(ref m_MarkerType) && m_MarkerOption)
			{
				if (nativeArray8.Length != 0)
				{
					for (int n = num; n < num2; n++)
					{
						Game.Objects.Transform transform6 = nativeArray2[n];
						Attached attached2 = nativeArray8[n];
						PrefabRef prefabRef6 = nativeArray4[n];
						ObjectGeometryData prefabObjectData6 = m_PrefabGeometryData[prefabRef6.m_Prefab];
						UnityEngine.Color color2 = ((attached2.m_Parent == Entity.Null) ? UnityEngine.Color.red : ((!m_PrefabRefData.HasComponent(attached2.m_Parent)) ? UnityEngine.Color.yellow : UnityEngine.Color.green));
						if (m_NetConnectionOption && GetAttachPosition(attached2, out var attachPosition2))
						{
							m_GizmoBatcher.DrawLine(transform6.m_Position, attachPosition2, color2);
							m_GizmoBatcher.DrawWireNode(attachPosition2, 1f, color2);
						}
						if (nativeArray3.Length != 0 && m_PrefabStackData.TryGetComponent(prefabRef6.m_Prefab, out var componentData5))
						{
							DrawObject(transform6, nativeArray3[n], prefabObjectData6, componentData5, color2, outlineColor);
						}
						else
						{
							DrawObject(transform6, prefabObjectData6, color2, outlineColor);
						}
					}
				}
				else
				{
					for (int num3 = num; num3 < num2; num3++)
					{
						Game.Objects.Transform transform7 = nativeArray2[num3];
						PrefabRef prefabRef7 = nativeArray4[num3];
						ObjectGeometryData prefabObjectData7 = m_PrefabGeometryData[prefabRef7.m_Prefab];
						if (nativeArray3.Length != 0 && m_PrefabStackData.TryGetComponent(prefabRef7.m_Prefab, out var componentData6))
						{
							DrawObject(transform7, nativeArray3[num3], prefabObjectData7, componentData6, pivotColor, outlineColor);
						}
						else
						{
							DrawObject(transform7, prefabObjectData7, pivotColor, outlineColor);
						}
					}
				}
			}
			if (m_NetConnectionOption)
			{
				NativeArray<Building> nativeArray10 = chunk.GetNativeArray(ref m_BuildingType);
				if (nativeArray10.Length != 0)
				{
					for (int num4 = num; num4 < num2; num4++)
					{
						Building building = nativeArray10[num4];
						Game.Objects.Transform transform8 = nativeArray2[num4];
						PrefabRef prefabRef8 = nativeArray4[num4];
						BuildingData buildingData = m_PrefabBuildingData[prefabRef8.m_Prefab];
						if ((buildingData.m_Flags & Game.Prefabs.BuildingFlags.NoRoadConnection) == 0)
						{
							float3 @float = BuildingUtils.CalculateFrontPosition(transform8, buildingData.m_LotSize.y);
							if (building.m_RoadEdge != Entity.Null)
							{
								float3 float2 = MathUtils.Position(m_NetCurveData[building.m_RoadEdge].m_Bezier, building.m_CurvePosition);
								m_GizmoBatcher.DrawLine(@float, float2, UnityEngine.Color.green);
								m_GizmoBatcher.DrawWireNode(@float, 2f, UnityEngine.Color.green);
								m_GizmoBatcher.DrawWireNode(float2, 1f, UnityEngine.Color.green);
							}
							else
							{
								m_GizmoBatcher.DrawWireNode(@float, 2f, UnityEngine.Color.red);
							}
						}
					}
				}
				NativeArray<Game.Objects.SpawnLocation> nativeArray11 = chunk.GetNativeArray(ref m_SpawnLocationType);
				if (nativeArray11.Length != 0)
				{
					for (int num5 = num; num5 < num2; num5++)
					{
						Game.Objects.SpawnLocation spawnLocation = nativeArray11[num5];
						Game.Objects.Transform transform9 = nativeArray2[num5];
						if (spawnLocation.m_ConnectedLane1 != Entity.Null)
						{
							float3 float3 = MathUtils.Position(m_NetCurveData[spawnLocation.m_ConnectedLane1].m_Bezier, spawnLocation.m_CurvePosition1);
							m_GizmoBatcher.DrawWireNode(transform9.m_Position, 2f, UnityEngine.Color.green);
							m_GizmoBatcher.DrawLine(transform9.m_Position, float3, UnityEngine.Color.green);
							m_GizmoBatcher.DrawWireNode(float3, 1f, UnityEngine.Color.green);
							if (spawnLocation.m_ConnectedLane2 != Entity.Null)
							{
								float3 float4 = MathUtils.Position(m_NetCurveData[spawnLocation.m_ConnectedLane2].m_Bezier, spawnLocation.m_CurvePosition2);
								m_GizmoBatcher.DrawLine(transform9.m_Position, float4, UnityEngine.Color.green);
								m_GizmoBatcher.DrawWireNode(float4, 1f, UnityEngine.Color.green);
							}
						}
						else
						{
							m_GizmoBatcher.DrawWireNode(transform9.m_Position, 2f, UnityEngine.Color.red);
						}
					}
				}
			}
			if (m_GroupConnectionOption)
			{
				NativeArray<GroupMember> nativeArray12 = chunk.GetNativeArray(ref m_GroupMemberType);
				if (nativeArray12.Length != 0)
				{
					for (int num6 = num; num6 < num2; num6++)
					{
						Game.Objects.Transform transform10 = nativeArray2[num6];
						GroupMember groupMember2 = nativeArray12[num6];
						if (m_TransformData.HasComponent(groupMember2.m_Leader))
						{
							Game.Objects.Transform transform11 = m_TransformData[groupMember2.m_Leader];
							if (m_CurrentVehicleData.HasComponent(groupMember2.m_Leader))
							{
								CurrentVehicle currentVehicle3 = m_CurrentVehicleData[groupMember2.m_Leader];
								PrefabRef prefabRef9 = m_PrefabRefData[groupMember2.m_Leader];
								ObjectGeometryData prefabObjectData8 = m_PrefabGeometryData[prefabRef9.m_Prefab];
								GetVehicleTransform(groupMember2.m_Leader, currentVehicle3, prefabObjectData8, ref transform11);
							}
							m_GizmoBatcher.DrawLine(transform10.m_Position, transform11.m_Position, UnityEngine.Color.green);
						}
						else
						{
							m_GizmoBatcher.DrawWireNode(transform10.m_Position, 2f, UnityEngine.Color.red);
						}
					}
				}
			}
			if (m_DistrictOption)
			{
				NativeArray<CurrentDistrict> nativeArray13 = chunk.GetNativeArray(ref m_CurrentDistrictType);
				if (nativeArray13.Length != 0)
				{
					for (int num7 = num; num7 < num2; num7++)
					{
						CurrentDistrict currentDistrict = nativeArray13[num7];
						Game.Objects.Transform transform12 = nativeArray2[num7];
						if (currentDistrict.m_District != Entity.Null)
						{
							Geometry geometry = m_AreaGeometryData[currentDistrict.m_District];
							m_GizmoBatcher.DrawLine(transform12.m_Position, geometry.m_CenterPosition, UnityEngine.Color.green);
							m_GizmoBatcher.DrawWireNode(transform12.m_Position, 8f, UnityEngine.Color.green);
						}
						else
						{
							m_GizmoBatcher.DrawWireNode(transform12.m_Position, 8f, UnityEngine.Color.red);
						}
					}
				}
			}
			if (!m_LotHeightOption)
			{
				return;
			}
			NativeArray<Game.Buildings.Lot> nativeArray14 = chunk.GetNativeArray(ref m_BuildingLotType);
			if (nativeArray14.Length == 0)
			{
				return;
			}
			for (int num8 = num; num8 < num2; num8++)
			{
				Game.Buildings.Lot lot = nativeArray14[num8];
				Game.Objects.Transform transform13 = nativeArray2[num8];
				PrefabRef prefabRef10 = nativeArray4[num8];
				int2 lotSize = 1;
				BuildingExtensionData componentData8;
				if (m_PrefabBuildingData.TryGetComponent(prefabRef10.m_Prefab, out var componentData7))
				{
					lotSize = componentData7.m_LotSize;
				}
				else if (m_PrefabBuildingExtensionData.TryGetComponent(prefabRef10.m_Prefab, out componentData8))
				{
					if (!componentData8.m_External)
					{
						continue;
					}
					lotSize = componentData8.m_LotSize;
				}
				Quad3 quad = BuildingUtils.CalculateCorners(transform13, lotSize);
				Bezier4x3 bezier = NetUtils.StraightCurve(quad.a, quad.b);
				Bezier4x3 bezier2 = NetUtils.StraightCurve(quad.b, quad.c);
				Bezier4x3 bezier3 = NetUtils.StraightCurve(quad.c, quad.d);
				Bezier4x3 bezier4 = NetUtils.StraightCurve(quad.d, quad.a);
				bezier.y = new Bezier4x1(bezier.y.abcd + new float4(lot.m_FrontHeights, lot.m_RightHeights.x));
				bezier2.y = new Bezier4x1(bezier2.y.abcd + new float4(lot.m_RightHeights, lot.m_BackHeights.x));
				bezier3.y = new Bezier4x1(bezier3.y.abcd + new float4(lot.m_BackHeights, lot.m_LeftHeights.x));
				bezier4.y = new Bezier4x1(bezier4.y.abcd + new float4(lot.m_LeftHeights, lot.m_FrontHeights.x));
				m_GizmoBatcher.DrawCurve(bezier, (float)lotSize.x * 8f, UnityEngine.Color.magenta);
				m_GizmoBatcher.DrawCurve(bezier2, (float)lotSize.y * 8f, UnityEngine.Color.magenta);
				m_GizmoBatcher.DrawCurve(bezier3, (float)lotSize.x * 8f, UnityEngine.Color.magenta);
				m_GizmoBatcher.DrawCurve(bezier4, (float)lotSize.y * 8f, UnityEngine.Color.magenta);
			}
		}

		private void GetVehicleTransform(Entity entity, CurrentVehicle currentVehicle, ObjectGeometryData prefabObjectData, ref Game.Objects.Transform transform)
		{
			if (m_TransformData.HasComponent(currentVehicle.m_Vehicle))
			{
				Unity.Mathematics.Random random = new Unity.Mathematics.Random((uint)(1851936439 + entity.Index));
				random.NextInt();
				PrefabRef prefabRef = m_PrefabRefData[currentVehicle.m_Vehicle];
				transform = m_TransformData[currentVehicle.m_Vehicle];
				ObjectGeometryData objectGeometryData = m_PrefabGeometryData[prefabRef.m_Prefab];
				float3 max = math.max(0f, objectGeometryData.m_Size - prefabObjectData.m_Size);
				float3 v = random.NextFloat3(max);
				v.xz -= max.xz * 0.5f;
				transform.m_Position += math.rotate(transform.m_Rotation, v);
			}
		}

		private bool GetAttachPosition(Attached attached, out float3 attachPosition)
		{
			if (m_NetCurveData.HasComponent(attached.m_Parent))
			{
				attachPosition = MathUtils.Position(m_NetCurveData[attached.m_Parent].m_Bezier, attached.m_CurvePosition);
				return true;
			}
			attachPosition = default(float3);
			return false;
		}

		private void DrawObject(Game.Objects.Transform transform, ObjectGeometryData prefabObjectData, UnityEngine.Color pivotColor, UnityEngine.Color outlineColor)
		{
			if (m_PivotOption)
			{
				m_GizmoBatcher.DrawWireNode(transform.m_Position, math.sqrt(prefabObjectData.m_Size.x + prefabObjectData.m_Size.z) * 0.25f, pivotColor);
			}
			if (!m_OutlineOption)
			{
				return;
			}
			if (ObjectUtils.GetStandingLegCount(prefabObjectData, out var legCount))
			{
				for (int i = 0; i < legCount; i++)
				{
					float3 standingLegPosition = ObjectUtils.GetStandingLegPosition(prefabObjectData, transform, i);
					float4x4 trs = new float4x4(transform.m_Rotation, standingLegPosition);
					if ((prefabObjectData.m_Flags & Game.Objects.GeometryFlags.CircularLeg) != Game.Objects.GeometryFlags.None)
					{
						m_GizmoBatcher.DrawWireCylinder(trs, new float3(0f, prefabObjectData.m_LegSize.y * 0.5f, 0f), prefabObjectData.m_LegSize.x * 0.5f, prefabObjectData.m_LegSize.y, outlineColor);
					}
					else
					{
						m_GizmoBatcher.DrawWireCube(trs, new float3(0f, prefabObjectData.m_LegSize.y * 0.5f, 0f), prefabObjectData.m_LegSize, outlineColor);
					}
				}
				float4x4 trs2 = new float4x4(transform.m_Rotation, transform.m_Position);
				if ((prefabObjectData.m_Flags & Game.Objects.GeometryFlags.Circular) != Game.Objects.GeometryFlags.None)
				{
					float num = prefabObjectData.m_Size.y - prefabObjectData.m_LegSize.y;
					m_GizmoBatcher.DrawWireCylinder(trs2, new float3(0f, prefabObjectData.m_LegSize.y + num * 0.5f, 0f), prefabObjectData.m_Size.x * 0.5f, num, outlineColor);
					return;
				}
				prefabObjectData.m_Bounds.min.y = prefabObjectData.m_LegSize.y;
				float3 center = MathUtils.Center(prefabObjectData.m_Bounds);
				float3 size = MathUtils.Size(prefabObjectData.m_Bounds);
				m_GizmoBatcher.DrawWireCube(trs2, center, size, outlineColor);
			}
			else
			{
				float4x4 trs3 = new float4x4(transform.m_Rotation, transform.m_Position);
				if ((prefabObjectData.m_Flags & Game.Objects.GeometryFlags.Circular) != Game.Objects.GeometryFlags.None)
				{
					m_GizmoBatcher.DrawWireCylinder(trs3, new float3(0f, prefabObjectData.m_Size.y * 0.5f, 0f), prefabObjectData.m_Size.x * 0.5f, prefabObjectData.m_Size.y, outlineColor);
					return;
				}
				float3 center2 = MathUtils.Center(prefabObjectData.m_Bounds);
				float3 size2 = MathUtils.Size(prefabObjectData.m_Bounds);
				m_GizmoBatcher.DrawWireCube(trs3, center2, size2, outlineColor);
			}
		}

		private void DrawObject(Game.Objects.Transform transform, Stack stack, ObjectGeometryData prefabObjectData, StackData stackData, UnityEngine.Color pivotColor, UnityEngine.Color outlineColor)
		{
			if (m_PivotOption)
			{
				m_GizmoBatcher.DrawWireNode(transform.m_Position, math.sqrt(prefabObjectData.m_Size.x + prefabObjectData.m_Size.z) * 0.25f, pivotColor);
			}
			if (!m_OutlineOption)
			{
				return;
			}
			switch (stackData.m_Direction)
			{
			case StackDirection.Right:
			{
				float3 float3 = math.rotate(transform.m_Rotation, math.right());
				transform.m_Position += float3 * (stack.m_Range.min - prefabObjectData.m_Bounds.min.x);
				prefabObjectData.m_Size.x = stack.m_Range.max - stack.m_Range.min;
				prefabObjectData.m_Bounds.x = stack.m_Range - (stack.m_Range.min - prefabObjectData.m_Bounds.min.x);
				break;
			}
			case StackDirection.Up:
			{
				float3 float2 = math.rotate(transform.m_Rotation, math.up());
				transform.m_Position += float2 * (stack.m_Range.min - prefabObjectData.m_Bounds.min.y);
				prefabObjectData.m_LegSize.y -= stack.m_Range.min - prefabObjectData.m_Bounds.min.y;
				prefabObjectData.m_Size.y = stack.m_Range.max - stack.m_Range.min;
				prefabObjectData.m_Bounds.y = stack.m_Range - (stack.m_Range.min - prefabObjectData.m_Bounds.min.y);
				break;
			}
			case StackDirection.Forward:
			{
				float3 @float = math.rotate(transform.m_Rotation, math.forward());
				transform.m_Position += @float * (stack.m_Range.min - prefabObjectData.m_Bounds.min.z);
				prefabObjectData.m_Size.z = stack.m_Range.max - stack.m_Range.min;
				prefabObjectData.m_Bounds.z = stack.m_Range - (stack.m_Range.min - prefabObjectData.m_Bounds.min.z);
				break;
			}
			}
			if (ObjectUtils.GetStandingLegCount(prefabObjectData, out var legCount))
			{
				for (int i = 0; i < legCount; i++)
				{
					float3 standingLegPosition = ObjectUtils.GetStandingLegPosition(prefabObjectData, transform, i);
					float4x4 trs = new float4x4(transform.m_Rotation, standingLegPosition);
					if ((prefabObjectData.m_Flags & Game.Objects.GeometryFlags.CircularLeg) != Game.Objects.GeometryFlags.None)
					{
						m_GizmoBatcher.DrawWireCylinder(trs, new float3(0f, prefabObjectData.m_LegSize.y * 0.5f, 0f), prefabObjectData.m_LegSize.x * 0.5f, prefabObjectData.m_LegSize.y, outlineColor);
					}
					else
					{
						m_GizmoBatcher.DrawWireCube(trs, new float3(0f, prefabObjectData.m_LegSize.y * 0.5f, 0f), prefabObjectData.m_LegSize, outlineColor);
					}
				}
				float4x4 trs2 = new float4x4(transform.m_Rotation, transform.m_Position);
				if ((prefabObjectData.m_Flags & Game.Objects.GeometryFlags.Circular) != Game.Objects.GeometryFlags.None)
				{
					float num = prefabObjectData.m_Size.y - prefabObjectData.m_LegSize.y;
					m_GizmoBatcher.DrawWireCylinder(trs2, new float3(0f, prefabObjectData.m_LegSize.y + num * 0.5f, 0f), prefabObjectData.m_Size.x * 0.5f, num, outlineColor);
					return;
				}
				prefabObjectData.m_Bounds.min.y = prefabObjectData.m_LegSize.y;
				float3 center = MathUtils.Center(prefabObjectData.m_Bounds);
				float3 size = MathUtils.Size(prefabObjectData.m_Bounds);
				m_GizmoBatcher.DrawWireCube(trs2, center, size, outlineColor);
			}
			else
			{
				float4x4 trs3 = new float4x4(transform.m_Rotation, transform.m_Position);
				if ((prefabObjectData.m_Flags & Game.Objects.GeometryFlags.Circular) != Game.Objects.GeometryFlags.None)
				{
					m_GizmoBatcher.DrawWireCylinder(trs3, new float3(0f, prefabObjectData.m_Size.y * 0.5f, 0f), prefabObjectData.m_Size.x * 0.5f, prefabObjectData.m_Size.y, outlineColor);
					return;
				}
				float3 center2 = MathUtils.Center(prefabObjectData.m_Bounds);
				float3 size2 = MathUtils.Size(prefabObjectData.m_Bounds);
				m_GizmoBatcher.DrawWireCube(trs3, center2, size2, outlineColor);
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
		public EntityTypeHandle __Unity_Entities_Entity_TypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Game.Objects.Transform> __Game_Objects_Transform_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Stack> __Game_Objects_Stack_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Attached> __Game_Objects_Attached_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<ObjectGeometry> __Game_Objects_ObjectGeometry_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Game.Objects.Marker> __Game_Objects_Marker_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Game.Objects.SpawnLocation> __Game_Objects_SpawnLocation_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<InterpolatedTransform> __Game_Rendering_InterpolatedTransform_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Temp> __Game_Tools_Temp_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<CurrentVehicle> __Game_Creatures_CurrentVehicle_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<GroupMember> __Game_Creatures_GroupMember_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<CurrentDistrict> __Game_Areas_CurrentDistrict_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Building> __Game_Buildings_Building_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Game.Buildings.Lot> __Game_Buildings_Lot_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentLookup<ObjectGeometryData> __Game_Prefabs_ObjectGeometryData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<StackData> __Game_Prefabs_StackData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<BuildingData> __Game_Prefabs_BuildingData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<BuildingExtensionData> __Game_Prefabs_BuildingExtensionData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Objects.Transform> __Game_Objects_Transform_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Curve> __Game_Net_Curve_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Geometry> __Game_Areas_Geometry_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<CurrentVehicle> __Game_Creatures_CurrentVehicle_RO_ComponentLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
			__Game_Objects_Transform_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Game.Objects.Transform>(isReadOnly: true);
			__Game_Objects_Stack_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Stack>(isReadOnly: true);
			__Game_Objects_Attached_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Attached>(isReadOnly: true);
			__Game_Objects_ObjectGeometry_RO_ComponentTypeHandle = state.GetComponentTypeHandle<ObjectGeometry>(isReadOnly: true);
			__Game_Objects_Marker_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Game.Objects.Marker>(isReadOnly: true);
			__Game_Objects_SpawnLocation_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Game.Objects.SpawnLocation>(isReadOnly: true);
			__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PrefabRef>(isReadOnly: true);
			__Game_Rendering_InterpolatedTransform_RO_ComponentTypeHandle = state.GetComponentTypeHandle<InterpolatedTransform>(isReadOnly: true);
			__Game_Tools_Temp_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Temp>(isReadOnly: true);
			__Game_Creatures_CurrentVehicle_RO_ComponentTypeHandle = state.GetComponentTypeHandle<CurrentVehicle>(isReadOnly: true);
			__Game_Creatures_GroupMember_RO_ComponentTypeHandle = state.GetComponentTypeHandle<GroupMember>(isReadOnly: true);
			__Game_Areas_CurrentDistrict_RO_ComponentTypeHandle = state.GetComponentTypeHandle<CurrentDistrict>(isReadOnly: true);
			__Game_Buildings_Building_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Building>(isReadOnly: true);
			__Game_Buildings_Lot_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Game.Buildings.Lot>(isReadOnly: true);
			__Game_Prefabs_ObjectGeometryData_RO_ComponentLookup = state.GetComponentLookup<ObjectGeometryData>(isReadOnly: true);
			__Game_Prefabs_StackData_RO_ComponentLookup = state.GetComponentLookup<StackData>(isReadOnly: true);
			__Game_Prefabs_BuildingData_RO_ComponentLookup = state.GetComponentLookup<BuildingData>(isReadOnly: true);
			__Game_Prefabs_BuildingExtensionData_RO_ComponentLookup = state.GetComponentLookup<BuildingExtensionData>(isReadOnly: true);
			__Game_Prefabs_PrefabRef_RO_ComponentLookup = state.GetComponentLookup<PrefabRef>(isReadOnly: true);
			__Game_Objects_Transform_RO_ComponentLookup = state.GetComponentLookup<Game.Objects.Transform>(isReadOnly: true);
			__Game_Net_Curve_RO_ComponentLookup = state.GetComponentLookup<Curve>(isReadOnly: true);
			__Game_Areas_Geometry_RO_ComponentLookup = state.GetComponentLookup<Geometry>(isReadOnly: true);
			__Game_Creatures_CurrentVehicle_RO_ComponentLookup = state.GetComponentLookup<CurrentVehicle>(isReadOnly: true);
		}
	}

	private EntityQuery m_ObjectGroup;

	private GizmosSystem m_GizmosSystem;

	private ToolSystem m_ToolSystem;

	private Option m_GeometryOption;

	private Option m_MarkerOption;

	private Option m_PivotOption;

	private Option m_OutlineOption;

	private Option m_InterpolatedOption;

	private Option m_NetConnectionOption;

	private Option m_GroupConnectionOption;

	private Option m_DistrictOption;

	private Option m_LotHeightOption;

	private TypeHandle __TypeHandle;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_GizmosSystem = base.World.GetOrCreateSystemManaged<GizmosSystem>();
		m_ToolSystem = base.World.GetOrCreateSystemManaged<ToolSystem>();
		m_ObjectGroup = GetEntityQuery(ComponentType.ReadOnly<Game.Objects.Object>(), ComponentType.ReadOnly<Game.Objects.Transform>(), ComponentType.ReadOnly<PrefabRef>(), ComponentType.Exclude<Deleted>(), ComponentType.Exclude<Hidden>());
		m_GeometryOption = AddOption("Physical Objects", defaultEnabled: true);
		m_MarkerOption = AddOption("Marker Objects", defaultEnabled: true);
		m_PivotOption = AddOption("Draw Pivots", defaultEnabled: true);
		m_OutlineOption = AddOption("Draw Outlines", defaultEnabled: true);
		m_InterpolatedOption = AddOption("Interpolated Positions", defaultEnabled: true);
		m_NetConnectionOption = AddOption("Net Connections", defaultEnabled: true);
		m_GroupConnectionOption = AddOption("Group Connections", defaultEnabled: true);
		m_DistrictOption = AddOption("District Connections", defaultEnabled: false);
		m_LotHeightOption = AddOption("Lot Heights", defaultEnabled: false);
		base.Enabled = false;
	}

	[Preserve]
	protected override void OnUpdate()
	{
		if (!m_ObjectGroup.IsEmptyIgnoreFilter)
		{
			base.Dependency = DrawObjectGizmos(m_ObjectGroup, base.Dependency);
		}
	}

	private JobHandle DrawObjectGizmos(EntityQuery group, JobHandle inputDeps)
	{
		JobHandle dependencies;
		JobHandle jobHandle = JobChunkExtensions.ScheduleParallel(new ObjectGizmoJob
		{
			m_GeometryOption = m_GeometryOption.enabled,
			m_MarkerOption = m_MarkerOption.enabled,
			m_PivotOption = m_PivotOption.enabled,
			m_OutlineOption = m_OutlineOption.enabled,
			m_InterpolatedOption = m_InterpolatedOption.enabled,
			m_NetConnectionOption = m_NetConnectionOption.enabled,
			m_GroupConnectionOption = m_GroupConnectionOption.enabled,
			m_DistrictOption = m_DistrictOption.enabled,
			m_LotHeightOption = m_LotHeightOption.enabled,
			m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
			m_TransformType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Objects_Transform_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_StackType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Objects_Stack_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_AttachedType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Objects_Attached_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_ObjectGeometryType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Objects_ObjectGeometry_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_MarkerType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Objects_Marker_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_SpawnLocationType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Objects_SpawnLocation_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_PrefabRefType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_InterpolatedTransformType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Rendering_InterpolatedTransform_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_TempType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Tools_Temp_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_CurrentVehicleType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Creatures_CurrentVehicle_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_GroupMemberType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Creatures_GroupMember_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_CurrentDistrictType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Areas_CurrentDistrict_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_BuildingType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Buildings_Building_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_BuildingLotType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Buildings_Lot_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_PrefabGeometryData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_ObjectGeometryData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabStackData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_StackData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabBuildingData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_BuildingData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabBuildingExtensionData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_BuildingExtensionData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabRefData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup, ref base.CheckedStateRef),
			m_TransformData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Transform_RO_ComponentLookup, ref base.CheckedStateRef),
			m_NetCurveData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Curve_RO_ComponentLookup, ref base.CheckedStateRef),
			m_AreaGeometryData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Areas_Geometry_RO_ComponentLookup, ref base.CheckedStateRef),
			m_CurrentVehicleData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Creatures_CurrentVehicle_RO_ComponentLookup, ref base.CheckedStateRef),
			m_Selected = m_ToolSystem.selected,
			m_GizmoBatcher = m_GizmosSystem.GetGizmosBatcher(out dependencies)
		}, group, JobHandle.CombineDependencies(inputDeps, dependencies));
		m_GizmosSystem.AddGizmosBatcherWriter(jobHandle);
		return jobHandle;
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
	public ObjectDebugSystem()
	{
	}
}
