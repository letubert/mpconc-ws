using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuickSort.cs
{
    public class QuickSort
    {
        public static void QuickSort_Sequential<T>(T[] items) where T : IComparable<T>
        {
            QuickSort_Sequential(items, 0, items.Length);
        }
        private static void QuickSort_Sequential<T>(T[] items, int left, int right) where T : IComparable<T>
        {
            if (left == right) return;
            int pivot = Partition(items, left, right);
            QuickSort_Sequential(items, left, pivot);
            QuickSort_Sequential(items, pivot + 1, right);
        }
        private static int Partition<T>(T[] items, int left, int right) where T : IComparable<T>
        {
            int pivotPos = (right + left) / 2; //often a random index between left and right is used
            T pivotValue = items[pivotPos];
            Swap(ref items[right - 1], ref items[pivotPos]);
            int store = left;
            for (int i = left; i < right - 1; ++i)
            {
                if (items[i].CompareTo(pivotValue) < 0)
                {
                    Swap(ref items[i], ref items[store]);
                    ++store;
                }
            }
            Swap(ref items[right - 1], ref items[store]);
            return store;
        }
        private static void Swap<T>(ref T a, ref T b)
        {
            T temp = a;
            a = b;
            b = temp;
        }

        public static void QuickSort_Parallel<T>(T[] items) where T : IComparable<T>
        {
            QuickSort_Parallel(items, 0, items.Length);
        }
        private static void QuickSort_Parallel<T>(T[] items, int left, int right) where T : IComparable<T>
        {
            if (right - left < 2)
            {
                if (left+1 == right &&
                    items[left].CompareTo(items[right]) > 0)
                    Swap(ref items[left], ref items[right]);
                return;
            }
            int pivot = Partition(items, left, right);
            Task leftTask = Task.Run(() => QuickSort_Parallel(items, left, pivot));
            Task rightTask = Task.Run(() => QuickSort_Parallel(items, pivot + 1, right));
            Task.WaitAll(leftTask, rightTask);
        }

        // TODO : 1.4
        // (1) write a parallel and fast quick sort
        public static void QuickSort_Parallel_Threshold<T>(T[] items) where T : IComparable<T>
        {

        }

    }
}
