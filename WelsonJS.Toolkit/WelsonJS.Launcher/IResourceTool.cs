using System.Net;
using System.Threading.Tasks;

namespace WelsonJS.Launcher
{
    public interface IResourceTool
    {
        bool CanHandle(string path);
        Task HandleAsync(HttpListenerContext context, string path);
    }
}
