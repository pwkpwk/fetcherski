using System.Net;
using System.Text;
using System.Text.Json;

namespace fetcherski.client;

public class Client(Uri baseUri)
{
    public IAsyncEnumerable<string[]> Query(int pageSize, bool descending) =>
        new Enumerable(baseUri, pageSize, descending);

    private class Enumerable(Uri baseUri, int pageSize, bool descending) : IAsyncEnumerable<string[]>
    {
        IAsyncEnumerator<string[]> IAsyncEnumerable<string[]>.GetAsyncEnumerator(CancellationToken cancellationToken)
        {
            return new Enumerator(baseUri, pageSize, descending, cancellationToken);
        }
    }

    private class Enumerator(
        Uri baseUri,
        int pageSize,
        bool descending,
        CancellationToken cancellation) : IAsyncEnumerator<string[]>
    {
        private readonly CancellationTokenSource _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellation);
        private readonly HttpClient _client = new();
        private bool _exhausted = false;
        private string[]? _current = null;
        private string? _continuationToken = null;

        async ValueTask<bool> IAsyncEnumerator<string[]>.MoveNextAsync()
        {
            if (_exhausted)
            {
                return false;
            }

            if (_continuationToken is null)
            {
                StringBuilder query = new("api/query?pageSize=");
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

        string[] IAsyncEnumerator<string[]>.Current => _current;

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
            _current = document.RootElement.Deserialize<string[]>();
                
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