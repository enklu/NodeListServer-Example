﻿// This file is part of the NodeListServer Example package.

using System.Collections;
using UnityEngine;

namespace NodeListServer
{
    public class NodeListServerAdapter : MonoBehaviour
    {
        [Header("API Configuration")]
        [Tooltip("The URL to connect to the NodeListServer. For example, http://127.0.0.1:8889. DO NOT include any endpoint.")]
        [SerializeField] private string ServerAddress = "http://127.0.0.1:8889";
        [SerializeField] private string CommunicationKey = "NodeListServerDefaultKey";

        [Header("Controls")]
        [Tooltip("If set to yes, this script will be set to not be destroyed on loading a new scene.")]
        [SerializeField] private bool DontKillMe = false;

        [Tooltip("If a registration attempt fails (ie. bad network conditions), should it get a retry as an update?")]
        [SerializeField] private bool RetryRegistrationAsUpdateOnFail = true;

        [Tooltip("Should we auto-update the List Server periodically?")]
        [SerializeField] private bool UpdateServerPeriodically = false;
        [Tooltip("Value in minutes. How often the server should phone home to update it's status.")]
        [SerializeField] private int UpdateServerPeriod = 5;

        [Header("Server Information")]
        public ServerInfo CurrentServerInfo = new ServerInfo()
        {
            Name = "Untitled Server",
            Port = 7777,
            PlayerCount = 0,
            PlayerCapacity = 0
        };

        [Header("For experts only")]
        [Tooltip("Don't touch this unless you really need specific UUIDs for your servers.")]
        // The server's GUID. Should be set in the editor via OnValidate.
        [SerializeField] private string autoGeneratedUuid = "";

        // Don't touch these unless you've modified your Node List Server instance endpoints.
        private string addServerEndpoint = "/add";
        private string updateServerEndpoint = "/update";
        private string removeServerEndpoint = "/remove";

        private bool hasRegistered = false;
        private bool isBusy = false;

        private void Awake()
        {
            if (DontKillMe)
            {
                DontDestroyOnLoad(this);
            }
        }

        /// <summary>
        /// Public function to call the register server routine of the Adapter.
        /// </summary>
        public void RegisterServer()
        {
            if (isBusy)
            {
                Debug.LogWarning("Trying to register the server while we're already busy...");
                return;
            }

            StartCoroutine(nameof(RegisterServerInternal));
        }

        private IEnumerator RegisterServerInternal()
        {
            bool hasFailed = false;
            WWWForm registerServerRequest = new WWWForm();

            isBusy = true;

            // Assign all the fields required.
            registerServerRequest.AddField("serverKey", CommunicationKey);

            registerServerRequest.AddField("serverUuid", autoGeneratedUuid);
            registerServerRequest.AddField("serverName", CurrentServerInfo.Name);
            registerServerRequest.AddField("serverPort", CurrentServerInfo.Port);
            registerServerRequest.AddField("serverPlayers", CurrentServerInfo.PlayerCount);
            registerServerRequest.AddField("serverCapacity", CurrentServerInfo.PlayerCapacity);
            registerServerRequest.AddField("serverExtras", CurrentServerInfo.ExtraInformation);

            using (UnityEngine.Networking.UnityWebRequest www = UnityEngine.Networking.UnityWebRequest.Post($"{ServerAddress}{addServerEndpoint}", registerServerRequest))
            {
                yield return www.SendWebRequest();

                if (www.responseCode == 200)
                {
                    print("Successfully registered server with NodeListServer instance!");
                    hasRegistered = true;
                }
                else
                {
                    Debug.LogError("Mission failed. We'll get them next time.\n" +
                        "An error occurred while registering the server. One or more required fields, like the server UUID, " +
                        "name and port might be missing. You will need to fix this and call RegisterServer again to retry.");
                    Debug.LogError(www.error);
                    hasFailed = true;
                }
            }

            isBusy = false;

            // It may have failed registration because the server came back online
            // (ie. bad connection). Try an update if desired.
            if (hasFailed && RetryRegistrationAsUpdateOnFail)
            {
                print("But it's not over yet. Get ready for the next round: retrying as an update as specified.");
                yield return UpdateServerInternal(true);
            }

            if (UpdateServerPeriodically)
            {
                // Schedule it to be invoked automatically.
                InvokeRepeating(nameof(UpdateServer), Time.realtimeSinceStartup + (UpdateServerPeriod * 60), UpdateServerPeriod * 60);
            }

            yield break;
        }

        /// <summary>
        /// Public function to call the server removal routine of the Adapter.
        /// </summary>
        public void DeregisterServer()
        {
            if (isBusy)
            {
                Debug.LogWarning("Trying to deregister the server while we're already busy...");
                return;
            }

            StartCoroutine(nameof(DeregisterServerInternal));
        }

        private IEnumerator DeregisterServerInternal()
        {
            WWWForm deregisterServerRequest = new WWWForm();
            isBusy = true;

            // Assign all the fields required.
            deregisterServerRequest.AddField("serverKey", CommunicationKey);
            deregisterServerRequest.AddField("serverUuid", autoGeneratedUuid);

            using (UnityEngine.Networking.UnityWebRequest www = UnityEngine.Networking.UnityWebRequest.Post($"{ServerAddress}{removeServerEndpoint}", deregisterServerRequest))
            {
                yield return www.SendWebRequest();

                if (www.responseCode == 200)
                {
                    print("Successfully Deregistered Server!");

                    // Clear out any remaining invoke calls.
                    CancelInvoke();
                }
                else
                {
                    Debug.LogError("Mission failed. We'll get them next time.\n" + 
                        "An error occurred while deregistering the server. Please check the Communication Key and the Server UUID. " +
                        "Do note that there is a chance that this server instance did not update before the configured NodeListServer " +
                        "deadline, therefore the Server UUID is invalid.");
                }
            }

            isBusy = false;
            yield break;
        }

        /// <summary>
        /// Public function to call the update routine of the Adapter.
        /// </summary>
        public void UpdateServer()
        {
            if (isBusy)
            {
                Debug.LogWarning("Trying to update the server while we're already busy...");
                return;
            }

            StartCoroutine(nameof(UpdateServerInternal), false);
        }


        private IEnumerator UpdateServerInternal(bool overrideRegisteredCheck = false)
        {
            if (!overrideRegisteredCheck && !hasRegistered) yield break;

            WWWForm updateServerRequest = new WWWForm();
            isBusy = true;

            // Assign all the fields required.
            updateServerRequest.AddField("serverKey", CommunicationKey);
            
            // Can't update the IP address or port while updating the server - might
            // be implemented at a later date

            updateServerRequest.AddField("serverUuid", autoGeneratedUuid);
            updateServerRequest.AddField("serverName", CurrentServerInfo.Name);

            updateServerRequest.AddField("serverPlayers", CurrentServerInfo.PlayerCount);
            updateServerRequest.AddField("serverCapacity", CurrentServerInfo.PlayerCapacity);
            updateServerRequest.AddField("serverExtras", CurrentServerInfo.ExtraInformation);

            using (UnityEngine.Networking.UnityWebRequest www = UnityEngine.Networking.UnityWebRequest.Post($"{ServerAddress}{updateServerEndpoint}", updateServerRequest))
            {
                yield return www.SendWebRequest();

                if (www.responseCode == 200)
                {
                    print("Successfully updated server information!");
                    if(overrideRegisteredCheck) hasRegistered = true;
                }
                else
                {
                    Debug.LogError("Mission failed. We'll get them next time.\n" +
                        "An error occurred while updating the server information. The communication key or the server UUID might be wrong, or some" +
                        " other information is bogus. Or it could be you are experiencing connection problems.");
                }
            }

            isBusy = false;
            yield break;
        }

        [System.Serializable]
        public struct ServerInfo
        {
            public string Name;         // The name of the server.
            public int Port;             // The port of the server.
            public int PlayerCount;     // The count of players currently on the server.
            public int PlayerCapacity;  // The count of players allowed on the server.
            public string ExtraInformation; // Some extra information, probably best in JSON format for easy parsing.
        }

        private void OnDisable()
        {
            CancelInvoke();
        }

        // Editor only functions
#if UNITY_EDITOR
        private void OnValidate()
        {
            // Make sure our GUID is valid.
            if (string.IsNullOrEmpty(autoGeneratedUuid))
            {
                // Generate a new one.
                System.Guid randomId = System.Guid.NewGuid();
                autoGeneratedUuid = randomId.ToString();

                print($"Automatically assigned a UUID to the Server Adapter: {autoGeneratedUuid}. Good to go!");
            }
        }
#endif
    }

}

