using AstaFantacalcio.Models;
using AstaFantacalcio.Services;
using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;

namespace AstaFantacalcio.Hubs
{
    public class AuctionHub : Hub
    {
        private readonly AuctionService _auctionService;
        private readonly AuctionOrchestrator _auctionOrchestrator;
        public AuctionHub(AuctionService auctionService, AuctionOrchestrator auctionOrchestrator)
        {
            _auctionService = auctionService;
            _auctionOrchestrator = auctionOrchestrator;
        }

        //Utente
        public override async Task OnConnectedAsync()
        {
         
            // Puoi loggare o inviare un messaggio solo al nuovo utente
            await Clients.Caller.SendAsync("Connected", Context.ConnectionId);

            await Clients.All.SendAsync("ParticipantsUpdated", _auctionService.GetAllParticipants());

            await base.OnConnectedAsync();
        }

        public override async Task<Task> OnDisconnectedAsync(Exception? exception)
        {
         
            var connectionId = Context.ConnectionId;
            _auctionService.RemoveParticipant(connectionId);


            await Clients.All.SendAsync("ParticipantsUpdated", _auctionService.GetAllParticipants());
            return base.OnDisconnectedAsync(exception);
        }

        public async Task Reconnect(string userName)
        {
            var p = _auctionService.Reconnect(Context.ConnectionId, userName);

            if (p != null)
            {
                await Clients.Caller.SendAsync("Reconnected", p);

                await Clients.All.SendAsync("ParticipantsUpdated", _auctionService.GetAllParticipants());
            }
            else
            {
                await Clients.Caller.SendAsync("ReconnectionFailed", userName);
            }

            var status = _auctionService.GetAuctionStatus();
            if (status.AuctionOpen)
                await Clients.Caller.SendAsync("AuctionOpened", status); 
        }

        public async Task Register(string userName, string teamName, string managerName)
        {
            var p = _auctionService.RegisterParticipant(Context.ConnectionId, userName, teamName, managerName);
            if (p != null)
            {

                await Clients.Caller.SendAsync("RegistrationSuccess", p);
               
                await Clients.All.SendAsync("ParticipantsUpdated", _auctionService.GetAllParticipants());
            }
        }


        /// <summary>
        /// Stato attuale dell'asta (utile per nuovi client)
        /// </summary>
        public async Task GetAuctionStatus()
        {
            var status = _auctionService.GetAuctionStatus();
            await Clients.Caller.SendAsync("AuctionStatus", status);
        }

 
        // Offerta aperta
        public async Task PlaceOpenBid(int amount)
        {
            var result = _auctionService.TryPlaceOpenBid(Context.ConnectionId, amount, out var newHighBidder, out var newHighAmount, out var error);
            if (!result)
            {
                await Clients.Caller.SendAsync("BidRejected", error ?? "Offerta non valida");
                return;
            }

            await Clients.All.SendAsync("BidAccepted", newHighBidder, newHighAmount);

            _auctionOrchestrator.ExtendTimer(_auctionService.GetParticipant(Context.ConnectionId).ExtendSecondsOnBid);

        }

        // Offerta busta chiusa
        public async Task PlaceSealedBid(int amount)
        {
            var result = _auctionService.TryPlaceSealedBid(Context.ConnectionId, amount, out var error);
            if (!result)
            {
                await Clients.Caller.SendAsync("BidRejected", error ?? "Offerta non valida");
            }
        }

    }
}
