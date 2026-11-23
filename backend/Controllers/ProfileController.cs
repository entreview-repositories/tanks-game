using System.Security.Claims;
using BattleTanks_Backend.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
namespace BattleTanks_Backend.Controllers;

[ApiController, Authorize, Route("api/profile")]
public class ProfileController(BattleTanksDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var id = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var profile = await db.Players.AsNoTracking().Where(p => p.Id == id).Select(p => new { p.Username, p.Email, p.TotalScore, p.Victories, p.GamesPlayed }).SingleAsync();
        return Ok(profile);
    }
}
