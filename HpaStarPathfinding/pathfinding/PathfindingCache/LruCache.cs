namespace HpaStarPathfinding.pathfinding.PathfindingCache;

/// <summary>
/// Cache with a fixed capacity. When a new entry is added to a full cache,
/// the least recently used entry gets evicted.
/// </summary>
public class LruCache<TKey, TValue> where TKey : notnull
{
    private class CacheItem(TKey key, TValue value)
    {
        public readonly TKey Key = key;
        public TValue Value = value;
    }

    private readonly int _capacity;
    private readonly Action<TKey, TValue>? _onEvict;
    //First = most recently used, Last = least recently used (next to be evicted)
    private readonly LinkedList<CacheItem> _usageOrder = new();
    private readonly Dictionary<TKey, LinkedListNode<CacheItem>> _items;

    public LruCache(int capacity, Action<TKey, TValue>? onEvict = null)
    {
        _capacity = Math.Max(1, capacity);
        _onEvict = onEvict;
        _items = new Dictionary<TKey, LinkedListNode<CacheItem>>(capacity);
    }

    public int Count => _items.Count;

    public bool TryGet(TKey key, out TValue? value)
    {
        if (_items.TryGetValue(key, out var node))
        {
            MoveToFront(node);
            value = node.Value.Value;
            return true;
        }

        value = default;
        return false;
    }

    public void Add(TKey key, TValue value)
    {
        if (_items.TryGetValue(key, out var existing))
        {
            existing.Value.Value = value;
            MoveToFront(existing);
            return;
        }

        var node = new LinkedListNode<CacheItem>(new CacheItem(key, value));
        _usageOrder.AddFirst(node);
        _items.Add(key, node);

        if (_items.Count <= _capacity) return;

        var leastUsed = _usageOrder.Last!;
        _usageOrder.RemoveLast();
        _items.Remove(leastUsed.Value.Key);
        _onEvict?.Invoke(leastUsed.Value.Key, leastUsed.Value.Value);
    }

    public bool Remove(TKey key, out TValue? value)
    {
        if (!_items.Remove(key, out var node))
        {
            value = default;
            return false;
        }

        _usageOrder.Remove(node);
        value = node.Value.Value;
        return true;
    }

    public void Clear()
    {
        _items.Clear();
        _usageOrder.Clear();
    }

    private void MoveToFront(LinkedListNode<CacheItem> node)
    {
        if (node.Previous == null) return; //already the most recently used entry
        _usageOrder.Remove(node);
        _usageOrder.AddFirst(node);
    }
}
