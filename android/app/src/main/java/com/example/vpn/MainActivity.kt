package com.example.vpn

import android.app.Activity
import android.content.BroadcastReceiver
import android.content.Context
import android.content.Intent
import android.content.IntentFilter
import android.net.VpnManager
import android.net.VpnService
import android.os.Build
import android.os.Bundle
import androidx.appcompat.app.AppCompatActivity
import androidx.core.content.getSystemService
import androidx.localbroadcastmanager.content.LocalBroadcastManager
import com.example.vpn.databinding.ActivityMainBinding

class MainActivity : AppCompatActivity() {
    private lateinit var binding: ActivityMainBinding
    private var isConnected = false

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        binding = ActivityMainBinding.inflate(layoutInflater)
        setContentView(binding.root)

        binding.connectButton.setOnClickListener {
            if (!isConnected) {
                prepareVpn()
            } else {
                MyVpnService.stop(this)
                isConnected = false
                updateStatus("Отключено")
            }
        }

        LocalBroadcastManager.getInstance(this).registerReceiver(statusReceiver,
            IntentFilter(MyVpnService.ACTION_STATUS))
    }

    override fun onDestroy() {
        LocalBroadcastManager.getInstance(this).unregisterReceiver(statusReceiver)
        super.onDestroy()
    }

    private fun prepareVpn() {
        val intent = VpnService.prepare(this)
        if (intent != null) {
            startActivityForResult(intent, REQUEST_VPN)
        } else {
            onActivityResult(REQUEST_VPN, Activity.RESULT_OK, null)
        }
    }

    override fun onActivityResult(requestCode: Int, resultCode: Int, data: Intent?) {
        super.onActivityResult(requestCode, resultCode, data)
        if (requestCode == REQUEST_VPN) {
            if (resultCode == Activity.RESULT_OK) {
                MyVpnService.start(this)
                enableAlwaysOn()
            } else {
                updateStatus("Ошибка подключения")
            }
        }
    }

    private fun enableAlwaysOn() {
        if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.S) {
            try {
                val vpnManager: VpnManager? = getSystemService()
                vpnManager?.setAlwaysOnVpnPackage(packageName, null, true)
            } catch (e: Exception) {
                updateStatus("Always-on недоступно: ${e.localizedMessage}")
            }
        }
    }

    private val statusReceiver = object : BroadcastReceiver() {
        override fun onReceive(context: Context?, intent: Intent?) {
            val status = intent?.getStringExtra(MyVpnService.EXTRA_STATUS) ?: return
            isConnected = status == MyVpnService.STATUS_CONNECTED
            updateStatus(status)
        }
    }

    private fun updateStatus(text: String) {
        binding.statusText.text = text
    }

    companion object {
        private const val REQUEST_VPN = 100
    }
}
