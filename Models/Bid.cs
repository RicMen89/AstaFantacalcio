
namespace AstaFantacalcio.Models
{
    public class Bid
    {
        public string Bidder { get; set; } = string.Empty;
        public string BidderTeam { get; set; } = string.Empty;
        public int Amount { get; set; }
        public DateTime TimestampUtc { get; set; } = DateTime.UtcNow;
    }
}

