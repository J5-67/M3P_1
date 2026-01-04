using UnityEngine;

public class FlashLight : MonoBehaviour
{
    [Header("--- Settings ---")]
    [SerializeField] private Light flashlight;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip clickSound;

    [Header("--- Sway Settings ---")]
    [SerializeField] private float smooth = 8f;
    [SerializeField] private float swayMultiplier = 2f;

    private Player playerScript;
    private bool isFlashLightOn = false;

    private void Start()
    {
        if (!transform.root.TryGetComponent(out playerScript))
        {
            Debug.LogError("Player 스크립트를 찾을 수 없습니다.");
        }
    }

    private void Update()
    {
        if (playerScript == null) return;

        Vector2 lookInput = playerScript.GetLookInput();

        float mouseX = lookInput.x * swayMultiplier;
        float mouseY = lookInput.y * swayMultiplier;

        Quaternion rotationX = Quaternion.AngleAxis(-mouseY, Vector3.right);
        Quaternion rotationY = Quaternion.AngleAxis(mouseX, Vector3.up);

        Quaternion targetRotation = rotationX * rotationY;

        transform.localRotation = Quaternion.Slerp(transform.localRotation, targetRotation, smooth * Time.deltaTime);
    }

    public void ToggleFlashlight()
    {
        isFlashLightOn = !isFlashLightOn;

        flashlight.enabled = isFlashLightOn;

        if (audioSource != null && clickSound != null)
        {
            audioSource.PlayOneShot(clickSound);
        }
    }
}