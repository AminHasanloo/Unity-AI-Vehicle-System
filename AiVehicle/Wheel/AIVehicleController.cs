using UnityEngine;
using UnityEngine.AI;

public class AIVehicleController : MonoBehaviour
{
    [Header("Vehicle Physics")]
    public Vector3 centerOfMass = new Vector3(0, -1, 0);
    public wheel[] wheels;
    public float maxBrakeTorque = 1000f;
    public float motorForce = 1500;
    public float currentSpeed;

    [Header("AI Navigation")]
    public Transform destination;
    public float detectionRange = 50f;
    public float targetReachedDistance = 5f;
    public float maxSteerAngle = 35f;
    public float steeringSensitivity = 2f;
    public LayerMask obstacleLayer;
    
    [Header("Speed Settings")]
    public float normalSpeed = 100f;
    public float boostSpeed = 150f;
    public float corneringSpeed = 70f;
    public float brakingDistance = 15f;

    private NavMeshAgent navAgent;
    private Rigidbody rb;
    private AudioSource aSource;
    private VehicleObstacleAvoidance obstacleAvoidance;
    private float targetSteerAngle;
    private Vector3 previousPosition;
    private bool isCorneringLeft;
    private bool isCorneringRight;

    private void Start()
    {
        // Initialize components
        rb = GetComponent<Rigidbody>();
        rb.centerOfMass = centerOfMass;
        aSource = GetComponent<AudioSource>();
        obstacleAvoidance = GetComponent<VehicleObstacleAvoidance>();
        
        navAgent = gameObject.AddComponent<NavMeshAgent>();
        navAgent.enabled = false;

        // Initialize wheels anti-stuck system
        for (int i = 0; i < wheels.Length; i++)
        {
            wheels[i].antiStuck = wheels[i].wheelC.GetComponent<VehiclesAntiStuckSystem>();
            if(wheels[i].antiStuck != null)
                wheels[i].antiStuck.enable = true;
        }

        previousPosition = transform.position;
        SetDestination();
    }

    private void SetDestination()
    {
        if (destination == null) return;
        
        navAgent.enabled = true;
        NavMeshHit hit;
        
        if (NavMesh.SamplePosition(destination.position, out hit, 100f, NavMesh.AllAreas))
        {
            NavMeshPath path = new NavMeshPath();
            if (NavMesh.CalculatePath(transform.position, hit.position, NavMesh.AllAreas, path))
            {
                destination.position = hit.position;
            }
        }
        
        navAgent.enabled = false;
    }

    private void Update()
    {
        if (destination == null) return;

        UpdateVehicleMovement();
        EngineSound();
    }

    private void FixedUpdate()
    {
        UpdateWheelPoses();
    }

    private void UpdateVehicleMovement()
    {
        Vector3 directionToTarget = (destination.position - transform.position).normalized;
        float distanceToTarget = Vector3.Distance(transform.position, destination.position);
        
        // Calculate target angle and steering
        float targetAngle = Mathf.Atan2(directionToTarget.x, directionToTarget.z) * Mathf.Rad2Deg;
        float currentAngle = transform.eulerAngles.y;
        float angleDifference = Mathf.DeltaAngle(currentAngle, targetAngle);

        // Check if obstacle avoidance is active
        bool isAvoiding = obstacleAvoidance != null && obstacleAvoidance.IsAvoidingObstacle();
        float avoidanceAmount = isAvoiding ? obstacleAvoidance.GetAvoidanceAmount() : 0f;

        // Update steering based on situation
        if (isAvoiding)
        {
            targetSteerAngle = maxSteerAngle * avoidanceAmount;
            isCorneringLeft = avoidanceAmount < 0;
            isCorneringRight = avoidanceAmount > 0;
        }
        else
        {
            targetSteerAngle = Mathf.Clamp(angleDifference * steeringSensitivity, -maxSteerAngle, maxSteerAngle);
            isCorneringLeft = angleDifference < -20;
            isCorneringRight = angleDifference > 20;
        }

        // Apply steering to wheels
        foreach (wheel wheel in wheels)
        {
            if (wheel.steering)
            {
                wheel.wheelC.steerAngle = targetSteerAngle;
            }
        }

        // Calculate target speed based on situation
        float targetSpeed = normalSpeed;
        
        if (isAvoiding)
        {
            targetSpeed = corneringSpeed;
        }
        else if (distanceToTarget > detectionRange && Mathf.Abs(angleDifference) < 10f)
        {
            targetSpeed = boostSpeed;
        }
        else if (distanceToTarget < brakingDistance)
        {
            targetSpeed = normalSpeed * (distanceToTarget / brakingDistance);
        }
        else if (isCorneringLeft || isCorneringRight)
        {
            targetSpeed = corneringSpeed;
        }

        // Apply motor forces
        currentSpeed = rb.linearVelocity.magnitude * 3.6f;
        
        foreach (wheel wheel in wheels)
        {
            if (wheel.motorTorque)
            {
                if (currentSpeed < targetSpeed)
                {
                    wheel.wheelC.brakeTorque = 0;
                    wheel.wheelC.motorTorque = motorForce;
                }
                else
                {
                    wheel.wheelC.motorTorque = 0;
                    wheel.wheelC.brakeTorque = maxBrakeTorque * 0.3f; // Soft braking
                }
            }
        }

        // Check if destination reached
        if (distanceToTarget <= targetReachedDistance)
        {
            // Implement destination reached behavior here
            Debug.Log("Destination reached!");
        }
    }

    private void UpdateWheelPoses()
    {
        foreach (wheel wheel in wheels)
        {
            Vector3 _pos = wheel.wheelT.position;
            Quaternion _quat = wheel.wheelT.rotation;

            wheel.wheelC.GetWorldPose(out _pos, out _quat);

            wheel.wheelT.position = _pos;
            wheel.wheelT.rotation = _quat;
        }
    }

    private void EngineSound()
    {
        if (aSource != null)
        {
            aSource.pitch = 1 + (currentSpeed / boostSpeed);
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (destination != null)
        {
            // Draw path to destination
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(transform.position, destination.position);
            
            // Draw detection ranges
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(transform.position, detectionRange);
            
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, brakingDistance);
        }
    }
}