
using System;
using System.Collections.Generic;


[Serializable]
public class SerializableFileActions {
    public List<FileAction> actions;

    public SerializableFileActions(List<FileAction> actions) {
        this.actions = actions;
    }
}


[Serializable]
public class FileAction {
    

    public FileAction() {

    }
    public string name;
    public WorldState[] environmentalPreconditions;
    public WorldState[] preconditions;
    public WorldState[] effects;
    public string utilityBelief;
    public int[] connections;
    public int actionID;

    [Serializable]
    public class WorldState {
        public string key;
        public bool op;
    }
}
