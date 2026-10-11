using System.Collections.Generic;
using UnityEngine;

/// <summary>Scene-owned voices; actor destruction cannot truncate impacts or death tails.</summary>
[DefaultExecutionOrder(-900)]
[DisallowMultipleComponent]
public sealed class GameAudioController : MonoBehaviour
{
    public static GameAudioController Instance { get; private set; }
    public GameAudioCatalog catalog;
    public AudioSource music, ambience;
    [Range(0f, 1f)] public float musicVolume = 0.6f;
    [Range(0f, 1f)] public float effectsVolume = 0.8f;
    [Range(0f, 1f)] public float ambienceVolume = 0.12f;
    [SerializeField, Range(8, 48)] private int voiceLimit = 24;
    private sealed class Voice
    {
        public AudioSource source;
        public GameSound sound;
        public float remaining, gain;
        public int token;
        public bool ui;
    }
    private Voice[] voices;
    private int nextToken;
    private static string restartingLevel;
    private readonly Dictionary<(Object, GameSound), float> lastPlayed = new Dictionary<(Object, GameSound), float>();
    private readonly Dictionary<GameSound, int> counts = new Dictionary<GameSound, int>();
    public int Played(GameSound sound) => counts.TryGetValue(sound, out int count) ? count : 0;
    public int ActiveVoices { get { int count = 0; if (voices != null) foreach (var v in voices) if (v.remaining > 0) count++; return count; } }

    private void Awake()
    {
        Instance = this;
        voices = new Voice[voiceLimit];
        for (int i = 0; i < voices.Length; i++)
        {
            var child = new GameObject("EffectVoice_" + i);
            child.transform.SetParent(transform, false);
            var source = child.AddComponent<AudioSource>();
            source.playOnAwake = false; source.spatialBlend = 0f; source.dopplerLevel = 0;
            voices[i] = new Voice { source = source };
        }
        ApplyVolumes();
    }

    // Carry only a cue request across a reload, never a controller or a music source.
    public static void QueueLevelRestart(string scenePath) { restartingLevel = scenePath; }
    private void Start()
    {
        bool restarted = restartingLevel == gameObject.scene.path;
        restartingLevel = null;
        if (restarted) Play(GameSound.LevelRestart, this, 1f, true);
    }

    public int Play(GameSound sound, Object emitter = null, float gain = 1f, bool ui = false)
    {
        if (!ui && (Time.timeScale <= 0 || AudioListener.pause)) return 0;
        var cue = catalog == null ? null : catalog.Find(sound);
        if (cue == null || cue.clips == null || cue.clips.Length == 0 || voices == null) return 0;
        var key = (emitter, sound);
        // Bound bookkeeping as projectiles are created and destroyed during long runs.
        if (lastPlayed.Count > 2048)
        {
            var expired = new List<(Object, GameSound)>();
            foreach (var entry in lastPlayed)
                if (entry.Key.Item1 == null || Time.unscaledTime - entry.Value > 60f) expired.Add(entry.Key);
            foreach (var entry in expired) lastPlayed.Remove(entry);
        }
        float now = Time.unscaledTime;
        if (lastPlayed.TryGetValue(key, out float previous) && now - previous < cue.minimumInterval) return 0;
        int simultaneous = 0;
        Voice free = null, weakest = null;
        foreach (var voice in voices)
        {
            if (voice.remaining <= 0) { if (free == null) free = voice; continue; }
            if (voice.sound == sound) simultaneous++;
            if (weakest == null || voice.source.priority > weakest.source.priority) weakest = voice;
        }
        if (simultaneous >= cue.maximumConcurrent) return 0;
        if (free == null && (weakest == null || weakest.source.priority <= cue.priority)) return 0;
        var selected = free ?? weakest;
        var clip = cue.clips[Random.Range(0, cue.clips.Length)];
        if (clip == null) return 0;
        selected.source.Stop(); selected.source.clip = clip;
        selected.source.pitch = Mathf.Clamp(Random.Range(cue.pitch.x, cue.pitch.y), 0.5f, 2f);
        selected.source.priority = cue.priority; selected.source.ignoreListenerPause = ui;
        selected.sound = sound; selected.ui = ui; selected.gain = cue.volume * Mathf.Clamp01(gain);
        selected.source.volume = selected.gain * effectsVolume;
        selected.remaining = clip.length / selected.source.pitch + 0.05f;
        selected.token = ++nextToken;
        lastPlayed[key] = now; counts[sound] = Played(sound) + 1;
        selected.source.Play();
        return selected.token;
    }

    public void Cancel(int token)
    {
        if (token == 0 || voices == null) return;
        foreach (var voice in voices) if (voice.token == token)
        { voice.source.Stop(); voice.remaining = 0; return; }
    }

    private void Update()
    {
        ApplyVolumes();
        foreach (var voice in voices)
        {
            if (voice.remaining <= 0 || !voice.ui && AudioListener.pause) continue;
            voice.remaining -= voice.ui ? Time.unscaledDeltaTime : Time.deltaTime;
            if (voice.remaining <= 0) { voice.source.Stop(); voice.source.clip = null; }
        }
    }

    private void ApplyVolumes()
    {
        if (music != null) music.volume = musicVolume;
        if (ambience != null) ambience.volume = ambienceVolume;
        if (voices != null) foreach (var voice in voices) voice.source.volume = voice.gain * effectsVolume;
    }

    // Short interaction tails may cross a scene load; they never contain music/ambience.
    private static int transitionVoices;
    public void PlayTransition(GameSound sound, Object emitter)
    {
        var cue = catalog == null ? null : catalog.Find(sound);
        if (transitionVoices >= 4 || cue == null || cue.clips == null || cue.clips.Length == 0) return;
        var key = (emitter, sound);
        float now = Time.unscaledTime;
        if (lastPlayed.TryGetValue(key, out float previous) && now - previous < cue.minimumInterval) return;
        var clip = cue.clips[Random.Range(0, cue.clips.Length)]; if (clip == null) return;
        var root = new GameObject("InteractionSoundTail"); DontDestroyOnLoad(root);
        var source = root.AddComponent<AudioSource>(); source.playOnAwake = false; source.clip = clip;
        source.volume = cue.volume * effectsVolume; source.priority = cue.priority; source.ignoreListenerPause = true;
        lastPlayed[key] = now;
        transitionVoices++; counts[sound] = Played(sound) + 1; source.Play();
        root.AddComponent<InteractionSoundTail>().remaining = clip.length + 0.1f;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetSession() { Instance = null; transitionVoices = 0; restartingLevel = null; }
    private void OnDestroy() { if (Instance == this) Instance = null; }
    public sealed class InteractionSoundTail : MonoBehaviour
    {
        public float remaining;
        private void Update() { remaining -= Time.unscaledDeltaTime; if (remaining <= 0) Destroy(gameObject); }
        private void OnDestroy() { transitionVoices = Mathf.Max(0, transitionVoices - 1); }
    }
}
