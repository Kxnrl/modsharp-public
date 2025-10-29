namespace Sharp.Shared.Enums;

public enum SchemaTypeCategory : byte
{
    Builtin = 0,
    Pointer,
    Bitfield,
    FixedArray,
    Atomic,
    DeclaredClass,
    DeclaredEnum,
    Invalid,
}
