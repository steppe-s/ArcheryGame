using UnityEngine;

namespace _Scripts.Objects
{
    [CreateAssetMenu]
    public class BowType : ScriptableObject
    {
        [SerializeField] private float velocity;
        [SerializeField] private float drawTime;
        [SerializeField] private float cooldown;
        [SerializeField] private float aimSpeed;
        [SerializeField] private AnimationCurve drawCurve;

        public float Velocity => velocity;

        public float DrawTime => drawTime;

        public float Cooldown => cooldown;

        public float AimSpeed => aimSpeed;

        public AnimationCurve DrawCurve => drawCurve;
    }
}
