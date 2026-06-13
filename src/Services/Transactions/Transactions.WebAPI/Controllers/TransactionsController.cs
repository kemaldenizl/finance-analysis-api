using Microsoft.AspNetCore.Mvc;
using Transactions.WebAPI.Models;

namespace Transactions.WebAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TransactionsController : ControllerBase
{
    private readonly IHttpClientFactory _httpClientFactory;

    public TransactionsController(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    [HttpPost("file-input")]
    [RequestSizeLimit(50 * 1024 * 1024)]
    public async Task<IActionResult> FileInput(
        [FromForm] string user_id,
        IFormFile file,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(user_id))
        {
            return BadRequest("user_id is required.");
        }

        if (file is null || file.Length == 0)
        {
            return BadRequest("A file is required.");
        }

        using var form = new MultipartFormDataContent();
        form.Add(new StringContent(user_id), "user_id");

        await using var fileStream = file.OpenReadStream();
        var fileContent = new StreamContent(fileStream);
        if (!string.IsNullOrWhiteSpace(file.ContentType))
        {
            fileContent.Headers.ContentType =
                new System.Net.Http.Headers.MediaTypeHeaderValue(file.ContentType);
        }

        form.Add(fileContent, "file", file.FileName);

        var client = _httpClientFactory.CreateClient("InputsApi");
        using var response = await client.PostAsync("/v1/inputs", form, cancellationToken);

        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

        object? parsedResponse = responseBody;
        try
        {
            parsedResponse = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(responseBody);
        }
        catch (System.Text.Json.JsonException)
        {
            parsedResponse = responseBody;
        }

        var result = new
        {
            response = parsedResponse,
            file = new
            {
                fileName = file.FileName,
                contentType = file.ContentType,
                size = file.Length,
                uploadedAt = DateTime.UtcNow
            }
        };

        return StatusCode((int)response.StatusCode, result);
    }

    [HttpPost("file-extract")]
    public async Task<IActionResult> FileExtract(
        [FromBody] FileExtractRequest request,
        CancellationToken cancellationToken
    ){
        string file_type = "";

        switch(request.file_name){
            case "scanned_pdf":
                file_type = "image";
                break;
            case "real_pdf":
                file_type = "pdf";
                break;
            case "camera_photo":
                file_type = "image";
                break;
            case "screenshot":
                file_type = "image";
                break;
            default:
                return BadRequest("Invalid file name.");
        }

        var client = _httpClientFactory.CreateClient("InputsApi");
        string url = "/v1/extractions/" + file_type + "/" + request.input_id;
        using var response = await client.PostAsync(url, null, cancellationToken);

        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

        object? parsedResponse = responseBody;
        try
        {
            parsedResponse = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(responseBody);
        }
        catch (System.Text.Json.JsonException)
        {
            parsedResponse = responseBody;
        }

        var result = new
        {
            response = parsedResponse,
        };

        var client2 = _httpClientFactory.CreateClient("InputsApi");
        string url2 = "/v1/normalizations/" + request.input_id;
        using var response2 = await client2.PostAsync(url2, null, cancellationToken);

        var responseBody2 = await response2.Content.ReadAsStringAsync(cancellationToken);

        object? parsedResponse2 = responseBody2;
        try
        {
            parsedResponse2 = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(responseBody2);
        }catch (System.Text.Json.JsonException)
        {
            parsedResponse2 = responseBody2;
        }
        var result2 = new
        {
            response = parsedResponse2,
        };

        return StatusCode((int)response.StatusCode, result2);
    }
    [HttpPost("ai-save")]
    public async Task<IActionResult> AISave(
        [FromBody] AISaveRequest request,
        CancellationToken cancellationToken
    ){
        var client = _httpClientFactory.CreateClient("InputsApi");
        string url = "/v1/ai/analyze-and-save";
        using var response = await client.PostAsJsonAsync(url, request, cancellationToken);

        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

        object? parsedResponse = responseBody;
        try
        {
            parsedResponse = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(responseBody);
        }
        catch (System.Text.Json.JsonException)
        {
            parsedResponse = responseBody;
        }
        var result = new
        {
            response = parsedResponse,
        };

        return StatusCode((int)response.StatusCode, result);
    }

    [HttpPost("ai-chat")]
    public async Task<IActionResult> AIChat([FromBody] AIChatRequest request, CancellationToken cancellationToken){
        var client = _httpClientFactory.CreateClient("InputsApi");
        string url = "/v1/ai/chat";
        using var response = await client.PostAsJsonAsync(url, request, cancellationToken);

        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

        object? parsedResponse = responseBody;
        try
        {
            parsedResponse = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(responseBody);
        }
        catch (System.Text.Json.JsonException)
        {
            parsedResponse = responseBody;
        }
        var result = new
        {
            response = parsedResponse,
        };
        return StatusCode((int)response.StatusCode, result);
    }
}
