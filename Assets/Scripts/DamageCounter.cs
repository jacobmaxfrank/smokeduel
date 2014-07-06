using UnityEngine;
using System.Collections.Generic;

public class DamageCounter : MonoBehaviour {
	public PlayerController controller;
	private Texture2D _texture;
	private Color32[] _pixels;
	private static readonly float WORLD_UNITS_TO_PIXELS = 0.01f;

	void Start() {
		//controller should be set by the controller

		_texture = GetComponent<SpriteRenderer>().sprite.texture;
		_pixels = _texture.GetPixels32();

		//TODO Optimize: precache indices and local positions of pixels with a>0
	}

	void FixedUpdate() {
		float damage = 0;

		for (int i = 0; i < _pixels.Length; ++i) {
			if (_pixels[i].a > 0) {
				int x = i / _texture.width - _texture.width / 2;
				int y = i % _texture.width - _texture.height / 2;
				Vector3 pixel = new Vector3(x * WORLD_UNITS_TO_PIXELS, y * WORLD_UNITS_TO_PIXELS, 0.0f);

				Vector3 world = transform.TransformPoint(pixel);

				CFDController cfd = CFDController.Get();
				int cfdX, cfdY;
				cfd.WorldToGrid(world, out cfdX, out cfdY);

				if (cfd.IsInRange(cfdX) && cfd.IsInRange(cfdY))
					damage += cfd.GetDensityAt(cfdX, cfdY);
			}
		}

		controller.damage += damage;
	}
}
