using UnityEngine;
using System.Collections;

public class StartSequence : MonoBehaviour
{
    private EndGameConditions endGame;

    [SerializeField] private GameObject cam;
    [SerializeField] private GameObject trackers;

    private float timer = 0f;
    private bool activated = false;

    private void Start()
    {
        endGame = GameObject.FindGameObjectWithTag("Scripts").GetComponent<EndGameConditions>();
    }

    private void Update()
    {
        timer += Time.deltaTime;

        if (timer >= 5.63f && !activated)
        {
            activated = true;

            endGame.isPaused = false;

            StartCoroutine(MoveCamera());
            StartCoroutine(RotateCamera());
        }
    }

    private IEnumerator MoveCamera()
    {
        yield return new WaitForSeconds(0.35f);

        float positionTimer = 0f;

        Vector3 startPosition = cam.transform.position;
        Vector3 finalPosition = new Vector3(0, 9.7f, 10f);

        while (positionTimer < 1f)
        {
            positionTimer += Time.deltaTime;

            cam.transform.position = Vector3.Lerp(startPosition, finalPosition, positionTimer);

            yield return null;
        }
    }

    private IEnumerator RotateCamera()
    {
        yield return new WaitForSeconds(0.35f);

        float rotationTimer = 0f;

        Quaternion startRotation = cam.transform.rotation;
        Quaternion finalRotation = Quaternion.Euler(new Vector3(50, -180, 0));

        while (rotationTimer < 1f)
        {
            rotationTimer += Time.deltaTime;

            cam.transform.rotation = Quaternion.Slerp(startRotation, finalRotation, rotationTimer);

            yield return null;
        }

        trackers.SetActive(true);
        endGame.isPaused = false;
    }
}
