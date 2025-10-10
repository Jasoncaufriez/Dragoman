using AutoMapper;
using AutoMapper.QueryableExtensions; // ✅ nécessaire pour ProjectTo
using Dragoman.Server.Dtos;
using Dragoman.Server.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[ApiController]
[Route("api/interpretes/{tolkcode:int}/tva")]
[Produces("application/json")]
public class TvaController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly IMapper _mapper;

    public TvaController(ApplicationDbContext db, IMapper mapper)
    {
        _db = db;
        _mapper = mapper;
    }

    // GET /api/interpretes/{tolkcode}/tva  -> Liste + libellé du statut
    [HttpGet]
    public async Task<ActionResult<IEnumerable<TvaRowDto>>> Get(int tolkcode)
    {
        // 1) Projette TolkTva -> TvaRowDto (sans le libellé Statut)
        var rows = await _db.TolkTvas
            .AsNoTracking()
            .Where(t => t.Tolkcode == tolkcode)
            .OrderBy(t => t.StartDate)
            .ProjectTo<TvaRowDto>(_mapper.ConfigurationProvider)
            .ToListAsync();

        // 2) Charge les libellés de statuts en une fois (clé: byte)
        var statuts = await _db.Statuts
            .AsNoTracking()
            .ToDictionaryAsync(s => s.IdStatut, s => (s.TypeStatut ?? string.Empty).Trim());

        // 3) Complète la propriété "Statut" pour chaque ligne
        foreach (var r in rows)
            r.Statut = statuts.TryGetValue(r.IdStatut, out var lib) ? lib : string.Empty;

        return Ok(rows);
    }

    // GET /api/tva/statuts  -> pour un <select> côté front
    [HttpGet("/api/tva/statuts")]
    public async Task<ActionResult<IEnumerable<StatutDto>>> GetStatuts()
    {
        var rows = await _db.Statuts
            .AsNoTracking()
            .OrderBy(s => s.IdStatut)
            .ProjectTo<StatutDto>(_mapper.ConfigurationProvider)
            .ToListAsync();

        return Ok(rows);
    }

    // POST /api/interpretes/{tolkcode}/tva
    // Ajoute un nouveau statut + clôture l'ancien (EndDate = Startdate(new) - 1 jour)
    [HttpPost]
    public async Task<IActionResult> Add(int tolkcode, [FromBody] NewTvaDto dto)
    {
        var current = await _db.TolkTvas
            .Where(t => t.Tolkcode == tolkcode && t.EndDate == null)
            .OrderByDescending(t => t.StartDate)
            .FirstOrDefaultAsync();

        if (current != null)
        {
            var previousEnd = dto.Startdate.Date.AddDays(-1);
            if (current.StartDate.HasValue && previousEnd < current.StartDate.Value.Date)
                current.EndDate = current.StartDate.Value.Date;
            else
                current.EndDate = previousEnd;
        }

        var newRow = new TolkTva
        {
            Tolkcode = tolkcode,
            IdStatut = dto.IdStatut,            // byte non-nullable -> OK
            StartDate = dto.Startdate.Date,
            EndDate = null
        };

        _db.TolkTvas.Add(newRow);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
