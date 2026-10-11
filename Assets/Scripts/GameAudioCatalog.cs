using System;
using UnityEngine;

public enum GameSound
{
    Footstep, Jump, DoubleJump, Land, HardLand, TailSweep, FireLaunch, FireImpact,
    PlayerHurt, PlayerDeath, ChomperWarn, ChomperBite, ChomperHit, ChomperDeath,
    SporaWarn, SporaLaunch, SporeImpact, SporaHit, SporaDeath, RockStep, RockWarn,
    RockSlam, RockHit, RockDeath, BossSlamWarn, BossLungeWarn, BossSporeWarn,
    BossSlam, BossShockwave, BossLunge, BossVolley, BossHit, BossPhase, BossDeath,
    WhisperseedRecruit, DryadRecruit, PetalbornRecruit, CrystalAppear, CrystalTouch,
    MenuFocus, MenuClick, Pause, Resume, BossThornWarn, BossThorns, AmuletCollect, LevelRestart
}

[CreateAssetMenu(menuName = "Lost in the Forest/Audio catalog")]
public sealed class GameAudioCatalog : ScriptableObject
{
    [Serializable]
    public sealed class Cue
    {
        public GameSound sound;
        public AudioClip[] clips;
        [Range(0f, 1f)] public float volume = 0.5f;
        public Vector2 pitch = new Vector2(0.96f, 1.04f);
        [Min(0f)] public float minimumInterval = 0.06f;
        [Range(0, 256)] public int priority = 100;
        [Range(1, 12)] public int maximumConcurrent = 4;
    }

    public Cue[] cues;
    public Cue Find(GameSound sound) => Array.Find(cues ?? Array.Empty<Cue>(), c => c.sound == sound);
}
