using System.Collections;
using System.Collections.Generic;
using _Scripts.Character;
using _Scripts.CharacterParts;
using UnityEngine;

public class PoisonArrow : ArrowBehaviour
{
    [SerializeField] private float damage;
    protected override void OnHitDamageableObjectServerOnly(GameObject damageable)
    {
        base.OnHitDamageableObjectServerOnly(damageable);
        var d = damageable.GetComponent<DamageableObjectBehaviour>();
        d.Damage(damage);
    }
}
