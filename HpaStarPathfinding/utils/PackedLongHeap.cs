using System.Runtime.CompilerServices;

namespace HpaStarPathfinding.utils;

/// <summary>
/// Binary min-heap over (priority, key) pairs packed into a single long per element:
/// ordering is one integer comparison, elements live in one dense array (8 bytes each)
/// and a second dense array maps key => heap slot for O(1) Contains/UpdatePriority.
/// No object references anywhere, so iterating the heap never chases pointers.
/// </summary>
public sealed class PackedLongHeap
{
    private long[] _elements; //1-based: priority in the high 32 bits, key in the low 32 bits
    private int[] _positions; //key => slot in _elements; stale slots are rejected by the key check in Contains

    public int Count { get; private set; }

    public PackedLongHeap(int maxKeys)
    {
        _elements = new long[maxKeys + 1];
        _positions = new int[maxKeys];
    }

    public void EnsureCapacity(int maxKeys)
    {
        if (_positions.Length >= maxKeys) return;
        _elements = new long[maxKeys + 1];
        _positions = new int[maxKeys];
        Count = 0;
    }

    public void Clear()
    {
        Count = 0;
    }

    public bool Contains(int key)
    {
        int slot = _positions[key];
        return slot >= 1 && slot <= Count && (int)(uint)_elements[slot] == key;
    }

    public void Enqueue(int key, int priority)
    {
        SiftUp(++Count, Pack(priority, key));
    }

    public int Dequeue()
    {
        long top = _elements[1];
        long last = _elements[Count--];
        if (Count > 0)
        {
            int slot = 1;
            while (true)
            {
                int child = slot << 1;
                if (child > Count) break;
                long childElement = _elements[child];
                if (child + 1 <= Count && _elements[child + 1] < childElement)
                {
                    child++;
                    childElement = _elements[child];
                }
                if (childElement >= last) break;
                _elements[slot] = childElement;
                _positions[(int)(uint)childElement] = slot;
                slot = child;
            }
            _elements[slot] = last;
            _positions[(int)(uint)last] = slot;
        }
        return (int)(uint)top;
    }

    //Lowers the priority of a key already in the heap (A* only ever improves priorities)
    public void UpdatePriority(int key, int priority)
    {
        SiftUp(_positions[key], Pack(priority, key));
    }

    private void SiftUp(int slot, long element)
    {
        while (slot > 1)
        {
            int parent = slot >> 1;
            long parentElement = _elements[parent];
            if (parentElement <= element) break;
            _elements[slot] = parentElement;
            _positions[(int)(uint)parentElement] = slot;
            slot = parent;
        }
        _elements[slot] = element;
        _positions[(int)(uint)element] = slot;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static long Pack(int priority, int key)
    {
        return (long)priority << 32 | (uint)key;
    }
}
