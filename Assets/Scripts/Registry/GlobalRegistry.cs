using UnityEngine;

public class GlobalRegistry : MonoBehaviour, IRegistry {
    private static GlobalRegistry _instance;
    public static GlobalRegistry instance {
        get {
            if (_instance == null)
                _instance = FindObjectOfType<GlobalRegistry>();
            return _instance;
        }
    }

    public CompositeRegistry gRegistry;

    private Player player;
    public static Player Player {
        get {
            return _instance.player;
        }
    }

    private TimeManager timeManager;
    public static TimeManager TimeManager {
		get { return _instance.timeManager; }
	}

    private WayPointContainer wayPointContainer;
    public static WayPointContainer WayPointContainer {
		get {
            return _instance.wayPointContainer;
		}
        set {
            _instance.wayPointContainer = value;
        }
	}
    private void Awake() {
        if (_instance == null) {
            _instance = this;
            DontDestroyOnLoad(gameObject);
            gRegistry = new CompositeRegistry();
        } else {
            Destroy(gameObject);
        }
        Load();
    }

    public void Load() {
        player = FindObjectOfType<Player>();
        timeManager = FindObjectOfType<TimeManager>();
        wayPointContainer = FindObjectOfType<WayPointContainer>();
    }

    public void Destroy() {
        // Destruction logic here
    }
}
