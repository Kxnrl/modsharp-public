namespace SharedInterface.Shared;

public interface IMySharedModule
{
    const string Identity = nameof(IMySharedModule);

    void CallMe();
}

public interface IMySecondSharedModule
{
    const string Identity = nameof(IMySecondSharedModule);

    void CallYou();
}