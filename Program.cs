using Microsoft.EntityFrameworkCore;
using Tickets.Data;
using Tickets.Demo;
using Tickets.Mistakes;
using Tickets.Purchases;

var builder = WebApplication.CreateBuilder(args);

var tickets = builder.Configuration
    .GetConnectionString("Tickets");
builder.Services.AddDbContext<TicketsDb>(o => o
    .UseNpgsql(tickets)
    .UseSnakeCaseNamingConvention());
builder.AddDemoPause();
builder.AddHandlers();
builder.Services.AddProblemDetails();

var app = builder.Build();
app.UseExceptionHandler();
await app.PrepareDatabaseAsync();

app.MapPost("/buy", async (BuyRequest r,
    IBuyHandler handler, CancellationToken ct) =>
{
    if (string.IsNullOrWhiteSpace(r.Phone))
        return Problem(400, "Нужен телефон");
    var result = await handler.BuyAsync(r, ct);
    return result switch
    {
        BuyResult.Ok => Results.Ok(),
        BuyResult.NotFound => Problem(404, "Места нет"),
        _ => Problem(409, "Место занято"),
    };
});

app.Run();

// Ошибки — ProblemDetails: код и заголовок, который покажет клиент.
static IResult Problem(int status, string title) =>
    Results.Problem(title: title, statusCode: status);
