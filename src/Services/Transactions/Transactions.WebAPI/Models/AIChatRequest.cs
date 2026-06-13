namespace Transactions.WebAPI.Models;

public sealed record AIChatRequest(
    string analysis_id,
    string question
);