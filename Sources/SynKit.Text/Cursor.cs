namespace SynKit.Text;

/// <summary>
/// A simple helper to trach positioning in text.
/// </summary>
public record struct Cursor
{
    /// <summary>
    /// The current position of the cursor.
    /// </summary>
    public Position Position { get; private set; }

    private char lastChar = '\0';

    /// <summary>
    /// Feeds in a character to the cursor, stepping it appropriately.
    /// </summary>
    /// <param name="ch">The character to feed in.</param>
    public void Push(char ch)
    {
        if (this.lastChar == '\r' && ch == '\n')
        {
            // Windows-style newline, we already stepped on \r
        }
        else if (ch == '\r' || ch == '\n')
        {
            // OS-X 9 or Unix-style newline
            this.Position = this.Position.Newline();
        }
        else if (char.IsWhiteSpace(ch) || !char.IsControl(ch))
        {
            // If it's a non-control character, move ahead
            this.Position = this.Position.Advance();
        }
        this.lastChar = ch;
    }
}
