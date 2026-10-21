using UnityEngine;
using UnityEngine.InputSystem;

// Walks the player around with WASD, hold Shift to sprint, Space for a short hop.
// Keeps them on the ground with gravity. Uses a CharacterController, which slides along walls
// and climbs small steps by itself. While crouched (see PlayerCrouch) the player creeps slowly
// and can't sprint or jump.
[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    public float walkSpeed = 2.3f;     // metres per second, a slow cautious walk
    public float runSpeed = 4.3f;
    public float crouchSpeed = 1.3f;
    public float jumpHeight = 0.4f;    // metres: a short, heavy hop, not a platformer leap
    public float gravity = -15f;

    CharacterController controller;
    PlayerCrouch crouch;
    float verticalSpeed;

    // How fast the player actually moved last frame (footsteps listen to this).
    public float CurrentSpeed { get; private set; }
    public bool IsGrounded => controller.isGrounded;
    public bool IsCrouched => crouch != null && crouch.IsCrouched;

    void Awake()
    {
        controller = GetComponent<CharacterController>();
        crouch = GetComponent<PlayerCrouch>();
    }

    void Update()
    {
        var keyboard = Keyboard.current;
        Vector2 input = Vector2.zero;
        bool running = false, jump = false;
        if (keyboard != null)
        {
            if (keyboard.wKey.isPressed) input.y += 1;
            if (keyboard.sKey.isPressed) input.y -= 1;
            if (keyboard.dKey.isPressed) input.x += 1;
            if (keyboard.aKey.isPressed) input.x -= 1;
            running = keyboard.leftShiftKey.isPressed && !IsCrouched;
            jump = keyboard.spaceKey.wasPressedThisFrame && !IsCrouched;
        }
        input = Vector2.ClampMagnitude(input, 1f);

        // Move relative to where the body faces (the body turns left/right with the mouse).
        float speed = IsCrouched ? crouchSpeed : running ? runSpeed : walkSpeed;
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
