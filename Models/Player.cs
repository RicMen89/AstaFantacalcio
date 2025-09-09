using System.ComponentModel.DataAnnotations;

namespace AstaFantacalcio.Models
{
    public class Player
    {
        public int Id { get; set; }

        [Required]
        public string Nome { get; set; }

        [Required]
        public string Squadra { get; set; }

        [Required]
        public string Ruolo { get; set; }

        public string RuoloMantra { get; set; }

        [Required]
        public decimal Quotazione { get; set; }
        public int RankRuolo { get; set; }
        public int Under { get; set; }
        public bool FuoriLista { get; set; } = false;
        
        // Campi per gestire l'asta
        public bool IsSelected { get; set; } = false;
        public bool IsAuctioned { get; set; } = false;
        public int? PrezzoVendita { get; set; }
        public string? AcquirenteLega { get; set; }
        public string? AcquirenteSquadraLega { get; set; }
        
        // Proprietà per visualizzare il ruolo principale
        public string RuoloPrincipale => Ruolo?.Split('/')[0] ?? "";

        // Proprietà per sapere se è un giocatore fuori lista
        public string DisplayNome => FuoriLista ? $"{Nome} *" : Nome;
    }
}
