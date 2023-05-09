using UnityEngine;
using BEN;
using UnityEngine.SceneManagement;
/*
* ZiroDev Copyright(c)
*
*/
public class Player : MonoBehaviour {

    #region Variables
    public int currentArea;
    public int prevArea;
    public static Player instance;
    private PlayerMovement mover;
    public Agent agent = new Agent();
    public int money;
    #endregion

    #region Unity Methods
    
    void Awake() {
        mover = GetComponent<PlayerMovement>();
        instance = this;

    }

    void Update() {
        
    }

    public string GetMoney() {
        return "$ " + money;
	}

    public void ResetScene() {
        Scene currentScene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(currentScene.name);
    }

    #endregion
}
