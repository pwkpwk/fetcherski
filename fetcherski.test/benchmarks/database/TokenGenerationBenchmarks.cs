using System.Text.Json;
using System.Text.Json.Nodes;
using BenchmarkDotNet.Attributes;

namespace fetcherski.test.benchmarks.database;

public class TokenGenerationBenchmarks
{
    private readonly byte[] _buffer = new byte[1024];
    private readonly DateTime _timestamp = DateTime.Now;

    [Benchmark(Baseline = true)]
    public string JsonObject()
    {
        using var stream = new MemoryStream(_buffer, true);
        using var writer = new Utf8JsonWriter(stream);

        var obj = new JsonObject
        {
            ["q"] = "table_name",
            ["t"] = _timestamp.Ticks,
            ["s"] = 6545,
            ["o"] = "ascending",
            ["p"] = 50
        };

        obj.WriteTo(writer);
        writer.Flush();

        return Convert.ToBase64String(_buffer, 0, (int)stream.Position);
    }

    [Benchmark]
    public string Utf8JsonWriter()
    {
        using var stream = new MemoryStream(_buffer, true);
        using var writer = new Utf8JsonWriter(stream);

        writer.WriteStartObject();
        writer.WriteString("q", "table_name");
        writer.WriteNumber("t", _timestamp.Ticks);
        writer.WriteNumber("s", 6545);
        writer.WriteString("o", "ascending");
        writer.WriteNumber("p", 50);
        writer.WriteEndObject();
        writer.Flush();

        return Convert.ToBase64String(_buffer, 0, (int)stream.Position);
    }
}