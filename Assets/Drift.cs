using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// Every Interval:
// - Choose either an Attractor or the Repellor
// - Apply a constant force dictated by the chosen object, varying with the square of the distance from the object.
// - Continue doing this until anxiety interval is complete.
public class Drift : MonoBehaviour
{
	public Rigidbody LookTarget;
	public Transform Stimulus;
	public Camera Cam;

	public Transform[] Distractions;

	public Range ControlImpulse;
	public float ControlChargeTime = 5f;

	public Range PanicStrength;
	public Range TimeBetweenPanic;
	public Range PanicDuration;

	public Image barMask;
	public Image barBorder;

	public float maxHeight;
	public float originalWidth;

	public float progress;
	public float lookDistance = 2f;
	public float SDU = 0;
	public float SDUFall = 0.2f;
	public float SDUBuild = 2.0f;
	public float progressThreshold = 0.8f;
	public float progressBuild = 0.1f;

	public Image reticule;

	public Vector3 ToGoal => (Stimulus.position - LookTarget.position).normalized;

	// Always look at the look target;
	public void LateUpdate() {
		Cam.transform.LookAt(LookTarget.transform);

		if(Vector3.Distance(Stimulus.position, LookTarget.position) < lookDistance) {
			SDU = Mathf.Lerp(SDU, 1.0f, Time.deltaTime * SDUBuild);

			if (SDU > progressThreshold) {
				progress += Time.deltaTime * progressBuild; 
			}
			reticule.color = new Color(0.3f, 0.3f, 1f, 0.5f);
		} else {
			SDU = Mathf.Clamp01(SDU - Time.deltaTime * SDUFall);
			reticule.color = new Color(1f, 1f, 1f, 0.5f);
		}

		barMask.GetComponent<RectTransform>().sizeDelta = new Vector2(originalWidth, maxHeight * SDU * (1.0f - progress));
		barBorder.GetComponent<RectTransform>().sizeDelta = new Vector2(originalWidth, maxHeight * (1.0f - progress));
	}

	// Start is called before the first frame update
	void Start() {
		maxHeight = barMask.GetComponent<RectTransform>().sizeDelta.y;
		originalWidth = barMask.GetComponent<RectTransform>().sizeDelta.x;
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
		Ray ray = new Ray(Cam.transform.position, (-Cam.transform.position + distractionTarget.position).normalized);
		Vector3 targetPosition = Vector3.zero;
		if (new Plane(Vector3.forward, Vector3.zero).Raycast(ray, out float dist)) {
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
			//Debug.Log("Starting Charge...");
		} else if (Input.GetButtonUp("Fire1")) {
			// Release.
			//Debug.Log("Fire! " + chargeTime);
			float ratio = Mathf.Clamp01(chargeTime / ControlChargeTime);

			Ray clickRay = Cam.ScreenPointToRay(Input.mousePosition);
			Vector3 targetPosition = Vector3.zero;
			if (new Plane(Vector3.forward, Vector3.zero).Raycast(clickRay, out float dist)) {
				targetPosition = clickRay.GetPoint(dist);

				//GameObject debugSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
				//debugSphere.transform.position = targetPosition;
			}

			LookTarget.AddForce((-LookTarget.position + targetPosition).normalized * ControlImpulse.Lerp(ratio), ForceMode.Impulse);
		} else if (Input.GetButton("Fire1")) {
			chargeTime += Time.deltaTime;
			//Debug.Log("Charging... " + chargeTime);
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
