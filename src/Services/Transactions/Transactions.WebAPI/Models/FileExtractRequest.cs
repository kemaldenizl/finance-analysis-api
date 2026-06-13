namespace Transactions.WebAPI.Models;
public sealed record FileExtractRequest(
    string input_id,
    string file_name
);