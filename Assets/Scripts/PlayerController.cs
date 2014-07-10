using UnityEngine;
using System.Collections;

[RequireComponent (typeof(Thruster), typeof(DamageCounter), typeof(Detonateable))]
public class PlayerController : MonoBehaviour, IPreResetable {
	// Maneuvering
	[SerializeField]
	private float _turnRate;

	// Weapons
	[SerializeField]
	private GameObject _missileToClone;
	[SerializeField]
	private GameObject _mineToClone;
	[SerializeField]
	private Transform _missileTransform;
	[SerializeField]
	private Transform _mineTransform;
	[SerializeField]
	private float _refireRate; //seconds

	private float _missileNextFireTime, _mineNextFireTime;
	private GameObject _firedMissile, _firedMine;
	private string _missileSpriteFilename, _thrustingMissileSpriteFilename;
	private string _mineSpriteFilename;

	// Torus wrapping
	private Vector2 _canvasBounds = new Vector2(-1f, -1f);
	private GameObject _torusHorizontal, _torusVertical, _torusCorner;
	private static readonly Vector3 OFFSCREEN = new Vector3(-1000f, 0f, 0f);
	private int _horizontalWrap = 0, _verticalWrap = 0;
	public int horizontalWrap { //Prevent editor from displaying
		set { _horizontalWrap = value; }
		get { return _horizontalWrap; }
	}
	public int verticalWrap {
		set { _verticalWrap = value; }
		get { return _verticalWrap; }
	}

	//Scoreboard
	private Scoreboard _myScoreboard, _enemyScoreboard;

	//Damage
	public float maxDamage;
	private float _damage;
	public float damage {
		set { _damage = value; }
		get { return _damage; }
	}

	void Start () {
		// Save world bounding box size for torus wrapping
		object[] obj = GameObject.FindObjectsOfType(typeof (GameObject));
		foreach (object o in obj) {
			GameObject g = (GameObject) o;
			if (g.name.Equals("Bounds")) {
				BoxCollider2D bounds_collider = (BoxCollider2D)g.collider2D;
				_canvasBounds = new Vector2(bounds_collider.size.x * bounds_collider.transform.localScale.x,
				                             bounds_collider.size.y * bounds_collider.transform.localScale.y);
				break;
			}
		}
		if (_canvasBounds.x == -1f)
			throw new UnityException("Unable to locate world bounding box");

		GetComponent<DamageCounter>().controller = this;

		ResetManager.Get().Register(this);
	}

	[RPC]
	public void SetUpPlayer(string playerName, NetworkViewID horizontalID,
			NetworkViewID verticalID, NetworkViewID cornerID, string colorName) {
		gameObject.name = playerName;

		//Set up scoreboards
		if (IsOwnedByServer()) {
			_myScoreboard = Server.Get().GetComponent<Scoreboard>();
			_myScoreboard.player = this;
			_enemyScoreboard = Client.Get().GetComponent<Scoreboard>();
		} else {
			_myScoreboard = Client.Get().GetComponent<Scoreboard>();
			_myScoreboard.player = this;
			_enemyScoreboard = Server.Get().GetComponent<Scoreboard>();
		}

		//My sprites
		string normalSpriteFilename = "Sprites/fighter_" + colorName;
		string thrustingSpriteFilename = "Sprites/fighter_" + colorName + "_thrust";
		GetComponent<Thruster>().SetSprites(normalSpriteFilename, thrustingSpriteFilename);

		//Weapon sprites
		_missileSpriteFilename  = "Sprites/missile_" + colorName;
		_thrustingMissileSpriteFilename = "Sprites/missile_" + colorName + "_thrust";
		_mineSpriteFilename = "Sprites/mine_" + colorName;

		SetUpTorusClone(out _torusHorizontal, horizontalID, playerName, normalSpriteFilename, thrustingSpriteFilename);
		SetUpTorusClone(out _torusVertical, verticalID, playerName, normalSpriteFilename, thrustingSpriteFilename);
		SetUpTorusClone(out _torusCorner, cornerID, playerName, normalSpriteFilename, thrustingSpriteFilename);
		GetComponent<Thruster>().SetTorusClones(_torusHorizontal, _torusVertical, _torusCorner);
	}

	public void PreReset() {
		Network.Destroy(gameObject);
	}

	private void SetUpTorusClone(out GameObject torusPointer, NetworkViewID id, string playerName,
			string normalSpriteFilename, string thrustingSpriteFilename) {
		torusPointer = NetworkView.Find(id).gameObject;
		torusPointer.name = playerName + " horizontal";
		torusPointer.GetComponent<DamageCounter>().controller = this;
		torusPointer.GetComponentInChildren<Shield>().controller = this;
	}

	void Update () {
		if (! gameObject.networkView.isMine)
			return;

		if (Input.GetAxisRaw("Missile") == 1) {
			if (CanFireMissile()) {
				_firedMissile = Network.Instantiate(_missileToClone, _missileTransform.position, _missileTransform.rotation, 0) as GameObject;
				_firedMissile.GetComponent<NetworkView>().RPC("SetSprites", RPCMode.AllBuffered, _missileSpriteFilename, _thrustingMissileSpriteFilename);
				_firedMissile.GetComponent<MissileController>().SetFirer(this);
			}
		}

		if (Input.GetAxisRaw("Mine") == 1) {
			if (CanFireMine()) {
				_firedMine = Network.Instantiate(_mineToClone, _mineTransform.position, _mineTransform.rotation, 0) as GameObject;
				_firedMine.GetComponent<NetworkView>().RPC("SetSprite", RPCMode.AllBuffered, _mineSpriteFilename);
				
				_firedMine.GetComponent<MineController>().SetFirer(this);
			}
		}

		if (damage >= maxDamage)
			GetComponent<Detonateable>().Detonate();
	}

	private bool CanFireMissile() {
		return _firedMissile == null && Time.time > _missileNextFireTime;
	}

	private bool CanFireMine() {
		return _firedMine == null && Time.time > _mineNextFireTime;
	}

	public void MissileDetonated() {
		_missileNextFireTime = Time.time + _refireRate;
	}

	public void MineDetonated() {
		_mineNextFireTime = Time.time + _refireRate;
	}

	void FixedUpdate() {
		if (gameObject.networkView.isMine) {
			float thrust = Input.GetAxisRaw ("Thrust");
			if (thrust > 0.0f)
				GetComponent<Thruster>().thrusting = true;
			else
				GetComponent<Thruster>().thrusting = false;

			// Directly set angular velocity (no angular acceleration/force,
			// though adding that that might add to gameplay)
			float turn = Input.GetAxisRaw ("Turn");
			rigidbody2D.angularVelocity = _turnRate * turn;
		}

		UpdateTorusClones();
	}

	void OnSerializeNetworkView(BitStream stream, NetworkMessageInfo info) {
		stream.Serialize(ref _damage);
	}

	private void UpdateTorusClones()
	{
		switch (_horizontalWrap) {
		case -1:	// left edge
			_torusHorizontal.transform.eulerAngles = rigidbody2D.transform.eulerAngles;
			_torusHorizontal.transform.position = new Vector3(transform.position.x - _canvasBounds.x, transform.position.y, 0f);
			break;
		case 1:		// right edge
			_torusHorizontal.transform.eulerAngles = rigidbody2D.transform.eulerAngles;
			_torusHorizontal.transform.position = new Vector3(transform.position.x + _canvasBounds.x, transform.position.y, 0f);
			break;
		default:	// neither horizontal edge
			_torusHorizontal.transform.position = OFFSCREEN;
			break;
		}

		switch (_verticalWrap) {
		case -1:	// top edge
			_torusVertical.transform.eulerAngles = rigidbody2D.transform.eulerAngles;
			_torusVertical.transform.position = new Vector3(transform.position.x, transform.position.y - _canvasBounds.y, 0f);
			break;
		case 1:		// bottom edge
			_torusVertical.transform.eulerAngles = rigidbody2D.transform.eulerAngles;
			_torusVertical.transform.position = new Vector3(transform.position.x, transform.position.y + _canvasBounds.y, 0f);
			break;
		default:	// neither vertical edge
			_torusVertical.transform.position = OFFSCREEN;
			break;
		}

		if (_horizontalWrap != 0 && _verticalWrap != 0) {
			_torusCorner.transform.eulerAngles = rigidbody2D.transform.eulerAngles;
			_torusCorner.transform.position = new Vector3(transform.position.x + _canvasBounds.x * _horizontalWrap, transform.position.y + _canvasBounds.y * _verticalWrap, 0f);
		}
		else
			_torusCorner.transform.position = OFFSCREEN;
	}

	void OnDetonate() {
		Network.Destroy(_torusHorizontal);
		Network.Destroy(_torusVertical);
		Network.Destroy(_torusCorner);
		_enemyScoreboard.GetComponent<NetworkView>().RPC("AddWin", RPCMode.AllBuffered);
		//TODO disable my scoreboard?
	}

	public bool IsOwnedByServer() {
		return (Network.isServer && GetComponent<NetworkView>().isMine) ||
			(Network.isClient && ! GetComponent<NetworkView>().isMine);
	}
}
