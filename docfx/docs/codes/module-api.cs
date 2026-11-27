namespace SharedInterface.Shared;

public interface IMySharedModule
{
    // Identities are global keys: include the namespace to avoid short-name collisions.
    const string Identity = "SharedInterface.Shared.IMySharedModule";

    void CallMe();
}

public interface IMySecondSharedModule
{
    const string Identity = "SharedInterface.Shared.IMySecondSharedModule";

    void CallYou();
}
