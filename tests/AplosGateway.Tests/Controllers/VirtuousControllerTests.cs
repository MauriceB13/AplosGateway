using AplosGateway.Api.Controllers;
using AplosGateway.Core.Transactions;
using AplosGateway.Core.Virtuous;
using Microsoft.AspNetCore.Mvc;

namespace AplosGateway.Tests.Controllers;

public sealed class VirtuousControllerTests
{
[Fact]
public async Task ProcessGift_UnsupportedEvent_ReturnsBadRequest()
{
    var giftService =
        new StubGiftService(
            """{"status":200}""");

    var transactionMapper =
        new StubTransactionMapper(
            new AplosTransactionRequest());

    var webhookMapper =
        new ThrowingWebhookMapper(
            new InvalidOperationException(
                "Unsupported Virtuous event 'GiftUpdate'."));

    var controller =
        new VirtuousController(
            giftService,
            transactionMapper,
            webhookMapper);

    var request =
        new VirtuousGiftWebhookRequest
        {
            Event = "GiftUpdate"
        };

    var result =
        await controller.ProcessGift(
            request,
            CancellationToken.None);

    var badRequest =
        Assert.IsType<BadRequestObjectResult>(
            result);

    Assert.Equal(
        400,
        badRequest.StatusCode);

    Assert.Equal(
        0,
        giftService.CallCount);

    Assert.Equal(
        0,
        transactionMapper.CallCount);
}  

[Fact]
public void PreviewGift_UnsupportedEvent_ReturnsBadRequest()
{
    var giftService =
        new StubGiftService(
            """{"status":200}""");

    var transactionMapper =
        new StubTransactionMapper(
            new AplosTransactionRequest());

    var webhookMapper =
        new ThrowingWebhookMapper(
            new InvalidOperationException(
                "Unsupported Virtuous event 'GiftUpdate'."));

    var controller =
        new VirtuousController(
            giftService,
            transactionMapper,
            webhookMapper);

    var request =
        new VirtuousGiftWebhookRequest
        {
            Event = "GiftUpdate"
        };

    var result =
        controller.PreviewGift(request);

    var badRequest =
        Assert.IsType<BadRequestObjectResult>(
            result.Result);

    Assert.Equal(
        400,
        badRequest.StatusCode);

    Assert.Equal(
        0,
        giftService.CallCount);

    Assert.Equal(
        0,
        transactionMapper.CallCount);
}

    [Fact]
    public async Task ProcessGift_MapsWebhook_AndProcessesGift()
    {
        var mappedGift =
            new VirtuousGift
            {
                Id = 38241,
                ContactName = "John Smith",
                GiftDateUtc =
                    new DateTime(
                        2026,
                        9,
                        2,
                        0,
                        0,
                        0,
                        DateTimeKind.Utc),
                Amount = 150m,
                Project = "Reunion Golf Hole Sponsor",
                ProjectCode = "49999-25",
                Segment = "2026 Golf Scramble Web All Contacts "
            };

        var webhookMapper =
            new StubWebhookMapper(mappedGift);

        var giftService =
            new StubGiftService(
                """{"status":200,"transactionId":70064235}""");

        var transactionMapper =
            new StubTransactionMapper(
                new AplosTransactionRequest());

        var controller =
            new VirtuousController(
                giftService,
                transactionMapper,
                webhookMapper);

        var request =
            CreateWebhookRequest();

        var result =
            await controller.ProcessGift(
                request,
                CancellationToken.None);

        var contentResult =
            Assert.IsType<ContentResult>(result);

        Assert.Equal(
            "application/json",
            contentResult.ContentType);

        Assert.Equal(
            """{"status":200,"transactionId":70064235}""",
            contentResult.Content);

        Assert.Same(
            request,
            webhookMapper.LastRequest);

        Assert.Same(
            mappedGift,
            giftService.LastGift);

        Assert.Equal(
            1,
            webhookMapper.CallCount);

        Assert.Equal(
            1,
            giftService.CallCount);
    }

    [Fact]
    public void PreviewGift_MapsWebhook_AndReturnsTransaction()
    {
        var mappedGift =
            new VirtuousGift
            {
                Id = 38241,
                ContactName = "John Smith",
                GiftDateUtc =
                    new DateTime(
                        2026,
                        9,
                        2,
                        0,
                        0,
                        0,
                        DateTimeKind.Utc),
                Amount = 150m
            };

        var expectedTransaction =
            new AplosTransactionRequest
            {
                Note = "Preview transaction"
            };

        var webhookMapper =
            new StubWebhookMapper(mappedGift);

        var transactionMapper =
            new StubTransactionMapper(
                expectedTransaction);

        var giftService =
            new StubGiftService(
                """{"status":200}""");

        var controller =
            new VirtuousController(
                giftService,
                transactionMapper,
                webhookMapper);

        var request =
            CreateWebhookRequest();

        var result =
            controller.PreviewGift(request);

        var okResult =
            Assert.IsType<OkObjectResult>(
                result.Result);

        Assert.Same(
            expectedTransaction,
            okResult.Value);

        Assert.Same(
            request,
            webhookMapper.LastRequest);

        Assert.Same(
            mappedGift,
            transactionMapper.LastGift);

        Assert.Equal(
            1,
            webhookMapper.CallCount);

        Assert.Equal(
            1,
            transactionMapper.CallCount);

        Assert.Equal(
            0,
            giftService.CallCount);
    }

    private static VirtuousGiftWebhookRequest
        CreateWebhookRequest()
    {
        return new VirtuousGiftWebhookRequest
        {
            Event = "GiftCreate",

            EventId =
                "6b2be4f5-b67b-4808-a800-9fadef0c5d01",

            EventDateTimeUtc =
                new DateTime(
                    2026,
                    9,
                    3,
                    16,
                    13,
                    7,
                    DateTimeKind.Utc),

            Gift =
                new VirtuousWebhookGift
                {
                    Id = 38241,
                    ContactId = 6020,
                    ContactName = "John Smith",
                    GiftType = "Check",

                    GiftDateUtc =
                        new DateTime(
                            2026,
                            9,
                            2,
                            0,
                            0,
                            0,
                            DateTimeKind.Utc),

                    Amount = 150m,
                    CurrencyCode = "USD",
                    CheckNumber = "1000",

                    Segment =
                        "2026 Golf Scramble Web All Contacts ",

                    SegmentCode =
                        "2026GSWBAC",

                    State = "ME",

                    GiftDesignations =
                    [
                        new VirtuousGiftDesignation
                        {
                            Id = 38414,
                            GiftId = 38241,
                            ProjectId = 104,

                            Project =
                                "Reunion Golf Hole Sponsor",

                            ProjectCode =
                                "49999-25",

                            AmountDesignated = 150m
                        }
                    ]
                },

            Organization =
                new VirtuousOrganization
                {
                    Id = 7472,
                    Name = "Maine Central Institute"
                },

            OrganizationUser =
                new VirtuousOrganizationUser
                {
                    Id = 157923,
                    EmailAddress = "test@example.org"
                }
        };
    }

    private sealed class StubWebhookMapper
        : IVirtuousWebhookMapper
    {
        private readonly VirtuousGift _gift;

        public StubWebhookMapper(
            VirtuousGift gift)
        {
            _gift = gift;
        }

        public VirtuousGiftWebhookRequest?
            LastRequest { get; private set; }

        public int CallCount { get; private set; }

        public VirtuousGift Map(
            VirtuousGiftWebhookRequest request)
        {
            LastRequest = request;
            CallCount++;

            return _gift;
        }
    }

    private sealed class StubTransactionMapper
        : IVirtuousGiftTransactionMapper
    {
        private readonly AplosTransactionRequest
            _transaction;

        public StubTransactionMapper(
            AplosTransactionRequest transaction)
        {
            _transaction = transaction;
        }

        public VirtuousGift?
            LastGift { get; private set; }

        public int CallCount { get; private set; }

        public AplosTransactionRequest Map(
            VirtuousGift gift)
        {
            LastGift = gift;
            CallCount++;

            return _transaction;
        }
    }

    private sealed class StubGiftService
        : IVirtuousGiftService
    {
        private readonly string _result;

        public StubGiftService(
            string result)
        {
            _result = result;
        }

        public VirtuousGift?
            LastGift { get; private set; }

        public int CallCount { get; private set; }

        public Task<string> ProcessGiftAsync(
            VirtuousGift gift,
            CancellationToken cancellationToken = default)
        {
            LastGift = gift;
            CallCount++;

            return Task.FromResult(
                _result);
        }
    }

    private sealed class ThrowingWebhookMapper
    : IVirtuousWebhookMapper
{
    private readonly Exception _exception;

    public ThrowingWebhookMapper(
        Exception exception)
    {
        _exception = exception;
    }

    public VirtuousGift Map(
        VirtuousGiftWebhookRequest request)
    {
        throw _exception;
    }
}
}