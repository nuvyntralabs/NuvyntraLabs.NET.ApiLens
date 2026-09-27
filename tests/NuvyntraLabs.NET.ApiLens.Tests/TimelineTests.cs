using NuvyntraLabs.NET.ApiLens;

namespace NuvyntraLabs.NET.ApiLens.Tests;

public class TimelineTests
{
    [Fact]
    public void Sequential_operations_share_the_request_without_double_counting()
    {
        var total = TimeSpan.FromMilliseconds(200);
        OperationInterval[] operations =
        [
            new(OperationKind.Database, "SQL", TimeSpan.Zero, TimeSpan.FromMilliseconds(100), "select 1", null, 0),
            new(OperationKind.Http, "inventory.test", TimeSpan.FromMilliseconds(100), TimeSpan.FromMilliseconds(50), null, 200, null)
        ];

        var contributors = TimelineAttributor.Attribute(total, operations);

        Assert.Equal(total.Ticks, contributors.Sum(contributor => contributor.Duration.Ticks));
        Assert.Equal(TimeSpan.FromMilliseconds(100), Single(contributors, "Database").Duration);
        Assert.Equal(TimeSpan.FromMilliseconds(50), Single(contributors, "inventory.test").Duration);
        Assert.Equal(TimeSpan.FromMilliseconds(50), Single(contributors, "Application").Duration);
        Assert.Equal(TimeSpan.Zero, TimelineAttributor.Concurrent(total, operations));
    }

    [Fact]
    public void Overlapping_calls_are_counted_once()
    {
        var total = TimeSpan.FromMilliseconds(100);
        OperationInterval[] operations =
        [
            new(OperationKind.Http, "inventory.test", TimeSpan.Zero, total, null, 200, null),
            new(OperationKind.Http, "billing.test", TimeSpan.Zero, total, null, 200, null)
        ];

        var contributors = TimelineAttributor.Attribute(total, operations);

        Assert.Equal(total.Ticks, contributors.Sum(contributor => contributor.Duration.Ticks));
        Assert.Equal(total, Single(contributors, "inventory.test").Duration);
        Assert.DoesNotContain(contributors, contributor => contributor.Name == "billing.test");
        Assert.Equal(total, TimelineAttributor.Concurrent(total, operations));
    }

    private static Contributor Single(IReadOnlyList<Contributor> contributors, string name)
        => Assert.Single(contributors, contributor => contributor.Name == name);
}
