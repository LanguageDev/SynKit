using SynKit.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace SynKit.Text;

/// <summary>
/// Utility class for handwritten lexers.
/// </summary>
public sealed class Scanner
{
    private const int readBufferSize = 8;

    /// <summary>
    /// The current position the scanner is at.
    /// </summary>
    public Position Position => this.cursor.Position;

    private readonly TextReader reader;
    private readonly char[] readBuffer = new char[readBufferSize];
    private readonly RingBuffer<char> peekBuffer = new();
    private Cursor cursor;

    /// <summary>
    /// Initializes a new <see cref="Scanner"/>.
    /// </summary>
    /// <param name="reader">The reader to read characters from.</param>
    public Scanner(TextReader reader)
    {
        this.reader = reader;
    }

    /// <summary>
    /// Peeks ahead a number of characters in the reader.
    /// </summary>
    /// <param name="n">The number of characters to peek.</param>
    /// <param name="ch">The peeked character.</param>
    /// <returns>True, if there was a character to peek. False, if the end of input has been reached before being
    /// able to peek the character.</returns>
    public bool TryPeek(int n, out char ch)
    {
        while (this.peekBuffer.Count <= n)
        {
            // Read in a chunk
            var readCount = this.reader.Read(this.readBuffer);
            for (var i = 0; i < readCount; ++i) this.peekBuffer.AddBack(this.readBuffer[i]);
            // If we couldn't read a whole chunk, we are done
            if (readCount < readBufferSize) break;
        }
        // We either read enough, or EOF has been reached
        if (this.peekBuffer.Count <= n)
        {
            ch = default;
            return false;
        }
        else
        {
            ch = this.peekBuffer[n];
            return true;
        }
    }

    /// <summary>
    /// Peeks ahead a number of characters in the reader.
    /// </summary>
    /// <param name="ch">The peeked character.</param>
    /// <returns>True, if there was a character to peek. False, if the end of input has been reached before being
    /// able to peek the character.</returns>
    public bool TryPeek(out char ch) => this.TryPeek(0, out ch);

    /// <summary>
    /// Consumes a given amount of characters from the input.
    /// </summary>
    /// <param name="n">The amount of characters to consume.</param>
    /// <returns>The amount of actually consumed characters.</returns>
    public int Consume(int n)
    {
        if (n == 0) return 0;
        this.TryPeek(n - 1, out _);
        n = Math.Min(n, this.peekBuffer.Count);
        var i = 0;
        for (; i < n; ++i) this.cursor.Push(this.peekBuffer.RemoveFront());
        return n;
    }

    /// <summary>
    /// Consumes a given amount of characters from the input and passes the data to a factory function.
    /// </summary>
    /// <typeparam name="TResult">The type the factory function returns.</typeparam>
    /// <param name="length">The length to consume.</param>
    /// <param name="factory">The factory function.</param>
    /// <returns>The value constructed by the factory function, or default, if there were not enough characters.</returns>
    public TResult? Consume<TResult>(int length, Func<Range, string, TResult> factory)
    {
        if (length == 0) return factory(new(this.Position, 0), string.Empty);
        if (!this.TryPeek(length - 1, out _)) return default;
        var start = this.Position;
        var sb = new StringBuilder();
        for (var i = 0; i < length; ++i) sb.Append(this.peekBuffer.RemoveFront());
        return factory(new(start, this.Position), sb.ToString());
    }
}
