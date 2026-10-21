using NGame.Core.Multiplayer;
using UnityEngine;

namespace NGame.Presentation
{
    /// <summary>Owns the camera and daylight transition; village art lives in separate scene objects.</summary>
    public sealed class VillageStage : MonoBehaviour
    {
        private Camera stageCamera;
        private Material paperMaterial;
        private LayeredVillageScene scene;
        private float daylight, lightFrom, lightTarget, lightElapsed;
        private bool quiet, changingLight, initialized;

        public LayeredVillageScene Scene => scene;
        public float AmbientClock => scene != null ? scene.Clock : 0;
        public float DaylightAmount => daylight;
        public int ActiveLampCount => scene != null && daylight < .99f ? scene.LampCount : 0;
        public bool WaterAnimationEnabled => scene != null && scene.WaterAnimationEnabled;
        public bool LampFlickerEnabled => scene != null && scene.LampFlickerEnabled;
        public float FirstLampIntensity => scene != null ? scene.FirstLampIntensity : 0;
        public bool WolfArticulated => scene != null && scene.WolfIndependentPartCount == 2;
        public int WolfMeshVertexCount => 0;
        public int WolfIndependentPartCount => scene != null ? scene.WolfIndependentPartCount : 0;
        public float WolfHeadAngleDegrees => scene != null ? scene.WolfHeadAngleDegrees : 0;
        public float WolfLeftEarAngleDegrees => 0;
        public float WolfRightEarAngleDegrees => 0;
        public float WolfEyeGlow => 0;
        public bool MoonIndependent => scene != null && scene.HasIndependentMoon;
        public Vector3 MoonPosition => scene != null ? scene.MoonPosition : Vector3.zero;
        public float CloudDisplacement => scene != null ? scene.CloudDisplacement : 0;
        // Kept for source compatibility only: the old full-picture detail mapper is not instantiated.
        public VillageDetails Details => null;

        public void Initialize()
        {
            if (initialized) return;
            initialized = true;
            stageCamera = Camera.main;
            if (stageCamera == null)
            {
                var cameraObject = new GameObject("Storybook camera", typeof(Camera), typeof(AudioListener));
                cameraObject.transform.SetParent(transform, false); cameraObject.tag = "MainCamera";
                stageCamera = cameraObject.GetComponent<Camera>();
            }
            stageCamera.orthographic = true; stageCamera.orthographicSize = 4.66f;
            stageCamera.transform.position = new Vector3(0, 0, -10);
            stageCamera.clearFlags = CameraClearFlags.SolidColor; stageCamera.backgroundColor = UITheme.Midnight;
            var shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default") ?? Shader.Find("Sprites/Default");
            if (shader != null) paperMaterial = new Material(shader) { name = "Separate village paper objects" };
            var content = new GameObject("Layered independent village"); content.transform.SetParent(transform, false);
            scene = content.AddComponent<LayeredVillageScene>(); scene.Initialize(stageCamera, paperMaterial);
            scene.SetReducedMotion(quiet); scene.SetDaylight(daylight);
        }
        public void BindPublic(RoomView view) => scene?.BindPublic(view);
        public void SetDaylight(float amount)
        { changingLight = false; ApplyDaylight(amount); }
        public void TransitionToDaylight(float amount)
        {
            var target = Mathf.Clamp01(amount);
            if (changingLight && Mathf.Approximately(lightTarget, target) || !changingLight && Mathf.Approximately(daylight, target)) return;
            lightFrom = daylight; lightTarget = target; lightElapsed = 0; changingLight = true;
        }
        private void ApplyDaylight(float amount)
        { daylight = Mathf.Clamp01(amount); scene?.SetDaylight(daylight); }
        public void SetMotionReduced(bool enabled)
        { quiet = enabled; scene?.SetReducedMotion(enabled); }
        public void SetHomeComposition(bool enabled) => scene?.SetHomeComposition(enabled);
        private void LateUpdate()
        {
            if (!changingLight || scene == null) return;
            lightElapsed += Time.unscaledDeltaTime;
            var progress = Mathf.Clamp01(lightElapsed / (quiet ? .28f : 3.4f));
            ApplyDaylight(Mathf.Lerp(lightFrom, lightTarget, Mathf.SmoothStep(0, 1, progress)));
            if (progress >= 1) changingLight = false;
        }
        private void OnDestroy() { if (paperMaterial != null) Destroy(paperMaterial); }
    }
}
