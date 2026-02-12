using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Interactable: MonoBehaviour
{
    public string pickUpMessage;
    public void BaseInteract()
    {
        Interact();
    }
    protected virtual void Interact()
    {

    }
}
