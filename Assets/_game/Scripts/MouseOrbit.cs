using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[AddComponentMenu("Camera-Control/Mouse Orbit")]
public class MouseOrbit : MonoBehaviourPlus
{
    public static MouseOrbit Instance;
    public static Camera MainCamera;
    public static Ray AimRay;
	public Transform target;
    public Vector2 Distance;
    public float d = 10.0f;
    public Vector2 Height;


    public float xSpeed= 250.0f;
	public float ySpeed= 120.0f;
	
	public float yMinLimit= -20f;
	public float yMaxLimit= 80f;
	float Pose = 0.5f;
    public float ZoommingSpeed;

    public float FoV = 75;

    public Vector2 ZoomMinMax;
    [System.NonSerialized]
    public float Zoom;

	float x= 0.0f;
	float y= 0.0f;

    public float XAdd = 10f;
    //public Vector2 AimingPointOffset;
    [System.NonSerialized]
    public RaycastHit AimingHit;
    [System.NonSerialized]
    public bool AimingHasHit;

    [System.NonSerialized]
    public Transform Tr;
    [System.NonSerialized]
    public Quaternion Rotation;
    Quaternion rotation;
    UILink Crosshair;


    private void Awake()
    {
        Instance = this;
        Tr = transform;
        rotation = Tr.rotation;
        MainCamera = GetComponent<Camera>();
        AimingHit = new RaycastHit();
    }

    void  Start (){
		Vector3 angles = transform.eulerAngles;
		x = angles.y;
		y = angles.x;
		
		if (GetComponent<Rigidbody>())
			GetComponent<Rigidbody>().freezeRotation = true;

        Crosshair = UILink.MainCanvas.GetChildByName("Weapon").GetChildByName("Crosshair");

        InputEvents.Instance.OnButtonDown("UnlockCursor").AddListener(delegate { UnlockCursor(); });
        InputEvents.Instance.OnButtonUp("UnlockCursor").AddListener(delegate { LockCursor(); });
        LockCursor();
    }

    private void OnDestroy()
    {
        UnlockCursor();
    }

    void LateUpdate()
    {
        if (!target)
            return;

        float zomm = Input.GetAxis("Zoom");
        
        Pose = Mathf.Clamp(Pose + Input.GetAxis("Camera") * ZoommingSpeed, 0f, 1f);

        Vector3 tp = target.position + target.up * Mathf.Lerp(Height.x, Height.y, Pose);

        d = Mathf.Lerp(Distance.x, Distance.y, Pose);
        
        y = ClampAngle(y);

        Zoom = Mathf.Lerp(ZoomMinMax.x, ZoomMinMax.y, zomm);

        MainCamera.fieldOfView = FoV / Zoom;

        if (!Input.GetButton("UnlockCursor"))
        {
            x += Input.GetAxis("Mouse X") * xSpeed / Zoom * Time.fixedDeltaTime * ((y > 90 || y < -90) ? -1 : 1);
            y -= Input.GetAxis("Mouse Y") * ySpeed / Zoom * Time.fixedDeltaTime;

            rotation = Quaternion.Euler(y + XAdd * (1 - zomm * 0.5f), x, 0);

            if (!Input.GetButton("LockTurels"))
                Rotation = Quaternion.Euler(y, x, 0);
        }

        Vector3 position = Tr.rotation * new Vector3(0, 0, -d) + tp;
        Tr.rotation = Quaternion.Lerp(Tr.rotation, rotation, Time.fixedDeltaTime * 4);
        if (Quaternion.Angle(Tr.rotation, rotation) > 60)
            Tr.rotation = Quaternion.RotateTowards(rotation, Tr.rotation, 59);
        Tr.position = position;

        AimRay = new Ray(Tr.position, Rotation.GetForward());//MainCamera.ScreenPointToRay(new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, 0));
        AimingHasHit = Physics.Raycast(AimRay, out AimingHit, Mathf.Infinity, GameValues.AimingLayer);
        if (!AimingHasHit)
            AimingHit.point = AimRay.GetPoint(2000);

        Vector3 scP = MainCamera.WorldToScreenPoint(AimingHit.point);

        Crosshair.RectTransform.anchoredPosition = new Vector2(scP.x, Screen.height - scP.y);
    }

    /* private void OnGUI()
     {
         GUI.skin.label.alignment = TextAnchor.MiddleCenter;
         GUI.Label(new Rect(Screen.width * AimingPointOffset.x - 15, Screen.height * (1 - AimingPointOffset.y) - 15, 30, 30), "+");
     }*/
}