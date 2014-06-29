﻿using UnityEngine;
using System.Collections;

/// <summary>
/// Player ship logic
/// </summary>
public class PlayerController : MonoBehaviour
{
	// Graphics
	public Texture _tex;

	// Maneuvering
	public float _thrustFactor, _turnRate;

	// Missile
	public GameObject _missileToClone, _mineToClone;
	public Transform _missileTransform, _mineTransform;
	public float _missileFireRate,			// seconds
				 _missileLaunchVelocity;
	public float _mineFireRate,			// seconds
				 _mineLaunchVelocity;
	private float m_missileNextFireTime, m_mineNextFireTime;
	public GameObject _firedMissile, _firedMine;

	// Torus wrapping
	private Vector2 m_canvasBounds = new Vector2(-1f, -1f);
	public GameObject _torusHorizontal, _torusVertical, _torusCorner;
	private static readonly Vector3 OFFSCREEN = new Vector3(-1000f, 0f, 0f);
	public int _horizontalWrap = 0, _verticalWrap = 0;

	public CFDController _CFD;

	public float damage;

	/// <summary>
	/// Initialization
	/// </summary>
	void Start ()
	{
		// Save world bounding box size for torus wrapping
		object[] obj = GameObject.FindObjectsOfType(typeof (GameObject));
		foreach (object o in obj)
		{
			GameObject g = (GameObject) o;
			if (g.name.Equals("Bounds"))
			{
				BoxCollider2D bounds_collider = (BoxCollider2D)g.collider2D;
				m_canvasBounds = new Vector2(bounds_collider.size.x * bounds_collider.transform.localScale.x,
				                             bounds_collider.size.y * bounds_collider.transform.localScale.y);
				break;
			}
		}
		if (m_canvasBounds.x == -1f)
			throw new UnityException("Unable to locate world bounding box");

		GetComponent<DamageCounter>().controller = this;
	}

	[RPC]
	public void SetUpPlayer(string playerName, NetworkViewID horizontalID, NetworkViewID verticalID, NetworkViewID cornerID) {
		gameObject.name = playerName;
		_CFD = GameObject.Find("CFD").GetComponent<CFDController>();

		Color color = Color.white;
		//enemy is red/black TODO server is blue, client is red/black TODO missiles are same color
		if (gameObject.networkView.owner != Network.player)
			color = Color.red;

		GetComponent<SpriteRenderer>().color = color;
		GetComponentInChildren<Shield>().controller = this;

		_torusHorizontal = NetworkView.Find(horizontalID).gameObject;
		_torusHorizontal.GetComponent<SpriteRenderer>().color = color;
		_torusHorizontal.name = playerName + " horizontal";
		_torusHorizontal.GetComponent<DamageCounter>().controller = this;
		_torusHorizontal.GetComponentInChildren<Shield>().controller = this;

		_torusVertical = NetworkView.Find(verticalID).gameObject;
		_torusVertical.GetComponent<SpriteRenderer>().color = color;
		_torusVertical.name = playerName + " vertical";
		_torusVertical.GetComponent<DamageCounter>().controller = this;
		_torusVertical.GetComponentInChildren<Shield>().controller = this;

		_torusCorner = NetworkView.Find(cornerID).gameObject;
		_torusCorner.GetComponent<SpriteRenderer>().color = color;
		_torusCorner.name = playerName + " horizontal";
		_torusCorner.GetComponent<DamageCounter>().controller = this;
		_torusCorner.GetComponentInChildren<Shield>().controller = this;

	}

	/// <summary>
	/// Update once per frame (handle user input)
	/// </summary>
	void Update ()
	{
		if (! gameObject.networkView.isMine)
			return;

		if (Input.GetAxisRaw("Missile") == 1)
		{
			if (_firedMissile == null)
			{
				if (Time.time > m_missileNextFireTime) // TODO: BUG > Need to bump this when missile detonation occurs?  Check original code.
				{
					// Reset fire rate counter and initialize missile
					m_missileNextFireTime = Time.time + _missileFireRate;
					_firedMissile = Network.Instantiate(_missileToClone, _missileTransform.position, _missileTransform.rotation, 0) as GameObject;
					
					// Set velocity to be the ship's velocity plus the launch velocity along look vector, and set hooks to CFD and self
					_firedMissile.rigidbody2D.velocity = rigidbody2D.velocity + ForwardVec2() * _missileLaunchVelocity;
					MissileController mc = _firedMissile.GetComponent<MissileController>();
					mc._CFD = _CFD;
					mc._firer = this;
				}
			}
		}

		if (Input.GetAxisRaw("Mine") == 1)
		{
			if (_firedMine == null)
			{
				if (Time.time > m_mineNextFireTime) // TODO: BUG > Need to bump this when missile detonation occurs?  Check original code.
				{
					// Reset fire rate counter and initialize mine
					m_mineNextFireTime = Time.time + _mineFireRate;
					_firedMine = Network.Instantiate(_mineToClone, _mineTransform.position, _mineTransform.rotation, 0) as GameObject;
					
					// Set velocity to be the ship's velocity plus the launch velocity along look vector, and set hooks to CFD and self
					_firedMine.rigidbody2D.velocity = rigidbody2D.velocity + ForwardVec2() * _mineLaunchVelocity;
					MineController mc = _firedMine.GetComponent<MineController>();
					mc._CFD = _CFD;
					mc._firer = this;
				}
			}
		}

	}

	/// <summary>
	/// Single physics timestep update
	/// </summary>
	void FixedUpdate()
	{
		if (gameObject.networkView.isMine) {
			// Apply thrust along look vector
			Vector3 forward = ForwardVec3 ();
			float thrust = Input.GetAxisRaw ("Thrust");
			rigidbody2D.AddForce(new Vector2(forward.x, forward.y) * thrust * _thrustFactor);

			// Directly set angular velocity (no angular acceleration/force, though adding that that might add to gameplay)
			float turn = Input.GetAxisRaw ("Turn");
			rigidbody2D.angularVelocity = _turnRate * turn;
		}

		UpdateTorusClones ();
	}

	void OnSerializeNetworkView(BitStream stream, NetworkMessageInfo info) {
		stream.Serialize(ref damage);
	}

	/// <summary>
	/// Update the position of edge/corner torus clones
	/// </summary>
	private void UpdateTorusClones()
	{
		switch (_horizontalWrap)
		{
		case -1:	// left edge
			_torusHorizontal.transform.eulerAngles = rigidbody2D.transform.eulerAngles;
			_torusHorizontal.transform.position = new Vector3(transform.position.x - m_canvasBounds.x, transform.position.y, 0f);
			break;
		case 1:		// right edge
			_torusHorizontal.transform.eulerAngles = rigidbody2D.transform.eulerAngles;
			_torusHorizontal.transform.position = new Vector3(transform.position.x + m_canvasBounds.x, transform.position.y, 0f);
			break;
		default:	// neither horizontal edge
			_torusHorizontal.transform.position = OFFSCREEN;
			break;
		}

		switch (_verticalWrap)
		{
		case -1:	// top edge
			_torusVertical.transform.eulerAngles = rigidbody2D.transform.eulerAngles;
			_torusVertical.transform.position = new Vector3(transform.position.x, transform.position.y - m_canvasBounds.y, 0f);
			break;
		case 1:		// bottom edge
			_torusVertical.transform.eulerAngles = rigidbody2D.transform.eulerAngles;
			_torusVertical.transform.position = new Vector3(transform.position.x, transform.position.y + m_canvasBounds.y, 0f);
			break;
		default:	// neither vertical edge
			_torusVertical.transform.position = OFFSCREEN;
			break;
		}

		if (_horizontalWrap != 0 && _verticalWrap != 0)	// corner
		{
			_torusCorner.transform.eulerAngles = rigidbody2D.transform.eulerAngles;
			_torusCorner.transform.position = new Vector3(transform.position.x + m_canvasBounds.x * _horizontalWrap, transform.position.y + m_canvasBounds.y * _verticalWrap, 0f);
		}
		else
			_torusCorner.transform.position = OFFSCREEN;
	}

	/// <summary>
	/// Get look/forward vector as a Vector2
	/// </summary>
	private Vector2 ForwardVec2()
	{
		Quaternion rotation = Quaternion.Euler(0f, 0f, rigidbody2D.transform.eulerAngles.z);
		return rotation * new Vector3 (1f, 0f);
	}

	/// <summary>
	/// Get look/forward vector as a Vector3
	/// </summary>
	private Vector3 ForwardVec3()
	{
		Vector2 forward = ForwardVec2 ();
		return new Vector3 (forward.x, forward.y, 0f);
	}
	
	// This is how to draw a sprite with the current transformation and rotation of the parent object, for reference
	/*void OnGUI()
	{
		if (Event.current.type.Equals(EventType.Repaint))
		{
			Matrix4x4 swap = GUI.matrix;
			{	
				Vector3 screen_pos = Camera.main.WorldToScreenPoint(rigidbody2D.transform.position);
				screen_pos.y = Screen.height - screen_pos.y;

				GUIUtility.RotateAroundPivot(-rigidbody2D.transform.eulerAngles.z, screen_pos);
				Graphics.DrawTexture(new Rect(screen_pos.x - _tex.width / 2f, screen_pos.y - _tex.height / 2f, _tex.width, _tex.height), _tex);
			}
			GUI.matrix = swap;
		}
	}*/
}
