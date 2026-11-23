using System.Collections.Concurrent;
namespace BattleTanks_Backend.Services;
// Membership is scoped to this local server. PostgreSQL retains room metadata.
public class RoomMembership
{
    public SemaphoreSlim Gate { get; } = new(1, 1);
    private readonly ConcurrentDictionary<int, ConcurrentDictionary<string, bool>> rooms = new();
    public void Add(int room, string player, bool owner = false) => rooms.GetOrAdd(room, _ => new())[player] = owner;
    public bool Contains(int room, string player) => rooms.TryGetValue(room, out var members) && members.ContainsKey(player);
    public bool IsOwner(int room, string player) => rooms.TryGetValue(room, out var members) && members.TryGetValue(player, out var owner) && owner;
    public bool Remove(int room, string player) => rooms.TryGetValue(room, out var members) && members.TryRemove(player, out _);
}
