using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    static GameManager current;
    PlayerMovement player;
    
    public float deathSequenceDuration = 1.5f;  //How long player death takes before restarting

    //Door lockedDoor;                            //The scene door
    //SceneFader sceneFader;                      //The scene fader

    bool isGameOver;                            //Is the game currently over?

    void Awake()
    {
        //If a Game Manager exists and this isn't it...
        if (current != null && current != this)
        {
            //...destroy this and exit. There can only be one Game Manager
            Destroy(gameObject);
            return;
        }

        //Set this as the current game manager
        current = this;

        //Persis this object between scene reloads
        DontDestroyOnLoad(gameObject);

        player = FindObjectOfType<PlayerMovement>();
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        //If the game is over, exit
        if (isGameOver)
            return;
    }

    //public static void RegisterSceneFader(SceneFader fader)
    //{
    //    //If there is no current Game Manager, exit
    //    if (current == null)
    //        return;

    //    //Record the scene fader reference
    //    current.sceneFader = fader;
    //}

    //public static void RegisterDoor(Door door)
    //{
    //    //If there is no current Game Manager, exit
    //    if (current == null)
    //        return;

    //    //Record the door reference
    //    current.lockedDoor = door;
    //}

    public static void PlayerDied()
    {
        //If there is no current Game Manager, exit
        if (current == null)
            return;

        //If we have a scene fader, tell it to fade the scene out
        //if (current.sceneFader != null)
        //    current.sceneFader.FadeSceneOut();

        //Invoke the RestartScene() method after a delay
        current.Invoke("RestartScene", current.deathSequenceDuration);
    }

    void RestartScene()
    {
        //Reload the current scene
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

}
