using UnityEngine;

public class AITurretController : MonoBehaviour
{
    [Header("AI Settings")]
    public float detectionRange = 1250f;
    public float shootingCooldown = 4f;
    public LayerMask playerLayer;
    public LayerMask obstacleLayer;
    public string playerTag = "Player";
    
    [Header("Base Turret")]
    public Transform baseTurret;
    public Vector3 initialBaseRotation;
    public bool limitRotation = true;
    [Range(0, 180)]
    public float leftLimit = 60.0f;
    [Range(0, 180)]
    public float rightLimit = 60.0f;

    [Header("Barrel Turret")]
    public Transform barrelTurret;
    public Vector3 initialBarrelRotation;
    [Range(0, 180)]
    public float upLimit = 60.0f;
    [Range(0, 180)]
    public float downLimit = 5.0f;
    public float turnRate = 30f;

    [Header("Shooting Settings")]
    [Tooltip("حداکثر دامنه افست تصادفی جهت شلیک (هر چه مقدار بیشتر، پراکندگی بیشتر)")]
    public float shootingOffsetRange = 5f;
    [Tooltip("تعداد رِی‌کست‌های ارسال شده جهت تشخیص پلیر")]
    public int numberOfRays = 3;
    [Tooltip("حداکثر زاویه انحراف (به درجه) برای رِی‌کست‌ها")]
    public float raySpreadAngle = 5f;

    public Transform player;
    private Vector3 targetPosition;
    private float nextFireTime;
    public bool isTargetInRange;
    public bool hasLineOfSight;
    private AITankController tankController;
    private GunsController gunsController;

    private void Start()
    {
        tankController = GetComponent<AITankController>();
        gunsController = GetComponent<GunsController>();
        if (gunsController)
        {
            gunsController.gunsActive = true;
        }

        player = GameObject.FindGameObjectWithTag(playerTag)?.transform;
        if (!player)
        {
            Debug.LogWarning("Player not found!");
            return;
        }
    }

    private void Update()
    {
        if (!player) return;
        RotateTowardsTarget();
        UpdateTargetStatus();
        
        if (isTargetInRange && hasLineOfSight)
        {
            // جهت توپ به سمت هدف تنظیم می‌شود
            RotateTowardsTarget();
            if (CanShoot())
            {
                Shoot();
            }
        }

        UpdateTankMovement();
    }

    private void UpdateTargetStatus()
    {
        targetPosition = player.position; // به‌روز رسانی موقعیت هدف
        
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);
        isTargetInRange = distanceToPlayer <= detectionRange;
        
        if (isTargetInRange)
        {
            hasLineOfSight = CheckLineOfSight();
            Debug.DrawLine(barrelTurret.position, player.position, hasLineOfSight ? Color.green : Color.red);
        }
    }

    /// <summary>
    /// از چند رِی‌کست با زاویه‌های متفاوت جهت تشخیص پلیر استفاده می‌شود.
    /// استفاده از hit.transform.root تضمین می‌کند که هر بخش فرعی پلیر نیز تشخیص داده شود.
    /// </summary>
    /// <returns>آیا پلیر در دید تانک قرار دارد یا خیر</returns>
    private bool CheckLineOfSight()
    {
        // به ازای تعداد تعیین شده رِی‌کست اجرا می‌شود
        for (int i = 0; i < numberOfRays; i++)
        {
            // محاسبه زاویه افست: اگر i==0 مرکز، اگر i==1 انحراف به سمت راست، اگر i==2 انحراف به سمت چپ و ...
            float angleOffset = 0f;
            if (numberOfRays > 1)
            {
                // توزیع زاویه‌ها از -raySpreadAngle تا +raySpreadAngle
                angleOffset = Mathf.Lerp(-raySpreadAngle, raySpreadAngle, (float)i / (numberOfRays - 1));
            }
            
            Vector3 directionToTarget = (player.position - barrelTurret.position).normalized;
            // چرخش جهت به اندازه افست حول محور Y
            Vector3 rotatedDirection = Quaternion.Euler(0, angleOffset, 0) * directionToTarget;
            
            Ray ray = new Ray(barrelTurret.position, rotatedDirection);
            Debug.DrawRay(barrelTurret.position, rotatedDirection * detectionRange, Color.yellow);
            
            if (Physics.Raycast(ray, out RaycastHit hit, detectionRange, obstacleLayer | playerLayer))
            {
                // استفاده از hit.transform.root برای اطمینان از شناسایی صحیح پلیر
                // if (hit.transform.root == player)
                // {
                    return true;
                // }
            }
        }
        return false;
    }

    private void RotateTowardsTarget()
    {
        // چرخش پایه (Base Turret)
        Vector3 targetPosBase = transform.InverseTransformPoint(targetPosition);
        targetPosBase.y = 0f;
        Vector3 clampedTargetPosBase = targetPosBase;
        if (limitRotation)
        {
            float angle = Vector3.SignedAngle(Vector3.forward, targetPosBase, Vector3.up);
            if (angle >= 0f)
                clampedTargetPosBase = Vector3.RotateTowards(Vector3.forward, targetPosBase, Mathf.Deg2Rad * rightLimit, float.MaxValue);
            else
                clampedTargetPosBase = Vector3.RotateTowards(Vector3.forward, targetPosBase, Mathf.Deg2Rad * leftLimit, float.MaxValue);
        }
        Quaternion baseRotationGoal = Quaternion.LookRotation(clampedTargetPosBase);
        baseTurret.localRotation = Quaternion.RotateTowards(baseTurret.localRotation, baseRotationGoal, turnRate * Time.deltaTime);

        // چرخش توپ (Barrel Turret)
        Vector3 targetPosBarrel = baseTurret.InverseTransformPoint(targetPosition);
        targetPosBarrel.x = 0f;
        Vector3 clampedTargetPosBarrel = targetPosBarrel;
        float verticalAngle = Vector3.SignedAngle(Vector3.forward, targetPosBarrel, Vector3.right);
        if (verticalAngle <= 0f)
            clampedTargetPosBarrel = Vector3.RotateTowards(Vector3.forward, targetPosBarrel, Mathf.Deg2Rad * downLimit, float.MaxValue);
        else
            clampedTargetPosBarrel = Vector3.RotateTowards(Vector3.forward, targetPosBarrel, Mathf.Deg2Rad * upLimit, float.MaxValue);
        Quaternion barrelRotationGoal = Quaternion.LookRotation(clampedTargetPosBarrel);
        barrelTurret.localRotation = Quaternion.RotateTowards(barrelTurret.localRotation, barrelRotationGoal, turnRate * Time.deltaTime);
    }

    /// <summary>
    /// اگر زمان شلیک سپری شده باشد و زاویه هدف نسبت به جهت توپ کمتر از 5 درجه باشد، شلیک انجام می‌شود.
    /// </summary>
    /// <returns></returns>
    private bool CanShoot()
    {
        if (Time.time < nextFireTime)
            return false;
        
        Vector3 directionToTarget = (targetPosition - barrelTurret.position).normalized;
        float angleToTarget = Vector3.Angle(barrelTurret.forward, directionToTarget);
        return angleToTarget < 5f;
    }

    /// <summary>
    /// در متد شلیک، یک افست تصادفی به موقعیت پلیر اضافه می‌شود تا جهت شلیک گاهی دقیق و گاهی با انحراف (به اطراف پلیر) باشد.
    /// </summary>
    private void Shoot()
    {
        if (gunsController)
        {
            // محاسبه افست تصادفی در جهت افقی
            Vector2 randomCircle = Random.insideUnitCircle * shootingOffsetRange;
            Vector3 randomOffset = new Vector3(randomCircle.x, 0, randomCircle.y);
            // نقطه هدف نهایی به همراه افست تصادفی
            Vector3 finalTarget = player.position + randomOffset;
            // محاسبه جهت شلیک از موقعیت توپ به نقطه نهایی
            Vector3 shootDirection = (finalTarget - barrelTurret.position).normalized;
            Quaternion shootRotation = Quaternion.LookRotation(shootDirection);
            
            // تنظیم جهت توپ به سمت shootRotation (می‌توانید این خط را در صورت نیاز حذف کنید یا به عنوان انیمیشن جهت‌دهی استفاده کنید)
            barrelTurret.rotation = Quaternion.RotateTowards(barrelTurret.rotation, shootRotation, turnRate * Time.deltaTime);
            
            gunsController.gunsActive = true;
            gunsController.Shoot();
            nextFireTime = Time.time + shootingCooldown;
        }
    }

    // متدهای مربوط به حرکت تانک
    private GameObject destinationObject;
    private Vector3 lastTargetPosition;
    private float positionUpdateCooldown = 1f;
    private float nextPositionUpdate;
    
    private void UpdateTankMovement()
    {
        if (!tankController || !destinationObject || !player || Time.time < nextPositionUpdate)
            return;

        try
        {
            Vector3 newPosition = player.position;  // تانک همیشه به سمت پلیر حرکت می‌کند
            
            if (isTargetInRange && hasLineOfSight)
            {
                Vector3 directionToPlayer = (player.position - transform.position).normalized;
                float optimalDistance = detectionRange * 0.7f;
                newPosition = player.position - directionToPlayer * optimalDistance;
            }

            destinationObject.transform.position = newPosition;
            tankController.destination = destinationObject.transform;
            nextPositionUpdate = Time.time + positionUpdateCooldown;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error in UpdateTankMovement: {e.Message}");
        }
    }

    private void OnDestroy()
    {
        if (destinationObject != null)
        {
            Destroy(destinationObject);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
    }
}
