namespace Muzjiki_Server.GameLoop.Models;

public sealed class PlayerInfo
{
    public int MaxHealth { get; private set; }
    public int Health { get; private set; }
    public int Stamina { get; private set; }
    public int Strength { get; private set; }
    public int Speed { get; private set; }
    public int Energy { get; private set; }

    public PlayerInfo(int maxHealth, int health, int stamina, int strength, int speed, int energy)
    {
        MaxHealth = ValidateNonNegative(maxHealth, nameof(maxHealth));
        Health = ValidateInRange(health, 0, MaxHealth, nameof(health));
        Stamina = ValidateInRange(stamina, 0, 100, nameof(stamina));
        Strength = ValidateInRange(strength, 0, 100, nameof(strength));
        Speed = ValidateInRange(speed, 0, 100, nameof(speed));
        Energy = ValidateInRange(energy, 0, 100, nameof(energy));
    }

    public void SetHealth(int health)
    {
        Health = ValidateInRange(health, 0, MaxHealth, nameof(health));
    }

    public void SetMaxHealth(int maxHealth)
    {
        MaxHealth = ValidateNonNegative(maxHealth, nameof(maxHealth));
        if (Health > MaxHealth)
        {
            Health = MaxHealth;
        }
    }

    private static int ValidateNonNegative(int value, string paramName)
    {
        if (value < 0)
        {
            throw new ArgumentOutOfRangeException(paramName, "Value must be non-negative.");
        }

        return value;
    }

    private static int ValidateInRange(int value, int min, int max, string paramName)
    {
        if (value < min || value > max)
        {
            throw new ArgumentOutOfRangeException(paramName, $"Value must be in range [{min}; {max}].");
        }

        return value;
    }
}
