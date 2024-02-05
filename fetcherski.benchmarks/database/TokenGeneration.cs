using System.Text.Json;
using System.Text.Json.Nodes;
using BenchmarkDotNet.Attributes;
using fetcherski.tools;

namespace fetcherski.benchmarks.database;

[MemoryDiagnoser, ReturnValueValidator(failOnError: true)]
public class TokenGeneration
{
    private readonly byte[] _buffer = new byte[1024];
    private readonly DateTime _timestamp = DateTime.Now;

    [Benchmark(Baseline = true, Description = "Serialize JsonObject")]
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
            ["p"] = 50,
            ["x"] = new JsonArray(1, 2, 3, 4, 5)
        };

        obj.WriteTo(writer);
        writer.Flush();

        return Convert.ToBase64String(_buffer, 0, (int)stream.Position);
    }

    [Benchmark(Description = "Write to Utf8JsonWriter")]
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
        writer.WriteStartArray("x");
        writer.WriteNumberValue(1);
        writer.WriteNumberValue(2);
        writer.WriteNumberValue(3);
        writer.WriteNumberValue(4);
        writer.WriteNumberValue(5);
        writer.WriteEndArray();
        writer.WriteEndObject();
        writer.Flush();

        return Convert.ToBase64String(_buffer, 0, (int)stream.Position);
    }

    [Benchmark(Description = "Write to PooledMemoryBufferWriter")]
    public string PooledMemoryBufferWriter()
    {
        using var buffer = new PooledMemoryBufferWriter(1024);
        using var writer = new Utf8JsonWriter(buffer);

        writer.WriteStartObject();
        writer.WriteString("q", "table_name");
        writer.WriteNumber("t", _timestamp.Ticks);
        writer.WriteNumber("s", 6545);
        writer.WriteString("o", "ascending");
        writer.WriteNumber("p", 50);
        writer.WriteStartArray("x");
        writer.WriteNumberValue(1);
        writer.WriteNumberValue(2);
        writer.WriteNumberValue(3);
        writer.WriteNumberValue(4);
        writer.WriteNumberValue(5);
        writer.WriteEndArray();
        writer.WriteEndObject();
        writer.Flush();

        return Convert.ToBase64String(buffer.WrittenBytes);
    }
}