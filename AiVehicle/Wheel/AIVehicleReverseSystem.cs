using UnityEngine;
using System.Collections;

public class VehicleReverseSystem : MonoBehaviour
{
    [Header("Target Tracking")]
    public float targetOvershootDistance = 10f;
    public float turnAroundThreshold = 150f;
    public float backFacingThreshold = 120f;
    public float minTurnAngle = 30f;
    public bool preferReverseTurn = true;
    public float turnCheckRadius = 15f;
    public float backwardDetectionRange = 20f;
    
    [Header("Turn Around Settings")]
    public float quickTurnThreshold = 5f;
    public float turnInPlaceForce = 800f;
    public float turnRecoveryTime = 1.5f;
    
    [Header("Reverse Detection")]
    public float stuckCheckDuration = 2f;
    public float minimumSpeedThreshold = 2f;
    public float stuckAngleThreshold = 45f;
    public float obstacleCheckDistance = 1.5f;
    public LayerMask obstacleLayer;
    
    [Header("Reverse Behavior")]
    public float reverseTime = 2f;
    public float reverseTorque = 1000f;
    public float reverseSteerMultiplier = -1f;
    public float minReverseDistance = 3f;
    public float maxReverseDistance = 8f;
    public float reverseSteerAngle = 25f;

    // Component references
    private AIVehicleController aiController;
    private AIVehicleController vehicleController;
    private VehicleObstacleAvoidance obstacleAvoidance;
    private Rigidbody rb;

    // State tracking
    private float stuckTimer;
    private float reverseTimer;
    private bool isReversing;
    private Vector3 reverseTarget;
    private ReverseState currentState;
    private float turnTimer;

    private enum ReverseState
    {
        Normal,
        Reversing,
        TurningAround,
        WaitingForClearance
    }

    private void Start()
    {
        aiController = GetComponent<AIVehicleController>();
        vehicleController = GetComponent<AIVehicleController>();
        obstacleAvoidance = GetComponent<VehicleObstacleAvoidance>();
        rb = GetComponent<Rigidbody>();
        
        currentState = ReverseState.Normal;
    }

    private void Update()
    {
        switch (currentState)
        {
            case ReverseState.Normal:
                CheckForStuckState();
                break;
                
            case ReverseState.Reversing:
                HandleReversing();
                break;
                
            case ReverseState.TurningAround:
                HandleTurning();
                break;
                
            case ReverseState.WaitingForClearance:
                CheckForClearance();
                break;
        }
    }

    private void CheckForStuckState()
    {
        if (aiController == null || aiController.destination == null) return;

        Vector3 toTarget = aiController.destination.position - transform.position;
        Vector3 forward = transform.forward;
        
        float angleToTarget = Vector3.Angle(forward, toTarget);
        bool isBackFacing = angleToTarget > backFacingThreshold;
        bool isOvershot = toTarget.magnitude > targetOvershootDistance && angleToTarget > turnAroundThreshold;
        
        // Check if we're stuck
        bool isStuck = CheckIfStuck();
        
        if (isStuck || isBackFacing || isOvershot)
        {
            HandleStuckSituation();
        }
    }

    private bool CheckIfStuck()
    {
        float currentSpeed = rb.linearVelocity.magnitude;
        
        if (currentSpeed < minimumSpeedThreshold)
        {
            stuckTimer += Time.deltaTime;
            if (stuckTimer >= stuckCheckDuration)
            {
                return true;
            }
        }
        else
        {
            stuckTimer = 0;
        }
        
        // Check for obstacles in multiple directions
        bool frontBlocked = Physics.Raycast(transform.position, transform.forward, 
            obstacleCheckDistance, obstacleLayer);
        bool leftBlocked = Physics.Raycast(transform.position, -transform.right, 
            obstacleCheckDistance, obstacleLayer);
        bool rightBlocked = Physics.Raycast(transform.position, transform.right, 
            obstacleCheckDistance, obstacleLayer);

        return frontBlocked && (leftBlocked || rightBlocked);
    }

    private void HandleStuckSituation()
    {
        // Check if we can do a quick turn based on speed
        if (rb.linearVelocity.magnitude < quickTurnThreshold && HasSpaceForTurn())
        {
            StartTurnAround();
        }
        else
        {
            StartReverse();
        }
    }

    private bool HasSpaceForTurn()
    {
        float checkRadius = turnCheckRadius * 0.7f;
        
        // Check in multiple directions for obstacles
        for (float angle = 0; angle < 360; angle += 45)
        {
            Vector3 direction = Quaternion.Euler(0, angle, 0) * transform.forward;
            if (Physics.SphereCast(transform.position, 2f, direction, out RaycastHit hit, 
                checkRadius, obstacleLayer))
            {
                return false;
            }
        }
        
        return true;
    }

    private void StartTurnAround()
    {
        currentState = ReverseState.TurningAround;
        turnTimer = turnRecoveryTime;
        
        // Calculate turn direction based on target position
        Vector3 toTarget = aiController.destination.position - transform.position;
        float turnDirection = Mathf.Sign(Vector3.Dot(toTarget, transform.right));
        
        // Apply initial turn forces
        foreach (wheel wheel in vehicleController.wheels)
        {
            if (wheel.motorTorque)
            {
                if (turnDirection > 0)
                {
                    wheel.wheelC.motorTorque = wheel.wheelT.localPosition.x > 0 ? 
                        -turnInPlaceForce : turnInPlaceForce;
                }
                else
                {
                    wheel.wheelC.motorTorque = wheel.wheelT.localPosition.x < 0 ? 
                        -turnInPlaceForce : turnInPlaceForce;
                }
            }
        }
    }

    private void StartReverse()
    {
        currentState = ReverseState.Reversing;
        isReversing = true;
        reverseTimer = reverseTime;
        
        // Calculate reverse target
        Vector3 reverseDirection = -transform.forward;
        RaycastHit hit;
        float reverseDistance = maxReverseDistance;
        
        if (Physics.Raycast(transform.position, -transform.forward, out hit, 
            maxReverseDistance, obstacleLayer))
        {
            reverseDistance = Mathf.Max(hit.distance - 1f, minReverseDistance);
        }
        
        reverseTarget = transform.position + reverseDirection * reverseDistance;
    }

    private void HandleReversing()
    {
        if (!isReversing) return;
        
        reverseTimer -= Time.deltaTime;
        if (reverseTimer <= 0 || Vector3.Distance(transform.position, reverseTarget) < 1f)
        {
            CompleteReverse();
            return;
        }

        // Apply reverse movement
        foreach (wheel wheel in vehicleController.wheels)
        {
            if (wheel.motorTorque)
            {
                wheel.wheelC.motorTorque = -reverseTorque;
            }
        }

        // Calculate and apply steering while reversing
        Vector3 toTarget = aiController.destination.position - transform.position;
        float targetAngle = Vector3.SignedAngle(transform.forward, toTarget, Vector3.up);
        float reverseSteer = Mathf.Clamp(targetAngle / stuckAngleThreshold * reverseSteerAngle, 
            -reverseSteerAngle, reverseSteerAngle) * reverseSteerMultiplier;

        foreach (wheel wheel in vehicleController.wheels)
        {
            if (wheel.steering)
            {
                wheel.wheelC.steerAngle = reverseSteer;
            }
        }
    }

    private void HandleTurning()
    {
        turnTimer -= Time.deltaTime;
        if (turnTimer <= 0)
        {
            CompleteTurn();
            return;
        }

        Vector3 toTarget = aiController.destination.position - transform.position;
        float angleToTarget = Vector3.Angle(transform.forward, toTarget);
        
        if (angleToTarget < minTurnAngle)
        {
            CompleteTurn();
            return;
        }

        // Continue applying turn forces
        float turnDirection = Mathf.Sign(Vector3.Dot(toTarget, transform.right));
        foreach (wheel wheel in vehicleController.wheels)
        {
            if (wheel.motorTorque)
            {
                wheel.wheelC.motorTorque = wheel.wheelT.localPosition.x * turnDirection * 
                    turnInPlaceForce;
            }
        }
    }

    private void CompleteReverse()
    {
        isReversing = false;
        currentState = ReverseState.WaitingForClearance;
        
        // Reset wheel controls
        foreach (wheel wheel in vehicleController.wheels)
        {
            if (wheel.motorTorque)
            {
                wheel.wheelC.motorTorque = 0;
                wheel.wheelC.brakeTorque = vehicleController.maxBrakeTorque;
            }
        }
    }

    private void CompleteTurn()
    {
        currentState = ReverseState.Normal;
        
        // Reset wheel controls
        foreach (wheel wheel in vehicleController.wheels)
        {
            if (wheel.motorTorque)
            {
                wheel.wheelC.motorTorque = 0;
                wheel.wheelC.brakeTorque = 0;
            }
        }
    }

    private void CheckForClearance()
    {
        // Check if path ahead is clear
        if (!Physics.Raycast(transform.position, transform.forward, 
            obstacleCheckDistance, obstacleLayer))
        {
            currentState = ReverseState.Normal;
        }
    }

    public bool IsInReverseMode()
    {
        return currentState != ReverseState.Normal;
    }

    private void OnDrawGizmos()
    {
        // Draw reverse target when reversing
        if (isReversing)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(reverseTarget, 1f);
            Gizmos.DrawLine(transform.position, reverseTarget);
        }

        // Draw detection rays
        Gizmos.color = Color.yellow;
        Gizmos.DrawRay(transform.position, transform.forward * obstacleCheckDistance);
        Gizmos.DrawRay(transform.position, -transform.right * obstacleCheckDistance);
        Gizmos.DrawRay(transform.position, transform.right * obstacleCheckDistance);
    }
}