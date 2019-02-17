using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Networking.Types;
using UnityEngine.Networking.Match;
using System.Collections.Generic;

public class HostGame : MonoBehaviour
{

    // Matchmaker related
    List<MatchInfoSnapshot> m_MatchList = new List<MatchInfoSnapshot>();
    bool m_MatchCreated;
    bool m_MatchJoined;
    MatchInfo m_MatchInfo;
    string m_MatchName = "NewRoom";
    NetworkMatch m_NetworkMatch;

    // Connection/communication related
    int m_HostId = -1;

    // On the server there will be multiple connections, on the client this will only contain one ID
    List<int> m_ConnectionIds = new List<int>();
    byte[] m_ReceiveBuffer;
    string m_NetworkMessage = "Hello world";
    string m_LastReceivedMessage = "";
    NetworkWriter m_Writer;
    NetworkReader m_Reader;
    bool m_ConnectionEstablished;

    const int k_ServerPort = 25000;
    const int k_MaxMessageSize = 65535;

    void Awake()
    {
        m_NetworkMatch = gameObject.AddComponent<NetworkMatch>();
    }

    void Start()
    {
        m_ReceiveBuffer = new byte[k_MaxMessageSize];
        m_Writer = new NetworkWriter();
        // While testing with multiple standalone players on one machine this will need to be enabled
        Application.runInBackground = true;
    }

    void OnApplicationQuit()
    {
        NetworkTransport.Shutdown();
    }


    public void createRoom()
    {
        //m_NetworkMatch.CreateMatch(m_MatchName, 4, true, "", "", "", 0, 0, OnMatchCreate);
        NetworkTransport.ConnectAsNetworkHost(
            m_HostId, relayIp, relayPort, networkId, Utility.GetSourceID(), nodeId, out error);
    }

    public void joinRoom()
    {
        
        //m_NetworkMatch.JoinMatch(matches[0].networkId, "", "", "", 0, 0, OnMatchJoined);
    }



    public virtual void OnMatchCreate(bool success, string extendedInfo, MatchInfo matchInfo)
    {
        Debug.Log(success);
        /*if (success)
        {
            Debug.Log("Create match succeeded");
            Utility.SetAccessTokenForNetwork(matchInfo.networkId, matchInfo.accessToken);

            m_MatchCreated = true;
            m_MatchInfo = matchInfo;

            StartServer(matchInfo.address, matchInfo.port, matchInfo.networkId,
                matchInfo.nodeId);
        }
        else
        {
            Debug.LogError("Create match failed: " + extendedInfo);
        }*/
    }

    public void OnMatchList(bool success, string extendedInfo, List<MatchInfoSnapshot> matches)
    {
        if (success && matches != null)
        {
            m_MatchList = matches;
        }
        else if (!success)
        {
            Debug.LogError("List match failed: " + extendedInfo);
        }
    }

    // When we've joined a match we connect to the server/host
    public virtual void OnMatchJoined(bool success, string extendedInfo, MatchInfo matchInfo)
    {
        //matchInfo.address = "";
        
        if (success)
        {
            Debug.Log("Join match succeeded");
            Utility.SetAccessTokenForNetwork(matchInfo.networkId, matchInfo.accessToken);

            m_MatchJoined = true;
            m_MatchInfo = matchInfo;

            Debug.Log("Connecting to Address:" + matchInfo.address +
                      " Port:" + matchInfo.port +
                      " NetworKID: " + matchInfo.networkId +
                      " NodeID: " + matchInfo.nodeId);
            ConnectThroughRelay(matchInfo.address, matchInfo.port, matchInfo.networkId,
                matchInfo.nodeId);
        }
        else
        {
            Debug.LogError("Join match failed: " + extendedInfo);
        }
    }

    void SetupHost(bool isServer)
    {
        Debug.Log("Initializing network transport");
        NetworkTransport.Init();
        var config = new ConnectionConfig();
        config.AddChannel(QosType.Reliable);
        config.AddChannel(QosType.Unreliable);
        var topology = new HostTopology(config, 4);
        if (isServer)
            m_HostId = NetworkTransport.AddHost(topology, k_ServerPort);
        else
            m_HostId = NetworkTransport.AddHost(topology);
    }

    void StartServer(string relayIp, int relayPort, NetworkID networkId, NodeID nodeId)
    {
        SetupHost(true);

        byte error;
        NetworkTransport.ConnectAsNetworkHost(
            m_HostId, relayIp, relayPort, networkId, Utility.GetSourceID(), nodeId, out error);
    }

    void ConnectThroughRelay(string relayIp, int relayPort, NetworkID networkId, NodeID nodeId)
    {
        SetupHost(false);

        byte error;
        NetworkTransport.ConnectToNetworkPeer(
            m_HostId, relayIp, relayPort, 0, 0, networkId, Utility.GetSourceID(), nodeId, out error);
    }

    void Update()
    {
        if (m_HostId == -1)
            return;

        var networkEvent = NetworkEventType.Nothing;
        int connectionId;
        int channelId;
        int receivedSize;
        byte error;

        // Get events from the relay connection
        networkEvent = NetworkTransport.ReceiveRelayEventFromHost(m_HostId, out error);
        if (networkEvent == NetworkEventType.ConnectEvent)
            Debug.Log("Relay server connected");
        if (networkEvent == NetworkEventType.DisconnectEvent)
            Debug.Log("Relay server disconnected");

        do
        {
            // Get events from the server/client game connection
            networkEvent = NetworkTransport.ReceiveFromHost(m_HostId, out connectionId, out channelId,
                m_ReceiveBuffer, (int) m_ReceiveBuffer.Length, out receivedSize, out error);
            if ((NetworkError) error != NetworkError.Ok)
            {
                Debug.LogError("Error while receiveing network message: " + (NetworkError) error);
            }

            switch (networkEvent)
            {
                case NetworkEventType.ConnectEvent:
                {
                    Debug.Log("Connected through relay, ConnectionID:" + connectionId +
                              " ChannelID:" + channelId);
                    m_ConnectionEstablished = true;
                    m_ConnectionIds.Add(connectionId);
                    break;
                }
                case NetworkEventType.DataEvent:
                {
                    Debug.Log("Data event, ConnectionID:" + connectionId +
                              " ChannelID: " + channelId +
                              " Received Size: " + receivedSize);
                    m_Reader = new NetworkReader(m_ReceiveBuffer);
                    m_LastReceivedMessage = m_Reader.ReadString();
                    break;
                }
                case NetworkEventType.DisconnectEvent:
                {
                    Debug.Log("Connection disconnected, ConnectionID:" + connectionId);
                    break;
                }
                case NetworkEventType.Nothing:
                    break;
            }
        } while (networkEvent != NetworkEventType.Nothing);
    }
}