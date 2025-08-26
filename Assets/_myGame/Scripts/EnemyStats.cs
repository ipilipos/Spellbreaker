using System;
using UnityEngine;
using System.Collections;
public class EnemyStats : MonoBehaviour
{
    [Header("Health Settings")]
    [SerializeField] private int maxHealth = 100;
    [SerializeField] private int currentHealth;
    [SerializeField] private bool invulnerable = false;

    [Header("Damage Settings")]
    [SerializeField] private int damage = 25;
    [SerializeField] private float damageMultiplier = 1f;

    [Header("Visual Feedback")]
    [SerializeField] private bool showDamageNumbers = true;
    [SerializeField] private Color damageColor = Color.red;
    [SerializeField] private bool flashOnDamage = true;
    [SerializeField] private Color flashColor = Color.red;
    [SerializeField] private float flashDuration = 0.1f;

    [Header("Audio")]
    [SerializeField] private AudioClip hurtSound;
    [SerializeField] private AudioClip deathSound;

    // Events
    public event Action OnDeath;
    public event Action<int> OnHealthChanged;
    public event Action<int> OnDamageTaken;

    // Components
    private SpriteRenderer spriteRenderer;
    private AudioSource audioSource;
    private Color originalColor;

    // State
    private bool isDead = false;

    void Awake()
    {
        // Initialize health
        currentHealth = maxHealth;

        // Get components
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        audioSource = GetComponent<AudioSource>();

        // Store original color for damage flash
        if (spriteRenderer != null)
        {
            originalColor = spriteRenderer.color;
        }

        // Add AudioSource if not present
        if (audioSource == null && (hurtSound != null || deathSound != null))
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
        }
    }

    void Start()
    {
        // Trigger initial health event
        OnHealthChanged?.Invoke(currentHealth);
    }

    public void TakeDamage(int damageAmount)
    {
        if (isDead || invulnerable) return;

        // Calculate actual damage
        int actualDamage = Mathf.RoundToInt(damageAmount * damageMultiplier);
        currentHealth -= actualDamage;

        // Clamp health to minimum 0
        currentHealth = Mathf.Max(0, currentHealth);

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

        currentHealth += healAmount;
        currentHealth = Mathf.Min(currentHealth, maxHealth);

        OnHealthChanged?.Invoke(currentHealth);

        // Show heal numbers (green)
        if (showDamageNumbers)
        {
            ShowDamageNumber(healAmount, Color.green, "+");
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

        // Trigger death event
        OnDeath?.Invoke();
    }

    IEnumerator DamageFlash()
    {
        if (spriteRenderer == null) yield break;

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

        // Create floating damage text (you can expand this with a DamageText component)
        GameObject damageText = new GameObject("DamageText");
        damageText.transform.position = transform.position + Vector3.up * 1f;

        // Add text component (basic implementation)
        TextMesh textMesh = damageText.AddComponent<TextMesh>();
        textMesh.text = prefix + amount.ToString();
        textMesh.fontSize = 20;
        textMesh.color = color;
        textMesh.anchor = TextAnchor.MiddleCenter;

        // Simple floating animation
        StartCoroutine(AnimateDamageText(damageText));
    }

    IEnumerator AnimateDamageText(GameObject damageText)
    {
        Vector3 startPos = damageText.transform.position;
        Vector3 endPos = startPos + Vector3.up * 2f;
        float duration = 1f;
        float elapsed = 0f;

        TextMesh textMesh = damageText.GetComponent<TextMesh>();
        Color startColor = textMesh.color;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / duration;

            // Move up
            damageText.transform.position = Vector3.Lerp(startPos, endPos, progress);

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
    public int GetDamage() => Mathf.RoundToInt(damage * damageMultiplier);

    public void SetMaxHealth(int newMaxHealth)
    {
        maxHealth = newMaxHealth;
        currentHealth = Mathf.Min(currentHealth, maxHealth);
        OnHealthChanged?.Invoke(currentHealth);
    }

    public void SetDamage(int newDamage) => damage = newDamage;
    public void SetDamageMultiplier(float multiplier) => damageMultiplier = multiplier;
    public void SetInvulnerable(bool invulnerable) => this.invulnerable = invulnerable;

    // Full heal
    public void FullHeal()
    {
        currentHealth = maxHealth;
        OnHealthChanged?.Invoke(currentHealth);
    }

    // Instant kill
    public void InstantKill()
    {
        if (!isDead)
        {
            currentHealth = 0;
            OnHealthChanged?.Invoke(currentHealth);
            Die();
        }
    }

    // For debugging
    void OnValidate()
    {
        // Ensure health values are valid in editor
        if (maxHealth < 1) maxHealth = 1;
        if (damage < 0) damage = 0;
        if (damageMultiplier < 0) damageMultiplier = 0;
    }

    // Debug display
    void OnDrawGizmos()
    {
        // Show health bar above enemy in editor
        if (Application.isPlaying && !isDead)
        {
            Vector3 pos = transform.position + Vector3.up * 2.5f;
            float healthPercent = GetHealthPercentage();

            // Background
            Gizmos.color = Color.black;
            Gizmos.DrawCube(pos, new Vector3(2f, 0.2f, 0f));

            // Health bar
            Gizmos.color = Color.Lerp(Color.red, Color.green, healthPercent);
            Vector3 healthBarSize = new Vector3(2f * healthPercent, 0.15f, 0f);
            Vector3 healthBarPos = pos + Vector3.left * (1f - healthPercent);
            Gizmos.DrawCube(healthBarPos, healthBarSize);
        }
    }
}