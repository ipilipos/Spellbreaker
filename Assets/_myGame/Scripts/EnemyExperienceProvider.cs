using UnityEngine;

public class EnemyExperienceProvider : MonoBehaviour
{
    [Header("Experience Settings")]
    [SerializeField] private int baseExperienceValue = 10;
    [SerializeField] private int experienceVariance = 2; // Random variance (+/- this amount)
    [SerializeField] private bool scaleWithEnemyLevel = false;
    [SerializeField] private int enemyLevel = 1; // If using level scaling

    [Header("Bonus Experience Conditions")]
    [SerializeField] private bool bonusForFullHealth = false; // Bonus XP if killed at full health
    [SerializeField] private int fullHealthBonus = 5;
    [SerializeField] private bool bonusForQuickKill = false; // Bonus XP if killed quickly
    [SerializeField] private float quickKillTime = 3f; // Time in seconds
    [SerializeField] private int quickKillBonus = 3;

    // Private variables
    private EnemyStats enemyStats;
    private float spawnTime;
    private bool wasAtFullHealth;

    void Start()
    {
        // Get components
        enemyStats = GetComponent<EnemyStats>();

        // Record spawn time for quick kill bonus
        spawnTime = Time.time;

        // Check if enemy starts at full health
        if (enemyStats != null)
        {
            wasAtFullHealth = enemyStats.GetCurrentHealth() >= enemyStats.GetMaxHealth();
        }

        // Subscribe to damage events to track full health status
        if (enemyStats != null && bonusForFullHealth)
        {
            enemyStats.OnDamageTaken += OnDamageTaken;
        }
    }

    void OnDamageTaken(int damage)
    {
        // Once damaged, no longer eligible for full health bonus
        wasAtFullHealth = false;
    }

    public int GetExperienceValue()
    {
        int totalExperience = baseExperienceValue;

        // Add random variance
        if (experienceVariance > 0)
        {
            int variance = Random.Range(-experienceVariance, experienceVariance + 1);
            totalExperience += variance;
        }

        // Scale with enemy level if enabled
        if (scaleWithEnemyLevel && enemyLevel > 1)
        {
            float levelMultiplier = 1f + ((enemyLevel - 1) * 0.1f); // +10% per level above 1
            totalExperience = Mathf.RoundToInt(totalExperience * levelMultiplier);
        }

        // Add bonus experience
        totalExperience += GetBonusExperience();

        // Ensure minimum of 1 experience
        return Mathf.Max(1, totalExperience);
    }

    int GetBonusExperience()
    {
        int bonus = 0;

        // Full health bonus
        if (bonusForFullHealth && wasAtFullHealth)
        {
            bonus += fullHealthBonus;
        }

        // Quick kill bonus
        if (bonusForQuickKill)
        {
            float timeSurvived = Time.time - spawnTime;
            if (timeSurvived <= quickKillTime)
            {
                bonus += quickKillBonus;
            }
        }

        return bonus;
    }

    // Public methods for external modification
    public void SetExperienceValue(int newValue) => baseExperienceValue = newValue;
    public void SetEnemyLevel(int level) => enemyLevel = level;
    public void AddExperienceBonus(int bonus) => baseExperienceValue += bonus;

    // Debugging information
    public string GetExperienceBreakdown()
    {
        int baseXP = baseExperienceValue;
        int variance = experienceVariance > 0 ? Random.Range(-experienceVariance, experienceVariance + 1) : 0;
        int levelBonus = scaleWithEnemyLevel && enemyLevel > 1 ?
            Mathf.RoundToInt(baseXP * ((enemyLevel - 1) * 0.1f)) : 0;
        int bonus = GetBonusExperience();

        return $"Base: {baseXP}, Variance: {variance}, Level Bonus: {levelBonus}, Bonus: {bonus}, Total: {GetExperienceValue()}";
    }

    void OnDestroy()
    {
        // Unsubscribe from events
        if (enemyStats != null)
        {
            enemyStats.OnDamageTaken -= OnDamageTaken;
        }
    }

    void OnValidate()
    {
        // Ensure values are valid in editor
        if (baseExperienceValue < 1) baseExperienceValue = 1;
        if (experienceVariance < 0) experienceVariance = 0;
        if (enemyLevel < 1) enemyLevel = 1;
        if (fullHealthBonus < 0) fullHealthBonus = 0;
        if (quickKillTime < 0.1f) quickKillTime = 0.1f;
        if (quickKillBonus < 0) quickKillBonus = 0;
    }
}

// Helper script for easy setup of different enemy types
[System.Serializable]
public class EnemyExperienceTemplate
{
    [Header("Enemy Type")]
    public string enemyName;
    public int experienceValue;
    public int experienceVariance;
    public bool hasQuickKillBonus;
    public bool hasFullHealthBonus;

    // Predefined templates
    public static EnemyExperienceTemplate Weak => new EnemyExperienceTemplate
    {
        enemyName = "Weak Enemy",
        experienceValue = 5,
        experienceVariance = 1,
        hasQuickKillBonus = false,
        hasFullHealthBonus = false
    };

    public static EnemyExperienceTemplate Normal => new EnemyExperienceTemplate
    {
        enemyName = "Normal Enemy",
        experienceValue = 10,
        experienceVariance = 2,
        hasQuickKillBonus = true,
        hasFullHealthBonus = false
    };

    public static EnemyExperienceTemplate Strong => new EnemyExperienceTemplate
    {
        enemyName = "Strong Enemy",
        experienceValue = 20,
        experienceVariance = 3,
        hasQuickKillBonus = true,
        hasFullHealthBonus = true
    };

    public static EnemyExperienceTemplate Boss => new EnemyExperienceTemplate
    {
        enemyName = "Boss Enemy",
        experienceValue = 100,
        experienceVariance = 10,
        hasQuickKillBonus = false,
        hasFullHealthBonus = true
    };

    public void ApplyToProvider(EnemyExperienceProvider provider)
    {
        provider.SetExperienceValue(experienceValue);
        // Note: You'd need to add public setters for variance and bonus settings
        // in EnemyExperienceProvider if you want to use these templates
    }
}