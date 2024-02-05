using System.Text.Json;
using System.Text.Json.Nodes;
using fetcherski.tools;

namespace fetcherski.test.tools;

[TestFixture]
public class PooledMemoryBufferWriterTests
{
    [Test]
    public void AdvancePastEnd_Throws()
    {
        using PooledMemoryBufferWriter buffer = new(32);

        Assert.Throws<ArgumentOutOfRangeException>(() => buffer.Advance(100));
    }
    
    [Test]
    public void WriteBytes_GetWrittenBytes_SameData()
    {
        using PooledMemoryBufferWriter buffer = new(32);

        var writeSpan = buffer.GetSpan(5);
        writeSpan[0] = 1;
        writeSpan[1] = 2;
        writeSpan[2] = 3;
        writeSpan[3] = 4;
        writeSpan[4] = 5;
        buffer.Advance(5);

        var writtenData = buffer.WrittenBytes;
        Assert.That(writtenData.ToArray(), Is.EquivalentTo(new byte[] { 1, 2, 3, 4, 5 }));
    }

    [Test]
    public void WriteToUtf8JsonWriter_Deserialize_SameProperties()
    {
        DateTime now = DateTime.Now;
        using PooledMemoryBufferWriter buffer = new(1024);
        using var writer = new Utf8JsonWriter(buffer);

        writer.WriteStartObject();
        writer.WriteString("q", "table_name");
        writer.WriteNumber("t", now.Ticks);
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

        var node = JsonSerializer.Deserialize<JsonNode>(buffer.WrittenBytes)!;

        Assert.That(node["q"]!.AsValue().GetValue<string>(), Is.EqualTo("table_name"));
        Assert.That(node["t"]!.AsValue().GetValue<long>(), Is.EqualTo(now.Ticks));
        Assert.That(node["s"]!.AsValue().GetValue<int>(), Is.EqualTo(6545));
        Assert.That(node["o"]!.AsValue().GetValue<string>(), Is.EqualTo("ascending"));
        Assert.That(node["p"]!.AsValue().GetValue<int>(), Is.EqualTo(50));
        Assert.That(node["x"]!.AsArray().GetValues<int>(), Is.EquivalentTo(new[] { 1, 2, 3, 4, 5 }));
    }
}