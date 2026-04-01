using UnityEngine;

/// <summary>
/// CameraController handles the camera following the player in a smooth, side-scroller perspective.
/// </summary>
public class CameraController : MonoBehaviour
{
    [SerializeField] private Transform playerTransform;
    [SerializeField] private float followSpeed = 5f;
    [SerializeField] private Vector3 cameraOffset = new Vector3(0, 2, -10);
    
    private void LateUpdate()
    {
        if (playerTransform == null) return;
        
        // Calculate target position
        Vector3 targetPosition = playerTransform.position + cameraOffset;
        
        // Smoothly move camera towards target
        transform.position = Vector3.Lerp(transform.position, targetPosition, followSpeed * Time.deltaTime);
        
        // Look at player
        transform.LookAt(playerTransform.position + Vector3.up * 1f);
    }
}