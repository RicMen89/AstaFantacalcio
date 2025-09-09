using AstaFantacalcio.Models;
using AstaFantacalcio.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Text;

namespace AstaFantacalcio.Controllers
{
    public class AuctionController : Controller
    {
        
        private readonly AuctionService _auctionService;
        private readonly AuctionOrchestrator _orchestrator;
      
        //static AuctionType defaultType = AuctionType.OpenAscending;

        public AuctionController(AuctionService auctionService, AuctionOrchestrator orchestrator)
        {
            
            _auctionService = auctionService;
            _orchestrator = orchestrator;;
        }

        public IActionResult Index()
        {
            if (HttpContext.Session.GetString("IsParticipant") != "true")
                return RedirectToAction("ParticipantLogin", "Home");

            var model = _auctionService.GetAuctionStatus();
            ViewBag.RoleStats = _auctionService.GetAvailablePlayersByRole();
            return View(model);
        }
        public IActionResult Admin()
        {
            if (HttpContext.Session.GetString("IsAdmin") != "true")
                return RedirectToAction("AdminLogin","Home");
            

            var model = _auctionService.GetAuctionStatus();
            ViewBag.RoleStats = _auctionService.GetAvailablePlayersByRole();

            string exportPath = Path.Combine(Directory.GetCurrentDirectory(), @"wwwroot\CsvAcquisti");
            model.CsvAcquistiFiles = new DirectoryInfo(exportPath).GetFiles().OrderByDescending(o => o.LastWriteTime).Select(f => new SelectListItem { Value = f.FullName, Text = f.Name });
            return View(model);
        }
        

        [HttpPost]
        public IActionResult UploadListino(IFormFile file)
        {
            if (HttpContext.Session.GetString("IsAdmin") != "true")
                return RedirectToAction("AdminLogin", "Home");

            int importedPlayers = _auctionService.ImportListoneFromCsv(file);

            if (importedPlayers > 0)
            {
                TempData["Success"] = $"Caricati {importedPlayers} giocatori dal listino!";
            }
            else
            {
                TempData["Error"] = "Errore nel caricamento del listino. Assicurati che il file sia in formato CSV corretto.";
            }

            return RedirectToAction("Admin");
        }
        [HttpPost]
        public IActionResult ReloadAuctionedPlayers(string filePath)
        {
            if (HttpContext.Session.GetString("IsAdmin") != "true")
                return RedirectToAction("AdminLogin", "Home");

            if (_auctionService.ReloadAuctionedPlayers(out var error, filePath))
            {
                TempData["Success"] = $"Acquisti ricaricati correttamente";
            }
            else
            {
                TempData["Error"] = error;
            }

            return RedirectToAction("Admin");

        }
   
        [HttpPost]
        public IActionResult DrawPlayer(string? ruolo = null)
        {
            if (HttpContext.Session.GetString("IsAdmin") != "true")
                return RedirectToAction("AdminLogin", "Home");

            var player = _auctionService.DrawRandomPlayerByRole(ruolo);
            if (player == null)
            {
                var roleText = string.IsNullOrEmpty(ruolo) ? "" : $" per il ruolo {ruolo}";
                TempData["Warning"] = $"Non ci sono più giocatori disponibili{roleText}!";
            }
            else
            {
                var fuoriListaText = player.FuoriLista ? " (FUORI LISTA)" : "";
                TempData["Success"] = $"Estratto: {player.Nome} ({player.Squadra}) - {player.RuoloMantra} - Quotazione: €{player.Quotazione}{fuoriListaText}";
            }

            return RedirectToAction("Admin");
        }

        [HttpPost]
        public IActionResult DrawFromTop(string ruolo, int topN)
        {
            if (HttpContext.Session.GetString("IsAdmin") != "true")
                return RedirectToAction("AdminLogin", "Home");

            var player = _auctionService.DrawPlayerFromTopByQuotation(ruolo, topN);
            if (player == null)
            {
                TempData["Warning"] = $"Non ci sono giocatori disponibili tra i top {topN} {ruolo}!";
            }
            else
            {
                var fuoriListaText = player.FuoriLista ? " (FUORI LISTA)" : "";
                TempData["Success"] = $"Estratto dai TOP {topN}: {player.Nome} ({player.Squadra}) - {player.RuoloMantra} - Quotazione: €{player.Quotazione}{fuoriListaText}";
            }

            return RedirectToAction("Admin");
        }


        [HttpPost]
        public async Task<IActionResult> DrawFromRangeAsync(string ruolo, decimal minQuota, decimal maxQuota)
        {
            if (HttpContext.Session.GetString("IsAdmin") != "true")
                return RedirectToAction("AdminLogin", "Home");

            var player = _auctionService.DrawPlayerFromRankRange(minQuota, maxQuota, ruolo);
            if (player == null)
            {
                TempData["Warning"] = $"Non ci sono giocatori {ruolo} disponibili nella fascia rank {minQuota}-{maxQuota}!";
            }
            else
            {
                var fuoriListaText = player.FuoriLista ? " (FUORI LISTA)" : "";
                TempData["Success"] = $"Estratto ({minQuota}-{maxQuota}): {player.Nome} ({player.Squadra}) - {player.RuoloMantra} - Quotazione: {player.Quotazione}{fuoriListaText}";

                await _orchestrator.NotifyDrawedPlayer(player);

            }

            return RedirectToAction("Admin");
        }

        [HttpPost]
        public async Task<IActionResult> OpenAuction(int seconds, AuctionType type)
        {
            if (HttpContext.Session.GetString("IsAdmin") != "true")
                return RedirectToAction("AdminLogin", "Home");

            var result = await _orchestrator.OpenAuctionAsync(seconds, type);
            if (!result)
                TempData["Error"] = "Impossibile aprire l'asta.";

            return RedirectToAction("Admin");
        }
        [HttpPost]
        public IActionResult CancelAuction()
        {
            if (HttpContext.Session.GetString("IsAdmin") != "true")
                return RedirectToAction("AdminLogin", "Home");

            _orchestrator.CancelAuction();
            return RedirectToAction("Admin");
        }

        [HttpPost]
        public IActionResult CompleteAuction(int finalPrice, string buyer, string buyerTeam)
        {
            if (HttpContext.Session.GetString("IsAdmin") != "true")
                return RedirectToAction("AdminLogin", "Home");

            _auctionService.CompleteAuction(finalPrice, buyer, buyerTeam);
            TempData["Success"] = "Asta completata!";
            return RedirectToAction("Admin");
        }

  
        public IActionResult SkipPlayer()
        {
            if (HttpContext.Session.GetString("IsAdmin") != "true")
                return RedirectToAction("AdminLogin", "Home");

            _auctionService.ResetCurrentPlayer();
            TempData["Info"] = "Giocatore rimesso in palio";
            return RedirectToAction("Admin");
        }

        
        public IActionResult ExportAuctionedPlayers()
        {
            if (HttpContext.Session.GetString("IsAdmin") != "true")
                return RedirectToAction("AdminLogin", "Home");

            var csvContent = _auctionService.ExportAuctionedPlayersToCsv();
            var bytes = Encoding.UTF8.GetBytes(csvContent);

            return File(bytes, "text/csv", "auctioned_players.csv");
        }
    }
}
