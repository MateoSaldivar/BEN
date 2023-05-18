using System;
using System.Collections.Generic;


namespace Utils {
	[Serializable]
	public class SymbolTable {
		static private Dictionary<string, int> _IDTable;
		static int _NextID = 1;

		public static int GetID(string key) {
			if (string.IsNullOrEmpty(key)) {
				throw new ArgumentException("Key cannot be null or empty");
			}

			if (_IDTable == null) {
				_NextID = 0;
				_IDTable = new Dictionary<string, int>();
			}

			if (_IDTable.ContainsKey(key)) {
				return _IDTable[key];
			} else {
				int ID = _NextID++;
				_IDTable[key] = ID;
				return ID;
			}
		}

		public static string GetString(int id) {
			if (_IDTable == null) {
				throw new InvalidOperationException("SymbolTable is not initialized");
			}

			foreach (var entry in _IDTable) {
				if (entry.Value == id) {
					return entry.Key;
				}
			}

			throw new ArgumentException($"ID {id} not found in SymbolTable");
		}

		public static void Destroy() {
			if (_IDTable != null) _IDTable.Clear();
			_IDTable = null;
			_NextID = 0;
		}
	}

	[Serializable]
	public class OrderedMap<T> {
		private const int DefaultCapacity = 4;

		private (int, T)[] container;
		private int lastIndex;
		private int pointer;
		public int length { get { return lastIndex; } }

		public OrderedMap() {
			container = new (int, T)[DefaultCapacity];
			lastIndex = 0;
			pointer = 0;

			// Initialize the container with (int.MaxValue, default(T)) values
			for (int i = 0; i < container.Length; i++) {
				container[i] = (int.MaxValue, default(T));
			}
		}

		public OrderedMap(int capacity) {
			container = new (int, T)[capacity];
			lastIndex = 0;
			pointer = 0;

			// Initialize the container with (int.MaxValue, default(T)) values
			for (int i = 0; i < container.Length; i++) {
				container[i] = (int.MaxValue, default(T));
			}
		}

		public void Insert(int key, T value) {
			if (lastIndex == container.Length) {
				Array.Resize(ref container, container.Length * 2);

				// Fill the new slots with (int.MaxValue, null)
				for (int i = lastIndex; i < container.Length; i++) {
					container[i] = (int.MaxValue, default(T));
				}
			}

			int index = lastIndex;

			// Find the insertion index based on the key order
			while (index > 0 && container[index - 1].Item1 > key) {
				container[index] = container[index - 1];
				index--;
			}

			container[index] = (key, value);
			lastIndex++;
		}

		public void Remove(int key) {
			int index = Array.BinarySearch(container, 0, lastIndex, (key, default(T)));

			if (index >= 0) {
				// Found the key, shift the rest of the items to the left
				for (int i = index; i < lastIndex - 1; i++) {
					container[i] = container[i + 1];
				}

				// Set the last item to (int.MaxValue, default(T))
				container[lastIndex - 1] = (int.MaxValue, default(T));

				lastIndex--;
			}
		}

		public object GetValue(int key) {
			int index = Array.BinarySearch(container, 0, lastIndex, (key, default(T)));
			return index >= 0 ? container[index].Item2 : null;
		}

		public T Cycle(int key) {
			while (pointer < lastIndex && key <= container[pointer].Item1) {
				if (container[pointer].Item1 == key) {
					return container[pointer].Item2;
				}

				pointer++;
			}

			return default(T);
		}

		public int GetCurrentKey() {
			return container[pointer].Item1;
		}

		public void ResetPointer() {
			pointer = 0;
		}

		public T CleanCycle() {
			ResetPointer();
			return Cycle(int.MaxValue);
		}

		public int GetPointerValue() {
			return pointer;
		}

		public T GetCurrentValue() {
			return pointer < lastIndex ? container[pointer].Item2 : default(T);
		}

		public void Advance() {
			pointer++;
		}

		public bool CompletedList() {
			return pointer >= lastIndex;
		}

		public (int, T) GetData() {
			return container[pointer];
		}

		public bool ContainsKey(int key) {
			int index = Array.BinarySearch(container, 0, lastIndex, (key, default(T)));
			return index >= 0;
		}

		public T this[int index] {
			get {
				if (index >= 0 && index < lastIndex) {
					return container[index].Item2;
				} else {
					throw new IndexOutOfRangeException();
				}
			}
		}

		public bool TryGetValue(int key, out T value) {
			int index = Array.BinarySearch(container, 0, lastIndex, (key, default(T)));

			if (index >= 0) {
				value = container[index].Item2;
				return true;
			} else {
				value = default(T);
				return false;
			}
		}

		public int GetKey(int index) {
			if (index >= 0 && index < lastIndex) {
				return container[index].Item1;
			} else {
				throw new IndexOutOfRangeException();
			}

		}
	}

}