﻿using HarmonyLib;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityModManagerNet;
using DV.ThingTypes;

namespace EasyTex
{
	public class Main
	{
		static UnityModManager.ModEntry _mod;
		static AssetBundle bundle;
		static GameObject[] patchedPrefabs = { null };
		static bool slicedCarsInstalled = false;

		static void Load(UnityModManager.ModEntry mod)
		{
			// Setup UMM
			_mod = mod;

			slicedCarsInstalled = Directory.Exists(Path.Combine(mod.Path, "../SlicedPassengerCars"));
			if (slicedCarsInstalled)
				mod.Logger.Warning("Sliced Passenger Cars mod detected! Patched passenger cars have been disabled.");
			// Setup the harmony patches
			Harmony harmony = new Harmony(_mod.Info.Id);
			harmony.Patch(
				original: AccessTools.Method(typeof(TrainCar), nameof(TrainCar.GetCarPrefab)),
				postfix: new HarmonyMethod(typeof(Patches), nameof(Patches.GetCarPrefab_Patch)));

#if DEBUG
			_mod.Logger.Log("[EasyTex] Started");
#endif
		}

		public class Patches
		{
			private static string[] carTankLODMap = new string[] {
				"car_tanker_lod/car_tanker_LOD0",
				"car_tanker_lod/car_tanker_LOD1",
				"car_tanker_lod/car_tanker_LOD2",
				"car_tanker_lod/car_tanker_LOD3"};
			private static string[] carTankLODMap_Sim = new string[]
			{
				"CarTank/CarTank_LOD0",
				"CarTank/CarTank_LOD1",
				"CarTank/CarTank_LOD2",
				"CarTank/CarTank_LOD3"
			};
			private static string[] cabooseLODMap = new string[] {
				"CarCaboose_exterior/CabooseExterior",
				"CarCaboose_exterior/CabooseExterior_LOD1",
				"CarCaboose_exterior/CabooseExterior_LOD2",
				"CarCaboose_exterior/Caboose_LOD3"
			};
			private static string[] flatCarLODMap = new string[] {
				"car_flatcar_lod/flatcar",
				"car_flatcar_lod/car_flatcar_LOD1",
				"car_flatcar_lod/car_flatcar_LOD2",
				"car_flatcar_lod/car_flatcar_LOD3"};
			private static string[] boxCarLODMap = new string[] {
				"car_boxcar_lod/car_boxcar",
				"car_boxcar_lod/car_boxcar_LOD1",
				"car_boxcar_lod/car_boxcar_LOD2",
				"car_boxcar_lod/car_boxcar_LOD3"
			};
			private static string[] boxCarLODMap_Sim = new string[] {
				"CarBoxcar/CarBoxcar_LOD0",
				"CarBoxcar/CarBoxcar_LOD1",
				"CarBoxcar/CarBoxcar_LOD2",
				"CarBoxcar/CarBoxcar_LOD3"
			};
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
#if DEBUG
						_mod.Logger.Log("[EasyTex] Getting Tank Car LODs");
#endif
						return carTankLODMap;
					case TrainCarType.CabooseRed:
#if DEBUG
						_mod.Logger.Log("[EasyTex] Getting Caboose LODs");
#endif
						return cabooseLODMap;
					// Disabled, but I'm not willing to remove it completely
					//case TrainCarType.FlatbedEmpty:
					//	return flatCarLODMap;
					case TrainCarType.BoxcarBrown:
					case TrainCarType.BoxcarGreen:
					case TrainCarType.BoxcarPink:
					case TrainCarType.BoxcarRed:
#if DEBUG
						_mod.Logger.Log("[EasyTex] Getting Box Car LODs");
#endif
						return boxCarLODMap;
					// Needs reworked
					//case TrainCarType.PassengerBlue:
					//case TrainCarType.PassengerGreen:
					//case TrainCarType.PassengerRed:
					//	return passengerLODMap;
					default:
						return null;
				}
			}

			// Return a LOD mapping based on the supplied carType
			private static string[] GetLODs_Sim(TrainCarType carType)
			{
				switch (carType)
				{
					case TrainCarType.TankBlack:
					case TrainCarType.TankBlue:
					case TrainCarType.TankChrome:
					case TrainCarType.TankOrange:
					case TrainCarType.TankWhite:
					case TrainCarType.TankYellow:
#if DEBUG
						_mod.Logger.Log("[EasyTex] Getting Tank Car LODs");
#endif
						return carTankLODMap_Sim;
					case TrainCarType.CabooseRed:
#if DEBUG
						_mod.Logger.Log("[EasyTex] Getting Caboose LODs");
#endif
						return cabooseLODMap;
					// Disabled, but I'm not willing to remove it completely
					//case TrainCarType.FlatbedEmpty:
					//	return flatCarLODMap;
					case TrainCarType.BoxcarBrown:
					case TrainCarType.BoxcarGreen:
					case TrainCarType.BoxcarPink:
					case TrainCarType.BoxcarRed:
#if DEBUG
						_mod.Logger.Log("[EasyTex] Getting Box Car LODs");
#endif
						return boxCarLODMap_Sim;
					case TrainCarType.PassengerBlue:
					case TrainCarType.PassengerGreen:
					case TrainCarType.PassengerRed:
						//return passengerLODMap;
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
					{
						patchedPrefabs[i] = (GameObject)bundle.LoadAsset(patchedPrefabList[i]);
					}
				}
#if DEBUG
				_mod.Logger.Log("[EasyTex] Loaded " + patchedPrefabs.Length + "Patches");
				foreach(GameObject go in patchedPrefabs)
					_mod.Logger.Log("[EasyTex] " + go.name);
#endif
			}

			// Return a patchec prefab based on the specified carType
			static GameObject GetPatchedPrefab(TrainCarType carType)
			{
				if (patchedPrefabs[0] == null) LoadPrefabs();
#if DEBUG
				_mod.Logger.Log("[EasyTex] Patching a " + carType.ToString());
#endif
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
				try
				{
					// Return if result is invalid
					if (__result == null)
						return;

					// Get the traincar component
					TrainCar tc = __result.GetComponent<TrainCar>();

					if ((
							tc.carType == TrainCarType.PassengerBlue ||
							tc.carType == TrainCarType.PassengerGreen ||
							tc.carType == TrainCarType.PassengerRed
						) &&
						slicedCarsInstalled)
						return;

					// Get the LOD tree for the carType, return on invalid carType
					string[] lods = GetLODs(tc.carType);
					string[] lods_sim = GetLODs_Sim(tc.carType);
					if (lods == null || lods_sim == null)
					{
#if DEBUG
						_mod.Logger.Log("[EasyTex] LODs null return");
#endif
						return;
					}

					// Patch it!
					// Go through each LOD and copy the UVs from the patched meshes
					// to the OG meshes
					//foreach (string lod, in lods)					
					for(int i = 0; i < lods.Length; i++)
					{						
						Mesh m = GetPatchedPrefab(tc.carType).transform.Find(lods[i]).GetComponent<MeshFilter>().mesh;
			
						m.RecalculateBounds();
						m.RecalculateTangents();
                        m.Optimize();
						__result.transform.Find(lods_sim[i]).GetComponent<MeshFilter>().mesh = m;						
					}
					_mod.Logger.Log("Successfully patched mesh for " + tc.name);
					// LittleLad: Nice AND easy!

				} catch(Exception e)
				{
					_mod.Logger.LogException(e);
				}
			}
		}
	}
}