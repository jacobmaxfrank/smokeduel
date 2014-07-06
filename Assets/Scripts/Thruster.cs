using UnityEngine;

[RequireComponent (typeof(SpriteRenderer), typeof(Rigidbody2D))]
public class Thruster : MonoBehaviour {
	[SerializeField]
	private float _thrustForce;

	private Sprite _normalSprite, _thrustingSprite;
	private SpriteRenderer _torusHorizontal, _torusVertical, _torusCorner;

	private bool _thrusting;
	public bool thrusting {
		set {
			if (_thrusting == value)
				return;

			_thrusting = value;

			Sprite sprite;
			if (_thrusting)
				sprite = _thrustingSprite;
			else
				sprite = _normalSprite;

			GetComponent<SpriteRenderer>().sprite = sprite;
			if (_torusHorizontal)
				_torusHorizontal.sprite = sprite;
			if (_torusVertical)
				_torusVertical.sprite = sprite;
			if (_torusCorner)
				_torusCorner.sprite = sprite;
		}
		get { return _thrusting; }
	}

	[RPC]
	public void SetSprites(string normalFilename, string thrustingFilename) {
		_normalSprite = Resources.Load<Sprite>(normalFilename);
		_thrustingSprite = Resources.Load<Sprite>(thrustingFilename);
		GetComponent<SpriteRenderer>().sprite = _normalSprite;
	}

	public void SetTorusClones(GameObject horizontal, GameObject vertical, GameObject corner) {
		_torusHorizontal = horizontal.GetComponent<SpriteRenderer>();
		_torusVertical = vertical.GetComponent<SpriteRenderer>();
		_torusCorner = corner.GetComponent<SpriteRenderer>();
	}

	//Should update after controllers decide if they're thrusting this physics
	//frame or not
	void FixedUpdate() {
		if (thrusting) {
			Vector2 forward = transform.right;

			if (GetComponent<NetworkView>().isMine)
				rigidbody2D.AddForce(forward * _thrustForce);

			if (Network.isServer) {
				float cfdThrustForce = _thrustForce * 10000.0f;
				CFDController cfd = CFDController.Get();

				int cfdX, cfdY;
				cfd.WorldToGrid (transform.position, out cfdX, out cfdY);
				if (cfd.IsInRange(cfdX) && cfd.IsInRange(cfdY)) {
					cfd.AddUForce(-cfdThrustForce * forward.x, cfdX, cfdY);
					//Unity goes y: bottom to top, CFD goes V: top to bottom
					cfd.AddVForce(-cfdThrustForce * -forward.y, cfdX, cfdY);
				}
			}
		}
	}

	void OnSerializeNetworkView(BitStream stream, NetworkMessageInfo info) {
		if (stream.isWriting) {
			stream.Serialize(ref _thrusting);
		} else {
			bool t = false;
			stream.Serialize(ref t);
			thrusting = t;
		}
	}
}
