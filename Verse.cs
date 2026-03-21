namespace NotifierBotV2
{
    public class State
    {
        public int LastVerse { get; set; } = 0;
        public string LastDate { get; set; } = string.Empty;
    }

    public class Verse
    {
        public int Number { get; set; }
        public string Chapter { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string Reference { get; set; } = string.Empty;
    }
}
