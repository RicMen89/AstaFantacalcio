using Microsoft.AspNetCore.Mvc.Rendering;

namespace AstaFantacalcio.Models
{
    public class AuctionViewModel
    {
        public Player? CurrentPlayer { get; set; }
        public List<Player> AvailablePlayers { get; set; } = new();
        public List<Player> AuctionedPlayers { get; set; } = new();
        public decimal CurrentBid { get; set; }
        public string? CurrentBidder { get; set; }
        public string? CurrentTeamBidder { get; set; }
        public int TotalPlayers { get; set; }
        public int RemainingPlayers { get; set; }
        public bool AuctionOpen { get; set; }
        public int SecondsRemaining { get; set; }
        public AuctionType AuctionType { get; set; }
        public AuctionSettings? Settings { get; set; }
        public IEnumerable<SelectListItem> CsvAcquistiFiles { get; set; }
    }
}
