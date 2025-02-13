using UnityEngine;

public class VehiclesAntiStuckSystem : MonoBehaviour
{
    public bool enable;

    public Transform wheelModel;
    public int raysNumber = 36;
    public float raysMaxAngle = 180f;
    public float wheelWidth = 0.3f;
    public float correctionSpeed = 10f;

    WheelCollider _wheelCollider;
    float orgRadius;
    AIVehicleController vController;

    void Awake()
    {
        _wheelCollider = GetComponent<WheelCollider>();
        orgRadius = _wheelCollider.radius;
        vController = GetComponentInParent<AIVehicleController>();
    }

    void Update()
    {
        if (!enable)
            return;

        float radiusOffset = 0f;

        for (int i = 0; i <= raysNumber; i++)
        {
            Vector3 rayDirection = Quaternion.AngleAxis(_wheelCollider.steerAngle, transform.up) * Quaternion.AngleAxis(i * (raysMaxAngle / raysNumber) + ((180f - raysMaxAngle) / 2f), transform.right) * transform.up;

            //Center Raycast

            if (Physics.Raycast(wheelModel.position, rayDirection, out RaycastHit hit, _wheelCollider.radius))
            {
                if (!hit.transform.IsChildOf(vController.transform))
                {
                    Debug.DrawLine(wheelModel.position, hit.point, Color.red);
                    radiusOffset = Mathf.Max(radiusOffset, _wheelCollider.radius - hit.distance);
                }
            }

            //Right Raycast

            if (Physics.Raycast(wheelModel.position + wheelModel.right * wheelWidth * .5f, rayDirection, out RaycastHit rightHit, _wheelCollider.radius))
            {
                if (!rightHit.transform.IsChildOf(vController.transform))
                {
                    Debug.DrawLine(wheelModel.position + wheelModel.right * wheelWidth * .5f, rightHit.point, Color.red);
                    radiusOffset = Mathf.Max(radiusOffset, _wheelCollider.radius - rightHit.distance);
                }
            }

            //Left Raycast

            if (Physics.Raycast(wheelModel.position - wheelModel.right * wheelWidth * .5f, rayDirection, out RaycastHit leftHit, _wheelCollider.radius))
            {
                if (!leftHit.transform.IsChildOf(vController.transform))
                {
                    Debug.DrawLine(wheelModel.position - wheelModel.right * wheelWidth * .5f, leftHit.point, Color.red);
                    radiusOffset = Mathf.Max(radiusOffset, _wheelCollider.radius - leftHit.distance);
                }
            }
        }

        _wheelCollider.radius = Mathf.LerpUnclamped(_wheelCollider.radius, orgRadius + radiusOffset, Time.deltaTime * correctionSpeed);
    }
}
