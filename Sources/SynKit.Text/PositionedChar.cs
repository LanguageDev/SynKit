namespace SynKit.Text;

/// <summary>
/// Represents a positioned character.
/// </summary>
/// <param name="Position">The position of the character.</param>
/// <param name="Char">The character.</param>
public readonly record struct PositionedChar(Position Position, char Char);
