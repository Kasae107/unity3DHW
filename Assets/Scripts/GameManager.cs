using System.Collections.Generic;
using System;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class GameManager : MonoBehaviour
{
    private const int ExperiencePerEnemy = 1;

    public static GameManager Instance { get; private set; }

    [Header("玩家")]
    [SerializeField, Tooltip("場景中的玩家單位。")]
    private PlayerUnit player;

    [Header("物件池")]
    [SerializeField, Tooltip("統一管理場景中敵人的物件池。")]
    private EnemyPool enemyPool;

    [SerializeField, Tooltip("統一管理所有武器投射物的物件池。")]
    private ProjectilePool projectilePool;

    private readonly HashSet<EnemyUnit> enemies = new();

    public PlayerUnit Player => player;
    public int EnemyCount => enemies.Count;
    public bool IsGameOver { get; private set; }
    public event Action GameOverStarted;

    public void BeginGameOver()
    {
        if (IsGameOver) return;
        IsGameOver = true;
        GameOverStarted?.Invoke();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("場景中存在重複的 GameManager，將移除重複物件。", this);
            Destroy(gameObject);
            return;
        }

        Instance = this;
        ValidateConfiguration();
    }

    private void OnDestroy()
    {
        foreach (EnemyUnit enemy in enemies)
        {
            if (enemy != null)
            {
                enemy.Died -= HandleEnemyDeath;
            }
        }

        enemies.Clear();

        if (Instance == this)
        {
            Instance = null;
        }
    }

    public PlayerUnit GetPlayer()
    {
        return player;
    }

    public EnemyPool GetEnemyPool()
    {
        return enemyPool;
    }

    public ProjectilePool GetProjectilePool()
    {
        return projectilePool;
    }

    public void RegisterEnemy(EnemyUnit enemy)
    {
        if (enemy == null || !enemies.Add(enemy))
        {
            return;
        }

        enemy.Died += HandleEnemyDeath;
    }

    public void UnregisterEnemy(EnemyUnit enemy)
    {
        if (enemy == null || !enemies.Remove(enemy))
        {
            return;
        }

        enemy.Died -= HandleEnemyDeath;
    }

    private void HandleEnemyDeath(EnemyUnit enemy)
    {
        if (IsGameOver || player == null || player.IsDead)
        {
            return;
        }

        player.AddExperience(ExperiencePerEnemy);
        Debug.Log($"擊殺 {enemy.name}，獲得 {ExperiencePerEnemy} EXP。", this);
    }

    public void HandlePlayerDeath(PlayerUnit deadPlayer)
    {
        if (IsGameOver || deadPlayer == null || deadPlayer != player)
        {
            return;
        }

        BeginGameOver();
        Debug.Log("玩家死亡，遊戲進入結束狀態。", this);
    }

    private void ValidateConfiguration()
    {
        if (player == null)
        {
            Debug.LogError("GameManager 尚未設定 PlayerUnit。", this);
        }

        if (enemyPool == null)
        {
            Debug.LogError("GameManager 尚未設定 EnemyPool。", this);
        }

        if (projectilePool == null)
        {
            Debug.LogError("GameManager 尚未設定 ProjectilePool。", this);
        }
    }
}
