using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum InteractableType { Enemy, Item }

public class Interactable : MonoBehaviour
{
    public Actor myActor { get; private set; }

    public InteractableType interactionType;

    void Awake()
    {
        if (interactionType == InteractableType.Enemy)
        {
            myActor = GetComponent<Actor>();
            if (myActor == null)
            {
                myActor = GetComponentInParent<Actor>();
            }
        }
    }

    /// <summary>
    /// Método principal para interagir com o objeto.
    /// </summary>
    /// <param name="player">O objeto do jogador que está interagindo.</param>
    public void Interact(GameObject player)
    {
        switch (interactionType)
        {
            case InteractableType.Enemy:
                InteractWithEnemy(player);
                break;
            case InteractableType.Item:
                InteractWithItem();
                break;
            default:
                Debug.LogWarning("Tipo de interação não definido.");
                break;
        }
    }

    private void InteractWithEnemy(GameObject player)
    {
        if (myActor == null)
        {
            myActor = GetComponentInParent<Actor>();
        }

        if (myActor != null)
        {
            if (myActor.healthBar != null)
            {
                myActor.healthBar.gameObject.SetActive(true);
            }

            Debug.Log($"Interagindo com inimigo: {myActor.name}");
            // Aqui voce pode adicionar logica para combate ou dialogo com inimigos

        }
        else
        {
            Debug.LogWarning("O ator inimigo nao foi encontrado!");
        }
    }

    private void InteractWithItem()
    {
        Debug.Log($"Item coletado: {gameObject.name}");
        Destroy(gameObject);
    }
}
