using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class PlayerActor : Actor
{
    public override void Awake()
    {
        base.Awake();
        healthBar.gameObject.SetActive(true); // A barra de vida do jogador está sempre visível
    }
}