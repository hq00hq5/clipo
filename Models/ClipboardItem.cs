using System;

namespace Clipo.Models
{
    public class ClipboardItem
    {
        public int Id { get; set; }
        public string Text { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
    }
}
