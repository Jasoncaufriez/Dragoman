namespace Dragoman.Server.Controllers
{
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.Configuration;
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Text.Json;

    // --- Modèles de Données ---

    public class AdUserStatusDto
    {
        public string SamAccountName { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string Ou { get; set; } = string.Empty;

        public string? PasswordLastSet { get; set; }
        public string? PasswordExpiresOn { get; set; }
        public string PasswordStatus { get; set; } = string.Empty;

        public string? LastLogonDate { get; set; }
        public string InactivityStatus { get; set; } = string.Empty;

        public int? DaysUntilExpiration { get; set; }

        public bool IsNormal { get; set; }
        public string? Comment { get; set; }
    }

    public class AdUserCommentDto
    {
        public string SamAccountName { get; set; } = string.Empty;
        public string Comment { get; set; } = string.Empty;
    }

    public class AdUserNormalStatusDto
    {
        public string SamAccountName { get; set; } = string.Empty;
        public bool IsNormal { get; set; }
    }

    public class AdUserPersistence
    {
        public string SamAccountName { get; set; } = string.Empty;
        public string? Comment { get; set; }
        public bool IsNormal { get; set; }
    }

    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = @"INTRRDM01\gg_rol_SystemAdministrator")]
    public class AdStatusController : ControllerBase
    {
        private readonly string _csvPath;
        private readonly string _persistencePath;

        private Dictionary<string, AdUserPersistence> _persistenceData = new();
        private readonly object _lock = new object();

        // Culture + formats dates attendus (EU + fallback ISO)
        private static readonly CultureInfo FrBe = CultureInfo.GetCultureInfo("fr-BE");

        private static readonly string[] DateFormats = new[]
        {
            "dd-MM-yy HH:mm:ss",      // format PowerShell actuel
            "dd-MM-yyyy HH:mm:ss",    // si 4 chiffres
            "yyyy-MM-dd HH:mm:ss",    // fallback ISO
            "yyyy-MM-ddTHH:mm:ss",
            "yyyy-MM-ddTHH:mm:ss.fff"
        };

        public AdStatusController(IConfiguration configuration)
        {
            _csvPath = configuration.GetValue<string>("AdStatus:CsvPath")
                      ?? @"D:\Dragoman\Data\AD_Users.csv";

            _persistencePath = Path.Combine(Path.GetDirectoryName(_csvPath) ?? "Data", "adstatus_persistence.json");

            LoadPersistenceData();
        }

        private void LoadPersistenceData()
        {
            lock (_lock)
            {
                if (System.IO.File.Exists(_persistencePath))
                {
                    try
                    {
                        var jsonString = System.IO.File.ReadAllText(_persistencePath);
                        var list = JsonSerializer.Deserialize<List<AdUserPersistence>>(jsonString);
                        _persistenceData = list?.ToDictionary(p => p.SamAccountName, p => p)
                                           ?? new Dictionary<string, AdUserPersistence>();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Erreur lors du chargement du fichier de persistance: {ex.Message}");
                        _persistenceData = new Dictionary<string, AdUserPersistence>();
                    }
                }
            }
        }

        private void SavePersistenceData()
        {
            lock (_lock)
            {
                var jsonOptions = new JsonSerializerOptions { WriteIndented = true };
                var jsonString = JsonSerializer.Serialize(_persistenceData.Values.ToList(), jsonOptions);
                System.IO.File.WriteAllText(_persistencePath, jsonString);
            }
        }

        [HttpGet]
        public ActionResult<IEnumerable<AdUserStatusDto>> GetAll()
        {
            if (!System.IO.File.Exists(_csvPath))
            {
                return NotFound($"Fichier CSV introuvable à: {_csvPath}");
            }

            var users = new List<AdUserStatusDto>();

            using var fs = new FileStream(_csvPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);

            // detectEncodingFromByteOrderMarks = true => gère BOM si présent
            using var reader = new StreamReader(fs, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);

            var headerLine = reader.ReadLine();
            if (headerLine == null) return Ok(users);

            // Sécurise le cas BOM UTF-8 qui se retrouve dans le 1er header
            headerLine = headerLine.TrimStart('\uFEFF');

            var headers = headerLine.Trim().Trim('"')
                .Split(';')
                .Select(h => h.Trim().Trim('"').Trim('\uFEFF'))
                .ToArray();

            // Mapping index
            int idxSamAccountName = Array.IndexOf(headers, "SamAccountName");
            int idxDisplayName = Array.IndexOf(headers, "DisplayName");
            int idxOu = Array.IndexOf(headers, "OU");

            int idxPwdLastSet = Array.IndexOf(headers, "PasswordLastSet");
            int idxPwdExpiryDate = Array.IndexOf(headers, "PasswordExpiryDate");
            int idxPwdExpired = Array.IndexOf(headers, "PasswordExpired");
            int idxPwdNeverExpires = Array.IndexOf(headers, "PasswordNeverExpires");
            int idxPwdStatus = Array.IndexOf(headers, "PasswordStatus"); // si présent dans ton CSV

            int idxLastLogonDate = Array.IndexOf(headers, "LastLogonDate");
            int idxInactive = Array.IndexOf(headers, "Inactive");
            int idxInactiveSoon = Array.IndexOf(headers, "InactiveSoon");

            // Fail-fast si colonne critique absente
            if (idxSamAccountName < 0)
            {
                return BadRequest("CSV invalide: colonne SamAccountName introuvable (headers/BOM).");
            }

            string? line;
            while ((line = reader.ReadLine()) != null)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;

                var cols = line.Split(';');

                var samAccountName = GetString(cols, idxSamAccountName);

                // --- Données CSV ---
                bool isPwdExpired = GetBool(cols, idxPwdExpired);
                bool isPwdNeverExpires = GetBool(cols, idxPwdNeverExpires);

                // Si le CSV contient déjà PasswordStatus (Expires15d/7d/24h/OK/Expired/NeverExpires),
                // on l’utilise en priorité.
                string csvPwdStatus = GetString(cols, idxPwdStatus);
                string passwordStatus;

                if (!string.IsNullOrWhiteSpace(csvPwdStatus))
                {
                    passwordStatus = csvPwdStatus;
                }
                else
                {
                    passwordStatus = isPwdExpired ? "Expired"
                                  : isPwdNeverExpires ? "NeverExpires"
                                  : "OK";
                }

                // Inactivité
                bool isInactive = GetBool(cols, idxInactive);
                bool isInactiveSoon = GetBool(cols, idxInactiveSoon);

                string inactivityStatus = isInactive ? "Inactive90Plus"
                                        : isInactiveSoon ? "InactiveSoon"
                                        : "Active";

                // Dates (parsing robuste EU + fallback)
                string pwdLastSetStr = GetString(cols, idxPwdLastSet);
                string expiryStr = GetString(cols, idxPwdExpiryDate);
                string lastLogonStr = GetString(cols, idxLastLogonDate);

                DateTime? pwdLastSetDt = TryParseDate(pwdLastSetStr, out var pls) ? pls : null;
                DateTime? expiryDt = TryParseDate(expiryStr, out var exp) ? exp : null;
                DateTime? lastLogonDt = TryParseDate(lastLogonStr, out var lld) ? lld : null;

                // Calcul jours restants (basé sur date d’expiration)
                int? daysUntilExpiration = null;
                if (expiryDt.HasValue)
                {
                    daysUntilExpiration = (int)Math.Floor((expiryDt.Value - DateTime.Now).TotalDays);
                }

                // --- Fusion avec persistance JSON ---
                _persistenceData.TryGetValue(samAccountName, out var persistence);

                var dto = new AdUserStatusDto
                {
                    SamAccountName = samAccountName,
                    DisplayName = GetString(cols, idxDisplayName),
                    Ou = GetString(cols, idxOu),

                    // Normalisation des dates renvoyées au front (stable)
                    PasswordLastSet = NormalizeDate(pwdLastSetDt),
                    PasswordExpiresOn = NormalizeDate(expiryDt),
                    LastLogonDate = NormalizeDate(lastLogonDt),

                    PasswordStatus = passwordStatus,
                    InactivityStatus = inactivityStatus,
                    DaysUntilExpiration = daysUntilExpiration,

                    IsNormal = persistence?.IsNormal ?? false,
                    Comment = persistence?.Comment
                };

                users.Add(dto);
            }

            return Ok(users);
        }

        [HttpPost("comment")]
        public IActionResult SaveComment([FromBody] AdUserCommentDto dto)
        {
            if (string.IsNullOrEmpty(dto.SamAccountName)) return BadRequest();

            if (!_persistenceData.TryGetValue(dto.SamAccountName, out var persistence))
            {
                persistence = new AdUserPersistence { SamAccountName = dto.SamAccountName };
                _persistenceData[dto.SamAccountName] = persistence;
            }

            persistence.Comment = dto.Comment;
            SavePersistenceData();

            return NoContent();
        }

        [HttpPost("normalstatus")]
        public IActionResult SaveNormalStatus([FromBody] AdUserNormalStatusDto dto)
        {
            if (string.IsNullOrEmpty(dto.SamAccountName)) return BadRequest();

            if (!_persistenceData.TryGetValue(dto.SamAccountName, out var persistence))
            {
                persistence = new AdUserPersistence { SamAccountName = dto.SamAccountName };
                _persistenceData[dto.SamAccountName] = persistence;
            }

            persistence.IsNormal = dto.IsNormal;
            SavePersistenceData();

            return NoContent();
        }

        private static bool TryParseDate(string? input, out DateTime dt)
        {
            dt = default;
            if (string.IsNullOrWhiteSpace(input)) return false;

            input = input.Trim().Trim('"');

            return DateTime.TryParseExact(
                input,
                DateFormats,
                FrBe,
                DateTimeStyles.AllowWhiteSpaces | DateTimeStyles.AssumeLocal,
                out dt
            );
        }

        private static string NormalizeDate(DateTime? dt)
        {
            // Renvoie vide si pas de date. Sinon stable et EU (4 chiffres).
            return dt.HasValue
                ? dt.Value.ToString("dd-MM-yyyy HH:mm:ss", FrBe)
                : string.Empty;
        }

        private static string GetString(string[] cols, int index)
        {
            if (index < 0 || index >= cols.Length) return string.Empty;
            return cols[index]?.Trim().Trim('\"') ?? string.Empty;
        }

        private static bool GetBool(string[] cols, int index)
        {
            if (index < 0 || index >= cols.Length) return false;

            var raw = cols[index]?.Trim().Trim('\"');
            return bool.TryParse(raw, out var result) && result;
        }
    }
}
