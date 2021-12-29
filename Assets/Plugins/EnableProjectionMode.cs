using UnityEngine;

public class EnableProjectionMode : MonoBehaviour
{
    private void Start()
    {
        foreach (GameObject p in GameObject.FindGameObjectsWithTag("Player"))
            foreach (ConfigurableJoint j in p.GetComponentsInChildren<ConfigurableJoint>())
                if (j.name != "PointHips" && j.name != "PointChest")
                    j.projectionMode = JointProjectionMode.PositionAndRotation;
    }
}
