using UnityEngine;
using UnityEngine.InputSystem;

// Press C to crouch, press C again to stand up (a toggle, not a hold).
// Crouching shrinks the player's body so they fit under low things, and lowers the camera smoothly.
// You can't stand up while something is above your head: the key press is simply ignored.
[RequireComponent(typeof(CharacterController))]
public class PlayerCrouch : MonoBehaviour
{
    public Transform cameraPivot;          // the camera, moved down to crouching eye height
    public float standHeight = 1.8f;       // body height in metres
    public float crouchHeight = 1.1f;
    public float standEyeHeight = 1.65f;   // camera height above the feet
    public float crouchEyeHeight = 0.95f;
    public float changeSpeed = 5f;         // metres per second the body/camera move when changing pose

    public bool IsCrouched { get; private set; }

    CharacterController controller;

    void Awake() => controller = GetComponent<CharacterController>();

    void Update()
    {
        if (Controls.Pressed(GameAction.Crouch))
        {
            if (!IsCrouched) IsCrouched = true;
            else if (HasHeadroom()) IsCrouched = false;
        }

        // Ease the body height toward the target, keeping the feet on the ground.
        float targetHeight = IsCrouched ? crouchHeight : standHeight;
        controller.height = Mathf.MoveTowards(controller.height, targetHeight, changeSpeed * Time.deltaTime);
        controller.center = new Vector3(0f, controller.height * 0.5f, 0f);

        // Ease the camera to the matching eye height.
        float targetEyes = IsCrouched ? crouchEyeHeight : standEyeHeight;
        Vector3 eyes = cameraPivot.localPosition;
        eyes.y = Mathf.MoveTowards(eyes.y, targetEyes, changeSpeed * Time.deltaTime);
        cameraPivot.localPosition = eyes;
    }

    // True when there is room above the head to stand up to full height.
    bool HasHeadroom()
    {
        float radius = controller.radius * 0.95f;
        // Start the check inside the top of the body (things the sphere starts inside are ignored,
        // which skips the player's own collider), then sweep up by the height we would gain.
        Vector3 start = transform.position + Vector3.up * (controller.height - controller.radius);
        float distance = standHeight - controller.height + 0.05f;
        return !Physics.SphereCast(start, radius, Vector3.up, out _, distance, ~0, QueryTriggerInteraction.Ignore);
    }
}
