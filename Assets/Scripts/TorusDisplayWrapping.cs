using UnityEngine;
using System.Collections;

/// <summary>
/// Update player's controller with which edges it's touching
/// </summary>
public class TorusDisplayWrapping : MonoBehaviour
{
	void OnTriggerEnter2D(Collider2D collider)
	{
		GameObject parent = (GameObject)collider.attachedRigidbody.transform.root.gameObject; // get parent of colliding object
		if (parent.tag.Equals("Player"))
		{
			PlayerController c = parent.GetComponent<PlayerController>();

			switch (name)
			{
			case "Right Bound":
				c._horizontalWrap = -1;
				break;
			case "Left Bound":
				c._horizontalWrap = 1;
				break;
			case "Bottom Bound":
				c._verticalWrap = 1;
				break;
			case "Top Bound":
				c._verticalWrap = -1;
				break;

			default:
				break;
			}
		}
	}

	void OnTriggerExit2D(Collider2D collider)
	{
		GameObject parent = (GameObject)collider.attachedRigidbody.transform.root.gameObject; // get parent of colliding object
		if (parent.tag.Equals("Player"))
		{
			PlayerController c = parent.GetComponent<PlayerController>();
			
			switch (name)
			{
			case "Right Bound":
				c._horizontalWrap = 0;
				break;
			case "Left Bound":
				c._horizontalWrap = 0;
				break;
			case "Bottom Bound":
				c._verticalWrap = 0;
				break;
			case "Top Bound":
				c._verticalWrap = 0;
				break;
				
			default:
				break;
			}
		}
	}
}
