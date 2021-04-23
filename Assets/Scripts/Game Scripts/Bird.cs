/* 
    ------------------- Code Monkey -------------------

    Thank you for downloading this package
    I hope you find it useful in your projects
    If you have any questions let me know
    Cheers!

               unitycodemonkey.com
    --------------------------------------------------
 */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;
using CodeMonkey;
using WiimoteApi;

public class Bird : MonoBehaviour {

    private const float JUMP_AMOUNT = 90f;

    private static Bird instance;

    public static Bird GetInstance() {
        return instance;
    }

    public event EventHandler OnDied;
    public event EventHandler OnStartedPlaying;

    private Rigidbody2D birdRigidbody2D;
    private State state;

    // Wiimote variables
    public WiimoteModel model;

    private Quaternion initial_rotation;

    private Wiimote wiimote;

    private Vector2 scrollPosition;

    private Vector3 wmpOffset = Vector3.zero;

    private enum State {
        WaitingToStart,
        Playing,
        Dead
    }

    private void Awake() {
        instance = this;
        birdRigidbody2D = GetComponent<Rigidbody2D>();
        birdRigidbody2D.bodyType = RigidbodyType2D.Static;
        state = State.WaitingToStart;
    }

    private void Update()
    {
        if (!WiimoteManager.HasWiimote()) { return; }

        wiimote = WiimoteManager.Wiimotes[0];

        int ret;

        do
        {
            ret = wiimote.ReadWiimoteData();

            if (ret > 0 && wiimote.current_ext == ExtensionController.MOTIONPLUS)
            {
                // Divide by 95Hz (average updates per second from wiimote)
                Vector3 offset = new Vector3(-wiimote.MotionPlus.PitchSpeed,
                                                wiimote.MotionPlus.YawSpeed,
                                                wiimote.MotionPlus.RollSpeed) / 95f; 
                wmpOffset += offset;
                switch (state)
                {
                    default:
                    case State.WaitingToStart:
                        if (TestInput())
                        {
                            // Start playing
                            state = State.Playing;
                            birdRigidbody2D.bodyType = RigidbodyType2D.Dynamic;
                            Jump();
                            if (OnStartedPlaying != null) OnStartedPlaying(this, EventArgs.Empty);
                        }
                        break;
                    case State.Playing:
                        // If wiimote is moved upwards
                        if (wiimote.MotionPlus.PitchSlow == false)
                        {
                            Jump();
                        }

                        if (TestInput())
                        {
                            Jump();
                        }

                        // Rotate bird as it jumps and falls
                        transform.eulerAngles = new Vector3(0, 0, birdRigidbody2D.velocity.y * .15f);
                        break;
                    case State.Dead:
                        break;
                }

            }
        } while (ret > 0);

        switch (state) {
        default:
        case State.WaitingToStart:
            if (TestInput()) {
                // Start playing
                state = State.Playing;
                birdRigidbody2D.bodyType = RigidbodyType2D.Dynamic;
                Jump();
                if (OnStartedPlaying != null) OnStartedPlaying(this, EventArgs.Empty);
            }
            break;
        case State.Playing:
            if (TestInput()) {
                Jump();
            }
            // Rotate bird as it jumps and falls
            transform.eulerAngles = new Vector3(0, 0, birdRigidbody2D.velocity.y * .15f);
            break;
        case State.Dead:
            break;
        }
    }

    private bool TestInput() {
        return
            wiimote.Button.a;
    }

    private void Jump() {
        birdRigidbody2D.velocity = Vector2.up * JUMP_AMOUNT;
        SoundManager.PlaySound(SoundManager.Sound.BirdJump);
    }

    private void OnTriggerEnter2D(Collider2D collider) {
        birdRigidbody2D.bodyType = RigidbodyType2D.Static;
        SoundManager.PlaySound(SoundManager.Sound.Lose);
        if (OnDied != null) OnDied(this, EventArgs.Empty);
    }

    // Display for player to connect their wiimote to Unity
    void OnGUI()
    {
        GUI.Box(new Rect(0, 0, 235, Screen.height), "");

        GUILayout.BeginVertical(GUILayout.Width(230));
        // Find wiimote
        GUILayout.Label(" Wiimote Found: " + WiimoteManager.HasWiimote());
        if (GUILayout.Button("Find Your Wiimote"))
        {
            WiimoteManager.FindWiimotes();
        }

        // Remove wiimote
        if (GUILayout.Button("Cleanup"))
        {
            WiimoteManager.Cleanup(wiimote);
            wiimote = null;
        }

        if (wiimote == null)
            return;

        GUILayout.Label(" Press A on your wiimote to start!");
        // Finds and activates wii motion plus
        if (GUILayout.Button("Press if Wiimotion Plus attached"))
        {
            wiimote.SetupIRCamera(IRDataType.BASIC);
            wiimote.RequestIdentifyWiiMotionPlus();
        }
        if ((wiimote.wmp_attached || wiimote.Type == WiimoteType.PROCONTROLLER)
                            && GUILayout.Button("Press to activate Wiimotion Plus"))
            wiimote.ActivateWiiMotionPlus();

        // Instructions for player
        if (wiimote.current_ext == ExtensionController.MOTIONPLUS)
        {
            GUILayout.Label(" Wii Motion Plus Activated!");
            GUILayout.Label(" Press A on your wiimote to start!");
            GUILayout.Label(" Press A or shake wiimote upwards to control the flappy bird");
        }

        GUILayout.EndVertical();
    }

    [System.Serializable]
    public class WiimoteModel
    {
        public Transform bird;
    }

    void OnApplicationQuit()
    {
        if (wiimote != null)
        {
            WiimoteManager.Cleanup(wiimote);
            wiimote = null;
        }
    }
}
