namespace PurchasesBot
{
    public interface ITokenHolder
    {
        string? Token { get; set; }
    }

    public class TokenHolder : ITokenHolder
    {
        public string? Token { get; set; }
    }
}

