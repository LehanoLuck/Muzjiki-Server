namespace Muzjiki_Server;

public sealed class SharedEnergyState
{
    private readonly Dictionary<Guid, int> _energyByPlayer = new();

    public IReadOnlyDictionary<Guid, int> EnergyByPlayer => _energyByPlayer;

    public void InitializePlayers(IEnumerable<Guid> playerIds, int initialEnergy)
    {
        ArgumentNullException.ThrowIfNull(playerIds);

        if (initialEnergy < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(initialEnergy), "Initial energy must be non-negative.");
        }

        _energyByPlayer.Clear();
        foreach (var playerId in playerIds)
        {
            _energyByPlayer[playerId] = initialEnergy;
        }
    }

    public int GetEnergy(Guid playerId)
    {
        return _energyByPlayer.TryGetValue(playerId, out var energy) ? energy : 0;
    }

    public bool TrySpendAndTransfer(Guid spenderId, Guid receiverId, int amount)
    {
        EnsureKnownPlayer(spenderId);
        EnsureKnownPlayer(receiverId);

        if (amount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amount), "Transfer amount must be greater than zero.");
        }

        if (_energyByPlayer[spenderId] < amount)
        {
            return false;
        }

        _energyByPlayer[spenderId] -= amount;
        _energyByPlayer[receiverId] += amount;
        return true;
    }

    public void SetEnergy(Guid playerId, int amount)
    {
        EnsureKnownPlayer(playerId);

        if (amount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amount), "Energy must be non-negative.");
        }

        _energyByPlayer[playerId] = amount;
    }

    private void EnsureKnownPlayer(Guid playerId)
    {
        if (!_energyByPlayer.ContainsKey(playerId))
        {
            throw new KeyNotFoundException($"Player '{playerId}' is not registered in shared energy state.");
        }
    }
}
