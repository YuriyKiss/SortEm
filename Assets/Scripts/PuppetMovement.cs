using UnityEngine;
using System.Collections;
using RootMotion.Dynamics;
using UnityEngine.Animations;

using Input = InputWrapper.Input;

public class PuppetMovement : MonoBehaviour
{
    #region Initializing

    /* Global components */
    private Stash stash;

    /* Puppet personal components */
    private Rigidbody rigid;
    private Animator animator;
    private PuppetMaster puppetMaster;

    /* Puppet personal values */
    [Header("Unique Values")]
    public string color;
    private float timer = 1f;

    [Header("Positioning")]
    public string startAnimaion;
    private string fallingAnimation = "Falling Idle";

    private Vector3 destination = Vector3.zero;
    private Vector3 lastPosition;

    // State booleans
    private bool isSelected = false;
    private bool actionCompleted = false;

    /* These components are set up in inspector */
    [Header("Shared Components")]
    [SerializeField] private GameObject pointHips;
    [SerializeField] private GameObject pointChest;

    [SerializeField] private RotationConstraint rotation;
    [SerializeField] private GameObject smokeParticle;

    void Start()
	{
		stash = GameObject.FindGameObjectWithTag("Scripts").GetComponent<Stash>();

        rigid = GetComponent<Rigidbody>();
        animator = GetComponentInChildren<Animator>();
        puppetMaster = GetComponentInChildren<PuppetMaster>();

        animator.Play(startAnimaion, -1, Random.Range(0, 1f));
    }

    #endregion

    private void FixedUpdate()
    {
        if (isSelected && Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);

            if (touch.phase == TouchPhase.Moved)
            {
                float y = rigid.position.y;

                if (y != stash.desiredYCoord && timer < 0.15f)
                {
                    timer += Time.deltaTime * 0.1f;

                    y = Mathf.Lerp(rigid.position.y, stash.desiredYCoord, timer);
                }

                rigid.MovePosition(new Vector3(
                    Mathf.Clamp(rigid.position.x - touch.deltaPosition.x * 0.006f,
                                stash.playerMovementLimitations.x, stash.playerMovementLimitations.y),
                    y,
                    Mathf.Clamp(rigid.position.z - touch.deltaPosition.y * 0.006f,
                                stash.playerMovementLimitations.z, stash.playerMovementLimitations.w)));
            }

            if (touch.phase == TouchPhase.Ended)
            {
                timer = 0f;

                StartCoroutine(InstantiateSmoke());
                DestroyHand();

                PreparePositioning(-0.55f, 0.55f, 0f);

                isSelected = false;
            }
        }

        if (!isSelected)
        {
            if (timer < 0.5f)
            {
                timer += Time.deltaTime;

                rigid.MovePosition(Vector3.Lerp(lastPosition, destination, timer * 2f));
            }
            else if (!actionCompleted)
            {
                rotation.constraintActive = false;
                UpdateWeights(0.8f, 0.6f, false);
                animator.Play(startAnimaion, -1, Random.Range(0, 1f));

                actionCompleted = true;
            }
        }
    }

    public void SetSelected(GameObject hand, Vector3 position) 
	{
        timer = 0f;

        CreateHand(hand, position);

        rotation.constraintActive = true;
        UpdateWeights(0.3f, 0.2f, true);
        animator.Play(fallingAnimation);

        isSelected = true;
        actionCompleted = false;
    }

    public IEnumerator StartDancing()
    {
        yield return new WaitForSeconds(Random.Range(0, 2f));

        animator.Play("Dancing");
    }

    private void PreparePositioning(float limitLeft, float limitRight, float middlePoint)
    {
        lastPosition = rigid.position;

        // Calculating destination coordinates
        if (lastPosition.x > limitLeft && lastPosition.x < limitRight)
        {
            if (lastPosition.x < middlePoint) 
                destination.x = limitLeft;
            else 
                destination.x = limitRight;
        }
        else
        {
            destination.x = lastPosition.x;
        }

        destination.y = stash.originalYCoord;

        if (lastPosition.z > limitLeft && lastPosition.z < limitRight)
        {
            if (lastPosition.z < middlePoint) 
                destination.z = limitLeft;
            else 
                destination.z = limitRight;
        }
        else
        {
            destination.z = lastPosition.z;
        }
    }

    private IEnumerator InstantiateSmoke()
    {
        GameObject smoke = Instantiate(smokeParticle, transform, true);

        smoke.transform.position += transform.position;

        yield return new WaitForSeconds(1.1f);

        Destroy(smoke);
    }

    #region PuppetMaster
    /* Puppet master muscle weight control */

    private void UpdateWeights(float muscle, float pin, bool points)
    {
        pointChest.SetActive(points);
        pointHips.SetActive(points);

        SetMuscleWeight(muscle);
        SetPinWeight(pin);
    }

    private void SetPinWeight(float weight) =>
        puppetMaster.pinWeight = weight;

    private void SetMuscleWeight(float weight) =>
		puppetMaster.muscleWeight = weight;
    #endregion

    #region HandTracker
    private void CreateHand(GameObject hand, Vector3 position)
    {
        if (hand == null) return;

        hand.transform.position = position + new Vector3(0.2f, -0.20f, 0.25f);
        hand.transform.rotation = Quaternion.Euler(Vector3.right * - Camera.main.transform.rotation.eulerAngles.x);
        Instantiate(hand, gameObject.transform, true);
    }

    private void DestroyHand()
    {
        foreach (GameObject tracker in GameObject.FindGameObjectsWithTag("TutorialHand"))
        {
            if (tracker.GetComponentInParent<PuppetMovement>() == this)
            {
                Destroy(tracker);
            }
        }
    }
    #endregion
}
