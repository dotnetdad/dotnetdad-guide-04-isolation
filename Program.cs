using Microsoft.EntityFrameworkCore;
using Tickets.Data;
using Tickets.Demo;
using Tickets.Mistakes;
using Tickets.Purchases;
using Tickets.Reports;
using Tickets.Sms;

var builder = WebApplication.CreateBuilder(args);

var tickets = builder.Configuration
    .GetConnectionString("Tickets");
builder.Services.AddDbContext<TicketsDb>(o => o
    .UseNpgsql(tickets, npgsql => npgsql
        .EnableRetryOnFailure(
            maxRetryCount: 3,
            maxRetryDelay: TimeSpan.FromSeconds(1),
            errorCodesToAdd: null))
    .UseSnakeCaseNamingConvention());
builder.AddDemoPause();
builder.AddHandlers();
builder.Services.AddSms();
builder.Services.AddProblemDetails();

var app = builder.Build();
app.UseExceptionHandler();
await app.PrepareDatabaseAsync();

app.MapPost("/buy", async (BuyRequest r,
    IBuyHandler handler, HttpResponse response,
    CancellationToken ct) =>
{
    if (!Phones.TryToE164(r.Phone, out var phone))
        return Problem(400, "Неверный телефон");
    var result = await handler.BuyAsync(
        r with { Phone = phone }, ct);
    if (result == BuyResult.Busy)
        response.Headers.RetryAfter = "1";
    return result switch
    {
        BuyResult.Ok => Results.Ok(),
        BuyResult.NotFound => Problem(404, "Места нет"),
        BuyResult.Taken => Problem(409, "Место занято"),
        BuyResult.Limit => Problem(409, "Лимит: 4 билета"),
        _ => Problem(503, "Попробуйте ещё раз"),
    };
});

app.MapGet("/concerts/{concertId:int}/report",
    (int concertId, IReportHandler handler,
        CancellationToken ct) => handler.GetAsync(concertId, ct));

app.Run();

// Ошибки — ProblemDetails: код и заголовок, который покажет клиент.
static IResult Problem(int status, string title) =>
    Results.Problem(title: title, statusCode: status);
