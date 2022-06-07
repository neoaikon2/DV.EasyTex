using HarmonyLib;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityModManagerNet;

namespace EasyTex
{
	public class Main
	{
		static UnityModManager.ModEntry _mod;
		static AssetBundle bundle;
		static GameObject[] patchedPrefabs = { null };

		static void Load(UnityModManager.ModEntry mod)
		{
			// Setup UMM
			_mod = mod;

			// Setup the harmony patches
			Harmony harmony = new Harmony(_mod.Info.Id);
			harmony.Patch(
				original: AccessTools.Method(typeof(CarTypes), nameof(CarTypes.GetCarPrefab)),
				postfix: new HarmonyMethod(typeof(Patches), nameof(Patches.GetCarPrefab_Patch)));
		}

		public class Patches
		{
			private static string[] carTankLODMap = new string[] {
				"car_tanker_lod/car_tanker_LOD0",
				"car_tanker_lod/car_tanker_LOD1",
				"car_tanker_lod/car_tanker_LOD2",
				"car_tanker_lod/car_tanker_LOD3"};
			private static string[] cabooseLODMap = new string[] {
				"CarCaboose_exterior/CabooseExterior",
				"CarCaboose_exterior/CabooseExterior_LOD1",
				"CarCaboose_exterior/CabooseExterior_LOD2",
				"CarCaboose_exterior/Caboose_LOD3"};
			private static string[] flatCarLODMap = new string[] {
				"car_flatcar_lod/flatcar",
				"car_flatcar_lod/car_flatcar_LOD1",
				"car_flatcar_lod/car_flatcar_LOD2",
				"car_flatcar_lod/car_flatcar_LOD3"};
			private static string[] boxCarLODMap = new string[] {
				"car_boxcar_lod/car_boxcar",
				"car_boxcar_lod/car_boxcar_LOD1",
				"car_boxcar_lod/car_boxcar_LOD2",
				"car_boxcar_lod/car_boxcar_LOD3"};
			private static string[] passengerLODMap = new string[]
			{
				"car_passenger_lod/passenger_car/exterior",
				"car_passenger_lod/passenger_car/front doors",
				"car_passenger_lod/passenger_car/side doors",
				"car_passenger_lod/car_passenger_LOD1",
				"car_passenger_lod/car_passenger_LOD2",
				"car_passenger_lod/car_passenger_LOD3"
			};

			private static string[] patchedPrefabList = new string[]
			{
				"Patched_CarTanker",
				"Patched_CarCaboose",
				"Patched_FlatCar",
				"Patched_BoxCar",
				"Patched_PassengerCar"
			};

			// Return a LOD mapping based on the supplied carType
			private static string[] GetLODs(TrainCarType carType)
			{
				switch (carType)
				{
					case TrainCarType.TankBlack:
					case TrainCarType.TankBlue:
					case TrainCarType.TankChrome:
					case TrainCarType.TankOrange:
					case TrainCarType.TankWhite:
					case TrainCarType.TankYellow:
						return carTankLODMap;
					case TrainCarType.CabooseRed:
						return cabooseLODMap;
					// Disabled, but I'm not willing to remove it completely
					//case TrainCarType.FlatbedEmpty:
					//	return flatCarLODMap;
					case TrainCarType.BoxcarBrown:
					case TrainCarType.BoxcarGreen:
					case TrainCarType.BoxcarPink:
					case TrainCarType.BoxcarRed:
						return boxCarLODMap;
					case TrainCarType.PassengerBlue:
					case TrainCarType.PassengerGreen:
					case TrainCarType.PassengerRed:
						return passengerLODMap;
					default:
						return null;
				}
			}

			// Load/reload the prefabs from the asset bundle
			static void LoadPrefabs()
			{
				// Load the patched prefab bundle (if not already)
				if (bundle == null)
					bundle = AssetBundle.LoadFromFile(Path.Combine(_mod.Path, "patched.assets"));

				// Load a bundle with all the patched prefabs contained within				
				if (patchedPrefabs[0] == null)
				{
					patchedPrefabs = new GameObject[patchedPrefabList.Length];
					for (int i = 0; i < patchedPrefabList.Length; i++)
						patchedPrefabs[i] = (GameObject)bundle.LoadAsset(patchedPrefabList[i]);
				}
			}

			// Return a patchec prefab based on the specified carType
			static GameObject GetPatchedPrefab(TrainCarType carType)
			{
				if (patchedPrefabs[0] == null) LoadPrefabs();
				switch (carType)
				{
					case TrainCarType.TankBlack:
					case TrainCarType.TankBlue:
					case TrainCarType.TankChrome:
					case TrainCarType.TankOrange:
					case TrainCarType.TankWhite:
					case TrainCarType.TankYellow:
						return patchedPrefabs[0];
					case TrainCarType.CabooseRed:
						return patchedPrefabs[1];
					// Disabled, but I'm not willing to remove it completely
					//case TrainCarType.FlatbedEmpty:
					//	return patchedPrefabs[2];
					case TrainCarType.BoxcarBrown:
					case TrainCarType.BoxcarGreen:
					case TrainCarType.BoxcarPink:
					case TrainCarType.BoxcarRed:
						return patchedPrefabs[3];
					case TrainCarType.PassengerBlue:
					case TrainCarType.PassengerGreen:
					case TrainCarType.PassengerRed:
						return patchedPrefabs[4];
					default:
						return null;
				}
			}

			public static void GetCarPrefab_Patch(ref GameObject __result)
			{
				// Return if result is invalid
				if (__result == null)
					return;

				// Get the traincar component
				TrainCar tc = __result.GetComponent<TrainCar>();

				// Get the LOD tree for the carType, return on invalid carType
				string[] lods = GetLODs(tc.carType);
				if (lods == null) return;

				// Patch it!
				// Go through each LOD and copy the UVs from the patched meshes
				// to the OG meshes
				foreach (string lod in lods)
				{
					Mesh m = GetPatchedPrefab(tc.carType).transform.Find(lod).GetComponent<MeshFilter>().mesh;
					m.RecalculateBounds();
					m.Optimize();
					__result.transform.Find(lod).GetComponent<MeshFilter>().mesh = m;
				}
				Debug.Log("Successfully patched mesh for " + tc.name);
				// LittleLad: Nice AND easy!
			}
		}
	}
}