using System;
using System.Collections.Generic;
using System.Linq;
using Colossal;
using Colossal.Entities;
using Colossal.Mathematics;
using Colossal.Rendering;
using Game.Buildings;
using Game.Citizens;
using Game.Creatures;
using Game.Objects;
using Game.Prefabs;
using Game.Rendering;
using Game.SceneFlow;
using Game.Tools;
using Game.Vehicles;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

namespace Game.Debug;

[DebugContainer]
public class DebugSystemSearch
{
	private const int kPageItems = 10;

	private Dictionary<ComponentType, DebugUI.Button> m_All = new Dictionary<ComponentType, DebugUI.Button>();

	private Dictionary<ComponentType, DebugUI.Button> m_Any = new Dictionary<ComponentType, DebugUI.Button>();

	private Dictionary<ComponentType, DebugUI.Button> m_None = new Dictionary<ComponentType, DebugUI.Button>();

	private bool m_DeepSearch;

	private string m_SearchString = string.Empty;

	private bool m_SearchPrefabName = true;

	private bool m_SearchMeshName;

	private bool m_UseBatchMesh = true;

	private bool m_MustHaveTransform = true;

	private int m_LastIndex;

	private bool m_SearchLocalizedPrefabName = true;

	private bool m_SearchActivityName;

	private DebugUI.IContainer m_AllComponents;

	private DebugUI.IContainer m_AnyComponents;

	private DebugUI.IContainer m_NoneComponents;

	private DebugUI.Foldout m_Result;

	private DebugUI.IntField m_Page;

	private int m_SelectedPage;

	private int m_SelectedEntity;

	private int m_GroupIndex;

	private EntityManager EntityManager;

	private Entity[] m_SearchResults = Array.Empty<Entity>();

	private CameraUpdateSystem m_CameraUpdateSystem;

	private PrefabSystem m_PrefabSystem;

	private BatchManagerSystem m_BatchManagerSystem;

	private ToolSystem m_ToolSystem;

	[DebugTab("Search", -10000)]
	private List<DebugUI.Widget> BuildSearchDebugUI(World world)
	{
		m_SearchString = string.Empty;
		m_SelectedPage = 0;
		m_SelectedEntity = 0;
		EntityManager = world.EntityManager;
		m_CameraUpdateSystem = world.GetOrCreateSystemManaged<CameraUpdateSystem>();
		m_PrefabSystem = world.GetOrCreateSystemManaged<PrefabSystem>();
		m_BatchManagerSystem = world.GetOrCreateSystemManaged<BatchManagerSystem>();
		m_ToolSystem = world.GetOrCreateSystemManaged<ToolSystem>();
		DebugUI.Container container = new DebugUI.Container();
		container.children.Add(new DebugUI.TextField
		{
			displayName = "Search string",
			getter = () => m_SearchString,
			setter = delegate(string value)
			{
				m_SearchString = value;
				m_LastIndex = 0;
			}
		});
		container.children.Add(new DebugUI.BoolField
		{
			displayName = "Deep search",
			getter = () => m_DeepSearch,
			setter = delegate(bool value)
			{
				m_DeepSearch = value;
			}
		});
		container.children.Add(new DebugUI.BoolField
		{
			displayName = "Search prefab name",
			getter = () => m_SearchPrefabName,
			setter = delegate(bool value)
			{
				m_SearchPrefabName = value;
			}
		});
		container.children.Add(new DebugUI.BoolField
		{
			displayName = "Search localized prefab name",
			getter = () => m_SearchLocalizedPrefabName,
			setter = delegate(bool value)
			{
				m_SearchLocalizedPrefabName = value;
			}
		});
		container.children.Add(new DebugUI.BoolField
		{
			displayName = "Search mesh name",
			getter = () => m_SearchMeshName,
			setter = delegate(bool value)
			{
				m_SearchMeshName = value;
			}
		});
		container.children.Add(new DebugUI.BoolField
		{
			displayName = "Use batch meshes",
			getter = () => m_UseBatchMesh,
			setter = delegate(bool value)
			{
				m_UseBatchMesh = value;
			}
		});
		container.children.Add(new DebugUI.BoolField
		{
			displayName = "Search activity name",
			getter = () => m_SearchActivityName,
			setter = delegate(bool value)
			{
				m_SearchActivityName = value;
			}
		});
		container.children.Add(new DebugUI.BoolField
		{
			displayName = "Must have transform",
			getter = () => m_MustHaveTransform,
			setter = delegate(bool value)
			{
				m_MustHaveTransform = value;
			}
		});
		DebugUI.Foldout foldout = new DebugUI.Foldout
		{
			displayName = "FILTERS"
		};
		container.children.Add(foldout);
		m_AllComponents = AddFilter(foldout, "All Components filter", m_All);
		m_AnyComponents = AddFilter(foldout, "Any Components filter", m_Any);
		m_NoneComponents = AddFilter(foldout, "None Components filter", m_None);
		DebugUI.Foldout foldout2 = new DebugUI.Foldout
		{
			displayName = "Templates"
		};
		foldout.children.Add(foldout2);
		foldout2.children.Add(new DebugUI.Button
		{
			displayName = "Buildings",
			action = delegate
			{
				ClearComponents(m_AllComponents, m_All, apply: false);
				AddComponent(m_AllComponents, m_All, typeof(Building), apply: false);
				AddComponent(m_AllComponents, m_All, typeof(PrefabRef), apply: false);
			}
		});
		foldout2.children.Add(new DebugUI.Button
		{
			displayName = "Vehicles",
			action = delegate
			{
				ClearComponents(m_AllComponents, m_All, apply: false);
				AddComponent(m_AllComponents, m_All, typeof(Vehicle), apply: false);
				AddComponent(m_AllComponents, m_All, typeof(PrefabRef), apply: false);
			}
		});
		foldout2.children.Add(new DebugUI.Button
		{
			displayName = "Human",
			action = delegate
			{
				ClearComponents(m_AllComponents, m_All, apply: false);
				AddComponent(m_AllComponents, m_All, typeof(Human), apply: false);
				AddComponent(m_AllComponents, m_All, typeof(PrefabRef), apply: false);
				AddComponent(m_NoneComponents, m_None, typeof(Unspawned), apply: false);
			}
		});
		foldout2.children.Add(new DebugUI.Button
		{
			displayName = "Meshes",
			action = delegate
			{
				ClearComponents(m_AllComponents, m_All, apply: false);
				AddComponent(m_AllComponents, m_All, typeof(Vehicle), apply: false);
				AddComponent(m_AllComponents, m_All, typeof(PrefabRef), apply: false);
			}
		});
		container.children.Add(new DebugUI.Button
		{
			displayName = "APPLY SEARCH",
			action = Apply
		});
		container.children.Add(new DebugUI.Button
		{
			displayName = "Previous Entity",
			action = PrevEntity
		});
		container.children.Add(new DebugUI.Button
		{
			displayName = "Next Entity",
			action = NextEntity
		});
		ObservableList<DebugUI.Widget> children = container.children;
		DebugUI.IntField obj = new DebugUI.IntField
		{
			displayName = "Page",
			getter = () => m_SelectedPage + 1,
			setter = delegate(int value)
			{
				m_SelectedPage = value - 1;
				ShowResults();
			},
			min = () => 1,
			max = () => Math.Max((int)math.ceil((float)m_SearchResults.Length / 10f), 1)
		};
		DebugUI.IntField item = obj;
		m_Page = obj;
		children.Add(item);
		container.children.Add(m_Result = new DebugUI.Foldout
		{
			displayName = "Results"
		});
		return new List<DebugUI.Widget> { container };
	}

	private DebugUI.IContainer AddFilter(DebugUI.IContainer container, string dispayName, Dictionary<ComponentType, DebugUI.Button> items)
	{
		string search = string.Empty;
		DebugUI.Foldout filter = new DebugUI.Foldout
		{
			displayName = dispayName
		};
		filter.children.Add(new DebugUI.TextField
		{
			displayName = "Component type name",
			getter = () => search,
			setter = delegate(string value)
			{
				search = value;
			}
		});
		filter.children.Add(new DebugUI.Button
		{
			displayName = "Add Component to filter",
			action = delegate
			{
				Type type = Type.GetType(search);
				AddComponent(filter, items, type);
			}
		});
		filter.children.Add(new DebugUI.Button
		{
			displayName = "Clear Component filter",
			action = delegate
			{
				ClearComponents(filter, items);
			}
		});
		container.children.Add(filter);
		return filter;
	}

	private void AddComponent(DebugUI.IContainer container, Dictionary<ComponentType, DebugUI.Button> components, Type type, bool apply = true)
	{
		if (!(type == null) && IsSupportedComponentType(type))
		{
			ComponentType component = ComponentType.ReadOnly(type);
			AddComponent(container, components, component, apply);
		}
	}

	private void AddComponent(DebugUI.IContainer container, Dictionary<ComponentType, DebugUI.Button> components, ComponentType component, bool apply = true)
	{
		if (!components.ContainsKey(component))
		{
			DebugUI.Button button = new DebugUI.Button
			{
				displayName = TypeManager.GetType(component.TypeIndex).Name,
				action = delegate
				{
					RemoveComponent(container, components, component);
				}
			};
			container.children.Insert(components.Count, button);
			components.Add(component, button);
			if (apply)
			{
				Apply();
			}
		}
	}

	private void RemoveComponent(DebugUI.IContainer container, Dictionary<ComponentType, DebugUI.Button> components, ComponentType component, bool apply = true)
	{
		if (components.TryGetValue(component, out var value))
		{
			container.children.Remove(value);
		}
		components.Remove(component);
		if (apply)
		{
			Apply();
		}
	}

	private void ClearComponents(DebugUI.IContainer container, Dictionary<ComponentType, DebugUI.Button> components, bool apply = true)
	{
		foreach (var (_, item) in components)
		{
			container.children.Remove(item);
		}
		components.Clear();
		if (apply)
		{
			Apply();
		}
	}

	private bool MatchGroups(Entity entity, HashSet<Entity> desired, ref HashSet<Entity> results)
	{
		if (EntityManager.TryGetComponent<CurrentTransport>(entity, out var component))
		{
			entity = component.m_CurrentTransport;
		}
		if (EntityManager.HasComponent<MeshGroup>(entity) && EntityManager.TryGetBuffer(entity, isReadOnly: true, out DynamicBuffer<MeshGroup> buffer) && EntityManager.TryGetComponent<PrefabRef>(entity, out var component2) && m_PrefabSystem.TryGetPrefab<ObjectGeometryPrefab>(component2, out var prefab) && prefab.m_Meshes != null)
		{
			EntityManager.TryGetComponent<CreatureData>(component2, out var component3);
			for (int i = 0; i < buffer.Length; i++)
			{
				int subMeshGroup = buffer[i].m_SubMeshGroup;
				for (int j = 0; j < prefab.m_Meshes.Length; j++)
				{
					if (!(prefab.m_Meshes[j].m_Mesh is CharacterGroup { m_Characters: not null } characterGroup))
					{
						continue;
					}
					for (int k = 0; k < characterGroup.m_Characters.Length; k++)
					{
						if ((characterGroup.m_Characters[k].m_Style.m_Gender & component3.m_Gender) == component3.m_Gender && subMeshGroup-- == 0 && desired.Contains(m_PrefabSystem.GetEntity(characterGroup)) && (m_GroupIndex == -1 || m_GroupIndex == k))
						{
							return AddToResults(ref results, entity);
						}
					}
					if (characterGroup.m_Overrides == null)
					{
						continue;
					}
					for (int l = 0; l < characterGroup.m_Overrides.Length; l++)
					{
						for (int m = 0; m < characterGroup.m_Characters.Length; m++)
						{
							if ((characterGroup.m_Characters[m].m_Style.m_Gender & component3.m_Gender) == component3.m_Gender && subMeshGroup-- == 0 && desired.Contains(m_PrefabSystem.GetEntity(characterGroup)) && (m_GroupIndex == -1 || m_GroupIndex == m))
							{
								return AddToResults(ref results, entity);
							}
						}
					}
				}
			}
		}
		return false;
	}

	private bool MatchRenderPrefab(Entity entity, HashSet<Entity> desired, ref HashSet<Entity> results)
	{
		if (EntityManager.TryGetComponent<CurrentTransport>(entity, out var component))
		{
			entity = component.m_CurrentTransport;
		}
		if (EntityManager.HasComponent<MeshBatch>(entity) && EntityManager.TryGetBuffer(entity, isReadOnly: true, out DynamicBuffer<MeshBatch> buffer))
		{
			JobHandle dependencies;
			NativeBatchGroups<CullingData, GroupData, BatchData, InstanceData> nativeBatchGroups = m_BatchManagerSystem.GetNativeBatchGroups(readOnly: true, out dependencies);
			JobHandle dependencies2;
			NativeBatchInstances<CullingData, GroupData, BatchData, InstanceData> nativeBatchInstances = m_BatchManagerSystem.GetNativeBatchInstances(readOnly: true, out dependencies2);
			ManagedBatches<OptionalProperties> managedBatches = m_BatchManagerSystem.GetManagedBatches();
			dependencies.Complete();
			dependencies2.Complete();
			for (int i = 0; i < buffer.Length; i++)
			{
				MeshBatch meshBatch = buffer[i];
				int batchCount = nativeBatchGroups.GetBatchCount(meshBatch.m_GroupIndex);
				int mergedInstanceGroupIndex = nativeBatchInstances.GetMergedInstanceGroupIndex(meshBatch.m_GroupIndex, meshBatch.m_InstanceIndex);
				for (int j = 0; j < batchCount; j++)
				{
					int managedBatchIndex = nativeBatchGroups.GetManagedBatchIndex(meshBatch.m_GroupIndex, j);
					int num = -1;
					if (mergedInstanceGroupIndex >= 0)
					{
						num = nativeBatchGroups.GetManagedBatchIndex(mergedInstanceGroupIndex, j);
					}
					if (managedBatchIndex < 0)
					{
						continue;
					}
					CustomBatch customBatch = (CustomBatch)managedBatches.GetBatch(managedBatchIndex);
					if (num >= 0)
					{
						CustomBatch customBatch2 = (CustomBatch)managedBatches.GetBatch(num);
						if (desired.Contains(customBatch.sourceMeshEntity) || desired.Contains(customBatch2.sourceMeshEntity))
						{
							return AddToResults(ref results, entity);
						}
					}
					else if (desired.Contains(customBatch.sourceMeshEntity))
					{
						return AddToResults(ref results, entity);
					}
				}
			}
		}
		return false;
	}

	private bool MatchActivity(Entity entity, string searchString, ref HashSet<Entity> results)
	{
		if (EntityManager.TryGetComponent<CurrentTransport>(entity, out var component))
		{
			entity = component.m_CurrentTransport;
		}
		if (EntityManager.TryGetComponent<HumanNavigation>(entity, out var component2) && (Enum.GetName(typeof(ActivityType), component2.m_TargetActivity).CaseInsensitiveContains(searchString) || Enum.GetName(typeof(ActivityType), component2.m_LastActivity).CaseInsensitiveContains(searchString)))
		{
			return AddToResults(ref results, entity);
		}
		return false;
	}

	private bool MatchSearch(string searchString, Entity entity, HashSet<Entity> desired, ref HashSet<Entity> results)
	{
		if (desired.Contains(entity))
		{
			return AddToResults(ref results, entity);
		}
		if (m_UseBatchMesh && m_SearchMeshName)
		{
			return MatchRenderPrefab(entity, desired, ref results);
		}
		if (MatchGroups(entity, desired, ref results))
		{
			return true;
		}
		if (m_SearchActivityName && MatchActivity(entity, searchString, ref results))
		{
			return true;
		}
		if (EntityManager.TryGetComponent<PrefabRef>(entity, out var component))
		{
			if (desired.Contains(component))
			{
				return AddToResults(ref results, entity);
			}
			if (m_SearchMeshName && EntityManager.TryGetBuffer(component.m_Prefab, isReadOnly: true, out DynamicBuffer<SubMesh> buffer))
			{
				foreach (SubMesh item in buffer)
				{
					if (desired.Contains(item.m_SubMesh))
					{
						return AddToResults(ref results, entity);
					}
				}
			}
		}
		return false;
	}

	private bool AddToResults(ref HashSet<Entity> results, Entity entity)
	{
		if (m_MustHaveTransform && !EntityManager.HasComponent<Game.Objects.Transform>(entity))
		{
			return false;
		}
		results.Add(entity);
		return true;
	}

	private void Apply()
	{
		HashSet<Entity> results = new HashSet<Entity>();
		if (m_All.Count == 0 && m_Any.Count == 0 && m_None.Count == 0 && string.IsNullOrEmpty(m_SearchString))
		{
			return;
		}
		EntityQuery entityQuery = EntityManager.CreateEntityQuery(new EntityQueryDesc
		{
			All = m_All.Keys.ToArray(),
			Any = m_Any.Keys.ToArray(),
			None = m_None.Keys.ToArray()
		});
		m_SelectedPage = 0;
		m_SelectedEntity = 0;
		NativeArray<Entity> nativeArray = entityQuery.ToEntityArray(Allocator.Temp);
		string text = m_SearchString;
		m_GroupIndex = -1;
		int num = m_SearchString.IndexOf(" #", StringComparison.Ordinal);
		if (num > -1)
		{
			text = m_SearchString.Substring(0, num);
			if (int.TryParse(m_SearchString.Substring(num + 2), out var result))
			{
				m_GroupIndex = result;
			}
		}
		HashSet<Entity> hashSet = new HashSet<Entity>();
		foreach (PrefabBase prefab in m_PrefabSystem.prefabs)
		{
			bool flag = (m_SearchMeshName && prefab is RenderPrefab) || (m_SearchPrefabName && !(prefab is RenderPrefab));
			if (!prefab.name.Contains("_LOD"))
			{
				bool flag2 = false;
				if (m_SearchPrefabName || m_SearchMeshName)
				{
					flag2 |= prefab.name.CaseInsensitiveContains(text);
				}
				if (m_SearchLocalizedPrefabName && GameManager.instance.localizationManager.activeDictionary.TryGetValue("Assets.NAME[" + prefab.name + "]", out var value))
				{
					flag2 |= value.CaseInsensitiveContains(text);
				}
				if (flag && flag2)
				{
					hashSet.Add(m_PrefabSystem.GetEntity(prefab));
				}
			}
		}
		if (m_DeepSearch || m_LastIndex >= nativeArray.Length)
		{
			m_LastIndex = 0;
		}
		int lastIndex = m_LastIndex;
		for (int i = lastIndex; i < nativeArray.Length; i++)
		{
			Entity entity = nativeArray[i];
			if (MatchSearch(text, entity, hashSet, ref results) && !m_DeepSearch)
			{
				m_LastIndex = i + 1;
				break;
			}
		}
		string arg = ((m_LastIndex == 0) ? nativeArray.Length.ToString() : $"{m_LastIndex}/{nativeArray.Length}");
		nativeArray.Dispose();
		if (results.Count == 0)
		{
			m_LastIndex = 0;
		}
		if (!m_DeepSearch)
		{
			results.UnionWith(m_SearchResults);
		}
		m_SearchResults = results.ToArray();
		m_Result.displayName = $"Results ({m_SearchResults.Length}) ({lastIndex}-{arg})";
		ShowResults();
		FocusEntity((m_SearchResults.Length != 0) ? m_SearchResults[0] : Entity.Null);
	}

	private void ShowResults()
	{
		for (int num = m_Result.children.Count - 1; num >= 0; num--)
		{
			m_Result.children.RemoveAt(num);
		}
		m_SelectedEntity = m_SelectedPage * 10;
		for (int i = m_SelectedPage * 10; i < Math.Min((m_SelectedPage + 1) * 10, m_SearchResults.Length); i++)
		{
			Entity entity = m_SearchResults[i];
			PrefabRef component;
			PrefabBase prefab;
			string displayName = ((EntityManager.TryGetComponent<PrefabRef>(entity, out component) && m_PrefabSystem.TryGetPrefab<PrefabBase>(component, out prefab)) ? $"{entity} {prefab.name}" : entity.ToString());
			m_Result.children.Add(new DebugUI.Button
			{
				displayName = displayName,
				action = delegate
				{
					FocusEntity(entity);
				}
			});
		}
		m_Result.opened = true;
	}

	private void NextEntity()
	{
		m_SelectedEntity = Math.Min(m_SelectedEntity + 1, Math.Max(m_SearchResults.Length - 1, 0));
		FocusEntity(m_SearchResults[m_SelectedEntity]);
	}

	private void PrevEntity()
	{
		m_SelectedEntity = Math.Min(Math.Max(m_SelectedEntity - 1, 0), m_SearchResults.Length);
		FocusEntity(m_SearchResults[m_SelectedEntity]);
	}

	private void FocusEntity(Entity entity)
	{
		if (entity != Entity.Null && EntityManager.Exists(entity))
		{
			m_ToolSystem.selected = entity;
			m_CameraUpdateSystem.orbitCameraController.followedEntity = entity;
			m_CameraUpdateSystem.orbitCameraController.TryMatchPosition(m_CameraUpdateSystem.activeCameraController);
			if (EntityManager.TryGetComponent<PrefabRef>(entity, out var component) && EntityManager.TryGetComponent<ObjectGeometryData>(component, out var component2))
			{
				m_CameraUpdateSystem.orbitCameraController.rotation = new Vector3(45f, m_CameraUpdateSystem.orbitCameraController.rotation.y, 0f);
				m_CameraUpdateSystem.orbitCameraController.zoom = math.length(MathUtils.Extents(component2.m_Bounds)) * 2.5f;
			}
			m_CameraUpdateSystem.activeCameraController = m_CameraUpdateSystem.orbitCameraController;
		}
		else
		{
			m_CameraUpdateSystem.orbitCameraController.followedEntity = Entity.Null;
			m_CameraUpdateSystem.gamePlayController.TryMatchPosition(m_CameraUpdateSystem.orbitCameraController);
			m_CameraUpdateSystem.activeCameraController = m_CameraUpdateSystem.gamePlayController;
		}
	}

	private static bool IsSupportedComponentType(Type type)
	{
		if (!typeof(IComponentData).IsAssignableFrom(type) && !typeof(ISharedComponentData).IsAssignableFrom(type))
		{
			return typeof(IBufferElementData).IsAssignableFrom(type);
		}
		return true;
	}
}
