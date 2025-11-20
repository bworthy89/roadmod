using System.Runtime.CompilerServices;
using Colossal.UI.Binding;
using Game.Prefabs;
using Game.SceneFlow;
using Game.Tutorials;
using Game.UI.InGame;
using Unity.Collections;
using Unity.Entities;
using UnityEngine.Scripting;

namespace Game.UI.Editor;

[CompilerGenerated]
public class EditorTutorialsUISystem : TutorialsUISystem
{
	private const string kEditorGroup = "editorTutorials";

	private EntityQuery m_TutorialCategoryQuery;

	private bool m_EditorTutorialsDisabled;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_TutorialSystem = base.World.GetOrCreateSystemManaged<EditorTutorialSystem>();
		m_TutorialCategoryQuery = GetEntityQuery(ComponentType.ReadOnly<UIEditorTutorialGroupData>(), ComponentType.ReadOnly<UIObjectData>());
		m_UnlockQuery = GetEntityQuery(ComponentType.ReadOnly<Unlock>(), ComponentType.ReadOnly<EditorTutorial>());
		m_EditorTutorialsDisabled = true;
		AddUpdateBinding(new GetterValueBinding<bool>("editorTutorials", "tutorialsDisabled", () => m_EditorTutorialsDisabled));
		AddUpdateBinding(new GetterValueBinding<bool>("editorTutorials", "tutorialsEnabled", () => m_TutorialSystem.tutorialEnabled));
		AddUpdateBinding(new GetterValueBinding<bool>("editorTutorials", "introActive", () => m_TutorialSystem.mode == TutorialMode.Intro));
		AddUpdateBinding(new GetterValueBinding<bool>("editorTutorials", "listIntroActive", () => m_TutorialSystem.mode == TutorialMode.ListIntro));
		AddUpdateBinding(new GetterValueBinding<bool>("editorTutorials", "listOutroActive", () => m_TutorialSystem.mode == TutorialMode.ListOutro));
		AddUpdateBinding(new GetterValueBinding<Entity>("editorTutorials", "next", () => m_TutorialSystem.nextListTutorial));
		AddUpdateBinding(new GetterValueBinding<Entity>("editorTutorials", "advisorPanelVisible", () => m_TutorialSystem.nextListTutorial));
		AddBinding(m_TutorialCategoriesBinding = new RawValueBinding("editorTutorials", "categories", BindCategories));
		AddBinding(m_ActiveTutorialBinding = new RawValueBinding("editorTutorials", "activeTutorial", delegate(IJsonWriter writer)
		{
			BindTutorial(writer, m_TutorialSystem.activeTutorial);
		}));
		AddBinding(m_ActiveTutorialPhaseBinding = new RawValueBinding("editorTutorials", "activeTutorialPhase", delegate(IJsonWriter writer)
		{
			BindTutorialPhase(writer, m_TutorialSystem.activeTutorialPhase);
		}));
		AddBinding(m_ActiveTutorialListBinding = new RawValueBinding("editorTutorials", "activeList", base.BindActiveTutorialList));
		AddBinding(new TriggerBinding<bool>("editorTutorials", "completeListIntro", CompleteEditorIntro));
		AddBinding(new TriggerBinding("editorTutorials", "toggleTutorials", ToggleTutorials));
	}

	private NativeList<UIObjectInfo> GetSortedCategories(Allocator allocator)
	{
		NativeArray<Entity> nativeArray = m_TutorialCategoryQuery.ToEntityArray(Allocator.TempJob);
		NativeArray<UIObjectData> nativeArray2 = m_TutorialCategoryQuery.ToComponentDataArray<UIObjectData>(Allocator.TempJob);
		NativeList<UIObjectInfo> nativeList = new NativeList<UIObjectInfo>(allocator);
		for (int i = 0; i < nativeArray.Length; i++)
		{
			if (nativeArray2[i].m_Group == Entity.Null)
			{
				nativeList.Add(new UIObjectInfo(nativeArray[i], nativeArray2[i].m_Priority));
			}
		}
		nativeList.Sort();
		nativeArray.Dispose();
		nativeArray2.Dispose();
		return nativeList;
	}

	private void BindCategories(IJsonWriter writer)
	{
		NativeList<UIObjectInfo> sortedCategories = GetSortedCategories(Allocator.Temp);
		writer.ArrayBegin(sortedCategories.Length);
		for (int i = 0; i < sortedCategories.Length; i++)
		{
			UIObjectInfo uIObjectInfo = sortedCategories[i];
			UITutorialGroupPrefab prefab = m_PrefabSystem.GetPrefab<UITutorialGroupPrefab>(uIObjectInfo.entity);
			writer.TypeBegin(TypeNames.kAdvisorCategory);
			writer.PropertyName("entity");
			writer.Write(uIObjectInfo.entity);
			writer.PropertyName("name");
			writer.Write(prefab.name);
			writer.PropertyName("shown");
			writer.Write(base.EntityManager.HasComponent<TutorialShown>(uIObjectInfo.entity));
			writer.PropertyName("locked");
			writer.Write(value: false);
			writer.PropertyName("children");
			BindTutorialGroup(writer, uIObjectInfo.entity);
			writer.TypeEnd();
		}
		writer.ArrayEnd();
		sortedCategories.Dispose();
	}

	[Preserve]
	protected override void OnUpdate()
	{
		if (!GameManager.instance.gameMode.IsGame())
		{
			base.OnUpdate();
		}
	}

	private void CompleteEditorIntro(bool value)
	{
		m_TutorialSystem.mode = TutorialMode.Default;
		m_TutorialSystem.tutorialEnabled = value;
	}

	protected override void CompleteActiveTutorialPhase()
	{
		if (GameManager.instance.gameMode.IsEditor())
		{
			m_TutorialSystem.CompleteCurrentTutorialPhase();
		}
	}

	private void ToggleTutorials()
	{
		m_TutorialSystem.tutorialEnabled = !m_TutorialSystem.tutorialEnabled;
		if (m_TutorialSystem.tutorialEnabled)
		{
			World.DefaultGameObjectInjectionWorld.GetExistingSystemManaged<EditorTutorialSystem>().OnResetTutorials();
		}
	}

	[Preserve]
	public EditorTutorialsUISystem()
	{
	}
}
