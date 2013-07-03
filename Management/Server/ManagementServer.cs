﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using SuperSocket.Common;
using SuperSocket.Management.Server.Config;
using SuperSocket.SocketBase;
using SuperSocket.SocketBase.Config;
using SuperSocket.SocketBase.Protocol;
using SuperSocket.WebSocket;
using SuperSocket.WebSocket.Protocol;
using SuperSocket.WebSocket.SubProtocol;
using SuperSocket.SocketBase.Metadata;

namespace SuperSocket.Management.Server
{
    /// <summary>
    /// Server manager app server
    /// </summary>
    public class ManagementServer : WebSocketServer<ManagementSession>
    {
        private Dictionary<string, UserConfig> m_UsersDict;

        private string[] m_ExcludedServers;

        private List<KeyValuePair<string, StatusInfoAttribute[]>> m_ServerStatusMetadataSource;

        /// <summary>
        /// Gets the server status metadata source.
        /// </summary>
        /// <value>
        /// The server status metadata source.
        /// </value>
        internal List<KeyValuePair<string, StatusInfoAttribute[]>> ServerStatusMetadataSource
        {
            get { return m_ServerStatusMetadataSource; }
        }

        private NodeStatus m_CurrentNodeStatus;

        /// <summary>
        /// Gets the current node status.
        /// </summary>
        /// <value>
        /// The current node status.
        /// </value>
        internal NodeStatus CurrentNodeStatus
        {
            get { return m_CurrentNodeStatus; }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ManagementServer"/> class.
        /// </summary>
        public ManagementServer()
            : base(new BasicSubProtocol<ManagementSession>("ServerManager"))
        {

        }


        /// <summary>
        /// Setups the specified root config.
        /// </summary>
        /// <param name="rootConfig">The root config.</param>
        /// <param name="config">The config.</param>
        /// <returns></returns>
        protected override bool Setup(IRootConfig rootConfig, IServerConfig config)
        {
            if (!base.Setup(rootConfig, config))
                return false;

            var users = config.GetChildConfig<UserConfigCollection>("users");

            if (users == null || users.Count <= 0)
            {
                Logger.Error("No user defined");
                return false;
            }

            m_UsersDict = new Dictionary<string, UserConfig>(StringComparer.OrdinalIgnoreCase);

            foreach (var u in users)
            {
                m_UsersDict.Add(u.Name, u);
            }

            m_ExcludedServers = config.Options.GetValue("excludedServers", string.Empty).Split(
                new char[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries);

            return true;
        }

        /// <summary>
        /// Gets the name of the server by.
        /// </summary>
        /// <param name="serverName">Name of the server.</param>
        /// <returns></returns>
        internal IWorkItem GetServerByName(string serverName)
        {
            return Bootstrap.AppServers.FirstOrDefault(s => s.Name.Equals(serverName, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Gets the name of the user by.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <returns></returns>
        internal UserConfig GetUserByName(string name)
        {
            UserConfig user;
            m_UsersDict.TryGetValue(name, out user);
            return user;
        }

        private void OnServerStatusCollected(object status)
        {
            m_CurrentNodeStatus = (NodeStatus)status;
            //Logger.Info(JsonSerialize(status));
        }

        protected override void OnSystemMessageReceived(string messageType, object messageData)
        {
            if (messageType == "ServerStatusCollected")
            {
                this.AsyncRun(OnServerStatusCollected, messageData);
            }
            else if (messageType == "ServerMetadataCollected")
            {
                //Logger.Info(JsonSerialize(messageData));
                m_ServerStatusMetadataSource = (List<KeyValuePair<string, StatusInfoAttribute[]>>)messageData;
            }
        }

        private static JsonConverter m_IPEndPointConverter = new ListenersJsonConverter();

        /// <summary>
        /// Jsons the serialize.
        /// </summary>
        /// <param name="target">The target.</param>
        /// <returns></returns>
        public override string JsonSerialize(object target)
        {
            return JsonConvert.SerializeObject(target, m_IPEndPointConverter);
        }
    }
}