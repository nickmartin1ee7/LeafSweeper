using Godot;

namespace LeafSweeper;

/// <summary>
/// Shared shape of the season churn props (summer tornado, fall water
/// streams): a telegraphed world-space show that announces itself before
/// it touches litter, then covers the floor while Main.ShuffleRound moves
/// the debris, bug and coins. Purely visual — never moves a piece itself.
/// </summary>
public abstract partial class FloorChurn : Node2D
{
    /// <summary>True while the prop is on screen (telegraph or show).</summary>
    public abstract bool Active { get; protected set; }

    /// <summary>True during the telegraph, before the shuffle begins.</summary>
    public abstract bool Telegraphing { get; protected set; }

    /// <summary>Starts the show.</summary>
    public abstract void Begin(Vector2 from, Vector2 to);

    /// <summary>Ends the show at once (win / menu / restart).</summary>
    public abstract void EndShow();
}