using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
/*
* ZiroDev Copyright(c)
*
*/
namespace BDI {
	public enum State {
		Inactive,
		Running,
		Success,
		Failed
	}

	public class Agent {
		private Dictionary<int, Belief> beliefs;


	}


	[Serializable]
	public class MentalState {

		public string name;
		public float lifeTime;
		public float decompositionRate;
		public float priority;
		public float Degrade(float deltaTime) {
			lifeTime -= decompositionRate * deltaTime;
			return lifeTime;
		}
	}

	[Serializable]
	public class Belief : MentalState {
		public object value;
		public Belief(string name, object value) {
			this.name = name;
			this.value = value;
			lifeTime = 1;
			decompositionRate = 0;
		}

		public Belief(string name, object value, float lifeTime, float decompositionRate) {
			this.name = name;
			this.value = value;
			this.lifeTime = lifeTime;
			this.decompositionRate = decompositionRate;
		}
	}


	public class SymbolTable {
		private Dictionary<string, int> _IDTable;
		int _NextID = 0;

		public int GetID(string key) {
			if(_IDTable == null) _IDTable = new Dictionary<string, int>();

			if (_IDTable.ContainsKey(key)) {
				return _IDTable[key];
			} else { 
				int ID = _NextID++;
				_IDTable[key] = ID;
				return ID;
			}
		}
	}
}
