namespace Transactions.WebAPI.Models;

public sealed record AISaveRequest(
    string input_id,
    string status,
    AISaveResult result,
    AISaveScores scores,
    HistoricalTransaction[] historical_transactions,
    string question,
    PurchaseScenario purchase_scenario,
    bool use_llm
);

public sealed record PurchaseScenario(
    int amount,
    string currency,
    int max_installment_months
);

public sealed record AISaveScores(
    ScoreSummary summary
);
public sealed record ScoreSummary(
    float overall_confidence
);

public sealed record AISaveResult(
    Transaction[] transactions,
    Summary summary
);

public sealed record Transaction(
    string transaction_id,
    string date,
    string description,
    Merchant merchant,
    float amount,
    string currency,
    string direction
);

public sealed record Summary(
    string primary_currency,
    float average_confidence
);

public sealed record Merchant(
    string normalized
);

public sealed record HistoricalTransaction(
    string transaction_id,
    string date,
    string description,
    Merchant merchant,
    float amount,
    string currency,
    string direction
);