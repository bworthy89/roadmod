using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Colossal;
using Colossal.IO.AssetDatabase;
using Colossal.Mathematics;
using Colossal.PSI.Environment;
using Colossal.Serialization.Entities;
using Colossal.UI.Binding;
using Game.Assets;
using Game.CinematicCamera;
using Game.Input;
using Game.Rendering;
using Game.Rendering.CinematicCamera;
using Game.SceneFlow;
using Game.Settings;
using Game.Tutorials;
using Game.UI.Menu;
using UnityEngine;
using UnityEngine.Scripting;

namespace Game.UI.InGame;

[CompilerGenerated]
public class CinematicCameraUISystem : UISystemBase
{
	private static readonly string kGroup = "cinematicCamera";

	private static readonly CinematicCameraSequence.CinematicCameraCurveModifier[] kEmptyModifierArray = Array.Empty<CinematicCameraSequence.CinematicCameraCurveModifier>();

	private static readonly string kCaptureKeyframeTutorialTag = "CinematicCameraPanelCaptureKey";

	private PhotoModeRenderSystem m_PhotoModeRenderSystem;

	private TutorialUITriggerSystem m_TutorialUITriggerSystem;

	private CameraUpdateSystem m_CameraUpdateSystem;

	private GetterValueBinding<CinematicCameraAsset[]> m_Assets;

	private ValueBinding<CinematicCameraAsset> m_LastLoaded;

	private ValueBinding<CinematicCameraSequence.CinematicCameraCurveModifier[]> m_TransformAnimationCurveBinding;

	private ValueBinding<CinematicCameraSequence.CinematicCameraCurveModifier[]> m_ModifierAnimationCurveBinding;

	private GetterValueBinding<List<string>> m_AvailableCloudTargetsBinding;

	private GetterValueBinding<string> m_SelectedCloudTargetBinding;

	private CinematicCameraSequence m_ActiveAutoplaySequence;

	private IGameCameraController m_PreviousController;

	private ProxyAction m_MoveAction;

	private ProxyAction m_ZoomAction;

	private ProxyAction m_RotateAction;

	private bool m_Playing;

	public CinematicCameraSequence activeSequence { get; set; } = new CinematicCameraSequence();

	private float m_TimelinePositionBindingValue => MathUtils.Snap(t, 0.05f);

	private float t { get; set; }

	private bool playing
	{
		get
		{
			return m_Playing;
		}
		set
		{
			if (value != m_Playing)
			{
				m_CameraUpdateSystem.cinematicCameraController.inputEnabled = !value;
				m_CameraUpdateSystem.orbitCameraController.inputEnabled = !value;
				if (!m_Playing)
				{
					m_PreviousController = m_CameraUpdateSystem.activeCameraController;
					m_CameraUpdateSystem.activeCameraController = m_CameraUpdateSystem.cinematicCameraController;
				}
				else
				{
					m_CameraUpdateSystem.activeCameraController = m_PreviousController;
				}
				m_Playing = value;
			}
		}
	}

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		AddBinding(new TriggerBinding<float>(kGroup, "setPlaybackDuration", OnSetPlaybackDuration));
		AddBinding(new TriggerBinding<float>(kGroup, "setTimelinePosition", OnSetTimelinePosition));
		AddBinding(new TriggerBinding(kGroup, "togglePlayback", TogglePlayback));
		AddBinding(new TriggerBinding(kGroup, "stopPlayback", StopPlayback));
		AddBinding(new TriggerBinding<string, string>(kGroup, "captureKey", OnCapture));
		AddBinding(new TriggerBinding<int, int>(kGroup, "removeCameraTransformKey", OnRemoveSelectedTransform));
		AddBinding(new CallBinding<string, int, int, Keyframe, int>(kGroup, "moveKeyFrame", OnMoveKeyFrame));
		AddBinding(new TriggerBinding<string, int, int>(kGroup, "removeKeyFrame", OnRemoveKeyFrame));
		AddBinding(new CallBinding<string, float, float, int, int>(kGroup, "addKeyFrame", OnAddKeyFrame));
		AddBinding(new TriggerBinding(kGroup, "reset", Reset));
		AddUpdateBinding(new GetterValueBinding<bool>(kGroup, "loop", () => activeSequence.loop));
		AddBinding(new TriggerBinding<bool>(kGroup, "toggleLoop", OnToggleLoop));
		AddBinding(new CallBinding<float[]>(kGroup, "getControllerDelta", GetControllerDelta));
		AddBinding(new CallBinding<float[]>(kGroup, "getControllerPanDelta", GetControllerPanDelta));
		AddBinding(new CallBinding<float>(kGroup, "getControllerZoomDelta", GetControllerZoomDelta));
		AddBinding(new TriggerBinding<bool>(kGroup, "toggleCurveEditorFocus", OnCurveEditorFocusChange));
		AddUpdateBinding(new GetterValueBinding<float>(kGroup, "playbackDuration", () => activeSequence.playbackDuration));
		AddBinding(new TriggerBinding(kGroup, "onAfterPlaybackDurationChange", delegate
		{
			activeSequence.AfterModifications();
		}));
		AddUpdateBinding(new GetterValueBinding<float>(kGroup, "timelinePosition", () => m_TimelinePositionBindingValue));
		AddUpdateBinding(new GetterValueBinding<float>(kGroup, "timelineLength", () => activeSequence.timelineLength));
		AddUpdateBinding(new GetterValueBinding<bool>(kGroup, "playing", () => playing));
		AddBinding(new TriggerBinding<string, string>(kGroup, "save", Save));
		AddBinding(new TriggerBinding<string, string>(kGroup, "load", Load));
		AddBinding(m_LastLoaded = new ValueBinding<CinematicCameraAsset>(kGroup, "lastLoaded", null, ValueWriters.Nullable(new ValueWriter<CinematicCameraAsset>())));
		AddBinding(m_Assets = new GetterValueBinding<CinematicCameraAsset[]>(kGroup, "assets", UpdateAssets, new ArrayWriter<CinematicCameraAsset>(new ValueWriter<CinematicCameraAsset>())));
		AddBinding(new TriggerBinding<string, string>(kGroup, "delete", Delete));
		AddBinding(m_TransformAnimationCurveBinding = new ValueBinding<CinematicCameraSequence.CinematicCameraCurveModifier[]>(kGroup, "transformAnimationCurves", kEmptyModifierArray, new ListWriter<CinematicCameraSequence.CinematicCameraCurveModifier>(new ValueWriter<CinematicCameraSequence.CinematicCameraCurveModifier>())));
		AddBinding(m_ModifierAnimationCurveBinding = new ValueBinding<CinematicCameraSequence.CinematicCameraCurveModifier[]>(kGroup, "modifierAnimationCurves", kEmptyModifierArray, new ListWriter<CinematicCameraSequence.CinematicCameraCurveModifier>(new ValueWriter<CinematicCameraSequence.CinematicCameraCurveModifier>())));
		AddBinding(m_AvailableCloudTargetsBinding = new GetterValueBinding<List<string>>(kGroup, "availableCloudTargets", MenuHelpers.GetAvailableCloudTargets, new ListWriter<string>()));
		AddUpdateBinding(m_SelectedCloudTargetBinding = new GetterValueBinding<string>(kGroup, "selectedCloudTarget", () => MenuHelpers.GetSanitizedCloudTarget(SharedSettings.instance.userState.lastCloudTarget).name));
		AddBinding(new TriggerBinding<string>(kGroup, "selectCloudTarget", delegate(string cloudTarget)
		{
			SharedSettings.instance.userState.lastCloudTarget = cloudTarget;
		}));
		m_TutorialUITriggerSystem = base.World.GetOrCreateSystemManaged<TutorialUITriggerSystem>();
		m_CameraUpdateSystem = base.World.GetOrCreateSystemManaged<CameraUpdateSystem>();
		m_PhotoModeRenderSystem = base.World.GetOrCreateSystemManaged<PhotoModeRenderSystem>();
		m_MoveAction = InputManager.instance.FindAction("Camera", "Move");
		m_ZoomAction = InputManager.instance.FindAction("Camera", "Zoom");
		m_RotateAction = InputManager.instance.FindAction("Camera", "Rotate");
		AssetDatabase.global.onAssetDatabaseChanged.Subscribe(OnCloudTargetsChanged, delegate(AssetChangedEventArgs args)
		{
			ChangeType change = args.change;
			return change == ChangeType.DatabaseRegistered || change == ChangeType.DatabaseUnregistered || change == ChangeType.BulkAssetsChange;
		}, AssetChangedEventArgs.Default);
		AssetDatabase.global.onAssetDatabaseChanged.Subscribe<CinematicCameraAsset>(OnAssetsChanged, AssetChangedEventArgs.Default);
		Reset();
	}

	public void ToggleModifier(PhotoModeProperty p)
	{
		m_TutorialUITriggerSystem.ActivateTrigger(kCaptureKeyframeTutorialTag);
		foreach (PhotoModeProperty item in PhotoModeUtils.ExtractMultiPropertyComponents(p, m_PhotoModeRenderSystem.photoModeProperties))
		{
			float min = item.min?.Invoke() ?? (-10000f);
			float max = item.max?.Invoke() ?? 10000f;
			activeSequence.AddModifierKey(item.id, t, item.getValue(), min, max);
		}
		m_ModifierAnimationCurveBinding.Update(activeSequence.modifiers.ToArray());
	}

	protected override void OnGameLoaded(Context serializationContext)
	{
		if (m_CameraUpdateSystem.cinematicCameraController != null)
		{
			CinematicCameraController cinematicCameraController = m_CameraUpdateSystem.cinematicCameraController;
			cinematicCameraController.eventCameraMove = (Action)Delegate.Remove(cinematicCameraController.eventCameraMove, new Action(PausePlayback));
			CinematicCameraController cinematicCameraController2 = m_CameraUpdateSystem.cinematicCameraController;
			cinematicCameraController2.eventCameraMove = (Action)Delegate.Combine(cinematicCameraController2.eventCameraMove, new Action(PausePlayback));
		}
		if (m_CameraUpdateSystem.orbitCameraController != null)
		{
			OrbitCameraController orbitCameraController = m_CameraUpdateSystem.orbitCameraController;
			orbitCameraController.EventCameraMove = (Action)Delegate.Remove(orbitCameraController.EventCameraMove, new Action(PausePlayback));
			OrbitCameraController orbitCameraController2 = m_CameraUpdateSystem.orbitCameraController;
			orbitCameraController2.EventCameraMove = (Action)Delegate.Combine(orbitCameraController2.EventCameraMove, new Action(PausePlayback));
		}
		if (serializationContext.purpose != Purpose.Cleanup)
		{
			m_ActiveAutoplaySequence = null;
		}
		m_Playing = false;
		Reset();
	}

	[Preserve]
	protected override void OnUpdate()
	{
		base.OnUpdate();
		if (playing)
		{
			UpdatePlayback();
		}
	}

	[Preserve]
	protected override void OnDestroy()
	{
		if (m_CameraUpdateSystem?.cinematicCameraController != null)
		{
			CinematicCameraController cinematicCameraController = m_CameraUpdateSystem.cinematicCameraController;
			cinematicCameraController.eventCameraMove = (Action)Delegate.Remove(cinematicCameraController.eventCameraMove, new Action(PausePlayback));
		}
		if (m_CameraUpdateSystem?.orbitCameraController != null)
		{
			OrbitCameraController orbitCameraController = m_CameraUpdateSystem.orbitCameraController;
			orbitCameraController.EventCameraMove = (Action)Delegate.Remove(orbitCameraController.EventCameraMove, new Action(PausePlayback));
		}
		base.OnDestroy();
	}

	private void OnToggleLoop(bool loop)
	{
		activeSequence.loop = loop;
		if (loop)
		{
			m_ModifierAnimationCurveBinding.Update(activeSequence.modifiers.ToArray());
			m_TransformAnimationCurveBinding.Update(GetTransformCurves());
		}
	}

	private int OnAddKeyFrame(string id, float time, float value, int curveIndex)
	{
		if (id == "Property")
		{
			CinematicCameraSequence.CinematicCameraCurveModifier cinematicCameraCurveModifier = activeSequence.modifiers[curveIndex];
			string id2 = cinematicCameraCurveModifier.id;
			int result = activeSequence.AddModifierKey(id2, time, value, cinematicCameraCurveModifier.min, cinematicCameraCurveModifier.max);
			m_ModifierAnimationCurveBinding.Update(activeSequence.modifiers.ToArray());
			return result;
		}
		int result2 = activeSequence.transforms[curveIndex].curve.AddKey(time, value);
		m_TransformAnimationCurveBinding.Update(GetTransformCurves());
		return result2;
	}

	private void Reset()
	{
		activeSequence.Reset();
		m_TransformAnimationCurveBinding.Update(GetTransformCurves());
		m_ModifierAnimationCurveBinding.Update(activeSequence.modifiers.ToArray());
	}

	private void OnSetTimelinePosition(float position)
	{
		playing = false;
		t = position;
		activeSequence.Refresh(position, m_PhotoModeRenderSystem.photoModeProperties, m_CameraUpdateSystem.activeCameraController);
	}

	private void OnSetPlaybackDuration(float duration)
	{
		activeSequence.playbackDuration = Mathf.Max(duration, activeSequence.timelineLength);
	}

	private void OnCapture(string id, string property)
	{
		if (id == "Property")
		{
			foreach (PhotoModeProperty value in m_PhotoModeRenderSystem.photoModeProperties.Values)
			{
				if (PhotoModeUtils.ExtractPropertyID(value) == property)
				{
					ToggleModifier(value);
					break;
				}
			}
			return;
		}
		OnCaptureTransform();
	}

	private void OnCaptureTransform()
	{
		m_TutorialUITriggerSystem.ActivateTrigger(kCaptureKeyframeTutorialTag);
		Vector3 position = m_CameraUpdateSystem.activeCameraController.position;
		Vector3 rotation = m_CameraUpdateSystem.activeCameraController.rotation;
		activeSequence.AddCameraTransform(t, position, rotation);
		m_TransformAnimationCurveBinding.Update(GetTransformCurves());
	}

	private void Save(string name, string hash = null)
	{
		ILocalAssetDatabase item = MenuHelpers.GetSanitizedCloudTarget(SharedSettings.instance.userState.lastCloudTarget).db;
		if (string.IsNullOrEmpty(hash))
		{
			AssetDataPath name2 = name;
			if (!item.dataSource.isRemoteStorageSource)
			{
				string specialPath = EnvPath.GetSpecialPath<CinematicCameraAsset>();
				if (specialPath != null)
				{
					name2 = AssetDataPath.Create(specialPath, name);
				}
			}
			CinematicCameraAsset cinematicCameraAsset = item.AddAsset<CinematicCameraAsset>(name2);
			cinematicCameraAsset.target = activeSequence;
			cinematicCameraAsset.Save();
			m_LastLoaded.Update(cinematicCameraAsset);
			m_Assets.Update();
		}
		else
		{
			Colossal.Hash128 guid = new Colossal.Hash128(hash);
			CinematicCameraAsset asset = item.GetAsset<CinematicCameraAsset>(guid);
			if (asset != null)
			{
				asset.target = activeSequence;
				asset.Save();
				m_LastLoaded.Update(asset);
				m_Assets.Update();
			}
		}
	}

	private void Load(string hash, string storage)
	{
		Colossal.Hash128 guid = new Colossal.Hash128(hash);
		CinematicCameraAsset asset = MenuHelpers.GetSanitizedCloudTarget(storage).db.GetAsset<CinematicCameraAsset>(guid);
		if (asset != null)
		{
			Reset();
			asset.Load();
			if (asset.target != null)
			{
				activeSequence = asset.target;
				m_LastLoaded.Update(asset);
				m_TransformAnimationCurveBinding.Update(GetTransformCurves());
				m_ModifierAnimationCurveBinding.Update(activeSequence.modifiers.ToArray());
			}
		}
	}

	private void Delete(string hash, string storage)
	{
		Colossal.Hash128 guid = new Colossal.Hash128(hash);
		MenuHelpers.GetSanitizedCloudTarget(storage).db.DeleteAsset(guid);
		m_Assets.Update();
	}

	private void OnAssetsChanged(AssetChangedEventArgs args)
	{
		GameManager.instance.RunOnMainThread(delegate
		{
			m_Assets.Update();
		});
	}

	private void OnCloudTargetsChanged(AssetChangedEventArgs args)
	{
		GameManager.instance.RunOnMainThread(delegate
		{
			m_AvailableCloudTargetsBinding.Update();
			m_SelectedCloudTargetBinding.Update();
		});
	}

	private CinematicCameraAsset[] UpdateAssets()
	{
		return AssetDatabase.global.GetAssets(default(SearchFilter<CinematicCameraAsset>)).ToArray();
	}

	private void OnRemoveSelectedTransform(int curveIndex, int index)
	{
		OnRemoveKeyFrame("Transform", curveIndex, index);
	}

	private void OnRemoveKeyFrame(string id, int curveIndex, int index)
	{
		if (id == "Property")
		{
			string id2 = activeSequence.modifiers[curveIndex].id;
			activeSequence.RemoveModifierKey(id2, index);
			m_ModifierAnimationCurveBinding.Update(activeSequence.modifiers.ToArray());
		}
		else
		{
			activeSequence.RemoveCameraTransform(curveIndex, index);
			m_TransformAnimationCurveBinding.Update(GetTransformCurves());
		}
	}

	private void GetData(string id, out CinematicCameraSequence.CinematicCameraCurveModifier[] modifiers, out ValueBinding<CinematicCameraSequence.CinematicCameraCurveModifier[]> binding)
	{
		if (id == "Position")
		{
			modifiers = activeSequence.transforms.ToArray();
			binding = m_TransformAnimationCurveBinding;
		}
		else
		{
			modifiers = activeSequence.modifiers.ToArray();
			binding = m_ModifierAnimationCurveBinding;
		}
	}

	private int OnMoveKeyFrame(string id, int curveIndex, int index, Keyframe keyframe)
	{
		GetData(id, out var modifiers, out var binding);
		CinematicCameraSequence.CinematicCameraCurveModifier modifier = modifiers[curveIndex];
		int result = activeSequence.MoveKeyframe(modifier, index, keyframe);
		binding.Update(modifiers);
		activeSequence.Refresh(t, m_PhotoModeRenderSystem.photoModeProperties, m_CameraUpdateSystem.activeCameraController);
		return result;
	}

	private void UpdatePlayback()
	{
		t += UnityEngine.Time.unscaledDeltaTime;
		CinematicCameraSequence cinematicCameraSequence = m_ActiveAutoplaySequence ?? activeSequence;
		if (t >= cinematicCameraSequence.playbackDuration)
		{
			if (cinematicCameraSequence.loop)
			{
				t -= cinematicCameraSequence.playbackDuration;
			}
			else
			{
				playing = false;
			}
		}
		t = Mathf.Min(t, cinematicCameraSequence.playbackDuration);
		cinematicCameraSequence.Refresh(t, m_PhotoModeRenderSystem.photoModeProperties, m_CameraUpdateSystem.activeCameraController);
	}

	private void PausePlayback()
	{
		if (playing && (m_CameraUpdateSystem.activeCameraController is CinematicCameraController || (m_CameraUpdateSystem.activeCameraController is OrbitCameraController && m_CameraUpdateSystem.orbitCameraController.mode == OrbitCameraController.Mode.PhotoMode)))
		{
			playing = false;
		}
	}

	private void TogglePlayback()
	{
		playing = !playing;
		if (t > activeSequence.playbackDuration - 0.1f)
		{
			t = 0f;
		}
	}

	private void StopPlayback()
	{
		t = 0f;
		activeSequence.Refresh(t, m_PhotoModeRenderSystem.photoModeProperties, m_CameraUpdateSystem.activeCameraController);
		playing = false;
	}

	public void Autoplay(CinematicCameraAsset sequence)
	{
		m_ActiveAutoplaySequence = sequence.target;
		m_ActiveAutoplaySequence.loop = true;
		t = 0f;
		playing = true;
	}

	public void StopAutoplay()
	{
		m_ActiveAutoplaySequence = null;
		t = 0f;
		playing = false;
	}

	private CinematicCameraSequence.CinematicCameraCurveModifier[] GetTransformCurves()
	{
		if (activeSequence.transformCount > 0)
		{
			List<CinematicCameraSequence.CinematicCameraCurveModifier> list = new List<CinematicCameraSequence.CinematicCameraCurveModifier>();
			CinematicCameraSequence.CinematicCameraCurveModifier[] transforms = activeSequence.transforms;
			for (int i = 0; i < transforms.Length; i++)
			{
				CinematicCameraSequence.CinematicCameraCurveModifier item = transforms[i];
				if (item.curve != null)
				{
					list.Add(item);
				}
			}
			return list.ToArray();
		}
		return kEmptyModifierArray;
	}

	private float[] GetControllerDelta()
	{
		Vector2 vector = m_MoveAction.ReadValue<Vector2>() * UnityEngine.Time.deltaTime;
		return new float[2] { vector.x, vector.y };
	}

	private float[] GetControllerPanDelta()
	{
		Vector2 vector = m_RotateAction.ReadValue<Vector2>() * UnityEngine.Time.deltaTime;
		return new float[2] { vector.x, vector.y };
	}

	private float GetControllerZoomDelta()
	{
		return m_ZoomAction.ReadValue<float>() * UnityEngine.Time.deltaTime;
	}

	private void OnCurveEditorFocusChange(bool focused)
	{
		m_CameraUpdateSystem.orbitCameraController.inputEnabled = !focused;
		m_CameraUpdateSystem.cinematicCameraController.inputEnabled = !focused;
	}

	[Preserve]
	public CinematicCameraUISystem()
	{
	}
}
