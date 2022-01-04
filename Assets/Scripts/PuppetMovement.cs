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
    private EndGameConditions endGame;

    /* Puppet personal components */
    private Rigidbody rigid;
    private BoxCollider coll;
    private Animator animator;
    private PuppetMaster puppetMaster;

    /* Puppet personal values */
    [Header("Unique Values")]
    public string color;

    [Header("Positioning")]
    public string startAnimaion;

    private float walkDelay;
    public bool canWalkAround = false;

    /* State properties */
    private float timer = 0f;

    private bool isSelected = false;
    private bool isDisabled = false;
    private bool isRunning = false;
    private bool isCentered = false;

    /* These components are set up in inspector */
    [Header("Shared Components")]
    [SerializeField] private GameObject pointHips;
    [SerializeField] private GameObject pointChest;

    [SerializeField] private Rigidbody hips;

    [SerializeField] private RotationConstraint rotation;
    [SerializeField] private GameObject smokeParticle;

    /* Constant values */
    private float walkDelayMin = 3f;
    private float walkDelayMax = 10f;

    private float maxDancingDelay = 4f;

    private float movementMultiplier = 0.006f;
    private float puppetInDisabledStateTime = 2f;

    private float flyingMuscleWeight = 0.3f;
    private float flyingPinWeight = 0.2f;
    private float standingMuscleWeight = 0.8f;
    private float standingPinWeight = 0.6f;

    private float smokeDisappearingTime = 1.1f;

    private string runAnimation = "Running";
    private string danceAnimation = "Dancing";
    private string fallingAnimation = "Falling Idle";

    private Vector3 handDisplacement = new Vector3(0.2f, -0.20f, 0.25f);

    void Start()
	{
        GameObject scripts = GameObject.FindGameObjectWithTag("Scripts");

        stash = scripts.GetComponent<Stash>();
        endGame = scripts.GetComponent<EndGameConditions>();

        rigid = GetComponent<Rigidbody>();
        coll = GetComponent<BoxCollider>();
        animator = GetComponentInChildren<Animator>();
        puppetMaster = GetComponentInChildren<PuppetMaster>();

        animator.Play(startAnimaion, -1, Random.Range(0, 1f));

        walkDelay = Random.Range(walkDelayMin, walkDelayMax);
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
                DestroyHand();

                isSelected = false;

                DisablePuppet();
            }
        }

        if (!isSelected)
        {
            timer += Time.deltaTime;

            if (timer >= puppetInDisabledStateTime && isDisabled)
            {
                EnablePuppet();
            }

            if (timer >= walkDelay && !isRunning && canWalkAround && !endGame.isPaused)
            {
                StartCoroutine(RunOnScene());
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
                    DisablePuppet();
                }
            }
        }
    }

    public void SetSelected(GameObject hand, Vector3 position) 
	{
        if (!isDisabled && !endGame.isPaused)
        {
            timer = 0f;

            CreateHand(hand, position);

            rotation.constraintActive = true;
            UpdateWeights(flyingMuscleWeight, flyingPinWeight, true);
            animator.Play(fallingAnimation);

            StartCoroutine(InstantiateSmoke());

            isCentered = false;
            isRunning = false;
            isSelected = true;
        }
    }

    #region IEnumerators

    public IEnumerator StartDancing()
    {
        yield return new WaitForSeconds(Random.Range(0, maxDancingDelay));

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

    #region Running

    private IEnumerator RunOnScene()
    {
        isRunning = true;

        int mode = 0; // 0 = walk to wall, 1 = walk to center

        if (!isCentered)
            mode = Random.Range(0, 2);

        if (mode == 0) isCentered = false;
        else isCentered = true;

        yield return StartCoroutine(RandomiseRotation(mode));

        animator.Play(runAnimation);

        yield return StartCoroutine(StartMovement(mode));

        animator.Play(startAnimaion);

        timer = 0f;
        walkDelay = Random.Range(walkDelayMin, walkDelayMax);

        isRunning = false;
    }

    private IEnumerator RandomiseRotation(int mode)
    {
        float deltaRotation = 0;
        if (mode == 0)
            deltaRotation = Mathf.Sign(Random.Range(-1f, 1f)) * Random.Range(90, 180);

        Quaternion startRotation = transform.rotation;
        Quaternion endRotation = Quaternion.identity;

        if (mode == 0)
            endRotation = Quaternion.Euler(deltaRotation * Vector3.up);
        else if (mode == 1)
            endRotation = Quaternion.Euler(Quaternion.LookRotation((- transform.position + FindZone()).normalized).eulerAngles.y * Vector3.up);

        float rotationTimer = 0f;
        while (rotationTimer < 0.5f && isRunning)
        {
            rotationTimer += Time.deltaTime;

            transform.rotation = Quaternion.Slerp(startRotation, endRotation, rotationTimer * 2);

            yield return null;
        }
    }

    private IEnumerator StartMovement(int mode)
    {
        float speed = 0.05f;

        if (mode == 0)
        {
            while (!Physics.Raycast(transform.position, transform.forward, 0.15f, 1 << 4) && isRunning)
            {
                transform.position += transform.forward * speed;

                if (Physics.Raycast(transform.position, transform.forward, 0.2f, 1 << 6)) break;

                yield return null;
            }
        }
        else if (mode == 1)
        {
            float positionTimer = 0f;

            Vector3 startPosition = transform.position;
            Vector3 finalPosition = FindZone();

            while (Vector3.Distance(transform.position, finalPosition) > 0.4f && isRunning)
            {
                positionTimer += Time.deltaTime;

                transform.position = Vector3.Lerp(startPosition, finalPosition, positionTimer);

                if (Physics.Raycast(transform.position, transform.forward, 0.2f, 1 << 6)) break;

                yield return null;
            }

            if (!isRunning) isCentered = false;
        }
    }

    private Vector3 FindZone()
    {
        foreach (GameObject zone in GameObject.FindGameObjectsWithTag("Zone"))
        {
            ZoneController controller = zone.GetComponent<ZoneController>();

            if (controller.FindCharacter(gameObject))
            {
                return zone.transform.position;
            } 
        }

        return transform.position;
    }

    #endregion

    #region Enable/Disable

    private void DisablePuppet()
    {
        if (!isSelected)
        {
            timer = 0f;

            coll.enabled = false;
            UpdateWeights(flyingMuscleWeight, flyingPinWeight, false);
            puppetMaster.state = PuppetMaster.State.Dead;
            hips.constraints = RigidbodyConstraints.None;

            isRunning = false;
            isDisabled = true;
        }
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
        timer = 0f;

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

    public bool IsRunning() => isRunning;

    public bool IsSelected() => isSelected;

    public bool IsCentered() => isCentered;
    #endregion
}
