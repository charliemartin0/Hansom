using Hansom.Application.Serialization;
using Hansom.Domain.Envelope;
using Hansom.Domain.Envelope.ValueObjects;
using Hansom.Domain.Messaging.ValueObjects;
using Hansom.Infrastructure.Serialization;
using Hansom.Tests.Domain;
using Hansom.Tests.Domain.Envelope;

namespace Hansom.Tests.Application.Serialization;

public sealed class SerializerContractTests
{
    [Fact]
    public void serialize_body_then_deserialize_body_round_trips_to_equivalent_object()
    {
        ISerializer serializer = new JsonSerializer();
        var original = new PlaceOrder(42);

        byte[] bytes = serializer.SerializeBody(original, typeof(PlaceOrder));
        var roundTripped = (PlaceOrder)serializer.DeserializeBody(bytes, typeof(PlaceOrder));

        Assert.Equal(original.OrderId, roundTripped.OrderId);
    }

    [Fact]
    public void serialize_then_deserialize_envelope_preserves_headers_and_content_type()
    {
        ISerializer serializer = new JsonSerializer();
        var headers = new Headers(new Dictionary<string, string>
        {
            ["trace-id"] = "abc-123",
            ["sender"] = "test",
        });
        Envelope envelope = EnvelopeFactory.Create(
            message: new Message(new PlaceOrder(7)),
            headers: headers,
            contentType: "application/json");

        byte[] bodyBytes = serializer.SerializeBody(envelope.Message.Value, typeof(PlaceOrder));
        var body = (PlaceOrder)serializer.DeserializeBody(bodyBytes, typeof(PlaceOrder));

        Assert.Equal(7, body.OrderId);
        Assert.Equal("abc-123", envelope.Headers.Value["trace-id"]);
        Assert.Equal("test", envelope.Headers.Value["sender"]);
        Assert.Equal("application/json", envelope.ContentType.Value);
    }

    [Fact]
    public void serialize_body_with_null_body_throws_argument_null_exception()
    {
        ISerializer serializer = new JsonSerializer();

        Assert.Throws<ArgumentNullException>(() => serializer.SerializeBody(null!, typeof(PlaceOrder)));
    }
}
