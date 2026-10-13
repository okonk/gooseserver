using System.Text;

namespace Goose.Scripting
{
    public interface IScript
    {
        void LoadScript();

        Action CompileForSwap();
    }
}
