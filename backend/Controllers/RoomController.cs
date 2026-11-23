using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using BattleTanks_Backend.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BattleTanks_Backend.Data;
using BattleTanks_Backend.DTOs;
using BattleTanks_Backend.Models;

namespace BattleTanks_Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class RoomController : ControllerBase
{
    private readonly BattleTanksDbContext _context;
    private readonly RoomMembership _members;
    private string PlayerId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    public RoomController(BattleTanksDbContext context, RoomMembership members)
    {
        _context = context;
        _members = members;
    }

    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(List<RoomResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<RoomResponse>>> GetRooms()
    {
        /* var stale = await _context.GameSessions
            .Where(s => s.CurrentPlayers <= 0 || s.Status == SessionStatus.Finished)
            .ToListAsync();
        if (stale.Any())
        {
            _context.GameSessions.RemoveRange(stale);
            await _context.SaveChangesAsync();
        }

        */
        var rooms = await _context.GameSessions
            .Where(s => (s.Status == SessionStatus.Waiting || s.Status == SessionStatus.InProgress) && s.CurrentPlayers > 0)
            .Select(s => new RoomResponse(
                s.Id,
                s.RoomName,
                s.CurrentPlayers,
                s.MaxPlayers,
                s.Status.ToString(),
                s.MapName
            ))
            .ToListAsync();

        return Ok(rooms);
    }

    [HttpPost]
    [ProducesResponseType(typeof(RoomResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<RoomResponse>> CreateRoom(CreateRoomRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.RoomName) || request.RoomName.Length > 60) return BadRequest("Choose a room name (1–60 characters)");
        var session = new GameSession
        {
            RoomName = request.RoomName,
            MapName = request.MapName,
            MaxPlayers = 4,
            CurrentPlayers = 1,
            Status = SessionStatus.Waiting,
            CreatedAt = DateTime.UtcNow
        };

        _context.GameSessions.Add(session);
        await _context.SaveChangesAsync();

        _members.Add(session.Id, PlayerId, true);
        var response = new RoomResponse(session.Id, session.RoomName, session.CurrentPlayers, session.MaxPlayers, session.Status.ToString(), session.MapName);
        return CreatedAtAction(nameof(GetRooms), response);
    }

    [HttpPut("{id}/join")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult> JoinRoom(int id)
    {
        var session = await _context.GameSessions.FindAsync(id);

        if (session == null)
            return NotFound("Room not found");

        if (session.CurrentPlayers >= session.MaxPlayers)
            return Conflict("Room is full");

        if (session.Status == SessionStatus.Finished)
            return Conflict("Room has already finished");

        await _members.Gate.WaitAsync();
        try
        {
            if (_members.Contains(id, PlayerId)) return Ok(new { message = "Already joined" });
            var joined = await _context.GameSessions.Where(s => s.Id == id && s.CurrentPlayers < s.MaxPlayers && s.Status == SessionStatus.Waiting)
              .ExecuteUpdateAsync(update => update.SetProperty(s => s.CurrentPlayers, s => s.CurrentPlayers + 1));
            if (joined == 0) return Conflict("Room full or already started");
            _members.Add(id, PlayerId);
        }
        finally { _members.Gate.Release(); }

        return Ok(new { message = "Joined successfully", currentPlayers = session.CurrentPlayers });
    }

    [HttpPut("{id}/leave")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult> LeaveRoom(int id)
    {
        var session = await _context.GameSessions.FindAsync(id);

        if (session == null)
            return NotFound("Room not found");

        if (!_members.Remove(id, PlayerId)) return Ok(new { message = "Already left" });
        session.CurrentPlayers = Math.Max(0, session.CurrentPlayers - 1);

        if (session.CurrentPlayers <= 0)
        {
            _context.GameSessions.Remove(session);
            await _context.SaveChangesAsync();
            return Ok(new { message = "Room removed (empty)" });
        }

        await _context.SaveChangesAsync();
        return Ok(new { message = "Left successfully", currentPlayers = session.CurrentPlayers });
    }

    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult> DeleteRoom(int id)
    {
        if (!_members.IsOwner(id, PlayerId)) return Forbid();
        var session = await _context.GameSessions.FindAsync(id);

        if (session == null)
            return NotFound("Room not found");

        _context.GameSessions.Remove(session);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}
