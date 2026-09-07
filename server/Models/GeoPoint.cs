using MongoDB.Bson.Serialization.Attributes;

namespace ZoneMart.Server.Models;

public class GeoPoint
{
    [BsonElement("type")]
    public string Type { get; set; } = "Point";

    // [Kinh độ (Longitude), Vĩ độ (Latitude)]
    [BsonElement("coordinates")]
    public double[] Coordinates { get; set; } = [105.8342, 21.0278];
}
