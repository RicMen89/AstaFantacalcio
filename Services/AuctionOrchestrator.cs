using AstaFantacalcio.Hubs;
using AstaFantacalcio.Models;
using Microsoft.AspNetCore.SignalR;

namespace AstaFantacalcio.Services
{
    public class AuctionOrchestrator
    {
        private readonly AuctionService _auctionService;
        private readonly IHubContext<AuctionHub> _hub;
        private CancellationTokenSource? _cts;
        private int _remainingSeconds;
        private readonly object _timerLock = new object();

        public AuctionOrchestrator(AuctionService auctionService, IHubContext<AuctionHub> hub)
        {
            _auctionService = auctionService;
            _hub = hub;
        }

        public async Task NotifyDrawedPlayer(Player player)
        {
            await _hub.Clients.All.SendAsync("PlayerDrawed", player);

        }

        public async Task<bool> OpenAuctionAsync(int seconds, AuctionType type)
        {
            if (!_auctionService.CanOpenAuction(out var reason))
                return false;

            _cts?.Cancel();
            _cts = new CancellationTokenSource();
            var endUtc = DateTime.UtcNow.AddSeconds(Math.Max(5, seconds));


            _auctionService.OpenAuction(type, endUtc);
            await _hub.Clients.All.SendAsync("AuctionOpened", new
            {
                type = type.ToString(),
                seconds = (int)(endUtc - DateTime.UtcNow).TotalSeconds
            });


            _ = RunTimerAsync(endUtc, _cts.Token);
            return true;
        }


        private async Task RunTimerAsync(DateTime endUtc, CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                _remainingSeconds = (int)Math.Max(0, (endUtc - DateTime.UtcNow).TotalSeconds);
                await _hub.Clients.All.SendAsync("TimerUpdate", _remainingSeconds);
                if (_remainingSeconds <= 0) break;
                try { await Task.Delay(1000, ct); } catch { }
            }


            if (!ct.IsCancellationRequested)
            {
                var (winner, price, message) = _auctionService.CloseAuction();
                await _hub.Clients.All.SendAsync("AuctionEnded", new { winner, price, message });
            }
        }

        public void ExtendTimer(int extraSeconds)
        {
            lock (_timerLock)
            {
                if (_remainingSeconds <= 10)
                {
                    _remainingSeconds += extraSeconds;
                }
            }
        }

        public void CancelAuction()
        {
            _cts?.Cancel();
            _auctionService.CancelAuction();
        }

  

    }
}
