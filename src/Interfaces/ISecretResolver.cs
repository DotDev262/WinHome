namespace WinHome.Interfaces
{
    public interface ISecretResolver
    {
        string Resolve(string input);
        void ResolveObject(object obj);
    }
}
