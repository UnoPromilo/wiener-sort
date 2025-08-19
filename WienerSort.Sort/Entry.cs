using System.Runtime.CompilerServices;
using System.Text;

namespace WienerSort.Sort;

public unsafe struct Entry()
{
    public uint Number = 0;
    public fixed byte Phrase[256];
    public int PhraseLength = 0;

    public override string ToString()
    {
        fixed (byte* p = Phrase)
        {
            return $"{Number}. {Encoding.ASCII.GetString(p, PhraseLength)}";
        }
    }

    public static Entry FromSpan(ReadOnlySpan<byte> line)
    {
        Entry r = default;

        var i = 0;
        uint num = 0;
        while (i < line.Length && line[i] != (byte)'.')
        {
            num = num * 10 + (uint)(line[i] - (byte)'0');
            i++;
        }

        r.Number = num;

        // skip ". "
        i += 2;

        if (line.Length - i > 256)
        {
            throw new("Too many characters in line");
        }

        var len = Math.Min(256, line.Length - i);
        r.PhraseLength = len;

        line.Slice(i, len).CopyTo(new(r.Phrase, 256));

        return r;
    }

    public ReadOnlySpan<byte> ToSpan()
    {
        Span<byte> buffer = new byte[sizeof(Entry)];
        var len = ToBytes(buffer);
        return buffer[..len];
    }

    public int ToBytes(Span<byte> buffer)
    {
        var pos = buffer.Length;
        var val = Number;
        do
        {
            buffer[--pos] = (byte)('0' + val % 10);
            val /= 10;
        } while (val != 0);

        var numLen = buffer.Length - pos;
        buffer[..numLen].CopyTo(buffer[..numLen]); // ensure proper position
        var dst = 0;
        for (var i = 0; i < numLen; i++)
            buffer[dst++] = buffer[pos + i];

        buffer[dst++] = (byte)'.';
        buffer[dst++] = (byte)' ';

        fixed (byte* p = Phrase)
        {
            new ReadOnlySpan<byte>(p, PhraseLength).CopyTo(buffer[dst..]);
        }

        dst += PhraseLength;

        buffer[dst++] = (byte)'\n';

        return dst;
    }
}

public sealed class EntryComparer : IComparer<Entry>
{
    public int Compare(Entry x, Entry y)
    {
        var cmp = ComparePhrases(x, y);
        return cmp != 0 ? cmp : x.Number.CompareTo(y.Number);
    }

    // TODO is there any build in method?
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static unsafe int ComparePhrases(Entry x, Entry y)
    {
        var len = Math.Min(x.PhraseLength, y.PhraseLength);
        for (var i = 0; i < len; i++)
        {
            var c = ToLowerAscii(x.Phrase[i]) - ToLowerAscii(y.Phrase[i]);
            if (c != 0) return c;
        }

        return x.PhraseLength - y.PhraseLength;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static byte ToLowerAscii(byte b)
    {
        return b is >= (byte)'A' and <= (byte)'Z'
            ? (byte)(b | 0x20)
            : b;
    }
}