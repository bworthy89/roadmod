using Colossal.Serialization.Entities;

namespace Game.Serialization;

public interface IPostDeserialize
{
	void PostDeserialize(Context context);
}
