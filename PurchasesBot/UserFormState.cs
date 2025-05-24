using System.Collections.Generic;

namespace PurchasesBot
{
    public class UserFormState
    {
        public string? Name { get; set; }
        public HashSet<string> PhotoFileIds { get; set; } = new HashSet<string>();
        public string? MediaGroupId { get; set; }
        public bool AlbumMediaGroupIdSet { get; set; } = false;
        public string? Price { get; set; }
        public string? Section { get; set; }
        public string? Rating { get; set; }
        public string? Comment { get; set; }
        public int Step { get; set; } = 0;
    }
}

