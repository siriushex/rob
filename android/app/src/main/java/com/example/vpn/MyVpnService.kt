package com.example.vpn

import android.content.Context
import android.content.Intent
import android.net.VpnService
import android.os.Build
import android.os.ParcelFileDescriptor
import android.util.Log
import androidx.core.content.ContextCompat
import androidx.localbroadcastmanager.content.LocalBroadcastManager

class MyVpnService : VpnService() {
    private var vpnInterface: ParcelFileDescriptor? = null
    private var ssProcess: Process? = null

    override fun onStartCommand(intent: Intent?, flags: Int, startId: Int): Int {
        return try {
            startShadowsocks()
            startVpn()
            notifyStatus(STATUS_CONNECTED)
            START_STICKY
        } catch (e: Exception) {
            Log.e(TAG, "VPN start failed", e)
            notifyStatus("Ошибка: ${e.localizedMessage}")
            stopSelf()
            START_NOT_STICKY
        }
    }

    override fun onDestroy() {
        stopVpn()
        stopShadowsocks()
        notifyStatus(STATUS_DISCONNECTED)
        super.onDestroy()
    }

    private fun startVpn() {
        val builder = Builder()
        builder.addAddress("10.0.0.2", 32)
        builder.addRoute("0.0.0.0", 0)
        vpnInterface = builder.setSession("VPN").establish()
        startTun2Socks()
    }

    private fun stopVpn() {
        vpnInterface?.close()
        vpnInterface = null
    }

    private fun startShadowsocks() {
        val cmd = arrayOf(
            "ss-local", "-s", "server", "-p", "8388", "-l", "1080",
            "-k", "password", "-m", "aes-256-gcm"
        )
        ssProcess = Runtime.getRuntime().exec(cmd)
    }

    private fun stopShadowsocks() {
        ssProcess?.destroy()
        ssProcess = null
    }

    private fun startTun2Socks() {
        val fd = vpnInterface?.fileDescriptor ?: return
        Thread {
            try {
                // Placeholder: integrate with tun2socks library
                // Tun2Socks.start(fd, "127.0.0.1", 1080)
            } catch (e: Exception) {
                Log.e(TAG, "tun2socks error", e)
            }
        }.start()
    }

    private fun notifyStatus(status: String) {
        val intent = Intent(ACTION_STATUS).apply {
            putExtra(EXTRA_STATUS, status)
        }
        LocalBroadcastManager.getInstance(this).sendBroadcast(intent)
    }

    companion object {
        const val ACTION_STATUS = "com.example.vpn.STATUS"
        const val EXTRA_STATUS = "status"
        const val STATUS_CONNECTED = "Подключено"
        const val STATUS_DISCONNECTED = "Отключено"
        private const val TAG = "MyVpnService"

        fun start(context: Context) {
            val intent = Intent(context, MyVpnService::class.java)
            if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.O) {
                ContextCompat.startForegroundService(context, intent)
            } else {
                context.startService(intent)
            }
        }

        fun stop(context: Context) {
            context.stopService(Intent(context, MyVpnService::class.java))
        }
    }
}
