using UnityEngine;
using System.Collections;

public class StartSequence : MonoBehaviour
{
    private EndGameConditions endGame;

    [SerializeField] private GameObject cam;
    [SerializeField] private GameObject trackers;
    [SerializeField] private GameObject playMessage;

    private float timer = 0f;
    private bool activated = false;

    private Vector3 endPosition = new Vector3(0, 9.7f, 10f);
    private Vector3 endRotation = new Vector3(50, -180, 0);
    private float animationLength = 5.63f;
    private float pauseBeforeCameraUpdate = 0.35f;

    private void Start()
    {
        endGame = GameObject.FindGameObjectWithTag("Scripts").GetComponent<EndGameConditions>();
    }

    private void Update()
    {
        timer += Time.deltaTime;

        if (timer >= animationLength && !activated)
        {
            activated = true;

            endGame.isPaused = false;
            playMessage.SetActive(false);

            StartCoroutine(CameraUpdate());
        }
    }

    private IEnumerator CameraUpdate()
    {
        StartCoroutine(MoveCamera());
        yield return StartCoroutine(RotateCamera());

        trackers.SetActive(true);
        endGame.isPaused = false;

        enabled = false;
    }

    private IEnumerator MoveCamera()
    {
        yield return new WaitForSeconds(pauseBeforeCameraUpdate);

        Vector3 startPosition = cam.transform.position;
        Vector3 finalPosition = endPosition;

        float positionTimer = 0f;
        while (positionTimer < 1f)
        {
            positionTimer += Time.deltaTime;

            cam.transform.position = Vector3.Lerp(startPosition, finalPosition, positionTimer);

            yield return null;
        }
    }

    private IEnumerator RotateCamera()
    {
        yield return new WaitForSeconds(pauseBeforeCameraUpdate);

        Quaternion startRotation = cam.transform.rotation;
        Quaternion finalRotation = Quaternion.Euler(endRotation);

        float rotationTimer = 0f;
        while (rotationTimer < 1f)
        {
            rotationTimer += Time.deltaTime;

            cam.transform.rotation = Quaternion.Slerp(startRotation, finalRotation, rotationTimer);

            yield return null;
        }
    }
}
