// Author Vojtech Bruza, Jonas Rosecky

using System;
using System.Collections.Generic;

namespace EDIVE.MirrorNetworking.ServerCodes
{
    /// <summary>
    /// server/register
    /// </summary>
    [Serializable]
    public struct ServerRegisterRequest
    {
        /// <summary>
        /// Server ip address
        /// </summary>
        public string address;
        /// <summary>
        /// Server port
        /// </summary>
        public ushort port;
        /// <summary>
        /// The app version
        /// </summary>
        public string version;
        /// <summary>
        /// The server type (to determine compatibility).
        /// Seach server build could support only specific server builds.
        /// </summary>
        public string flavour;
        /// <summary>
        /// The code that will be used to access this server.
        /// Can be null or empty -- the server manager will then have to assign one.
        /// </summary>
        public string code;
        /// <summary>
        /// The code of the organization that this server belongs to.
        /// If null or empty, the default public organization will be used.
        /// </summary>
        public string org;
        /// <summary>
        /// The startup time of the server. Can be used to determine the uptime.
        /// </summary>
        public string time;
    }

    /// <summary>
    /// server/register
    /// </summary>
    [Serializable]
    public struct ServerRegisterResponse
    {
        /// <summary>
        /// 0 means no error
        /// </summary>
        public int status;
        /// <summary>
        /// what happened human readable
        /// </summary>
        public string message;

        public Data data;
        /// <summary>
        /// Contains the response data
        /// </summary>
        [Serializable]
        public struct Data
        {
            /// <summary>
            /// The secret that can be used for further communication.
            /// Needs to be used to refresh the server.
            /// </summary>
            public string secret;
            /// <summary>
            /// The assigned code.
            /// Will be the same if not null or empty before.
            /// </summary>
            public string code;
        }
    }

    /// <summary>
    /// server/refresh
    /// </summary>
    [Serializable]
    public struct ServerRefreshRequest
    {
        /// <summary>
        /// The secret that was assigned when registering the server.
        /// </summary>
        public string secret;
        /// <summary>
        /// Server human-readable title.
        /// </summary>
        public string title;
        /// <summary>
        /// Any other data...can be used for example for the online player count.
        /// </summary>
        public object extra;
    }

    /// <summary>
    /// server/refresh
    /// </summary>
    [Serializable]
    public struct ServerRefreshResponse
    {
        /// <summary>
        /// 0 means no error
        /// </summary>
        public int status;
        /// <summary>
        /// what happened human readable
        /// </summary>
        public string message;
    }

    /// <summary>
    /// server/dispose
    /// </summary>
    [Serializable]
    public struct ServerDisposeRequest
    {
        /// <summary>
        /// Which server to dispose.
        /// Must be the same as the one assigned at reqistration.
        /// </summary>
        public string secret;
    }

    /// <summary>
    /// server/dispose
    /// </summary>
    [Serializable]
    public struct ServerDisposeResponse
    {
        /// <summary>
        /// 0 means no error
        /// </summary>
        public int status;
        /// <summary>
        /// what happened human readable
        /// </summary>
        public string message;
    }

    /// <summary>
    /// query/server
    /// </summary>
    [Serializable]
    public struct QueryServerRequest
    {
        /// <summary>
        /// Which server to get ip for.
        /// Each server is represented by its string code.
        /// Should be numbers and a few letters.
        /// </summary>
        public string code;
        /// <summary>
        /// Which organization does the server belong to.
        /// </summary>
        public string org;
    }

    /// <summary>
    /// query/server
    /// </summary>
    [Serializable]
    public struct QueryServerResponse
    {
        /// <summary>
        /// 0 means no error
        /// </summary>
        public int status;
        /// <summary>
        /// what happened human readable
        /// </summary>
        public string message;

        public Data data;
        /// <summary>
        /// Contains the response data.
        /// Same as the data from the reqister and refresh server methods.
        /// </summary>
        [Serializable]
        public struct Data
        {
            /// <summary>
            /// Server ip address
            /// </summary>
            public string address;
            /// <summary>
            /// Server port
            /// </summary>
            public ushort port;
            /// <summary>
            /// The app version
            /// </summary>
            public string version;
            /// <summary>
            /// The server type (to determine compatibility).
            /// Seach server build could support only specific server builds.
            /// </summary>
            public string flavour;
            /// <summary>
            /// The code that will be used to access this server.
            /// Can be null or empty -- the server manager will then have to assign one.
            /// </summary>
            public string code;
            /// <summary>
            /// The startup time of the server. Can be used to determine the uptime.
            /// </summary>
            public string time;
            /// <summary>
            /// Server human-readable title.
            /// </summary>
            public string title;
            /// <summary>
            /// Any other data...can be used for example for the online player count.
            /// Just make sure that the serialization is same as deserialization.
            /// </summary>
            public object extra;

            public override bool Equals(object obj)
            {
                if (!(obj is Data))
                {
                    return false;
                }

                var data = (Data)obj;
                return address == data.address &&
                       port == data.port;
            }

            public override int GetHashCode()
            {
                var hashCode = 271335409;
                hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(address);
                hashCode = hashCode * -1521134295 + port.GetHashCode();
                return hashCode;
            }

            public override string ToString()
            {
                return $"{title} ({flavour}_{version}), ip {address}:{port}, started {time}";
            }
        }
    }

    /// <summary>
    /// query/org
    /// </summary>
    [Serializable]
    public struct QueryOrgRequest
    {
        /// <summary>
        /// The client code of the organization. Can be left empty for default public organization.
        /// Different from the server code. Can be displayed in the server administration.
        /// </summary>
        public string org;
    }

    /// <summary>
    /// query/org
    /// </summary>
    [Serializable]
    public struct QueryOrgResponse
    {
        /// <summary>
        /// 0 means no error
        /// </summary>
        public int status;
        /// <summary>
        /// what happened human readable
        /// </summary>
        public string message;

        public Data data;
        /// <summary>
        /// Contains the response data.
        /// </summary>
        [Serializable]
        public struct Data
        {
            /// <summary>
            /// The human readable name of the organization.
            /// </summary>
            public string title;
            /// <summary>
            /// The number of servers in that organization.
            /// </summary>
            public int servers;
            /// <summary>
            /// The administrator of the organization.
            /// </summary>
            public string contact;
        }
    }
}
