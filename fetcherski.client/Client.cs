﻿using System.Net;
using System.Text;
using System.Text.Json;

namespace fetcherski.client;

public class Client(Uri baseUri)
{
    public record struct Item(Guid id, string description);

    public IAsyncEnumerable<Item[]> QueryLooseItemsAsync(int pageSize, bool descending = false) =>
        new Enumerable(baseUri, "query-loose-items", pageSize, descending);

    public IAsyncEnumerable<Item[]> QueryPackItemsAsync(int pageSize, bool descending = false) =>
        new Enumerable(baseUri, "query-pack-items", pageSize, descending);

    private class Enumerable(
        Uri baseUri,
        string query,
        int pageSize,
        bool descending) : IAsyncEnumerable<Item[]>
    {
        IAsyncEnumerator<Item[]> IAsyncEnumerable<Item[]>.GetAsyncEnumerator(CancellationToken cancellationToken)
        {
            return new Enumerator(baseUri, query, pageSize, descending, cancellationToken);
        }
    }

    private class Enumerator(
        Uri baseUri,
        string queryName,
        int pageSize,
        bool descending,
        CancellationToken cancellation) : IAsyncEnumerator<Item[]>
    {
        private readonly CancellationTokenSource _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellation);
        private readonly HttpClient _client = new();
        private bool _exhausted = false;
        private Item[]? _current = null;
        private string? _continuationToken = null;

        async ValueTask<bool> IAsyncEnumerator<Item[]>.MoveNextAsync()
        {
            if (_exhausted)
            {
                return false;
            }

            if (_continuationToken is null)
            {
                StringBuilder query = new("api/");
                query.Append(queryName);
                query.Append("?pageSize=");
                query.Append(pageSize);
                if (descending)
                {
                    query.Append("&order=descending");
                }

                using HttpRequestMessage request = new(HttpMethod.Get, new Uri(baseUri, query.ToString()));
                using var response = await _client.SendAsync(request, _cts.Token);

                return await ProcessResponse(response);
            }
            else
            {
                using HttpRequestMessage request = new(HttpMethod.Get, new Uri(baseUri, "api/continue"));
                request.Headers.Add("X-Continuation-Token", _continuationToken);
                using var response = await _client.SendAsync(request, _cts.Token);

                return await ProcessResponse(response);
            }
        }

        Item[] IAsyncEnumerator<Item[]>.Current => _current;

        public ValueTask DisposeAsync()
        {
            _cts.Cancel();
            _cts.Dispose();
            _client.Dispose();
            return ValueTask.CompletedTask;
        }

        private async Task<bool> ProcessResponse(HttpResponseMessage response)
        {
            if (response.StatusCode != HttpStatusCode.OK)
            {
                _exhausted = true;
                return false;
            }

            await using var stream = await response.Content.ReadAsStreamAsync(_cts.Token);
            var document = await JsonDocument.ParseAsync(stream, default, _cts.Token);
            _current = document.RootElement.Deserialize<Item[]>(new JsonSerializerOptions { IncludeFields = true });

            if (_current is null || !response.Headers.TryGetValues("X-Continuation-Token", out var values))
            {
                _exhausted = true;
            }
            else
            {
                _continuationToken = values.First();
            }

            return _current is not null && _current.Length > 0;
        }
    }
}