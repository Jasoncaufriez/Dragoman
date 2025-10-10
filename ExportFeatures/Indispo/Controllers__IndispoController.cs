using AutoMapper;
using AutoMapper.QueryableExtensions;
using Dragoman.Server.Dtos;
using Dragoman.Server.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[ApiController]
[Route("api/interpretes/{tolkcode:int}/indispo")]
[Produces("application/json")]
public class IndispoController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly IMapper _mapper;

    public IndispoController(ApplicationDbContext db, IMapper mapper)
    {
        _db = db;
        _mapper = mapper;
    }

    // GET /api/interpretes/{tolkcode}/indispo
    [HttpGet]
    public async Task<ActionResult<IEnumerable<IndispoDto>>> Get(int tolkcode)
    {
        var tk = tolkcode.ToString(); // en base, TOLKCODE est VARCHAR2(5)
        var rows = await _db.Tolkindispos
            .AsNoTracking()
            .Where(x => x.Tolkcode == tk)
            .OrderBy(x => x.Startindispo)
            .ProjectTo<IndispoDto>(_mapper.ConfigurationProvider)
            .ToListAsync();

        return Ok(rows);
    }

    // POST /api/interpretes/{tolkcode}/indispo
    // - si Endindispo du nouveau est vide: on clôture l'ancienne (End = Start(new) - 1 jour)
    // - anti-chevauchement: rejet 409 si recouvrement sur la période
    [HttpPost]
    public async Task<IActionResult> Add(int tolkcode, [FromBody] NewIndispoDto dto)
    {
        var tk = tolkcode.ToString();
        var start = dto.Startindispo.Date;
        var end = (dto.Endindispo?.Date) ?? DateTime.MaxValue.Date;

        if (dto.Endindispo.HasValue && end < start)
            return BadRequest("Endindispo doit être >= Startindispo.");

        // 1) Clôturer l'ancienne période ouverte (Endindispo NULL), si elle existe
        var open = await _db.Tolkindispos
            .Where(x => x.Tolkcode == tk && x.Endindispo == null)
            .OrderByDescending(x => x.Startindispo)
            .FirstOrDefaultAsync();

        if (open != null)
        {
            var prevEnd = start.AddDays(-1);
            open.Endindispo = (prevEnd < open.Startindispo.Date) ? open.Startindispo.Date : prevEnd;
        }

        // 2) Anti-chevauchement (version corrigée pour Oracle)
        var existingPeriods = await _db.Tolkindispos
            .AsNoTracking()
            .Where(x => x.Tolkcode == tk && (open == null || x.IdIndispo != open.IdIndispo))
            .Select(x => new { x.Startindispo, x.Endindispo })
            .ToListAsync();

        // Vérification du chevauchement en mémoire
        bool overlap = existingPeriods.Any(existing =>
        {
            var existingStart = existing.Startindispo.Date;
            var existingEnd = existing.Endindispo?.Date ?? DateTime.MaxValue.Date;

            // Chevauchement si : start < existingEnd ET end > existingStart
            return start < existingEnd && end > existingStart;
        });

        if (overlap)
            return Conflict("Chevauchement détecté sur la période.");

        // 3) Insert
        var entity = _mapper.Map<Tolkindispo>(dto);
        entity.Tolkcode = tk;
        entity.Datecreate = DateTime.Now;
        entity.Usercreate = "api";

        _db.Tolkindispos.Add(entity);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    // DELETE /api/interpretes/{tolkcode}/indispo/{id}
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int tolkcode, int id)
    {
        var tk = tolkcode.ToString();
        var row = await _db.Tolkindispos.FirstOrDefaultAsync(x => x.IdIndispo == id && x.Tolkcode == tk);
        if (row == null) return NotFound();

        _db.Tolkindispos.Remove(row);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}