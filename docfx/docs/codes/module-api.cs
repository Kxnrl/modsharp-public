namespace SharedInterface.Shared;

public interface IMySharedModule
{
    // WARNING: nameof() only produces the short type name (e.g. "IMySharedModule").
    // If another module defines an interface with the same name in a different namespace,
    // their identities will collide and the second registration will throw.
    // For modules intended for wide distribution, prefer a fully-qualified or
    // namespace-prefixed identity to guarantee uniqueness, for example:
    //   const string Identity = "SharedInterface.Shared.IMySharedModule";
    // or:
    //   static string Identity => typeof(IMySharedModule).FullName!;  // not a const, but always unique
    const string Identity = nameof(IMySharedModule);

    void CallMe();
}

public interface IMySecondSharedModule
{
    const string Identity = nameof(IMySecondSharedModule);

    void CallYou();
}