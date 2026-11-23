using Microsoft.AspNetCore.Mvc;
using StackExchange.Redis;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using BattleTanks_Backend.Data;

namespace BattleTanks_Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RankingController : ControllerBase
{
    private readonly BattleTanksDbContext _context;
    private readonly IDatabase _redis;

    public RankingController(BattleTanksDbContext context, IConnectionMultiplexer redis)
    {
        _context = context;
        _redis = redis.GetDatabase();
    }

    [HttpGet]
    public async Task<IActionResult> GetRanking([FromQuery] int page = 1, [FromQuery] int pageSize = 10, [FromQuery] string sortBy = "victories")
    {
        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 100) pageSize = 10;

        var cacheKey = $"ranking:{page}:{pageSize}:{sortBy}";
        var cached = await _redis.StringGetAsync(cacheKey);
        if (cached.HasValue) return Content(cached.ToString(), "application/json");
        var skip = (page - 1) * pageSize;

        var query = _context.Players.AsNoTracking();

        var ordered = sortBy == "score"
            ? query.OrderByDescending(p => p.TotalScore)
            : query.OrderByDescending(p => p.Victories).ThenByDescending(p => p.TotalScore);

        var players = await ordered
            .Skip(skip)
            .Take(pageSize)
            .Select(p => new
            {
                p.Id,
                p.Username,
                p.TotalScore,
                p.Victories,
                p.GamesPlayed
            })
            .ToListAsync();

        var totalPlayers = await _context.Players.AsNoTracking().CountAsync();

        var result = new
        {
            page,
            pageSize,
            totalPlayers,
            totalPages = (int)Math.Ceiling(totalPlayers / (double)pageSize),
            data = players
        };
        await _redis.StringSetAsync(cacheKey, JsonSerializer.Serialize(result, new JsonSerializerOptions(JsonSerializerDefaults.Web)), TimeSpan.FromSeconds(10));
        return Ok(result);
    }

}
