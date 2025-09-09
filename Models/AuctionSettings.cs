namespace AstaFantacalcio.Models
{
    public class AuctionSettings
    {
        public string LeagueName { get; set; } = "Fantacalcio Serie A";
        public string Season { get; set; } = DateTime.Now.Year.ToString() + "/" + (DateTime.Now.Year + 1).ToString();
        public int CreditPerTeam { get; set; } = 500;
        public int PorPlayerLimit { get; set; } = 3;
        public int DefPlayerLimit { get; set; } = 8;
        public int MidPlayerLimit { get; set; } = 8;
        public int AttPlayerLimit { get; set; } = 6;
        public int ExtendSecondsOnBid { get; set; } = 10;
    }
}
