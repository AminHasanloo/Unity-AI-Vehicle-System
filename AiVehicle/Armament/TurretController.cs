using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TurretController : MonoBehaviour
{
    public bool turretActive;

    [Header("Look")]
    public GameObject cam;
    public Transform cameraTarget;
    private Vector3 lookPos;
    public int lookPosMaxDistance = 20;
    public float turnRate = 30;
    private GameObject lookPosTransform;

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
    [Range(0,180)]
    public float upLimit = 60.0f;
    [Range(0, 180)]
    public float downLimit = 5.0f;

    private void Start()
    {
        lookPosTransform = new GameObject("LookPos");

        cam = GameObject.FindGameObjectWithTag("VehiclesCamera");

        if (cam == null)
            Debug.LogWarning("No Camera Detected !");
    }

    private void FixedUpdate()
    {
        if (!turretActive)
            return;

        LookPos();
        RotateBase();
        RotateBarrel();
    }

    private void RotateBase()
    {
        Vector3 targetPos = transform.InverseTransformPoint(lookPos);
        targetPos.y = 0.0f;

        Vector3 clampedTargetPös = targetPos;

        if(limitRotation)
        {
            if (targetPos.x >= 0.0f)
                clampedTargetPös = Vector3.RotateTowards(Vector3.forward, targetPos, Mathf.Deg2Rad * rightLimit, float.MaxValue);
            else
                clampedTargetPös = Vector3.RotateTowards(Vector3.forward, targetPos, Mathf.Deg2Rad * leftLimit, float.MaxValue);
        }

        Quaternion rotationGoal = Quaternion.LookRotation(clampedTargetPös);
        Quaternion newRotation;

        if (turretActive)
        {
            newRotation = Quaternion.RotateTowards(baseTurret.localRotation, rotationGoal, turnRate * Time.deltaTime);
        }
        else
        {
            newRotation = Quaternion.RotateTowards(baseTurret.localRotation, Quaternion.Euler(initialBaseRotation), turnRate * Time.deltaTime);
        }

        baseTurret.localRotation = newRotation;
    }

    private void RotateBarrel()
    {
        Vector3 targetPos = baseTurret.InverseTransformPoint(lookPos);
        targetPos.x = 0.0f;

        Vector3 clampedTargetPös = targetPos;


        if (targetPos.y <= 0.0f)
            clampedTargetPös = Vector3.RotateTowards(Vector3.forward, targetPos, Mathf.Deg2Rad * downLimit, float.MaxValue);
        else
            clampedTargetPös = Vector3.RotateTowards(Vector3.forward, targetPos, Mathf.Deg2Rad * upLimit, float.MaxValue);

        Quaternion rotationGoal = Quaternion.LookRotation(clampedTargetPös);
        Quaternion newRotation;

        if(turretActive)
        {
            newRotation = Quaternion.RotateTowards(barrelTurret.localRotation, rotationGoal, turnRate * Time.deltaTime);
        }
        else
        {
            newRotation = Quaternion.RotateTowards(barrelTurret.localRotation, Quaternion.Euler(initialBarrelRotation), turnRate * Time.deltaTime);
        }

        barrelTurret.localRotation = newRotation;
    }

    private void LookPos()
    {
        Ray ray = new Ray(cam.transform.position, cam.transform.forward);

        lookPos = ray.GetPoint(lookPosMaxDistance);
        lookPosTransform.transform.position = lookPos;

        Debug.DrawRay(cam.transform.position, cam.transform.forward * lookPosMaxDistance);
    }
}
