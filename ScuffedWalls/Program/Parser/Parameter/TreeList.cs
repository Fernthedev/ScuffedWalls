using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace ScuffedWalls;

/// <summary>
///     An IEnumerable which supports nested item indexing. As a good practice, only ever make instances of this readonly
///     or gettable.
/// </summary>
/// <typeparam name="T"></typeparam>
public class TreeList<T> : IEnumerable<T>
{
    private readonly List<T> _list;
    private readonly List<TreeList<T>> _lookupGroups;

    public TreeList(Func<T, string> Exposer)
    {
        _list = new List<T>();
        _lookupGroups = new List<TreeList<T>>();
        this.Exposer = Exposer;
    }

    public TreeList(IEnumerable<T> collection, Func<T, string> Exposer)
    {
        _list = collection.ToList();
        _lookupGroups = new List<TreeList<T>>();
        this.Exposer = Exposer;
    }

    //  public List<T> Values => _totalValues;
    public Func<T, string> Exposer { get; }

    public List<T> Values
    {
        get
        {
            var list = new List<T>(_list);
            for (var i = 0; i < _lookupGroups.Count; ++i) list.AddRange(_lookupGroups[i]);
            return list;
        }
    }

    public int Count => Values.Count;

    public IEnumerator<T> GetEnumerator()
    {
        return Values.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public void Clear()
    {
        _list.Clear();
        foreach (var tree in _lookupGroups) tree.Clear();
    }

    public void Add(T value)
    {
        _list.Add(value);
    }

    public void AddRange(IEnumerable<T> values)
    {
        _list.AddRange(values);
    }

    public void Register(TreeList<T> lookup)
    {
        _lookupGroups.Add(lookup);
    }

    public bool Remove(T item)
    {
        if (_list.Remove(item)) return true;
        foreach (var lookup in _lookupGroups)
            if (lookup.Remove(item))
                return true;
        return false;
    }

    public T Get(string key)
    {
        var item = _list.FirstOrDefault(item => Exposer(item).ToLower() == key.ToLower());
        if (item != null) return item;

        if (_lookupGroups != null)
            foreach (var lookup in _lookupGroups)
            {
                item = lookup.Get(key);
                if (item != null)
                    return item;
            }

        return default;
    }

    public T Get(string key, T DefaultValue)
    {
        return Get(key) ?? DefaultValue;
    }

    public H Get<H>(string key, H DefaultValue, Func<T, H> converter)
    {
        var item = Get(key);
        if (item == null) return DefaultValue;
        return converter(item);
    }
}