namespace WienerSort.Sort;

public class SubStream : Stream
{
    private readonly Stream _baseStream;
    private readonly long _length;
    private long _position;
    private bool _disposed;

    public SubStream(Stream baseStream, long offset, long length)
    {
        _baseStream = baseStream;
        _length = length;

        if (baseStream.CanSeek)
        {
            baseStream.Seek(offset, SeekOrigin.Current);
        }
        else
        {
            throw new NotSupportedException("Stream is not seekable");
        }
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        CheckDisposed();
        var remaining = _length - _position;
        if (remaining <= 0) return 0;
        if (remaining < count) count = (int)remaining;
        var read = _baseStream.Read(buffer, offset, count);
        _position += read;
        return read;
    }

    private void CheckDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
    }

    public override long Length
    {
        get
        {
            CheckDisposed();
            return _length;
        }
    }

    public override bool CanRead
    {
        get
        {
            CheckDisposed();
            return true;
        }
    }

    public override bool CanWrite
    {
        get
        {
            CheckDisposed();
            return false;
        }
    }

    public override bool CanSeek
    {
        get
        {
            CheckDisposed();
            return false;
        }
    }

    public override long Position
    {
        get
        {
            CheckDisposed();
            return _position;
        }
        set => throw new NotSupportedException();
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
        throw new NotSupportedException();
    }

    public override void SetLength(long value)
    {
        throw new NotSupportedException();
    }

    public override void Flush()
    {
        CheckDisposed();
        _baseStream.Flush();
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (!disposing) return;

        _baseStream.Dispose();
        _disposed = true;
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        throw new NotSupportedException();
    }
}