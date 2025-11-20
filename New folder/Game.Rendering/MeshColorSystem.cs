using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Colossal.Collections;
using Colossal.Serialization.Entities;
using Game.Buildings;
using Game.Common;
using Game.Companies;
using Game.Objects;
using Game.Prefabs;
using Game.Prefabs.Climate;
using Game.Routes;
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
using UnityEngine;
using UnityEngine.Scripting;

namespace Game.Rendering;

[CompilerGenerated]
public class MeshColorSystem : GameSystemBase
{
	[BurstCompile]
	private struct FindUpdatedMeshColorsJob : IJobChunk
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentTypeHandle<RentersUpdated> m_RentersUpdatedType;

		[ReadOnly]
		public ComponentTypeHandle<ColorUpdated> m_ColorUpdatedType;

		[ReadOnly]
		public BufferLookup<MeshColor> m_MeshColors;

		[ReadOnly]
		public BufferLookup<Game.Objects.SubObject> m_SubObjects;

		[ReadOnly]
		public BufferLookup<RouteVehicle> m_RouteVehicles;

		[ReadOnly]
		public BufferLookup<LayoutElement> m_LayoutElements;

		public NativeQueue<Entity>.ParallelWriter m_Queue;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<RentersUpdated> nativeArray = chunk.GetNativeArray(ref m_RentersUpdatedType);
			if (nativeArray.Length != 0)
			{
				for (int i = 0; i < nativeArray.Length; i++)
				{
					Entity property = nativeArray[i].m_Property;
					if (m_MeshColors.HasBuffer(property))
					{
						m_Queue.Enqueue(property);
					}
					AddSubObjects(property);
				}
				return;
			}
			NativeArray<ColorUpdated> nativeArray2 = chunk.GetNativeArray(ref m_ColorUpdatedType);
			if (nativeArray2.Length != 0)
			{
				for (int j = 0; j < nativeArray2.Length; j++)
				{
					Entity route = nativeArray2[j].m_Route;
					AddRouteVehicles(route);
				}
				return;
			}
			NativeArray<Entity> nativeArray3 = chunk.GetNativeArray(m_EntityType);
			for (int k = 0; k < nativeArray3.Length; k++)
			{
				Entity value = nativeArray3[k];
				m_Queue.Enqueue(value);
			}
		}

		private void AddSubObjects(Entity owner)
		{
			if (!m_SubObjects.TryGetBuffer(owner, out var bufferData))
			{
				return;
			}
			for (int i = 0; i < bufferData.Length; i++)
			{
				Entity subObject = bufferData[i].m_SubObject;
				if (m_MeshColors.HasBuffer(subObject))
				{
					m_Queue.Enqueue(subObject);
				}
				AddSubObjects(subObject);
			}
		}

		private void AddRouteVehicles(Entity owner)
		{
			if (!m_RouteVehicles.TryGetBuffer(owner, out var bufferData))
			{
				return;
			}
			for (int i = 0; i < bufferData.Length; i++)
			{
				Entity vehicle = bufferData[i].m_Vehicle;
				if (m_LayoutElements.TryGetBuffer(vehicle, out var bufferData2) && bufferData2.Length != 0)
				{
					for (int j = 0; j < bufferData2.Length; j++)
					{
						if (m_MeshColors.HasBuffer(bufferData2[j].m_Vehicle))
						{
							m_Queue.Enqueue(bufferData2[j].m_Vehicle);
						}
					}
				}
				else if (m_MeshColors.HasBuffer(vehicle))
				{
					m_Queue.Enqueue(vehicle);
				}
			}
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	[BurstCompile]
	private struct ListUpdatedMeshColorsJob : IJob
	{
		[StructLayout(LayoutKind.Sequential, Size = 1)]
		private struct EntityComparer : IComparer<Entity>
		{
			public int Compare(Entity x, Entity y)
			{
				return x.Index - y.Index;
			}
		}

		public NativeQueue<Entity> m_Queue;

		public NativeList<Entity> m_List;

		public void Execute()
		{
			int count = m_Queue.Count;
			if (count == 0)
			{
				return;
			}
			int length = m_List.Length;
			m_List.ResizeUninitialized(length + count);
			for (int i = 0; i < count; i++)
			{
				m_List[length + i] = m_Queue.Dequeue();
			}
			m_List.Sort(default(EntityComparer));
			Entity entity = Entity.Null;
			int num = 0;
			int num2 = 0;
			while (num < m_List.Length)
			{
				Entity entity2 = m_List[num++];
				if (entity2 != entity)
				{
					m_List[num2++] = entity2;
					entity = entity2;
				}
			}
			if (num2 < m_List.Length)
			{
				m_List.RemoveRange(num2, m_List.Length - num2);
			}
		}
	}

	private struct CopyColorData
	{
		public Entity m_Source;

		public Entity m_Target;

		public uint m_RandomSeed;

		public int m_ColorIndex;

		public sbyte m_ExternalChannel0;

		public sbyte m_ExternalChannel1;

		public sbyte m_ExternalChannel2;

		public byte m_HueRange;

		public byte m_SaturationRange;

		public byte m_ValueRange;

		public byte m_AlphaRange0;

		public byte m_AlphaRange1;

		public byte m_AlphaRange2;

		public bool hasVariationRanges => (m_HueRange != 0) | (m_SaturationRange != 0) | (m_ValueRange != 0);

		public bool hasAlphaRanges => (m_AlphaRange0 != 0) | (m_AlphaRange1 != 0) | (m_AlphaRange2 != 0);

		public int GetExternalChannelIndex(int colorIndex)
		{
			return colorIndex switch
			{
				0 => m_ExternalChannel0, 
				1 => m_ExternalChannel1, 
				2 => m_ExternalChannel2, 
				_ => -1, 
			};
		}
	}

	private enum UpdateStage
	{
		Default,
		IgnoreSubs,
		IgnoreOwners
	}

	[BurstCompile]
	private struct SetMeshColorsJob : IJobParallelForDefer
	{
		private struct SyncData
		{
			public ColorGroupID m_GroupID;

			public uint m_RandomSeed;

			public int m_ColorIndex;
		}

		private struct SearchData
		{
			public Entity m_ColorSource;

			public Game.Prefabs.AgeMask m_Age;

			public GenderMask m_Gender;

			public bool m_ExternalSearched;

			public bool m_FiltersSearched;
		}

		private struct ColorData
		{
			public ColorVariation m_Color;

			public int m_Probability;

			public int m_Index;

			public uint m_RandomSeed;
		}

		private struct ColorDatas
		{
			public ColorData m_Match;

			public ColorData m_Unmatch;

			public ColorData m_Unsync;

			public uint m_SeedOffset;
		}

		[ReadOnly]
		public RandomSeed m_RandomSeed;

		[ReadOnly]
		public Entity m_DefaultBrand;

		[ReadOnly]
		public Entity m_Season1;

		[ReadOnly]
		public Entity m_Season2;

		[ReadOnly]
		public Entity m_OverrideEntity;

		[ReadOnly]
		public Entity m_OverrideMesh;

		[ReadOnly]
		public float m_SeasonBlend;

		[ReadOnly]
		public int m_OverrideIndex;

		[ReadOnly]
		public UpdateStage m_Stage;

		[ReadOnly]
		public NativeArray<Entity> m_Entities;

		[ReadOnly]
		public ComponentLookup<Owner> m_OwnerData;

		[ReadOnly]
		public ComponentLookup<PseudoRandomSeed> m_PseudoRandomSeedData;

		[ReadOnly]
		public ComponentLookup<Plant> m_PlantData;

		[ReadOnly]
		public ComponentLookup<Controller> m_ControllerData;

		[ReadOnly]
		public ComponentLookup<CurrentRoute> m_CurrentRouteData;

		[ReadOnly]
		public ComponentLookup<Game.Routes.Color> m_RouteColorData;

		[ReadOnly]
		public ComponentLookup<CompanyData> m_CompanyData;

		[ReadOnly]
		public ComponentLookup<Temp> m_TempData;

		[ReadOnly]
		public ComponentLookup<PrefabRef> m_PrefabRefData;

		[ReadOnly]
		public ComponentLookup<BrandData> m_BrandData;

		[ReadOnly]
		public ComponentLookup<CreatureData> m_CreatureData;

		[ReadOnly]
		public ComponentLookup<ResidentData> m_ResidentData;

		[ReadOnly]
		public BufferLookup<MeshGroup> m_MeshGroups;

		[ReadOnly]
		public BufferLookup<Renter> m_Renters;

		[ReadOnly]
		public BufferLookup<SubMesh> m_SubMeshes;

		[ReadOnly]
		public BufferLookup<SubMeshGroup> m_SubMeshGroups;

		[ReadOnly]
		public BufferLookup<ColorVariation> m_ColorVariations;

		[ReadOnly]
		public BufferLookup<ColorFilter> m_ColorFilters;

		[ReadOnly]
		public BufferLookup<OverlayElement> m_OverlayElements;

		[ReadOnly]
		public BufferLookup<CharacterElement> m_CharacterElements;

		[NativeDisableParallelForRestriction]
		public BufferLookup<MeshColor> m_MeshColors;

		public NativeQueue<CopyColorData>.ParallelWriter m_CopyColors;

		public unsafe void Execute(int index)
		{
			Entity entity = m_Entities[index];
			if (m_Stage != UpdateStage.Default && m_OwnerData.HasComponent(entity) == (m_Stage == UpdateStage.IgnoreSubs))
			{
				return;
			}
			PrefabRef prefabRef = m_PrefabRefData[entity];
			DynamicBuffer<MeshColor> meshColors = m_MeshColors[entity];
			DynamicBuffer<MeshGroup> bufferData = default(DynamicBuffer<MeshGroup>);
			int num = 0;
			int num2 = 0;
			if (m_SubMeshes.TryGetBuffer(prefabRef.m_Prefab, out var bufferData2))
			{
				num = 1;
				num2 = bufferData2.Length;
			}
			bool flag = false;
			if (m_SubMeshGroups.TryGetBuffer(prefabRef.m_Prefab, out var bufferData3))
			{
				if (m_MeshGroups.TryGetBuffer(entity, out bufferData))
				{
					num = bufferData.Length;
				}
				num2 = 0;
				for (int i = 0; i < num; i++)
				{
					CollectionUtils.TryGet(bufferData, i, out var value);
					SubMeshGroup subMeshGroup = bufferData3[value.m_SubMeshGroup];
					num2 += subMeshGroup.m_SubMeshRange.y - subMeshGroup.m_SubMeshRange.x;
					for (int j = subMeshGroup.m_SubMeshRange.x; j < subMeshGroup.m_SubMeshRange.y; j++)
					{
						SubMesh subMesh = bufferData2[j];
						if (m_OverlayElements.HasBuffer(subMesh.m_SubMesh))
						{
							flag = true;
							num2 += 8;
							break;
						}
					}
				}
			}
			if (num2 == 0)
			{
				meshColors.Clear();
				return;
			}
			SyncData* syncData = stackalloc SyncData[num2 * 2];
			meshColors.ResizeUninitialized(num2);
			num2 = 0;
			if (!m_PseudoRandomSeedData.TryGetComponent(entity, out var componentData))
			{
				componentData.m_Seed = (ushort)m_RandomSeed.GetRandom(index).NextUInt(65536u);
			}
			SearchData searchData = default(SearchData);
			int syncIndex = 0;
			SubMeshGroup subMeshGroup2 = default(SubMeshGroup);
			for (int k = 0; k < num; k++)
			{
				MeshGroup value2 = default(MeshGroup);
				if (bufferData3.IsCreated)
				{
					CollectionUtils.TryGet(bufferData, k, out value2);
					subMeshGroup2 = bufferData3[value2.m_SubMeshGroup];
				}
				else
				{
					subMeshGroup2.m_SubMeshRange = new int2(0, bufferData2.Length);
				}
				for (int l = subMeshGroup2.m_SubMeshRange.x; l < subMeshGroup2.m_SubMeshRange.y; l++)
				{
					SubMesh subMesh2 = bufferData2[l];
					Unity.Mathematics.Random random = componentData.GetRandom((uint)(PseudoRandomSeed.kColorVariation | (subMesh2.m_RandomSeed << 16)));
					SetColor(meshColors, syncData, num2++, entity, subMesh2.m_SubMesh, ref random, ref searchData, ref syncIndex);
				}
				if (!flag)
				{
					continue;
				}
				for (int m = subMeshGroup2.m_SubMeshRange.x; m < subMeshGroup2.m_SubMeshRange.y; m++)
				{
					SubMesh subMesh3 = bufferData2[m];
					if (m_OverlayElements.TryGetBuffer(subMesh3.m_SubMesh, out var bufferData4))
					{
						CharacterElement characterElement = default(CharacterElement);
						if (m_CharacterElements.TryGetBuffer(prefabRef.m_Prefab, out var bufferData5))
						{
							characterElement = bufferData5[value2.m_SubMeshGroup];
						}
						SetColor(meshColors, syncData, num2++, entity, bufferData4, characterElement.m_OverlayWeights.m_Weight0, componentData, ref searchData, ref syncIndex);
						SetColor(meshColors, syncData, num2++, entity, bufferData4, characterElement.m_OverlayWeights.m_Weight1, componentData, ref searchData, ref syncIndex);
						SetColor(meshColors, syncData, num2++, entity, bufferData4, characterElement.m_OverlayWeights.m_Weight2, componentData, ref searchData, ref syncIndex);
						SetColor(meshColors, syncData, num2++, entity, bufferData4, characterElement.m_OverlayWeights.m_Weight3, componentData, ref searchData, ref syncIndex);
						SetColor(meshColors, syncData, num2++, entity, bufferData4, characterElement.m_OverlayWeights.m_Weight4, componentData, ref searchData, ref syncIndex);
						SetColor(meshColors, syncData, num2++, entity, bufferData4, characterElement.m_OverlayWeights.m_Weight5, componentData, ref searchData, ref syncIndex);
						SetColor(meshColors, syncData, num2++, entity, bufferData4, characterElement.m_OverlayWeights.m_Weight6, componentData, ref searchData, ref syncIndex);
						SetColor(meshColors, syncData, num2++, entity, bufferData4, characterElement.m_OverlayWeights.m_Weight7, componentData, ref searchData, ref syncIndex);
						break;
					}
				}
			}
		}

		private unsafe void SetColor(DynamicBuffer<MeshColor> meshColors, SyncData* syncData, int colorIndex, Entity entity, DynamicBuffer<OverlayElement> overlayElements, BlendWeight overlayWeight, PseudoRandomSeed pseudoRandomSeed, ref SearchData searchData, ref int syncIndex)
		{
			Entity prefab = Entity.Null;
			if (overlayWeight.m_Index >= 0 && overlayWeight.m_Index < overlayElements.Length && overlayWeight.m_Weight > 0f)
			{
				prefab = overlayElements[overlayWeight.m_Index].m_Overlay;
			}
			Unity.Mathematics.Random random = pseudoRandomSeed.GetRandom((uint)(PseudoRandomSeed.kColorVariation | (overlayWeight.m_Index << 16)));
			SetColor(meshColors, syncData, colorIndex, entity, prefab, ref random, ref searchData, ref syncIndex);
		}

		private unsafe void SetColor(DynamicBuffer<MeshColor> meshColors, SyncData* syncData, int colorIndex, Entity entity, Entity prefab, ref Unity.Mathematics.Random random, ref SearchData searchData, ref int syncIndex)
		{
			MeshColor meshColor = new MeshColor
			{
				m_ColorSet = new ColorSet(UnityEngine.Color.white)
			};
			SyncData syncValue = new SyncData
			{
				m_GroupID = new ColorGroupID(-1),
				m_RandomSeed = 0u,
				m_ColorIndex = -1
			};
			if (m_ColorVariations.TryGetBuffer(prefab, out var bufferData))
			{
				ColorDatas colors = default(ColorDatas);
				ColorDatas colors2 = default(ColorDatas);
				int num = 0;
				int num2 = 0;
				uint randomSeed = 0u;
				bool flag = false;
				bool flag2 = false;
				ColorGroupID colorGroupID = new ColorGroupID(-2);
				ColorFilter colorFilter = default(ColorFilter);
				ref ColorDatas reference = ref colors;
				float num3 = 0f;
				if (m_ColorFilters.TryGetBuffer(prefab, out var bufferData2) && !searchData.m_FiltersSearched)
				{
					FindFilters(entity, ref searchData.m_Age, ref searchData.m_Gender);
					searchData.m_FiltersSearched = true;
				}
				int num4 = math.select(-1, m_OverrideIndex, entity == m_OverrideEntity && prefab == m_OverrideMesh && m_OverrideIndex < bufferData.Length);
				for (int i = 0; i < bufferData.Length; i++)
				{
					ColorVariation color = bufferData[i];
					if (color.m_GroupID != colorGroupID)
					{
						num = 0;
						num2 = -1;
						randomSeed = 0u;
						colorGroupID = color.m_GroupID;
						flag = false;
						colorFilter.m_OverrideProbability = -1;
						colorFilter.m_OverrideAlpha = -1f;
						reference = ref colors;
						num3 = 0f;
						if (color.m_SyncFlags != ColorSyncFlags.None)
						{
							for (int j = 0; j < syncIndex; j++)
							{
								if (syncData[j].m_GroupID == colorGroupID)
								{
									num2 = syncData[j].m_ColorIndex;
									randomSeed = syncData[j].m_RandomSeed;
									flag = true;
									break;
								}
							}
							flag2 = flag2 || flag;
						}
						if (bufferData2.IsCreated)
						{
							float2 @float = default(float2);
							for (int k = 0; k < bufferData2.Length; k++)
							{
								ColorFilter colorFilter2 = bufferData2[k];
								if (colorFilter2.m_GroupID != colorGroupID || (colorFilter2.m_AgeFilter & searchData.m_Age) == 0 || (colorFilter2.m_GenderFilter & searchData.m_Gender) == 0)
								{
									continue;
								}
								if ((colorFilter2.m_Flags & ColorFilterFlags.SeasonFilter) != 0)
								{
									if ((colorFilter2.m_Flags & ColorFilterFlags.BlendColor) != 0)
									{
										if (m_Season1 != colorFilter2.m_EntityFilter)
										{
											if (!(m_Season2 == colorFilter2.m_EntityFilter))
											{
												continue;
											}
											reference = ref colors2;
											num3 = m_SeasonBlend;
										}
										reference.m_SeedOffset += (uint)(k * -1571468583);
									}
									else
									{
										if ((colorFilter2.m_Flags & ColorFilterFlags.BlendProbability) != 0)
										{
											if (m_Season1 == colorFilter2.m_EntityFilter)
											{
												@float += new float2((float)colorFilter2.m_OverrideProbability * (1f - m_SeasonBlend), 1f - m_SeasonBlend);
											}
											else if (m_Season2 == colorFilter2.m_EntityFilter)
											{
												@float += new float2((float)colorFilter2.m_OverrideProbability * m_SeasonBlend, m_SeasonBlend);
											}
											continue;
										}
										if (m_Season1 != colorFilter2.m_EntityFilter)
										{
											continue;
										}
									}
								}
								if (colorFilter2.m_OverrideProbability >= 0)
								{
									colorFilter.m_OverrideProbability = colorFilter2.m_OverrideProbability;
								}
								colorFilter.m_OverrideAlpha = math.select(colorFilter.m_OverrideAlpha, colorFilter2.m_OverrideAlpha, colorFilter2.m_OverrideAlpha >= 0f);
							}
							if (@float.y != 0f)
							{
								colorFilter.m_OverrideProbability = (sbyte)math.clamp((int)((float)(int)color.m_Probability * (1f - @float.y) + @float.x + 0.5f), 0, 100);
							}
						}
					}
					if (num4 != -1)
					{
						color.m_Probability = (byte)math.select(0, 100, i == num4);
					}
					else if (colorFilter.m_OverrideProbability != -1)
					{
						color.m_Probability = (byte)colorFilter.m_OverrideProbability;
					}
					bool3 x = colorFilter.m_OverrideAlpha >= 0f;
					if (math.any(x))
					{
						color.m_ColorSet.m_Channel0.a = math.select(color.m_ColorSet.m_Channel0.a, colorFilter.m_OverrideAlpha.x, x.x);
						color.m_ColorSet.m_Channel1.a = math.select(color.m_ColorSet.m_Channel1.a, colorFilter.m_OverrideAlpha.y, x.y);
						color.m_ColorSet.m_Channel2.a = math.select(color.m_ColorSet.m_Channel2.a, colorFilter.m_OverrideAlpha.z, x.z);
					}
					if (color.m_SyncFlags != ColorSyncFlags.None)
					{
						bool flag3 = true;
						if ((color.m_SyncFlags & ColorSyncFlags.SameGroup) != ColorSyncFlags.None)
						{
							flag3 = flag3 && flag;
						}
						if ((color.m_SyncFlags & ColorSyncFlags.DifferentGroup) != ColorSyncFlags.None)
						{
							flag3 = flag3 && !flag;
						}
						if ((color.m_SyncFlags & ColorSyncFlags.SameIndex) != ColorSyncFlags.None)
						{
							flag3 = flag3 && num == num2;
						}
						if ((color.m_SyncFlags & ColorSyncFlags.DifferentIndex) != ColorSyncFlags.None)
						{
							flag3 = flag3 && num != num2;
						}
						ref ColorData reference2 = ref reference.m_Unmatch;
						if (flag3)
						{
							reference2 = ref reference.m_Match;
						}
						reference2.m_Probability += color.m_Probability;
						if (random.NextInt(reference2.m_Probability) < color.m_Probability)
						{
							reference2.m_Color = color;
							reference2.m_Index = num;
							reference2.m_RandomSeed = randomSeed;
						}
					}
					else
					{
						reference.m_Unsync.m_Probability += color.m_Probability;
						if (random.NextInt(reference.m_Unsync.m_Probability) < color.m_Probability)
						{
							reference.m_Unsync.m_Color = color;
							reference.m_Unsync.m_Color.m_GroupID = new ColorGroupID(-1);
						}
					}
					num++;
				}
				Unity.Mathematics.Random random2 = random;
				random2.state += colors.m_SeedOffset;
				random2.state = math.select(random2.state, random.state, random2.state == 0);
				CalculateMeshColor(ref meshColor, ref syncValue, ref random2, ref searchData, ref colors, entity, colorIndex, flag2);
				if (num3 != 0f)
				{
					random2 = random;
					random2.state += colors2.m_SeedOffset;
					random2.state = math.select(random2.state, random.state, random2.state == 0);
					MeshColor meshColor2 = default(MeshColor);
					SyncData syncValue2 = new SyncData
					{
						m_GroupID = new ColorGroupID(-1),
						m_RandomSeed = 0u,
						m_ColorIndex = -1
					};
					CalculateMeshColor(ref meshColor2, ref syncValue2, ref random2, ref searchData, ref colors2, entity, colorIndex, flag2);
					if (colors2.m_Match.m_Probability > 0)
					{
						if (colors.m_Match.m_Probability > 0)
						{
							meshColor.m_ColorSet.m_Channel0 = UnityEngine.Color.Lerp(meshColor.m_ColorSet.m_Channel0, meshColor2.m_ColorSet.m_Channel0, num3);
							meshColor.m_ColorSet.m_Channel1 = UnityEngine.Color.Lerp(meshColor.m_ColorSet.m_Channel1, meshColor2.m_ColorSet.m_Channel1, num3);
							meshColor.m_ColorSet.m_Channel2 = UnityEngine.Color.Lerp(meshColor.m_ColorSet.m_Channel2, meshColor2.m_ColorSet.m_Channel2, num3);
						}
						else
						{
							meshColor.m_ColorSet = meshColor2.m_ColorSet;
						}
						syncData[syncIndex++] = syncValue2;
					}
				}
			}
			meshColors[colorIndex] = meshColor;
			syncData[syncIndex++] = syncValue;
		}

		private void CalculateMeshColor(ref MeshColor meshColor, ref SyncData syncValue, ref Unity.Mathematics.Random random, ref SearchData searchData, ref ColorDatas colors, Entity entity, int colorIndex, bool anyGroupUsed)
		{
			colors.m_Match.m_Probability += colors.m_Unmatch.m_Probability;
			if (!anyGroupUsed && random.NextInt(colors.m_Match.m_Probability) < colors.m_Unmatch.m_Probability)
			{
				colors.m_Match.m_Color = colors.m_Unmatch.m_Color;
				colors.m_Match.m_Index = colors.m_Unmatch.m_Index;
				colors.m_Match.m_RandomSeed = colors.m_Unmatch.m_RandomSeed;
			}
			colors.m_Match.m_Probability += colors.m_Unsync.m_Probability;
			if (random.NextInt(colors.m_Match.m_Probability) < colors.m_Unsync.m_Probability)
			{
				colors.m_Match.m_Color = colors.m_Unsync.m_Color;
				colors.m_Match.m_Index = -1;
				colors.m_Match.m_RandomSeed = 0u;
			}
			if (colors.m_Match.m_Probability <= 0)
			{
				return;
			}
			meshColor.m_ColorSet = colors.m_Match.m_Color.m_ColorSet;
			syncValue.m_GroupID = colors.m_Match.m_Color.m_GroupID;
			syncValue.m_RandomSeed = random.state;
			syncValue.m_ColorIndex = colors.m_Match.m_Index;
			if ((colors.m_Match.m_Color.m_SyncFlags & ColorSyncFlags.SyncRangeVariation) != ColorSyncFlags.None && colors.m_Match.m_RandomSeed != 0)
			{
				syncValue.m_RandomSeed = colors.m_Match.m_RandomSeed;
			}
			if (colors.m_Match.m_Color.hasExternalChannels)
			{
				if (!searchData.m_ExternalSearched)
				{
					searchData.m_ColorSource = FindExternalSource(entity, colors.m_Match.m_Color.m_ColorSourceType);
					searchData.m_ExternalSearched = true;
				}
				BrandData componentData;
				Game.Routes.Color componentData2;
				if (colors.m_Match.m_Color.m_ColorSourceType == ColorSourceType.Parent)
				{
					DynamicBuffer<MeshColor> bufferData;
					if (m_Stage == UpdateStage.Default || m_OwnerData.HasComponent(searchData.m_ColorSource) == (m_Stage == UpdateStage.IgnoreOwners))
					{
						if (searchData.m_ColorSource != Entity.Null)
						{
							m_CopyColors.Enqueue(new CopyColorData
							{
								m_Source = searchData.m_ColorSource,
								m_Target = entity,
								m_RandomSeed = syncValue.m_RandomSeed,
								m_ColorIndex = colorIndex,
								m_ExternalChannel0 = colors.m_Match.m_Color.m_ExternalChannel0,
								m_ExternalChannel1 = colors.m_Match.m_Color.m_ExternalChannel1,
								m_ExternalChannel2 = colors.m_Match.m_Color.m_ExternalChannel2,
								m_HueRange = colors.m_Match.m_Color.m_HueRange,
								m_SaturationRange = colors.m_Match.m_Color.m_SaturationRange,
								m_ValueRange = colors.m_Match.m_Color.m_ValueRange,
								m_AlphaRange0 = colors.m_Match.m_Color.m_AlphaRange0,
								m_AlphaRange1 = colors.m_Match.m_Color.m_AlphaRange1,
								m_AlphaRange2 = colors.m_Match.m_Color.m_AlphaRange2
							});
						}
					}
					else if (m_MeshColors.TryGetBuffer(searchData.m_ColorSource, out bufferData) && bufferData.Length != 0)
					{
						MeshColor meshColor2 = bufferData[math.min(colorIndex, bufferData.Length - 1)];
						for (int i = 0; i < 3; i++)
						{
							int externalChannelIndex = colors.m_Match.m_Color.GetExternalChannelIndex(i);
							if (externalChannelIndex >= 0)
							{
								meshColor.m_ColorSet[externalChannelIndex] = meshColor2.m_ColorSet[i];
							}
						}
					}
				}
				else if (m_BrandData.TryGetComponent(searchData.m_ColorSource, out componentData))
				{
					for (int j = 0; j < 3; j++)
					{
						int externalChannelIndex2 = colors.m_Match.m_Color.GetExternalChannelIndex(j);
						if (externalChannelIndex2 >= 0)
						{
							meshColor.m_ColorSet[externalChannelIndex2] = componentData.m_ColorSet[j];
						}
					}
				}
				else if (m_RouteColorData.TryGetComponent(searchData.m_ColorSource, out componentData2))
				{
					for (int k = 0; k < 3; k++)
					{
						int externalChannelIndex3 = colors.m_Match.m_Color.GetExternalChannelIndex(k);
						if (externalChannelIndex3 >= 0)
						{
							meshColor.m_ColorSet[externalChannelIndex3] = componentData2.m_Color;
						}
					}
				}
			}
			Unity.Mathematics.Random random2 = new Unity.Mathematics.Random
			{
				state = syncValue.m_RandomSeed
			};
			if (colors.m_Match.m_Color.hasVariationRanges)
			{
				float3 @float = new float3((int)colors.m_Match.m_Color.m_HueRange, (int)colors.m_Match.m_Color.m_SaturationRange, (int)colors.m_Match.m_Color.m_ValueRange) * 0.01f;
				float3 min = 1f - @float;
				float3 max = 1f + @float;
				RandomizeColor(ref meshColor.m_ColorSet.m_Channel0, ref random2, min, max);
				RandomizeColor(ref meshColor.m_ColorSet.m_Channel1, ref random2, min, max);
				RandomizeColor(ref meshColor.m_ColorSet.m_Channel2, ref random2, min, max);
			}
			if (colors.m_Match.m_Color.hasAlphaRanges)
			{
				float3 float2 = new float3((int)colors.m_Match.m_Color.m_AlphaRange0, (int)colors.m_Match.m_Color.m_AlphaRange1, (int)colors.m_Match.m_Color.m_AlphaRange2) * 0.01f;
				float3 min2 = -float2;
				float3 max2 = float2;
				RandomizeAlphas(ref meshColor.m_ColorSet, ref random2, min2, max2);
			}
			if ((colors.m_Match.m_Color.m_SyncFlags & ColorSyncFlags.SyncRangeVariation) == 0 || colors.m_Match.m_RandomSeed == 0)
			{
				random = random2;
			}
		}

		private void FindFilters(Entity entity, ref Game.Prefabs.AgeMask age, ref GenderMask gender)
		{
			PrefabRef prefabRef = m_PrefabRefData[entity];
			if (m_CreatureData.TryGetComponent(prefabRef.m_Prefab, out var componentData))
			{
				gender = componentData.m_Gender;
			}
			else
			{
				gender = GenderMask.Any;
			}
			if (m_ResidentData.TryGetComponent(prefabRef.m_Prefab, out var componentData2))
			{
				age = componentData2.m_Age;
			}
			else
			{
				age = Game.Prefabs.AgeMask.Any;
			}
		}

		private Entity FindExternalSource(Entity entity, ColorSourceType colorSourceType)
		{
			bool flag = false;
			bool flag2 = false;
			if (m_PlantData.HasComponent(entity))
			{
				return Entity.Null;
			}
			if (m_TempData.TryGetComponent(entity, out var componentData))
			{
				if (componentData.m_Original != Entity.Null)
				{
					entity = componentData.m_Original;
				}
				else
				{
					flag = true;
				}
			}
			if (m_ControllerData.TryGetComponent(entity, out var componentData2) && componentData2.m_Controller != Entity.Null)
			{
				entity = componentData2.m_Controller;
			}
			switch (colorSourceType)
			{
			case ColorSourceType.Brand:
			{
				if (m_Renters.TryGetBuffer(entity, out var bufferData))
				{
					if (FindBrand(bufferData, out var brand))
					{
						return brand;
					}
					flag2 = true;
				}
				if (m_CurrentRouteData.TryGetComponent(entity, out var componentData4) && m_RouteColorData.HasComponent(componentData4.m_Route))
				{
					return componentData4.m_Route;
				}
				if (m_RouteColorData.HasComponent(entity))
				{
					return entity;
				}
				Owner componentData5;
				while (m_OwnerData.TryGetComponent(entity, out componentData5))
				{
					entity = componentData5.m_Owner;
					if (flag && m_TempData.TryGetComponent(entity, out componentData) && componentData.m_Original != Entity.Null)
					{
						entity = componentData.m_Original;
						flag = false;
					}
					if (m_Renters.TryGetBuffer(entity, out bufferData))
					{
						if (FindBrand(bufferData, out var brand2))
						{
							return brand2;
						}
						flag2 = true;
					}
					if (m_CurrentRouteData.TryGetComponent(entity, out componentData4) && m_RouteColorData.HasComponent(componentData4.m_Route))
					{
						return componentData4.m_Route;
					}
					if (m_RouteColorData.HasComponent(entity))
					{
						return entity;
					}
				}
				if (flag2)
				{
					return m_DefaultBrand;
				}
				return Entity.Null;
			}
			case ColorSourceType.Parent:
			{
				Entity result = Entity.Null;
				Owner componentData3;
				while (m_OwnerData.TryGetComponent(entity, out componentData3))
				{
					entity = componentData3.m_Owner;
					if (flag && m_TempData.TryGetComponent(entity, out componentData) && componentData.m_Original != Entity.Null)
					{
						entity = componentData.m_Original;
						flag = false;
					}
					if (m_MeshColors.HasBuffer(entity))
					{
						result = entity;
					}
				}
				return result;
			}
			default:
				return Entity.Null;
			}
		}

		private bool FindBrand(DynamicBuffer<Renter> renters, out Entity brand)
		{
			for (int i = 0; i < renters.Length; i++)
			{
				Entity renter = renters[i].m_Renter;
				if (m_CompanyData.HasComponent(renter))
				{
					CompanyData companyData = m_CompanyData[renter];
					if (companyData.m_Brand != Entity.Null)
					{
						brand = companyData.m_Brand;
						return true;
					}
				}
			}
			brand = Entity.Null;
			return false;
		}
	}

	[BurstCompile]
	private struct CopyMeshColorsJob : IJob
	{
		public BufferLookup<MeshColor> m_MeshColors;

		public NativeQueue<CopyColorData> m_CopyColors;

		public void Execute()
		{
			CopyColorData item;
			while (m_CopyColors.TryDequeue(out item))
			{
				DynamicBuffer<MeshColor> dynamicBuffer = m_MeshColors[item.m_Source];
				DynamicBuffer<MeshColor> dynamicBuffer2 = m_MeshColors[item.m_Target];
				if (dynamicBuffer.Length == 0)
				{
					continue;
				}
				MeshColor meshColor = dynamicBuffer[math.min(item.m_ColorIndex, dynamicBuffer.Length - 1)];
				ref MeshColor reference = ref dynamicBuffer2.ElementAt(item.m_ColorIndex);
				Unity.Mathematics.Random random = new Unity.Mathematics.Random
				{
					state = item.m_RandomSeed
				};
				if (item.hasVariationRanges)
				{
					float3 @float = new float3((int)item.m_HueRange, (int)item.m_SaturationRange, (int)item.m_ValueRange) * 0.01f;
					float3 min = 1f - @float;
					float3 max = 1f + @float;
					RandomizeColor(ref meshColor.m_ColorSet.m_Channel0, ref random, min, max);
					RandomizeColor(ref meshColor.m_ColorSet.m_Channel1, ref random, min, max);
					RandomizeColor(ref meshColor.m_ColorSet.m_Channel2, ref random, min, max);
				}
				if (item.hasAlphaRanges)
				{
					float3 float2 = new float3((int)item.m_AlphaRange0, (int)item.m_AlphaRange1, (int)item.m_AlphaRange2) * 0.01f;
					float3 min2 = -float2;
					float3 max2 = float2;
					RandomizeAlphas(ref meshColor.m_ColorSet, ref random, min2, max2);
				}
				for (int i = 0; i < 3; i++)
				{
					int externalChannelIndex = item.GetExternalChannelIndex(i);
					if (externalChannelIndex >= 0)
					{
						reference.m_ColorSet[externalChannelIndex] = meshColor.m_ColorSet[i];
					}
				}
			}
		}
	}

	private struct TypeHandle
	{
		[ReadOnly]
		public EntityTypeHandle __Unity_Entities_Entity_TypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<RentersUpdated> __Game_Buildings_RentersUpdated_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<ColorUpdated> __Game_Routes_ColorUpdated_RO_ComponentTypeHandle;

		[ReadOnly]
		public BufferLookup<MeshColor> __Game_Rendering_MeshColor_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<Game.Objects.SubObject> __Game_Objects_SubObject_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<RouteVehicle> __Game_Routes_RouteVehicle_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<LayoutElement> __Game_Vehicles_LayoutElement_RO_BufferLookup;

		[ReadOnly]
		public ComponentLookup<Owner> __Game_Common_Owner_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PseudoRandomSeed> __Game_Common_PseudoRandomSeed_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Plant> __Game_Objects_Plant_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Controller> __Game_Vehicles_Controller_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<CurrentRoute> __Game_Routes_CurrentRoute_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Routes.Color> __Game_Routes_Color_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<CompanyData> __Game_Companies_CompanyData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Temp> __Game_Tools_Temp_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<BrandData> __Game_Prefabs_BrandData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<CreatureData> __Game_Prefabs_CreatureData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<ResidentData> __Game_Prefabs_ResidentData_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<MeshGroup> __Game_Rendering_MeshGroup_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<Renter> __Game_Buildings_Renter_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<SubMesh> __Game_Prefabs_SubMesh_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<SubMeshGroup> __Game_Prefabs_SubMeshGroup_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<ColorVariation> __Game_Prefabs_ColorVariation_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<ColorFilter> __Game_Prefabs_ColorFilter_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<OverlayElement> __Game_Prefabs_OverlayElement_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<CharacterElement> __Game_Prefabs_CharacterElement_RO_BufferLookup;

		public BufferLookup<MeshColor> __Game_Rendering_MeshColor_RW_BufferLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
			__Game_Buildings_RentersUpdated_RO_ComponentTypeHandle = state.GetComponentTypeHandle<RentersUpdated>(isReadOnly: true);
			__Game_Routes_ColorUpdated_RO_ComponentTypeHandle = state.GetComponentTypeHandle<ColorUpdated>(isReadOnly: true);
			__Game_Rendering_MeshColor_RO_BufferLookup = state.GetBufferLookup<MeshColor>(isReadOnly: true);
			__Game_Objects_SubObject_RO_BufferLookup = state.GetBufferLookup<Game.Objects.SubObject>(isReadOnly: true);
			__Game_Routes_RouteVehicle_RO_BufferLookup = state.GetBufferLookup<RouteVehicle>(isReadOnly: true);
			__Game_Vehicles_LayoutElement_RO_BufferLookup = state.GetBufferLookup<LayoutElement>(isReadOnly: true);
			__Game_Common_Owner_RO_ComponentLookup = state.GetComponentLookup<Owner>(isReadOnly: true);
			__Game_Common_PseudoRandomSeed_RO_ComponentLookup = state.GetComponentLookup<PseudoRandomSeed>(isReadOnly: true);
			__Game_Objects_Plant_RO_ComponentLookup = state.GetComponentLookup<Plant>(isReadOnly: true);
			__Game_Vehicles_Controller_RO_ComponentLookup = state.GetComponentLookup<Controller>(isReadOnly: true);
			__Game_Routes_CurrentRoute_RO_ComponentLookup = state.GetComponentLookup<CurrentRoute>(isReadOnly: true);
			__Game_Routes_Color_RO_ComponentLookup = state.GetComponentLookup<Game.Routes.Color>(isReadOnly: true);
			__Game_Companies_CompanyData_RO_ComponentLookup = state.GetComponentLookup<CompanyData>(isReadOnly: true);
			__Game_Tools_Temp_RO_ComponentLookup = state.GetComponentLookup<Temp>(isReadOnly: true);
			__Game_Prefabs_PrefabRef_RO_ComponentLookup = state.GetComponentLookup<PrefabRef>(isReadOnly: true);
			__Game_Prefabs_BrandData_RO_ComponentLookup = state.GetComponentLookup<BrandData>(isReadOnly: true);
			__Game_Prefabs_CreatureData_RO_ComponentLookup = state.GetComponentLookup<CreatureData>(isReadOnly: true);
			__Game_Prefabs_ResidentData_RO_ComponentLookup = state.GetComponentLookup<ResidentData>(isReadOnly: true);
			__Game_Rendering_MeshGroup_RO_BufferLookup = state.GetBufferLookup<MeshGroup>(isReadOnly: true);
			__Game_Buildings_Renter_RO_BufferLookup = state.GetBufferLookup<Renter>(isReadOnly: true);
			__Game_Prefabs_SubMesh_RO_BufferLookup = state.GetBufferLookup<SubMesh>(isReadOnly: true);
			__Game_Prefabs_SubMeshGroup_RO_BufferLookup = state.GetBufferLookup<SubMeshGroup>(isReadOnly: true);
			__Game_Prefabs_ColorVariation_RO_BufferLookup = state.GetBufferLookup<ColorVariation>(isReadOnly: true);
			__Game_Prefabs_ColorFilter_RO_BufferLookup = state.GetBufferLookup<ColorFilter>(isReadOnly: true);
			__Game_Prefabs_OverlayElement_RO_BufferLookup = state.GetBufferLookup<OverlayElement>(isReadOnly: true);
			__Game_Prefabs_CharacterElement_RO_BufferLookup = state.GetBufferLookup<CharacterElement>(isReadOnly: true);
			__Game_Rendering_MeshColor_RW_BufferLookup = state.GetBufferLookup<MeshColor>();
		}
	}

	private ClimateSystem m_ClimateSystem;

	private SimulationSystem m_SimulationSystem;

	private PrefabSystem m_PrefabSystem;

	private RenderPrefabBase m_OverridePrefab;

	private Dictionary<string, int> m_GroupIDs;

	private EntityQuery m_UpdateQuery;

	private EntityQuery m_AllQuery;

	private EntityQuery m_PlantQuery;

	private EntityQuery m_BuildingSettingsQuery;

	private Entity m_LastSeason1;

	private Entity m_LastSeason2;

	private Entity m_OverrideEntity;

	private uint m_LastUpdateGroup;

	private uint m_UpdateGroupCount;

	private int m_OverrideIndex;

	private float m_LastSeasonBlend;

	private bool m_Loaded;

	private TypeHandle __TypeHandle;

	public bool smoothColorsUpdated { get; private set; }

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_ClimateSystem = base.World.GetOrCreateSystemManaged<ClimateSystem>();
		m_SimulationSystem = base.World.GetOrCreateSystemManaged<SimulationSystem>();
		m_PrefabSystem = base.World.GetOrCreateSystemManaged<PrefabSystem>();
		m_UpdateQuery = GetEntityQuery(new EntityQueryDesc
		{
			All = new ComponentType[1] { ComponentType.ReadOnly<MeshColor>() },
			Any = new ComponentType[3]
			{
				ComponentType.ReadOnly<Updated>(),
				ComponentType.ReadOnly<BatchesUpdated>(),
				ComponentType.ReadOnly<Deleted>()
			}
		}, new EntityQueryDesc
		{
			All = new ComponentType[1] { ComponentType.ReadOnly<Game.Common.Event>() },
			Any = new ComponentType[2]
			{
				ComponentType.ReadOnly<RentersUpdated>(),
				ComponentType.ReadOnly<ColorUpdated>()
			}
		});
		m_AllQuery = GetEntityQuery(ComponentType.ReadOnly<MeshColor>());
		m_PlantQuery = GetEntityQuery(ComponentType.ReadOnly<MeshColor>(), ComponentType.ReadOnly<Plant>(), ComponentType.ReadOnly<UpdateFrame>());
		m_BuildingSettingsQuery = GetEntityQuery(ComponentType.ReadOnly<BuildingConfigurationData>());
		m_GroupIDs = new Dictionary<string, int>();
	}

	protected override void OnGameLoaded(Context serializationContext)
	{
		m_Loaded = true;
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

	public void SetOverride(Entity entity, RenderPrefabBase prefab, int variationIndex)
	{
		if (m_OverrideEntity != entity && m_OverrideEntity != Entity.Null && base.EntityManager.Exists(m_OverrideEntity) && !base.EntityManager.HasComponent<Deleted>(m_OverrideEntity))
		{
			base.World.GetExistingSystemManaged<EndFrameBarrier>().CreateCommandBuffer().AddComponent<BatchesUpdated>(m_OverrideEntity);
		}
		m_OverrideEntity = entity;
		m_OverridePrefab = prefab;
		m_OverrideIndex = variationIndex;
	}

	[Preserve]
	protected override void OnUpdate()
	{
		bool flag = GetLoaded() && !m_AllQuery.IsEmptyIgnoreFilter;
		bool flag2 = !flag && !m_UpdateQuery.IsEmptyIgnoreFilter;
		uint num = (m_SimulationSystem.frameIndex >> 9) & 0xF;
		Entity currentClimate = m_ClimateSystem.currentClimate;
		smoothColorsUpdated = !flag && !m_PlantQuery.IsEmptyIgnoreFilter;
		if (currentClimate != Entity.Null)
		{
			ClimatePrefab prefab = m_PrefabSystem.GetPrefab<ClimatePrefab>(m_ClimateSystem.currentClimate);
			float num2 = m_ClimateSystem.currentDate;
			var (seasonInfo, num3, num4) = prefab.FindSeasonByTime(num2);
			if (num2 < num3)
			{
				num2 += 1f;
			}
			float num5 = (num3 + num4) * 0.5f;
			ClimateSystem.SeasonInfo seasonInfo2;
			float num6;
			float num7;
			if (num2 < num5)
			{
				num6 = num3 - 0.001f;
				if (num6 < 0f)
				{
					num6 += 1f;
				}
				(seasonInfo2, num6, num7) = prefab.FindSeasonByTime(num6);
				if (num6 > num3)
				{
					num5 += 1f;
					num2 += 1f;
				}
			}
			else
			{
				num7 = num4 + 0.001f;
				if (num7 >= 1f)
				{
					num7 -= 1f;
				}
				(seasonInfo2, num6, num7) = prefab.FindSeasonByTime(num7);
				if (num6 < num3)
				{
					num6 += 1f;
					num7 += 1f;
				}
			}
			float xMax = (num6 + num7) * 0.5f;
			float num8 = math.round(math.smoothstep(num5, xMax, num2) * 1600f) * 0.000625f;
			Entity entity = ((seasonInfo != null) ? m_PrefabSystem.GetEntity(seasonInfo.m_Prefab) : Entity.Null);
			Entity entity2 = ((seasonInfo2 != null) ? m_PrefabSystem.GetEntity(seasonInfo2.m_Prefab) : Entity.Null);
			if (entity != m_LastSeason1 || entity2 != m_LastSeason2 || num8 != m_LastSeasonBlend)
			{
				m_LastSeason1 = entity;
				m_LastSeason2 = entity2;
				m_LastSeasonBlend = num8;
				m_UpdateGroupCount = 16u;
			}
			if (m_UpdateGroupCount != 0 && m_LastUpdateGroup != num)
			{
				m_UpdateGroupCount--;
			}
			else
			{
				smoothColorsUpdated = false;
			}
		}
		m_LastUpdateGroup = num;
		if (flag || flag2 || smoothColorsUpdated)
		{
			NativeList<Entity> list;
			JobHandle outJobHandle;
			if (flag)
			{
				list = m_AllQuery.ToEntityListAsync(Allocator.TempJob, out outJobHandle);
			}
			else if (smoothColorsUpdated)
			{
				m_PlantQuery.ResetFilter();
				m_PlantQuery.SetSharedComponentFilter(new UpdateFrame
				{
					m_Index = num
				});
				list = m_PlantQuery.ToEntityListAsync(Allocator.TempJob, out outJobHandle);
			}
			else
			{
				list = new NativeList<Entity>(Allocator.TempJob);
				outJobHandle = default(JobHandle);
			}
			if (flag2)
			{
				NativeQueue<Entity> queue = new NativeQueue<Entity>(Allocator.TempJob);
				FindUpdatedMeshColorsJob jobData = new FindUpdatedMeshColorsJob
				{
					m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
					m_RentersUpdatedType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Buildings_RentersUpdated_RO_ComponentTypeHandle, ref base.CheckedStateRef),
					m_ColorUpdatedType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Routes_ColorUpdated_RO_ComponentTypeHandle, ref base.CheckedStateRef),
					m_MeshColors = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Rendering_MeshColor_RO_BufferLookup, ref base.CheckedStateRef),
					m_SubObjects = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Objects_SubObject_RO_BufferLookup, ref base.CheckedStateRef),
					m_RouteVehicles = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Routes_RouteVehicle_RO_BufferLookup, ref base.CheckedStateRef),
					m_LayoutElements = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Vehicles_LayoutElement_RO_BufferLookup, ref base.CheckedStateRef),
					m_Queue = queue.AsParallelWriter()
				};
				JobHandle jobHandle = IJobExtensions.Schedule(new ListUpdatedMeshColorsJob
				{
					m_Queue = queue,
					m_List = list
				}, JobHandle.CombineDependencies(job1: JobChunkExtensions.ScheduleParallel(jobData, m_UpdateQuery, base.Dependency), job0: outJobHandle));
				outJobHandle = jobHandle;
				queue.Dispose(jobHandle);
			}
			else
			{
				outJobHandle = JobHandle.CombineDependencies(outJobHandle, base.Dependency);
			}
			Entity entity3 = Entity.Null;
			if (m_OverridePrefab != null)
			{
				m_PrefabSystem.TryGetEntity(m_OverridePrefab, out entity3);
			}
			NativeQueue<CopyColorData> copyColors = new NativeQueue<CopyColorData>(Allocator.TempJob);
			SetMeshColorsJob jobData2 = new SetMeshColorsJob
			{
				m_RandomSeed = RandomSeed.Next(),
				m_DefaultBrand = m_BuildingSettingsQuery.GetSingleton<BuildingConfigurationData>().m_DefaultRenterBrand,
				m_Season1 = m_LastSeason1,
				m_Season2 = m_LastSeason2,
				m_SeasonBlend = m_LastSeasonBlend,
				m_OverrideEntity = m_OverrideEntity,
				m_OverrideMesh = entity3,
				m_OverrideIndex = m_OverrideIndex,
				m_Stage = (flag ? UpdateStage.IgnoreSubs : UpdateStage.Default),
				m_Entities = list.AsDeferredJobArray(),
				m_OwnerData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Common_Owner_RO_ComponentLookup, ref base.CheckedStateRef),
				m_PseudoRandomSeedData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Common_PseudoRandomSeed_RO_ComponentLookup, ref base.CheckedStateRef),
				m_PlantData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Plant_RO_ComponentLookup, ref base.CheckedStateRef),
				m_ControllerData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Vehicles_Controller_RO_ComponentLookup, ref base.CheckedStateRef),
				m_CurrentRouteData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Routes_CurrentRoute_RO_ComponentLookup, ref base.CheckedStateRef),
				m_RouteColorData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Routes_Color_RO_ComponentLookup, ref base.CheckedStateRef),
				m_CompanyData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Companies_CompanyData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_TempData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Tools_Temp_RO_ComponentLookup, ref base.CheckedStateRef),
				m_PrefabRefData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup, ref base.CheckedStateRef),
				m_BrandData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_BrandData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_CreatureData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_CreatureData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_ResidentData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_ResidentData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_MeshGroups = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Rendering_MeshGroup_RO_BufferLookup, ref base.CheckedStateRef),
				m_Renters = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Buildings_Renter_RO_BufferLookup, ref base.CheckedStateRef),
				m_SubMeshes = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Prefabs_SubMesh_RO_BufferLookup, ref base.CheckedStateRef),
				m_SubMeshGroups = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Prefabs_SubMeshGroup_RO_BufferLookup, ref base.CheckedStateRef),
				m_ColorVariations = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Prefabs_ColorVariation_RO_BufferLookup, ref base.CheckedStateRef),
				m_ColorFilters = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Prefabs_ColorFilter_RO_BufferLookup, ref base.CheckedStateRef),
				m_OverlayElements = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Prefabs_OverlayElement_RO_BufferLookup, ref base.CheckedStateRef),
				m_CharacterElements = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Prefabs_CharacterElement_RO_BufferLookup, ref base.CheckedStateRef),
				m_MeshColors = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Rendering_MeshColor_RW_BufferLookup, ref base.CheckedStateRef),
				m_CopyColors = copyColors.AsParallelWriter()
			};
			CopyMeshColorsJob jobData3 = new CopyMeshColorsJob
			{
				m_MeshColors = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Rendering_MeshColor_RW_BufferLookup, ref base.CheckedStateRef),
				m_CopyColors = copyColors
			};
			JobHandle jobHandle2 = jobData2.Schedule(list, 4, outJobHandle);
			if (flag)
			{
				jobData2.m_Stage = UpdateStage.IgnoreOwners;
				jobHandle2 = jobData2.Schedule(list, 4, jobHandle2);
			}
			JobHandle jobHandle3 = IJobExtensions.Schedule(jobData3, jobHandle2);
			list.Dispose(jobHandle2);
			copyColors.Dispose(jobHandle3);
			base.Dependency = jobHandle3;
		}
	}

	public ColorGroupID GetColorGroupID(string name)
	{
		int value = -1;
		if (!string.IsNullOrEmpty(name) && !m_GroupIDs.TryGetValue(name, out value))
		{
			value = m_GroupIDs.Count;
			m_GroupIDs.Add(name, value);
		}
		return new ColorGroupID(value);
	}

	private static void RandomizeColor(ref UnityEngine.Color color, ref Unity.Mathematics.Random random, float3 min, float3 max)
	{
		float3 @float = default(float3);
		UnityEngine.Color.RGBToHSV(color, out @float.x, out @float.y, out @float.z);
		float a = color.a;
		float3 float2 = random.NextFloat3(min, max);
		@float.x = math.frac(@float.x + float2.x);
		@float.yz = math.saturate(@float.yz * float2.yz);
		color = UnityEngine.Color.HSVToRGB(@float.x, @float.y, @float.z);
		color.a = a;
	}

	private static void RandomizeAlphas(ref ColorSet colorSet, ref Unity.Mathematics.Random random, float3 min, float3 max)
	{
		float3 @float = new float3(colorSet.m_Channel0.a, colorSet.m_Channel1.a, colorSet.m_Channel2.a);
		float3 float2 = random.NextFloat3(min, max);
		@float = math.saturate(@float + float2);
		colorSet.m_Channel0.a = @float.x;
		colorSet.m_Channel1.a = @float.y;
		colorSet.m_Channel2.a = @float.z;
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
	public MeshColorSystem()
	{
	}
}
