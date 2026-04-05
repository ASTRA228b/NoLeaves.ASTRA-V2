using BepInEx;
using System.Collections;
using System.Net;
using UnityEngine;


namespace NoLeaves.ASTRA
{

    [BepInPlugin("ASTRA.MODS.NoLeaves", "No Leaves", "5.0.5")]
    public class NoLeaves : BaseUnityPlugin
    {
        private const string URL = "https://raw.githubusercontent.com/ASTRA228b/No-Leaves.ASTRA-OBJNAME/main/OBJECTNSME.txt";
        private List<string> ObjNames = new List<string>();

        private void Start()
        {
            StartCoroutine(LoadFromURL());
        }

        private IEnumerator LoadFromURL()
        {
            using (WebClient CLI = new WebClient())
            {
                string url = CLI.DownloadString(URL);
                if (!string.IsNullOrEmpty(url))
                {
                    ObjNames = new List<string>(url.Split('\n'));
                    StartCoroutine(DisableObjects());
                }

            }
            yield return null;
        }


        private IEnumerator DisableObjects()
        {

            while (true)
            {
                foreach (string raw in ObjNames)
                {
                    string name = raw.Trim();
                    if (string.IsNullOrEmpty(name)) continue;

                    GameObject obj = GameObject.Find(name);
                    if (obj != null)
                    {
                        obj.SetActive(false);
                    }
                }
                yield return new WaitForSeconds(5f);
            }

        }
    }
}
