using Cinemachine;
using UnityEngine;

namespace Game.Rendering;

public interface IGameCameraController
{
	float zoom { get; set; }

	Vector3 pivot { get; set; }

	Vector3 position { get; set; }

	Vector3 rotation { get; set; }

	bool controllerEnabled { get; set; }

	bool inputEnabled { get; set; }

	ICinemachineCamera virtualCamera { get; }

	ref LensSettings lens { get; }

	void TryMatchPosition(IGameCameraController other);

	void UpdateCamera();

	void ResetCamera();
}
