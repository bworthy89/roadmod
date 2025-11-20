using System.Threading;
using System.Threading.Tasks;
using Colossal.PSI.Common;
using Game.Input;

namespace Game.SceneFlow;

public class SwitchUserScreen : FullScreenOverlay
{
	protected override OverlayScreen overlayScreen => OverlayScreen.Wait;

	public override async Task Execute(GameManager manager, CancellationToken token)
	{
		using (InputManager.instance.CreateOverlayBarrier("SwitchUserScreen"))
		{
			OverlayBindings overlayBindings = manager.userInterface.overlayBindings;
			using (overlayBindings.ActivateScreenScoped(overlayScreen))
			{
				await PlatformManager.instance.SignIn(SignInOptions.None, null);
			}
		}
	}
}
