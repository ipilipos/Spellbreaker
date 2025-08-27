using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayerLevelUI : MonoBehaviour
{
    [Header("Experience Bar Components")]
    [SerializeField] private Slider experienceSlider;
    [SerializeField] private Image experienceFillImage;
    [SerializeField] private Image experienceBackgroundImage;

    [Header("Level Text")]
    [SerializeField] private TextMeshProUGUI levelText;
    [SerializeField] private TextMeshProUGUI experienceText;
    [SerializeField] private bool showExperienceNumbers = true;
    [SerializeField] private string experienceTextFormat = "{0} / {1}"; // Current/Required format
    [SerializeField] private string levelTextFormat = "LV {0}"; // Level format

    [Header("Visual Settings")]
    [SerializeField] private bool useColorGradient = true;
    [SerializeField] private Color lowLevelColor = Color.blue;
    [SerializeField] private Color midLevelColor = Color.green;
    [SerializeField] private Color highLevelColor = Color.yellow;
    [SerializeField] private Color maxLevelColor = Color.red;

    [Header("Animation Settings")]
    [SerializeField] private bool animateExperienceBar = true;
    [SerializeField] private float animationSpeed = 2f;
    [SerializeField] private bool flashOnLevelUp = true;
    [SerializeField] private Color levelUpFlashColor = Color.yellow;
    [SerializeField] private float flashDuration = 0.5f;

    [Header("Level Up Popup")]
    [SerializeField] private GameObject levelUpPopup; // Optional popup panel
    [SerializeField] private TextMeshProUGUI levelUpPopupText;
    [SerializeField] private float popupDuration = 3f;
    [SerializeField] private bool autoHidePopup = true;

    // Private variables
    private PlayerLevelSystem playerLevelSystem;
    private float targetExperiencePercentage = 0f;
    private bool isAnimating = false;
    private Coroutine flashCoroutine;
    private Coroutine popupCoroutine;

    void Start()
    {
        // Find the player and get PlayerLevelSystem component
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
        {
            player = GameObject.Find("Lightbreaker");
        }

        if (player != null)
        {
            playerLevelSystem = player.GetComponent<PlayerLevelSystem>();
            if (playerLevelSystem != null)
            {
                // Subscribe to level system events
                playerLevelSystem.OnExperienceChanged += UpdateExperienceBar;
                playerLevelSystem.OnLevelUp += OnLevelUp;
                playerLevelSystem.OnExperienceGained += OnExperienceGained;

                // Initialize UI
                InitializeLevelUI();
            }
            else
            {
                Debug.LogError("PlayerLevelSystem component not found on player!");
            }
        }
        else
        {
            Debug.LogError("Player GameObject not found! Make sure it has 'Player' tag or is named 'Lightbreaker'");
        }

        // Initialize UI components if not set in inspector
        if (experienceSlider == null)
            experienceSlider = GetComponent<Slider>();

        if (experienceFillImage == null && experienceSlider != null)
            experienceFillImage = experienceSlider.fillRect.GetComponent<Image>();

        if (levelText == null)
            levelText = GetComponentInChildren<TextMeshProUGUI>();

        // Hide level up popup initially
        if (levelUpPopup != null)
        {
            levelUpPopup.SetActive(false);
        }
    }

    void InitializeLevelUI()
    {
        if (playerLevelSystem == null) return;

        // Set initial values
        int currentLevel = playerLevelSystem.GetCurrentLevel();
        int currentExp = playerLevelSystem.GetCurrentExperience();
        int requiredExp = playerLevelSystem.GetExperienceRequiredForNextLevel();

        targetExperiencePercentage = playerLevelSystem.GetExperiencePercentage();

        if (experienceSlider != null)
        {
            experienceSlider.maxValue = 1f;
            experienceSlider.value = targetExperiencePercentage;
        }

        UpdateExperienceDisplay(currentExp, requiredExp, currentLevel);
        UpdateLevelColor(currentLevel);
    }

    void UpdateExperienceBar(int currentExp, int requiredExp, int currentLevel)
    {
        if (playerLevelSystem == null) return;

        targetExperiencePercentage = playerLevelSystem.GetExperiencePercentage();

        UpdateExperienceDisplay(currentExp, requiredExp, currentLevel);

        if (animateExperienceBar)
        {
            StartCoroutine(AnimateExperienceBar());
        }
        else
        {
            if (experienceSlider != null)
            {
                experienceSlider.value = targetExperiencePercentage;
            }
            UpdateLevelColor(currentLevel);
        }
    }

    void UpdateExperienceDisplay(int currentExp, int requiredExp, int currentLevel)
    {
        // Update level text
        if (levelText != null)
        {
            levelText.text = string.Format(levelTextFormat, currentLevel);
        }

        // Update experience text
        if (experienceText != null && showExperienceNumbers)
        {
            if (playerLevelSystem.IsMaxLevel())
            {
                experienceText.text = "MAX LEVEL";
            }
            else
            {
                experienceText.text = string.Format(experienceTextFormat, currentExp, requiredExp);
            }
        }
    }

    void UpdateLevelColor(int currentLevel)
    {
        if (!useColorGradient || experienceFillImage == null || playerLevelSystem == null) return;

        Color targetColor;

        if (playerLevelSystem.IsMaxLevel())
        {
            targetColor = maxLevelColor;
        }
        else
        {
            // Calculate color based on level progression
            float levelProgress = (float)currentLevel / 100f; // Assuming max level 100, adjust as needed

            if (levelProgress <= 0.33f)
            {
                // Low to mid level
                float t = levelProgress / 0.33f;
                targetColor = Color.Lerp(lowLevelColor, midLevelColor, t);
            }
            else if (levelProgress <= 0.66f)
            {
                // Mid to high level
                float t = (levelProgress - 0.33f) / 0.33f;
                targetColor = Color.Lerp(midLevelColor, highLevelColor, t);
            }
            else
            {
                // High to max level
                float t = (levelProgress - 0.66f) / 0.34f;
                targetColor = Color.Lerp(highLevelColor, maxLevelColor, t);
            }
        }

        experienceFillImage.color = targetColor;
    }

    IEnumerator AnimateExperienceBar()
    {
        if (experienceSlider == null || isAnimating) yield break;

        isAnimating = true;
        float startValue = experienceSlider.value;
        float elapsedTime = 0f;
        float duration = Mathf.Abs(targetExperiencePercentage - startValue) / animationSpeed;

        // Minimum animation time
        duration = Mathf.Max(duration, 0.1f);

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / duration;

            float currentValue = Mathf.Lerp(startValue, targetExperiencePercentage, progress);
            experienceSlider.value = currentValue;

            yield return null;
        }

        experienceSlider.value = targetExperiencePercentage;
        UpdateLevelColor(playerLevelSystem.GetCurrentLevel());
        isAnimating = false;
    }

    void OnLevelUp(int newLevel)
    {
        // Flash effect when leveling up
        if (flashOnLevelUp)
        {
            if (flashCoroutine != null)
            {
                StopCoroutine(flashCoroutine);
            }
            flashCoroutine = StartCoroutine(FlashExperienceBar());
        }

        // Show level up popup
        ShowLevelUpPopup(newLevel);
    }

    void OnExperienceGained(int expGained)
    {
        // Optional: Add visual feedback for experience gained
        // You could add a subtle glow effect or number popup here
    }

    IEnumerator FlashExperienceBar()
    {
        if (experienceFillImage == null) yield break;

        Color originalColor = experienceFillImage.color;
        float elapsed = 0f;

        while (elapsed < flashDuration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / flashDuration;

            // Flash between original color and flash color
            Color flashColor = Color.Lerp(originalColor, levelUpFlashColor, Mathf.Sin(progress * Mathf.PI * 4));
            experienceFillImage.color = flashColor;

            yield return null;
        }

        experienceFillImage.color = originalColor;
        UpdateLevelColor(playerLevelSystem.GetCurrentLevel());
    }

    void ShowLevelUpPopup(int newLevel)
    {
        if (levelUpPopup == null) return;

        // Stop any existing popup coroutine
        if (popupCoroutine != null)
        {
            StopCoroutine(popupCoroutine);
        }

        // Update popup text
        if (levelUpPopupText != null)
        {
            levelUpPopupText.text = $"LEVEL UP!\nLevel {newLevel}";
        }

        // Show popup
        levelUpPopup.SetActive(true);

        // Auto-hide popup if enabled
        if (autoHidePopup)
        {
            popupCoroutine = StartCoroutine(HidePopupAfterDelay());
        }
    }

    IEnumerator HidePopupAfterDelay()
    {
        yield return new WaitForSeconds(popupDuration);

        if (levelUpPopup != null)
        {
            levelUpPopup.SetActive(false);
        }
    }

    // Public methods for external control
    public void HideLevelUpPopup()
    {
        if (levelUpPopup != null)
        {
            levelUpPopup.SetActive(false);
        }

        if (popupCoroutine != null)
        {
            StopCoroutine(popupCoroutine);
            popupCoroutine = null;
        }
    }

    public void SetExperienceBarVisibility(bool visible)
    {
        gameObject.SetActive(visible);
    }

    public void SetExperienceTextFormat(string newFormat)
    {
        experienceTextFormat = newFormat;
        if (playerLevelSystem != null)
        {
            UpdateExperienceDisplay(
                playerLevelSystem.GetCurrentExperience(),
                playerLevelSystem.GetExperienceRequiredForNextLevel(),
                playerLevelSystem.GetCurrentLevel()
            );
        }
    }

    public void SetColors(Color low, Color mid, Color high, Color max)
    {
        lowLevelColor = low;
        midLevelColor = mid;
        highLevelColor = high;
        maxLevelColor = max;

        if (playerLevelSystem != null)
        {
            UpdateLevelColor(playerLevelSystem.GetCurrentLevel());
        }
    }

    void OnDestroy()
    {
        // Unsubscribe from events to prevent memory leaks
        if (playerLevelSystem != null)
        {
            playerLevelSystem.OnExperienceChanged -= UpdateExperienceBar;
            playerLevelSystem.OnLevelUp -= OnLevelUp;
            playerLevelSystem.OnExperienceGained -= OnExperienceGained;
        }
    }

    void OnValidate()
    {
        // Ensure animation speed is positive
        if (animationSpeed <= 0) animationSpeed = 1f;
        if (flashDuration <= 0) flashDuration = 0.5f;
        if (popupDuration <= 0) popupDuration = 1f;
    }
}