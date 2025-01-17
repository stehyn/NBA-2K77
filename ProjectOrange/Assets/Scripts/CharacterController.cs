﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class CharacterController : MonoBehaviour {
    public float
        jumpSpeed = 1.5f, ballHeightDiff = 0.8f,
        groundSpeed = 8, airSpeed = 1.5f,
        maxGroundSpeed = 10, maxAirSpeedGrappling = 50, maxAirSpeedNonGrappling = 30,
        brakeStrength = 5,
        groundDrag = 3, airDrag = 0;
    public Vector3 ballBottomPos = new Vector3(-1.1f, -0.5f, 0.7f);
    public bool Grounded { get; private set; }
    public const float gracePeriod = 0.2f;
    public float gracePeriodRemaining { get; private set; } = 0.0f;
    public List<Ball> balls {get; private set; } = new List<Ball>();
    public GrappleGun gun { get; private set; }
    private float distToGround;
    private new Rigidbody rigidbody;

    public float windVolume = 1.0f;

    void Start() {
        Cursor.lockState = CursorLockMode.Locked;
        rigidbody = GetComponent<Rigidbody>();
        distToGround = GetComponent<Collider>().bounds.extents.y;
        gun = GetComponentInChildren<GrappleGun>();

        Cursor.visible = false;
    }

    void Update() {
        gracePeriodRemaining = Mathf.Max(0.0f, gracePeriodRemaining -= Time.deltaTime);
        UpdateGrounded();
        UpdateWindAudio();
      
        if (Input.GetKeyDown(KeyCode.Escape)) {
            // Turn on the cursor
            Cursor.lockState = CursorLockMode.None;
        }

        if (Input.GetKeyDown(KeyCode.Space) && Grounded) {
            // Jump by adding force to the Rigidbody (so we handle gravity)
            rigidbody.AddForce(jumpSpeed * Vector3.up, ForceMode.Impulse);
        }

        float minFovSpeed = 20;
        float maxFovSpeed = 60;

        float minFov = 70;
        float maxFov = 90;

        float fovUpdateSpeed = 0.05f;
        float targetFov = Mathf.Lerp(minFov, maxFov, (rigidbody.velocity.magnitude - minFovSpeed) / (maxFovSpeed - minFovSpeed));
        Camera.main.fieldOfView = Mathf.MoveTowards(Camera.main.fieldOfView, targetFov, fovUpdateSpeed);

    }

    private void UpdateWindAudio () {
        float audioVolume = Mathf.Clamp(Mathf.InverseLerp(10, 40, rigidbody.velocity.magnitude), 0, 1);
        AudioSource audio = transform.Find("WindAudioSource").GetComponent<AudioSource>();
        audio.volume = audioVolume * windVolume;
    }
    private void UpdateGrounded() {
        // Draw a short downwards ray
        RaycastHit hit;
        bool didCollide = Physics.Raycast(transform.position, -Vector3.up, out hit, distToGround + 0.1f);

        // Case 1: Within the dunking grace period
        if (gracePeriodRemaining > 0.0f) {
            Grounded = false;
            return;
        };

        // Case 2: Just collected a ball whilst standing on the hoop
        if (didCollide && HoopController.IsHoop(hit.transform.gameObject) && (balls.Count > 0)) {
            Grounded = false;
            OnDunk(hit.transform.gameObject);
            return;
        }

        // Case 3: Standing on the ground / on the hoop without a ball
        Grounded = didCollide && hit.transform.CompareTag("Surface");
    }

    private bool IsPullingPlayer() {
        return gun.Hook && gun.Hook.GetComponent<GrapplingHook>().Stuck;
    }

    void FixedUpdate() {
        UpdateGrounded();

        float moveSpeed = Grounded ? groundSpeed : airSpeed;
        rigidbody.drag = Grounded && !IsPullingPlayer() ? groundDrag : airDrag;

        // Calculate unit vector of desired direction in the XZ plane
        Vector3 directionVector = new Vector3();
        directionVector.z = Input.GetAxis("Vertical") * moveSpeed;
        directionVector.x = Input.GetAxis("Horizontal") * moveSpeed;
        directionVector = Camera.main.transform.TransformDirection(directionVector);
        directionVector.y = 0;
        directionVector.Normalize();

        rigidbody.AddForce(moveSpeed * directionVector);

        // Cap movement speed
        float speed = rigidbody.velocity.magnitude;
        float maxSpeed =
            Grounded ? maxGroundSpeed
            : IsPullingPlayer() ? maxAirSpeedGrappling
            : maxAirSpeedNonGrappling;
        if (speed > maxSpeed) {
            // Apply force in reverse direction to slow player down
            float brakeSpeed = speed - maxSpeed;
            rigidbody.AddForce(-rigidbody.velocity.normalized * brakeSpeed * brakeStrength);
        }
    }

    /* Add a ball to the stack the player is holding.
     * Note: This is idempotent (won't add the same ball twice).
     */
    public void GiveBall(Ball ball) {
        if (!balls.Contains(ball)) {
            ball.transform.parent = transform;
            ball.Target = ballBottomPos + Vector3.up * balls.Count * ballHeightDiff;
            balls.Add(ball);
            UpdateGrounded();
            if (!Grounded && TimerSystem.GetTime() > 0) ScoreSystem.UpdateMultiplier(1);
            ScoreSystem.UpdateScore(100);
        }
        PlayCollectAudio();
    }

    private void PlayCollectAudio() {
        AudioSource audio = transform.Find("BallAudioSource").GetComponent<AudioSource>();
        float desiredSemitone = Math.Min(ScoreSystem.GetMultiplier()-4, 4);
        audio.pitch = AudioUtils.St2pitch(desiredSemitone);
        audio.Play();
    }
    void OnCollisionEnter(Collision collision) {
        OnDunk(collision.gameObject);
    }

    /** Remove balls and explode away from the hoop */
    void OnDunk(GameObject collisionObject) {
        if (HoopController.IsHoop(collisionObject) && balls.Count > 0) {
            // Start a grace period to stop the multiplier resetting
            gracePeriodRemaining = gracePeriod;

            // Find the associated hoop to explode away from
            HoopController hoop = collisionObject.GetComponentInParent<HoopController>();
            PlayExplosionAudio(hoop.disabled);
            
            ScoreSystem.Dunk(balls.Count, hoop.disabled);
            hoop.HandleDunk(balls);
            ExplodeAwayFrom(hoop);

            ResetHeldBalls();
            gun.DestroyHook();
        }
    }

    private void PlayExplosionAudio(bool isDisabled) {
        if (isDisabled) {
            AudioSource audio = transform.Find("ExplosionBadAudioSource").GetComponent<AudioSource>(); 
            audio.Play();
        } else {
            AudioSource audio = transform.Find("ExplosionGoodAudioSource").GetComponent<AudioSource>(); 
            audio.Play();
        }

    }

    void ResetHeldBalls() {
        foreach (Ball ball in balls) {
            if (!MenuScript.isSandbox) {
                Destroy(ball.gameObject);
            } else {
                ball.Reset();
            }
        }
        balls.Clear();
    }

    void ExplodeAwayFrom(HoopController hoop) {
        Vector3 hoopPosition = hoop.gameObject.transform.position;
        rigidbody.AddExplosionForce(300.0f, hoopPosition, 0.0f, 2.0f);
    }
}
