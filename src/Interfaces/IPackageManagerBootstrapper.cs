namespace WinHome.Interfaces
{
    public interface IPackageManagerBootstrapper
    {
        string Name { get; }
        bool IsInstalled();
        void Install(bool dryRun);
    }
}
