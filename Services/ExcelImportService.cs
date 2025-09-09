using AstaFantacalcio.Models;
using System.Globalization;

namespace AstaFantacalcio.Services
{
    public class ExcelImportService
    {
        public List<Player> ImportPlayersFromCsv(IFormFile file)
        {
            var players = new List<Player>();

            using (var reader = new StreamReader(file.OpenReadStream()))
            {
                string? line;
                bool isFirstLine = true;

                while ((line = reader.ReadLine()) != null)
                {
                    // Salta l'header
                    if (isFirstLine)
                    {
                        isFirstLine = false;
                        continue;
                    }

                    try
                    {
                        var columns = line.Split(';');
                        if (columns.Length < 12) continue; // Assicurati di avere abbastanza colonne

                        var player = new Player
                        {
                            Id = int.TryParse(columns[0], out var id) ? id : 0,
                            Nome = columns[1]?.Trim() ?? "",
                            FuoriLista = !string.IsNullOrEmpty(columns[2]?.Trim()),
                            Squadra = columns[3]?.Trim() ?? "",
                            Under = int.TryParse(columns[4], out var under) ? under : 0,
                            Ruolo = columns[5]?.Trim() ?? "",
                            RuoloMantra = columns[6]?.Trim() ?? "",
                            Quotazione = decimal.TryParse(columns[11], NumberStyles.Any, CultureInfo.InvariantCulture, out var quota) ? quota : 0,
                        };

                        if (!string.IsNullOrEmpty(player.Nome) && player.Quotazione > 0)
                            players.Add(player);
                    }
                    catch
                    {
                        // Skip righe con errori
                        continue;
                    }
                }
            }

            return players;
        }
    }
}
