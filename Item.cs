namespace NotifierBotV2
{
    public class State
    {
        public int LastItem { get; set; } = 0;
        public string LastDate { get; set; } = string.Empty;
    }

    public class Item
    {
        public int Number { get; set; }
        public string Chapter { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string Reference { get; set; } = string.Empty;
    }
}
