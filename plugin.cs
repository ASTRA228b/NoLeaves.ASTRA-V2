using BepInEx;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;


namespace NoLeaves.ASTRA;

[BepInPlugin("ASTRA.MODS.NoLeaves", "No Leaves", "5.1.0")]
public class NoLeaves : BaseUnityPlugin
{
    private const string URL = "https://raw.githubusercontent.com/ASTRA228b/No-Leaves.ASTRA-OBJNAME/main/OBJECTNSME.txt";
    private readonly HashSet<string> KnownNames = new();
    private readonly HashSet<string> DetectedNames = new();
    private string CachePath => Path.Combine(Paths.CachePath, "NoLeaves.ASTRA.cache");
    private void Start()
    {
        StartCoroutine(StartDelayed());
    }

    private IEnumerator StartDelayed()
    {
        yield return new WaitForSeconds(2f);
        LoadCache();
        yield return StartCoroutine(LoadFromURL());
        StartCoroutine(DisableObjects());
    }


    private IEnumerator LoadFromURL()
    {
        using UnityWebRequest request = UnityWebRequest.Get(URL);
        request.timeout = 5;
        yield return request.SendWebRequest();
        if (request.result != UnityWebRequest.Result.Success)
        {
            Logger.LogInfo("Failed To Load URL -> " + request.error);

            if (KnownNames.Count > 0)
                Logger.LogInfo("Using Cached Object Names");

            yield break;
        }

        string data = request.downloadHandler.text;
        if (string.IsNullOrWhiteSpace(data))
            yield break;

        KnownNames.Clear();
        string[] strings = data.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
        foreach (string value in strings)
        {
            string name = value.Trim();

            if (!string.IsNullOrWhiteSpace(name))
                KnownNames.Add(name);
        }
        SaveCache();
        Logger.LogInfo("Loaded " + KnownNames.Count + " Object Name(s) From URL");
    }


    private IEnumerator DisableObjects()
    {
        WaitForSeconds wait = new(5f);
        while (true)
        {
            GameObject[] objects = FindObjectsByType<GameObject>(FindObjectsSortMode.None);
            int found = DisableKnownObjects(objects);
            if (found == 0)
            {
                string detected = DetectLeafName(objects);
                if (!string.IsNullOrEmpty(detected))
                {
                    if (DetectedNames.Add(detected))
                        Logger.LogInfo("Automatically Detected -> " + detected);

                    DisableDetectedObjects(objects);
                }
            }

            yield return wait;
        }
    }

    private int DisableKnownObjects(GameObject[] objects)
    {
        int found = 0;
        foreach (GameObject obj in objects)
        {
            if (obj == null || !obj.activeSelf)
                continue;

            if (!obj.scene.IsValid())
                continue;

            if (!KnownNames.Contains(obj.name) && !DetectedNames.Contains(obj.name))
                continue;

            obj.SetActive(false);
            found++;
        }

        return found;
    }

    private void DisableDetectedObjects(GameObject[] objects)
    {
        foreach (GameObject obj in objects)
        {
            if (obj == null || !obj.activeSelf)
                continue;

            if (!obj.scene.IsValid())
                continue;

            if (DetectedNames.Contains(obj.name))
                obj.SetActive(false);
        }
    }

    private string DetectLeafName(GameObject[] objects)
    {
        Dictionary<string, Candidate> candidates = new();
        foreach (GameObject obj in objects)
        {
            if (obj == null)
                continue;

            if (!obj.scene.IsValid())
                continue;

            string name = obj.name;
            if (!IsLeafTempFileName(name))
                continue;

            if (!candidates.TryGetValue(name, out Candidate candidate))
            {
                candidate = new Candidate();
                candidates.Add(name, candidate);
            }

            candidate.Count++;
            Transform parent = obj.transform.parent;
            if (parent != null)
                candidate.Parents.Add(parent);

            Transform root = GetBranchRoot(obj.transform);
            if (root != null)
                candidate.Roots.Add(root);

            MeshFilter meshFilter = obj.GetComponent<MeshFilter>();
            if (meshFilter != null && meshFilter.sharedMesh != null)
                candidate.Meshes.Add(meshFilter.sharedMesh);

            Renderer renderer = obj.GetComponent<Renderer>();
            if (renderer != null)
                candidate.RendererCount++;
        }
        string bestName = "";
        float bestScore = 0f;
        foreach (KeyValuePair<string, Candidate> pair in candidates)
        {
            Candidate candidate = pair.Value;

            if (candidate.Count < 2)
                continue;

            float score = 0f;
            score += candidate.Count * 10f;
            score += candidate.Parents.Count * 6f;
            score += candidate.Roots.Count * 12f;
            score += candidate.RendererCount * 2f;
            if (candidate.Meshes.Count == 1)
                score += 25f;

            if (KnownNames.Contains(pair.Key))
                score += 1000f;

            Logger.LogInfo(": Candidate -> " +pair.Key + " | Count: " + candidate.Count + " | Parents: " + candidate.Parents.Count + " | Roots: " + candidate.Roots.Count + " | Meshes: " + candidate.Meshes.Count + " | Score: " + score);
            if (score > bestScore)
            {
                bestScore = score;
                bestName = pair.Key;
            }
        }
        return bestName;
    }

    private bool IsLeafTempFileName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return false;

        bool tempFile = name.StartsWith("UnityTempFile-", StringComparison.OrdinalIgnoreCase);
        bool combined = name.IndexOf("combined by EdMeshCombiner", StringComparison.OrdinalIgnoreCase) >= 0;
        return tempFile && combined;
    }

    private Transform GetBranchRoot(Transform transform)
    {
        Transform current = transform;
        while (current.parent != null && current.parent.parent != null)
            current = current.parent;

        return current;
    }


    private void LoadCache()
    {
        if (!File.Exists(CachePath))
            return;

        try
        {
            string[] lines = File.ReadAllLines(CachePath);
            foreach (string value in lines)
            {
                string name = value.Trim();

                if (!string.IsNullOrWhiteSpace(name))
                    KnownNames.Add(name);
            }
            Logger.LogInfo("Loaded " + KnownNames.Count + " Cached Object Name(s)");
        }
        catch (Exception e)
        {
            Logger.LogInfo("Failed To Load Cache -> " + e.Message);
        }
    }

    private void SaveCache()
    {
        try
        {
            File.WriteAllLines(CachePath, KnownNames);
        }
        catch (Exception e)
        {
            Logger.LogInfo("Failed To Save Cache -> " + e.Message);
        }
    }

    private class Candidate
    {
        public int Count;
        public int RendererCount;
        public readonly HashSet<Transform> Parents = new();
        public readonly HashSet<Transform> Roots = new();
        public readonly HashSet<Mesh> Meshes = new();
    }
}
