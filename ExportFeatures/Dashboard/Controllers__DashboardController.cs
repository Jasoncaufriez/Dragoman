using Dragoman.Server.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[ApiController]
[Route("api/[controller]")]
public class DashboardController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    public DashboardController(ApplicationDbContext db) => _db = db;

    // Helpers pour factoriser l’accès aux 2 vues
    private static IQueryable<VueCalendarVrmPc> AllVrm(ApplicationDbContext db) =>
        db.VueCalendarVrmPc.AsNoTracking();

    private static IQueryable<VueCalendarAnn> AllAnn(ApplicationDbContext db) =>
        db.VueCalendarAnn.AsNoTracking();

    [HttpGet("audiences/today")]
    public async Task<IActionResult> GetAudiencesToday(CancellationToken ct)
    {
        var from = DateTime.Today; var to = from.AddDays(1);

        var vrm = AllVrm(_db).Where(a => a.DateAudience >= from && a.DateAudience < to)
            .Select(a => new {  a.DateAudience, a.HeureAudience, a.SalleAudience });

        var ann = AllAnn(_db).Where(a => a.DateAudience >= from && a.DateAudience < to)
            .Select(a => new { a.DateAudience, a.HeureAudience, a.SalleAudience });

        var rows = await vrm.Union(ann)
            .Distinct()
            .OrderBy(x => x.HeureAudience)
            .ThenBy(x => x.SalleAudience)
            .ToListAsync(ct);

        return Ok(rows);
    }

    [HttpGet("audiences/count-today")]
    public async Task<IActionResult> GetAudienceCountToday(CancellationToken ct)
    {
        var from = DateTime.Today; var to = from.AddDays(1);

        var vrm = AllVrm(_db).Where(a => a.DateAudience >= from && a.DateAudience < to)
            .Select(a => new {  a.DateAudience, a.HeureAudience, a.SalleAudience });

        var ann = AllAnn(_db).Where(a => a.DateAudience >= from && a.DateAudience < to)
            .Select(a => new { a.DateAudience, a.HeureAudience, a.SalleAudience });

        var count = await vrm.Union(ann).Distinct().CountAsync(ct);
        return Ok(new { nbAudiences = count });
    }

    [HttpGet("interpretes/count-today")]
    public async Task<IActionResult> GetInterpretesCountToday(CancellationToken ct)
    {
        var from = DateTime.Today; var to = from.AddDays(1);

        var vrm = AllVrm(_db).Where(a => a.DateAudience >= from && a.DateAudience < to)
            .Select(a => a.Tolkcode);

        var ann = AllAnn(_db).Where(a => a.DateAudience >= from && a.DateAudience < to)
            .Select(a => a.Tolkcode);

        var count = await vrm.Concat(ann)
            .Where(code => code != null)
            .Distinct()
            .CountAsync(ct);

        return Ok(new { nbInterpretes = count });
    }

    [HttpGet("langues/today")]
    public async Task<IActionResult> GetLanguesToday(CancellationToken ct)
    {
        var from = DateTime.Today; var to = from.AddDays(1);

        var allLangs =
            AllVrm(_db).Where(a => a.DateAudience >= from && a.DateAudience < to).Select(a => a.LangueRequete)
            .Concat(
            AllAnn(_db).Where(a => a.DateAudience >= from && a.DateAudience < to).Select(a => a.LangueRequete));

        var data = await allLangs
            .Where(l => l != null && l != "*Aucun interprète demandé")
            .GroupBy(l => l)
            .Select(g => new { langue = g.Key!, nb = g.Count() })
            .OrderByDescending(x => x.nb)
            .ToListAsync(ct);

        return Ok(data);
    }

    [HttpGet("audiences-supprimees/today")]
    public async Task<IActionResult> GetAudiencesSupprimeesToday(CancellationToken ct)
    {
        var from = DateTime.Today; var to = from.AddDays(1);

        var vrm = AllVrm(_db).Where(a => a.DateAudience >= from && a.DateAudience < to)
            .Select(a => new { a.DateAudience, a.HeureAudience, a.SalleAudience, a.NroRoleGen, a.LangueRequete });

        var ann = AllAnn(_db).Where(a => a.DateAudience >= from && a.DateAudience < to)
            .Select(a => new { a.DateAudience, a.HeureAudience, a.SalleAudience, a.NroRoleGen, a.LangueRequete });

        var list = await vrm.Concat(ann).ToListAsync(ct);
        return Ok(list);
    }
}
