﻿using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace HS2_BetterPenetration
{

    [BepInPlugin("animal42069.HS2betterpenetration", "HS2 Better Penetration", VERSION)]
    [BepInProcess("HoneySelect2")]
    [BepInProcess("HoneySelect2VR")]
    public class HS2_BetterPenetration : BaseUnityPlugin
    {
        public const string VERSION = "2.1.9.0";
        private static Harmony harmony;
		private static HScene hScene;
        private static readonly ConfigEntry<float>[] _dan_length = new ConfigEntry<float>[2];
        private static readonly ConfigEntry<float>[] _dan_girth = new ConfigEntry<float>[2];
        private static readonly ConfigEntry<float>[] _dan_sack_size = new ConfigEntry<float>[2];
        private static readonly ConfigEntry<float>[] _dan_softness = new ConfigEntry<float>[2];
        private static readonly ConfigEntry<float>[] _dan_collider_headlength = new ConfigEntry<float>[2];
        private static readonly ConfigEntry<float>[] _dan_collider_radius = new ConfigEntry<float>[2];
        private static readonly ConfigEntry<float>[] _dan_collider_verticalcenter = new ConfigEntry<float>[2]; 
        private static readonly ConfigEntry<float>[] _allow_telescope_percent = new ConfigEntry<float>[2];
        private static readonly ConfigEntry<bool>[] _force_telescope = new ConfigEntry<bool>[2];

        private static ConfigEntry<float> _clipping_depth;
        private static ConfigEntry<float> _kokanForwardOffset;
        private static ConfigEntry<float> _kokanUpOffset;
        private static ConfigEntry<float> _headForwardOffset;
        private static ConfigEntry<float> _headUpOffset;
        private static readonly List<ConfigEntry<float>> _front_collision_point_offset = new List<ConfigEntry<float>>();
        private static readonly List<ConfigEntry<float>> _back_collision_point_offset = new List<ConfigEntry<float>>();

        private static bool inHScene = false;
        private static bool b2MAnimation;

        public static AIChara.ChaControl[] fem_list;
        public static AIChara.ChaControl[] male_list;
        public static List<DynamicBone>[] kokanBones = new List<DynamicBone>[2];
        public static DynamicBoneCollider[] danCollider = new DynamicBoneCollider[2];

        private static readonly bool[] bDansFound = new bool[2] { false, false };
        private static DanPoints[] danPoints;
        private static readonly bool[] bDanPenetration = new bool[2] { false, false };
        private static Transform[] referenceLookAtTarget;
        private static readonly Transform[] bpKokanTarget = new Transform[2];
        private static readonly Quaternion[] lastDanRotation = new Quaternion[2] { new Quaternion(0, 0, 0, 0), new Quaternion(0, 0, 0, 0) };
        private static readonly Vector3[] lastDan109Position = new Vector3[2] { new Vector3(0, 0, 0), new Vector3(0, 0, 0) };
        private static readonly float[] lastDan101TargetDistance = new float[2] { 0, 0 };
        private static readonly float[] lastAdjustTime = new float[2] { 0, 0 };
        private static readonly bool[] changingAnimations = new bool[2] { false, false };
        private static readonly bool[] bHPointsFound = new bool[2] { false, false };
        private static readonly int[] targetF = new int[2] { 0, 0 };
        private static ConstrainPoints[] constrainPoints;

        private const string head_target = "k_f_head_00";
        private const string chest_target = "k_f_spine03_00";
        private const string kokan_target = "k_f_kokan_00";
        private const string bp_kokan_target = "cf_J_Vagina_root";
        private const string ana_target = "k_f_ana_00";
        private const string dan_base = "cm_J_dan101_00";
        private const string dan_head = "cm_J_dan109_00";
        private const string dan_sack = "cm_J_dan_f_top";

        private const string headHPoint = "cf_J_Head";
        private static readonly string[] frontHPointsList = { kokan_target, "cf_J_sk_00_02", "N_Waist_f", "k_f_spine03_03" };
        private static readonly string[] backHPointsList = { ana_target, "cf_J_sk_04_02", "N_Waist_b", "N_Back" };
        private static readonly float[] frontOffsets = { -0.35f, 0.25f, 0f, -0.65f };
        private static readonly float[] backOffsets = { -0.05f, 0.25f, 0.05f, 0.05f };
        private static readonly bool[] frontHPointsInward = { false, false, false, false };
        private static readonly bool[] backHPointsInward = { false, false, true, true };

        private static readonly string[] colliderList = { "cf_J_Vagina_Collider_B", "cf_J_Vagina_Collider_F", "cf_J_Vagina_Collider_Inner_F", "cf_J_Vagina_Collider_L.005", "cf_J_Vagina_Collider_R.005" };
        private static readonly string[] dynamicBonesList = { "cf_J_Vagina_Pivot_B", "cf_J_Vagina_Pivot_F", "cf_J_Vagina_Pivot_Inner_F", "cf_J_Vagina_Pivot_L.005", "cf_J_Vagina_Pivot_R.005" };
        private static readonly float[] colliderHeightList = { 0.39f, 0.19f, 0.34f, 0.39f, 0.39f };
        private static readonly float[] colliderRadiusList = { 0.0021f, 0.0011f, 0.0011f, 0.0021f, 0.0021f };

        private void Awake()
        {
            for (int maleNum = 0; maleNum < _dan_length.Length; maleNum++)
            {
                _dan_collider_headlength[maleNum] = Config.Bind<float>("Male " + (maleNum + 1) + " Options", "Collider: Length of Head", 0.4f, "Distance from the center of the head bone to the tip, used for collision purposes.");
                _dan_collider_radius[maleNum] = Config.Bind<float>("Male " + (maleNum + 1) + " Options", "Collider: Radius of Shaft", 0.32f, "Radius of the shaft collider.");
                _dan_collider_verticalcenter[maleNum] = Config.Bind<float>("Male " + (maleNum + 1) + " Options", "Collider: Vertical Center", -0.03f, "Vertical Center of the shaft collider");
                _dan_length[maleNum] = Config.Bind<float>("Male " + (maleNum + 1) + " Options", "Penis: Length", 1.75f, "Set the length of the penis.  Apparent Length is about 0.2 larget than this, depending on uncensor.  2.0 is about 8 inches or 20 cm.");
                _dan_girth[maleNum] = Config.Bind<float>("Male " + (maleNum + 1) + " Options", "Penis: Girth", 1.0f, "Set the scale of the circumference of the penis.");
                _dan_sack_size[maleNum] = Config.Bind<float>("Male " + (maleNum + 1) + " Options", "Penis: Sack Size", 1.0f, "Set the scale (size) of the sack");
                _dan_softness[maleNum] = Config.Bind<float>("Male " + (maleNum + 1) + " Options", "Penis: Softness", 0.15f, "Set the softness of the penis.  A value of 0 means maximum hardness, the penis will remain the same length at all times.  A value greater than 0 will cause the penis to begin to telescope after penetration.  A small value can make it appear there is friction during penetration.");
                _allow_telescope_percent[maleNum] = Config.Bind<float>("Male " + (maleNum + 1) + " Options", "Limiter: Telescope Threshold", 0.6f, "Allow the penis to begin telescoping after it has penetrated a certain amount. 0 = never telescope, 0.5 = allow telescoping after the halfway point, 1 = always allow telescoping.");
                _force_telescope[maleNum] = Config.Bind<bool>("Male " + (maleNum + 1) + " Options", "Limiter: Telescope Always", true, "Force the penis to always telescope at the threshold point, instead of only doing it when it prevents clipping.");

                _dan_girth[maleNum].SettingChanged += delegate
                {
                    for (int index = 0; index < _dan_girth.Length; index++)
                    {
                        if (inHScene && bDansFound[index])
                        {
                            danPoints[index].danStart.localScale = new Vector3(_dan_girth[index].Value, _dan_girth[index].Value, 1);
                        }
                    }
                };

                _dan_sack_size[maleNum].SettingChanged += delegate
                {
                    for (int index = 0; index < _dan_girth.Length; index++)
                    {
                        if (inHScene && bDansFound[index])
                        {
                            danPoints[index].danTop.localScale = new Vector3(_dan_sack_size[index].Value, _dan_sack_size[index].Value, _dan_sack_size[index].Value);
                        }
                    }
                };

                _dan_collider_radius[maleNum].SettingChanged += delegate
                {
                    for (int index = 0; index < _dan_girth.Length; index++)
                    { 
                        if (inHScene && danCollider[index] != null)
                        {
                            danCollider[index].m_Radius = _dan_collider_radius[index].Value;
                        }
                    }
                };

                _dan_collider_headlength[maleNum].SettingChanged += delegate
                {
                    for (int index = 0; index < _dan_girth.Length; index++)
                    {
                        if (inHScene && danCollider[index] != null)
                        {
                            danCollider[index].m_Height = _dan_length[index].Value + (_dan_collider_headlength[index].Value * 2);
                        }
                    }
                };

            }

            _clipping_depth = Config.Bind<float>("Female Options", "Clipping Depth", 0.25f, "Set how close to body surface to limit penis for clipping purposes. Smaller values will result in more clipping through the body, larger values will make the shaft wander further away from the intended penetration point.");
            for (int femaleNum = 0; femaleNum < frontOffsets.Length; femaleNum++)
                _front_collision_point_offset.Add(Config.Bind<float>("Female Options", "Clipping Offset: Front Collision " + femaleNum, frontOffsets[femaleNum], "Individual offset on colision point, to improve clipping"));
            for (int femaleNum = 0; femaleNum < backOffsets.Length; femaleNum++)
                _back_collision_point_offset.Add(Config.Bind<float>("Female Options", "Clipping Offset: Back Collision " + femaleNum, backOffsets[femaleNum], "Individual offset on colision point, to improve clipping"));
            _kokanForwardOffset = Config.Bind<float>("Female Options", "Target Offset: Vagina Vertical", 0.0f, "Vertical offset of the vagina target");
            _kokanUpOffset = Config.Bind<float>("Female Options", "Target Offset: Vagina Depth", 0.0f, "Depth offset of the vagina target");
            _headForwardOffset = Config.Bind<float>("Female Options", "Target Offset: Mouth Depth", 0.0f, "Depth offset of the mouth target");
            _headUpOffset = Config.Bind<float>("Female Options", "Target Offset: Mouth Vertical", 0.025f, "Vertical offset of the mouth target");

            harmony = new Harmony("HS2_BetterPenetration");
            SceneManager.sceneLoaded += SceneManager_sceneLoaded;
        }

        [HarmonyPostfix, HarmonyPatch(typeof(HScene), "SetStartVoice")]
        public static void AddPColliders(HScene __instance)
        {
            hScene = __instance;
            male_list = __instance.GetMales().Where(male => male != null).ToArray();
            fem_list = __instance.GetFemales().Where(female => female != null).ToArray();

            danPoints = new DanPoints[male_list.Length];
            referenceLookAtTarget = new Transform[male_list.Length];
            constrainPoints = new ConstrainPoints[fem_list.Length];
            kokanBones = new List<DynamicBone>[fem_list.Length];

            int maleNum = 0;
            foreach (var male in male_list.Where(male => male != null))
            {
                Transform dan101 = male.GetComponentsInChildren<Transform>().Where(x => x.name.Contains(dan_base)).FirstOrDefault();
                Transform dan109 = male.GetComponentsInChildren<Transform>().Where(x => x.name.Contains(dan_head)).FirstOrDefault();
                Transform danTop = male.GetComponentsInChildren<Transform>().Where(x => x.name.Contains(dan_sack)).FirstOrDefault();

                bDansFound[maleNum] = false;
                bDanPenetration[maleNum] = false;
                targetF[maleNum] = 0;
                if (dan101 != null && dan109 != null && danTop != null)
                {
                    danPoints[maleNum] = new DanPoints(dan101, dan109, danTop);

                    bDansFound[maleNum] = true;
                    dan101.localScale = new Vector3(_dan_girth[maleNum].Value, _dan_girth[maleNum].Value, 1);

                    danCollider[maleNum] = dan101.GetComponent<DynamicBoneCollider>();

                    if (danCollider[maleNum] == null)
                        danCollider[maleNum] = dan101.gameObject.AddComponent(typeof(DynamicBoneCollider)) as DynamicBoneCollider;
                    
                    danCollider[maleNum].m_Direction = DynamicBoneColliderBase.Direction.Z;
                    danCollider[maleNum].m_Center = new Vector3(0, _dan_collider_verticalcenter[maleNum].Value, _dan_length[maleNum].Value / 2);
                    danCollider[maleNum].m_Bound = DynamicBoneColliderBase.Bound.Outside;
                    danCollider[maleNum].m_Radius = _dan_collider_radius[maleNum].Value;
                    danCollider[maleNum].m_Height = _dan_length[maleNum].Value + (_dan_collider_headlength[maleNum].Value * 2);
					
                    danPoints[maleNum].danTop.localScale = new Vector3(_dan_sack_size[maleNum].Value, _dan_sack_size[maleNum].Value, _dan_sack_size[maleNum].Value);
                    lastDan109Position[maleNum] = danPoints[maleNum].danEnd.position;
                    lastDanRotation[maleNum] = danPoints[maleNum].danEnd.rotation;
                    lastAdjustTime[maleNum] = Time.time;
                }

                referenceLookAtTarget[maleNum] = danPoints[maleNum].danEnd;
                lastDan101TargetDistance[maleNum] = Vector3.Distance(referenceLookAtTarget[maleNum].position, danPoints[maleNum].danStart.position);
                Console.WriteLine("bDansFound " + bDansFound[maleNum]);
                maleNum++;
            }

            int femaleNum = 0;
            foreach (var female in fem_list.Where(female => female != null))
            {
                List<Transform> frontHPoints = new List<Transform>();
                List<Transform> backHPoints = new List<Transform>();
                Transform hPointBackOfHead;
                bHPointsFound[femaleNum] = false;

                for (int index = 0; index < frontHPointsList.Length; index++)
                    frontHPoints.Add(female.GetComponentsInChildren<Transform>().Where(x => x.name.Contains(frontHPointsList[index])).FirstOrDefault());

                for (int index = 0; index < backHPointsList.Length; index++)
                    backHPoints.Add(female.GetComponentsInChildren<Transform>().Where(x => x.name.Contains(backHPointsList[index])).FirstOrDefault());

                hPointBackOfHead = female.GetComponentsInChildren<Transform>().Where(x => x.name.Contains(headHPoint)).FirstOrDefault();

                bpKokanTarget[femaleNum] = female.GetComponentsInChildren<Transform>().Where(x => x.name.Equals(bp_kokan_target)).FirstOrDefault();
                if (bpKokanTarget[femaleNum] != null)
                {
                    Console.WriteLine("BP Target Found " + bpKokanTarget[femaleNum].name);
                    frontHPoints[0] = bpKokanTarget[femaleNum];
                }

                if (frontHPoints.Count == frontHPointsList.Length && backHPoints.Count == backHPointsList.Length && hPointBackOfHead != null)
                {
                    bHPointsFound[femaleNum] = true;
                    constrainPoints[femaleNum] = new ConstrainPoints(frontHPoints, backHPoints, hPointBackOfHead);
                }

                foreach (DynamicBone db in female.GetComponentsInChildren<DynamicBone>().Where(x => x.name.Contains("cf_J_Vagina")))
                    db.m_Colliders.Clear();

                Console.WriteLine("bHPointsFound " + bHPointsFound[femaleNum]);
                for (int i = 0; i < colliderList.Length; i++)
                {
                    DynamicBone db = female.GetComponentsInChildren<DynamicBone>().Where(x => x.m_Root.name.Equals(dynamicBonesList[i])).FirstOrDefault();
                    DynamicBoneCollider dbc = female.GetComponentsInChildren<DynamicBoneCollider>().Where(x => x.name.Equals(colliderList[i])).FirstOrDefault();

                    if (dbc == null)
                    {
                        Transform colliderTransform = female.GetComponentsInChildren<Transform>().Where(x => x.name.Contains(colliderList[i])).FirstOrDefault();

                        if (colliderTransform != null)
                        {
                            dbc = colliderTransform.gameObject.AddComponent(typeof(DynamicBoneCollider)) as DynamicBoneCollider;
                            dbc.m_Bound = DynamicBoneColliderBase.Bound.Inside;
                            dbc.m_Direction = DynamicBoneColliderBase.Direction.Y;
                            dbc.m_Height = colliderHeightList[i];
                            dbc.m_Radius = colliderRadiusList[i];
                        }
                    }

                    if (db != null && dbc != null)
                    {             
                        db.m_Colliders.Add(dbc);

                        Console.WriteLine(dbc.name + " collider radius " + dbc.m_Radius + ", height: " + dbc.m_Height);

                        foreach (DynamicBoneColliderBase dbcb in db.m_Colliders)
                            Console.WriteLine(db.m_Root.name + " collider " + dbcb.name);
                    }
                    else
                    {
                        if (db == null)
                            Console.WriteLine(dynamicBonesList[i] + " bone not found for " + female.name);
                        if (dbc == null)
                            Console.WriteLine(colliderList[i] + " collider not found for " + female.name);
                    }
                }

                List<DynamicBone> dbList = new List<DynamicBone>();
                foreach (DynamicBone db in female.GetComponentsInChildren<DynamicBone>().Where(x => x.name.Contains("cf_J_Vagina")))
                {
                    if (db != null)
                    {
                        dbList.Add(db);
                    }
                }

                kokanBones[femaleNum] = dbList;
                femaleNum++;
            }
            inHScene = true;
            Console.WriteLine("AddColliders done.");
        }

        [HarmonyPrefix, HarmonyPatch(typeof(HScene), "ChangeAnimation")]
        private static void HScene_ChangeAnimation()
        {
            if (!inHScene)
                return;

            for (int maleNum = 0; maleNum < male_list.Length; maleNum++)
            	changingAnimations[maleNum] = true;
        }

        [HarmonyPostfix, HarmonyPatch(typeof(H_Lookat_dan), "setInfo")]
        private static void H_Lookat_dan_ChangeTarget(H_Lookat_dan __instance, System.Text.StringBuilder ___assetName, AIChara.ChaControl ___male)
        {

            if (!inHScene || __instance == null)
                return;

            int maleNum = 0;
            if (___male != null && ___male.chaID != 99)
                maleNum = 1;

            if (!bDansFound[maleNum])
                return;

            b2MAnimation = false;
            if (___assetName != null && ___assetName.Length != 0 && ___assetName.ToString().Contains("m2f"))
                b2MAnimation = true;

            if (maleNum == 1 && b2MAnimation == false)
                return;

            targetF[maleNum] = __instance.numFemale;
            if (targetF[maleNum] >= constrainPoints.Length)
                targetF[maleNum] = 0;

            if (!bHPointsFound[targetF[maleNum]])
                return;

            SetupNewDanTarget(__instance, maleNum);
            SetDanTarget(maleNum);
        }

        [HarmonyPostfix, HarmonyPatch(typeof(H_Lookat_dan), "LateUpdate")]
        public static void H_Lookat_dan_LateUpdate(H_Lookat_dan __instance, AIChara.ChaControl ___male)
        {
            if (!inHScene)
                return;

            int maleNum = 0;
            if (___male != null && ___male.chaID != 99)
            {
                if (!b2MAnimation)
                    return;
                maleNum = 1;
            }

            if (!bDansFound[maleNum])
                return;

            danPoints[maleNum].danStart.localScale = new Vector3(_dan_girth[maleNum].Value, _dan_girth[maleNum].Value, 1);
            danPoints[maleNum].danTop.localScale = new Vector3(_dan_sack_size[maleNum].Value, _dan_sack_size[maleNum].Value, _dan_sack_size[maleNum].Value);

            if (!bHPointsFound[targetF[maleNum]])
                return;

			if (changingAnimations[maleNum] && !hScene.NowChangeAnim)
		        SetupNewDanTarget(__instance, maleNum);

		    SetDanTarget(maleNum);
        }

        private static void SetupNewDanTarget(H_Lookat_dan lookAtDan, int maleNum)
        {
            referenceLookAtTarget[maleNum] = danPoints[maleNum].danEnd;
            lastDan109Position[maleNum] = danPoints[maleNum].danEnd.position;
            lastDanRotation[maleNum] = danPoints[maleNum].danEnd.rotation;
            lastAdjustTime[maleNum] = Time.time;
            bDanPenetration[maleNum] = false;
            changingAnimations[maleNum] = false;
            if (lookAtDan != null && lookAtDan.transLookAtNull != null && lookAtDan.strPlayMotion != null && lookAtDan.transLookAtNull.name != chest_target &&
                lookAtDan.strPlayMotion.Contains("Idle") == false && lookAtDan.strPlayMotion.Contains("OUT") == false && lookAtDan.strPlayMotion.Contains("Drop") == false)
            {
                bDanPenetration[maleNum] = true;
                referenceLookAtTarget[maleNum] = lookAtDan.transLookAtNull;

                if (referenceLookAtTarget[maleNum].name == kokan_target)
                {
                    if (bpKokanTarget[targetF[maleNum]] != null)
                    {
                        Console.WriteLine("SetupNewDanTarget using BP target");
                        referenceLookAtTarget[maleNum] = bpKokanTarget[targetF[maleNum]];
                    }
                } 
            }

            if (danCollider[maleNum] != null)
            {
                if (referenceLookAtTarget[maleNum].name != ana_target && referenceLookAtTarget[maleNum].name != head_target && referenceLookAtTarget[maleNum].name != chest_target)
                {
                    foreach (DynamicBone db in kokanBones[targetF[maleNum]])
                    {
                        if (db.m_Colliders.Contains(danCollider[maleNum]))
                        {
                            Console.WriteLine("Instance of " + danCollider[maleNum].name + " already exists in list for DB " + db.m_Root.name);
                        }
                        else
                        {
                            db.m_Colliders.Add(danCollider[maleNum]);
                            Console.WriteLine(danCollider[maleNum].name + " added to " + fem_list[targetF[maleNum]].name + " for bone " + db.m_Root.name);
                        }
                    }
                }
                else
                {
                    foreach (DynamicBone db in kokanBones[targetF[maleNum]])
                    {
                        if (db.m_Colliders.Contains(danCollider[maleNum]))
                        {
                            Console.WriteLine("Removing instance of " + danCollider[maleNum].name + " in list for DB " + db.m_Root.name);
                            db.m_Colliders.Remove(danCollider[maleNum]);
                        }
                    }
                }
            }

            lastDan101TargetDistance[maleNum] = Vector3.Distance(referenceLookAtTarget[maleNum].position, danPoints[maleNum].danStart.position);
        }

        private static void SetDanTarget(int maleNum)
        {
            Vector3 dan101_pos = danPoints[maleNum].danStart.position;
            Vector3 lookTarget = referenceLookAtTarget[maleNum].position;

            if (referenceLookAtTarget[maleNum].name == kokan_target || referenceLookAtTarget[maleNum].name == bp_kokan_target)
                lookTarget = lookTarget + (referenceLookAtTarget[maleNum].forward * _kokanForwardOffset.Value) + (referenceLookAtTarget[maleNum].up * _kokanUpOffset.Value);
            if (referenceLookAtTarget[maleNum].name == head_target)
                lookTarget = lookTarget + (referenceLookAtTarget[maleNum].forward * _headForwardOffset.Value) + (referenceLookAtTarget[maleNum].up * _headUpOffset.Value);

            float distDan101ToTarget = Vector3.Distance(dan101_pos, lookTarget);
            if (distDan101ToTarget == 0)
                return;

            Vector3 danVector = Vector3.Normalize(lookTarget - dan101_pos);
            Vector3 dan109_pos = dan101_pos + danVector * _dan_length[maleNum].Value;

            float adjustTime = Time.time;
            float timeSinceLastAdjust = adjustTime - lastAdjustTime[maleNum];
            lastAdjustTime[maleNum] = adjustTime;

            if (timeSinceLastAdjust < 0.0001)
            {
                danPoints[maleNum].danStart.rotation = lastDanRotation[maleNum];
                danPoints[maleNum].danEnd.SetPositionAndRotation(lastDan109Position[maleNum], lastDanRotation[maleNum]);
                return;
            }

            if (bDanPenetration[maleNum])
            {
                if (referenceLookAtTarget[maleNum].name == kokan_target || referenceLookAtTarget[maleNum].name == ana_target || referenceLookAtTarget[maleNum].name == bp_kokan_target)
                {
                    List<Vector3> frontHitPoints = new List<Vector3>();
                    List<Vector3> backHitPoints = new List<Vector3>();

                    for (int index = 0; index < constrainPoints[targetF[maleNum]].frontConstrainPoints.Count; index++)
                    {
                        if (frontHPointsInward[index])
                            frontHitPoints.Add(constrainPoints[targetF[maleNum]].frontConstrainPoints[index].position + (_clipping_depth.Value + _front_collision_point_offset[index].Value) * constrainPoints[targetF[maleNum]].frontConstrainPoints[index].forward);
                        else
                            frontHitPoints.Add(constrainPoints[targetF[maleNum]].frontConstrainPoints[index].position - (_clipping_depth.Value + _front_collision_point_offset[index].Value) * constrainPoints[targetF[maleNum]].frontConstrainPoints[index].forward);
                    }
                    for (int index = 0; index < constrainPoints[targetF[maleNum]].backConstrainPoints.Count; index++)
                    {
                        if (backHPointsInward[index])
                            backHitPoints.Add(constrainPoints[targetF[maleNum]].backConstrainPoints[index].position - (_clipping_depth.Value + _back_collision_point_offset[index].Value) * constrainPoints[targetF[maleNum]].backConstrainPoints[index].forward);
                        else
                            backHitPoints.Add(constrainPoints[targetF[maleNum]].backConstrainPoints[index].position + (_clipping_depth.Value + _back_collision_point_offset[index].Value) * constrainPoints[targetF[maleNum]].backConstrainPoints[index].forward);
                    }

                    float danLength = _dan_length[maleNum].Value;
                    Plane kokanPlane = new Plane(danPoints[maleNum].danStart.forward, lookTarget);

                    if (_dan_length[maleNum].Value > distDan101ToTarget)
                        danLength = _dan_length[maleNum].Value - (_dan_length[maleNum].Value - distDan101ToTarget) * _dan_softness[maleNum].Value;

                    if (kokanPlane.GetSide(dan101_pos))
                        danLength = _dan_length[maleNum].Value * (1 - _dan_softness[maleNum].Value);

                    float minDanLength = distDan101ToTarget + (danLength * (1 - _allow_telescope_percent[maleNum].Value));

                    if (minDanLength > danLength)
                        minDanLength = danLength;

                    if (_force_telescope[maleNum].Value)
                        danLength = minDanLength;

                    dan109_pos = dan101_pos + danVector * danLength;

                    bool bHitPointFound = false;
                    bool bConstrainPastNearSide = true;
                    bool bConstrainPastFarSide = false;
                    Vector3 adjustedDanPos = dan109_pos;

                    for (int index = 1; index < constrainPoints[targetF[maleNum]].frontConstrainPoints.Count; index++)
                    {
                        if (bHitPointFound)
                            break;

                        Vector3 firstVectorRight = constrainPoints[targetF[maleNum]].frontConstrainPoints[index - 1].right;
                        Vector3 secondVectorRight = constrainPoints[targetF[maleNum]].frontConstrainPoints[index].right;

                        if (frontHPointsInward[index - 1])
                            firstVectorRight = -firstVectorRight;

                        if (frontHPointsInward[index])
                            secondVectorRight = -secondVectorRight;

                        TwistedPlane hPlane = new TwistedPlane(frontHitPoints[index - 1], firstVectorRight, frontHitPoints[index], secondVectorRight);

                        if (index == constrainPoints[targetF[maleNum]].frontConstrainPoints.Count - 1)
                            bConstrainPastFarSide = true;

                        adjustedDanPos = hPlane.ConstrainLineToTwistedPlane(dan101_pos, adjustedDanPos, ref danLength, minDanLength, ref bConstrainPastNearSide, bConstrainPastFarSide, out bHitPointFound);
                    }

                    bConstrainPastFarSide = false;
                    bConstrainPastNearSide = true;
                    for (int index = 1; index < constrainPoints[targetF[maleNum]].backConstrainPoints.Count; index++)
                    {
                        if (bHitPointFound)
                            break;

                        Vector3 firstVectorRight = constrainPoints[targetF[maleNum]].backConstrainPoints[index - 1].right;
                        Vector3 secondVectorRight = constrainPoints[targetF[maleNum]].backConstrainPoints[index].right;

                        if (!backHPointsInward[index - 1])
                            firstVectorRight = -firstVectorRight;

                        if (!backHPointsInward[index])
                            secondVectorRight = -secondVectorRight;

                        TwistedPlane hPlane = new TwistedPlane(backHitPoints[index - 1], firstVectorRight, backHitPoints[index], secondVectorRight);

                        if (index == constrainPoints[targetF[maleNum]].backConstrainPoints.Count - 1)
                            bConstrainPastFarSide = true;

                        adjustedDanPos = hPlane.ConstrainLineToTwistedPlane(dan101_pos, adjustedDanPos, ref danLength, minDanLength, ref bConstrainPastNearSide, bConstrainPastFarSide, out bHitPointFound);
                   }
                    dan109_pos = adjustedDanPos;

                    if (danCollider[maleNum] != null)
                    {
                        danCollider[maleNum].m_Center = new Vector3(0, _dan_collider_verticalcenter[maleNum].Value, danLength / 2);
                        danCollider[maleNum].m_Height = danLength + (_dan_collider_headlength[maleNum].Value * 2);
                    }
                }
                else if (referenceLookAtTarget[maleNum].name == head_target)
                {
                    float danLength;
                    float max_dist;

                    if (Vector3.Distance(dan101_pos, constrainPoints[targetF[maleNum]].headConstrainPoint.position) < Vector3.Distance(lookTarget, constrainPoints[targetF[maleNum]].headConstrainPoint.position))
                    {
                        danLength = _dan_length[maleNum].Value * (1 - _dan_softness[maleNum].Value);
                        max_dist = Vector3.Distance(dan101_pos, constrainPoints[targetF[maleNum]].headConstrainPoint.position);
                    }
                    else
                    {
                        if (_dan_length[maleNum].Value > distDan101ToTarget)
                            danLength = _dan_length[maleNum].Value - (_dan_length[maleNum].Value - distDan101ToTarget) * _dan_softness[maleNum].Value;
                        else
                            danLength = _dan_length[maleNum].Value;
                        max_dist = distDan101ToTarget + Vector3.Distance(lookTarget, constrainPoints[targetF[maleNum]].headConstrainPoint.position);
                    }

                    if (danLength > max_dist)
                        danLength = max_dist;

                    dan109_pos = dan101_pos + danVector * danLength;
                }
            }

            Vector3 danForwardVector = Vector3.Normalize(dan109_pos - dan101_pos);
            Quaternion danQuaternion = Quaternion.LookRotation(danForwardVector, Vector3.Cross(danForwardVector, danPoints[maleNum].danTop.right));

            danPoints[maleNum].danStart.rotation = danQuaternion;
            danPoints[maleNum].danEnd.SetPositionAndRotation(dan109_pos, danQuaternion);

            lastDanRotation[maleNum] = danQuaternion;
            lastDan109Position[maleNum] = dan109_pos;
            lastDan101TargetDistance[maleNum] = distDan101ToTarget;
        }

        [HarmonyPrefix, HarmonyPatch(typeof(HScene), "EndProc")]
        public static void HScene_EndProc_Patch()
        {
            Console.WriteLine("HScene::EndProc");

            inHScene = false;
            for (int index = 0; index < male_list.Length; index++)
            {
                bDansFound[index] = false;
                bDanPenetration[index] = false;
                targetF[index] = 0;
            }

            for (int index = 0; index < fem_list.Length; index++)
            {
                bHPointsFound[index] = false;
            }

            if (!inHScene)
            {
                for (int i = 0; i < kokanBones.Length; i++)
                {
                    if (kokanBones[i].Any())
                    {
                        foreach (DynamicBone kokanBone in kokanBones[i])
                        {
                            if (kokanBone != null)
                            {
                                Console.WriteLine("Clearing colliders from " + kokanBone.m_Root.name);
                                kokanBone.m_Colliders.Clear();
                            }
                        }
                    }
                }

                for (int i = 0; i < danCollider.Length; i++)
                    Destroy(danCollider[i]);

                Console.WriteLine("Clearing females list");
                Array.Clear(fem_list, 0, fem_list.Length);
                Console.WriteLine("Clearing males list");
                Array.Clear(male_list, 0, male_list.Length);
            }
        }

        private static void SceneManager_sceneLoaded(Scene scene, LoadSceneMode lsm)
        {
            if (lsm != LoadSceneMode.Single)
                return;

            if (scene.name == "HScene")
                harmony.PatchAll(typeof(HS2_BetterPenetration));
            else
                harmony.UnpatchAll(nameof(HS2_BetterPenetration));
        }
    }
}