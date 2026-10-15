package com.umbra.client

import android.content.Intent
import android.os.Bundle
import android.os.Handler
import android.os.Looper
import android.widget.Button
import android.widget.ListView
import android.widget.TextView
import androidx.appcompat.app.AppCompatActivity

class MainActivity : AppCompatActivity() {

    private val handler = Handler(Looper.getMainLooper())
    private val hosts = ArrayList<UmbraHost>()
    private lateinit var adapter: HostAdapter
    private lateinit var status: TextView
    private var scanning = false

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        setContentView(R.layout.activity_main)

        status = findViewById(R.id.status)
        val list = findViewById<ListView>(R.id.hostList)
        adapter = HostAdapter(this, hosts)
        list.adapter = adapter
        list.setOnItemClickListener { _, _, position, _ ->
            val host = hosts[position]
            val intent = Intent(this, ViewerActivity::class.java)
            intent.putExtra("address", host.address)
            intent.putExtra("port", host.port)
            intent.putExtra("name", host.name)
            startActivity(intent)
        }

        findViewById<Button>(R.id.scanButton).setOnClickListener { scan() }
    }

    override fun onResume() {
        super.onResume()
        if (hosts.isEmpty()) scan()
    }

    private fun scan() {
        if (scanning) return
        scanning = true
        hosts.clear()
        adapter.notifyDataSetChanged()
        status.text = getString(R.string.scanning)

        Thread {
            Discovery.scan(2500) { host ->
                handler.post {
                    val key = host.address + ":" + host.port
                    if (hosts.none { (it.address + ":" + it.port) == key }) {
                        hosts.add(host)
                        adapter.notifyDataSetChanged()
                    }
                    status.text = getString(R.string.found, hosts.size)
                }
            }
            handler.post {
                scanning = false
                if (hosts.isEmpty()) status.text = getString(R.string.none_found)
            }
        }.start()
    }
}
