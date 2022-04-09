using Modding;
using System.Collections.Generic;
using UnityEngine;
using UObject = UnityEngine.Object;
using UCamera = UnityEngine.Camera;
using InControl;
using GlobalEnums;
using System;

namespace Mirrored_Hallownest
{
    public class isFlipped : MonoBehaviour
    {
        public bool flipped = false;
    }
    public class Mirrored_Hallownest : Mod
    {
        internal static Mirrored_Hallownest Instance;

        private Matrix4x4 _reflectMatrix = Matrix4x4.identity;
        private Matrix4x4 _reflectMatrixBlur = Matrix4x4.identity;
        private bool flipPrompt = false;
        //public override List<ValueTuple<string, string>> GetPreloadNames()
        //{
        //    return new List<ValueTuple<string, string>>
        //    {
        //        new ValueTuple<string, string>("White_Palace_18", "White Palace Fly")
        //    };
        //}

        public Mirrored_Hallownest() : base("Mirrored Hallownest")
        {
            Instance = this;
        }

        public override string GetVersion() => "0.1.0.0";

        public void Unload() //i planned for this to be a togglable mod but i couldn't make it work properly
        {
            On.tk2dCamera.UpdateCameraMatrix -= OnUpdateCameraMatrix;
            On.GameCameras.StartScene -= OnNewSceneCam;
            On.InputHandler.ActionButtonToPlayerAction -= OnAction;
            flipPrompt = false;
            //On.HeroController.GetCState -= OnCState;
            if (hasBlurCam(GameCameras.instance))
            {
                FlipUCam(GameCameras.instance.tk2dCam.transform.GetComponentInChildren<UCamera>());
            }
        }

        public override void Initialize(Dictionary<string, Dictionary<string, GameObject>> preloadedObjects)
        {
            Log("Initializing");

            Instance = this;
            On.tk2dCamera.UpdateCameraMatrix += OnUpdateCameraMatrix;
            On.GameCameras.StartScene += OnNewSceneCam;
            On.InputHandler.ActionButtonToPlayerAction += OnAction;
            On.ObjectPool.Spawn_GameObject_Transform_Vector3_Quaternion += OnSpawnObject;
            On.GameMap.Update += GameMapUpdate;
            flipPrompt = true;

            Log("Initialized");
        }

        private void GameMapUpdate(On.GameMap.orig_Update orig, GameMap self)
        {
            orig(self);
            
            float scaleX = self.transform.GetScaleX();
            if (scaleX >= 0)
            {
                self.transform.SetScaleX(scaleX * -1);
            }
        }

        private PlayerAction OnAction(On.InputHandler.orig_ActionButtonToPlayerAction orig, InputHandler self, HeroActionButton actionButtonType)
        {
            HeroActionButton newButton = actionButtonType;
            if (actionButtonType == HeroActionButton.LEFT) newButton = HeroActionButton.RIGHT;
            else if (actionButtonType == HeroActionButton.RIGHT) newButton = HeroActionButton.LEFT;
            return orig(self, newButton);

        }

        void exchangeInputs(PlayerAction a, PlayerAction b)
        {
            for (int i = 0; i < Mathf.Min(a.Bindings.Count, b.Bindings.Count); i++)
            {
                BindingSource aBinding = a.Bindings[i];
                a.ReplaceBinding(a.Bindings[i], b.Bindings[i]);
                b.ReplaceBinding(b.Bindings[i], aBinding);
            }
            bool aHigh = a.Bindings.Count > b.Bindings.Count;
            for (int i = Mathf.Min(a.Bindings.Count, b.Bindings.Count) - 1; i < (aHigh ? a.Bindings.Count - b.Bindings.Count : b.Bindings.Count - a.Bindings.Count); i++)
            {
                if (aHigh)
                {
                    b.AddBinding(a.Bindings[i]);
                    a.RemoveBinding(a.Bindings[i]);
                }else
                {
                    a.AddBinding(b.Bindings[i]);
                    b.RemoveBinding(b.Bindings[i]);
                }
            }
        }


        private void OnNewSceneCam(On.GameCameras.orig_StartScene orig, GameCameras self)
        {
            
            orig(self);
            if (!hasBlurCam(self))
            {
                return;
            }
            if (self.tk2dCam.transform.GetComponentInChildren<isFlipped>() == null) self.tk2dCam.gameObject.AddComponent<isFlipped>();
            if (self.tk2dCam.transform.GetComponentInChildren<isFlipped>().flipped)
            {
                return;
            }

            self.tk2dCam.transform.GetComponentInChildren<isFlipped>().flipped = true;
            FlipUCam(self.tk2dCam.transform.GetComponentInChildren<UCamera>());
        }
        private void OnNewSceneCam(GameCameras self)
        {
            if (!hasBlurCam(self))
            {
                return;
            }
            if (self.tk2dCam.transform.GetComponentInChildren<isFlipped>() == null) self.tk2dCam.gameObject.AddComponent<isFlipped>();
            if (self.tk2dCam.transform.GetComponentInChildren<isFlipped>().flipped)
            {
                return;
            }

            self.tk2dCam.transform.GetComponentInChildren<isFlipped>().flipped = true;
            FlipUCam(self.tk2dCam.transform.GetComponentInChildren<UCamera>());
        }

        private GameObject OnSpawnObject(On.ObjectPool.orig_Spawn_GameObject_Transform_Vector3_Quaternion orig, GameObject prefab, Transform parent, Vector3 pos, Quaternion rot)
        {
            prefab = orig(prefab, parent, pos, rot);
            PromptMarker prefabPrompt;
            prefab.gameObject.TryGetComponent<PromptMarker>(out prefabPrompt);
            if (prefabPrompt != null)
            {
                prefab.transform.localScale = new Vector3(flipPrompt ? -1 : 1, 1, 1);
            }
            return prefab;
        }

        private void OnUpdateCameraMatrix(On.tk2dCamera.orig_UpdateCameraMatrix orig, tk2dCamera self)
        {
            orig(self);

            if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "Menu_Title") return;

            // Can't use ?. on a Unity type because they override == to null.
            if (GameCameras.instance == null || GameCameras.instance.tk2dCam == null)
                return;

            UCamera cam = self.GetComponent<UCamera>();

            if (cam == null)
                return;

            Matrix4x4 p = cam.projectionMatrix;

            //_reflectMatrix[1, 1] = -1;
            //p *= _reflectMatrix;
            _reflectMatrix[0, 0] = -1;
            p *= _reflectMatrix;

            cam.projectionMatrix = p;


            /*UCamera blurCam = cam.transform.GetComponentInChildren<UCamera>();

            if (blurCam == null)
                return;

            p = blurCam.projectionMatrix;

            //_reflectMatrix[1, 1] = -1;
            //p *= _reflectMatrix;
            _reflectMatrixBlur[0, 0] = -1;
            p *= _reflectMatrixBlur;

            blurCam.projectionMatrix = p;*/
        }

        void FlipUCam(UCamera cam)
        {
            Matrix4x4 mat = cam.projectionMatrix;
            mat *= Matrix4x4.Scale(new Vector3(-1, 1, 1));
            cam.projectionMatrix = mat;
        }
        bool hasBlurCam(GameCameras self)
        {
            return !(self.tk2dCam == null || self.tk2dCam.transform.GetComponentInChildren<Camera>() == null);
        }

    }
}