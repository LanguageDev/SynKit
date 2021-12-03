namespace SynKit.Text;

/// <summary>
/// Interface for tokens.
/// </summary>
public interface IToken
{
    /// <summary>
    /// The <see cref="Text.Range"/> that the token can be found at in the source.
    /// </summary>
    public Range Range { get; }

    /// <summary>
    /// The text this token was parsed from.
    /// </summary>
    public string Text { get; }
}
