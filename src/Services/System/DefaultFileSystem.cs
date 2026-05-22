using System.IO;
using WinHome.Interfaces;

namespace WinHome.Services.System
{
    public class DefaultFileSystem : IFileSystem
    {
        public bool FileExists(string path)
        {
            return File.Exists(path);
        }
    }
}
