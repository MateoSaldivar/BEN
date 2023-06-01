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
    public Agent agent = new Agent();
    public int money;
    #endregion

    #region Unity Methods
    


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
