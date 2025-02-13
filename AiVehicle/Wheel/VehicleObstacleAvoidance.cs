using UnityEngine;

public class VehicleObstacleAvoidance : MonoBehaviour
{
    [Header("Sensor Settings")]
    public float sensorLength = 20f;
    public float frontSensorStartPoint = 3f;
    public float frontSensorSpacing = 2f;
    public float sideSensorAngle = 45f;
    public int frontSensorCount = 5;
    public LayerMask obstacleLayer;
    
    [Header("Avoidance Settings")]
    public float dangerZone = 5f;
    public float cautionZone = 10f;
    public float avoidanceMultiplier = 1.5f;
    public float smoothing = 5f;
    public float recoverySteerSpeed = 2f;
    public float pathPredictionTime = 1f;
    
    [Header("Advanced Driving")]
    public float overtakeSpeed = 120f;
    public float overtakeDistance = 15f;
    public float racingLineOffset = 3f;
    
    [Header("Debug")]
    public bool showSensorGizmos = true;
    
    private AIVehicleController aiController;
    private float avoidanceSteerAmount;
    private Vector3[] frontSensorPositions;
    private Vector3[] sideSensorPositions;
    private float[] frontSensorDistances;
    private float[] sideSensorDistances;
    private Vector3 predictedPath;
    private bool isOvertaking;
    private float overtakeTimer;

    private void Start()
    {
        aiController = GetComponent<AIVehicleController>();
        
        // Initialize sensor arrays
        frontSensorPositions = new Vector3[frontSensorCount];
        frontSensorDistances = new float[frontSensorCount];
        sideSensorPositions = new Vector3[2];
        sideSensorDistances = new float[2];
        
        UpdateSensorPositions();
    }

    private void Update()
    {
        UpdateSensorPositions();
        CheckObstacles();
        CalculateAvoidanceBehavior();
        HandleOvertaking();
    }

    private void UpdateSensorPositions()
    {
        // Update front sensors
        float frontSpan = frontSensorSpacing * (frontSensorCount - 1);
        float startX = -frontSpan / 2;
        
        for (int i = 0; i < frontSensorCount; i++)
        {
            frontSensorPositions[i] = transform.position + 
                transform.forward * frontSensorStartPoint +
                transform.right * (startX + i * frontSensorSpacing);
        }

        // Update side sensors
        sideSensorPositions[0] = transform.position + transform.right * frontSensorSpacing;
        sideSensorPositions[1] = transform.position - transform.right * frontSensorSpacing;
    }

    private void CheckObstacles()
    {
        predictedPath = Vector3.zero;
        RaycastHit hit;

        // Check front sensors
        for (int i = 0; i < frontSensorCount; i++)
        {
            if (Physics.Raycast(frontSensorPositions[i], transform.forward, 
                out hit, sensorLength, obstacleLayer))
            {
                frontSensorDistances[i] = hit.distance;
                Debug.DrawLine(frontSensorPositions[i], hit.point, Color.red);
                
                // Add to predicted path calculation
                predictedPath += hit.normal * (sensorLength - hit.distance);
            }
            else
            {
                frontSensorDistances[i] = sensorLength;
                Debug.DrawLine(frontSensorPositions[i], 
                    frontSensorPositions[i] + transform.forward * sensorLength, Color.green);
            }
        }

        // Check side sensors
        for (int i = 0; i < 2; i++)
        {
            Vector3 sideDirection = i == 0 ? transform.right : -transform.right;
            Vector3 angleSideDirection = Quaternion.Euler(0, sideSensorAngle * (i == 0 ? 1 : -1), 0) * transform.forward;
            
            // Forward angled sensor
            if (Physics.Raycast(sideSensorPositions[i], angleSideDirection, 
                out hit, sensorLength, obstacleLayer))
            {
                sideSensorDistances[i] = hit.distance;
                Debug.DrawLine(sideSensorPositions[i], hit.point, Color.yellow);
            }
            else
            {
                sideSensorDistances[i] = sensorLength;
                Debug.DrawLine(sideSensorPositions[i], 
                    sideSensorPositions[i] + angleSideDirection * sensorLength, Color.green);
            }
            
            // Direct side sensor
            if (Physics.Raycast(sideSensorPositions[i], sideDirection, 
                out hit, sensorLength / 2, obstacleLayer))
            {
                sideSensorDistances[i] = Mathf.Min(sideSensorDistances[i], hit.distance);
                Debug.DrawLine(sideSensorPositions[i], hit.point, Color.red);
            }
        }
    }

    private void CalculateAvoidanceBehavior()
    {
        float targetSteer = 0;
        bool needsAvoidance = false;

        // Calculate front sensor weights
        for (int i = 0; i < frontSensorCount; i++)
        {
            if (frontSensorDistances[i] <= cautionZone)
            {
                needsAvoidance = true;
                float weight = 1 - (frontSensorDistances[i] / cautionZone);
                float sensorPosition = (i - (frontSensorCount - 1) / 2f) / (frontSensorCount - 1);
                targetSteer -= sensorPosition * weight;
            }
        }

        // Add side sensor influence
        if (sideSensorDistances[0] < sideSensorDistances[1])
        {
            targetSteer -= (1 - (sideSensorDistances[0] / sensorLength)) * 0.5f;
        }
        else if (sideSensorDistances[1] < sideSensorDistances[0])
        {
            targetSteer += (1 - (sideSensorDistances[1] / sensorLength)) * 0.5f;
        }

        // Apply racing line optimization
        if (!needsAvoidance && !isOvertaking)
        {
            Vector3 directionToTarget = (aiController.destination.position - transform.position).normalized;
            float targetAngle = Vector3.SignedAngle(transform.forward, directionToTarget, Vector3.up);
            
            if (Mathf.Abs(targetAngle) > 10f)
            {
                // Apply racing line by slightly cutting corners
                targetSteer += Mathf.Sign(targetAngle) * racingLineOffset * 
                    (1 - Mathf.Abs(targetAngle) / 90f);
            }
        }

        // Smooth the steering
        avoidanceSteerAmount = Mathf.Lerp(avoidanceSteerAmount, targetSteer, Time.deltaTime * smoothing);
    }

    private void HandleOvertaking()
    {
        if (isOvertaking)
        {
            overtakeTimer -= Time.deltaTime;
            if (overtakeTimer <= 0)
            {
                isOvertaking = false;
            }
            return;
        }

        // Check if overtaking is needed
        bool frontBlocked = false;
        bool sidesClear = true;

        // Check front sensors for obstacles
        for (int i = 0; i < frontSensorCount; i++)
        {
            if (frontSensorDistances[i] < overtakeDistance)
            {
                frontBlocked = true;
                break;
            }
        }

        // Check if sides are clear for overtaking
        if (frontBlocked)
        {
            for (int i = 0; i < 2; i++)
            {
                if (sideSensorDistances[i] < overtakeDistance)
                {
                    sidesClear = false;
                    break;
                }
            }

            // Initiate overtaking if conditions are met
            if (sidesClear && aiController.currentSpeed > aiController.normalSpeed * 0.8f)
            {
                // Choose overtaking direction based on sensor data and target position
                Vector3 toTarget = aiController.destination.position - transform.position;
                float rightDot = Vector3.Dot(toTarget.normalized, transform.right);
                
                // Prefer the side that's closer to the target
                float overtakeDirection = rightDot > 0 ? 1 : -1;
                
                // Check if the chosen direction is blocked
                if (overtakeDirection > 0 && sideSensorDistances[0] < overtakeDistance ||
                    overtakeDirection < 0 && sideSensorDistances[1] < overtakeDistance)
                {
                    overtakeDirection = -overtakeDirection;
                }

                isOvertaking = true;
                overtakeTimer = 3.0f; // Duration of overtaking maneuver
                avoidanceSteerAmount = overtakeDirection * 0.8f; // Smooth overtaking steering
            }
        }
    }

    public float GetAvoidanceAmount()
    {
        return avoidanceSteerAmount;
    }

    public bool IsAvoidingObstacle()
    {
        return Mathf.Abs(avoidanceSteerAmount) > 0.1f || isOvertaking;
    }

    private void OnDrawGizmos()
    {
        if (!showSensorGizmos) return;

        // Draw sensor positions
        if (frontSensorPositions != null && sideSensorPositions != null)
        {
            Gizmos.color = Color.cyan;
            
            // Draw front sensors
            foreach (Vector3 sensorPos in frontSensorPositions)
            {
                Gizmos.DrawWireSphere(sensorPos, 0.2f);
            }
            
            // Draw side sensors
            foreach (Vector3 sensorPos in sideSensorPositions)
            {
                Gizmos.DrawWireSphere(sensorPos, 0.2f);
            }

            // Draw avoidance zones
            Gizmos.color = new Color(1, 0, 0, 0.2f);
            Gizmos.DrawWireSphere(transform.position, dangerZone);
            
            Gizmos.color = new Color(1, 0.5f, 0, 0.2f);
            Gizmos.DrawWireSphere(transform.position, cautionZone);

            // Draw predicted path
            if (predictedPath != Vector3.zero)
            {
                Gizmos.color = Color.magenta;
                Gizmos.DrawRay(transform.position, predictedPath.normalized * 5f);
            }

            // Draw overtaking zones if active
            if (isOvertaking)
            {
                Gizmos.color = Color.blue;
                Gizmos.DrawWireSphere(transform.position, overtakeDistance);
            }
        }
    }
}