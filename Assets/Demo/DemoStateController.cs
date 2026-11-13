using UnityEngine;

namespace TechTalk
{
    public sealed class DemoStateController : MonoBehaviour
    {
        public const int StateCount = 10;
        public static readonly string[] Titles = {
            "Final Product", "Wireframe", "Transform Lab", "Vertex Deformation", "Rasterization Lab",
            "Solid / UV / Texture", "Normals / Lighting", "Thermal View", "Inspection / X-Ray", "Failure Scenario"
        };
        internal static readonly string[] Descriptions = {
            "A running turbine viewed through three diagnostic layers.",
            "Barycentric edge shading reveals the triangles submitted to the rasterizer.",
            "Follow one cube corner through object, world, view and clip space.",
            "A traveling wave moves mesh vertices before they are projected.",
            "Test coverage at cell centers, then interpolate attributes with barycentric weights.",
            "The same sphere supplies UV coordinates for color and texture lookups.",
            "Normals describe surface direction. Rotate the light to see the dot product change.",
            "A fixed Celsius scale maps the synthetic sensor field to color.",
            "The casing blends over depth-tested opaque rotor stages.",
            "A scripted event increases temperature and vibration while rotor speed drops."
        };

        public int State { get; private set; }
        public bool IsReady => overlay != null && Raster != null;
        internal int Step { get; private set; }
        internal bool Paused { get; private set; }
        internal bool Clean { get; set; }
        internal bool Orbit { get; set; }
        internal float Move { get; set; }
        internal float Turn { get; set; }
        internal float Size { get; set; }
        internal float LightAngle { get; set; }
        internal float Temperature { get; set; }
        internal float Opacity { get; set; }
        internal int TriangleCount => turbine.Triangles;
        internal Telemetry Current { get; private set; }
        internal FailureScenarioController Failure { get; } = new();
        internal Camera Camera { get; private set; }
        internal Transform Cube => cube.transform;
        internal Transform Sphere => sphere.transform;
        internal Vector3 LightDirection { get; private set; }
        internal RasterizationLab Raster { get; private set; }

        private TurbineGeometry turbine;
        private Material shellMat, sphereMat, cubeMat, floorMat, surface;
        private GameObject floor, cube, sphere;
        private CameraRig rig;
        private OverlayUI overlay;
        private TelemetryToShaderBinder binder;
        private float stateTime, clock, rotorAngle, orbitAngle;
        private bool opening, externalClock;
        private int lastSurfaceMode = -1;

        private void Awake()
        {
            // These objects create native Unity resources and must be initialized on the main thread.
            Raster = new RasterizationLab();
            binder = new TelemetryToShaderBinder();
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = 60;
            Camera = new GameObject("Presentation camera",typeof(Camera)).GetComponent<Camera>();
            Camera.clearFlags = CameraClearFlags.SolidColor;
            Camera.backgroundColor = new Color(.035f,.059f,.091f);
            Camera.fieldOfView = 37;
            Camera.nearClipPlane = .1f;
            Camera.farClipPlane = 80;
            rig = new CameraRig(Camera);

            surface = new Material(Resources.Load<Shader>("PipelineSurface"));
            shellMat = new Material(Resources.Load<Shader>("InspectionShell"));
            turbine = new TurbineGeometry(surface);
            turbine.InspectionCover.sharedMaterial = shellMat;
            floorMat = new Material(surface);
            floorMat.SetFloat("_Mode",2);
            floorMat.SetColor("_Color",new(.026f,.041f,.060f));
            floor = Primitive("Stage floor",PrimitiveType.Plane,floorMat);
            floor.transform.position = new Vector3(0,-1.61f,0);
            floor.transform.localScale = new Vector3(2,1,2);
            cubeMat = new Material(surface);
            cubeMat.SetColor("_Color",new(.10f,.44f,.63f));
            cubeMat.SetFloat("_Metal",.28f);
            cube = Primitive("Transform cube",PrimitiveType.Cube,cubeMat);
            sphereMat = new Material(surface);
            sphereMat.SetColor("_Color",new(.32f,.57f,.76f));
            sphereMat.SetTexture("_Tex",GenerateAtlas());
            sphere = Primitive("Material sphere",PrimitiveType.Sphere,sphereMat);
            sphere.transform.localScale = Vector3.one * 2.75f;
            overlay = new OverlayUI(this);
            ResetAll();
            if (CaptureSession.Requested) gameObject.AddComponent<CaptureSession>();
        }

        private void Update()
        {
            if (externalClock) return;
            PresentationInput.Update(this);
            Advance(Paused ? 0 : Time.unscaledDeltaTime);
        }

        private void OnGUI()
        {
            overlay.Draw();
        }

        internal void SetExternalClock() { externalClock = true; }

        internal void Advance(float dt)
        {
            stateTime += dt;
            clock += dt;
            if (opening)
            {
                // A reproducible 40-second prologue. The arrow keys always return manual control.
                int next = stateTime < 9 ? 0 : stateTime < 17 ? 7 : stateTime < 25 ? 8 : 9;
                if (State != next)
                {
                    Select(next,false);
                    if (next == 9) Failure.Start();
                }
                if (stateTime >= 40) opening = false;
            }

            Failure.Tick(dt);
            float signalTime = Failure.Running ? Failure.Time : clock;
            Current = TelemetrySimulator.Sample(signalTime,Failure.Time);
            rotorAngle = Mathf.Repeat(rotorAngle + Current.Rpm/3160f * 92 * dt,360);
            if (Orbit) orbitAngle = Mathf.Repeat(orbitAngle + dt * 10,360);
            turbine.Rotor.localRotation = Quaternion.AngleAxis(State == 1 ? 0 : rotorAngle,Vector3.forward);
            rig.Apply(State,orbitAngle);
            cube.transform.SetPositionAndRotation(new Vector3(Move,0,0),Quaternion.Euler(15,Turn,8));
            cube.transform.localScale = Vector3.one * Size;
            LightDirection = Quaternion.Euler(0,LightAngle,0) * new Vector3(.42f,.64f,-.63f).normalized;
            sphereMat.SetVector("_LightDirection",LightDirection);
            sphereMat.SetFloat("_Mode",State==5 ? 2+Step : Step==0 ? 2 : Step==1 ? 5 : 6);
            int mode = State==1 ? 1 : State==7 || State==9 ? 7 : 0;
            if (mode != lastSurfaceMode || State is 3 or 7 or 9)
            {
                lastSurfaceMode = mode;
                foreach(var renderer in turbine.Parts)
                {
                    bool displace = renderer == turbine.Shell || renderer.transform.parent == turbine.Rotor;
                    float amplitude = State==3 && Step==1 ? .19f : State==9 ? .12f*Current.Severity : 0;
                    binder.Apply(renderer,mode,State==7 ? Temperature : Current.Celsius,signalTime,
                        displace ? amplitude : 0);
                }
            }
            shellMat.SetFloat("_Opacity",Opacity);
        }

        public void Select(int state) => Select(state,true);

        private void Select(int state,bool manual)
        {
            State = (state+StateCount)%StateCount;
            if (manual)
            {
                opening = false;
                stateTime = 0;
                orbitAngle = 0;
                Orbit = false;
            }
            Step = 0;
            Failure.Reset();
            lastSurfaceMode = -1;
            turbine.Root.gameObject.SetActive(State != 2 && State != 4 && State != 5 && State != 6);
            cube.SetActive(State == 2);
            sphere.SetActive(State == 5 || State == 6);
            floor.SetActive(State != 4);
            turbine.Shell.sharedMaterial = State==8 ? shellMat : turbine.ShellMaterial;
            turbine.InspectionCover.gameObject.SetActive(State==8);
            rig.Apply(State,orbitAngle);
        }

        internal void TogglePause() => Paused = !Paused;

        internal void CycleStep()
        {
            int count = State switch { 3 => 2, 4 => 4, 5 or 6 => 3, _ => 1 };
            Step = (Step+1)%count;
        }

        internal void StartFailure()
        {
            Select(9);
            rotorAngle = 0;
            Failure.Start();
            Paused = false;
        }

        internal void PlayOpening()
        {
            ResetAll();
            opening = true;
        }

        internal void ResetState()
        {
            Select(State);
            clock = rotorAngle = 0;
            Paused = false;
            Move = 0;
            Turn = 27;
            Size = 1.6f;
            LightAngle = 0;
            Temperature = 106;
            Opacity = .14f;
            Raster.Reset();
        }

        internal void ResetAll()
        {
            Clean = false;
            Select(0);
            ResetState();
        }

        private static GameObject Primitive(string name,PrimitiveType kind,Material mat)
        {
            var obj = GameObject.CreatePrimitive(kind);
            obj.name = name;
            Destroy(obj.GetComponent<Collider>());
            obj.GetComponent<MeshRenderer>().sharedMaterial = mat;
            return obj;
        }

        private static Texture2D GenerateAtlas()
        {
            const int size = 256;
            var pixels = new Color32[size*size];
            for (int y=0;y<size;y++)
                for (int x=0;x<size;x++)
                {
                    bool edge = x%32 < 2 || y%32 < 2;
                    bool odd = (x/32+y/32)%2==1;
                    pixels[y*size+x] = edge ? new Color32(13,31,51,255) :
                        odd ? new Color32(29,143,172,255) : new Color32(245,182,77,255);
                }
            var tex = new Texture2D(size,size,TextureFormat.RGBA32,true);
            tex.SetPixels32(pixels);
            tex.Apply();
            return tex;
        }
    }

    internal sealed class CameraRig
    {
        private readonly Camera camera;
        public CameraRig(Camera camera) => this.camera=camera;

        public void Apply(int state,float orbit)
        {
            bool lab = state is 2 or 5 or 6;
            Vector3 target = lab ? Vector3.zero : new Vector3(0,-.04f,0);
            Vector3 offset = lab ? new Vector3(2.45f,1.65f,-5.6f) : new Vector3(6.9f,3.25f,-5.55f);
            offset = Quaternion.AngleAxis(orbit,Vector3.up)*offset;
            Quaternion facing = Quaternion.LookRotation(-offset,Vector3.up);
            Vector3 shift = facing*Vector3.right * (lab ? 1.04f : 1.38f);
            camera.transform.SetPositionAndRotation(target + offset - shift,facing);
        }
    }

    internal static class PresentationInput
    {
        public static void Update(DemoStateController demo)
        {
            for(int i=0;i<DemoStateController.StateCount;i++)
                if(Input.GetKeyDown(KeyCode.Alpha0+i) || Input.GetKeyDown(KeyCode.Keypad0+i)) demo.Select(i);
            if(Input.GetKeyDown(KeyCode.RightArrow)) demo.Select(demo.State+1);
            if(Input.GetKeyDown(KeyCode.LeftArrow)) demo.Select(demo.State-1);
            if(Input.GetKeyDown(KeyCode.Tab)) demo.CycleStep();
            if(Input.GetKeyDown(KeyCode.Space)) demo.TogglePause();
            if(Input.GetKeyDown(KeyCode.R))
            {
                if(Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)) demo.ResetAll();
                else demo.ResetState();
            }
            if(Input.GetKeyDown(KeyCode.F)) demo.StartFailure();
            if(Input.GetKeyDown(KeyCode.Home)) demo.Select(0);
            if(Input.GetKeyDown(KeyCode.O)) demo.Orbit=!demo.Orbit;
            if(Input.GetKeyDown(KeyCode.H)) demo.Clean=!demo.Clean;
            if(Input.GetKeyDown(KeyCode.I)) demo.PlayOpening();
            if(Input.GetKeyDown(KeyCode.Escape) && !Application.isEditor) Application.Quit();
        }
    }
}
