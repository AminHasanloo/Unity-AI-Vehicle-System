using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class wheel
{
	public WheelCollider wheelC;
	public Transform wheelT;
	public bool motorTorque = true;
	public bool steering = true;
	public float steeringAngle = 20;
	public VehiclesAntiStuckSystem antiStuck;
	[Space]
	public float curFriction;
	public float orgRadius;
}

public class VehicleController : MonoBehaviour
{
	public bool controlVehicle;

	public Vector3 centerOfMass = new Vector3(0, -1, 0);
	public wheel[] wheels;

	public float maxBrakeTorque = 1000f;
	public float motorForce = 1500;
	public float topSpeed = 150;
	public float currentSpeed;

	Rigidbody rb;
	private float m_horizontalInput;
	private float m_verticalInput;
	private AudioSource aSource;

	private void Start()
	{
		rb = GetComponent<Rigidbody>();
		rb.centerOfMass = centerOfMass;
		aSource = GetComponent<AudioSource>();

		for (int i = 0; i < wheels.Length; i++)
		{
			wheels[i].antiStuck = wheels[i].wheelC.GetComponent<VehiclesAntiStuckSystem>();
		}
	}

	private void Update()
	{
		if (controlVehicle)
		{
			GetComponent<AudioSource>().enabled = true;
			GetComponent<GunsController>().gunsActive = true;
			// GetComponent<TurretController>().turretActive = true;
		}
		else
		{
			GetComponent<AudioSource>().enabled = false;
			GetComponent<GunsController>().gunsActive = false;
			// GetComponent<TurretController>().turretActive = false;
		}

		if (!controlVehicle)
			return;

		Accelerate();
		EngineSound();
	}

	private void FixedUpdate()
	{
		UpdateWheelPoses();

		if (!controlVehicle)
			return;

		GetInput();
		Steer();
	}

	public void GetInput()
	{
		m_horizontalInput = Input.GetAxis("Horizontal");
		m_verticalInput = Input.GetAxis("Vertical");
	}

	private void Steer()
	{
		for (int i = 0; i < wheels.Length; i++)
		{
			if(wheels[i].steering == true)
			{
				wheels[i].wheelC.steerAngle = wheels[i].steeringAngle * m_horizontalInput;
			}
		}
	}

	private void Accelerate()
	{
		currentSpeed = rb.linearVelocity.magnitude * 3.6f;

		for (int i = 0; i < wheels.Length; i++)
		{
			if (Input.GetKey(KeyCode.Space))
			{
				wheels[i].wheelC.motorTorque = 0;
				wheels[i].wheelC.brakeTorque = maxBrakeTorque;
			}
			else
			{
				wheels[i].wheelC.brakeTorque = 0;

				if (currentSpeed < topSpeed)
					wheels[i].wheelC.motorTorque = m_verticalInput * motorForce;
				else
					wheels[i].wheelC.motorTorque = 0;
			}
		}
	}

	private void UpdateWheelPoses()
	{
		for (int i = 0; i < wheels.Length; i++)
		{
			UpdateWheelPose(wheels[i].wheelC, wheels[i].wheelT);
		}
	}

	private void UpdateWheelPose(WheelCollider _collider, Transform _transform)
	{
		Vector3 _pos = _transform.position;
		Quaternion _quat = _transform.rotation;

		_collider.GetWorldPose(out _pos, out _quat);

		_transform.position = _pos;
		_transform.rotation = _quat;
	}
	private void EngineSound()
	{
		aSource.pitch = 1 + (currentSpeed / topSpeed);
	}
}