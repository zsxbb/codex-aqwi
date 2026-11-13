/**
 * P2P Voice Chat - TCP & UDP Relay Signaling Server (v1.6)
 * Run this on your Ubuntu VPS (Contabo).
 * Usage: node signaling.js
 */

const net = require('net');
const dgram = require('dgram');

const tcpClients = new Set();
const udpEndpoints = new Map(); // username (lowercase) -> { address, port, lastSeen }

// --- TCP Server for Room Discovery & Presence ---
const tcpServer = net.createServer((socket) => {
    socket.setEncoding('utf8');
    let buffer = '';

    tcpClients.add(socket);
    console.log(`[TCP Connect] ${socket.remoteAddress}:${socket.remotePort}`);

    socket.on('data', (data) => {
        buffer += data;
        let boundary = buffer.indexOf('\n');
        while (boundary !== -1) {
            const line = buffer.substring(0, boundary).trim();
            buffer = buffer.substring(boundary + 1);
            if (line) {
                try {
                    const msg = JSON.parse(line);
                    handleTcpMessage(socket, msg);
                } catch (e) {
                    console.error('Failed to parse JSON:', e.message);
                }
            }
            boundary = buffer.indexOf('\n');
        }
    });

    socket.on('close', () => {
        tcpClients.delete(socket);
        console.log(`[TCP Disconnect] ${socket.remoteAddress}:${socket.remotePort}`);
        if (socket.username) {
            const usernameLower = socket.username.toLowerCase();
            udpEndpoints.delete(usernameLower);
            console.log(`[Offline] User ${socket.username} left`);
        }
    });

    socket.on('error', (err) => {
        console.log(`[TCP Error] ${socket.username || 'unknown'}: ${err.message}`);
    });
});

function handleTcpMessage(socket, msg) {
    if (msg.action === 'update') {
        socket.username = msg.username;
        socket.map = msg.map;
        socket.cell = msg.cell;

        console.log(`[TCP Update] ${socket.username} -> Map: ${socket.map}, Cell: ${socket.cell}`);
    }
}

// --- UDP Server for Audio Relay ---
const udpServer = dgram.createSocket('udp4');

udpServer.on('message', (msg, rinfo) => {
    // Expected Packet format: [NameLen (1 byte)][Name][AudioData / KeepAlive]
    if (msg.length < 2) return;

    const nameLen = msg[0];
    if (msg.length < 1 + nameLen) return;

    const username = msg.toString('utf8', 1, 1 + nameLen);
    const usernameLower = username.toLowerCase();

    // Register/update the UDP endpoint for this player
    const existing = udpEndpoints.get(usernameLower);
    if (!existing || existing.address !== rinfo.address || existing.port !== rinfo.port) {
        console.log(`[UDP Bind] ${username} -> ${rinfo.address}:${rinfo.port}`);
        udpEndpoints.set(usernameLower, {
            address: rinfo.address,
            port: rinfo.port,
            lastSeen: Date.now()
        });
    } else {
        existing.lastSeen = Date.now();
    }

    // Find the sender's current TCP metadata to get map & cell
    let senderTcp = null;
    for (const client of tcpClients) {
        if (client.username && client.username.toLowerCase() === usernameLower) {
            senderTcp = client;
            break;
        }
    }

    if (!senderTcp) {
        return;
    }

    if (!senderTcp.map || !senderTcp.cell) {
        return;
    }

    // Relay packet to other players in the same map & cell
    for (const client of tcpClients) {
        if (client.username && client.username.toLowerCase() !== usernameLower && client.map === senderTcp.map && client.cell === senderTcp.cell) {
            const peerUdp = udpEndpoints.get(client.username.toLowerCase());
            if (peerUdp) {
                // Forward the exact same packet to the peer
                udpServer.send(msg, 0, msg.length, peerUdp.port, peerUdp.address, (err) => {
                    if (err) {
                        console.error(`[UDP Send Error] to ${client.username}:`, err.message);
                    }
                });
            }
        }
    }
});

udpServer.on('listening', () => {
    const address = udpServer.address();
    console.log(`UDP Server listening on ${address.address}:${address.port}`);
});

udpServer.on('error', (err) => {
    console.error(`[UDP Server Error]: ${err.message}`);
});

const PORT = 3000;

// Listen TCP
tcpServer.listen(PORT, '0.0.0.0', () => {
    console.log(`===================================================`);
    console.log(`  TCP Signaling Server running on port ${PORT}`);
    console.log(`===================================================`);
});

// Listen UDP
udpServer.bind(PORT, '0.0.0.0');
