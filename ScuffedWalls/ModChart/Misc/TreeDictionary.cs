using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using ScuffedWalls;

namespace ModChart;

public class TreeDictionary : Dictionary<string, object?>, ICloneable
{
    [Flags]
    public enum MergeBindingFlags
    {
        Exists,
        HasValue
    }

    [Flags]
    public enum MergeType
    {
        Objects,
        Arrays,
        Dictionaries
    }

    public new object? this[string key]
    {
        get
        {
            if (!key.Contains('.'))
            {
                TryGetValue(key, out var Value);
                return Value;
            }

            var layers = key.Split('.');

            object currentLayer = this;
            foreach (var layer in layers)
                if (currentLayer is IDictionary<string, object> dictionary)
                    dictionary.TryGetValue(layer, out currentLayer);
                else
                    throw new NullReferenceException(
                        $"TreeDictionary does not contain one or more of the SubTrees referenced {{{key}}}");

            return currentLayer;
        }
        set
        {
            if (!key.Contains('.'))
            {
                base[key] = value;
                return;
            }

            var layers = key.Split('.');

            // TODO: Wtf is this
            object currentLayer = this;
            foreach (var layer in layers)
                if (currentLayer is IDictionary<string, object> dictionary)
                    dictionary.TryGetValue(layer, out currentLayer);
                else
                    throw new NullReferenceException(
                        $"TreeDictionary does not contain one or more of the SubTrees referenced {{{key}}}");

            ((IDictionary<string, object?>)currentLayer)[layers.Last()] = value;
        }
    }

    public object Clone()
    {
        var clone = new TreeDictionary();

        foreach (var Item in this)
            clone[Item.Key] = Item.Value switch
            {
                ICloneable cloneable => cloneable.Clone(),
                IEnumerable<object> array => array.CloneArray(),
                _ => Item.Value
            };

        return clone;
    }

    public void DeleteNullValues()
    {
        var nulls = Keys.Where(key => base[key] == null);
        foreach (var key in nulls) Remove(key);

        foreach (var item in this)
            switch (item.Value)
            {
                case TreeDictionary dict:
                    dict.DeleteNullValues();
                    break;
                case IEnumerable<object> array:
                {
                    foreach (var element in array)
                    {
                        if (element is TreeDictionary dict2)
                            dict2.DeleteNullValues();
                    }

                    break;
                }
            }
    }

    public static TreeDictionary Tree()
    {
        return new TreeDictionary();
    }

    public static TreeDictionary Tree(IDictionary<string, object> IDict)
    {
        var tree = new TreeDictionary();
        foreach (var item in IDict) tree.Add(item.Key, item.Value);
        return tree;
    }

    /// <summary>
    ///     Merges two IDictionaries, prioritizes Dictionary1
    /// </summary>
    /// <param name="Dictionary1"></param>
    /// <param name="Dictionary2"></param>
    /// <returns>A TreeDictionary as an IDictionary</returns>
    public static TreeDictionary Merge(IDictionary<string, object?>? Dictionary1,
        IDictionary<string, object?>? Dictionary2,
        MergeType mergeType = MergeType.Dictionaries | MergeType.Objects | MergeType.Arrays,
        MergeBindingFlags mergeBindingFlags = MergeBindingFlags.HasValue)
    {
        Dictionary1 ??= new TreeDictionary();
        Dictionary2 ??= new TreeDictionary();

        var merged = new TreeDictionary();
        foreach (var item in Dictionary1) merged[item.Key] = item.Value;
        foreach (var item in Dictionary2)
            if (!TreeItemExists(item))
            {
                if (mergeType.HasFlag(MergeType.Objects))
                    merged[item.Key] = item.Value;
                else
                    continue;
            }
            else
            {
                if (merged[item.Key] is IDictionary<string, object> dictionary1 &&
                    item.Value is IDictionary<string, object> dictionary2)
                    if (mergeType.HasFlag(MergeType.Dictionaries))
                        merged[item.Key] = Merge(dictionary1, dictionary2, mergeType, mergeBindingFlags);
                    else continue;
                else if (merged[item.Key] is IList<object> List1 && item.Value is IEnumerable<object> Array3)
                    if (mergeType.HasFlag(MergeType.Arrays))
                        foreach (var obj in Array3)
                            List1.Add(obj);
                    else continue;
                else if (merged[item.Key] is IEnumerable<object> Array1 && item.Value is IEnumerable<object> Array2)
                    if (mergeType.HasFlag(MergeType.Arrays)) merged[item.Key] = Array1.CombineWith(Array2);
                    else continue;
            }

        return merged;

        bool TreeItemExists(KeyValuePair<string, object> Item)
        {
            switch (mergeBindingFlags)
            {
                case MergeBindingFlags.Exists:
                    if (Dictionary1.ContainsKey(Item.Key)) return true;
                    return false;
                case MergeBindingFlags.HasValue:
                    if (Dictionary1.TryGetValue(Item.Key, out var Value) && Value != null) return true;
                    return false;
            }

            return false;
        }
    }

    public TreeDictionary? At(string key)
    {
        return (TreeDictionary?)base[key];
    }

    public T? At<T>(string key)
    {
        return (T?)base[key];
    }
}

public class TreeDictionaryJsonConverter : JsonConverter<TreeDictionary>
{
    public override TreeDictionary Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
            throw new JsonException($"JsonTokenType was of type {reader.TokenType}, only objects are supported");

        var dictionary = new TreeDictionary();
        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject) return dictionary;
            if (reader.TokenType != JsonTokenType.PropertyName)
                throw new JsonException("JsonTokenType was not PropertyName");

            var propertyName = reader.GetString();

            if (string.IsNullOrWhiteSpace(propertyName)) throw new JsonException("Failed to get property name");

            reader.Read();

            dictionary.Add(propertyName, ExtractValue(ref reader, options));
        }

        return dictionary;
    }

    public override void Write(Utf8JsonWriter writer, TreeDictionary value, JsonSerializerOptions options)
    {
        JsonSerializer.Serialize(writer, value as IDictionary<string, object>, options);
    }


    private object ExtractValue(ref Utf8JsonReader reader, JsonSerializerOptions options)
    {
        switch (reader.TokenType)
        {
            case JsonTokenType.String:
                if (reader.TryGetDateTime(out var date)) return date;
                return reader.GetString();
            case JsonTokenType.False:
                return false;
            case JsonTokenType.True:
                return true;
            case JsonTokenType.Null:
                return null;
            case JsonTokenType.Number:
                if (reader.TryGetInt64(out var result)) return result;
                return reader.GetDecimal();
            case JsonTokenType.StartObject:
                return Read(ref reader, null, options);
            case JsonTokenType.StartArray:
                IList<object> list = new List<object>();
                while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
                    list.Add(ExtractValue(ref reader, options));
                return list;
            default:
                throw new JsonException($"'{reader.TokenType}' is not supported");
        }
    }
}