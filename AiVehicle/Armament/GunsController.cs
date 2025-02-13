using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GunsController : MonoBehaviour
{
    [Header("Bullets Actions")]
	public bool gunsActive;
    public int currentBullets;
    public int maxBulletsPerMag = 100;
    public int bulletsLeft = 300;
    public float fireRate = 0.17f;
	
    [Header("Audio Actions")]
    public AudioClip shotSound;
    public AudioSource barrelSource;
    public float minPitch = .9f;
    public float maxPitch = 1.1f;
	
    [Header("Recoil Actions")]
    public float recoilForce;
    public Transform recoilPosition;
	
    [Header("Shot Actions")]
    public Vector3 randomRotation = new Vector3(.1f, .1f, .1f);
    public GameObject ShellPrefab;
    public float shellVelocity = 500f;
    public GameObject hitPrefab;
    public int hitDestroyTime = 10;
    public ParticleSystem muzzleFlash;
    public Transform ejectPoint;
	
    [Header("Barrel Recoil Actions")]
    public Transform recoilBarrelTransform;
    public Vector3 kickBackRecoilBarrel;
    public float kickBackSpeed = 8f;
    public float barrelReturnSpeed = 18f;

    private float fireTimer;
    private Vector3 positionRecoil;
    private Rigidbody rb;

    private void Start()
    {
        currentBullets = maxBulletsPerMag;
        rb = GetComponent<Rigidbody>();
        fireTimer = fireRate;
    }

    private void Update()
    {
        if (!gunsActive)
            return;

        // if (Input.GetKey(KeyCode.Mouse0) && gunsActive)
        // {
        //     Shoot();
        // }

        if (fireTimer < fireRate)
            fireTimer += Time.deltaTime;

        Debug.DrawRay(ejectPoint.position, ejectPoint.forward * 200);
    }

    private void FixedUpdate()
    {
        if(recoilBarrelTransform)
            Recoil();
    }

    private void Recoil()
    {
        positionRecoil = Vector3.Slerp(positionRecoil, Vector3.zero, barrelReturnSpeed * Time.deltaTime);
        recoilBarrelTransform.localPosition = Vector3.Slerp(recoilBarrelTransform.localPosition, -positionRecoil, kickBackSpeed * Time.fixedDeltaTime);
    }

    public void Shoot()
    {
        if (fireTimer < fireRate || currentBullets <= 0)
            return;

        currentBullets--;

        positionRecoil += kickBackRecoilBarrel;

        if(ShellPrefab != null)
        {
            ejectPoint.localEulerAngles = new Vector3(Random.Range(-randomRotation.x, randomRotation.x), Random.Range(-randomRotation.y, randomRotation.y), Random.Range(-randomRotation.z, randomRotation.z));

            GameObject shell = Instantiate(ShellPrefab, ejectPoint.position, ejectPoint.rotation) as GameObject;

            Projectile projectile = shell.GetComponent<Projectile>();
            projectile.hitPrefab = hitPrefab;
            projectile.hitDestroyTime = 10;

            Rigidbody shellBody = shell.GetComponent<Rigidbody>();
            shellBody.AddForce(ejectPoint.forward * shellVelocity, ForceMode.Impulse);
        }
        else
        {
            Debug.LogWarning("Please attribute a shell prefab !");
        }

        if(muzzleFlash)
            muzzleFlash.Play();

        barrelSource.pitch = Random.Range(minPitch, maxPitch);
        barrelSource.PlayOneShot(shotSound);

        rb.AddForceAtPosition(recoilPosition.forward * recoilForce, recoilPosition.position, ForceMode.Impulse);

        fireTimer = 0.0f;
    }
}
