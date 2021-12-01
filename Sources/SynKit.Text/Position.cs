namespace SynKit.Text;

/// <summary>
/// Represents a position in text.
/// </summary>
/// <param name="Line">The 0-based line index.</param>
/// <param name="Column">The 0-based column index.</param>
public readonly record struct Position(int Line, int Column) : IComparable, IComparable<Position>
{
    /// <inheritdoc/>
    public int CompareTo(object? obj) => obj is Position pos
        ? this.CompareTo(pos)
        : throw new ArgumentException("Argument must be a Position", nameof(obj));

    /// <inheritdoc/>
    public int CompareTo(Position other)
    {
        var l = this.Line.CompareTo(other.Line);
        return l == 0 ? this.Column.CompareTo(other.Column) : l;
    }

    /// <summary>
    /// Less-than compares two <see cref="Position"/>s.
    /// </summary>
    /// <param name="left">The first <see cref="Position"/> to compare.</param>
    /// <param name="right">The second <see cref="Position"/> to compare.</param>
    /// <returns>True, if <paramref name="left"/> comes before <paramref name="right"/> in a text.</returns>
    public static bool operator <(Position left, Position right) => left.CompareTo(right) < 0;

    /// <summary>
    /// Less-than or equals compares two <see cref="Position"/>s.
    /// </summary>
    /// <param name="left">The first <see cref="Position"/> to compare.</param>
    /// <param name="right">The second <see cref="Position"/> to compare.</param>
    /// <returns>True, if <paramref name="left"/> comes before <paramref name="right"/> in a text, or they happen
    /// to be the exact same <see cref="Position"/>.</returns>
    public static bool operator <=(Position left, Position right) => left.CompareTo(right) <= 0;

    /// <summary>
    /// Greater-than compares two <see cref="Position"/>s.
    /// </summary>
    /// <param name="left">The first <see cref="Position"/> to compare.</param>
    /// <param name="right">The second <see cref="Position"/> to compare.</param>
    /// <returns>True, if <paramref name="left"/> comes after <paramref name="right"/> in a text.</returns>
    public static bool operator >(Position left, Position right) => left.CompareTo(right) > 0;

    /// <summary>
    /// Greater-than or equals compares two <see cref="Position"/>s.
    /// </summary>
    /// <param name="left">The first <see cref="Position"/> to compare.</param>
    /// <param name="right">The second <see cref="Position"/> to compare.</param>
    /// <returns>True, if <paramref name="left"/> comes after <paramref name="right"/> in a text, or they happen
    /// to be the exact same <see cref="Position"/>.</returns>
    public static bool operator >=(Position left, Position right) => left.CompareTo(right) >= 0;

    /// <summary>
    /// Creates a <see cref="Position"/> that's advanced in the current line by the given amount.
    /// </summary>
    /// <param name="amount">The amount to advance in the current line.</param>
    /// <returns>The <see cref="Position"/> in the same line, advanced by columns.</returns>
    public Position Advance(int amount = 1) => new(Line: this.Line, Column: this.Column + amount);

    /// <summary>
    /// Creates a <see cref="Position"/> that points to the first character of the next line.
    /// </summary>
    /// <returns>A <see cref="Position"/> in the next line's first character.</returns>
    public Position Newline() => new(Line: this.Line + 1, Column: 0);
}
