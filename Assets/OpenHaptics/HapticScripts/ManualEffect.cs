using System.Timers;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

//! This MonoBehavior can be attached to any object with a collider. It will apply a haptic "effect"
//! to any haptic stylus that is within the boundries of the collider.
//! The parameters can be adjusted on the fly.
public class ManualEffect : MonoBehaviour {


	public enum EFFECT_TYPE { CONSTANT, VISCOUS, SPRING, FRICTION, VIBRATE };


	// Public, User-Adjustable Settings
    public string configName = "Left Device";
	public EFFECT_TYPE effectType = EFFECT_TYPE.VISCOUS; //!< Which type of effect occurs within this zone?
	[Range(0.0f,1.0f)] public double Gain = 0.333f;	
	[Range(0.0f,1.0f)] public double Magnitude = 0.333f;
	[Range(1.0f,1000.0f)] public double Frequency = 200.0f;
	private double Duration = 1.0f;
	public Vector3 Position = Vector3.zero;
	public Vector3 Direction = Vector3.up;


	// Keep track of the Haptic Devices
    HapticPlugin device;
	bool inTheZone;  		//Is the stylus in the effect zone?
	Vector3 devicePoint;	// Current location of stylus
	float delta;			// Distance from stylus to zone collider.
	int FXID;				// ID of the effect.  (Per device.)

	// These are the user adjustable vectors, converted to world-space. 
	private Vector3 focusPointWorld = Vector3.zero;
	private Vector3 directionWorld = Vector3.up;


	//! Start() is called at the beginning of the simulation.
	//!
	//! It will identify the Haptic devices, initizlize variables internal to this script, 
	//! and request an Effect ID from Open Haptics. (One for each device.)
	//!
	void Start () 
	{
		HapticPlugin[] devices = (HapticPlugin[]) Object.FindObjectsOfType(typeof(HapticPlugin));
		for (int ii = 0; ii < devices.Length; ii++)
            if (devices[ii].configName == configName) device = devices[ii];
        if (device == null) Debug.LogError("Unable to initialize the haptic device.");
        inTheZone = false;
        devicePoint = Vector3.zero;
        delta = 0.0f;
        FXID = HapticPlugin.effects_assignEffect(device.configName);
	}
	
	 
	//!  Update() is called once per frame.
 	//! 
	//! This function 
	//! - Determines if a haptic stylus is inside the collider
	//! - Updates the effect settings.
	//! - Starts and stops the effect when appropriate.
	void Update () 
	{
		// Find the pointer to the collider that defines the "zone". 
		Collider collider = gameObject.GetComponent<Collider>();
		if (collider == null)
		{
			Debug.LogError("This Haptic Effect Zone requires a collider");
			return;
		}

		// Update the World-Space vectors
		focusPointWorld = transform.TransformPoint(Position);
		directionWorld =  transform.TransformDirection(Direction);

		// Update the effect seperately for each haptic device.
        bool oldInTheZone = inTheZone;
        int ID = FXID;

        // If a haptic effect has not been assigned through Open Haptics, assign one now.
        if (ID == -1)
        {
            FXID = HapticPlugin.effects_assignEffect(device.configName);
            ID = FXID;
        
            if (ID == -1) // Still broken?
            {
                Debug.LogError("Unable to assign Haptic effect.");
                return;
            }
        }

        // Determine if the stylus is in the "zone". 
        Vector3 StylusPos = device.stylusPositionWorld;	//World Coordinates
        Vector3 CP = collider.ClosestPoint(StylusPos); 	//World Coordinates
        devicePoint = CP;
        delta = (CP - StylusPos).magnitude;

        //If the stylus is within the Zone, The ClosestPoint and the Stylus point will be identical.
        if (delta <= Mathf.Epsilon)
        {
            inTheZone = true;
                // Convert from the World coordinates to coordinates relative to the haptic device.
                Vector3 focalPointDevLocal = device.transform.InverseTransformPoint(focusPointWorld);
                Vector3 rotationDevLocal = device.transform.InverseTransformDirection(directionWorld);
                double[] pos = { focalPointDevLocal.x, focalPointDevLocal.y, focalPointDevLocal.z };
                double[] dir = { rotationDevLocal.x, rotationDevLocal.y, rotationDevLocal.z };

                double Mag = Magnitude;

                if (device.isInSafetyMode())
                    Mag = 0;

                double d = -5 * Mathf.Clamp(transform.InverseTransformPoint(StylusPos).y, -1, 0);

                // Send the current effect settings to OpenHaptics.
                HapticPlugin.effects_settings(
                    device.configName,
                    ID,
                    Gain,
                    Mag * d,
                    Frequency,
                    pos,
                    dir);
                HapticPlugin.effects_type(
                    device.configName,
                    ID,
                    (int)effectType);
        } else
        {
            inTheZone = false;

            // Note : If the device is not in the "Zone", there is no need to update the effect settings.
        }
        
        // If the on/off state has changed since last frame, send a Start or Stop event to OpenHaptics
        if (oldInTheZone != inTheZone)
        {
            if (inTheZone)
            {
                HapticPlugin.effects_startEffect(device.configName, ID );
            } else
            {
                HapticPlugin.effects_stopEffect(device.configName, ID);
            }
		}
	}

	void OnDestroy()
	{
        int ID = FXID;
        HapticPlugin.effects_stopEffect(device.configName, ID);
	}
	void OnDisable()
	{
        int ID = FXID;
        HapticPlugin.effects_stopEffect(device.configName, ID);
        inTheZone = false;
	}


	//! OnDrawGizmos() is called only when the Unity Editor is active.
	//! It draws some hopefully useful wireframes to the editor screen.
	 
	void OnDrawGizmos()
	{
		Gizmos.matrix = Matrix4x4.identity;
		Gizmos.matrix = this.transform.localToWorldMatrix;

		Gizmos.color = Color.white;

		Ray R = new Ray(); 
		R.direction = Direction;

		if (effectType == EFFECT_TYPE.CONSTANT)
		{
			Gizmos.DrawRay(R);
		}

		Vector3 focusPointWorld = transform.TransformPoint(Position);


		Gizmos.matrix = Matrix4x4.identity;
		Gizmos.color = Color.white;
		if (effectType == EFFECT_TYPE.SPRING)
		{
			Gizmos.DrawIcon(focusPointWorld, "anchor_icon.tiff");
		}

		// If the device is in the zone, draw a red marker. 
		// And draw a line indicating the spring force, if we're in that mode.
        if (delta <= Mathf.Epsilon)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(devicePoint, 1.0f);
            if(effectType == EFFECT_TYPE.SPRING)
                Gizmos.DrawLine(focusPointWorld, devicePoint);
        }
	}


}

#if UNITY_EDITOR
[CustomEditor(typeof(ManualEffect))]
public class ManualEffectEditor : Editor 
{
	override public void OnInspectorGUI()
	{
		ManualEffect ME = (ManualEffect)target;

        ME.configName = EditorGUILayout.TextField("Config Name", ME.configName);
		if (ME.gameObject.gameObject.GetComponent<Collider>() == null)
		{
			EditorGUILayout.LabelField("*********************************************************");
			EditorGUILayout.LabelField("   Haptic Effect must be assigned to an object with a collider.");
			EditorGUILayout.LabelField("*********************************************************");

		} else
		{
			ME.effectType = (ManualEffect.EFFECT_TYPE)EditorGUILayout.EnumPopup("Effect Type", ME.effectType);


			switch (ME.effectType)
			{
			case ManualEffect.EFFECT_TYPE.CONSTANT:
				ME.Direction = EditorGUILayout.Vector3Field("Direction", ME.Direction);
				ME.Magnitude = EditorGUILayout.Slider("Magnitude", (float)ME.Magnitude, 0.0f, 1.0f);
				break;
			case ManualEffect.EFFECT_TYPE.FRICTION:
				ME.Gain = EditorGUILayout.Slider("Gain", (float)ME.Gain, 0.0f, 1.0f);
				ME.Magnitude = EditorGUILayout.Slider("Magnitude", (float)ME.Magnitude, 0.0f, 1.0f);
				break;
			case ManualEffect.EFFECT_TYPE.SPRING:
				ME.Gain = EditorGUILayout.Slider("Gain", (float)ME.Gain, 0.0f, 1.0f);
				ME.Magnitude = EditorGUILayout.Slider("Magnitude", (float)ME.Magnitude, 0.0f, 1.0f);
				ME.Position = EditorGUILayout.Vector3Field("Position", ME.Position);
				break;
			case ManualEffect.EFFECT_TYPE.VIBRATE:
				ME.Gain = EditorGUILayout.Slider("Gain", (float)ME.Gain, 0.0f, 1.0f);
				ME.Magnitude = EditorGUILayout.Slider("Magnitude", (float)ME.Magnitude, 0.0f, 1.0f);
				ME.Frequency = EditorGUILayout.Slider("Frequency", (float)ME.Frequency, 1.0f, 1000.0f);
				ME.Direction = EditorGUILayout.Vector3Field("Direction", ME.Direction);
				break;
			case ManualEffect.EFFECT_TYPE.VISCOUS:
				ME.Gain = EditorGUILayout.Slider("Gain", (float)ME.Gain, 0.0f, 1.0f);
				ME.Magnitude = EditorGUILayout.Slider("Magnitude", (float)ME.Magnitude, 0.0f, 1.0f);
				break;

			}
		}

	}

}

#endif






