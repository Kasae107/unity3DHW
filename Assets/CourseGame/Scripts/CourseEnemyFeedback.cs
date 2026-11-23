using UnityEngine;

namespace CourseGame
{
    [RequireComponent(typeof(EnemyUnit))]
    public sealed class CourseEnemyFeedback : MonoBehaviour
    {
        EnemyUnit unit;
        void Awake() { unit = GetComponent<EnemyUnit>(); }
        void OnEnable() { if(!unit) unit=GetComponent<EnemyUnit>(); unit.Died += OnDeath; }
        void OnDisable() { if(unit) unit.Died -= OnDeath; }
        void OnDeath(EnemyUnit enemy) { if(CourseSession.Instance) CourseSession.Instance.RegisterKill(transform.position); }
    }
}
