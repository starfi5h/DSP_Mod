using BepInEx;
using HarmonyLib;
using System;
using UnityEngine;
using UnityEngine.UI;
using System.Reflection;
using UnityEngine.Events;
using BepInEx.Logging;


namespace DysonOrbitModifier
{
    class SphereLogic
    {

        public static ManualLogSource logger;


        public static int CheckSwarmRadius(DysonSphere sphere, float orbitRadius)
        {
            foreach (var planet in sphere.starData.planets)
            {
                if (planet.orbitAround > 0)
                    continue;
                if (Mathf.Abs(planet.orbitRadius * 40000.0f - orbitRadius) < 2199.95f)
                    return -2;
            }
            return 0;
        }

        public static int CheckLayerRadius(DysonSphere sphere, int layerId, float orbitRadius)
        {
            foreach (var planet in sphere.starData.planets)
            {
                if (planet.orbitAround > 0)
                    continue;
                if (Mathf.Abs(planet.orbitRadius * 40000.0f - orbitRadius) < 2199.95f)
                    return -2;
            }
            for (int i = 0; i < 10; i++)
            {
                if (sphere.layersSorted[i] != null && Mathf.Abs(sphere.layersSorted[i].orbitRadius - orbitRadius) < 999.95f)
                {
                    if (sphere.layersIdBased[layerId].orbitRadius == sphere.layersSorted[i].orbitRadius)
                        continue;
                    else
                        return -1;
                }
            }
            return 0;
        }


        public static void ConvertQuaternion(Quaternion rotation, out float inclination, out float longitude)
        {
            inclination = rotation.eulerAngles.z == 0 ? 0 : 360 - rotation.eulerAngles.z;
            longitude = rotation.eulerAngles.y == 0 ? 0 : 360 - rotation.eulerAngles.y;
        }

        public static void ChangeSwarm(DysonSphere sphere, int id, float radius, Quaternion rotation)
        {
            ConvertQuaternion(sphere.swarm.orbits[id].rotation, out float inclination, out float longitude);
            logger.LogInfo($"Previous Swarm[{id}]: ({sphere.swarm.orbits[id].radius}, {inclination}, {longitude})");
            sphere.swarm.orbits[id].radius = radius;
            sphere.swarm.orbits[id].rotation = rotation;
            sphere.swarm.orbits[id].up = rotation * Vector3.up;
            ConvertQuaternion(sphere.swarm.orbits[id].rotation, out inclination, out longitude);
            logger.LogInfo($" Current Swarm[{id}]: ({sphere.swarm.orbits[id].radius}, {inclination}, {longitude})");
        }

        public static void ChangeLayer(DysonSphere sphere, int id, float radius, Quaternion rotation, float angularSpeed)
        {
            ConvertQuaternion(sphere.GetLayer(id).orbitRotation, out float inclination, out float longitude);
            logger.LogInfo($"Previous Layer[{id}]: ({sphere.GetLayer(id).orbitRadius}, {inclination}, {longitude}, {sphere.GetLayer(id).orbitAngularSpeed})");
            sphere.GetLayer(id).orbitRadius = radius;
            sphere.GetLayer(id).orbitRotation = rotation;
            sphere.GetLayer(id).orbitAngularSpeed = angularSpeed;
            ConvertQuaternion(sphere.GetLayer(id).orbitRotation, out inclination, out longitude);
            logger.LogInfo($" Current Layer[{id}]: ({sphere.GetLayer(id).orbitRadius}, {inclination}, {longitude}, {angularSpeed})");
        }










    }
}
