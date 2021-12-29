using UnityEngine;

using Input = InputWrapper.Input;

public class GuySelection : MonoBehaviour
{
    private readonly int layerSelectable = 6;
    private readonly float rayLength = 100f;
    
    [SerializeField] private readonly bool handTracker;
    [SerializeField] private GameObject hand;

    private void Start() => 
        hand = handTracker ? hand : null;

    private void Update()
    {
        if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
        {
            Ray raycast = Camera.main.ScreenPointToRay(Input.GetTouch(0).position);

            if (Physics.Raycast(raycast, out RaycastHit raycastHit, rayLength, 1 << layerSelectable, QueryTriggerInteraction.Collide))
            {
                if (raycastHit.collider.CompareTag("Player"))
                {
                    PuppetMovement puppet = raycastHit.collider.GetComponent<PuppetMovement>();

                    puppet.SetSelected(hand, raycastHit.point);
                }
            }
        }
    }
}
