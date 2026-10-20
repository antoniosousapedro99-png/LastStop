using UnityEngine;
using UnityEngine.InputSystem;

// Walks the player around with WASD (hold Shift to move faster) and keeps them on the ground.
// Uses a CharacterController, which slides along walls and climbs small steps by itself.
[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    public float walkSpeed = 2.2f;   // metres per second, a slow cautious walk
    public float runSpeed = 4.0f;
    public float gravity = -15f;

    CharacterController controller;
    float verticalSpeed;

    // How fast the player actually moved last frame (footsteps listen to this).
    public float CurrentSpeed { get; private set; }
    public bool IsGrounded => controller.isGrounded;

    void Awake() => controller = GetComponent<CharacterController>();

    void Update()
    {
        var keyboard = Keyboard.current;
        Vector2 input = Vector2.zero;
        bool running = false;
        if (keyboard != null)
        {
            if (keyboard.wKey.isPressed) input.y += 1;
            if (keyboard.sKey.isPressed) input.y -= 1;
            if (keyboard.dKey.isPressed) input.x += 1;
            if (keyboard.aKey.isPressed) input.x -= 1;
            running = keyboard.leftShiftKey.isPressed;
        }
        input = Vector2.ClampMagnitude(input, 1f);

        // Move relative to where the body faces (the body turns left/right with the mouse).
        Vector3 move = transform.right * input.x + transform.forward * input.y;
        move *= running ? runSpeed : walkSpeed;

        // Simple gravity: pull down, and reset when standing on something.
        if (controller.isGrounded && verticalSpeed < 0) verticalSpeed = -2f;
        verticalSpeed += gravity * Time.deltaTime;
        move.y = verticalSpeed;

        controller.Move(move * Time.deltaTime);
        Vector3 flat = controller.velocity; flat.y = 0;
        CurrentSpeed = flat.magnitude;
    }
}
