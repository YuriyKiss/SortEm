using UnityEngine;
using System.Collections;

public class EndGameConditions : MonoBehaviour
{
    [SerializeField] private GameObject winMenu;

    public void CheckConditions()
    {
        bool finish = true;

        foreach(GameObject zone in GameObject.FindGameObjectsWithTag("Zone"))
        {
            ZoneController controller = zone.GetComponent<ZoneController>();

            if (!controller.CompareCharacters()) finish = false;
        }
        
        if (finish) StartCoroutine(FinishGame());
    }

    private IEnumerator FinishGame()
    {
        yield return new WaitForSeconds(0.5f);

        winMenu.SetActive(true);

        foreach (GameObject player in GameObject.FindGameObjectsWithTag("Player"))
        {
            PuppetMovement puppet = player.GetComponent<PuppetMovement>();

            StartCoroutine(puppet.StartDancing());
        }
    }
}
