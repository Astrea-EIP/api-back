using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace proto_back.Models.Entities;

public class ErrorLog
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = null!;

    [BsonElement("errorId")]
    public string ErrorId { get; set; } = null!;

    [BsonElement("message")]
    public string Message { get; set; } = null!;

    [BsonElement("stackTrace")]
    public string? StackTrace { get; set; }

    [BsonElement("requestPath")]
    public string RequestPath { get; set; } = null!;

    [BsonElement("httpMethod")]
    public string HttpMethod { get; set; } = null!;

    [BsonElement("occurredAt")]
    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
}
