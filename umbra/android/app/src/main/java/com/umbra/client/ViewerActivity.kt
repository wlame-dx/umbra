package com.umbra.client

import android.annotation.SuppressLint
import android.os.Build
import android.os.Bundle
import android.webkit.WebView
import android.webkit.WebViewClient
import androidx.appcompat.app.AppCompatActivity
import java.net.URLEncoder

class ViewerActivity : AppCompatActivity() {

    private lateinit var web: WebView

    @SuppressLint("SetJavaScriptEnabled")
    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        setContentView(R.layout.activity_viewer)

        val address = intent.getStringExtra("address")
        val port = intent.getIntExtra("port", 8777)
        if (address == null) {
            finish()
            return
        }

        web = findViewById(R.id.web)
        web.settings.javaScriptEnabled = true
        web.settings.domStorageEnabled = true
        web.settings.mediaPlaybackRequiresUserGesture = false
        web.webViewClient = WebViewClient()

        val model = Build.MODEL ?: "Android"
        val os = "Android " + Build.VERSION.RELEASE
        val url = "http://" + address + ":" + port + "/" +
                "?dname=" + enc(model) +
                "&dmodel=" + enc(model) +
                "&dplatform=" + enc(os) +
                "&dos=" + enc(os)
        web.loadUrl(url)
    }

    private fun enc(value: String): String {
        return try { URLEncoder.encode(value, "UTF-8") } catch (e: Exception) { value }
    }

    @Suppress("DEPRECATION")
    override fun onBackPressed() {
        if (this::web.isInitialized && web.canGoBack()) web.goBack() else super.onBackPressed()
    }
}
