using Grpc.Core;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using CurrencyDeltaGrpc.Models;

namespace CurrencyDeltaGrpc.Services;


public class CurrencyDeltaService : CurrencyDelta.CurrencyDeltaBase
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ExchangeRateApiSettings _settings;

    public CurrencyDeltaService(IHttpClientFactory httpClientFactory, IOptions<ExchangeRateApiSettings> settings)
    {
        _httpClientFactory = httpClientFactory;
        _settings = settings.Value;
    }

    public override async Task<CurrencyDeltaResponse> GetCurrencyDelta(CurrencyDeltaRequest request, ServerCallContext context)
    {
        ValidateRequest(request);

        var fromRates = await GetHistoricalRates(DateTime.Parse(request.FromDate), request.Baseline);
        var toRates = await GetHistoricalRates(DateTime.Parse(request.ToDate), request.Baseline);

        var response = new CurrencyDeltaResponse();
        foreach (var currency in request.Currencies)
        {
            if (fromRates.TryGetValue(currency, out var fromRate) && toRates.TryGetValue(currency, out var toRate))
            {
                var delta = toRate - fromRate;
                response.Results.Add(new CurrencyDeltaResult { Currency = currency, Delta = (double)Math.Round(delta, 3) });
            }
            else
            {
                throw new RpcException(new Status(StatusCode.InvalidArgument, $"Currency {currency} does not exist."));
            }
        }

        return response;
    }

    private async Task<Dictionary<string, decimal>> GetHistoricalRates(DateTime date, string baseCurrency)
    {
        var client = _httpClientFactory.CreateClient();
        var response = await client.GetAsync($"{_settings.BaseUrl}/{_settings.ApiKey}/history/{baseCurrency}/{date.Year}/{date.Month}/{date.Day}");
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync();
        var exchangeRate = JsonConvert.DeserializeObject<ExchangeRateDto>(content);

        if (exchangeRate == null || exchangeRate.ConversionRates == null)
        {
            throw new InvalidOperationException("Failed to retrieve exchange rates from the API.");
        }

        return exchangeRate.ConversionRates;
    }

    private void ValidateRequest(CurrencyDeltaRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Baseline))
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Baseline currency cannot be null or empty."));
        }

        if (request.Currencies == null || !request.Currencies.Any())
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Currencies list cannot be null or empty."));
        }

        if (request.Currencies.Contains(request.Baseline))
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Currencies must not contain the baseline currency."));
        }

        if (request.Currencies.Distinct().Count() != request.Currencies.Count)
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Currencies must be unique."));
        }

        if (!DateTime.TryParse(request.FromDate, out _) || !DateTime.TryParse(request.ToDate, out _))
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Dates must be in correct format."));
        }

        if (DateTime.Parse(request.FromDate) >= DateTime.Parse(request.ToDate))
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "To date must be greater than from date."));
        }
    }
}

public class ExchangeRateDto
{
    public string Result { get; set; }
    public string BaseCode { get; set; }
    public Dictionary<string, decimal> ConversionRates { get; set; }
}
