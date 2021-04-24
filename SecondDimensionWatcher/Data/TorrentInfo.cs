using System.Text.Json.Serialization;

namespace SecondDimensionWatcher.Data
{
    public class TorrentInfo
    {
        [JsonPropertyName("eta")] public int Eta { get; set; }

        [JsonPropertyName("state")] public string State { get; set; }

        [JsonPropertyName("progress")] public double Progress { get; set; }

        [JsonPropertyName("content_path")] public string SavePath { get; set; }

        [JsonPropertyName("dlspeed")] public int Speed { get; set; }
        [JsonPropertyName("hash")] public string Hash { get; set; }
    }
}