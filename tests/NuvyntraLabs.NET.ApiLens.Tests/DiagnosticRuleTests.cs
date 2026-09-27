using NuvyntraLabs.NET.ApiLens;

namespace NuvyntraLabs.NET.ApiLens.Tests;

public class DiagnosticRuleTests
{
    [Fact]
    public void Sql_literals_and_numbers_are_redacted_and_parameter_names_stay()
    {
        var redacted = SqlRedactor.Redact("SELECT * FROM Orders WHERE Name = N'O''Brien' AND Id = @p0 AND Age > 21");

        Assert.DoesNotContain("Brien", redacted, StringComparison.Ordinal);
        Assert.Contains("@p0", redacted, StringComparison.Ordinal);
        Assert.Contains("'?'", redacted, StringComparison.Ordinal);
        Assert.DoesNotContain("21", redacted, StringComparison.Ordinal);
    }

    [Fact]
    public void Repeated_sql_shape_is_a_possible_n_plus_one()
    {
        var operations = Enumerable.Range(0, 10)
            .Select(index => new OperationInterval(
                OperationKind.Database,
                "SQL",
                TimeSpan.FromMilliseconds(index),
                TimeSpan.FromMilliseconds(1),
                $"SELECT * FROM Customers WHERE Id = {index} AND Name = 'Ada'",
                null,
                1))
            .ToArray();

        var finding = Assert.Single(NPlusOneDetector.Detect(operations, threshold: 8, includeCommandText: true));

        Assert.Equal(10, finding.Count);
        Assert.Equal(9, finding.EstimatedUnnecessary);
        Assert.NotNull(finding.CommandText);
        Assert.DoesNotContain("Ada", finding.CommandText, StringComparison.Ordinal);
        Assert.Contains("'?'", finding.CommandText, StringComparison.Ordinal);
    }

    [Fact]
    public void Production_capture_drops_sql_text_after_grouping()
    {
        var operations = Enumerable.Range(0, 10)
            .Select(index => new OperationInterval(
                OperationKind.Database,
                "SQL",
                TimeSpan.FromMilliseconds(index * 200),
                TimeSpan.FromMilliseconds(200),
                $"SELECT Name FROM Customers WHERE Id = {index}",
                null,
                1))
            .ToArray();

        var report = RequestAnalyzer.Analyze(
            operations,
            "GET",
            "/api/orders",
            200,
            "trace",
            exceptionType: null,
            TimeSpan.FromSeconds(2),
            new ApiLensOptions { NPlusOneRepeatThreshold = 8, SlowRequestThreshold = TimeSpan.FromMilliseconds(500) },
            ApiLensCaptureMode.Production);

        Assert.All(report.Operations, operation => Assert.Null(operation.Detail));
        Assert.All(report.Explanation.NPlusOne, finding => Assert.Null(finding.CommandText));
        Assert.Contains(report.Explanation.Statements, statement => statement.Contains("Possible N+1", StringComparison.Ordinal));
        Assert.DoesNotContain(report.Explanation.Statements, statement => statement.Contains("Customers", StringComparison.Ordinal));
        Assert.True(report.Explanation.IsSlow);
        Assert.Equal("Database", report.Explanation.PrimarySource);
    }

    [Fact]
    public void Stored_request_has_no_body_field()
    {
        Assert.DoesNotContain(
            typeof(RequestReport).GetProperties(),
            property => property.Name.Contains("Body", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Store_keeps_only_the_newest_requests()
    {
        var store = new InMemoryApiLensStore(2);
        for (var index = 0; index < 3; index++)
        {
            store.Add(RequestAnalyzer.Analyze(
                [],
                "GET",
                "/api/" + index,
                200,
                "trace",
                null,
                TimeSpan.FromMilliseconds(1),
                new ApiLensOptions(),
                ApiLensCaptureMode.Development));
        }

        var routes = store.List().Select(report => report.Route).ToArray();
        Assert.Equal(["/api/2", "/api/1"], routes);
    }
}
