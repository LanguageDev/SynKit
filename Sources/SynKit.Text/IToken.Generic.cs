namespace SynKit.Text;

/// <summary>
/// Interface for tokens with some kind categorization.
/// </summary>
/// <typeparam name="TKind">The kind type this token uses. Usually an enum type.</typeparam>
public interface IToken<TKind> : IToken
{
    /// <summary>
    /// The kind tag of this <see cref="IToken{TKind}"/>.
    /// </summary>
    public TKind Kind { get; }
}
