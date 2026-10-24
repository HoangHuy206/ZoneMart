using System;
using MongoDB.Bson.Serialization.Attributes;

namespace ZoneMart.Server.Models;

public class PasskeyCredential
{
    [BsonElement("id")]
    public byte[] Id { get; set; } = Array.Empty<byte>();

    [BsonElement("public_key")]
    public byte[] PublicKey { get; set; } = Array.Empty<byte>();

    [BsonElement("user_handle")]
    public byte[] UserHandle { get; set; } = Array.Empty<byte>();

    [BsonElement("signature_counter")]
    public uint SignatureCounter { get; set; }

    [BsonElement("cred_type")]
    public string CredType { get; set; } = "public-key";

    [BsonElement("reg_date")]
    public DateTime RegDate { get; set; } = DateTime.UtcNow;

    [BsonElement("aa_guid")]
    public string AaGuid { get; set; } = string.Empty;

    [BsonElement("device_name")]
    public string DeviceName { get; set; } = "Thiết bị sinh trắc học";

    [BsonElement("biometric_type")]
    public string BiometricType { get; set; } = "biometric"; // "face-id", "touch-id", "android-biometric", "windows-hello"
}

