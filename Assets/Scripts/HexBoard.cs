using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

public readonly struct HexCoordinate : IEquatable<HexCoordinate>
{
    public int Q { get; }
    public int R { get; }

    public HexCoordinate(int q, int r)
    {
        Q = q;
        R = r;
    }

    public int DistanceTo(HexCoordinate other)
    {
        int deltaQ = Q - other.Q;
        int deltaR = R - other.R;
        return (Math.Abs(deltaQ) + Math.Abs(deltaR) + Math.Abs(deltaQ + deltaR)) / 2;
    }

    public bool Equals(HexCoordinate other)
    {
        return Q == other.Q && R == other.R;
    }

    public override bool Equals(object obj)
    {
        return obj is HexCoordinate other && Equals(other);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            return (Q * 397) ^ R;
        }
    }

    public override string ToString()
    {
        return $"({Q}, {R})";
    }

    public static bool operator ==(HexCoordinate left, HexCoordinate right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(HexCoordinate left, HexCoordinate right)
    {
        return !left.Equals(right);
    }
}

public enum HexTerrain
{
    Water,
    Rock
}

public enum HexCommandType
{
    MoveForward,
    MoveBackward,
    TurnLeft,
    TurnRight,
    Wait,
    Fire,
    FullSails
}

public sealed class HexBoard
{
    private static readonly HexCoordinate[] DirectionOffsets =
    {
        new HexCoordinate(1, 0),
        new HexCoordinate(1, -1),
        new HexCoordinate(0, -1),
        new HexCoordinate(-1, 0),
        new HexCoordinate(-1, 1),
        new HexCoordinate(0, 1)
    };

    private readonly ReadOnlyDictionary<HexCoordinate, HexTerrain> _cells;

    public IReadOnlyDictionary<HexCoordinate, HexTerrain> Cells => _cells;

    public HexBoard(IDictionary<HexCoordinate, HexTerrain> cells)
    {
        if (cells == null)
        {
            throw new ArgumentNullException(nameof(cells));
        }
        if (cells.Count == 0)
        {
            throw new ArgumentException("A hex board must contain at least one cell.", nameof(cells));
        }

        _cells = new ReadOnlyDictionary<HexCoordinate, HexTerrain>(
            new Dictionary<HexCoordinate, HexTerrain>(cells));
    }

    public bool Contains(HexCoordinate coordinate)
    {
        return _cells.ContainsKey(coordinate);
    }

    public bool IsTraversable(HexCoordinate coordinate)
    {
        return _cells.TryGetValue(coordinate, out HexTerrain terrain) &&
            terrain == HexTerrain.Water;
    }

    public static HexCoordinate GetNeighbor(HexCoordinate coordinate, int direction)
    {
        if (direction < 0 || direction >= DirectionOffsets.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(direction));
        }

        HexCoordinate offset = DirectionOffsets[direction];
        return new HexCoordinate(coordinate.Q + offset.Q, coordinate.R + offset.R);
    }
}

public sealed class HexShipState
{
    public LivingMixin Unit { get; }
    public HexCoordinate Position { get; private set; }
    public int Facing { get; private set; }

    public HexShipState(LivingMixin unit, HexCoordinate position, int facing)
    {
        Unit = unit != null ? unit : throw new ArgumentNullException(nameof(unit));
        if (facing < 0 || facing >= 6)
        {
            throw new ArgumentOutOfRangeException(nameof(facing));
        }

        Position = position;
        Facing = facing;
    }

    internal void MoveTo(HexCoordinate position)
    {
        Position = position;
    }

    internal void TurnTo(int facing)
    {
        Facing = (facing + 6) % 6;
    }
}

public sealed class HexEncounterState
{
    public const int DefaultFireRange = 4;

    public HexBoard Board { get; }
    public HexShipState Player { get; }
    public HexShipState Enemy { get; }

    public HexEncounterState(
        HexBoard board,
        LivingMixin player,
        LivingMixin enemy)
    {
        Board = board ?? throw new ArgumentNullException(nameof(board));
        if (player == null)
        {
            throw new ArgumentNullException(nameof(player));
        }
        if (enemy == null)
        {
            throw new ArgumentNullException(nameof(enemy));
        }
        if (ReferenceEquals(player, enemy))
        {
            throw new ArgumentException("Player and enemy must be different units.");
        }

        HexCoordinate playerStart = new HexCoordinate(-2, 0);
        HexCoordinate enemyStart = new HexCoordinate(2, 0);
        if (!Board.IsTraversable(playerStart) || !Board.IsTraversable(enemyStart))
        {
            throw new ArgumentException("The generated board must include both ship start positions.");
        }

        Player = new HexShipState(player, playerStart, 0);
        Enemy = new HexShipState(enemy, enemyStart, 3);
    }

    public bool TryMove(
        HexShipState ship,
        bool backward,
        out HexCoordinate from,
        out HexCoordinate to,
        out string result)
    {
        RequireShip(ship);
        from = ship.Position;
        int direction = backward ? (ship.Facing + 3) % 6 : ship.Facing;
        to = HexBoard.GetNeighbor(from, direction);

        if (!Board.Contains(to))
        {
            result = "Edge of board";
            return false;
        }
        if (!Board.IsTraversable(to))
        {
            result = "Blocked by rock";
            return false;
        }

        HexShipState otherShip = ReferenceEquals(ship, Player) ? Enemy : Player;
        if (to == otherShip.Position)
        {
            result = "Hex occupied";
            return false;
        }

        ship.MoveTo(to);
        result = "Moved";
        return true;
    }

    public void Turn(HexShipState ship, bool left, out int from, out int to)
    {
        RequireShip(ship);
        from = ship.Facing;
        to = (from + (left ? 5 : 1)) % 6;
        ship.TurnTo(to);
    }

    public bool TryGetTargetInFront(
        HexShipState attacker,
        HexShipState target,
        int range,
        out HexCoordinate targetPosition)
    {
        RequireShip(attacker);
        RequireShip(target);
        if (ReferenceEquals(attacker, target))
        {
            throw new ArgumentException("A ship cannot target itself.", nameof(target));
        }

        return TryGetTargetInFront(attacker.Position, attacker.Facing, target.Position, range, out targetPosition);
    }

    public bool TryGetTargetInFront(
        HexCoordinate attackerPosition,
        int facing,
        HexCoordinate targetPosition,
        int range,
        out HexCoordinate foundPosition)
    {
        if (facing < 0 || facing >= 6)
        {
            throw new ArgumentOutOfRangeException(nameof(facing));
        }
        if (range < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(range));
        }

        HexCoordinate current = attackerPosition;
        for (int distance = 1; distance <= range; distance++)
        {
            current = HexBoard.GetNeighbor(current, facing);
            if (!Board.Contains(current) || !Board.IsTraversable(current))
            {
                break;
            }
            if (current == targetPosition)
            {
                foundPosition = current;
                return true;
            }
        }

        foundPosition = default(HexCoordinate);
        return false;
    }

    private void RequireShip(HexShipState ship)
    {
        if (!ReferenceEquals(ship, Player) && !ReferenceEquals(ship, Enemy))
        {
            throw new ArgumentException("The ship does not belong to this encounter.", nameof(ship));
        }
    }
}

public static class HexBoardGenerator
{
    public static HexBoard Generate(int seed, int radius = 3)
    {
        if (radius < 2)
        {
            throw new ArgumentOutOfRangeException(nameof(radius), "The board radius must be at least 2.");
        }

        System.Random random = new System.Random(seed);
        Dictionary<HexCoordinate, HexTerrain> cells =
            new Dictionary<HexCoordinate, HexTerrain>();

        for (int q = -radius; q <= radius; q++)
        {
            for (int r = -radius; r <= radius; r++)
            {
                if (Math.Abs(q + r) > radius)
                {
                    continue;
                }

                HexCoordinate coordinate = new HexCoordinate(q, r);
                bool clearStartingLane = r == 0 && q >= -2 && q <= 2;
                HexTerrain terrain = !clearStartingLane && random.NextDouble() < 0.12d
                    ? HexTerrain.Rock
                    : HexTerrain.Water;
                cells.Add(coordinate, terrain);
            }
        }

        return new HexBoard(cells);
    }
}
