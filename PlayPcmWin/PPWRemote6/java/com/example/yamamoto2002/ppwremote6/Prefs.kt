package com.example.yamamoto2002.ppwremote6

import android.content.Context
import android.content.SharedPreferences

class Prefs (context: Context) {
    private val prefsFileName = """com.example.yamamoto2002.prefs"""
    private val serverIpAddressKey = """server_address"""
    private val serverPortKey = """server_port"""
    private val prefs: SharedPreferences = context.getSharedPreferences(prefsFileName, 0)

    var serverIpAddress: String
        get() {
            return prefs.getString(serverIpAddressKey, "")
        }
        set(value) = prefs.edit().putString(serverIpAddressKey, value).apply()
    var serverPort: Int
        get() = prefs.getInt(serverPortKey, 2002)
        set(value) = prefs.edit().putInt(serverPortKey, value).apply()
}