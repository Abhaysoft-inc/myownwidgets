using System.Globalization;
using System.Net.Http;
using System.Text.Json;

namespace CPContestWidget;

public sealed class ContestApiClient
{
    private static readonly HttpClient Http = new();
    private static readonly TimeZoneInfo IstTimeZone = TimeZoneInfo.FindSystemTimeZoneById("India Standard Time");

    public async Task<List<ContestItem>> GetNextDaysAsync(int dayCount, CancellationToken cancellationToken = default)
    {
        if (dayCount < 1)
        {
            dayCount = 1;
        }

        var localStart = DateTime.Today;
        var localEnd = localStart.AddDays(dayCount).AddTicks(-1);

        var startIso = new DateTimeOffset(localStart).ToUniversalTime().ToString("o", CultureInfo.InvariantCulture);
        var endIso = new DateTimeOffset(localEnd).ToUniversalTime().ToString("o", CultureInfo.InvariantCulture);

        var url = $"https://node.codolio.com/api/contest-calendar/v1/all/get-contests?startDate={Uri.EscapeDataString(startIso)}&endDate={Uri.EscapeDataString(endIso)}";

        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.TryAddWithoutValidation("accept", "*/*");
        request.Headers.TryAddWithoutValidation("origin", "https://codolio.com");
        request.Headers.TryAddWithoutValidation("referer", "https://codolio.com/");
        request.Headers.TryAddWithoutValidation("cache-control", "no-cache");
        request.Headers.TryAddWithoutValidation("pragma", "no-cache");
        request.Headers.TryAddWithoutValidation("user-agent", "Mozilla/5.0");

        using var response = await Http.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        var result = JsonSerializer.Deserialize<ContestApiResponse>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        var contests = result?.Data ?? [];
        var inclusiveLastDate = localStart.AddDays(dayCount - 1).Date;

        return contests
            .Where(c => c.ContestStartDate.HasValue)
            .Select(c =>
            {
                var istStartDate = TimeZoneInfo.ConvertTimeFromUtc(c.ContestStartDate!.Value.ToUniversalTime(), IstTimeZone);
                return new ContestItem
                {
                    Name = string.IsNullOrWhiteSpace(c.ContestName) ? "Untitled Contest" : c.ContestName,
                    Platform = string.IsNullOrWhiteSpace(c.Platform) ? "Unknown" : c.Platform,
                    Url = c.ContestUrl ?? string.Empty,
                    Start = istStartDate,
                    End = c.ContestEndDate.HasValue
                        ? TimeZoneInfo.ConvertTimeFromUtc(c.ContestEndDate.Value.ToUniversalTime(), IstTimeZone)
                        : null,
                    DurationSeconds = c.ContestDuration
                };
            })
            .Where(c => c.Start.Date >= localStart && c.Start.Date <= inclusiveLastDate)
            .OrderBy(c => c.Start)
            .ToList();
    }
}

public sealed class ContestItem
{
    public string Name { get; init; } = string.Empty;
    public string Platform { get; init; } = string.Empty;
    public string Url { get; init; } = string.Empty;
    public DateTime Start { get; init; }
    public DateTime? End { get; init; }
    public int? DurationSeconds { get; init; }
}

public sealed class ContestApiResponse
{
    public ContestStatus? Status { get; set; }
    public List<ContestApiItem>? Data { get; set; }
}

public sealed class ContestStatus
{
    public int Code { get; set; }
    public bool Success { get; set; }
    public string? Message { get; set; }
    public string? Error { get; set; }
}

public sealed class ContestApiItem
{
    public string? ContestName { get; set; }
    public string? Platform { get; set; }
    public string? ContestUrl { get; set; }
    public DateTime? ContestStartDate { get; set; }
    public DateTime? ContestEndDate { get; set; }
    public int? ContestDuration { get; set; }
}
