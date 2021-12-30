using UnityEngine;
using System.Collections.Generic;

public class ZoneController : MonoBehaviour
{
    private EndGameConditions endGame;

    public List<GameObject> characters;

    private void Start()
    {
        GameObject scripts = GameObject.FindGameObjectWithTag("Scripts");

        endGame = scripts.GetComponent<EndGameConditions>();

        characters = new List<GameObject>();
    }

    private void OnTriggerEnter(Collider other)
    {
        ReliableOnTriggerExit.NotifyTriggerEnter(other, gameObject, OnTriggerExit);

        if (other.CompareTag("Player"))
        {
            characters.Add(other.gameObject);
            endGame.CheckConditions();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        ReliableOnTriggerExit.NotifyTriggerExit(other, gameObject);

        if (other.CompareTag("Player"))
        {
            characters.Remove(other.gameObject);
        }
    }
    
    public bool CompareCharacters()
    {
        bool result = true;

        foreach(GameObject character in characters)
        {
            PuppetMovement puppet = character.GetComponent<PuppetMovement>();

            if (puppet.color != gameObject.name)
            {
                result = false;
            }
        }

        return result;
    }
}
