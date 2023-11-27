using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace MosquittoChat
{
    public static class TopicTypes
    {
        // The topic prefixes are used to group communication topics and avoid interference with other application using the network
        public const string GeneralTopicPrefix = "MosquittoChat";
        public const string MessageTopicPrefix = "Messaging";
        public const string ConfigTopicPrefix  = "Config";

        // Specific configuration topics
        /// <summary>
        /// Is used by the connection window to check that publishing is posssible. No response required.
        /// </summary>
        public const string ConnectionCheck = "ConnectionCheck";
        /// <summary> <para>
        /// Is used by the connection window to verify that username is unique. The message must be the chosen username.
        /// If another client has the same username, it will respond. If no response is received within chosen timelimit,
        /// treat username as valid. 
        /// </para> <para>
        /// A client with an identical username responding to this topic should publish to 
        ///     {TopicTypes.UsernameUniquenessCheck}/{username}.
        /// that is, append the username to the GenerateConfigTopic parameter string.
        /// </para> </summary>
        public const string UsernameUniquenessCheck = "UsernameUniquenessCheck";
        /// <summary>
        /// Is used when a client connects to a new topic. The message must be the name of the topic. 
        /// Response will arrive on the TopicTypes.ConnectionSyncRequest topic.
        /// </summary>
        public const string ConnectionSyncRequest = "ConnectionSyncRequest";
        /// <summary>
        /// Response topic for ConnectionSyncRequest. The message will be a serialized json string of the class "Topic".
        /// </summary>
        public const string ConnectionSyncResponse = "ConnectionSyncResponse";
        /// <summary>
        /// Is used when a client disconnects. The message should be the username of the disconnecting client.
        /// </summary>
        public const string ClientDisconnect = "ClientDisconnect";

        // The topic that all users join on connection:
        public const string DefaultMessagingTopic = "General";

        /// <summary>
        /// Generates a full topic string using the general and message prefixes and a specific topic
        /// </summary>
        public static string GenerateMessageTopic(string topic)
        {
            return $"{GeneralTopicPrefix}/{MessageTopicPrefix}/{topic}";
        }

        /// <summary>
        /// Generates a topic string using General and Config prefixes with no specific topic.
        /// Should only be used to subcribe, not to publish, since it is assumed that published messages have a specific topic.
        /// </summary>
        public static string GenerateConfigTopic()
        {
            return $"{GeneralTopicPrefix}/{ConfigTopicPrefix}";
        }

        /// <summary>
        /// Generates a topic string using General and Config prefixes and a specific topic.
        /// </summary>
        public static string GenerateConfigTopic(string topic)
        {
            return $"{GeneralTopicPrefix}/{ConfigTopicPrefix}/{topic}";
        }
    }

    /// <summary>
    /// The topic is used to store information about a given topic.
    /// </summary>
    public class Topic
    {
        public Topic(string topic) { this.topic = topic; }

        public string topic;
        public List<string> users = new();
        public List<string> messages = new();

        public string SerializeJSON(Topic msg)
        {
            return JsonSerializer.Serialize(msg);
        }

        public Topic DeserializeJSON(string json)
        {
            return JsonSerializer.Deserialize<Topic>(json)!;
        }
    }
}
