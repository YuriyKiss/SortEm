using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class ZoneController : MonoBehaviour
{
    private EndGameConditions endGame;
    [SerializeField] private Slider tracker;
    private MeshRenderer floor;

    private List<GameObject> characters;

    private float trackerSpeed;
    public int charactersAmount;
    private int correctCharactersAmount;

    [SerializeField] private Material floorColor;
    [SerializeField] private Material defaultColor;

    private void Start()
    {
        GameObject scripts = GameObject.FindGameObjectWithTag("Scripts");

        endGame = scripts.GetComponent<EndGameConditions>();

        floor = GetComponentInChildren<MeshRenderer>();

        characters = new List<GameObject>();

        trackerSpeed = 0.03f / charactersAmount;
    }

    private void OnTriggerEnter(Collider other)
    {
        ReliableOnTriggerExit.NotifyTriggerEnter(other, gameObject, OnTriggerExit);

        if (other.CompareTag("Trigger"))
        {
            characters.Add(other.GetComponentInParent<PuppetMovement>().gameObject);
            UpdateCorrectCharacters();
            endGame.CheckConditions();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        ReliableOnTriggerExit.NotifyTriggerExit(other, gameObject);

        if (other.CompareTag("Trigger"))
        {
            characters.Remove(other.GetComponentInParent<PuppetMovement>().gameObject);
            UpdateCorrectCharacters();
        }
    }

    private void UpdateCorrectCharacters()
    {
        int counter = 0;

        foreach (GameObject character in characters)
        {
            PuppetMovement puppet = character.GetComponent<PuppetMovement>();

            if (puppet.color == gameObject.name)
            {
                ++counter;
            }
        }

        correctCharactersAmount = counter;

        if (characters.Count == correctCharactersAmount && correctCharactersAmount == charactersAmount) 
            floor.material = floorColor;
        else 
            floor.material = defaultColor;
    }
    
    public bool CompareCharacters() => 
        correctCharactersAmount == charactersAmount;

    public bool FindCharacter(GameObject character) =>
        characters.Contains(character);

    private void Update()
    {
        float percentage = (float)correctCharactersAmount / (float)charactersAmount;

        if (tracker.value < percentage)
        {
            tracker.value += trackerSpeed;
        }
        else if (tracker.value > percentage + trackerSpeed)
        {
            tracker.value -= trackerSpeed;
        }
    }
}
