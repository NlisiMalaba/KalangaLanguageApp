using CsCheck;

namespace Kalanga.Tests;

public sealed class ScaffoldTests
{
    [Fact]
    public void Integer_identity_holds_for_generated_values()
    {
        Gen.Int.Sample(value => value + 0 == value);
    }
}
