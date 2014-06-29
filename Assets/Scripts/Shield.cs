using UnityEngine;
using System.Collections.Generic;

public class Shield : MonoBehaviour {
	public PlayerController controller; //should be set by controller

	public void Update() {
		float damage = controller.damage;

		SpriteRenderer renderer = GetComponent<SpriteRenderer>();

		float newAlpha;
		if (damage < 50.0f)
			newAlpha = 0.0f;
		else if (damage < 75.0f)
			newAlpha = Mathf.PingPong(Time.time, 1.0f);
		else
			newAlpha = Mathf.PingPong(Time.time * 2.0f, 1.0f);

		renderer.color = new Color(1.0f, 1.0f, 1.0f, newAlpha);
	}
}

