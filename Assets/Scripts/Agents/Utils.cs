using System;
using System.Collections.Generic;


namespace Utils {
	[Serializable]
	public class WorldState {
		public static OrderedMap<object> worldstate;
		public static void SetUp() {
			worldstate = new OrderedMap<object>();
		}

		public static void UpdateState(int id, object value) {
			if(worldstate == null) SetUp();
			worldstate.UpdateValue(id, value);
		}
	}

	[Serializable]
	public class SymbolTable {
		static private Dictionary<string, int> _IDTable;
		static int _NextID = 1;

		public static int GetID(string key) {
			EnsureIDTableInitialized();

			if (string.IsNullOrEmpty(key)) {
				throw new ArgumentException("Key cannot be null or empty");
			}

			if (_IDTable.ContainsKey(key)) {
				return _IDTable[key];
			} else {
				int ID = _NextID++;
				_IDTable[key] = ID;
				return ID;
			}
		}

		private static void EnsureIDTableInitialized() {
			if (_IDTable == null) {
				_NextID = 1;
				_IDTable = new Dictionary<string, int>();
				_IDTable["none"] = 0;
			}
		}

		public static string GetString(int id) {
			EnsureSymbolTableInitialized();

			string key = FindKeyById(id);

			if (key != null) {
				return key;
			}

			throw new ArgumentException($"ID {id} not found in SymbolTable");
		}

		private static void EnsureSymbolTableInitialized() {
			if (_IDTable == null) {
				throw new InvalidOperationException("SymbolTable is not initialized");
			}
		}

		private static string FindKeyById(int id) {
			foreach (var entry in _IDTable) {
				if (entry.Value == id) {
					return entry.Key;
				}
			}

			return null;
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
		public int Count { get { return lastIndex; } }

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
			EnsureCapacity();

			int index = FindInsertionIndex(key);

			// Shift elements to make space for the new item
			for (int i = lastIndex; i > index; i--) {
				container[i] = container[i - 1];
			}

			container[index] = (key, value);
			lastIndex++;
		}

		private void EnsureCapacity() {
			if (lastIndex == container.Length) {
				Array.Resize(ref container, container.Length * 2);

				// Fill the new slots with (int.MaxValue, default(T))
				for (int i = lastIndex; i < container.Length; i++) {
					container[i] = (int.MaxValue, default(T));
				}
			}
		}

		private int FindInsertionIndex(int key) {
			int index = lastIndex;

			// Find the insertion index based on the key order
			while (index > 0 && container[index - 1].Item1 > key) {
				index--;
			}

			return index;
		}

		public void Remove(int key) {
			int index = FindIndexByKey(key);
			RemoveAtIndex(index);
		}

		public void Remove(T value) {
			int index = FindIndexByValue(value);
			RemoveAtIndex(index);
		}

		private int FindIndexByKey(int key) {
			for (int i = 0; i < lastIndex; i++) {
				if (container[i].Item1 == key) {
					return i;
				}
			}
			return -1;
		}

		private int FindIndexByValue(T value) {
			for (int i = 0; i < lastIndex; i++) {
				if (container[i].Item2.Equals(value)) {
					return i;
				}
			}
			return -1;
		}

		private void RemoveAtIndex(int index) {
			if (index >= 0) {
				// Found the element, shift the rest of the items to the left
				for (int i = index; i < lastIndex - 1; i++) {
					container[i] = container[i + 1];
				}

				// Set the last item to (int.MaxValue, default(T))
				container[lastIndex - 1] = (int.MaxValue, default(T));

				lastIndex--;
			}
		}
		public object GetValue(int key) {
			int index = Index(key);
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
			int index = Index(key);
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
			int index = Index(key);

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

		private int Index(int key) {
			return  Array.BinarySearch(container, 0, lastIndex, (key, default(T)), Comparer<(int, T)>.Create((x, y) => x.Item1.CompareTo(y.Item1)));
		}

		public void UpdateValue(int id, T newValue) {
			int index = Index(id);

			if (index >= 0) {
				// Update the existing value
				container[index] = (id, newValue);
			} else {
				// ID not found, insert the new value
				Insert(id, newValue);
			}
		}
	}



}