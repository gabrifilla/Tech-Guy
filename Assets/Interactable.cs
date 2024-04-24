using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum InteractableType { Enemy, Item }

public class Interactable : MonoBehaviour
{
    public EnemyAI myActor { get; private set; }

    public InteractableType interactionType;

    void Awake()
    {
        if(interactionType == InteractableType.Enemy)
        {
            myActor = GetComponent<EnemyAI>();
        }
    }

    public void InteractWithItem()
    {
        Destroy(gameObject);
    }

}
