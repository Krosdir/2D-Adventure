using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerCombo : MonoBehaviour
{

    public string[] buttons;
    private int currentIndex = 0; //moves along the array as buttons are pressed

    public float allowedTimeBetweenButtons = 0.3f; //tweak as needed
    private float timeLastButtonPressed;

    public PlayerCombo(string[] b)
    {
        buttons = b;
    }

    //usage: call this once a frame. when the combo has been completed, it will return true
    public bool Check()
    {
        if (Time.time > timeLastButtonPressed + allowedTimeBetweenButtons) currentIndex = 0;
        {
            if (currentIndex < buttons.Length)
            {
                if ((buttons[currentIndex] == "attack")
                    || (buttons[currentIndex] == "shoot"))
                //(buttons[currentIndex] != "down" && buttons[currentIndex] != "up" && buttons[currentIndex] != "left" && buttons[currentIndex] != "right" && Input.GetButtonDown(buttons[currentIndex])))
                {
                    timeLastButtonPressed = Time.time;
                    currentIndex++;
                }

                if (currentIndex >= buttons.Length)
                {
                    currentIndex = 0;
                    return true;
                }
                else return false;
            }
        }

        return false;
    }
}
