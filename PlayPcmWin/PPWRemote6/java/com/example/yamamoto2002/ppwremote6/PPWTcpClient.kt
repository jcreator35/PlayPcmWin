package com.example.yamamoto2002.ppwremote6

import android.util.Log
import java.io.*
import java.net.InetAddress
import java.net.InetSocketAddress
import java.net.Socket

class PPWTcpClient
(listener: OnCommandReceived) {

    private var mMessageListener: OnCommandReceived? = null
    private var mRun = false
    private var mOut: OutputStream? = null
    private var mIs : InputStream? = null
    private val mSendCommandQ = mutableListOf<RemoteCommand>()
    private val mLock = Object()
    private var mSocket : Socket ?= null

    companion object {
        const val SOCKET_CONNECT_TIMEOUT : Int = 3000
        const val SOCKET_TIMEOUT : Int = 5000
        const val SLEEP_MS : Long = 100
    }

    init {
        mMessageListener = listener
    }

    private fun sendMessageBlocking(cmd : RemoteCommand) {
        if (mOut != null) {
            mOut!!.write(cmd.toByteArray())
        }
    }

    fun sendMessageAsync(cmd: RemoteCommand) {
        synchronized (mLock) {
            mSendCommandQ.add(cmd)
        }
    }

    private fun sendQueuedMessagesBlocking() : Boolean {
        var bContinue = true

        var count = 0
        synchronized(mLock) {
            count = mSendCommandQ.size
        }

        if (count == 0) {
            return true
        }

        while (0 < count) {
            var cmd = RemoteCommand(RemoteCommand.CommandType.Exit)
            synchronized(mLock) {
                cmd = mSendCommandQ[0]
                mSendCommandQ.removeAt(0)
            }

            sendMessageBlocking(cmd)

            if (cmd.cmd == RemoteCommand.CommandType.Exit) {
                bContinue = false
            }

            synchronized(mLock) {
                count = mSendCommandQ.size
            }
        }
        mOut!!.flush()

        return bContinue
    }

    fun abort() {
        mSocket?.close()
        mSocket = null
    }

    fun sendExitAsync() {
        sendMessageAsync(RemoteCommand(RemoteCommand.CommandType.Exit))
    }

    fun runBlocking(serverIpAddressStr : String, serverPort : Int) : String {
        mRun = true

        var resultMsg = "Successfully disconnected."

        try {
            Log.i("PPWTcpClient", "Connecting to $serverIpAddressStr:$serverPort ...")
            val serverAddress = InetAddress.getByName(serverIpAddressStr)
            mSocket = Socket()
            mSocket!!.connect(InetSocketAddress(serverAddress, serverPort), SOCKET_CONNECT_TIMEOUT)
            mSocket!!.soTimeout = SOCKET_TIMEOUT

            try {
                mOut = mSocket!!.getOutputStream()

                // send greeting and playlist request
                sendMessageBlocking(RemoteCommand(RemoteCommand.CommandType.Greetings))
                sendMessageBlocking(RemoteCommand(RemoteCommand.CommandType.PlaylistWant))
                mOut!!.flush()

                Log.i("PPWTcpClient", "PlaylistWant Sent.")

                mIs = mSocket!!.getInputStream()

                while (mRun) {
                    mRun = sendQueuedMessagesBlocking()

                    if (0 == mIs!!.available()) {
                        Thread.sleep(SLEEP_MS)
                        continue
                    }

                    val cmd = RemoteCommand.fromStream(mIs!!)
                    val bExit = mMessageListener!!.commandReceived(cmd)
                    if (bExit) {
                        resultMsg = "Disconnected from PPWServer."
                        break
                    }
                }
            } catch (e: Exception) {
                Log.e("PPWTcpClient", "Error $e")
                resultMsg = "$e"
            } finally {
                mSocket?.close()
                mIs = null
                mOut = null
            }
        } catch (e: Exception) {
            Log.e("PPWTcpClient", "Error $e")
            resultMsg = "$e"
        }

        return resultMsg
    }

    interface OnCommandReceived {
        /// return true: exit
        fun commandReceived(cmd: RemoteCommand) : Boolean
    }
}