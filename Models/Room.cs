namespace AstralLite.Models
{
    public class Room
    {
        public string Name { get; set; } = string.Empty;
        public string PlayerCount { get; set; } = string.Empty;
        public string Ping { get; set; } = string.Empty;
        public bool IsHost { get; set; }
        public string GameType { get; set; } = string.Empty;
    }
}
