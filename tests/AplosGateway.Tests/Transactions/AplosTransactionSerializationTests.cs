using System.Text.Json;
using AplosGateway.Core.Transactions;
using Xunit;
using AplosGateway.Core.Configuration;
using Microsoft.Extensions.Options;

namespace AplosGateway.Tests.Transactions;

public sealed class AplosTransactionSerializationTests
{
    [Fact]
    public void TransactionRequest_SerializesToExpectedAplosShape()
    {
        var request = new AplosTransactionRequest
        {
            Note = "Virtuous Gift 12345",
            Date = "2026-08-28",
            Contact = new AplosTransactionContact
            {
                CompanyName = "Virtuous",
                Type = "company"
            },
            Lines =
            [
                new AplosTransactionLine
                {
                    Amount = 100.00m,
                    Account = new AplosTransactionAccount
                    {
                        AccountNumber = 20114
                    },
                    Fund = new AplosFund
                    {
                        Id = 100
                    }
                },
                new AplosTransactionLine
                {
                    Amount = -100.00m,
                    Account = new AplosTransactionAccount
                    {
                        AccountNumber = 41025
                    },
                    Fund = new AplosFund
                    {
                        Id = 100
                    }
                }
            ]
        };

        var json = JsonSerializer.Serialize(request);

        using var document = JsonDocument.Parse(json);

        var root = document.RootElement;

        Assert.Equal(
            "Virtuous Gift 12345",
            root.GetProperty("note").GetString());

        Assert.Equal(
            "2026-08-28",
            root.GetProperty("date").GetString());

        var contact =
            root.GetProperty("contact");

        Assert.Equal(
            "Virtuous",
            contact.GetProperty("companyname").GetString());

        Assert.Equal(
            "company",
            contact.GetProperty("type").GetString());

        var lines =
            root.GetProperty("lines");

        Assert.Equal(
            2,
            lines.GetArrayLength());

        Assert.Equal(
            100.00m,
            lines[0].GetProperty("amount").GetDecimal());

        Assert.Equal(
            20114,
            lines[0]
                .GetProperty("account")
                .GetProperty("account_number")
                .GetInt32());

        Assert.Equal(
            100,
            lines[0]
                .GetProperty("fund")
                .GetProperty("id")
                .GetInt32());

        Assert.Equal(
            -100.00m,
            lines[1].GetProperty("amount").GetDecimal());

        Assert.Equal(
            41025,
            lines[1]
                .GetProperty("account")
                .GetProperty("account_number")
                .GetInt32());

        Assert.Equal(
            100,
            lines[1]
                .GetProperty("fund")
                .GetProperty("id")
                .GetInt32());
    }
}