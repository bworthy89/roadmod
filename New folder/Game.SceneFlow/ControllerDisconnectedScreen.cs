using System.Threading;
using System.Threading.Tasks;
using Colossal.PSI.Common;
using Game.Input;
using UnityEngine.InputSystem;

namespace Game.SceneFlow;

public class ControllerDisconnectedScreen : FullScreenOverlay
{
	protected override OverlayScreen overlayScreen => OverlayScreen.ControllerDisconnected;

	protected override string actionA => "AnyKey";

	protected override string continueDisplayProperty => "Continue";

	public override async Task Execute(GameManager manager, CancellationToken token)
	{
		using EnabledActionScoped continueAction = new EnabledActionScoped(manager, "Engagement", actionA, HandleScreenChange, continueDisplayProperty, continueDisplayPriority);
		using (Game.Input.InputManager.instance.CreateOverlayBarrier("ControllerDisconnectedScreen"))
		{
			OverlayBindings overlayBindings = manager.userInterface.overlayBindings;
			using (overlayBindings.ActivateScreenScoped(overlayScreen))
			{
				while (!m_Done)
				{
					Task<(bool ok, InputDevice device)> input = IScreenState.WaitForInput(continueAction, null, m_CompletedEvent, token);
					Task<object> device = IScreenState.WaitForDevice(m_CompletedEvent, token);
					await Task.WhenAny(input, device);
					m_CompletedEvent?.Invoke();
					if (input.IsCompletedSuccessfully)
					{
						m_Done = await PlatformManager.instance.AssociateDevice(input.Result.device);
					}
					else if (device.IsCompletedSuccessfully)
					{
						m_Done = true;
					}
					else
					{
						m_Done = true;
					}
				}
			}
		}
	}
}
