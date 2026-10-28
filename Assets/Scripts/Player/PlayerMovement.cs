using UnityEngine;
using UnityEngine.InputSystem;

// Walks the player around with WASD, hold Shift to sprint, Space for a short hop.
// Keeps them on the ground with gravity. Uses a CharacterController, which slides along walls
// and climbs small steps by itself. While crouched (see PlayerCrouch) the player creeps slowly
// and can't sprint or jump.
// Sprinting uses stamina: about 6 s of it, then the player is winded and can only jog (slower than
// Frankie) until they've caught their breath again. PlayerBreathing makes this audible.
[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    public float walkSpeed = 2.3f;     // metres per second, a slow cautious walk
    public float runSpeed = 4.3f;
    public float crouchSpeed = 1.3f;
    public float windedSpeed = 3.0f;   // the jog when out of breath: Frankie (3.5) gains on it
    public float sprintSeconds = 8f;   // full stamina lasts this long at a sprint
    public float recoverSeconds = 8f;  // empty to full again while not sprinting
    public float recoveredAt = 0.4f;   // once winded, sprinting comes back at this much stamina
    public float jumpHeight = 0.6f;    // metres: a short, heavy hop (enough for a crate), not a platformer leap
    public float gravity = -15f;

    CharacterController controller;
    PlayerCrouch crouch;
    PlayerVault vault;
    bool queuedJump;                   // a jump asked for by PlayerVault (Jump tapped at a ledge)
    float verticalSpeed;
    float stamina = 1f;

    // 1 rested .. 0 completely out of breath. Winded: ran it to empty and hasn't recovered yet.
    public float Stamina => stamina;
    public bool Winded { get; private set; }

    // How fast the player actually moved last frame (footsteps listen to this).
    public float CurrentSpeed { get; private set; }
    public bool IsGrounded => controller.isGrounded;
    public bool IsCrouched => crouch != null && crouch.IsCrouched;

    void Awake()
    {
        controller = GetComponent<CharacterController>();
        crouch = GetComponent<PlayerCrouch>();
        vault = GetComponent<PlayerVault>();
    }

    // Jump on the next frame (PlayerVault: Jump was only tapped at a ledge, not held to vault).
    public void QueueJump() => queuedJump = true;

    void Update()
    {
        // Keys (or the left stick) from Controls, so they follow the player's bindings.
        Vector2 input = Controls.Move();
        bool running = Controls.Sprinting() && !IsCrouched;
        // At a vaultable ledge the jump waits: holding Jump vaults (PlayerVault), letting go jumps (QueueJump).
        bool jump = (Controls.Pressed(GameAction.Jump) && !(vault && vault.LedgeAhead) || queuedJump) && !IsCrouched;
        queuedJump = false;

        // Move relative to where the body faces (the body turns left/right with the mouse).
        // Stamina drains only while actually sprinting somewhere, and refills otherwise.
        bool sprinting = running && !Winded && input.sqrMagnitude > 0.01f;
        stamina = Mathf.Clamp01(stamina + Time.deltaTime * (sprinting ? -1f / sprintSeconds : 1f / recoverSeconds));
        if (stamina <= 0f) Winded = true;
        else if (Winded && stamina >= recoveredAt) Winded = false;

        float speed = IsCrouched ? crouchSpeed : running ? (Winded ? windedSpeed : runSpeed) : walkSpeed;
        Vector3 move = (transform.right * input.x + transform.forward * input.y) * speed;

        // Gravity pulls down; standing on something resets it. A jump is the upward speed
        // needed to rise exactly jumpHeight against gravity.
        if (controller.isGrounded && verticalSpeed < 0) verticalSpeed = -2f;
        if (jump && controller.isGrounded) verticalSpeed = Mathf.Sqrt(2f * jumpHeight * -gravity);
        // Use the average of the speed before and after gravity acts this frame, so the jump
        // reaches the same height whether the game runs at 30 or 144 frames per second.
        float speedBefore = verticalSpeed;
        verticalSpeed += gravity * Time.deltaTime;
        move.y = (speedBefore + verticalSpeed) * 0.5f;

        controller.Move(move * Time.deltaTime);
        Vector3 flat = controller.velocity; flat.y = 0;
        CurrentSpeed = flat.magnitude;
    }
}
