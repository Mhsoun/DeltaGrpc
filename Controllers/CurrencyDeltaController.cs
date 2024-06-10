using CurrencyDeltaGrpc;
using Grpc.Net.Client;
using Microsoft.AspNetCore.Mvc;


namespace CurrencyDeltaGrpc.Controllers;

[ApiController]
[Route("[controller]")]
public class CurrencyDeltaController : ControllerBase
{
    private readonly IConfiguration _configuration;

    public CurrencyDeltaController(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    [HttpPost]
    public async Task<IActionResult> GetCurrencyDelta([FromBody] CurrencyDeltaRequest request)
    {
        var grpcUrl = _configuration["GrpcServiceUrl"];
        using var channel = GrpcChannel.ForAddress(grpcUrl);
        var client = new CurrencyDelta.CurrencyDeltaClient(channel);

        var grpcRequest = new CurrencyDeltaRequest
        {
            Baseline = request.Baseline,
            FromDate = request.FromDate,
            ToDate = request.ToDate
        };
        grpcRequest.Currencies.AddRange(request.Currencies);

        var grpcResponse = await client.GetCurrencyDeltaAsync(grpcRequest);

        return Ok(grpcResponse);
    }
}