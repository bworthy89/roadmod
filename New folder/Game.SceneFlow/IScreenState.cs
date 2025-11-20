using System;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using Colossal.PSI.Common;
using Game.Input;
using UnityEngine.InputSystem;

namespace Game.SceneFlow;

public interface IScreenState
{
	static Task WaitForWaitingState(InputAction inputAction)
	{
		if (inputAction.phase == InputActionPhase.Waiting)
		{
			return Task.CompletedTask;
		}
		TaskCompletionSource<bool> taskCompletionSource = new TaskCompletionSource<bool>();
		System.Timers.Timer timer = new System.Timers.Timer(33.333333333333336);
		timer.Elapsed += delegate
		{
			if (inputAction.phase == InputActionPhase.Waiting)
			{
				taskCompletionSource.SetResult(result: true);
				timer.Stop();
				timer.Dispose();
			}
		};
		timer.AutoReset = true;
		timer.Start();
		return taskCompletionSource.Task;
	}

	static async Task<(bool ok, InputDevice device)> WaitForInput(InputAction inputContinue, InputAction inputCancel, Action cancel, CancellationToken token)
	{
		TaskCompletionSource<(bool ok, InputDevice device)> performed = new TaskCompletionSource<(bool, InputDevice)>();
		using (token.Register(delegate
		{
			performed.TrySetCanceled();
		}))
		{
			inputContinue.performed += Handler;
			if (inputCancel != null)
			{
				inputCancel.performed += Handler;
			}
			if (cancel != null)
			{
				cancel = (Action)Delegate.Combine(cancel, new Action(CancelHandler));
			}
			GameManager.instance.userInterface.inputHintBindings.onInputHintPerformed += InputHintPerformedHandler;
			try
			{
				return await performed.Task;
			}
			finally
			{
				inputContinue.performed -= Handler;
				inputContinue.Reset();
				if (inputCancel != null)
				{
					inputCancel.performed -= Handler;
					inputCancel.Reset();
				}
				if (cancel != null)
				{
					cancel = (Action)Delegate.Remove(cancel, new Action(CancelHandler));
				}
				GameManager.instance.userInterface.inputHintBindings.onInputHintPerformed -= InputHintPerformedHandler;
			}
		}
		void CancelHandler()
		{
			performed.TrySetCanceled();
		}
		void Handler(InputAction.CallbackContext c)
		{
			performed.TrySetResult((inputContinue == c.action, c.action.activeControl?.device));
		}
		void InputHintPerformedHandler(ProxyAction action)
		{
			if (action.sourceAction == inputContinue)
			{
				performed.TrySetResult((true, Mouse.current));
			}
			else if (action.sourceAction == inputCancel)
			{
				performed.TrySetCanceled();
			}
		}
	}

	static async Task<object> WaitForDevice(Action cancel, CancellationToken token)
	{
		TaskCompletionSource<object> devicePaired = new TaskCompletionSource<object>();
		using (token.Register(delegate
		{
			devicePaired.TrySetCanceled();
		}))
		{
			Game.Input.InputManager.instance.EventDevicePaired += Handler;
			if (cancel != null)
			{
				cancel = (Action)Delegate.Combine(cancel, new Action(CancelHandler));
			}
			try
			{
				return await devicePaired.Task;
			}
			finally
			{
				Game.Input.InputManager.instance.EventDevicePaired -= Handler;
				if (cancel != null)
				{
					cancel = (Action)Delegate.Remove(cancel, new Action(CancelHandler));
				}
			}
		}
		void CancelHandler()
		{
			devicePaired.TrySetCanceled();
		}
		void Handler()
		{
			devicePaired.TrySetResult(null);
		}
	}

	static async Task<UserChangedFlags> WaitForUser(Action cancel, CancellationToken token)
	{
		TaskCompletionSource<UserChangedFlags> userSignedBackIn = new TaskCompletionSource<UserChangedFlags>();
		using (token.Register(delegate
		{
			userSignedBackIn.TrySetCanceled();
		}))
		{
			PlatformManager.instance.onUserUpdated += Handler;
			if (cancel != null)
			{
				cancel = (Action)Delegate.Combine(cancel, new Action(CancelHandler));
			}
			try
			{
				return await userSignedBackIn.Task;
			}
			finally
			{
				PlatformManager.instance.onUserUpdated -= Handler;
				if (cancel != null)
				{
					cancel = (Action)Delegate.Remove(cancel, new Action(CancelHandler));
				}
			}
		}
		void CancelHandler()
		{
			userSignedBackIn.TrySetCanceled();
		}
		void Handler(IPlatformServiceIntegration psi, UserChangedFlags flags)
		{
			if (PlatformManager.instance.IsPrincipalUserIntegration(psi) && flags.HasFlag(UserChangedFlags.UserSignedInAgain))
			{
				userSignedBackIn.TrySetResult(flags);
			}
		}
	}

	Task Execute(GameManager manager, CancellationToken token);
}
