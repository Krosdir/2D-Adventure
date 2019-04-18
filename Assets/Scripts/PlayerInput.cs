// This script handles inputs for the player. It serves two main purposes: 1) wrap up
// inputs so swapping between mobile and standalone is simpler and 2) keeping inputs
// from Update() in sync with FixedUpdate()

using UnityEngine;

//We first ensure this script runs before all other player scripts to prevent laggy
//inputs
[DefaultExecutionOrder(-100)]
public class PlayerInput : MonoBehaviour
{
    public bool testTouchControlsInEditor = false;  //Should touch controls be tested?
    public float verticalDPadThreshold = .5f;       //Threshold touch pad inputs
    public Joystick joystick;                   //Reference to Thumbstick
    public TouchButton jumpButton;                  //Reference to jump TouchButton

    [HideInInspector] public float horizontalMove;      //Float that stores horizontal input
    [HideInInspector] public float verticalMove;
    [HideInInspector] public bool jumpHeld;         //Bool that stores jump pressed
    [HideInInspector] public bool jumpPressed;      //Bool that stores jump held
    [HideInInspector] public bool crouchHeld;       //Bool that stores crouch pressed
    [HideInInspector] public bool crouchPressed;    //Bool that stores crouch held

    bool dPadCrouchPrev;                            //Previous values of touch Thumbstick
    bool readyToClear;                              //Bool used to keep input in sync


    void Update()
    {
        //Clear out existing input values
        ClearInput();

        //If the Game Manager says the game is over, exit
        //if (GameManager.IsGameOver())
        //    return;

        //Process keyboard, mouse, gamepad (etc) inputs
        ProcessInputs();
        //Process mobile (touch) inputs
        ProcessTouchInputs();

        //Clamp the horizontal input to be between -1 and 1
        horizontalMove = Mathf.Clamp(horizontalMove, -1f, 1f);
        verticalMove = Mathf.Clamp(verticalMove, -1f, 1f);
    }

    void FixedUpdate()
    {
        //In FixedUpdate() we set a flag that lets inputs to be cleared out during the 
        //next Update(). This ensures that all code gets to use the current inputs
        readyToClear = true;
    }

    void ClearInput()
    {
        //If we're not ready to clear input, exit
        if (!readyToClear)
            return;

        //Reset all inputs
        horizontalMove = 0f;
        verticalMove = 0f;
        jumpPressed = false;
        jumpHeld = false;
        crouchPressed = false;
        crouchHeld = false;

        readyToClear = false;
    }

    void ProcessInputs()
    {
        //Accumulate horizontal axis input
        horizontalMove += Input.GetAxis("Horizontal");

        //Accumulate button inputs
        jumpPressed = jumpPressed || Input.GetButtonDown("Jump");
        jumpHeld = jumpHeld || Input.GetButton("Jump");

        //crouchPressed = crouchPressed || Input.GetButtonDown("Crouch");
        //crouchHeld = crouchHeld || Input.GetButton("Crouch");
    }

    void ProcessTouchInputs()
    {
        //If this isn't a mobile platform AND we aren't testing in editor, exit
        if (!Application.isMobilePlatform && !testTouchControlsInEditor)
            return;


        //Accumulate horizontal input
        horizontalMove = joystick.Horizontal;
        verticalMove = joystick.Vertical;



        //Accumulate jump button input
        jumpPressed = jumpPressed || jumpButton.GetButtonDown();
        //jumpHeld = jumpHeld || jumpButton.GetButton();

        //Using thumbstick, accumulate crouch input
        //bool dPadCrouch = thumbstickInput.y <= -verticalDPadThreshold;
        //crouchPressed = crouchPressed || (dPadCrouch && !dPadCrouchPrev);
        //crouchHeld = crouchHeld || dPadCrouch;

        //Record whether or not playing is crouching this frame (used for determining
        //if button is pressed for first time or held
        //dPadCrouchPrev = dPadCrouch;
    }
}
