using RoR2;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace FathomlessVoidling.Controllers
{
    public class MazeSpawnPointController : NetworkBehaviour
    {
        private static MazeSpawnPointController _instance;
        public static MazeSpawnPointController instance => MazeSpawnPointController._instance;
        List<Vector3> mazePositions = new List<Vector3>()
          {
             // Top Left, Top Right
            new Vector3(-258.2f, 0f, 25.5f), new Vector3(83.8f, 0f, 223f), // new Vector3(83.8f, 21.1f, 223f) 21.3f, 19, 20
            // Bottom Left, Bottom Right
            new Vector3(-201f, 0f, -195f), new Vector3(205f, 0f, -101f),
            // Bottom Up Left, Top Down Left
            new Vector3(-141.3f, 0f, -261.7f), new Vector3(-232.2f, 0f, 93.4f),
            // Bottom Up Right, Top Down Right
            new Vector3(204.3f, 0f, -130.1f), new Vector3(73f,  0f, 217.3f)
          };

        private void Start()
        {
            //  if (!NetworkServer.active)
            //     return;
            for (int i = 0; i < mazePositions.Count; i++)
            {
                float angle = 0f;
                switch (i)
                {
                    case 0:
                        angle = 70f;
                        break;
                    case 1:
                        angle = 230f;
                        break;
                    case 2:
                        angle = 70f;
                        break;
                    case 3:
                        angle = 260f;
                        break;
                    case 4:
                        angle = 5f;
                        break;
                    case 5:
                        angle = 150f;
                        break;
                    case 6:
                        angle = -30f;
                        break;
                    case 7:
                        angle = 165f;
                        break;
                }
                string anchorName = "MazeAnchor" + i;
                GameObject mazeLaserAnchor = new GameObject(anchorName);
                mazeLaserAnchor.transform.parent = this.transform;
                mazeLaserAnchor.transform.localPosition = Vector3.zero;
                mazeLaserAnchor.transform.position = mazePositions[i];
                mazeLaserAnchor.transform.eulerAngles = new Vector3(0f, angle, 0f);
                /*
                mazeLaserAnchor.AddComponent<NetworkIdentity>();
                if (NetworkServer.active)
                    NetworkServer.Spawn(mazeLaserAnchor);
                    */
            }
            // sceneinfopos 82.2 32.6007 -215.1
        }

        private void OnEnable()
        {
            if ((bool)MazeSpawnPointController._instance)
                return;
            MazeSpawnPointController._instance = this;
            InstanceTracker.Add<MazeSpawnPointController>(this);
        }
        private void OnDisable()
        {
            if (!(MazeSpawnPointController._instance == this))
                return;
            MazeSpawnPointController._instance = null;
            InstanceTracker.Remove<MazeSpawnPointController>(this);
        }

    }
}