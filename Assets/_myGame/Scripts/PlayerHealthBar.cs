using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayerHealthBar : MonoBehaviour
{
    [Header("Health Bar Components")]
    [SerializeField] private Slider healthSlider;
    [SerializeField] private Image healthFillImage;
    [SerializeField] private Image healthBackgroundImage;

    [Header("Health Text")]
    [SerializeField] private TextMeshProUGUI healthText;
    [SerializeField] private bool showHealthNumbers = true;
    [SerializeField] private string healthTextFormat = "{0}/{1}"; // Current/Max format

    [Header("Visual Settings")]
    [SerializeField] private bool useColorGradient = true;
    [SerializeField] private Color highHealthColor = Color.green;
    [SerializeField] private Color mediumHealthColor = Color.yellow;
    [SerializeField] private Color lowHealthColor = Color.red;
    [SerializeField] private float lowHealthThreshold = 0.25f; // 25% of max health
    [SerializeField] private float mediumHealthThreshold = 0.6f; // 60% of max health

    [Header("Animation Settings")]
    [SerializeField] private bool animateChanges = true;
    [SerializeField] private float animationSpeed = 2f;
    [SerializeField] private bool pulseLowHealth = true;
    [SerializeField] private float pulseSpeed = 2f;

    [Header("Damage Flash")]
    [SerializeField] private bool flashOnDamage = true;
    [SerializeField] private Color damageFlashColor = Color.white;
    [SerializeField] private float flashDuration = 0.2f;

    // Private variables
    private PlayerStats playerStats;
    private float targetHealthPercentage = 1f;
    private bool isFlashing = false;
    private Coroutine pulseCoroutine;

    void Start()
    {
        // Find the player and get PlayerStats component
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
        {
            player = GameObject.Find("Lightbreaker");
        }

        if (player != null)
        {
            playerStats = player.GetComponent<PlayerStats>();
            if (playerStats != null)
            {
                // Subscribe to health events
                playerStats.OnHealthChanged += UpdateHealthBar;
                playerStats.OnDamageTaken += OnDamageTaken;
                playerStats.OnHealed += OnHealed;
                playerStats.OnDeath += OnPlayerDeath;
                playerStats.OnRevive += OnPlayerRevive;

                // Initialize health bar
                InitializeHealthBar();
            }
            else
            {
                Debug.LogError("PlayerStats component not found on player!");
            }
        }
        else
        {
            Debug.LogError("Player GameObject not found! Make sure it has 'Player' tag or is named 'Lightbreaker'");
        }

        // Initialize UI components if not set in inspector
        if (healthSlider == null)
            healthSlider = GetComponent<Slider>();

        if (healthFillImage == null && healthSlider != null)
            healthFillImage = healthSlider.fillRect.GetComponent<Image>();

        if (healthText == null)
            healthText = GetComponentInChildren<TextMeshProUGUI>();
    }

    void InitializeHealthBar()
    {
        if (playerStats == null) return;

        // Set initial values
        int currentHealth = playerStats.GetCurrentHealth();
        int maxHealth = playerStats.GetMaxHealth();

        targetHealthPercentage = playerStats.GetHealthPercentage();

        if (healthSlider != null)
        {
            healthSlider.maxValue = 1f;
            healthSlider.value = targetHealthPercentage;
        }

        UpdateHealthDisplay(currentHealth, maxHealth);
        UpdateHealthColor(targetHealthPercentage);
    }

    void UpdateHealthBar(int currentHealth)
    {
        if (playerStats == null) return;

        int maxHealth = playerStats.GetMaxHealth();
        targetHealthPercentage = playerStats.GetHealthPercentage();

        UpdateHealthDisplay(currentHealth, maxHealth);

        if (animateChanges)
        {
            StartCoroutine(AnimateHealthChange());
        }
        else
        {
            if (healthSlider != null)
            {
                healthSlider.value = targetHealthPercentage;
            }
            UpdateHealthColor(targetHealthPercentage);
        }

        // Handle low health pulsing
        HandleLowHealthPulse();
    }

    void UpdateHealthDisplay(int currentHealth, int maxHealth)
    {
        // Update health text
        if (healthText != null && showHealthNumbers)
        {
            healthText.text = string.Format(healthTextFormat, currentHealth, maxHealth);
        }
    }

    void UpdateHealthColor(float healthPercentage)
    {
        if (!useColorGradient || healthFillImage == null) return;

        Color targetColor;

        if (healthPercentage <= lowHealthThreshold)
        {
            targetColor = lowHealthColor;
        }
        else if (healthPercentage <= mediumHealthThreshold)
        {
            // Interpolate between low and medium health colors
            float t = (healthPercentage - lowHealthThreshold) / (mediumHealthThreshold - lowHealthThreshold);
            targetColor = Color.Lerp(lowHealthColor, mediumHealthColor, t);
        }
        else
        {
            // Interpolate between medium and high health colors
            float t = (healthPercentage - mediumHealthThreshold) / (1f - mediumHealthThreshold);
            targetColor = Color.Lerp(mediumHealthColor, highHealthColor, t);
        }

        healthFillImage.color = targetColor;
    }

    IEnumerator AnimateHealthChange()
    {
        if (healthSlider == null) yield break;

        float startValue = healthSlider.value;
        float elapsedTime = 0f;
        float duration = Mathf.Abs(targetHealthPercentage - startValue) / animationSpeed;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / duration;

            float currentValue = Mathf.Lerp(startValue, targetHealthPercentage, progress);
            healthSlider.value = currentValue;
            UpdateHealthColor(currentValue);

            yield return null;
        }

        healthSlider.value = targetHealthPercentage;
        UpdateHealthColor(targetHealthPercentage);
    }

    void OnDamageTaken(int damage)
    {
        // Flash effect when taking damage
        if (flashOnDamage && !isFlashing)
        {
            StartCoroutine(DamageFlash());
        }
    }

    void OnHealed(int healAmount)
    {
        // Could add a heal effect here if desired
        // For now, just the color change from UpdateHealthBar is sufficient
    }

    void OnPlayerDeath()
    {
        // Stop pulsing on death
        if (pulseCoroutine != null)
        {
            StopCoroutine(pulseCoroutine);
            pulseCoroutine = null;
        }

        // Optional: Fade out health bar or show death state
        if (healthFillImage != null)
        {
            healthFillImage.color = Color.red;
        }
    }

    void OnPlayerRevive()
    {
        // Resume normal health bar functionality
        if (playerStats != null)
        {
            UpdateHealthBar(playerStats.GetCurrentHealth());
        }
    }

    IEnumerator DamageFlash()
    {
        if (healthFillImage == null) yield break;

        isFlashing = true;
        Color originalColor = healthFillImage.color;

        // Flash to damage color
        healthFillImage.color = damageFlashColor;
        yield return new WaitForSeconds(flashDuration);

        // Return to original color
        healthFillImage.color = originalColor;
        isFlashing = false;
    }

    void HandleLowHealthPulse()
    {
        if (!pulseLowHealth) return;

        bool shouldPulse = targetHealthPercentage <= lowHealthThreshold && !playerStats.IsDead();

        if (shouldPulse && pulseCoroutine == null)
        {
            pulseCoroutine = StartCoroutine(PulseHealthBar());
        }
        else if (!shouldPulse && pulseCoroutine != null)
        {
            StopCoroutine(pulseCoroutine);
            pulseCoroutine = null;

            // Reset scale
            if (healthSlider != null)
            {
                healthSlider.transform.localScale = Vector3.one;
            }
        }
    }

    IEnumerator PulseHealthBar()
    {
        Vector3 originalScale = healthSlider.transform.localScale;
        Vector3 targetScale = originalScale * 1.1f;

        while (true)
        {
            // Pulse up
            float elapsedTime = 0f;
            float pulseDuration = 1f / pulseSpeed;

            while (elapsedTime < pulseDuration)
            {
                elapsedTime += Time.deltaTime;
                float progress = elapsedTime / pulseDuration;
                healthSlider.transform.localScale = Vector3.Lerp(originalScale, targetScale, Mathf.Sin(progress * Mathf.PI));
                yield return null;
            }

            yield return new WaitForSeconds(0.1f);
        }
    }

    // Public methods for external control
    public void SetHealthBarVisibility(bool visible)
    {
        gameObject.SetActive(visible);
    }

    public void SetHealthTextFormat(string newFormat)
    {
        healthTextFormat = newFormat;
        if (playerStats != null)
        {
            UpdateHealthDisplay(playerStats.GetCurrentHealth(), playerStats.GetMaxHealth());
        }
    }

    public void SetColors(Color high, Color medium, Color low)
    {
        highHealthColor = high;
        mediumHealthColor = medium;
        lowHealthColor = low;

        if (playerStats != null)
        {
            UpdateHealthColor(playerStats.GetHealthPercentage());
        }
    }

    // Update method for smooth interpolation (if animateChanges is false)
    void Update()
    {
        if (!animateChanges && healthSlider != null && playerStats != null)
        {
            float currentValue = healthSlider.value;
            float targetValue = playerStats.GetHealthPercentage();

            if (Mathf.Abs(currentValue - targetValue) > 0.01f)
            {
                healthSlider.value = Mathf.Lerp(currentValue, targetValue, Time.deltaTime * animationSpeed);
                UpdateHealthColor(healthSlider.value);
            }
        }
    }

    void OnDestroy()
    {
        // Unsubscribe from events to prevent memory leaks
        if (playerStats != null)
        {
            playerStats.OnHealthChanged -= UpdateHealthBar;
            playerStats.OnDamageTaken -= OnDamageTaken;
            playerStats.OnHealed -= OnHealed;
            playerStats.OnDeath -= OnPlayerDeath;
            playerStats.OnRevive -= OnPlayerRevive;
        }
    }

    void OnValidate()
    {
        // Ensure thresholds are valid in editor
        lowHealthThreshold = Mathf.Clamp01(lowHealthThreshold);
        mediumHealthThreshold = Mathf.Clamp01(mediumHealthThreshold);

        if (mediumHealthThreshold <= lowHealthThreshold)
        {
            mediumHealthThreshold = lowHealthThreshold + 0.1f;
        }
    }
}