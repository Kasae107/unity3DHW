using UnityEngine;
using UnityEngine.UI;
using CourseGame;

[DisallowMultipleComponent]
public sealed class LevelUpRewardController : MonoBehaviour
{
    [SerializeField] private GameManager gameManager;
    [SerializeField] private PlayerUnit player;
    [SerializeField] private PlayerUpgradeStats upgradeStats;
    [SerializeField] private CourseSession session;
    [SerializeField] private GameObject levelUpPanel;
    [SerializeField] private Button healthButton;
    [SerializeField] private Button damageButton;
    [SerializeField] private Button moveSpeedButton;
    [SerializeField] private Text pendingCountText;
    int pendingRewards;
    bool pausedForReward;
    float previousTimeScale = 1f;
    public int PendingRewards => pendingRewards;
    public bool IsChoosing => pausedForReward;
    public GameObject Panel => levelUpPanel;

    void Start()
    {
        if (!gameManager) gameManager = GameManager.Instance;
        if (!player && gameManager) player = gameManager.Player;
        if (!upgradeStats && player) upgradeStats = player.GetComponent<PlayerUpgradeStats>();
        if (!session) session = CourseSession.Instance;
        if (!gameManager || !player || !upgradeStats || !session || !levelUpPanel || !healthButton || !damageButton || !moveSpeedButton)
        {
            Debug.LogError("Level-up reward references are incomplete.", this); enabled = false; return;
        }
        levelUpPanel.SetActive(false);
        healthButton.onClick.AddListener(ChooseHealth);
        damageButton.onClick.AddListener(ChooseDamage);
        moveSpeedButton.onClick.AddListener(ChooseMoveSpeed);
        player.LevelChanged += HandleLevelChanged;
        gameManager.GameOverStarted += HandleGameOver;
    }
    void Update()
    {
        if (!gameManager || !session) return;
        if (gameManager.IsGameOver || player.IsDead) { HandleGameOver(); return; }
        TryOpen();
    }
    void HandleLevelChanged(int level)
    {
        if (gameManager.IsGameOver || player.IsDead) return;
        if (pendingRewards < int.MaxValue) pendingRewards++;
        RefreshLabels();
        TryOpen();
    }
    void TryOpen()
    {
        if (pausedForReward || pendingRewards == 0 || session.State != CourseSession.Phase.Playing) return;
        previousTimeScale = Time.timeScale;
        pausedForReward = true;
        Time.timeScale = 0;
        RefreshLabels();
        levelUpPanel.SetActive(true);
        levelUpPanel.transform.SetAsLastSibling();
    }
    public void ChooseHealth() => Choose(PlayerUpgradeType.Health);
    public void ChooseDamage() => Choose(PlayerUpgradeType.Damage);
    public void ChooseMoveSpeed() => Choose(PlayerUpgradeType.MoveSpeed);
    public bool Choose(PlayerUpgradeType type)
    {
        if (!pausedForReward || pendingRewards <= 0 || gameManager.IsGameOver || player.IsDead || session.State != CourseSession.Phase.Playing) return false;
        if (!upgradeStats.Apply(type)) return false;
        pendingRewards--;
        session.NotifyRewardChosen(type);
        RefreshLabels();
        if (pendingRewards == 0)
        {
            levelUpPanel.SetActive(false);
            pausedForReward = false;
            Time.timeScale = previousTimeScale;
        }
        return true;
    }
    void RefreshLabels()
    {
        if (pendingCountText) pendingCountText.text = $"剩餘升級選擇：{pendingRewards}";
        SetLabel(healthButton, $"生命強化\n最大生命與目前生命 +{upgradeStats.HealthDelta:0.#}");
        SetLabel(damageButton, $"傷害強化\n新發射子彈傷害 +{upgradeStats.DamageDelta:0.#}");
        SetLabel(moveSpeedButton, $"移動強化\n移動速度 +{upgradeStats.MoveSpeedDelta:0.#}");
    }
    static void SetLabel(Button button, string value)
    {
        var text = button.GetComponentInChildren<Text>(); if (text) text.text = value;
    }
    public void Cancel(bool restoreTime)
    {
        bool wasPaused = pausedForReward;
        pendingRewards = 0; pausedForReward = false;
        if (levelUpPanel) levelUpPanel.SetActive(false);
        if (restoreTime && wasPaused && gameManager && !gameManager.IsGameOver) Time.timeScale = previousTimeScale;
    }
    void HandleGameOver() => Cancel(false);
    void OnDestroy()
    {
        if (player) player.LevelChanged -= HandleLevelChanged;
        if (gameManager) gameManager.GameOverStarted -= HandleGameOver;
        if (healthButton) healthButton.onClick.RemoveListener(ChooseHealth);
        if (damageButton) damageButton.onClick.RemoveListener(ChooseDamage);
        if (moveSpeedButton) moveSpeedButton.onClick.RemoveListener(ChooseMoveSpeed);
        Cancel(true);
    }
}
