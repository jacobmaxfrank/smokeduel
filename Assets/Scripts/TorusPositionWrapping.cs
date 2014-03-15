using UnityEngine;
using System.Collections;

/// <summary>
/// Handle torus-wrapping the player's position
/// </summary>
public class TorusPositionWrapping : MonoBehaviour
{	
	void OnTriggerExit2D(Collider2D collider)
	{
		GameObject parent = (GameObject)collider.attachedRigidbody.transform.root.gameObject; // get parent of colliding object

		// Calculate bounds
		Vector3 bounds = GetComponent<BoxCollider2D> ().size;
		bounds.x *= transform.localScale.x;
		bounds.y *= transform.localScale.y;
		
		// Locate screen edges
		float right  = collider2D.transform.position.x + bounds.x * 0.5f,
		left   = collider2D.transform.position.x - bounds.x * 0.5f,
		top    = collider2D.transform.position.y + bounds.y * 0.5f,
		bottom = collider2D.transform.position.y - bounds.y * 0.5f;
		
		// Wrap horizontally
		if (parent.transform.position.x > right)
			parent.transform.position = new Vector3 (parent.transform.position.x - bounds.x, parent.transform.position.y, 0f);
		else if (parent.transform.position.x < left)
			parent.transform.position = new Vector3 (parent.transform.position.x + bounds.x, parent.transform.position.y, 0f);
		
		// Wrap vertically
		if (parent.transform.position.y > top)
			parent.transform.position = new Vector3 (parent.transform.position.x, parent.transform.position.y - bounds.y, 0f);
		else if (parent.transform.position.y < bottom)
			parent.transform.position = new Vector3 (parent.transform.position.x, parent.transform.position.y + bounds.y, 0f);
	}
}
