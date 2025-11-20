using System;
using Colossal.PSI.Common;
using Colossal.UI.Binding;
using Game.Input;
using Game.Settings;
using Game.Tools;
using UnityEngine;

namespace Game.UI;

public class InputBindings : CompositeBinding, IDisposable
{
	private const string kGroup = "input";

	private const float kCameraInputSensitivity = 0.2f;

	private const float kCameraInputSensitivitySqr = 0.040000003f;

	private CameraController m_CameraController;

	private readonly ValueBinding<bool> m_CameraMovingBinding;

	private readonly EventBinding<bool> m_CameraBarrierBinding;

	private readonly EventBinding<bool> m_ToolBarrierBinding;

	private readonly EventBinding<bool> m_ToolActionPerformedBinding;

	private InputBarrier m_CameraInputBarrier;

	private InputBarrier m_ToolInputBarrier;

	public InputBindings()
	{
		AddUpdateBinding(new GetterValueBinding<bool>("input", "mouseOverUI", () => InputManager.instance.mouseOverUI));
		AddUpdateBinding(new GetterValueBinding<bool>("input", "hideCursor", () => InputManager.instance.hideCursor));
		AddUpdateBinding(new GetterValueBinding<int>("input", "controlScheme", () => (int)InputManager.instance.activeControlScheme));
		AddUpdateBinding(new GetterValueBinding<float>("input", "scrollSensitivity", () => SharedSettings.instance.input.finalScrollSensitivity));
		AddUpdateBinding(new GetterValueBinding<Vector2>("input", "gamepadPointerPosition", () => InputManager.instance.gamepadPointerPosition));
		AddBinding(m_CameraMovingBinding = new ValueBinding<bool>("input", "cameraMoving", initialValue: false));
		AddBinding(m_ToolActionPerformedBinding = new EventBinding<bool>("input", "toolActionPerformed"));
		AddBinding(m_CameraBarrierBinding = new EventBinding<bool>("input", "cameraBarrier"));
		AddBinding(m_ToolBarrierBinding = new EventBinding<bool>("input", "toolBarrier"));
		AddBinding(new TriggerBinding<bool>("input", "onGamepadPointerEvent", OnGamepadPointerEvent));
		AddBinding(new TriggerBinding<int, int, int, int>("input", "setActiveTextFieldRect", SetActiveTextfieldRect));
		AddBinding(new GetterValueBinding<bool>("input", "useTextFieldInputBarrier", () => PlatformManager.instance.passThroughVKeyboard));
		m_CameraInputBarrier = InputManager.instance.CreateMapBarrier("Camera", "InputBindings");
		m_ToolInputBarrier = InputManager.instance.CreateMapBarrier("Tool", "InputBindings");
		ToolBaseSystem.EventToolActionPerformed += OnToolActionPerformed;
	}

	public void Dispose()
	{
		m_CameraInputBarrier.Dispose();
		m_ToolInputBarrier.Dispose();
		ToolBaseSystem.EventToolActionPerformed -= OnToolActionPerformed;
	}

	public override bool Update()
	{
		bool newValue = false;
		if (m_CameraController != null || CameraController.TryGet(out m_CameraController))
		{
			foreach (ProxyAction inputAction in m_CameraController.inputActions)
			{
				if (!inputAction.IsInProgress())
				{
					continue;
				}
				Type valueType = inputAction.valueType;
				if (valueType == typeof(float))
				{
					if (Mathf.Abs(inputAction.ReadRawValue<float>()) >= 0.2f)
					{
						newValue = true;
						break;
					}
				}
				else if (valueType == typeof(Vector2) && inputAction.ReadRawValue<Vector2>().sqrMagnitude >= 0.040000003f)
				{
					newValue = true;
					break;
				}
			}
		}
		m_CameraMovingBinding.Update(newValue);
		m_CameraInputBarrier.blocked = m_CameraBarrierBinding.observerCount > 0;
		m_ToolInputBarrier.blocked = m_ToolBarrierBinding.observerCount > 0;
		return base.Update();
	}

	private void OnToolActionPerformed(ProxyAction action)
	{
		m_ToolActionPerformedBinding.Trigger(value: true);
	}

	private void OnGamepadPointerEvent(bool pointerOverUI)
	{
		InputManager.instance.mouseOverUI = pointerOverUI;
	}

	private void SetActiveTextfieldRect(int x, int y, int width, int height)
	{
		PlatformManager.instance?.SetActiveTextFieldRect(x, y, width, height);
	}
}
