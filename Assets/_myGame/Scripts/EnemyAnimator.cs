using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Spine;
using Spine.Unity;
using System;

public class EnemyAnimator : MonoBehaviour
{
    // Script responsible for executing the animations of the enemy
    // Following the same pattern as CharacterAnimator

    // SkeletonAnimation is the class that has all the information of the spine animation
    SkeletonAnimation enemySkeleton;

    // Animation dictionary for enemies (simple - no job system needed)
    Dictionary<EnemyAnimations, string> EnemyAnimationNames = new Dictionary<EnemyAnimations, string>();

    EnemyAnimations AnimationToPlay = EnemyAnimations.Idle;
    string currentAnimationName = "";

    void Awake()
    {
        // Get SkeletonAnimation component (should be on this GameObject)
        enemySkeleton = GetComponent<SkeletonAnimation>();

        if (enemySkeleton == null)
        {
            Debug.LogError($"No SkeletonAnimation found on {gameObject.name}! EnemyAnimator requires SkeletonAnimation component.");
            return;
        }

        CreateAnimationsDictionary();
    }

    private void Start()
    {
        if (enemySkeleton != null)
        {
            // Subscribe to animation events if needed
            enemySkeleton.AnimationState.Event += OnEventAnimation;
        }

        // Start with idle animation
        PlayAnimation("Idle");
    }

    void OnEventAnimation(TrackEntry trackEntry, Spine.Event e)
    {
        // Handle animation events (similar to CharacterAnimator)
        // You can add specific enemy events here

        if (e.Data.Name == "AttackHit")
        {
            // This could trigger damage at the exact moment of impact
            // The EnemyMovement script handles the actual damage logic
        }
    }

    void CreateAnimationsDictionary()
    {
        // Fill the dictionary with enemy animations
        // Simple mapping - no job variants needed for enemies

        EnemyAnimationNames.Add(EnemyAnimations.Idle, "Idle");
        EnemyAnimationNames.Add(EnemyAnimations.Walk, "Walk");
        EnemyAnimationNames.Add(EnemyAnimations.Attack, "Attack");
        EnemyAnimationNames.Add(EnemyAnimations.Dead, "Dead");
    }

    // Public method to change animation (matches CharacterAnimator pattern)
    public void ChangeAnimation(string animationString)
    {
        // Convert string to enum
        if (System.Enum.TryParse(animationString, out EnemyAnimations newAnimation))
        {
            AnimationToPlay = newAnimation;
            AnimationManager();
        }
        else
        {
            Debug.LogWarning($"Animation '{animationString}' not found for enemy {gameObject.name}");
        }
    }

    // Overloaded method to accept enum directly
    public void PlayAnimation(EnemyAnimations animation)
    {
        AnimationToPlay = animation;
        AnimationManager();
    }

    // String version for compatibility with EnemyMovement
    public void PlayAnimation(string animationName)
    {
        ChangeAnimation(animationName);
    }

    // Runs the required animation using SetAnimation spine function (matches CharacterAnimator)
    void AnimationManager()
    {
        if (enemySkeleton == null) return;

        // Get the animation name from dictionary
        if (!EnemyAnimationNames.ContainsKey(AnimationToPlay))
        {
            Debug.LogWarning($"Animation {AnimationToPlay} not found in dictionary for {gameObject.name}");
            return;
        }

        string animationName = EnemyAnimationNames[AnimationToPlay];

        // Don't restart the same animation
        if (currentAnimationName == animationName) return;

        currentAnimationName = animationName;

        try
        {
            // Death animation should not loop, others should loop
            bool shouldLoop = AnimationToPlay != EnemyAnimations.Dead;

            enemySkeleton.AnimationState.SetAnimation(0, animationName, shouldLoop);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to play animation '{animationName}' on enemy {gameObject.name}: {e.Message}");
        }
    }

    // Public getters for state checking
    public string GetCurrentAnimation() => currentAnimationName;
    public EnemyAnimations GetCurrentAnimationEnum() => AnimationToPlay;
    public bool IsPlayingAnimation(string animationName) => currentAnimationName == animationName;
    public bool IsPlayingAnimation(EnemyAnimations animation) => AnimationToPlay == animation;

    // Animation state queries
    public bool IsIdle() => AnimationToPlay == EnemyAnimations.Idle;
    public bool IsWalking() => AnimationToPlay == EnemyAnimations.Walk;
    public bool IsAttacking() => AnimationToPlay == EnemyAnimations.Attack;
    public bool IsDead() => AnimationToPlay == EnemyAnimations.Dead;

    // Set animation speed (useful for different enemy types)
    public void SetAnimationSpeed(float speed)
    {
        if (enemySkeleton != null)
        {
            enemySkeleton.timeScale = speed;
        }
    }

    // Get animation duration
    public float GetAnimationDuration(EnemyAnimations animation)
    {
        if (enemySkeleton == null || !EnemyAnimationNames.ContainsKey(animation))
            return 0f;

        try
        {
            string animationName = EnemyAnimationNames[animation];
            var skeletonData = enemySkeleton.SkeletonDataAsset.GetSkeletonData(false);
            var spineAnimation = skeletonData.FindAnimation(animationName);
            return spineAnimation != null ? spineAnimation.Duration : 0f;
        }
        catch
        {
            return 0f;
        }
    }

    // Check if animation exists in Spine data
    public bool HasAnimation(EnemyAnimations animation)
    {
        if (enemySkeleton == null || !EnemyAnimationNames.ContainsKey(animation))
            return false;

        try
        {
            string animationName = EnemyAnimationNames[animation];
            var skeletonData = enemySkeleton.SkeletonDataAsset.GetSkeletonData(false);
            var spineAnimation = skeletonData.FindAnimation(animationName);
            return spineAnimation != null;
        }
        catch
        {
            return false;
        }
    }

    void OnDestroy()
    {
        // Clean up event subscriptions
        if (enemySkeleton != null && enemySkeleton.AnimationState != null)
        {
            enemySkeleton.AnimationState.Event -= OnEventAnimation;
        }
    }

    // Debug method to list all available animations in Spine data
    [ContextMenu("List Available Spine Animations")]
    public void ListAvailableAnimations()
    {
        if (enemySkeleton == null || enemySkeleton.SkeletonDataAsset == null)
        {
            Debug.Log("No Spine skeleton data available");
            return;
        }

        try
        {
            var skeletonData = enemySkeleton.SkeletonDataAsset.GetSkeletonData(false);
            Debug.Log($"Available Spine animations for {gameObject.name}:");

            foreach (var animation in skeletonData.Animations)
            {
                Debug.Log($"- {animation.Name} (Duration: {animation.Duration:F2}s)");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error listing animations: {e.Message}");
        }
    }
}

// Enemy animation enum (simple compared to player)
public enum EnemyAnimations
{
    Idle, Walk, Attack, Dead
}