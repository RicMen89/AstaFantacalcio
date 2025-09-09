using AstaFantacalcio.Models;
using Microsoft.Extensions.Options;
using System.Text;

namespace AstaFantacalcio.Services
{
    public enum AuctionMode
    {
        None,
        OpenBid,     // Offerte a rialzo
        ClosedBid    // Busta chiusa
    }


    public class AuctionService
    {
        private static List<Player> _allPlayers = new();
        private static Player? _currentPlayer;
        private static Random _random = new();
        private static bool _auctionOpen = false;
        private static int _currentBid = 0;
        private static string? _currentBidder;
        private static string? _currentTeamBidder;
        private static readonly List<Bid> _sealedBids = new();

        // Stato asta corrente
        private static AuctionType _auctionType = AuctionType.OpenAscending;
        //private static readonly Dictionary<string, Participant> _participants = new();
        private readonly Dictionary<string, Participant> _participantsByUser = new();
        private readonly Dictionary<string, Participant> _participantsByConnection = new();
        private readonly AuctionSettings _settings;
        private readonly ExcelImportService _excelService;

        static string exportPath = Path.Combine(Directory.GetCurrentDirectory(), @"wwwroot\CsvAcquisti");

        public string? _highestBidder;
        public decimal _highestBid = 0;
        public DateTime _auctionEndUtc;

        private static readonly object _lock = new();
        public AuctionService(IOptions<AuctionSettings> options, ExcelImportService excelService)
        {
            _settings = options.Value;
            _excelService = excelService;
        }

        public void LoadPlayers(List<Player> players)
        {
            lock (_lock)
            {
                _allPlayers = players;
                _currentPlayer = null;
                _auctionOpen = false;
                _currentBid = 0;
                _currentBidder = null;
                _currentTeamBidder = null;
                _sealedBids.Clear();

                var grouped = _allPlayers.GroupBy(p => p.RuoloPrincipale, StringComparer.OrdinalIgnoreCase);

                foreach (var group in grouped)
                {
                    var ordered = group
                        .OrderByDescending(p => p.Quotazione) // dal più alto al più basso
                        .ToList();

                    for (int i = 0; i < ordered.Count; i++)
                    {
                        ordered[i].RankRuolo = i + 1; // 1 = top player del ruolo
                    }
                }
            }

        }

        public Player? DrawRandomPlayerByRole(string? ruolo = null)
        {
            lock (_lock)
            {
                if (_auctionOpen) return null; // evita di cambiare giocatore durante un'asta

                var availablePlayers = _allPlayers
                    .Where(p => !p.IsAuctioned && !p.IsSelected)
                    .ToList();

                if (!string.IsNullOrEmpty(ruolo))
                {
                    availablePlayers = availablePlayers
                        .Where(p => p.RuoloPrincipale.Equals(ruolo, StringComparison.OrdinalIgnoreCase))
                        .ToList();
                }

                if (!availablePlayers.Any()) return null;

                var randomIndex = _random.Next(availablePlayers.Count);
                var selectedPlayer = availablePlayers[randomIndex];
                selectedPlayer.IsSelected = true;
                _currentPlayer = selectedPlayer;

                // reset stato asta
                _auctionOpen = false;
                _currentBid = 0;
                _currentBidder = null;
                _currentTeamBidder = null;
                _sealedBids.Clear();

                return selectedPlayer;
            }
        }

        public Player? DrawPlayerFromTopByQuotation(string? ruolo = null, int pickFromTop = 1)
        {
            lock (_lock)
            {
                // evita di cambiare giocatore mentre un'asta è aperta
                if (_auctionOpen) return null;

                var candidates = _allPlayers
                    .Where(p => !p.IsAuctioned && !p.IsSelected);

                if (!string.IsNullOrEmpty(ruolo))
                {
                    candidates = candidates.Where(p =>
                        p.RuoloPrincipale.Equals(ruolo, StringComparison.OrdinalIgnoreCase));
                }

                // ordina per Quotazione discendente
                var ordered = candidates
                    .OrderByDescending(p => p.Quotazione)
                    .ToList();

                if (ordered.Count == 0)
                    return null;

                // scegli tra i Top K (default 1 = il più quotato)
                var k = Math.Max(1, pickFromTop);
                var maxIndex = Math.Min(k, ordered.Count);
                var idx = _random.Next(maxIndex);
                var selected = ordered[idx];

                selected.IsSelected = true;
                _currentPlayer = selected;

                // reset stato asta
                _auctionOpen = false;
                _currentBid = 0;
                _currentBidder = null;
                _currentTeamBidder = null;
                _sealedBids.Clear();

                return selected;
            }
        }

        public Player? DrawPlayerFromQuotationRange(decimal min, decimal max, string? ruolo = null)
        {
            lock (_lock)
            {
                // Non cambiare giocatore se un'asta è già aperta
                if (_auctionOpen) return null;

                // Normalizza intervallo
                if (min > max)
                    (min, max) = (max, min);

                var candidates = _allPlayers
                    .Where(p => !p.IsAuctioned && !p.IsSelected);

                if (!string.IsNullOrWhiteSpace(ruolo))
                {
                    candidates = candidates.Where(p =>
                        p.RuoloPrincipale.Equals(ruolo, StringComparison.OrdinalIgnoreCase));
                }

                // Filtro per range di quotazione (inclusivo)
                candidates = candidates.Where(p => p.Quotazione >= min && p.Quotazione <= max);

                var list = candidates.ToList();
                if (list.Count == 0)
                    return null;

                // Pesca casuale tra i candidati nel range
                var idx = _random.Next(list.Count);
                var selected = list[idx];

                selected.IsSelected = true;
                _currentPlayer = selected;

                // reset stato asta
                _auctionOpen = false;
                _currentBid = 0;
                _currentBidder = null;
                _currentTeamBidder = null;
                _sealedBids.Clear();

                return selected;
            }
        }
        public Player? DrawPlayerFromRankRange(decimal min, decimal max, string ruolo)
        {
            lock (_lock)
            {
                // Non cambiare giocatore se un'asta è già aperta
                if (_auctionOpen) return null;

                // Normalizza intervallo
                if (min > max)
                    (min, max) = (max, min);

                var candidates = _allPlayers
                    .Where(p => !p.IsAuctioned && !p.IsSelected);

                if (!string.IsNullOrWhiteSpace(ruolo))
                {
                    candidates = candidates.Where(p =>
                        p.RuoloPrincipale.Equals(ruolo, StringComparison.OrdinalIgnoreCase));
                }

                // Filtro per range di quotazione (inclusivo)
                candidates = candidates.Where(p => p.RankRuolo >= min && p.RankRuolo <= max);

                var list = candidates.ToList();
                if (list.Count == 0)
                    return null;

                // Pesca casuale tra i candidati nel range
                var idx = _random.Next(list.Count);
                var selected = list[idx];

                selected.IsSelected = true;
                _currentPlayer = selected;

                // reset stato asta
                _auctionOpen = false;
                _currentBid = 0;
                _currentBidder = null;
                _currentTeamBidder = null;
                _sealedBids.Clear();

                return selected;
            }
        }

        public Player? GetCurrentPlayer() => _currentPlayer;

        public Dictionary<string, int> GetAvailablePlayersByRole()
        {
            var availablePlayers = GetAvailablePlayers();
            return availablePlayers
                .GroupBy(p => p.RuoloPrincipale)
                .ToDictionary(g => g.Key, g => g.Count());
        }

        public List<Player> GetAvailablePlayers()
        {
            lock (_lock)
            {
                return _allPlayers.Where(p => !p.IsAuctioned && !p.IsSelected).ToList();
            }
        }

        public List<Player> GetAuctionedPlayers()
        {
            lock (_lock)
            {
                return _allPlayers.Where(p => p.IsAuctioned).ToList();
            }
        }

        public AuctionViewModel GetAuctionStatus()
        {
            lock (_lock)
            {
                return new AuctionViewModel
                {
                    CurrentPlayer = _currentPlayer,
                    AvailablePlayers = GetAvailablePlayers(),
                    AuctionedPlayers = GetAuctionedPlayers(),
                    TotalPlayers = _allPlayers.Count,
                    RemainingPlayers = _allPlayers.Count(p => !p.IsAuctioned),
                    CurrentBid = _currentBid,
                    CurrentBidder = _currentBidder,
                    CurrentTeamBidder = _currentTeamBidder,
                    AuctionOpen = _auctionOpen,
                    SecondsRemaining = _auctionOpen ? (int)Math.Max(0, (_auctionEndUtc - DateTime.UtcNow).TotalSeconds) : 0,
                    AuctionType = _auctionType,
                    Settings = _settings,

                };
            }
        }

        // === Asta ===
        public bool CanOpenAuction(out string? reason)
        {
            lock (_lock)
            {
                if (_currentPlayer == null)
                {
                    reason = "Nessun giocatore selezionato";
                    return false;
                }
                if (_auctionOpen)
                {
                    reason = "Un'asta è già aperta";
                    return false;
                }
                reason = null;
                return true;
            }
        }

        public void OpenAuction(AuctionType type, DateTime endUtc)
        {
            lock (_lock)
            {
                _auctionType = type;
                _auctionEndUtc = endUtc;
                _auctionOpen = true;
                _currentBid = 0;
                _currentBidder = null;
                _currentTeamBidder = null;
                _sealedBids.Clear();
            }
        }

        public (string? winner, int price, string message) CloseAuction()
        {
            lock (_lock)
            {
                if (!_auctionOpen)
                    return (null, 0, "Nessuna asta aperta");

                _auctionOpen = false;

                string? winner = null;
                string? winnerTeam = null;
                int price = 0;

                if (_auctionType == AuctionType.OpenAscending)
                {
                    winner = _currentBidder;
                    winnerTeam = _currentTeamBidder;
                    price = _currentBid;
                }
                else // SealedFirstPrice
                {
                    var top = _sealedBids.OrderByDescending(b => b.Amount).FirstOrDefault();
                    if (top != null)
                    {
                        winner = top.Bidder;
                        winnerTeam = top.BidderTeam;
                        price = top.Amount;
                    }
                }

                if (_currentPlayer != null && winner != null && price > 0)
                {
                    _currentPlayer.IsAuctioned = true;
                    _currentPlayer.PrezzoVendita = price;
                    _currentPlayer.AcquirenteLega = winner;
                    _currentPlayer.AcquirenteSquadraLega = winnerTeam;
                    _currentPlayer.IsSelected = false;
                    _currentPlayer = null;


                    //Faccio un backup dei giocatori acquistati ogni volta che si chiude un'asta
                    string toBck = ExportAuctionedPlayersToCsv();
                    var bytes = Encoding.UTF8.GetBytes(toBck);

                    string filePath = Path.Combine(exportPath, $"auctioned_players_{DateTime.Now:yyyyMMdd}.csv");
                    if (!File.Exists(filePath))
                        File.Delete(filePath);

                    File.WriteAllBytes(filePath, bytes);

                }
                else if (_currentPlayer != null)
                {
                    // rimettiamo in palio se nessuna offerta valida
                    _currentPlayer.IsSelected = false;
                }

                return (winner, price, winner == null ? "Nessuna offerta valida" : "Asta assegnata");
            }
        }

        public void CancelAuction()
        {
            lock (_lock)
            {
                _auctionOpen = false;
                _sealedBids.Clear();
                _currentBid = 0;
                _currentBidder = null;
                _currentTeamBidder = null;
            }
        }

        // --- Offerte ---
        public bool TryPlaceOpenBid(string connectionId, int amount, out string? highBidder, out int highAmount, out string? error)
        {
            lock (_lock)
            {
                var participant = GetParticipant(connectionId);
                if (participant == null)
                {
                    error = "Partecipante non registrato.";
                    highBidder = null;
                    highAmount = 0;
                    return false;
                }

                highBidder = _currentBidder;
                highAmount = _currentBid;
                error = null;


                if (!_auctionOpen) { error = "Nessuna asta attiva"; return false; }
                if (_auctionType != AuctionType.OpenAscending) { error = "Modalità asta non corretta"; return false; }
                if (amount <= 0) { error = "Importo non valido"; return false; }
                if (amount <= _currentBid) { error = $"Offerta troppo bassa. Devi superare €{_currentBid}"; return false; }

                //Se l'offerta supera il budget disponibile
                if (amount > participant.BiddingBudget)
                {
                    error = $"Offerta troppo alta. Budget disponibile per le offerte: €{participant.BiddingBudget}";
                    return false;
                }

                //Verifico che non abbia superato il limite di giocatori per ruolo
                int playerInRole = participant.AcquiredPlayers.Where(p => p.RuoloPrincipale == _currentPlayer?.RuoloPrincipale).Count();

                switch (_currentPlayer?.RuoloPrincipale)
                {
                    case "P":
                        if (playerInRole >= participant.PorPlayerLimit)
                        {
                            error = $"Limite portieri raggiunto ({participant.PorPlayerLimit})";
                            return false;
                        }
                        break;
                    case "D":
                        if (playerInRole >= participant.DefPlayerLimit)
                        {
                            error = $"Limite difensori raggiunto ({participant.DefPlayerLimit})";
                            return false;
                        }
                        break;
                    case "C":
                        if (playerInRole >= participant.MidPlayerLimit)
                        {
                            error = $"Limite centrocampisti raggiunto ({participant.MidPlayerLimit})";
                            return false;
                        }
                        break;
                    case "A":
                        if (playerInRole >= participant.AttPlayerLimit)
                        {
                            error = $"Limite attaccanti raggiunto ({participant.AttPlayerLimit})";
                            return false;
                        }
                        break;
                }

                //Se i controlli sono ok, accetto l'offerta

                _currentBid = amount;
                _currentBidder = participant.Name;
                _currentTeamBidder = participant.TeamName;
                highBidder = participant.Name;
                highAmount = _currentBid;


                return true;
            }
        }

        public bool TryPlaceSealedBid(string connectionId, int amount, out string? error)
        {
            lock (_lock)
            {
                error = null;
                var participant = GetParticipant(connectionId);
                if (participant == null)
                {
                    error = "Partecipante non registrato.";
                    return false;
                }

                if (!_auctionOpen) { error = "Asta non aperta"; return false; }
                if (_auctionType != AuctionType.SealedFirstPrice) { error = "Modalità asta non corretta"; return false; }
                if (amount <= 0) { error = "Importo non valido"; return false; }

                _sealedBids.Add(new Bid { Bidder = participant.Name, BidderTeam = participant.TeamName, Amount = amount, TimestampUtc = DateTime.UtcNow });
                return true;
            }
        }

        public void CompleteAuction(int finalPrice, string buyer, string buyerTeam)
        {
            // Mantieni per compatibilità con il tuo controller
            lock (_lock)
            {
                if (_currentPlayer != null)
                {
                    _currentPlayer.IsAuctioned = true;
                    _currentPlayer.PrezzoVendita = finalPrice;
                    _currentPlayer.AcquirenteLega = buyer;
                    _currentPlayer.AcquirenteSquadraLega = buyerTeam;
                    _currentPlayer.IsSelected = false;
                    _currentPlayer = null;
                    _auctionOpen = false;
                    _sealedBids.Clear();
                    _currentBid = 0;
                    _currentBidder = null;
                    _currentTeamBidder = null;
                }
            }
        }

        public void ResetCurrentPlayer()
        {
            lock (_lock)
            {
                if (_currentPlayer != null)
                {
                    _currentPlayer.IsSelected = false;
                    _currentPlayer = null;
                }
                _auctionOpen = false;
                _sealedBids.Clear();
                _currentBid = 0;
                _currentBidder = null;
                _currentTeamBidder = null;
            }
        }


        //Partecipanti
        public Participant? RegisterParticipant(string connectionId, string userName, string team, string manager)
        {
            lock (_lock)
            {
                if (_participantsByUser.Values.Any(p => p.Name.Equals(userName, StringComparison.OrdinalIgnoreCase)))
                {
                    return _participantsByUser.Values.FirstOrDefault(p => p.Name.Equals(userName, StringComparison.OrdinalIgnoreCase));
                }
                if (_participantsByUser.Values.Any(p => p.TeamName.Equals(team, StringComparison.OrdinalIgnoreCase)))
                {
                    return _participantsByUser.Values.FirstOrDefault(p => p.TeamName.Equals(team, StringComparison.OrdinalIgnoreCase));
                }

                var participant = new Participant
                {
                    ConnectionId = connectionId,
                    Name = userName,
                    TeamName = team,
                    TeamManager = manager,
                    TotalBudget = _settings.CreditPerTeam,
                    PorPlayerLimit = _settings.PorPlayerLimit,
                    DefPlayerLimit = _settings.DefPlayerLimit,
                    MidPlayerLimit = _settings.MidPlayerLimit,
                    AttPlayerLimit = _settings.AttPlayerLimit,
                    ExtendSecondsOnBid = _settings.ExtendSecondsOnBid
                };

                _participantsByUser[userName] = participant;
                _participantsByConnection[connectionId] = participant;

                return participant;
            }
        }

        public void RemoveParticipant(string connectionId)
        {
            lock (_lock)
            {

                _participantsByConnection.Remove(connectionId);
            }
        }

        public Participant? GetParticipant(string connectionId)
        {
            lock (_lock)
            {
                var participant = _participantsByConnection.TryGetValue(connectionId, out var p) ? p : null;
                if (participant != null)
                {
                    participant.AcquiredPlayers = GetAuctionedPlayers()
                        .Where(p => p.AcquirenteLega.Equals(participant.Name, StringComparison.OrdinalIgnoreCase))
                        .ToList();
                }

                return participant;
            }
        }

        public List<Participant> GetAllParticipants()
        {
            lock (_lock) { return _participantsByUser.Values.ToList(); }
        }

        public Participant? Reconnect(string connectionId, string userName)
        {
            if (_participantsByUser.TryGetValue(userName, out var participant))
            {
                participant.ConnectionId = connectionId;
                _participantsByConnection[connectionId] = participant;

                participant.AcquiredPlayers = GetAuctionedPlayers()
                    .Where(p => p.AcquirenteLega.Equals(participant.Name, StringComparison.OrdinalIgnoreCase))
                    .ToList();

                return participant;
            }
            return null;
        }


        //Export in csv
        public string ExportAuctionedPlayersToCsv()
        {
            var sb = new StringBuilder();

            // intestazione
            sb.AppendLine("Squadra,PlayerID,Prezzo");

            foreach (var player in GetAuctionedPlayers())
            {
                sb.AppendLine($"{player.AcquirenteSquadraLega},{player.Id},{player.PrezzoVendita};");
            }

            return sb.ToString();
        }


        //Importa Listone Csv

        public int ImportListoneFromCsv(IFormFile file)
        {
            if (file != null && file.Length > 0)
            {
                try
                {
                    var players = _excelService.ImportPlayersFromCsv(file);
                    LoadPlayers(players);
                    return players.Count;

                }
                catch (Exception ex)
                {
                    return 0;
                }

            }
            return 0;
        }
   
        public bool ReloadAuctionedPlayers(out string? error, string filePath = "")
        {
            try
            {
                //Se non specificato prendo l'ultimo in ordine desc
                if (string.IsNullOrWhiteSpace(filePath))
                    filePath = new DirectoryInfo(exportPath).GetFiles().OrderByDescending(o => o.LastWriteTime).Select(f => f.FullName).FirstOrDefault();

                var lines = File.ReadAllLines(filePath);
                if (lines.Length <= 1)
                {
                    error = "File vuoto o non valido";
                    return false;
                }

                // Saltiamo intestazione
                foreach (var line in lines.Skip(1))
                {
                    //AcquirenteLega,PlayerID,Quotation
                    var parts = line.Split(';');
                    if (parts.Length < 3) continue;

                    var playerId = int.Parse(parts[1]);
                    var participantTeam = parts[0];

                    lock (_lock)
                    {
                        var player = _allPlayers.FirstOrDefault(p => p.Id == playerId);
                        if (player != null)
                        {
                            player.IsAuctioned = true;
                            player.IsSelected = true;
                            //player.AcquirenteLega = participant;
                            player.AcquirenteSquadraLega = participantTeam;
                        }
                    }
                }
                
                 error = null;
                return true;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
        }

   
    }
}