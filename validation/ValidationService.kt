package com.example.validation

import java.io.BufferedReader
import java.io.InputStreamReader
import java.net.HttpURLConnection
import java.net.URL
import org.json.JSONObject

/**
 * Sends purchase tokens/receipts to backend for validation.
 */
object ValidationService {
    @Throws(Exception::class)
    fun validateAndroidPurchase(purchaseToken: String): Boolean {
        val url = URL("https://example.com/api/validate/android")
        val connection = (url.openConnection() as HttpURLConnection).apply {
            requestMethod = "POST"
            doOutput = true
            setRequestProperty("Content-Type", "application/json")
            outputStream.use { it.write("{""token"":""$purchaseToken""}".toByteArray()) }
        }
        val response = connection.inputStream.bufferedReader().use(BufferedReader::readText)
        return JSONObject(response).optBoolean("success")
    }

    @Throws(Exception::class)
    fun validateIosReceipt(receipt: String): Boolean {
        val url = URL("https://example.com/api/validate/ios")
        val connection = (url.openConnection() as HttpURLConnection).apply {
            requestMethod = "POST"
            doOutput = true
            setRequestProperty("Content-Type", "application/json")
            outputStream.use { it.write("{""receipt"":""$receipt""}".toByteArray()) }
        }
        val response = connection.inputStream.bufferedReader().use(BufferedReader::readText)
        return JSONObject(response).optBoolean("success")
    }
}
