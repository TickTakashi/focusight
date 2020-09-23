using System.Collections;
using UnityEngine;

// Every Interval:
// - Choose either an Attractor or the Repellor
// - Apply a constant force dictated by the chosen object, varying with the square of the distance from the object.
// - Continue doing this until anxiety interval is complete.
public class Drift : MonoBehaviour
{
	public Rigidbody LookTarget;
	public Transform Stimulus;
	public Transform CameraTransform;

	public Transform[] Distractions;

	public Range ControlImpulse;
	public float ControlChargeTime = 5f;

	public Range PanicStrength;
	public Range TimeBetweenPanic;
	public Range PanicDuration;

	public Vector3 ToGoal => (Stimulus.position - LookTarget.position).normalized;

	// Always look at the look target;
	public void LateUpdate() {
		CameraTransform.LookAt(LookTarget.transform);
	}

	// Start is called before the first frame update
	void Start() {
		ChooseNextDistraction();
	}

	void ChooseNextDistraction() {
		// TODO: Randomly decide between targeted distraction and random drift.
		StartCoroutine(Distract());
	}

	IEnumerator Distract() {
		yield return new WaitForSeconds(TimeBetweenPanic.Sample);
		Transform distractionTarget = Distractions[Random.Range(0, Distractions.Length)];
		// Find the target using ray intersection with the Z plane.
		Ray ray = new Ray(CameraTransform.position, (-CameraTransform.position + distractionTarget.position).normalized);
		Plane zPlane = new Plane(Vector3.forward, Vector3.zero);
		float dist = 0;
		Vector3 targetPosition = Vector3.zero;
		if (zPlane.Raycast(ray, out dist)) {
			targetPosition = ray.GetPoint(dist);
//			GameObject debugSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
//			debugSphere.transform.position = targetPosition;
		}

		float duration = PanicDuration.Sample;
		float t = 0;
		float strength = PanicStrength.Sample;

		while (t < duration) {
			t += Time.fixedDeltaTime;
			LookTarget.AddForce((-LookTarget.position + targetPosition).normalized * strength);
			yield return new WaitForFixedUpdate();
		}

		ChooseNextDistraction();
	}

	// TODO: Have the player look at the door or the clinician instead of just moving around randomly.
	IEnumerator RandomDriftInterval() {
		
		float accumulatedTime = Time.fixedDeltaTime;
		float timeUntilPanic = TimeBetweenPanic.Sample;
		float strength = PanicStrength.Sample;
		float panicTime = PanicDuration.Sample;

		while (true) {
			if (accumulatedTime > timeUntilPanic) {
				if (accumulatedTime < (panicTime + timeUntilPanic)) {
					
					LookTarget.AddForce(-ToGoal * strength + Random.insideUnitSphere);
				} else {
					accumulatedTime = 0;
					timeUntilPanic = TimeBetweenPanic.Sample;
					strength = PanicStrength.Sample;
					panicTime = PanicDuration.Sample;
				}
			}

			yield return new WaitForFixedUpdate();

			accumulatedTime += Time.fixedDeltaTime;
		}
	}

	float chargeTime = 0f;
	// Update is called once per frame
	void Update() {
		if (Input.GetButtonDown("Fire1")) {
			// Charge.
			chargeTime = 0;
			Debug.Log("Starting Charge...");
		} else if (Input.GetButtonUp("Fire1")) {
			// Release.
			Debug.Log("Fire! " + chargeTime);
			float ratio = Mathf.Clamp01(chargeTime / ControlChargeTime);


			LookTarget.AddForce(ToGoal * ControlImpulse.Lerp(ratio), ForceMode.Impulse);
		} else if (Input.GetButton("Fire1")) {
			chargeTime += Time.deltaTime;
			Debug.Log("Charging... " + chargeTime);
		}
	}
}


[System.Serializable]
public class Range
{
	public float min;
	public float max;

	public float Sample => Random.Range(min, max);
	public float Size => max - min;

	public Range() : this(0, 0) { }

	public Range(float min, float max) {
		this.min = min;
		this.max = max;
	}

	public float Lerp(float t) {
		return Mathf.Lerp(min, max, t);
	}

}
