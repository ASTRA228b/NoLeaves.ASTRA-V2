using BepInEx;
using System.Collections;
using System.Net;
using UnityEngine;

namespace NoLeaves.ASTRA
{
    [BepInPlugin("ASTRA.MODS.NoLeaves", "No Leaves", "5.0.6")]
    public class NoLeaves : BaseUnityPlugin
    {
        private const string URL = "https://raw.githubusercontent.com/ASTRA228b/No-Leaves.ASTRA-OBJNAME/main/OBJECTNSME.txt";
        private List<string> ObjNames = new List<string>();

        private void Start()
        {
            StartCoroutine(StartDelayed());
        }

        private IEnumerator StartDelayed()
        {
            yield return new WaitForSeconds(2f);
            StartCoroutine(LoadFromURL());
        }

        private IEnumerator LoadFromURL()
        {
            string data = "";
            try
            {
                using (WebClient CLI = new())
                {
                    data = CLI.DownloadString(URL);
                }
            }
            catch (Exception e)
            {
                Logger.LogInfo("[NoLeaves]: Failed To Load URL ->" + e.Message);
                yield break;
            }

            if (!string.IsNullOrWhiteSpace(data))
            {
                string[] strings = data.Split(new[] { '\n', '\r' }, System.StringSplitOptions.RemoveEmptyEntries);
                ObjNames = new List<string>();
                for (int i = 0; i < strings.Length; i++)
                {
                    ObjNames.Add(strings[i].Trim());
                }
                StartCoroutine(DisableObjects());
            }
        }

        private IEnumerator DisableObjects()
        {
            var wait = new WaitForSeconds(5f);
            while (true)
            {
                if (ObjNames != null && ObjNames.Count > 0)
                {
                    GameObject[] all = FindObjectsByType<GameObject>(FindObjectsSortMode.None);
                    foreach (var obj in all)
                    {
                        if (!obj.activeSelf) continue;

                        foreach (var n in ObjNames)
                        {
                            if (string.IsNullOrWhiteSpace(n)) continue;
                            if (obj.name == n)
                            {
                                obj.SetActive(false);
                                break;
                            }
                        }
                    }
                }
                yield return wait;
            }
            
        }
    }
}
