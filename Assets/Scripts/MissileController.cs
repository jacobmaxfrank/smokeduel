using UnityEngine;
using System.Collections;

/// <summary>
/// Missile logic
/// </summary>
public class MissileController : MonoBehaviour
{
	public float _initialVelocity;

	public float _thrustStartDelay, _thrustEndDelay, _activationDelay, _autoDetonateDelay,	// seconds
				 _thrustForce;
	private float m_thrustStart, m_thrustEnd, m_activationReady, m_autoDetonate;			// seconds

	public CFDController _CFD;
	public PlayerController _firer;

	public float _smokeAmount;

	/// <summary>
	/// Initialization
	/// </summary>
	void Start ()
	{
		// Setup thrust timing
		m_thrustStart = Time.time + _thrustStartDelay;
		m_thrustEnd = Time.time + _thrustEndDelay;
		m_activationReady = Time.time + _activationDelay;
		m_autoDetonate = Time.time + _autoDetonateDelay;
	}

	/// <summary>
	/// Update once per physics timestep
	/// </summary>
	void FixedUpdate()
	{
		if (Time.time >= m_thrustStart && Time.time <= m_thrustEnd)
		{
			// Get quaternion representing heading
			Quaternion rotation = Quaternion.Euler(0f, 0f, rigidbody2D.transform.eulerAngles.z);
			
			// Get look vector from heading quat and apply thrust along it
			Vector3 forward = rotation * new Vector3 (1f, 0f, 0f);
			forward.Normalize();
			rigidbody2D.AddForce(forward * _thrustForce);
		}
	}

	/// <summary>
	/// Update once per frame (handle user input)
	/// </summary>
	void Update ()
	{
		if ((Input.GetAxisRaw("Missile") == 1 && Time.time >= m_activationReady) || Time.time >= m_autoDetonate)
			Detonate();
	}

	private void Detonate()
	{
		_firer._firedMissile = null;
		int cfd_x, cfd_y;
		_CFD.WorldToGrid (rigidbody2D.transform.position, out cfd_x, out cfd_y);
		_CFD.AddDensityAt(_smokeAmount, cfd_x, cfd_y);
		Destroy (gameObject);
	}
}
