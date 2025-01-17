﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class TutorialSystem : MonoBehaviour
{
    public TextMeshProUGUI instructionText;
    public const float defaultUpdateDelay = 0.8f;
    private float updateDelayRemaining = 0.0f;
    private bool updateDelayed = false;
    private GameObject player;
    private Rigidbody playerRigidbody;
    private CharacterController playerCharacterController;
    private const float eps = 0.2f;

    enum Step {
        Welcome,
        Moving,
        Jumping,
        Grappling,
        BallCollection,
        BallGrappling,
        Swinging,
        Dunking,
        Disabled,
        Score,
        Multiplier,
        Completed
    }

    private Step step = Step.Welcome;

    // Start is called before the first frame update
    void Start() {
        instructionText = this.GetComponent<TextMeshProUGUI>();
        instructionText.alignment = TextAlignmentOptions.Center;
        player = GameObject.FindWithTag("Player");
        playerRigidbody = player.GetComponent<Rigidbody>();
        playerCharacterController = player.GetComponent<CharacterController>();
        UpdateText();
    }

    // Update is called once per frame
    void FixedUpdate() {
        updateDelayRemaining = Mathf.Max(0.0f, updateDelayRemaining -= Time.deltaTime);
        if (updateDelayRemaining == 0.0f && updateDelayed) UpdateInstruction();
        DetectProgress();
    }

    /** Check success condition to go to the next step */
    private void DetectProgress() {
        if (updateDelayRemaining > 0.0f) return;

        switch(step) {
            case Step.Welcome: {
                BeginInstructionUpdate(1.5f);
                break;
            }
            case Step.Moving: {
                Vector3 playerVelocity = playerRigidbody.velocity;
                if (Mathf.Abs(playerVelocity.x) > eps || Mathf.Abs(playerVelocity.z) > eps) {
                    BeginInstructionUpdate(defaultUpdateDelay);
                }
                break;
            }
            case Step.Jumping:
            {
                Vector3 playerVelocity = playerRigidbody.velocity;
                if (Input.GetKeyDown("space") || playerVelocity.y > 2f) {
                    BeginInstructionUpdate(defaultUpdateDelay);
                }
                break;
            }
            case Step.Grappling:
            {
                if (playerCharacterController.gun.Hook) {
                    BeginInstructionUpdate(defaultUpdateDelay);
                }
                break;
            }
            case Step.BallCollection:
            {
                if (playerCharacterController.balls.Count >= 1) {
                    BeginInstructionUpdate(defaultUpdateDelay);
                }
                break;
            }
            case Step.BallGrappling:
            {
                if (playerCharacterController.balls.Count == 2) {
                    BeginInstructionUpdate(defaultUpdateDelay);
                }
                break;
            }
            case Step.Swinging:
            {
                if (playerRigidbody.position.z <= -60 && playerRigidbody.position.y <= 20) {
                    BeginInstructionUpdate(defaultUpdateDelay);
                }
                break;
            }
            case Step.Dunking:
            {
                    if (playerCharacterController.gracePeriodRemaining > 0.0f) {
                        BeginInstructionUpdate(defaultUpdateDelay);
                    }
                    break;
            }
            case Step.Disabled:
            case Step.Score:
            case Step.Multiplier:
            {
                BeginInstructionUpdate(5.0f);
                break;
            }
        }
    }

    /** Begin a timer to move to the next instruction */
    private void BeginInstructionUpdate(float delay) {
        updateDelayRemaining = delay;
        updateDelayed = true;
    }

    /** Increment the step enum */
    private void UpdateInstruction() {
        updateDelayed = false;
        switch(step) {
            case Step.Welcome:
                step = Step.Moving;
                break;
            case Step.Moving:
                step = Step.Jumping;
                break;
            case Step.Jumping:
                step = Step.Grappling;
                break;
            case Step.Grappling:
                step = Step.BallCollection;
                break;
            case Step.BallCollection:
                step = Step.BallGrappling;
                break;
            case Step.BallGrappling:
                step = Step.Swinging;
                break;
            case Step.Swinging:
                step = Step.Score;
                break;
            case Step.Score:
                step = Step.Dunking;
                break;
            case Step.Dunking:
                step = Step.Disabled;
                break;
            case Step.Disabled:
                step = Step.Multiplier;
                break;
            case Step.Multiplier:
                step = Step.Completed;
                break;
        }

        UpdateText();
    }

    /** Update instruction based on current step */
    private void UpdateText()
    {
        switch(step) {
            case Step.Welcome: {
                instructionText.text = "Welcome, athlete!";
                break;
            }
            case Step.Moving: {
                instructionText.text = "WASD to move";
                break;
            }
            case Step.Jumping: {
                instructionText.text = "Space to jump";
                break;
            }
            case Step.BallCollection: {
                instructionText.text = "Touch balls to pick them up";
                break;
            }
            case Step.Grappling: {
                instructionText.text = "Hold left click to shoot your grappling hook";
                break;
            }
            case Step.BallGrappling: {
                instructionText.text = "Use the grappling hook to pull balls towards you";
                break;
            }
            case Step.Swinging: {
                instructionText.text = "Swing across to the other platform with your grappling hook";
                break;
            }
            case Step.Score: {
                instructionText.text = "Score points for picking up balls, flying and dunking";
                break;
            }
            case Step.Dunking: {
                instructionText.text = "Jump into the hoop to dunk your balls";
                break;
            }
            case Step.Disabled: {
                instructionText.text = "After a dunk, hoops turn blue and offer less score";
                break;
            }
            case Step.Multiplier: {
                instructionText.text = "Avoid touching the ground to build up your multiplier";
                break;
            }
            case Step.Completed: {
                instructionText.text = "When you're ready, hit escape to go to the next level";
                break;
            }
        }
    }
}
