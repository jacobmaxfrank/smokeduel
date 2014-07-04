using UnityEngine;
using System.Collections.Generic;

public class Scoreboard : MonoBehaviour {
	public PlayerController player;
	private int wins;

	void OnGUI() {
		string str = wins.ToString();
		if (wins == 1)
			str += " win";
		else
			str += " wins";

		if (player != null) {
			int hpPercent = Mathf.RoundToInt((player.maxDamage - player.damage) / player.maxDamage * 100.0f);
			str += " | " + hpPercent + "%";
		}

		float screenHalf;
		if (gameObject.name == "Server")
			screenHalf = 0.0f;
		else
			screenHalf = 1.0f;

		GUIStyle style = GUI.skin.GetStyle("Label");
		style.alignment = TextAnchor.MiddleCenter;
		GUI.Label(new Rect(Screen.width * screenHalf / 2.0f, Screen.height - 40.0f, Screen.width / 2.0f, 40.0f), str, style);
	}

	[RPC]
	public void AddWin() {
		++wins;
	}
}
