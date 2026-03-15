using System.Collections.Generic;
namespace OurGame.Core.DataStructures
{
    
// Custom min-heap priority queue implementation
public class PriorityQueue<TElement, TPriority> where TPriority : System.IComparable<TPriority>
    {
        private List<(TElement Element, TPriority Priority)> heap = new List<(TElement, TPriority)>();

        public int Count => heap.Count;

        public void Enqueue(TElement element, TPriority priority)
        {
            heap.Add((element, priority));
            HeapifyUp(heap.Count - 1);
        }

        public bool TryPeek(out TElement element, out TPriority priority)
        {
            if (heap.Count == 0)
            {
                element = default;
                priority = default;
                return false;
            }
            element = heap[0].Element;
            priority = heap[0].Priority;
            return true;
        }

        public TElement Dequeue()
        {
            if (heap.Count == 0) throw new System.InvalidOperationException("Queue is empty");
            TElement root = heap[0].Element;
            heap[0] = heap[heap.Count - 1];
            heap.RemoveAt(heap.Count - 1);
            if (heap.Count > 0) HeapifyDown(0);
            return root;
        }

        private void HeapifyUp(int index)
        {
            while (index > 0)
            {
                int parent = (index - 1) / 2;
                if (heap[index].Priority.CompareTo(heap[parent].Priority) >= 0) break;
                Swap(index, parent);
                index = parent;
            }
        }

        private void HeapifyDown(int index)
        {
            int size = heap.Count;
            while (true)
            {
                int left = 2 * index + 1;
                int right = 2 * index + 2;
                int smallest = index;

                if (left < size && heap[left].Priority.CompareTo(heap[smallest].Priority) < 0) smallest = left;
                if (right < size && heap[right].Priority.CompareTo(heap[smallest].Priority) < 0) smallest = right;

                if (smallest == index) break;
                Swap(index, smallest);
                index = smallest;
            }
        }

        private void Swap(int i, int j)
        {
            var temp = heap[i];
            heap[i] = heap[j];
            heap[j] = temp;
        }

        public void Clear()
        {
            heap.Clear();
        }
    }

}
