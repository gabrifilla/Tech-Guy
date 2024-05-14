using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu]
public class WeaponScript : ScriptableObject
{
    public string weaponName;
    public float baseDamage;
    public float attackSpeed;
    public Ability[] abilities; // Lista de habilidades desta arma
}

