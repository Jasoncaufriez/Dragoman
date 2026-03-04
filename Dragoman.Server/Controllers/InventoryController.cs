using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Dragoman.Server.Controllers
{
    public class MachineRecord
    {
        public string ComputerName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? DNSHostName { get; set; }
        public string? OperatingSystem { get; set; }

        public string? LastIPAddress { get; set; }
        public string? LastLocalisation { get; set; }

        public string? GlobalProtectVersion { get; set; }
        public string? GlobalProtectStatus { get; set; }

        public bool LastEnLigne { get; set; }
        public DateTime LastScanDateUtc { get; set; }

        // NOUVEAUX CHAMPS
        public bool VerifiedByTeam { get; set; }
        public string? Remark { get; set; }
    }

    public class MachineUpdateDto
    {
        public bool VerifiedByTeam { get; set; }
        public string? Remark { get; set; }
    }

    [ApiController]
    [Route("api/[controller]")]
    public class InventoryController : ControllerBase
    {
        // Chemins de fichiers (dans le dossier de l'appli)
        public static string DataRoot
            => Path.Combine(AppContext.BaseDirectory, "Data");

        public static string MasterJson
            => Path.Combine(DataRoot, "GlobalProtectInventory.json");

        public static string LastCsv
            => Path.Combine(DataRoot, "LastUpload.csv");

        // GET api/inventory
        [HttpGet]
        public async Task<ActionResult<IEnumerable<MachineRecord>>> GetAll(CancellationToken ct)
        {
            var machines = await LoadMasterAsync(ct);
            var sorted = SortMachines(machines).ToList();
            return Ok(sorted);
        }

        // POST api/inventory/import (upload CSV)
        [HttpPost("import")]
        public async Task<ActionResult<IEnumerable<MachineRecord>>> Import(
            IFormFile file,
            CancellationToken ct)
        {
            if (file == null || file.Length == 0)
                return BadRequest("CSV manquant ou vide.");

            var machines = await ImportCsvAsync(file, ct);
            var sorted = SortMachines(machines).ToList();
            return Ok(sorted);
        }

        // PUT api/inventory/{computerName}
        // → met à jour VerifiedByTeam + Remark pour UNE machine
        [HttpPut("{computerName}")]
        public async Task<ActionResult<MachineRecord>> Update(
            string computerName,
            [FromBody] MachineUpdateDto dto,
            CancellationToken ct)
        {
            var machines = await LoadMasterAsync(ct);
            var machine = machines
                .FirstOrDefault(m => string.Equals(m.ComputerName, computerName, StringComparison.OrdinalIgnoreCase));

            if (machine == null)
                return NotFound();

            machine.VerifiedByTeam = dto.VerifiedByTeam;
            machine.Remark = dto.Remark;

            await SaveMasterAsync(machines, ct);

            return Ok(machine);
        }

        // Tri :
        // 1) au bureau + en ligne en premier
        // 2) ceux qui ont une version GlobalProtect
        // 3) version numérique (6.3.3 > 6.2.8)
        // 4) nom machine
        private static IEnumerable<MachineRecord> SortMachines(IEnumerable<MachineRecord> machines)
        {
            return machines
                .OrderByDescending(m =>
                    m.LastEnLigne &&
                    (m.LastLocalisation == "Bureau" ||
                     m.LastLocalisation == "Bureau (autre site)"))
                .ThenByDescending(m => !string.IsNullOrWhiteSpace(m.GlobalProtectVersion))
                .ThenBy(m =>
                {
                    if (Version.TryParse(m.GlobalProtectVersion, out var v))
                        return v;
                    return new Version(0, 0);
                })
                .ThenBy(m => m.ComputerName);
        }

        // Charge le JSON maître (dernier état connu)
        private async Task<List<MachineRecord>> LoadMasterAsync(CancellationToken ct)
        {
            if (!System.IO.File.Exists(MasterJson))
                return new List<MachineRecord>();

            await using var stream = System.IO.File.OpenRead(MasterJson);
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
            var list = await JsonSerializer.DeserializeAsync<List<MachineRecord>>(stream, options, ct);
            return list ?? new List<MachineRecord>();
        }

        // Sauvegarde du JSON maître
        private async Task SaveMasterAsync(List<MachineRecord> machines, CancellationToken ct)
        {
            Directory.CreateDirectory(DataRoot);

            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            };

            await using var stream = System.IO.File.Create(MasterJson);
            await JsonSerializer.SerializeAsync(stream, machines, options, ct);
        }

        // Import CSV envoyé par l'appli, merge dans le JSON maître
        private async Task<List<MachineRecord>> ImportCsvAsync(IFormFile file, CancellationToken ct)
        {
            Directory.CreateDirectory(DataRoot);

            // Copie brute du CSV (optionnel)
            await using (var fs = System.IO.File.Create(LastCsv))
            await using (var uploadStream = file.OpenReadStream())
            {
                await uploadStream.CopyToAsync(fs, ct);
            }

            using var reader = new StreamReader(LastCsv);

            var master = await LoadMasterAsync(ct);
            var dict = master.ToDictionary(m => m.ComputerName, StringComparer.OrdinalIgnoreCase);

            string? headerLine = await reader.ReadLineAsync();
            if (headerLine == null)
                return master;

            // Détection du séparateur (PowerShell FR = ;, EN = ,)
            char sep = headerLine.Contains(';') ? ';' : ',';

            var headerRaw = headerLine.Split(sep);
            var header = headerRaw
                .Select(h => h.Trim().Trim('"', '\uFEFF'))
                .ToArray();

            int idxComputerName = GetIndex(header, "ComputerName");
            int idxDNSHostName = GetIndex(header, "DNSHostName");
            int idxDescription = GetIndex(header, "Description");
            int idxOperatingSystem = GetIndex(header, "OperatingSystem");
            int idxIPAddress = GetIndex(header, "IPAddress");
            int idxLocalisation = GetIndex(header, "Localisation");
            int idxGPVersion = GetIndex(header, "GlobalProtectVersion");
            int idxGPStatus = GetIndex(header, "GlobalProtectStatus");
            int idxEnLigne = GetIndex(header, "EnLigne");

            var nowUtc = DateTime.UtcNow;

            while (!reader.EndOfStream)
            {
                var line = await reader.ReadLineAsync();
                if (string.IsNullOrWhiteSpace(line))
                    continue;

                var cols = line.Split(sep);

                string computerName = SafeCol(cols, idxComputerName);
                if (string.IsNullOrWhiteSpace(computerName))
                    continue;

                string dns = SafeCol(cols, idxDNSHostName);
                string description = SafeCol(cols, idxDescription);
                string os = SafeCol(cols, idxOperatingSystem);
                string ip = SafeCol(cols, idxIPAddress);
                string localisation = SafeCol(cols, idxLocalisation);
                string gpVersion = SafeCol(cols, idxGPVersion);
                string gpStatus = SafeCol(cols, idxGPStatus);
                string enLigneStr = SafeCol(cols, idxEnLigne);

                bool enLigne = false;
                bool.TryParse(enLigneStr, out enLigne);

                if (!dict.TryGetValue(computerName, out var m))
                {
                    m = new MachineRecord
                    {
                        ComputerName = computerName
                    };
                    dict[computerName] = m;
                }

                // Mise à jour des derniers états connus (ne touche PAS VerifiedByTeam / Remark)
                if (!string.IsNullOrWhiteSpace(dns)) m.DNSHostName = dns;
                if (!string.IsNullOrWhiteSpace(description)) m.Description = description;
                if (!string.IsNullOrWhiteSpace(os)) m.OperatingSystem = os;
                if (!string.IsNullOrWhiteSpace(ip)) m.LastIPAddress = ip;
                if (!string.IsNullOrWhiteSpace(localisation)) m.LastLocalisation = localisation;

                m.LastEnLigne = enLigne;
                m.LastScanDateUtc = nowUtc;

                // On ne met à jour la version que si non vide
                if (!string.IsNullOrWhiteSpace(gpVersion))
                    m.GlobalProtectVersion = gpVersion;

                // Le statut peut être mis à jour même s’il indique une erreur
                if (!string.IsNullOrWhiteSpace(gpStatus))
                    m.GlobalProtectStatus = gpStatus;
            }

            var newList = dict.Values.ToList();
            await SaveMasterAsync(newList, ct);
            return newList;
        }

        private static int GetIndex(string[] header, string name)
        {
            for (int i = 0; i < header.Length; i++)
            {
                var cleaned = header[i].Trim().Trim('"', '\uFEFF');
                if (string.Equals(cleaned, name, StringComparison.OrdinalIgnoreCase))
                    return i;
            }
            return -1;
        }

        private static string SafeCol(string[] cols, int idx)
        {
            if (idx < 0 || idx >= cols.Length)
                return string.Empty;

            return cols[idx].Trim().Trim('"');
        }
    }
}
