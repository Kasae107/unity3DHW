using UnityEngine;

public enum PlayerUpgradeType { Health, Damage, MoveSpeed }

[DisallowMultipleComponent]
[RequireComponent(typeof(PlayerUnit), typeof(PlayerMovement))]
public sealed class PlayerUpgradeStats : MonoBehaviour
{
    [SerializeField, Min(0f)] private float healthDelta = 20f;
    [SerializeField, Min(0f)] private float damageDelta = 2f;
    [SerializeField, Min(0f)] private float moveSpeedDelta = .5f;
    public float DamageBonus { get; private set; }
    public float HealthDelta => healthDelta;
    public float DamageDelta => damageDelta;
    public float MoveSpeedDelta => moveSpeedDelta;
    PlayerUnit player;
    PlayerMovement movement;

    void Awake() { player = GetComponent<PlayerUnit>(); movement = GetComponent<PlayerMovement>(); }
    void OnValidate()
    {
        healthDelta = Mathf.Max(0, healthDelta);
        damageDelta = Mathf.Max(0, damageDelta);
        moveSpeedDelta = Mathf.Max(0, moveSpeedDelta);
    }
    public bool Apply(PlayerUpgradeType type)
    {
        if (!player || !movement || player.IsDead) return false;
        switch (type)
        {
            case PlayerUpgradeType.Health:
                player.SetMaxHealth(player.MaxHealth + healthDelta);
                player.Heal(healthDelta);
                return true;
            case PlayerUpgradeType.Damage:
                DamageBonus += damageDelta;
                return true;
            case PlayerUpgradeType.MoveSpeed:
                movement.SetMoveSpeed(movement.MoveSpeed + moveSpeedDelta);
                return true;
            default: return false;
        }
    }
}
