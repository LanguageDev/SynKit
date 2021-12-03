namespace SynKit.Text;

/// <summary>
/// A default implementation for <see cref="IToken{TKind}"/>.
/// </summary>
/// <typeparam name="TKind">The kind type this <see cref="Token{TKind}"/> uses. Usually an enumeration type.</typeparam>
/// <param name="Range">The <see cref="Text.Range"/> of the <see cref="Token{TKind}"/> in the source.</param>
/// <param name="Text">The text the <see cref="Token{TKind}"/> was parsed from.</param>
/// <param name="Kind">The <typeparamref name="TKind"/> of the <see cref="Token{TKind}"/>.</param>
public sealed record Token<TKind>(Range Range, string Text, TKind Kind) : IToken<TKind>;
