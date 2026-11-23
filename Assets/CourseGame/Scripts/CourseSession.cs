using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace CourseGame
{
    [DefaultExecutionOrder(-50)]
    public sealed class CourseSession : MonoBehaviour
    {
        public enum Phase { Welcome, Playing, Paused, Defeat, Victory }
        public static CourseSession Instance { get; private set; }
        public PlayerUnit player;
        public EnemySpawner spawner;
        public WeaponSlot[] weapons;
        public float duration = 180f;
        public SurvivalTimer survivalTimer;
        public Phase State { get; private set; } = Phase.Welcome;
        public float Elapsed => survivalTimer != null ? survivalTimer.ElapsedSeconds : 0f;
        public int Kills { get; private set; }
        public int Level => player != null ? player.Level : 1;
        public LevelUpRewardController rewards;
        float pausedTimeScale = 1f;
        public bool IsChoosingReward => rewards != null && rewards.IsChoosing;
        public string Notice { get; private set; } = "";
        public float NoticeUntil { get; private set; }
        public int BestKills => PlayerPrefs.GetInt("CourseGame.BestKills", 0);
        public bool Muted { get; private set; }
        public bool VerificationRun { get; set; }
        public int Shots { get { int n=0; foreach(var w in weapons) if(w) n+=w.ShotsFired; return n; } }
        AudioSource audioSource;
        AudioClip hitSound, levelSound;
        float nextSound;
        int difficulty = -1;

        void Awake()
        {
            Instance = this;
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.volume = 0.18f;
            hitSound = Tone("Impact", 180f, 0.065f);
            levelSound = Tone("Upgrade", 660f, 0.22f);
            Muted = PlayerPrefs.GetInt("CourseGame.Muted", 0) == 1;
        }
        void Start()
        {
            player.Died += OnPlayerDied;
            Time.timeScale = 0;
        }
        void Update()
        {
            var k = Keyboard.current;
            if (k != null)
            {
                if (k.mKey.wasPressedThisFrame) ToggleMute();
                if (k.enterKey.wasPressedThisFrame || k.spaceKey.wasPressedThisFrame)
                {
                    if (State == Phase.Welcome) StartRun();
                    else if (State == Phase.Paused) Resume();
                }
                if (k.escapeKey.wasPressedThisFrame && !IsChoosingReward)
                {
                    if (State == Phase.Playing) Pause();
                    else if (State == Phase.Paused) Resume();
                }
                if (k.rKey.wasPressedThisFrame && State != Phase.Welcome) Restart();
            }
            if (State != Phase.Playing) return;
            int stage = Mathf.FloorToInt(Elapsed / 30f);
            if (stage != difficulty) { difficulty = stage; spawner.SetDifficulty(stage); }
            if (Elapsed >= duration) Finish(true);
        }
        public void StartRun()
        {
            if (State != Phase.Welcome) return;
            State = Phase.Playing;
            survivalTimer.Begin();
            Time.timeScale = 1;
            Notice = "移動閃避，武器會自動攻擊最近的敵人";
            NoticeUntil = Time.unscaledTime + 5;
        }
        public void Pause() { if(State != Phase.Playing || IsChoosingReward) return; pausedTimeScale=Time.timeScale; State = Phase.Paused; Time.timeScale = 0; }
        public void Resume() { if(State != Phase.Paused || IsChoosingReward) return; State = Phase.Playing; Time.timeScale = pausedTimeScale; }
        public void Restart() { if(rewards)rewards.Cancel(false); Time.timeScale = 1; SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex); }
        void OnPlayerDied(PlayerUnit unit) { Finish(false); }
        public void RegisterKill(Vector3 position)
        {
            if (State != Phase.Playing) return;
            Kills++;
            if (!Muted && Time.unscaledTime >= nextSound) { audioSource.PlayOneShot(hitSound); nextSound = Time.unscaledTime + .09f; }
        }
        public void NotifyRewardChosen(PlayerUpgradeType type)
        {
            Notice = type switch { PlayerUpgradeType.Health => "已選擇生命強化", PlayerUpgradeType.Damage => "已選擇傷害強化", _ => "已選擇移動強化" };
            NoticeUntil = Time.unscaledTime + 3.5f;
            if (!Muted) audioSource.PlayOneShot(levelSound);
        }
        public void Finish(bool victory)
        {
            if (State == Phase.Defeat || State == Phase.Victory) return;
            State = victory ? Phase.Victory : Phase.Defeat;
            Time.timeScale = 0;
            if(GameManager.Instance) GameManager.Instance.BeginGameOver();
            survivalTimer.StopTimer();
            Time.timeScale = 0;
            if (!VerificationRun && Kills > BestKills) { PlayerPrefs.SetInt("CourseGame.BestKills", Kills); PlayerPrefs.Save(); }
        }
        public void ToggleMute() { Muted = !Muted; PlayerPrefs.SetInt("CourseGame.Muted", Muted ? 1 : 0); PlayerPrefs.Save(); }
        static AudioClip Tone(string name, float frequency, float length)
        {
            int n = Mathf.CeilToInt(22050 * length);
            var data = new float[n];
            for(int i=0;i<n;i++) { float t=i/22050f; data[i] = Mathf.Sin(t*frequency*6.28318f) * (1f-i/(float)n) * .45f; }
            var clip=AudioClip.Create(name,n,1,22050,false); clip.SetData(data,0); return clip;
        }
        void OnDestroy()
        {
            if(player) player.Died -= OnPlayerDied;
            if(Instance == this) { Instance=null; Time.timeScale=1; }
            if(hitSound) Destroy(hitSound);
            if(levelSound) Destroy(levelSound);
        }
    }
}
