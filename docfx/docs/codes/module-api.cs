namespace SharedInterface.Shared;

public interface ISharedModule
{
    const string Identity = nameof(ISharedModule);

    void CallMe();
}