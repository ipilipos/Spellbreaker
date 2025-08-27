using System;
using UnityEngine;
using System.Collections;

public class PlayerLevelSystem : MonoBehaviour
{
    [Header("Level Settings")]
    [SerializeField] private int currentLevel = 1;
    [SerializeField] private int currentExperience = 0;
    [SerializeField] private int maxLevel = 100;

    [Header("Experience Curve Settings")]
    [SerializeField] private int baseExperienceRequired = 100; // XP required for level 2
    [SerializeField] private float experienceMultiplier = 1.2f; // How much XP requirements increase per level
    [SerializeField] private AnimationCurve experienceCurve; // Optional: custom curve for XP requirements

    [Header("Level Up Rewards")]
    [SerializeField] private int healthIncreasePerLevel = 10;
    [SerializeField] private int damageIncreasePerLevel = 2;
    [SerializeField] private float speedIncreasePerLevel = 0.5f;
    [SerializeField] private bool enableStatBonuses = true;

    [Header("Visual & Audio Feedback")]
    [SerializeField] private bool showLevelUpEffect = true;
    [SerializeField] private Color levelUpEffectColor = Color.yellow;
    [SerializeField] private float levelUpEffectDuration = 2f;
    [SerializeField] private AudioClip levelUpSound;
    [SerializeField] private AudioClip experienceGainSound;

    [Header("Experience Display")]
    [SerializeField] private bool showExperienceNumbers = true;
    [SerializeField] private Color experienceTextColor = Color.cyan;

    // Events
    public event Action<int> OnLevelUp; // Passes new level
    public event Action<int> OnExperienceGained; // Passes XP gained
    public event Action<int, int, int> OnExperienceChanged; // Current XP, Required XP, Level

    // Components
    private PlayerStats playerStats;
    private PlayerMovement playerMovement;
    private AudioSource audioSource;

    // Calculated values
    private int experienceRequiredForNextLevel;

    void Awake()
    {
        // Get components
        playerStats = GetComponent<PlayerStats>();
        playerMovement = GetComponent<PlayerMovement>();
        audioSource = GetComponent<AudioSource>();

        // Add AudioSource if not present
        if (audioSource == null && (levelUpSound != null || experienceGainSound != null))
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
        }

        // Initialize experience curve if not set
        if (experienceCurve == null || experienceCurve.keys.Length == 0)
        {
            CreateDefaultExperienceCurve();
        }
    }

    void Start()
    {
        // Calculate initial experience requirement
        experienceRequiredForNextLevel = CalculateExperienceForLevel(currentLevel + 1);

        // Trigger initial UI update
        OnExperienceChanged?.Invoke(currentExperience, experienceRequiredForNextLevel, currentLevel);

        // Subscribe to enemy death events to gain experience
        SubscribeToEnemyDeaths();
    }

    void SubscribeToEnemyDeaths()
    {
        // Find all enemies in scene and subscribe to their death events
        EnemyStats[] allEnemies = FindObjectsByType<EnemyStats>(FindObjectsSortMode.None);
        foreach (EnemyStats enemy in allEnemies)
        {
            enemy.OnDeath += () => OnEnemyKilled(enemy);
        }
    }

    void OnEnemyKilled(EnemyStats enemy)
    {
        // Get experience value from the enemy
        EnemyExperienceProvider expProvider = enemy.GetComponent<EnemyExperienceProvider>();
        int expGained = expProvider != null ? expProvider.GetExperienceValue() : 10; // Default 10 XP

        GainExperience(expGained);
    }

    public void GainExperience(int amount)
    {
        if (currentLevel >= maxLevel) return;

        currentExperience += amount;

        // Show experience gain visual
        if (showExperienceNumbers)
        {
            ShowExperienceNumber(amount);
        }

        // Play experience gain sound
        if (experienceGainSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(experienceGainSound);
        }

        // Trigger experience gained event
        OnExperienceGained?.Invoke(amount);

        // Check for level up
        CheckForLevelUp();

        // Update UI
        OnExperienceChanged?.Invoke(currentExperience, experienceRequiredForNextLevel, currentLevel);
    }

    void CheckForLevelUp()
    {
        while (currentExperience >= experienceRequiredForNextLevel && currentLevel < maxLevel)
        {
            LevelUp();
        }
    }

    void LevelUp()
    {
        // Subtract required experience
        currentExperience -= experienceRequiredForNextLevel;

        // Increase level
        currentLevel++;

        // Calculate new experience requirement
        if (currentLevel < maxLevel)
        {
            experienceRequiredForNextLevel = CalculateExperienceForLevel(currentLevel + 1);
        }

        // Apply stat bonuses
        if (enableStatBonuses)
        {
            ApplyLevelUpBonuses();
        }

        // Visual and audio effects
        if (showLevelUpEffect)
        {
            StartCoroutine(ShowLevelUpEffect());
        }

        if (levelUpSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(levelUpSound);
        }

        // Trigger level up event
        OnLevelUp?.Invoke(currentLevel);

        Debug.Log($"Level Up! Now level {currentLevel}");
    }

    void ApplyLevelUpBonuses()
    {
        if (playerStats != null)
        {
            // Increase max health
            playerStats.SetMaxHealth(playerStats.GetMaxHealth() + healthIncreasePerLevel);

            // Increase damage
            playerStats.SetBaseDamage(playerStats.GetDamage() + damageIncreasePerLevel);
        }

        // Note: Speed increase would need to be implemented in PlayerMovement
        // You could add a public method there to increase speed
    }

    int CalculateExperienceForLevel(int targetLevel)
    {
        if (targetLevel <= 1) return 0;

        // Method 1: Simple exponential scaling
        if (experienceCurve.keys.Length == 0)
        {
            return Mathf.RoundToInt(baseExperienceRequired * Mathf.Pow(experienceMultiplier, targetLevel - 2));
        }

        // Method 2: Using animation curve
        float normalizedLevel = (float)(targetLevel - 1) / (maxLevel - 1);
        float curveValue = experienceCurve.Evaluate(normalizedLevel);
        return Mathf.RoundToInt(baseExperienceRequired * curveValue);
    }

    void CreateDefaultExperienceCurve()
    {
        // Create a smooth exponential curve
        Keyframe[] keys = new Keyframe[5];
        keys[0] = new Keyframe(0f, 1f); // Level 1
        keys[1] = new Keyframe(0.25f, 2f); // 25% through levels
        keys[2] = new Keyframe(0.5f, 5f); // 50% through levels
        keys[3] = new Keyframe(0.75f, 10f); // 75% through levels
        keys[4] = new Keyframe(1f, 20f); // Max level

        experienceCurve = new AnimationCurve(keys);
    }

    IEnumerator ShowLevelUpEffect()
    {
        // Create level up text effect
        GameObject levelUpText = new GameObject("LevelUpText");
        levelUpText.transform.position = transform.position + Vector3.up * 2f;

        TextMesh textMesh = levelUpText.AddComponent<TextMesh>();
        textMesh.text = $"LEVEL UP!\nLevel {currentLevel}";
        textMesh.fontSize = 30;
        textMesh.color = levelUpEffectColor;
        textMesh.anchor = TextAnchor.MiddleCenter;

        // Make it face the camera
        levelUpText.transform.LookAt(Camera.main.transform);
        levelUpText.transform.Rotate(0, 180, 0);

        // Animate the text
        Vector3 startPos = levelUpText.transform.position;
        Vector3 endPos = startPos + Vector3.up * 2f;
        float elapsed = 0f;
        Color startColor = textMesh.color;

        while (elapsed < levelUpEffectDuration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / levelUpEffectDuration;

            // Move up and scale
            levelUpText.transform.position = Vector3.Lerp(startPos, endPos, progress);
            float scale = 1f + Mathf.Sin(progress * Mathf.PI) * 0.5f;
            levelUpText.transform.localScale = Vector3.one * scale;

            // Fade out in the last half
            if (progress > 0.5f)
            {
                Color newColor = startColor;
                newColor.a = 1f - ((progress - 0.5f) * 2f);
                textMesh.color = newColor;
            }

            yield return null;
        }

        Destroy(levelUpText);
    }

    void ShowExperienceNumber(int expAmount)
    {
        GameObject expText = new GameObject("ExperienceText");
        expText.transform.position = transform.position + Vector3.up * 1f;

        TextMesh textMesh = expText.AddComponent<TextMesh>();
        textMesh.text = $"+{expAmount} XP";
        textMesh.fontSize = 20;
        textMesh.color = experienceTextColor;
        textMesh.anchor = TextAnchor.MiddleCenter;

        textMesh.transform.LookAt(Camera.main.transform);
        textMesh.transform.Rotate(0, 180, 0);

        StartCoroutine(AnimateExperienceText(expText));
    }

    IEnumerator AnimateExperienceText(GameObject expText)
    {
        Vector3 startPos = expText.transform.position;
        Vector3 endPos = startPos + Vector3.up * 1f;
        float duration = 1f;
        float elapsed = 0f;

        TextMesh textMesh = expText.GetComponent<TextMesh>();
        Color startColor = textMesh.color;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / duration;

            expText.transform.position = Vector3.Lerp(startPos, endPos, progress);

            Color newColor = startColor;
            newColor.a = 1f - progress;
            textMesh.color = newColor;

            yield return null;
        }

        Destroy(expText);
    }

    // Public getters
    public int GetCurrentLevel() => currentLevel;
    public int GetCurrentExperience() => currentExperience;
    public int GetExperienceRequiredForNextLevel() => experienceRequiredForNextLevel;
    public int GetExperienceForCurrentLevel() => currentExperience;
    public float GetExperiencePercentage() => (float)currentExperience / experienceRequiredForNextLevel;
    public bool IsMaxLevel() => currentLevel >= maxLevel;

    // Public setters for debugging/cheats
    public void SetLevel(int level)
    {
        if (level < 1 || level > maxLevel) return;

        currentLevel = level;
        currentExperience = 0;
        experienceRequiredForNextLevel = CalculateExperienceForLevel(currentLevel + 1);

        OnExperienceChanged?.Invoke(currentExperience, experienceRequiredForNextLevel, currentLevel);
    }

    public void AddExperienceCheat(int amount) => GainExperience(amount);

    // Save/Load methods (you can expand these for persistence)
    [System.Serializable]
    public class LevelData
    {
        public int level;
        public int experience;
    }

    public LevelData GetLevelData()
    {
        return new LevelData { level = currentLevel, experience = currentExperience };
    }

    public void LoadLevelData(LevelData data)
    {
        currentLevel = data.level;
        currentExperience = data.experience;
        experienceRequiredForNextLevel = CalculateExperienceForLevel(currentLevel + 1);

        OnExperienceChanged?.Invoke(currentExperience, experienceRequiredForNextLevel, currentLevel);
    }

    void OnValidate()
    {
        // Ensure values are valid in editor
        if (maxLevel < 1) maxLevel = 1;
        if (currentLevel < 1) currentLevel = 1;
        if (currentLevel > maxLevel) currentLevel = maxLevel;
        if (baseExperienceRequired < 1) baseExperienceRequired = 1;
        if (experienceMultiplier < 1f) experienceMultiplier = 1f;
    }
}