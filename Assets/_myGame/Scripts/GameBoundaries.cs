using UnityEngine;

public class GameBoundaries : MonoBehaviour
{
    [Header("Boundary Settings")]
    [SerializeField] private bool useScreenBounds = true;
    [SerializeField] private float boundaryPadding = 0.5f; // Distance from screen edge

    [Header("Manual Boundary Settings (if not using screen bounds)")]
    [SerializeField] private float leftBoundary = -10f;
    [SerializeField] private float rightBoundary = 10f;
    [SerializeField] private float topBoundary = 8f;
    [SerializeField] private float bottomBoundary = -8f;

    [Header("Visual Debug")]
    [SerializeField] private bool showBoundariesInEditor = true;
    [SerializeField] private Color boundaryColor = Color.red;

    // Calculated boundaries
    private float actualLeftBoundary;
    private float actualRightBoundary;
    private float actualTopBoundary;
    private float actualBottomBoundary;

    // Components
    private Camera mainCamera;
    private BoxCollider2D playerCollider;

    void Start()
    {
        // Get main camera
        mainCamera = Camera.main;
        if (mainCamera == null)
        {
            mainCamera = FindFirstObjectByType<Camera>();
        }

        // Get player's collider for size calculations
        playerCollider = GetComponent<BoxCollider2D>();

        CalculateBoundaries();
    }

    void CalculateBoundaries()
    {
        if (useScreenBounds && mainCamera != null)
        {
            // Calculate screen boundaries in world coordinates
            float cameraHeight = mainCamera.orthographicSize;
            float cameraWidth = cameraHeight * mainCamera.aspect;

            // Calculate player's half-size for proper boundary positioning
            Vector2 playerHalfSize = Vector2.zero;
            if (playerCollider != null)
            {
                playerHalfSize = playerCollider.size * 0.5f;
            }

            // Set boundaries with padding and player size consideration
            actualLeftBoundary = mainCamera.transform.position.x - cameraWidth + boundaryPadding + playerHalfSize.x;
            actualRightBoundary = mainCamera.transform.position.x + cameraWidth - boundaryPadding - playerHalfSize.x;
            actualTopBoundary = mainCamera.transform.position.y + cameraHeight - boundaryPadding - playerHalfSize.y;
            actualBottomBoundary = mainCamera.transform.position.y - cameraHeight + boundaryPadding + playerHalfSize.y;
        }
        else
        {
            // Use manual boundaries
            actualLeftBoundary = leftBoundary;
            actualRightBoundary = rightBoundary;
            actualTopBoundary = topBoundary;
            actualBottomBoundary = bottomBoundary;
        }
    }

    void LateUpdate()
    {
        // Clamp player position within boundaries
        Vector3 pos = transform.position;

        pos.x = Mathf.Clamp(pos.x, actualLeftBoundary, actualRightBoundary);
        pos.y = Mathf.Clamp(pos.y, actualBottomBoundary, actualTopBoundary);

        transform.position = pos;
    }

    // Public methods for other scripts to check boundaries
    public bool IsWithinBoundaries(Vector3 position)
    {
        return position.x >= actualLeftBoundary && position.x <= actualRightBoundary &&
               position.y >= actualBottomBoundary && position.y <= actualTopBoundary;
    }

    public Vector3 ClampToBoundaries(Vector3 position)
    {
        position.x = Mathf.Clamp(position.x, actualLeftBoundary, actualRightBoundary);
        position.y = Mathf.Clamp(position.y, actualBottomBoundary, actualTopBoundary);
        return position;
    }

    public Vector2 GetBoundarySize()
    {
        return new Vector2(actualRightBoundary - actualLeftBoundary, actualTopBoundary - actualBottomBoundary);
    }

    // Recalculate boundaries (useful if camera moves or settings change)
    public void UpdateBoundaries()
    {
        CalculateBoundaries();
    }

    // Debug visualization
    void OnDrawGizmos()
    {
        if (showBoundariesInEditor)
        {
            // Calculate boundaries for editor preview
            if (Application.isPlaying)
            {
                // Use calculated boundaries during play
                DrawBoundaryGizmos(actualLeftBoundary, actualRightBoundary, actualTopBoundary, actualBottomBoundary);
            }
            else
            {
                // Use preview boundaries in editor
                if (useScreenBounds && mainCamera != null)
                {
                    float cameraHeight = mainCamera.orthographicSize;
                    float cameraWidth = cameraHeight * mainCamera.aspect;

                    Vector2 playerHalfSize = Vector2.zero;
                    if (playerCollider != null)
                    {
                        playerHalfSize = playerCollider.size * 0.5f;
                    }

                    float previewLeft = mainCamera.transform.position.x - cameraWidth + boundaryPadding + playerHalfSize.x;
                    float previewRight = mainCamera.transform.position.x + cameraWidth - boundaryPadding - playerHalfSize.x;
                    float previewTop = mainCamera.transform.position.y + cameraHeight - boundaryPadding - playerHalfSize.y;
                    float previewBottom = mainCamera.transform.position.y - cameraHeight + boundaryPadding + playerHalfSize.y;

                    DrawBoundaryGizmos(previewLeft, previewRight, previewTop, previewBottom);
                }
                else
                {
                    DrawBoundaryGizmos(leftBoundary, rightBoundary, topBoundary, bottomBoundary);
                }
            }
        }
    }

    private void DrawBoundaryGizmos(float left, float right, float top, float bottom)
    {
        Gizmos.color = boundaryColor;

        // Draw boundary lines
        Vector3 topLeft = new Vector3(left, top, 0);
        Vector3 topRight = new Vector3(right, top, 0);
        Vector3 bottomLeft = new Vector3(left, bottom, 0);
        Vector3 bottomRight = new Vector3(right, bottom, 0);

        // Draw the boundary rectangle
        Gizmos.DrawLine(topLeft, topRight);      // Top
        Gizmos.DrawLine(topRight, bottomRight);  // Right
        Gizmos.DrawLine(bottomRight, bottomLeft); // Bottom
        Gizmos.DrawLine(bottomLeft, topLeft);    // Left

        // Draw corner markers
        float cornerSize = 0.3f;
        Gizmos.DrawWireCube(topLeft, Vector3.one * cornerSize);
        Gizmos.DrawWireCube(topRight, Vector3.one * cornerSize);
        Gizmos.DrawWireCube(bottomLeft, Vector3.one * cornerSize);
        Gizmos.DrawWireCube(bottomRight, Vector3.one * cornerSize);
    }
}