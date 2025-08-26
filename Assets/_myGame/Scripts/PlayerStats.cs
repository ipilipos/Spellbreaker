using System;
using System.Collections;
using UnityEngine;

public class PlayerStats : MonoBehaviour
{
    [Header("Health Settings")]
    [SerializeField] private int maxHealth = 100;
    [SerializeField] private int currentHealth;
    [SerializeField] private bool invulnerable = false;
    [SerializeField] private float invulnerabilityTime = 1f; // Damage immunity after taking damage

    [Header("Damage Settings")]
    [SerializeField] private int baseDamage = 30;
    [SerializeField] private float damageMultiplier = 1f;

    [Header("Visual Feedback")]
    [SerializeField] private bool showDamageNumbers = true;
    [SerializeField] private Color damageColor = Color.red;
    [SerializeField] private bool flashOnDamage = true;
    [SerializeField] private Color flashColor = Color.red;
    [SerializeField] private float flashDuration = 0.15f;

    [Header("Audio")]
    [SerializeField] private AudioClip hurtSound;
    [SerializeField] private AudioClip deathSound;
    [SerializeField] private AudioClip healSound;

    [Header("Recovery")]
    [SerializeField] private bool canRegenerate = false;
    [SerializeField] private float regenerationRate = 5f; // Health per second
    [SerializeField] private float regenerationDelay = 3f; // Delay after taking damage

    // Events
    public event Action OnDeath;
    public event Action OnRevive;
    public event Action<int> OnHealthChanged;
    public event Action<int> OnDamageTaken;
    public event Action<int> OnHealed;

    // Components
    private SpriteRenderer spriteRenderer;
    private AudioSource audioSource;
    private CharacterAnimator characterAnimator;
    private PlayerMovement playerMovement;
    private Color originalColor;

    // State
    private bool isDead = false;
    private bool isInvulnerable = false;
    private float lastDamageTime;

    void Awake()
    {
        // Initialize health
        currentHealth = maxHealth;

        // Get components
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        audioSource = GetComponent<AudioSource>();
        characterAnimator = GetComponent<CharacterAnimator>();
        playerMovement = GetComponent<PlayerMovement>();

        // Store original color
        if (spriteRenderer != null)
        {
            originalColor = spriteRenderer.color;
        }

        // Add AudioSource if not present
        if (audioSource == null && (hurtSound != null || deathSound != null || healSound != null))
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
        }
    }

    void Start()
    {
        // Trigger initial health event
        OnHealthChanged?.Invoke(currentHealth);

        // Start regeneration if enabled
        if (canRegenerate)
        {
            StartCoroutine(RegenerationCoroutine());
        }
    }

    void Update()
    {
        // Handle invulnerability timer
        if (isInvulnerable && Time.time - lastDamageTime >= invulnerabilityTime)
        {
            isInvulnerable = false;
        }
    }

    public void TakeDamage(int damageAmount)
    {
        if (isDead || invulnerable || isInvulnerable) return;

        // Calculate actual damage
        int actualDamage = Mathf.Max(1, damageAmount); // Minimum 1 damage
        currentHealth -= actualDamage;

        // Clamp health to minimum 0
        currentHealth = Mathf.Max(0, currentHealth);

        // Set invulnerability period
        isInvulnerable = true;
        lastDamageTime = Time.time;

        // Trigger events
        OnDamageTaken?.Invoke(actualDamage);
        OnHealthChanged?.Invoke(currentHealth);

        // Visual and audio feedback
        if (showDamageNumbers)
        {
            ShowDamageNumber(actualDamage);
        }

        if (flashOnDamage && !isDead)
        {
            StartCoroutine(DamageFlash());
        }

        if (hurtSound != null && audioSource != null && !isDead)
        {
            audioSource.PlayOneShot(hurtSound);
        }

        // Check for death
        if (currentHealth <= 0 && !isDead)
        {
            Die();
        }
    }

    public void Heal(int healAmount)
    {
        if (isDead) return;

        int actualHeal = healAmount;
        int oldHealth = currentHealth;

        currentHealth += actualHeal;
        currentHealth = Mathf.Min(currentHealth, maxHealth);

        int actualHealed = currentHealth - oldHealth;

        if (actualHealed > 0)
        {
            OnHealed?.Invoke(actualHealed);
            OnHealthChanged?.Invoke(currentHealth);

            // Show heal numbers (green)
            if (showDamageNumbers)
            {
                ShowDamageNumber(actualHealed, Color.green, "+");
            }

            // Play heal sound
            if (healSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(healSound);
            }
        }
    }

    void Die()
    {
        if (isDead) return;

        isDead = true;

        // Play death sound
        if (deathSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(deathSound);
        }

        // Play death animation
        if (characterAnimator != null)
        {
            characterAnimator.ChangeAnimation("Death");
        }

        // Disable player movement
        if (playerMovement != null)
        {
            playerMovement.SetMovementEnabled(false);
        }

        // Trigger death event
        OnDeath?.Invoke();

        // Optional: Add game over logic here or let GameManager handle it
        Debug.Log("Player has died!");
    }

    public void Revive(bool fullHealth = true)
    {
        if (!isDead) return;

        isDead = false;
        isInvulnerable = false;

        // Restore health
        if (fullHealth)
        {
            currentHealth = maxHealth;
        }
        else
        {
            currentHealth = Mathf.Max(1, maxHealth / 2); // Revive with half health
        }

        // Re-enable movement
        if (playerMovement != null)
        {
            playerMovement.SetMovementEnabled(true);
        }

        // Reset color
        if (spriteRenderer != null)
        {
            spriteRenderer.color = originalColor;
        }

        // Play idle animation
        if (characterAnimator != null)
        {
            characterAnimator.ChangeAnimation("Idle");
        }

        OnRevive?.Invoke();
        OnHealthChanged?.Invoke(currentHealth);

        Debug.Log("Player revived!");
    }

    IEnumerator RegenerationCoroutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(1f);

            // Only regenerate if not at full health, not dead, and enough time has passed since damage
            if (!isDead && currentHealth < maxHealth &&
                Time.time - lastDamageTime >= regenerationDelay)
            {
                int regenAmount = Mathf.RoundToInt(regenerationRate);
                Heal(regenAmount);
            }
        }
    }

    IEnumerator DamageFlash()
    {
        if (spriteRenderer == null) yield break;

        Color currentColor = spriteRenderer.color;

        // Flash to damage color
        spriteRenderer.color = flashColor;
        yield return new WaitForSeconds(flashDuration);

        // Return to original color if not dead
        if (!isDead)
        {
            spriteRenderer.color = originalColor;
        }
    }

    void ShowDamageNumber(int amount, Color color = default, string prefix = "-")
    {
        if (color == default) color = damageColor;

        // Create floating damage text
        GameObject damageText = new GameObject("PlayerDamageText");
        damageText.transform.position = transform.position + Vector3.up * 1.5f;

        // Add text component
        TextMesh textMesh = damageText.AddComponent<TextMesh>();
        textMesh.text = prefix + amount.ToString();
        textMesh.fontSize = 25;
        textMesh.color = color;
        textMesh.anchor = TextAnchor.MiddleCenter;

        // Make it face the camera
        damageText.transform.LookAt(Camera.main.transform);
        damageText.transform.Rotate(0, 180, 0);

        // Animate the text
        StartCoroutine(AnimatePlayerDamageText(damageText));
    }

    IEnumerator AnimatePlayerDamageText(GameObject damageText)
    {
        Vector3 startPos = damageText.transform.position;
        Vector3 endPos = startPos + Vector3.up * 1.5f;
        float duration = 1.2f;
        float elapsed = 0f;

        TextMesh textMesh = damageText.GetComponent<TextMesh>();
        Color startColor = textMesh.color;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / duration;

            // Move up and slightly random direction
            Vector3 randomOffset = new Vector3(
                Mathf.Sin(progress * Mathf.PI) * 0.3f,
                0,
                0
            );
            damageText.transform.position = Vector3.Lerp(startPos, endPos, progress) + randomOffset;

            // Fade out
            Color newColor = startColor;
            newColor.a = 1f - progress;
            textMesh.color = newColor;

            yield return null;
        }

        Destroy(damageText);
    }

    // Public getters and setters
    public int GetCurrentHealth() => currentHealth;
    public int GetMaxHealth() => maxHealth;
    public float GetHealthPercentage() => (float)currentHealth / maxHealth;
    public bool IsDead() => isDead;
    public bool IsInvulnerable() => invulnerable || isInvulnerable;
    public int GetDamage() => Mathf.RoundToInt(baseDamage * damageMultiplier);

    public void SetMaxHealth(int newMaxHealth)
    {
        int healthDifference = newMaxHealth - maxHealth;
        maxHealth = newMaxHealth;

        // Adjust current health proportionally or add the difference
        currentHealth = Mathf.Min(currentHealth + healthDifference, maxHealth);
        currentHealth = Mathf.Max(0, currentHealth);

        OnHealthChanged?.Invoke(currentHealth);
    }

    public void SetBaseDamage(int newDamage) => baseDamage = newDamage;
    public void SetDamageMultiplier(float multiplier) => damageMultiplier = multiplier;
    public void SetInvulnerable(bool invulnerable) => this.invulnerable = invulnerable;
    public void SetRegenerationRate(float rate) => regenerationRate = rate;

    // Full heal
    public void FullHeal()
    {
        if (isDead) return;

        currentHealth = maxHealth;
        OnHealthChanged?.Invoke(currentHealth);

        if (showDamageNumbers)
        {
            ShowDamageNumber(maxHealth, Color.cyan, "FULL ");
        }
    }

    // Reset player to initial state
    public void ResetPlayer()
    {
        isDead = false;
        isInvulnerable = false;
        currentHealth = maxHealth;
        lastDamageTime = 0f;

        if (spriteRenderer != null)
        {
            spriteRenderer.color = originalColor;
        }

        if (playerMovement != null)
        {
            playerMovement.SetMovementEnabled(true);
        }

        OnHealthChanged?.Invoke(currentHealth);
    }

    // For debugging
    void OnValidate()
    {
        // Ensure values are valid in editor
        if (maxHealth < 1) maxHealth = 1;
        if (baseDamage < 0) baseDamage = 0;
        if (damageMultiplier < 0) damageMultiplier = 0;
        if (invulnerabilityTime < 0) invulnerabilityTime = 0;
    }
}