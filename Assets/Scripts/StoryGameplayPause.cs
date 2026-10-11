using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Owns a cutscene pause and restores only the behaviors it suspended.</summary>
public sealed class StoryGameplayPause : IDisposable
{
    private readonly List<Behaviour> suspended = new List<Behaviour>();
    private readonly Rigidbody2D playerBody;
    private readonly bool wasSimulated;
    private readonly float previousTimeScale;
    private readonly bool previousAudioPause;
    private bool disposed;

    public StoryGameplayPause(Movement player, Canvas storyCanvas = null)
    {
        previousTimeScale = Time.timeScale;
        previousAudioPause = AudioListener.pause;
        StoryRunSession.LockGameplay();
        Time.timeScale = 0f;
        AudioListener.pause = true;
        foreach (var behavior in UnityEngine.Object.FindObjectsByType<MonoBehaviour>())
            if (behavior is Movement || behavior is PlayerCombat || behavior is BlightEnemy ||
                behavior is BlightBoss || behavior is EnemyNavigator2D || behavior is BloomPetFollower)
            {
                if (!behavior.enabled) continue;
                suspended.Add(behavior);
                behavior.enabled = false;
            }
        if (storyCanvas != null)
            foreach (var canvas in UnityEngine.Object.FindObjectsByType<Canvas>())
            {
                if (!canvas.enabled || canvas == storyCanvas) continue;
                suspended.Add(canvas);
                canvas.enabled = false;
            }
        playerBody = player.GetComponent<Rigidbody2D>();
        if (playerBody != null)
        {
            wasSimulated = playerBody.simulated;
            playerBody.linearVelocity = Vector2.zero;
            playerBody.simulated = false;
        }
    }

    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        foreach (var behavior in suspended)
        {
            if (behavior == null) continue;
            var health = behavior.GetComponent<EnemyHealth>();
            var playerHealth = behavior.GetComponent<PlayerHealth>();
            if ((health != null && health.IsDead) || (playerHealth != null && playerHealth.IsDead)) continue;
            behavior.enabled = true;
        }
        if (playerBody != null && !(playerBody.TryGetComponent<PlayerHealth>(out var bodyHealth) && bodyHealth.IsDead))
        {
            playerBody.linearVelocity = Vector2.zero;
            playerBody.simulated = wasSimulated;
        }
        Time.timeScale = previousTimeScale;
        AudioListener.pause = previousAudioPause;
        StoryRunSession.UnlockGameplay();
    }
}
