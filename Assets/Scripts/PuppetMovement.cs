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
    private BoxCollider coll;
    private Animator animator;
    private PuppetMaster puppetMaster;

    /* These components are set up in inspector */
    [Header("Shared Components")]
    [SerializeField] private GameObject pointHips;
    [SerializeField] private GameObject pointChest;

    [SerializeField] private Rigidbody hips;

    [SerializeField] private RotationConstraint rotation;
    [SerializeField] private GameObject smokeParticle;

    /* Puppet personal values */
    [Header("Unique Values")]
    public string color;

    [Header("Positioning")]
    public string startAnimaion;
    public bool canWalkAround = false;

    /* State properties */
    private float timer = 0f;

    private bool isSelected = false;
    private bool isDisabled = false;

    /* Constant values */
    private float walkDelay;

    private float movementMultiplier = 0.006f;
    private float puppetInDisabledStateTime = 3f;

    private float flyingMuscleWeight = 0.3f;
    private float flyingPinWeight = 0.2f;
    private float standingMuscleWeight = 0.8f;
    private float standingPinWeight = 0.6f;

    private float smokeDisappearingTime = 1.1f;

    private string danceAnimation = "Dancing";
    private string fallingAnimation = "Falling Idle";

    private Vector3 handDisplacement = new Vector3(0.2f, -0.20f, 0.25f);

    void Start()
	{
		stash = GameObject.FindGameObjectWithTag("Scripts").GetComponent<Stash>();

        rigid = GetComponent<Rigidbody>();
        coll = GetComponent<BoxCollider>();
        animator = GetComponentInChildren<Animator>();
        puppetMaster = GetComponentInChildren<PuppetMaster>();

        animator.Play(startAnimaion, -1, Random.Range(0, 1f));

        walkDelay = Random.Range(3f, 8f);
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

                if (y != stash.desiredYCoord)
                {
                    timer += Time.deltaTime;

                    y = Mathf.Lerp(stash.originalYCoord, stash.desiredYCoord, timer);
                }

                rigid.MovePosition(new Vector3(
                    Mathf.Clamp(rigid.position.x - touch.deltaPosition.x * movementMultiplier,
                                stash.playerMovementLimitations.x, stash.playerMovementLimitations.y),
                    y,
                    Mathf.Clamp(rigid.position.z - touch.deltaPosition.y * movementMultiplier,
                                stash.playerMovementLimitations.z, stash.playerMovementLimitations.w)));
            }

            if (touch.phase == TouchPhase.Ended)
            {
                timer = 0f;

                StartCoroutine(InstantiateSmoke());
                DestroyHand();

                DisablePuppet();

                isSelected = false;
            }
        }

        if (!isSelected)
        {
            timer += Time.deltaTime;

            if (timer >= puppetInDisabledStateTime && isDisabled)
            {
                EnablePuppet();
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.name == hips.name)
        {
            PuppetMovement puppet = other.GetComponentInParent<PuppetMovement>();

            if (puppet != this && !isDisabled)
            {
                if (puppet.IsDisabled())
                {
                    timer = 1f;

                    DisablePuppet();
                }
            }
        }
    }

    public void SetSelected(GameObject hand, Vector3 position) 
	{
        if (!isDisabled)
        {
            timer = 0f;

            CreateHand(hand, position);

            rotation.constraintActive = true;
            UpdateWeights(flyingMuscleWeight, flyingPinWeight, true);
            animator.Play(fallingAnimation);

            isSelected = true;
        }
    }

    #region IEnumerators

    public IEnumerator StartDancing()
    {
        yield return new WaitForSeconds(Random.Range(0, 2f));

        animator.Play(danceAnimation);
    }

    private IEnumerator InstantiateSmoke()
    {
        GameObject smoke = Instantiate(smokeParticle, transform, true);

        smoke.transform.position += transform.position;

        yield return new WaitForSeconds(smokeDisappearingTime);

        Destroy(smoke);
    }

    #endregion

    #region PuppetActivity

    private void DisablePuppet()
    {
        coll.enabled = false;
        UpdateWeights(flyingMuscleWeight, flyingPinWeight, false);
        puppetMaster.state = PuppetMaster.State.Dead;
        hips.constraints = RigidbodyConstraints.None;

        isDisabled = true;
    }

    private Vector3 PreparePosition(float limitLeft, float limitRight, float middlePoint)
    {
        Vector3 result = Vector3.zero;

        if (hips.position.x > limitLeft && hips.position.x < limitRight)
        {
            if (hips.position.x < middlePoint)
                result.x = limitLeft;
            else
                result.x = limitRight;
        }
        else
        {
            result.x = hips.position.x;
        }

        result.y = stash.originalYCoord;

        if (hips.position.z > limitLeft && hips.position.z < limitRight)
        {
            if (hips.position.z < middlePoint)
                result.z = limitLeft;
            else
                result.z = limitRight;
        }
        else
        {
            result.z = hips.position.z;
        }

        return result;
    }

    private void EnablePuppet()
    {
        rigid.MovePosition(PreparePosition(-0.5f, 0.5f, 0));
        puppetMaster.state = PuppetMaster.State.Alive;
        UpdateWeights(standingMuscleWeight, standingPinWeight, false);
        hips.transform.rotation = gameObject.transform.rotation;
        hips.constraints = RigidbodyConstraints.FreezeRotation;
        rotation.constraintActive = false;
        animator.Play(startAnimaion, -1, Random.Range(0, 1f));
        coll.enabled = true;

        isDisabled = false;
    }

    #endregion

    #region PuppetMaster

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

        hand.transform.position = position + handDisplacement;
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

    #region Booleans
    public bool IsDisabled() => isDisabled;
    #endregion
}
