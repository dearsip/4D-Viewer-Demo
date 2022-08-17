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
public class SyncEffect : MonoBehaviour {


	public enum EFFECT_TYPE { CONSTANT, VISCOUS, SPRING, FRICTION, VIBRATE };


	// Public, User-Adjustable Settings
    public string configNameL = "Left Device";
    public string configNameR = "Right Device";
	public EFFECT_TYPE effectType = EFFECT_TYPE.VISCOUS; //!< Which type of effect occurs within this zone?
	[Range(0.0f,1.0f)] public double Gain = 0.333f;	
	[Range(0.0f,1.0f)] public double Magnitude = 0.333f;
	[Range(1.0f,1000.0f)] public double Frequency = 200.0f;
	private double Duration = 1.0f;
	public Vector3 Position = Vector3.zero;
	public Vector3 Direction = Vector3.up;


	// Keep track of the Haptic Devices
    HapticPlugin deviceL;
    HapticPlugin deviceR;
	bool inTheZone;  		//Is the stylus in the effect zone?
	Vector3 devicePoint;	// Current location of stylus
	float delta;			// Distance from stylus to zone collider.
	int FXID_L;				// ID of the effect.  (Per device.)
	int FXID_R;				// ID of the effect.  (Per device.)

	// These are the user adjustable vectors, converted to world-space. 
	private Vector3 focusPointWorld = Vector3.zero;
	private Vector3 directionWorld = Vector3.up;


    //! Start() is called at the beginning of the simulation.
    //!
    //! It will identify the Haptic devices, initizlize variables internal to this script, 
    //! and request an Effect ID from Open Haptics. (One for each device.)
    //!
    void Start()
    {
        HapticPlugin[] devices = (HapticPlugin[])Object.FindObjectsOfType(typeof(HapticPlugin));
        for (int ii = 0; ii < devices.Length; ii++)
        {
            if (devices[ii].configName == configNameL) deviceL = devices[ii];
            if (devices[ii].configName == configNameR) deviceR = devices[ii];
        }
        //if (device == null) Debug.LogError("Unable to initialize the haptic device.");
        inTheZone = false;
        devicePoint = Vector3.zero;
        delta = 0.0f;
        FXID_L = HapticPlugin.effects_assignEffect(deviceL.configName);
        FXID_R = HapticPlugin.effects_assignEffect(deviceR.configName);
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
        int ID = FXID_L;

        // If a haptic effect has not been assigned through Open Haptics, assign one now.
        if (ID == -1)
        {
            FXID_L = HapticPlugin.effects_assignEffect(deviceL.configName);
            ID = FXID_L;
        
            if (ID == -1) // Still broken?
            {
                Debug.LogError("Unable to assign Haptic effect.");
                return;
            }
        }

        ID = FXID_R;

        // If a haptic effect has not been assigned through Open Haptics, assign one now.
        if (ID == -1)
        {
            FXID_R = HapticPlugin.effects_assignEffect(deviceR.configName);
            ID = FXID_R;
        
            if (ID == -1) // Still broken?
            {
                Debug.LogError("Unable to assign Haptic effect.");
                return;
            }
        }

        // Determine if the stylus is in the "zone". 
        Vector3 StylusPos = deviceR.stylusPositionWorld;	//World Coordinates
        Vector3 CP = collider.ClosestPoint(StylusPos); 	//World Coordinates
        devicePoint = CP;
        delta = (CP - StylusPos).magnitude;

        //If the stylus is within the Zone, The ClosestPoint and the Stylus point will be identical.
        if (delta <= Mathf.Epsilon)
        {
            inTheZone = true;
                // Convert from the World coordinates to coordinates relative to the haptic device.
                Vector3 focalPointDevLocal = deviceL.transform.InverseTransformPoint(focusPointWorld);
                Vector3 rotationDevLocal = deviceL.transform.InverseTransformDirection(directionWorld);
                double[] pos = { focalPointDevLocal.x, focalPointDevLocal.y, focalPointDevLocal.z };
                double[] dir = { rotationDevLocal.x, rotationDevLocal.y, rotationDevLocal.z };

                double Mag = Magnitude;

                if (deviceL.isInSafetyMode())
                    Mag = 0;

                double yR = transform.InverseTransformPoint(StylusPos).y;
                double yL = transform.InverseTransformPoint(deviceL.stylusPositionWorld).y;

                double d = -5 * Mathf.Clamp((float)(yR+yL), -1, 0);

                // Send the current effect settings to OpenHaptics.
                HapticPlugin.effects_settings(
                    deviceL.configName,
                    FXID_L,
                    Gain,
                    Mag * d,
                    Frequency,
                    pos,
                    dir);
                HapticPlugin.effects_type(
                    deviceL.configName,
                    FXID_L,
                    (int)effectType);

                HapticPlugin.effects_settings(
                    deviceR.configName,
                    FXID_R,
                    Gain,
                    Mag * d,
                    Frequency,
                    pos,
                    dir);
                HapticPlugin.effects_type(
                    deviceR.configName,
                    FXID_R,
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
                HapticPlugin.effects_startEffect(deviceL.configName, ID );
                HapticPlugin.effects_startEffect(deviceR.configName, ID );
            } else
            {
                HapticPlugin.effects_stopEffect(deviceL.configName, ID);
                HapticPlugin.effects_stopEffect(deviceR.configName, ID);
            }
		}
	}

	void OnDestroy()
	{
        int ID = FXID_L;
        HapticPlugin.effects_stopEffect(deviceL.configName, ID);
        ID = FXID_R;
        HapticPlugin.effects_stopEffect(deviceR.configName, ID);
	}
	void OnDisable()
	{
        int ID = FXID_L;
        HapticPlugin.effects_stopEffect(deviceL.configName, ID);
        ID = FXID_R;
        HapticPlugin.effects_stopEffect(deviceR.configName, ID);
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
[CustomEditor(typeof(SyncEffect))]
public class SyncEffectEditor : Editor 
{
	override public void OnInspectorGUI()
	{
		SyncEffect SE = (SyncEffect)target;

        SE.configNameL = EditorGUILayout.TextField("Config Name Left", SE.configNameL);
        SE.configNameR = EditorGUILayout.TextField("Config Name Right", SE.configNameR);
		if (SE.gameObject.gameObject.GetComponent<Collider>() == null)
		{
			EditorGUILayout.LabelField("*********************************************************");
			EditorGUILayout.LabelField("   Haptic Effect must be assigned to an object with a collider.");
			EditorGUILayout.LabelField("*********************************************************");

		} else
		{
			SE.effectType = (SyncEffect.EFFECT_TYPE)EditorGUILayout.EnumPopup("Effect Type", SE.effectType);


			switch (SE.effectType)
			{
			case SyncEffect.EFFECT_TYPE.CONSTANT:
				SE.Direction = EditorGUILayout.Vector3Field("Direction", SE.Direction);
				SE.Magnitude = EditorGUILayout.Slider("Magnitude", (float)SE.Magnitude, 0.0f, 1.0f);
				break;
			case SyncEffect.EFFECT_TYPE.FRICTION:
				SE.Gain = EditorGUILayout.Slider("Gain", (float)SE.Gain, 0.0f, 1.0f);
				SE.Magnitude = EditorGUILayout.Slider("Magnitude", (float)SE.Magnitude, 0.0f, 1.0f);
				break;
			case SyncEffect.EFFECT_TYPE.SPRING:
				SE.Gain = EditorGUILayout.Slider("Gain", (float)SE.Gain, 0.0f, 1.0f);
				SE.Magnitude = EditorGUILayout.Slider("Magnitude", (float)SE.Magnitude, 0.0f, 1.0f);
				SE.Position = EditorGUILayout.Vector3Field("Position", SE.Position);
				break;
			case SyncEffect.EFFECT_TYPE.VIBRATE:
				SE.Gain = EditorGUILayout.Slider("Gain", (float)SE.Gain, 0.0f, 1.0f);
				SE.Magnitude = EditorGUILayout.Slider("Magnitude", (float)SE.Magnitude, 0.0f, 1.0f);
				SE.Frequency = EditorGUILayout.Slider("Frequency", (float)SE.Frequency, 1.0f, 1000.0f);
				SE.Direction = EditorGUILayout.Vector3Field("Direction", SE.Direction);
				break;
			case SyncEffect.EFFECT_TYPE.VISCOUS:
				SE.Gain = EditorGUILayout.Slider("Gain", (float)SE.Gain, 0.0f, 1.0f);
				SE.Magnitude = EditorGUILayout.Slider("Magnitude", (float)SE.Magnitude, 0.0f, 1.0f);
				break;

			}
		}

	}

}

#endif






