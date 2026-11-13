using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;
using Concentus;
using Concentus.Enums;
using Concentus.Structs;
using HarmonyLib;
using Pixelplacement;

public class VoiceChatManager : MonoBehaviour
{
    public static VoiceChatManager Instance { get; private set; }

    [Header("Settings")]
    public bool voiceEnabled = false;
    public bool isMuted = false;
    public bool isDeafened = false;
    public bool isLoopback = false;

    private OpusDecoder loopbackDecoder;
    private VoicePlayer loopbackPlayer;
    private GameObject loopbackGO;

    [Header("Network Status")]
    public int localPort;
    public IPEndPoint publicEP;
    public string connectionStatus = "Disconnected";

    private UdpClient udpClient;
    private bool isRunning = false;
    private Thread receiveThread;

    // Opus Encoder for local mic
    private OpusEncoder encoder;
    private int sampleRate = 48000;
    private int frameSize = 960; // 20ms at 48kHz
    private string micDevice = null;
    private AudioClip micClip;
    private int lastMicPos = 0;
    private float[] micTempBuf;

    // Active Remote Peers
    private Dictionary<string, RemotePeer> activePeers = new Dictionary<string, RemotePeer>();
    private object peersLock = new object();

    // Thread-safe signaling queue for Main Thread execution
    private string pendingSignalMessage = null;
    private object signalLock = new object();

    // Track map name to auto-disconnect/re-connect
    private string lastMapName = "";
    private float keepAliveTimer = 0f;
    private string myPublicIP = "";

    // New configuration & watchdog properties
    public float voiceVolume = 1.0f;
    public int selectedMicIndex = 0;
    public float micLevel = 0f;
    private float networkCheckTimer = 0f;
    private float stunRetryTimer = 0f;
    private string lastLocalIP = "";
    private string lastCellName = "";
    private float presenceBroadcastTimer = 0f;

    // TCP Signaling client
    public bool useExternalSignaling = true;
    public string signalingServerIP = Config.SignalingServerIP;
    public int signalingServerPort = Config.SignalingServerPort;
    private IPEndPoint signalingServerEP;
    
    private TcpClient tcpClient;
    private NetworkStream tcpStream;
    private Thread tcpThread;
    private bool isTcpRunning = false;
    private readonly object tcpLock = new object();

    // Debug variables
    public bool isTcpConnected = false;
    public string lastTcpError = "None";
    public string lastUdpError = "None";
    public int udpPacketsSent = 0;
    public int udpPacketsReceived = 0;
    public bool showVoiceDebug = false;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            micTempBuf = new float[frameSize];
            lastLocalIP = GetLocalIPAddress();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        // Force external signaling server to VPS
        useExternalSignaling = true;
        signalingServerIP = "46.250.228.209";
        signalingServerPort = 3000;

        // Subscribe to map changing event and network change notifications
        try
        {
            Area.changingMaps += OnMapChanging;
            System.Net.NetworkInformation.NetworkChange.NetworkAddressChanged += OnNetworkAddressChanged;
        }
        catch (Exception ex)
        {
            Debug.LogError("[CodexVoice] Failed to subscribe to events: " + ex.Message);
        }
    }

    void OnDestroy()
    {
        try
        {
            Area.changingMaps -= OnMapChanging;
            System.Net.NetworkInformation.NetworkChange.NetworkAddressChanged -= OnNetworkAddressChanged;
        }
        catch {}
        StopVoice();
        StopTcpSignaling();
    }

    private void OnNetworkAddressChanged(object sender, EventArgs e)
    {
        Debug.Log("[CodexVoice] Network address change detected! Re-initializing voice...");
        Loom.QueueOnMainThread(() =>
        {
            if (voiceEnabled)
            {
                StartVoice();
            }
        });
    }

    public void ToggleVoice(bool enable)
    {
        if (voiceEnabled == enable) return;
        voiceEnabled = enable;

        if (voiceEnabled)
        {
            StartVoice();
        }
        else
        {
            StopVoice();
        }
    }

    private void StartVoice()
    {
        if (isRunning) StopVoice();

        connectionStatus = "Resolving STUN...";
        isRunning = true;
        
        lastLocalIP = GetLocalIPAddress();
        lastCellName = Entity.mainPlayer != null ? Entity.mainPlayer.Frame : "";

        ThreadPool.QueueUserWorkItem(_ =>
        {
            try
            {
                if (useExternalSignaling)
                {
                    try
                    {
                        IPAddress[] ips = Dns.GetHostAddresses(signalingServerIP);
                        IPAddress targetIP = IPAddress.Loopback;
                        foreach (var ip in ips)
                        {
                            if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                            {
                                targetIP = ip;
                                break;
                            }
                        }
                        signalingServerEP = new IPEndPoint(targetIP, signalingServerPort);
                    }
                    catch (Exception dnsEx)
                    {
                        Debug.LogError("[CodexVoice] Failed to resolve signaling server DNS: " + dnsEx.Message);
                        signalingServerEP = new IPEndPoint(IPAddress.Parse("46.250.228.209"), signalingServerPort);
                    }
                }

                // 1. Initialize UDP Client on random free port
                udpClient = new UdpClient(0);
                localPort = ((IPEndPoint)udpClient.Client.LocalEndPoint).Port;

                // 2. Query Google STUN server for public IP:Port
                publicEP = GetPublicIPWithSocket(udpClient);
                if (publicEP == null)
                {
                    connectionStatus = "STUN Failed (using local)";
                    publicEP = new IPEndPoint(IPAddress.Loopback, localPort);
                    myPublicIP = "127.0.0.1";
                }
                else
                {
                    connectionStatus = "Ready";
                    myPublicIP = publicEP.Address.ToString();
                }

                // 3. Initialize Opus Encoder
                encoder = new OpusEncoder(sampleRate, 1, OpusApplication.OPUS_APPLICATION_VOIP);
                loopbackDecoder = new OpusDecoder(sampleRate, 1);

                // 4. Start Receiver Thread
                receiveThread = new Thread(ReceiveLoop);
                receiveThread.IsBackground = true;
                receiveThread.Start();

                // 5. Trigger microphone recording on Main Thread
                Loom.QueueOnMainThread(() =>
                {
                    loopbackGO = new GameObject("VoiceLoopback");
                    loopbackPlayer = loopbackGO.AddComponent<VoicePlayer>();
                    var audioSrc = loopbackPlayer.GetComponent<AudioSource>();
                    if (audioSrc != null)
                    {
                        audioSrc.volume = voiceVolume;
                    }
                    StartMicrophone();
                    BroadcastPresence(true);
                    if (useExternalSignaling)
                    {
                        StartTcpSignaling();
                    }
                });
            }
            catch (Exception ex)
            {
                connectionStatus = "Error: " + ex.Message;
                Debug.LogError("[CodexVoice] Start failed: " + ex.Message);
                StopVoice();
            }
        });
    }

    private void StopVoice()
    {
        if (!isRunning) return;
        isRunning = false;

        connectionStatus = "Disconnected";

        // Broadcast disconnect to peers
        BroadcastPresence(false);
        StopTcpSignaling();

        // Stop Microphone
        StopMicrophone();

        // Close Socket
        if (udpClient != null)
        {
            try { udpClient.Close(); } catch {}
            udpClient = null;
        }

        // Wait for thread
        if (receiveThread != null)
        {
            receiveThread = null;
        }

        // Clean up Peers
        lock (peersLock)
        {
            foreach (var peer in activePeers.Values)
            {
                peer.Dispose();
            }
            activePeers.Clear();
        }

        encoder = null;
        loopbackDecoder = null;
        Loom.QueueOnMainThread(() =>
        {
            if (loopbackGO != null)
            {
                Destroy(loopbackGO);
                loopbackGO = null;
                loopbackPlayer = null;
            }
        });
    }

    public void ToggleExternalSignaling(bool enable)
    {
        if (useExternalSignaling == enable) return;
        useExternalSignaling = enable;
        PlayerPrefs.SetInt("CodexVoice_UseExtSignaling", enable ? 1 : 0);
        PlayerPrefs.Save();

        if (voiceEnabled && isRunning)
        {
            StartVoice();
        }
    }

    private void StartTcpSignaling()
    {
        StopTcpSignaling();

        isTcpRunning = true;
        tcpThread = new Thread(TcpReceiveLoop);
        tcpThread.IsBackground = true;
        tcpThread.Start();
    }

    private void StopTcpSignaling()
    {
        isTcpRunning = false;

        lock (tcpLock)
        {
            if (tcpStream != null)
            {
                try { tcpStream.Close(); } catch {}
                tcpStream = null;
            }
            if (tcpClient != null)
            {
                try { tcpClient.Close(); } catch {}
                tcpClient = null;
            }
        }

        if (tcpThread != null)
        {
            tcpThread = null;
        }
    }

    private void SendTcpMessage(string message)
    {
        lock (tcpLock)
        {
            if (tcpStream == null || !isTcpRunning) return;
            try
            {
                byte[] bytes = Encoding.UTF8.GetBytes(message + "\n");
                tcpStream.Write(bytes, 0, bytes.Length);
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[CodexVoice] Failed to send TCP message: " + ex.Message);
            }
        }
    }

    private void TcpReceiveLoop()
    {
        while (isTcpRunning)
        {
            try
            {
                lock (tcpLock)
                {
                    tcpClient = new TcpClient();
                }

                Debug.Log($"[CodexVoice] Connecting to signaling server at {signalingServerIP}:{signalingServerPort}...");
                
                var result = tcpClient.BeginConnect(signalingServerIP, signalingServerPort, null, null);
                var success = result.AsyncWaitHandle.WaitOne(TimeSpan.FromSeconds(5));
                if (!success)
                {
                    throw new SocketException((int)SocketError.TimedOut);
                }
                tcpClient.EndConnect(result);

                lock (tcpLock)
                {
                    tcpStream = tcpClient.GetStream();
                }

                isTcpConnected = true;
                lastTcpError = "None";
                Debug.Log("[CodexVoice] Connected to signaling server successfully.");

                SendTcpPresenceUpdate();

                byte[] buffer = new byte[4096];
                StringBuilder sb = new StringBuilder();

                while (isTcpRunning)
                {
                    int readBytes = tcpStream.Read(buffer, 0, buffer.Length);
                    if (readBytes <= 0) break; // Disconnected

                    string chunk = Encoding.UTF8.GetString(buffer, 0, readBytes);
                    sb.Append(chunk);

                    string content = sb.ToString();
                    int newlineIdx = content.IndexOf('\n');
                    while (newlineIdx >= 0)
                    {
                        string line = content.Substring(0, newlineIdx).Trim();
                        content = content.Substring(newlineIdx + 1);
                        sb.Clear();
                        sb.Append(content);

                        if (!string.IsNullOrEmpty(line))
                        {
                            try
                            {
                                ProcessTcpMessage(line);
                            }
                            catch (Exception ex)
                            {
                                Debug.LogError("[CodexVoice] Error processing TCP message: " + ex.Message);
                            }
                        }

                        newlineIdx = content.IndexOf('\n');
                    }
                }
            }
            catch (Exception ex)
            {
                isTcpConnected = false;
                lastTcpError = ex.Message;
                Debug.LogWarning($"[CodexVoice] Signaling connection error: {ex.Message}. Retrying in 5s...");
            }

            isTcpConnected = false;
            lock (tcpLock)
            {
                if (tcpStream != null) { try { tcpStream.Close(); } catch {} tcpStream = null; }
                if (tcpClient != null) { try { tcpClient.Close(); } catch {} tcpClient = null; }
            }

            for (int i = 0; i < 50 && isTcpRunning; i++)
            {
                Thread.Sleep(100);
            }
        }
    }

    private void ProcessTcpMessage(string line)
    {
        if (line.Contains("\"action\":\"online\""))
        {
            string username = ExtractJsonValue(line, "username");
            string pubIP = ExtractJsonValue(line, "publicIP");
            int pubPort = int.Parse(ExtractJsonValue(line, "publicPort"));
            string locIP = ExtractJsonValue(line, "localIP");
            int locPort = int.Parse(ExtractJsonValue(line, "localPort"));

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(pubIP)) return;

            if (Entity.mainPlayer != null && username.Equals(Entity.mainPlayer.Name, StringComparison.OrdinalIgnoreCase)) return;

            IPEndPoint peerEP;
            if (pubIP == myPublicIP && myPublicIP != "127.0.0.1" && myPublicIP != "")
            {
                peerEP = new IPEndPoint(IPAddress.Parse(locIP), locPort);
            }
            else
            {
                peerEP = new IPEndPoint(IPAddress.Parse(pubIP), pubPort);
            }

            Loom.QueueOnMainThread(() =>
            {
                bool isNew = false;
                lock (peersLock)
                {
                    isNew = !activePeers.ContainsKey(username);
                }

                AddOrUpdatePeer(username, peerEP);

                if (isNew && Singleton<UIChat>.Instance != null)
                {
                    Singleton<UIChat>.Instance.SetText($"<color=yellow>[Codex] Voice connection established: {username}</color>");
                }
            });
        }
        else if (line.Contains("\"action\":\"offline\""))
        {
            string username = ExtractJsonValue(line, "username");
            if (string.IsNullOrEmpty(username)) return;

            Loom.QueueOnMainThread(() =>
            {
                lock (peersLock)
                {
                    if (activePeers.TryGetValue(username, out var peer))
                    {
                        peer.Dispose();
                        activePeers.Remove(username);
                        Debug.Log("[CodexVoice] Peer disconnected via server: " + username);

                        if (Singleton<UIChat>.Instance != null)
                        {
                            Singleton<UIChat>.Instance.SetText($"<color=yellow>[Codex] Voice disconnected: {username}</color>");
                        }
                    }
                }
            });
        }
    }

    private string ExtractJsonValue(string json, string key)
    {
        string pattern = $"\"{key}\":\"";
        int startIdx = json.IndexOf(pattern);
        if (startIdx >= 0)
        {
            startIdx += pattern.Length;
            int endIdx = json.IndexOf("\"", startIdx);
            if (endIdx >= 0)
            {
                return json.Substring(startIdx, endIdx - startIdx);
            }
        }
        else
        {
            pattern = $"\"{key}\":";
            startIdx = json.IndexOf(pattern);
            if (startIdx >= 0)
            {
                startIdx += pattern.Length;
                int endIdx = json.IndexOf(",");
                if (endIdx < 0) endIdx = json.IndexOf("}", startIdx);
                if (endIdx >= 0)
                {
                    return json.Substring(startIdx, endIdx - startIdx).Trim();
                }
            }
        }
        return "";
    }

    private void SendTcpPresenceUpdate()
    {
        if (Entity.mainPlayer == null || publicEP == null || !isTcpRunning) return;

        string localIP = GetLocalIPAddress();
        string map = Area.currentArea != null ? Area.currentArea.Name : "";
        string cell = Entity.mainPlayer.Frame;

        string msg = "{" +
                     $"\"action\":\"update\"," +
                     $"\"username\":\"{Entity.mainPlayer.Name}\"," +
                     $"\"map\":\"{map}\"," +
                     $"\"cell\":\"{cell}\"," +
                     $"\"publicIP\":\"{publicEP.Address}\"," +
                     $"\"publicPort\":{publicEP.Port}," +
                     $"\"localIP\":\"{localIP}\"," +
                     $"\"localPort\":{localPort}" +
                     "}";

        SendTcpMessage(msg);
    }

    private void StartMicrophone()
    {
        try
        {
            if (Microphone.devices.Length > 0)
            {
                if (selectedMicIndex < 0 || selectedMicIndex >= Microphone.devices.Length)
                {
                    selectedMicIndex = 0;
                }
                micDevice = Microphone.devices[selectedMicIndex];
            }
            else
            {
                micDevice = null;
            }
            micClip = Microphone.Start(micDevice, true, 10, sampleRate);
            lastMicPos = 0;
            Debug.Log("[CodexVoice] Microphone started: " + (micDevice ?? "Default"));
        }
        catch (Exception ex)
        {
            Debug.LogError("[CodexVoice] Failed to start microphone: " + ex.Message);
        }
    }

    private void StopMicrophone()
    {
        try
        {
            if (Microphone.IsRecording(micDevice))
            {
                Microphone.End(micDevice);
            }
            micClip = null;
        }
        catch {}
    }

    void Update()
    {
        // 1. Process queued signaling message on Main Thread
        lock (signalLock)
        {
            if (pendingSignalMessage != null)
            {
                if (Singleton<AEC>.Instance != null && Entity.mainPlayer != null)
                {
                    Singleton<AEC>.Instance.sendRequest(new RequestChat(pendingSignalMessage, "zone"));
                }
                pendingSignalMessage = null;
            }
        }

        // Decay VU meter
        if (micLevel > 0f)
        {
            micLevel -= Time.deltaTime * 2.0f;
            if (micLevel < 0f) micLevel = 0f;
        }

        if (!voiceEnabled || !isRunning) return;

        // Watchdog: Check if network (local IP) changed
        networkCheckTimer += Time.deltaTime;
        if (networkCheckTimer >= 5.0f)
        {
            networkCheckTimer = 0f;
            string currentLocalIP = GetLocalIPAddress();
            if (currentLocalIP != lastLocalIP && currentLocalIP != "127.0.0.1")
            {
                Debug.Log($"[CodexVoice] Local IP changed from {lastLocalIP} -> {currentLocalIP}. Re-initializing voice...");
                lastLocalIP = currentLocalIP;
                StartVoice();
                return;
            }
        }

        // Watchdog: Retry STUN query if it failed or errored out
        if (connectionStatus.Contains("Failed") || connectionStatus.Contains("Error"))
        {
            stunRetryTimer += Time.deltaTime;
            if (stunRetryTimer >= 10.0f)
            {
                stunRetryTimer = 0f;
                Debug.Log("[CodexVoice] Retrying voice initialization due to previous STUN failure...");
                StartVoice();
                return;
            }
        }
        else
        {
            stunRetryTimer = 0f;
        }

        // Watchdog: Cell change detection
        if (Entity.mainPlayer != null && Entity.mainPlayer.Frame != lastCellName)
        {
            string oldCell = lastCellName;
            lastCellName = Entity.mainPlayer.Frame;
            Debug.Log($"[CodexVoice] Cell changed from '{oldCell}' to '{lastCellName}'. Re-syncing voice...");
            lock (peersLock)
            {
                foreach (var peer in activePeers.Values)
                {
                    peer.Dispose();
                }
                activePeers.Clear();
            }
            BroadcastPresence(true);
        }

        // Self-healing: Periodic presence broadcast
        presenceBroadcastTimer += Time.deltaTime;
        if (presenceBroadcastTimer >= 15.0f)
        {
            presenceBroadcastTimer = 0f;
            BroadcastPresence(true);
        }

        // Send Keep-Alive packets to keep NAT holes open
        keepAliveTimer += Time.deltaTime;
        if (keepAliveTimer >= 1.0f)
        {
            keepAliveTimer = 0f;
            SendKeepAliveToPeers();
        }

        // 2. Check for Map changes to auto-broadcast ping
        if (Area.currentArea != null && Area.currentArea.Name != lastMapName)
        {
            lastMapName = Area.currentArea.Name;
            // Map changed! Disconnect old peers and notify new ones
            lock (peersLock)
            {
                foreach (var peer in activePeers.Values)
                {
                    peer.Dispose();
                }
                activePeers.Clear();
            }
            BroadcastPresence(true);
        }

        // 3. Process mic input and send voice data
        if (micClip != null && !isMuted && !isDeafened)
        {
            int curPos = Microphone.GetPosition(micDevice);
            if (curPos < lastMicPos) lastMicPos = 0;

            int samplesAvailable = curPos - lastMicPos;
            while (samplesAvailable >= frameSize)
            {
                micClip.GetData(micTempBuf, lastMicPos);

                // Calculate peak level for VU meter
                float peak = 0f;
                for (int i = 0; i < frameSize; i++)
                {
                    float val = Mathf.Abs(micTempBuf[i]);
                    if (val > peak) peak = val;
                }
                micLevel = peak;

                // Convert float to short
                short[] pcmData = new short[frameSize];
                for (int i = 0; i < frameSize; i++)
                {
                    pcmData[i] = (short)(micTempBuf[i] * 32767f);
                }

                // Encode sample chunk using Opus
                byte[] compBuf = new byte[1280];
                int encLen = encoder.Encode(pcmData, 0, frameSize, compBuf, 0, compBuf.Length);

                if (encLen > 0)
                {
                    byte[] packet = new byte[encLen];
                    Array.Copy(compBuf, packet, encLen);

                    // Send packet to all active peers
                    SendAudioToPeers(packet);

                    if (isLoopback && loopbackDecoder != null && loopbackPlayer != null)
                    {
                        try
                        {
                            short[] outBuf = new short[frameSize];
                            int decodedSamples = loopbackDecoder.Decode(packet, 0, packet.Length, outBuf, 0, frameSize, false);
                            if (decodedSamples > 0)
                            {
                                float[] floatBuf = new float[decodedSamples];
                                for (int i = 0; i < decodedSamples; i++)
                                {
                                    floatBuf[i] = outBuf[i] / 32767f;
                                }
                                loopbackPlayer.FeedSamples(floatBuf);
                            }
                        }
                        catch {}
                    }
                }

                lastMicPos += frameSize;
                samplesAvailable = curPos - lastMicPos;
            }
        }
        else
        {
            micLevel = 0f;
        }

        // 4. Validate peers (remove peers that are no longer in the map room or same cell)
        if (Area.currentArea != null)
        {
            List<string> peersToRemove = new List<string>();
            lock (peersLock)
            {
                foreach (var peer in activePeers)
                {
                    bool stillInArea = false;
                    foreach (var entity in Area.currentArea.allEntities)
                    {
                        if (entity != null && entity.isPlayer && entity.Name.Equals(peer.Key, StringComparison.OrdinalIgnoreCase))
                        {
                            Player p = entity as Player;
                            if (p != null && Entity.mainPlayer != null && p.Frame == Entity.mainPlayer.Frame)
                            {
                                stillInArea = true;
                            }
                            break;
                        }
                    }

                    if (!stillInArea)
                    {
                        peersToRemove.Add(peer.Key);
                    }
                }

                foreach (var name in peersToRemove)
                {
                    if (activePeers.TryGetValue(name, out var peer))
                    {
                        peer.Dispose();
                        activePeers.Remove(name);
                        Debug.Log("[CodexVoice] Peer left map/cell: " + name);
                    }
                }
            }
        }
    }

    private void SendAudioToPeers(byte[] data)
    {
        if (useExternalSignaling)
        {
            if (Entity.mainPlayer == null || signalingServerEP == null) return;
            try
            {
                byte[] nameBytes = Encoding.UTF8.GetBytes(Entity.mainPlayer.Name);
                byte[] packet = new byte[1 + nameBytes.Length + data.Length];
                packet[0] = (byte)nameBytes.Length;
                Array.Copy(nameBytes, 0, packet, 1, nameBytes.Length);
                Array.Copy(data, 0, packet, 1 + nameBytes.Length, data.Length);

                udpClient.Send(packet, packet.Length, signalingServerEP);
                udpPacketsSent++;
            }
            catch (Exception ex)
            {
                lastUdpError = "SendAudio: " + ex.Message;
            }
            return;
        }

        lock (peersLock)
        {
            if (activePeers.Count == 0) return;

            foreach (var peer in activePeers.Values)
            {
                if (peer.endpoint != null)
                {
                    try
                    {
                        udpClient.Send(data, data.Length, peer.endpoint);
                        udpPacketsSent++;
                    }
                    catch (Exception ex)
                    {
                        lastUdpError = "SendAudioP2P: " + ex.Message;
                    }
                }
            }
        }
    }

    private void SendKeepAliveToPeers()
    {
        if (useExternalSignaling)
        {
            if (Entity.mainPlayer == null || signalingServerEP == null) return;
            try
            {
                byte[] nameBytes = Encoding.UTF8.GetBytes(Entity.mainPlayer.Name);
                byte[] packet = new byte[1 + nameBytes.Length + 1];
                packet[0] = (byte)nameBytes.Length;
                Array.Copy(nameBytes, 0, packet, 1, nameBytes.Length);
                packet[packet.Length - 1] = 0; // 1-byte keep-alive payload

                udpClient.Send(packet, packet.Length, signalingServerEP);
                udpPacketsSent++;
            }
            catch (Exception ex)
            {
                lastUdpError = "SendKeepAlive: " + ex.Message;
            }
            return;
        }

        lock (peersLock)
        {
            if (activePeers.Count == 0) return;
            byte[] keepAliveData = new byte[] { 0 };
            foreach (var peer in activePeers.Values)
            {
                if (peer.endpoint != null)
                {
                    try
                    {
                        udpClient.Send(keepAliveData, keepAliveData.Length, peer.endpoint);
                        udpPacketsSent++;
                    }
                    catch (Exception ex)
                    {
                        lastUdpError = "SendKeepAliveP2P: " + ex.Message;
                    }
                }
            }
        }
    }

    private void ReceiveLoop()
    {
        IPEndPoint remoteEP = new IPEndPoint(IPAddress.Any, 0);
        while (isRunning)
        {
            try
            {
                byte[] packet = udpClient.Receive(ref remoteEP);
                udpPacketsReceived++;
                if (packet == null || packet.Length == 0) continue;
                if (isDeafened) continue;

                if (useExternalSignaling)
                {
                    // Check if it matches the signaling server IP address
                    if (signalingServerEP != null && remoteEP.Address.Equals(signalingServerEP.Address))
                    {
                        // Parse format: [NameLen (1 byte)][Name][Payload]
                        if (packet.Length < 2) continue;
                        int nameLen = packet[0];
                        if (packet.Length < 1 + nameLen) continue;

                        string senderName = Encoding.UTF8.GetString(packet, 1, nameLen);
                        int payloadLen = packet.Length - 1 - nameLen;
                        
                        // Ignore keepalives
                        if (payloadLen <= 1) continue;

                        byte[] audioData = new byte[payloadLen];
                        Array.Copy(packet, 1 + nameLen, audioData, 0, payloadLen);

                        lock (peersLock)
                        {
                            if (!activePeers.TryGetValue(senderName, out var peer))
                            {
                                IPEndPoint dummyEP = new IPEndPoint(IPAddress.Loopback, 0);
                                peer = new RemotePeer(senderName, dummyEP, sampleRate, frameSize);
                                activePeers[senderName] = peer;
                                Debug.Log("[CodexVoice] Automatically mapped remote peer via VPS UDP: " + senderName);
                            }
                            peer.FeedAudio(audioData);
                        }
                    }
                }
                else
                {
                    // Deliver audio packet to the matching peer based on IP/Port or IP-only for WAN (Symmetric NAT)
                    lock (peersLock)
                    {
                        foreach (var peer in activePeers.Values)
                        {
                            // 1. Direct match by IP & Port (works for LAN/Loopback/Full Cone)
                            if (peer.endpoint.Equals(remoteEP))
                            {
                                if (packet.Length > 4)
                                {
                                    peer.FeedAudio(packet);
                                }
                                break;
                            }
                            // 2. Fallback match by IP only for public WAN peers (handles Symmetric NAT port shifting)
                            else if (!IsPrivateIP(remoteEP.Address) && peer.endpoint.Address.Equals(remoteEP.Address))
                            {
                                Debug.Log($"[CodexVoice] Symmetric NAT detected for {peer.username}. Updating P2P port from {peer.endpoint.Port} to {remoteEP.Port}");
                                peer.endpoint = remoteEP;
                                if (packet.Length > 4)
                                {
                                    peer.FeedAudio(packet);
                                }
                                break;
                            }
                        }
                    }
                }
            }
            catch (SocketException)
            {
                // Handle close/abort socket cleanly
            }
            catch (Exception ex)
            {
                lastUdpError = "Receive: " + ex.Message;
                Debug.LogError("[CodexVoice] Receive error: " + ex.Message);
            }
        }
    }

    // --- Signaling Engine (Chat-based Handshake) ---

    public void BroadcastPresence(bool online)
    {
        if (useExternalSignaling)
        {
            if (online)
            {
                SendTcpPresenceUpdate();
            }
            else
            {
                StopTcpSignaling();
            }
            return;
        }

        if (Entity.mainPlayer == null || publicEP == null) return;

        string msg;
        if (online)
        {
            string localIP = GetLocalIPAddress();
            string rawData = $"{publicEP.Address}:{publicEP.Port}|{localIP}:{localPort}";
            string encryptedData = EncryptData(rawData);
            msg = $"<size=0><alpha=#00>[CodexVoicePing:{encryptedData}]</alpha></size>";
        }
        else
        {
            msg = "<size=0><alpha=#00>[CodexVoiceDisconnect]</alpha></size>";
        }

        lock (signalLock)
        {
            pendingSignalMessage = msg;
        }
    }

    private void SendReply(string targetUser)
    {
        if (useExternalSignaling) return;

        if (Entity.mainPlayer == null || publicEP == null) return;
        
        string localIP = GetLocalIPAddress();
        string rawData = $"{publicEP.Address}:{publicEP.Port}|{localIP}:{localPort}";
        string encryptedData = EncryptData(rawData);
        string msg = $"<size=0><alpha=#00>[CodexVoiceReply:{encryptedData}]</alpha></size>";
        
        Loom.QueueOnMainThread(() =>
        {
            if (Singleton<AEC>.Instance != null)
            {
                // Whisper to sender
                Singleton<AEC>.Instance.sendRequest(new RequestChat(new System.Collections.Generic.List<string> { msg, "whisper", targetUser }));
            }
        });
    }

    public bool HandleIncomingChat(ResponseChat rc)
    {
        if (rc == null || string.IsNullOrEmpty(rc.msg) || string.IsNullOrEmpty(rc.Name)) return false;

        bool isVoiceSignal = rc.msg.Contains("[CodexVoicePing:") || 
                             rc.msg.Contains("[CodexVoiceReply:") || 
                             rc.msg.Contains("[CodexVoiceDisconnect]");

        if (!isVoiceSignal) return false;

        if (useExternalSignaling)
        {
            return true;
        }

        // Skip our own broadcasts from processing, but still consume them to hide from UI
        if (Entity.mainPlayer != null && rc.Name.Equals(Entity.mainPlayer.Name, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        // 1. Peer is pinging the entire room
        if (rc.msg.Contains("[CodexVoicePing:"))
        {
            if (!voiceEnabled || !isRunning) return true; // Consume it but don't respond

            try
            {
                // Verify they are in the same cell as us
                Player peerPlayer = Entity.getPlayer(rc.Name);
                if (peerPlayer == null || Entity.mainPlayer == null || peerPlayer.Frame != Entity.mainPlayer.Frame)
                {
                    return true; // Discard ping if not in same cell
                }

                // Extract and decrypt payload
                int startIndex = rc.msg.IndexOf("[CodexVoicePing:");
                int endIndex = rc.msg.IndexOf("]", startIndex);
                string encryptedData = rc.msg.Substring(startIndex, endIndex - startIndex).Replace("[CodexVoicePing:", "");
                
                string data = DecryptData(encryptedData);
                if (string.IsNullOrEmpty(data)) return true;
                
                string[] parts = data.Split('|');
                if (parts.Length == 2)
                {
                    string[] pubParts = parts[0].Split(':');
                    string[] locParts = parts[1].Split(':');

                    if (pubParts.Length == 2 && locParts.Length == 2)
                    {
                        string pubIP = pubParts[0];
                        int pubPort = int.Parse(pubParts[1]);
                        string locIP = locParts[0];
                        int locPort = int.Parse(locParts[1]);

                        IPEndPoint peerEP;
                        // ICE Logic: If same public IP, we are on the same LAN or same PC!
                        if (pubIP == myPublicIP && myPublicIP != "127.0.0.1" && myPublicIP != "")
                        {
                            peerEP = new IPEndPoint(IPAddress.Parse(locIP), locPort);
                            Debug.Log($"[CodexVoice] Same LAN detected for {rc.Name}. Using local endpoint: {peerEP}");
                        }
                        else
                        {
                            peerEP = new IPEndPoint(IPAddress.Parse(pubIP), pubPort);
                            Debug.Log($"[CodexVoice] Different networks detected for {rc.Name}. Using public endpoint: {peerEP}");
                        }

                        bool isNew = false;
                        lock (peersLock)
                        {
                            isNew = !activePeers.ContainsKey(rc.Name);
                        }

                        AddOrUpdatePeer(rc.Name, peerEP);
                        
                        if (isNew && Singleton<UIChat>.Instance != null)
                        {
                            Singleton<UIChat>.Instance.SetText($"<color=yellow>[Codex] Voice connection established: {rc.Name}</color>");
                        }

                        // Reply back to let them know we are also active
                        SendReply(rc.Name);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError("[CodexVoice] Failed to parse VoicePing: " + ex.Message);
            }

            return true; // Block this message from showing in the user's Chat Box
        }

        // 2. Peer is replying to our ping
        if (rc.msg.Contains("[CodexVoiceReply:"))
        {
            if (!voiceEnabled || !isRunning) return true;

            try
            {
                // Verify they are in the same cell as us
                Player peerPlayer = Entity.getPlayer(rc.Name);
                if (peerPlayer == null || Entity.mainPlayer == null || peerPlayer.Frame != Entity.mainPlayer.Frame)
                {
                    return true; // Discard reply if not in same cell
                }

                // Extract and decrypt payload
                int startIndex = rc.msg.IndexOf("[CodexVoiceReply:");
                int endIndex = rc.msg.IndexOf("]", startIndex);
                string encryptedData = rc.msg.Substring(startIndex, endIndex - startIndex).Replace("[CodexVoiceReply:", "");
                
                string data = DecryptData(encryptedData);
                if (string.IsNullOrEmpty(data)) return true;
                
                string[] parts = data.Split('|');
                if (parts.Length == 2)
                {
                    string[] pubParts = parts[0].Split(':');
                    string[] locParts = parts[1].Split(':');

                    if (pubParts.Length == 2 && locParts.Length == 2)
                    {
                        string pubIP = pubParts[0];
                        int pubPort = int.Parse(pubParts[1]);
                        string locIP = locParts[0];
                        int locPort = int.Parse(locParts[1]);

                        IPEndPoint peerEP;
                        // ICE Logic: If same public IP, we are on the same LAN or same PC!
                        if (pubIP == myPublicIP && myPublicIP != "127.0.0.1" && myPublicIP != "")
                        {
                            peerEP = new IPEndPoint(IPAddress.Parse(locIP), locPort);
                            Debug.Log($"[CodexVoice] Same LAN detected for {rc.Name}. Using local endpoint: {peerEP}");
                        }
                        else
                        {
                            peerEP = new IPEndPoint(IPAddress.Parse(pubIP), pubPort);
                            Debug.Log($"[CodexVoice] Different networks detected for {rc.Name}. Using public endpoint: {peerEP}");
                        }

                        bool isNew = false;
                        lock (peersLock)
                        {
                            isNew = !activePeers.ContainsKey(rc.Name);
                        }

                        AddOrUpdatePeer(rc.Name, peerEP);

                        if (isNew && Singleton<UIChat>.Instance != null)
                        {
                            Singleton<UIChat>.Instance.SetText($"<color=yellow>[Codex] Voice connection established: {rc.Name}</color>");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError("[CodexVoice] Failed to parse VoiceReply: " + ex.Message);
            }

            return true;
        }

        // 3. Peer is disconnecting
        if (rc.msg.Contains("[CodexVoiceDisconnect]"))
        {
            lock (peersLock)
            {
                if (activePeers.TryGetValue(rc.Name, out var peer))
                {
                    peer.Dispose();
                    activePeers.Remove(rc.Name);
                    Debug.Log("[CodexVoice] Peer disconnected manually: " + rc.Name);
                    
                    if (Singleton<UIChat>.Instance != null)
                    {
                        Singleton<UIChat>.Instance.SetText($"<color=yellow>[Codex] Voice disconnected: {rc.Name}</color>");
                    }
                }
            }
            return true;
        }

        return false;
    }

    private void AddOrUpdatePeer(string username, IPEndPoint ep)
    {
        lock (peersLock)
        {
            if (activePeers.TryGetValue(username, out var peer))
            {
                peer.endpoint = ep;
            }
            else
            {
                Debug.Log($"[CodexVoice] Establishing voice connection to peer: {username} ({ep})");
                activePeers[username] = new RemotePeer(username, ep, sampleRate, frameSize);
            }
        }
    }

    private void OnMapChanging()
    {
        // When changing maps, disconnect from all current map room peers immediately
        lock (peersLock)
        {
            foreach (var peer in activePeers.Values)
            {
                peer.Dispose();
            }
            activePeers.Clear();
        }
    }

    // --- STUN Resolver ---
    private static IPEndPoint GetPublicIPWithSocket(UdpClient client)
    {
        string[] stunHosts = new string[]
        {
            "stun.l.google.com",
            "stun1.l.google.com",
            "stun.sipgate.net",
            "stun.voipbuster.com",
            "stun.xten.com"
        };

        foreach (var host in stunHosts)
        {
            try
            {
                IPAddress[] ips = Dns.GetHostAddresses(host);
                if (ips == null || ips.Length == 0) continue;
                
                IPAddress targetIP = null;
                foreach (var ip in ips)
                {
                    if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                    {
                        targetIP = ip;
                        break;
                    }
                }
                if (targetIP == null) continue;

                IPEndPoint stunServer = new IPEndPoint(targetIP, 19302);

                byte[] stunRequest = new byte[20];
                stunRequest[0] = 0x00; stunRequest[1] = 0x01; // Binding Request
                stunRequest[2] = 0x00; stunRequest[3] = 0x00; // Message Length
                stunRequest[4] = 0x21; stunRequest[5] = 0x12; stunRequest[6] = 0xA4; stunRequest[7] = 0x42; // Magic Cookie
                
                int oldReceiveTimeout = client.Client.ReceiveTimeout;
                int oldSendTimeout = client.Client.SendTimeout;
                client.Client.ReceiveTimeout = 1500;
                client.Client.SendTimeout = 1500;
                
                client.Send(stunRequest, stunRequest.Length, stunServer);
                
                IPEndPoint remoteEP = new IPEndPoint(IPAddress.Any, 0);
                byte[] stunResponse = client.Receive(ref remoteEP);
                
                client.Client.ReceiveTimeout = oldReceiveTimeout;
                client.Client.SendTimeout = oldSendTimeout;
                
                if (stunResponse != null && stunResponse.Length >= 20 && stunResponse[0] == 0x01 && stunResponse[1] == 0x01)
                {
                    int i = 20;
                    while (i < stunResponse.Length)
                    {
                        int type = (stunResponse[i] << 8) | stunResponse[i + 1];
                        int length = (stunResponse[i + 2] << 8) | stunResponse[i + 3];
                        if (type == 0x0001 || type == 0x0020) // MAPPED-ADDRESS or XOR-MAPPED-ADDRESS
                        {
                            int family = stunResponse[i + 5];
                            int port = (stunResponse[i + 6] << 8) | stunResponse[i + 7];
                            if (type == 0x0020)
                            {
                                port ^= 0x2112;
                            }
                            byte[] ipBytes = new byte[4];
                            if (type == 0x0020)
                            {
                                ipBytes[0] = (byte)(stunResponse[i + 8] ^ 0x21);
                                ipBytes[1] = (byte)(stunResponse[i + 9] ^ 0x12);
                                ipBytes[2] = (byte)(stunResponse[i + 10] ^ 0xA4);
                                ipBytes[3] = (byte)(stunResponse[i + 11] ^ 0x42);
                            }
                            else
                            {
                                ipBytes[0] = stunResponse[i + 8];
                                ipBytes[1] = stunResponse[i + 9];
                                ipBytes[2] = stunResponse[i + 10];
                                ipBytes[3] = stunResponse[i + 11];
                            }
                            return new IPEndPoint(new IPAddress(ipBytes), port);
                        }
                        i += 4 + length;
                    }
                }
            }
            catch
            {
                // Try next STUN server
            }
        }
        return null;
    }

    private string GetLocalIPAddress()
    {
        try
        {
            var host = System.Net.Dns.GetHostEntry(System.Net.Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                {
                    string ipStr = ip.ToString();
                    if (ipStr != "127.0.0.1")
                    {
                        return ipStr;
                    }
                }
            }
        }
        catch {}
        return "127.0.0.1";
    }

    private bool IsPrivateIP(IPAddress address)
    {
        if (IPAddress.IsLoopback(address)) return true;
        string ipStr = address.ToString();
        return ipStr.StartsWith("10.") || 
               ipStr.StartsWith("192.168.") || 
               ipStr.StartsWith("172.") || 
               ipStr.StartsWith("169.254.");
    }

    private string EncryptData(string plainText)
    {
        try
        {
            byte[] bytes = System.Text.Encoding.UTF8.GetBytes(plainText);
            string key = Config.VoiceEncryptionKey;
            for (int i = 0; i < bytes.Length; i++)
            {
                bytes[i] = (byte)(bytes[i] ^ key[i % key.Length]);
            }
            return Convert.ToBase64String(bytes)
                .Replace('+', '-')
                .Replace('/', '_')
                .Replace("=", "");
        }
        catch
        {
            return plainText;
        }
    }

    private string DecryptData(string cipherText)
    {
        try
        {
            string base64 = cipherText.Replace('-', '+').Replace('_', '/');
            int mod = base64.Length % 4;
            if (mod > 0)
            {
                base64 += new string('=', 4 - mod);
            }
            byte[] bytes = Convert.FromBase64String(base64);
            string key = Config.VoiceEncryptionKey;
            for (int i = 0; i < bytes.Length; i++)
            {
                bytes[i] = (byte)(bytes[i] ^ key[i % key.Length]);
            }
            return System.Text.Encoding.UTF8.GetString(bytes);
        }
        catch
        {
            return null;
        }
    }

    public void ChangeMicrophone(int newIndex)
    {
        if (Microphone.devices.Length == 0) return;
        selectedMicIndex = newIndex;
        string newDevice = Microphone.devices[newIndex];
        if (newDevice != micDevice)
        {
            Debug.Log($"[CodexVoice] Switching microphone to: {newDevice}");
            if (voiceEnabled && isRunning)
            {
                StopMicrophone();
                micDevice = newDevice;
                StartMicrophone();
            }
            else
            {
                micDevice = newDevice;
            }
        }
    }

    public void SetVoiceVolume(float volume)
    {
        voiceVolume = volume;
        if (loopbackPlayer != null)
        {
            var audioSrc = loopbackPlayer.GetComponent<AudioSource>();
            if (audioSrc != null) audioSrc.volume = volume;
        }
        lock (peersLock)
        {
            foreach (var peer in activePeers.Values)
            {
                peer.SetVolume(volume);
            }
        }
    }

    public List<string> GetActivePeerNames()
    {
        lock (peersLock)
        {
            return new List<string>(activePeers.Keys);
        }
    }
}

// Represents a Remote Player's Voice Stream
public class RemotePeer : IDisposable
{
    public string username;
    public IPEndPoint endpoint;
    
    private OpusDecoder decoder;
    private GameObject audioGO;
    private VoicePlayer voicePlayer;
    private int sampleRate;
    private int frameSize;

    public RemotePeer(string username, IPEndPoint endpoint, int sampleRate, int frameSize)
    {
        this.username = username;
        this.endpoint = endpoint;
        this.sampleRate = sampleRate;
        this.frameSize = frameSize;

        decoder = new OpusDecoder(sampleRate, 1);

        // Spawn a hidden GameObject to play this peer's audio
        Loom.QueueOnMainThread(() =>
        {
            // Find player character GameObject for 3D positional audio
            Player targetPlayer = Entity.getPlayer(username);
            GameObject parentGO = (targetPlayer != null) ? targetPlayer.getGameObject() : null;

            audioGO = new GameObject($"VoicePlayer_{username}");
            if (parentGO != null)
            {
                audioGO.transform.SetParent(parentGO.transform);
                audioGO.transform.localPosition = Vector3.zero;
            }
            
            voicePlayer = audioGO.AddComponent<VoicePlayer>();

            // Set initial volume
            var audioSrc = voicePlayer.GetComponent<AudioSource>();
            if (audioSrc != null)
            {
                audioSrc.volume = VoiceChatManager.Instance.voiceVolume;
            }
        });
    }

    public void SetVolume(float volume)
    {
        Loom.QueueOnMainThread(() =>
        {
            if (voicePlayer != null)
            {
                var audioSrc = voicePlayer.GetComponent<AudioSource>();
                if (audioSrc != null) audioSrc.volume = volume;
            }
        });
    }

    public void FeedAudio(byte[] data)
    {
        try
        {
            short[] outBuf = new short[frameSize];
            int decodedSamples = decoder.Decode(data, 0, data.Length, outBuf, 0, frameSize, false);

            if (decodedSamples > 0 && voicePlayer != null)
            {
                float[] floatBuf = new float[decodedSamples];
                for (int i = 0; i < decodedSamples; i++)
                {
                    floatBuf[i] = outBuf[i] / 32767f;
                }
                voicePlayer.FeedSamples(floatBuf);
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[CodexVoice] Failed to decode audio packet from {username}: {ex.Message}");
        }
    }

    public void Dispose()
    {
        decoder = null;
        Loom.QueueOnMainThread(() =>
        {
            if (audioGO != null)
            {
                UnityEngine.Object.Destroy(audioGO);
            }
        });
    }
}

// Plays PCM audio buffers in Unity real-time using low-latency OnAudioFilterRead
public class VoicePlayer : MonoBehaviour
{
    private AudioSource audioSource;
    private Queue<float> sampleQueue = new Queue<float>();
    private object queueLock = new object();

    void Awake()
    {
        audioSource = gameObject.GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        // Enable 3D Positional audio if attached to a character
        if (transform.parent != null)
        {
            audioSource.spatialBlend = 1.0f; // 100% 3D sound
            audioSource.minDistance = 2f;
            audioSource.maxDistance = 15f;
            audioSource.rolloffMode = AudioRolloffMode.Logarithmic;
        }
        else
        {
            audioSource.spatialBlend = 0.0f; // 2D flat sound
        }

        audioSource.loop = true;
        audioSource.playOnAwake = false;
        
        // OnAudioFilterRead requires the audio clip to be playing
        audioSource.clip = AudioClip.Create("VoiceStream", 48000, 1, 48000, false);
        audioSource.Play();
    }

    public void FeedSamples(float[] samples)
    {
        lock (queueLock)
        {
            // Limit buffer size to prevent memory leak and catch up with lag
            if (sampleQueue.Count > 48000)
            {
                sampleQueue.Clear();
            }

            foreach (var s in samples)
            {
                sampleQueue.Enqueue(s);
            }
        }
    }

    void OnAudioFilterRead(float[] data, int channels)
    {
        lock (queueLock)
        {
            for (int i = 0; i < data.Length; i += channels)
            {
                float sample = 0f;
                if (sampleQueue.Count > 0)
                {
                    sample = sampleQueue.Dequeue();
                }

                for (int c = 0; c < channels; c++)
                {
                    data[i + c] = sample;
                }
            }
        }
    }
}

// Thread Loom helper for executing actions on Unity main thread from background threads
public class Loom : MonoBehaviour
{
    private static Loom instance;
    private static readonly Queue<Action> queue = new Queue<Action>();
    private static readonly object queueLock = new object();

    public static void Initialize()
    {
        if (instance != null) return;

        var go = new GameObject("Loom");
        instance = go.AddComponent<Loom>();
        DontDestroyOnLoad(go);
    }

    public static void QueueOnMainThread(Action action)
    {
        lock (queueLock)
        {
            queue.Enqueue(action);
        }
    }

    void Update()
    {
        lock (queueLock)
        {
            while (queue.Count > 0)
            {
                queue.Dequeue()?.Invoke();
            }
        }
    }
}
