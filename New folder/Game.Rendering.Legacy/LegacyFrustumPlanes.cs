namespace Game.Rendering.Legacy;

public struct LegacyFrustumPlanes
{
	public Plane left;

	public Plane right;

	public Plane bottom;

	public Plane top;

	public Plane zNear;

	public Plane zFar;

	public Plane this[int i] => i switch
	{
		0 => left, 
		1 => right, 
		2 => bottom, 
		3 => top, 
		4 => zNear, 
		5 => zFar, 
		_ => default(Plane), 
	};
}
