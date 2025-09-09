namespace AstaFantacalcio.Models
{
    public class Participant
    {
        public string ConnectionId { get; set; } = string.Empty; // univoco per SignalR
        public string Name { get; set; } = string.Empty;        // nome persona
        public string TeamName { get; set; } = string.Empty;        // squadra scelta
        public string TeamManager { get; set; } = string.Empty; // Allenatore squadra

        public int PorPlayerLimit { get; set; } = 3;
        public int DefPlayerLimit { get; set; } = 8;
        public int MidPlayerLimit { get; set; } = 8;
        public int AttPlayerLimit { get; set; } = 6;
        public int PlayerLimit
        {
            get
            {
                return PorPlayerLimit + DefPlayerLimit + MidPlayerLimit + AttPlayerLimit;
            }
        }

        public int TotalBudget { get; set; }
        public int CurrentBudget
        {
            get
            {
                if (AcquiredPlayers != null && AcquiredPlayers.Count > 0)
                {
                    return TotalBudget - AcquiredPlayers.Sum(p => p.PrezzoVendita ?? 0);
                }

                return TotalBudget;
            }
        }
        public int BiddingBudget
        {
            get
            {
                return CurrentBudget - (PlayerLimit - AcquiredPlayers.Count);
            }
        }
        public List<Player> AcquiredPlayers { get; set; } = new List<Player>();

        public int PorPlayersAquired
        {
            get
            {
                if (AcquiredPlayers != null && AcquiredPlayers.Count > 0)
                    return AcquiredPlayers.Count(p => p.RuoloPrincipale == "P");

                return 0;
            }
        }
        public int DefPlayersAquired
        {
            get
            {
                if (AcquiredPlayers != null && AcquiredPlayers.Count > 0)
                    return AcquiredPlayers.Count(p => p.RuoloPrincipale == "D");

                return 0;
            }
        }
        public int MidPlayersAquired
        {
            get
            {
                if (AcquiredPlayers != null && AcquiredPlayers.Count > 0)
                    return AcquiredPlayers.Count(p => p.RuoloPrincipale == "C");

                return 0;
            }
        }
        public int AttPlayersAquired
        {
            get
            {
                if (AcquiredPlayers != null && AcquiredPlayers.Count > 0)
                    return AcquiredPlayers.Count(p => p.RuoloPrincipale == "A");

                return 0;
            }
        }

        public int ExtendSecondsOnBid { get; set; } = 10;
    }


    public class ParticipantLoginModel
    {
        public string UserName { get; set; } = string.Empty;
        public string ManagerName { get; set; } = string.Empty;
        public string TeamName { get; set; } = string.Empty;
        public string Password { get; set; }
    }
    
 

}
