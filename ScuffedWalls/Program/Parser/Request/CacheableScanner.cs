using System.Collections;
using System.Collections.Generic;

namespace ScuffedWalls;

internal class CacheableScanner<T> : IEnumerator<T>
{
    private readonly IEnumerator<T> _enumerator;
    private readonly IEnumerable<T> _parameters;

    public CacheableScanner(IEnumerable<T> items)
    {
        _parameters = items;
        _enumerator = _parameters.GetEnumerator();
        ResetCache();
    }

    public bool AnyCached => Cache.Count > 0;
    public List<T> Cache { get; private set; }

    public T Current => _enumerator.Current;
    object IEnumerator.Current => _enumerator.Current;

    public bool MoveNext()
    {
        return _enumerator.MoveNext();
    }

    public void Reset()
    {
        _enumerator.Reset();
    }

    public void Dispose()
    {
        _enumerator.Dispose();
    }

    public void ResetCache()
    {
        Cache = new List<T>();
    }

    public void AddToCache()
    {
        Cache.Add(_enumerator.Current);
    }

    public List<T> GetAndResetCache()
    {
        var cache = Cache;
        ResetCache();
        return cache;
    }
}