using AplosGateway.Infrastructure.Virtuous;

namespace AplosGateway.Tests.Virtuous;

public sealed class AplosTransactionResponseParserTests
{
    [Fact]
    public void Parse_ValidResponse_ReturnsTypedResult()
    {
        const string response =
            """
            {
              "status": 200,
              "data": {
                "transaction": {
                  "id": 70064235
                }
              }
            }
            """;

        var parser =
            new AplosTransactionResponseParser();

        var result =
            parser.Parse(
                38241,
                response);

        Assert.Equal(
            "processed",
            result.Status);

        Assert.Equal(
            38241,
            result.GiftId);

        Assert.Equal(
            70064235,
            result.AplosTransactionId);
    }

    [Fact]
    public void Parse_MissingTransactionId_ThrowsInvalidOperationException()
    {
        const string response =
            """
            {
              "status": 200,
              "data": {
                "transaction": {
                }
              }
            }
            """;

        var parser =
            new AplosTransactionResponseParser();

        var exception =
            Assert.Throws<InvalidOperationException>(
                () => parser.Parse(
                    38241,
                    response));

        Assert.Contains(
            "transaction ID",
            exception.Message,
            StringComparison.OrdinalIgnoreCase);
    }
}