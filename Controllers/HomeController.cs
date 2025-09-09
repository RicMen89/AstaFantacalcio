using AstaFantacalcio.Hubs;
using AstaFantacalcio.Models;
using AstaFantacalcio.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;
using System.Diagnostics;
using System.Net;
using System.Reflection;

namespace AstaFantacalcio.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly AuctionService _auctionService;

        static string uploadPath = Path.Combine(Directory.GetCurrentDirectory(), @"wwwroot\CsvListone");

        private readonly string _adminPassword;
        private readonly string _participatnPassword;

        public HomeController(ILogger<HomeController> logger, AuctionService auctionService, IOptions<AdminSettings> adminOptions, IOptions<ParticipantSettings> participantOptions)
        {
            _logger = logger;
            _auctionService = auctionService;
            _adminPassword = adminOptions.Value.Password;
            _participatnPassword = participantOptions.Value.Password;
        }

        public IActionResult Index()
        {
            var status = _auctionService.GetAuctionStatus();

            if (status.TotalPlayers == null || status.TotalPlayers <= 0)
            {
                //Se non c'� nessun giocatore caricato, prova a caricare l'ultimo file CSV nella cartella CsvGiocatori
                if (Path.Exists(uploadPath) && Directory.GetFiles(uploadPath).Length > 0)
                {
                    DirectoryInfo info = new DirectoryInfo(uploadPath);
                    FileInfo file = info.GetFiles().OrderByDescending(p => p.CreationTime).FirstOrDefault();
                    IFormFile formFile = new FormFile(new FileStream(file.FullName, FileMode.Open), 0, file.Length, "listino", file.Name);
                    
                    _ = _auctionService.ImportListoneFromCsv(formFile); 
                }
            }
            return View();
        }

        [HttpGet]
        public IActionResult AdminLogin()
        {
            return View(); // mostra form con input password
        }

        [HttpPost]
        public IActionResult AdminLogin(string password)
        {
            if (password == _adminPassword)
            {
                HttpContext.Session.SetString("IsAdmin", "true");
                return RedirectToAction("Admin","Auction");
            }

            ViewBag.Error = "Password non valida";
            return View();
        }

        [HttpGet]
        public IActionResult ParticipantLogin()
        {
            ParticipantLoginModel participantLogin = new ParticipantLoginModel();

            return View(participantLogin); // mostra form con input password
        }

       
        [HttpPost]
        public JsonResult ParticipantLogin([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] ParticipantLoginModel request)
        {
            if (request == null)
            {
                return new JsonResult(new
                {
                    StatusCode = (int)HttpStatusCode.BadRequest,
                    Message = "Richiesta non valida. Dati assenti."
                });
            }

            //Mi server solo questa
            if (request.Password == _participatnPassword)
            {
                HttpContext.Session.SetString("IsParticipant", "true");
                
                return new JsonResult(new
                {
                    StatusCode = (int)HttpStatusCode.OK,
                    Message = "Login Effettuato correttamente"
                });
            }
            
            return new JsonResult(new
            {
                StatusCode = (int)HttpStatusCode.BadRequest,
                Message = "Password non valida"
            });
        }


        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
