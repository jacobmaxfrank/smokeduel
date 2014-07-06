using UnityEngine;
using System.Collections;

public class TorusDisplayWrapping : MonoBehaviour
{
	void OnTriggerEnter2D(Collider2D collider)
	{
		//TODO there's gotta be an easier way to do this
		GameObject parent = (GameObject)collider.attachedRigidbody.transform.root.gameObject; // get parent of colliding object
		if (parent.tag.Equals("Player"))
		{
			PlayerController c = parent.GetComponent<PlayerController>();

			switch (name)
			{
			case "Right Bound":
				c.horizontalWrap = -1;
				break;
			case "Left Bound":
				c.horizontalWrap = 1;
				break;
			case "Bottom Bound":
				c.verticalWrap = 1;
				break;
			case "Top Bound":
				c.verticalWrap = -1;
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
				c.horizontalWrap = 0;
				break;
			case "Left Bound":
				c.horizontalWrap = 0;
				break;
			case "Bottom Bound":
				c.verticalWrap = 0;
				break;
			case "Top Bound":
				c.verticalWrap = 0;
				break;
				
			default:
				break;
			}
		}
	}
}
