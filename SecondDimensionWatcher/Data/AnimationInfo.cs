using System;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;

namespace SecondDimensionWatcher.Data
{
    [Index(nameof(Hash), IsUnique = true)]
    public class AnimationInfo
    {
        public string Id { get; set; }
        public string Description { get; set; }
        public DateTimeOffset PublishTime { get; set; }
        public string TorrentUrl { get; set; }
        public byte[] TorrentData { get; set; }
        public string Hash { get; set; }
        public bool IsTracked { get; set; } = false;
        public DateTimeOffset TrackTime { get; set; }
        public bool IsFinished { get; set; } = false;
        public string StorePath { get; set; }
    }

    public class AnimationInfoDto
    {
        public string Id { get; set; }
        public string Description { get; set; }

        [JsonConverter(typeof(ReadableConverter))]
        public DateTimeOffset PublishTime { get; set; }

        public string Hash { get; set; }
        public bool IsTracked { get; set; }
    }

    public class ReadableConverter : JsonConverter<DateTimeOffset>
    {
        public override DateTimeOffset Read(ref Utf8JsonReader reader, Type typeToConvert,
            JsonSerializerOptions options)
        {
            return DateTimeOffset.Parse(reader.GetString() ?? throw new InvalidOperationException());
        }

        public override void Write(Utf8JsonWriter writer, DateTimeOffset value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.ToString("f", CultureInfo.CurrentCulture));
        }
    }
}