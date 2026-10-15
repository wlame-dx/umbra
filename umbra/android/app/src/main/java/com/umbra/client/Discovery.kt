package com.umbra.client

import org.json.JSONObject
import java.net.DatagramPacket
import java.net.DatagramSocket
import java.net.InetAddress

/** Yerel agda bulunan bir Umbra bilgisayari. */
data class UmbraHost(
    val name: String,
    val address: String,
    val port: Int,
    val os: String
)

/**
 * Yerel ag kesfi. Umbra 8778 numarali UDP portunda kesif paketlerine yanit verir.
 * Yalnizca yayini acik bilgisayarlar yanit verir.
 */
object Discovery {

    const val DISCOVERY_PORT = 8778

    fun scan(timeoutMs: Int = 2500, onFound: (UmbraHost) -> Unit): List<UmbraHost> {
        val found = LinkedHashMap<String, UmbraHost>()
        var socket: DatagramSocket? = null
        try {
            socket = DatagramSocket()
            socket.broadcast = true
            socket.soTimeout = 400

            val payload = "{\"umbra\":\"discover\",\"v\":1}".toByteArray(Charsets.UTF_8)
            val target = InetAddress.getByName("255.255.255.255")
            socket.send(DatagramPacket(payload, payload.size, target, DISCOVERY_PORT))

            val deadline = System.currentTimeMillis() + timeoutMs
            val buffer = ByteArray(2048)
            while (System.currentTimeMillis() < deadline) {
                try {
                    val packet = DatagramPacket(buffer, buffer.size)
                    socket.receive(packet)
                    val text = String(packet.data, 0, packet.length, Charsets.UTF_8)
                    val hostAddress = packet.address?.hostAddress ?: continue
                    val host = parse(text, hostAddress) ?: continue
                    val key = host.address + ":" + host.port
                    if (!found.containsKey(key)) {
                        found[key] = host
                        onFound(host)
                    }
                } catch (e: java.net.SocketTimeoutException) {
                    // zaman asimi: donguye devam
                }
            }
        } catch (e: Exception) {
            // kesif basarisiz: bos liste doner
        } finally {
            try { socket?.close() } catch (e: Exception) { }
        }
        return found.values.toList()
    }

    private fun parse(text: String, address: String): UmbraHost? {
        return try {
            val json = JSONObject(text)
            if (json.optString("umbra") != "host") return null
            UmbraHost(
                name = json.optString("name", "Umbra"),
                address = address,
                port = json.optInt("port", 8777),
                os = json.optString("os", "")
            )
        } catch (e: Exception) {
            null
        }
    }
}
