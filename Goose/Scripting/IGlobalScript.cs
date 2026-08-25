using System.Text;

namespace Goose.Scripting
{
    public interface IGlobalScript
    {
        void OnLoaded(GameWorld world);
    }
}
