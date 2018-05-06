package com.example.yamamoto2002.ppwremote6

import android.util.Log
import java.io.IOException
import java.io.InputStream
import java.lang.System.arraycopy
import java.nio.ByteBuffer
import java.nio.ByteOrder
import java.nio.charset.StandardCharsets

/**
    @param trackIdx Track idx. Used on Play Pause and Seek.
 */
class RemoteCommand(var cmd : CommandType, var trackIdx : Int = 0) {
    enum class CommandType(val fourcc:Int) {
        PlaylistData(0x534c4c50),
        Exit(0x54495845),
        Pause(0x53554150),
        Stop(0x504f5453),
        PlayPositionUpdate(0x55504c50),

        Greetings(0x52575050),
        PlaylistWant(0x574c4c50),
        Play(0x59414c50),
        Seek(0x4b454553),
        SelectTrack(0x544c4553),
    }

    /// STATE_STOPPED STATE_PLAYING STATE_PAUSED
    var state : Int = 0

    /// Play position on track. Used on Play Pause and Seek.
    var positionMillisec: Int = 0

    /// used by Seek send
    var trackMillisec : Int = 0

    class PlayListItem {
        var durationMillsec: Int = 0
        var sampleRate : Int = 0
        var bitDepth : Int = 0
        var albumName: String = ""
        var artistName: String = ""
        var titleName: String = ""
        var albumCoverArt: ByteArray = ByteArray(0)
    }

    /// PlaylistData
    var playlist: MutableList<PlayListItem> = mutableListOf()

    companion object {
        const val VERSION: Long = 102

        const val STATE_STOPPED : Int = 0
        const val STATE_PLAYING : Int = 1
        const val STATE_PAUSED : Int = 2

        private const val TOO_LARGE : Long = 100 * 1000 * 1000
        private const val TOO_MANY_TRACKS : Long = 10000

        fun intToByteArray(v: Int): ByteArray {
            val b = ByteBuffer.allocate(4)
            b.order(ByteOrder.LITTLE_ENDIAN)
            b.putInt(v)
            return b.array()
        }

        fun longToByteArray(v: Long): ByteArray {
            val b = ByteBuffer.allocate(8)
            b.order(ByteOrder.LITTLE_ENDIAN)
            b.putLong(v)
            return b.array()
        }

        private fun byteArrayToInt(v :ByteArray) : Int {
            return ByteBuffer.wrap(v,0,4).order(java.nio.ByteOrder.LITTLE_ENDIAN).int
        }
        private fun byteArrayToLong(v :ByteArray) : Long {
            return ByteBuffer.wrap(v,0,8).order(java.nio.ByteOrder.LITTLE_ENDIAN).long
        }

        // ブロックして所望サイズのデータを受信する。
        private fun readBlocking(ins : InputStream, bytes : Int) : ByteArray {
            val r = ByteArray(bytes)
            var offs = 0
            while (offs < bytes) {
                if (0 == ins.available()) {
                    Thread.sleep(10)
                    //Log.i("RemoteCommand", "Sleep...")
                    continue
                }
                val wantBytes = bytes - offs
                val readBytes = ins.read(r, offs, wantBytes)
                if (readBytes == -1) {
                    throw IOException("readBlocking failed")
                }
                offs += readBytes
            }

            return r
        }

        private fun readInt(ins : InputStream) : Int {
            val buff = readBlocking(ins, 4)
            return byteArrayToInt(buff)
        }

        private fun readLong(ins : InputStream) : Long {
            val buff = readBlocking(ins, 8)
            return byteArrayToLong(buff)
        }

        private fun readByteArray(ins : InputStream) : ByteArray {
            val bytes = readInt(ins)
            if (bytes < 0 || TOO_LARGE < bytes) {
                throw IOException("String too large")
            }
            if (bytes == 0) {
                return ByteArray(0)
            }

            return readBlocking(ins, bytes)
        }

        private fun readUtf8(ins : InputStream) : String {
            val buff = readByteArray(ins)
            if (buff.isEmpty()) {
                return ""
            }
            return String(buff, StandardCharsets.UTF_8)
        }

        fun fromStream(ins : InputStream) : RemoteCommand {
            val cmd = RemoteCommand(CommandType.Exit)

            val fourcc = readInt(ins)
            val bytes = readLong(ins)

            if (bytes < 0 || TOO_LARGE < bytes) {
                return cmd
            }

            when (fourcc) {
                CommandType.PlaylistData.fourcc -> {
                    /*
                    * Number of payload bytes (int64)
                    *
                    * playbackState    (int32)
                    *
                    * Number of tracks (int32)
                    * if (0 < number of tracks) {
                    *   selected track (int32)
                    *
                    *   Track0 duration millisec (int32)
                    *   Track0 sampleRate        (int32)
                    *   Track0 bit depth         (int32)
                    *   Track0 albumName bytes (int32)
                    *   Track0 albumName (utf8 string)
                    *   Track0 artistName bytes (int32)
                    *   Track0 artistName (utf8 string)
                    *   Track0 titleName bytes (int32)
                    *   Track0 titleName (utf8 string)
                    *   Track0 albumCoverArt bytes (int32)
                    *   Track0 albumCoverArt (binary)
                    *
                    *   Track1
                    *   ...
                    * }
                    */

                    cmd.state = readInt(ins)
                    if (cmd.state < 0 || STATE_PAUSED < cmd.state) {
                        Log.e("RemoteCommand", "nTracks is out of range ${cmd.state}")
                        return cmd
                    }

                    val nTracks = readInt(ins)
                    if (nTracks < 0 || TOO_MANY_TRACKS < nTracks) {
                        Log.e("RemoteCommand", "nTracks is out of range $nTracks")
                        return cmd
                    }

                    if (nTracks == 0) {
                        cmd.cmd = RemoteCommand.CommandType.PlaylistData
                        return cmd
                    }

                    cmd.trackIdx = readInt(ins)
                    if (cmd.trackIdx < 0 || nTracks <= cmd.trackIdx) {
                        Log.i("RemoteCommand", "trackIdx is out of range ${cmd.trackIdx} fixed to 0")
                        cmd.trackIdx = 0
                    }

                    for (i in 0 until nTracks) {
                        //Log.i("RemoteCommand", "Reading track $i of $nTracks")

                        val p = RemoteCommand.PlayListItem()
                        p.durationMillsec = readInt(ins)
                        p.sampleRate = readInt(ins)
                        p.bitDepth = readInt(ins)
                        p.albumName = readUtf8(ins)
                        p.artistName = readUtf8(ins)
                        p.titleName = readUtf8(ins)
                        p.albumCoverArt = readByteArray(ins)
                       //Log.i("RemoteCommand", "p.albumCoverArt $i size = ${p.albumCoverArt.size}")
                        cmd.playlist.add(p)
                    }

                    cmd.cmd = CommandType.PlaylistData
                }
                CommandType.Pause.fourcc -> {
                    if (bytes != 4L) {
                        Log.e("RemoteCommand", "Pause bytes not equal to 4 $bytes")
                        return cmd
                    }
                    cmd.trackIdx = readInt(ins)
                    if (cmd.trackIdx < 0) {
                        Log.e("RemoteCommand", "trackIdx is negative ${cmd.trackIdx}")
                        return cmd
                    }
                    cmd.cmd = CommandType.Pause
                }
                CommandType.PlayPositionUpdate.fourcc -> {
                    if (bytes != 12L) {
                        Log.e("RemoteCommand", "PlayPositionUpdate bytes not equal to 12 $bytes")
                        return cmd
                    }
                    cmd.state = readInt(ins)
                    if (cmd.state < 0 || 2 < cmd.state) {
                        Log.e("RemoteCommand", "state is invalid ${cmd.state}")
                        return cmd
                    }
                    cmd.trackIdx = readInt(ins)
                    if (cmd.trackIdx < 0) {
                        Log.e("RemoteCommand", "trackIdx is negative ${cmd.trackIdx}")
                        return cmd
                    }
                    cmd.positionMillisec = readInt(ins)
                    if (cmd.positionMillisec < 0) {
                        Log.e("RemoteCommand", "positionMs is negative ${cmd.positionMillisec}")
                        return cmd
                    }
                    cmd.cmd = CommandType.PlayPositionUpdate
                }
                CommandType.Exit.fourcc -> {
                    if (bytes != 0L) {
                        Log.e("RemoteCommand", "Exit bytes not equal to 0 $bytes")
                        return cmd
                    }
                }
                CommandType.Stop.fourcc -> {
                    if (bytes != 0L) {
                        Log.e("RemoteCommand", "Stop bytes not equal to 0 $bytes")
                        return cmd
                    }
                    cmd.cmd = CommandType.Stop
                }
                else -> {
                    Log.i("RemoteCommand", "Unknown fourcc $fourcc")
                }
            }
            return cmd
        }
    }

    private fun countByteArrayListTotalBytes(a : List<ByteArray>) : Int {
        var bytes = 0
        a.onEach { bytes += it.size }

        return bytes
    }

    private fun byteArrayListToByteArray(a : List<ByteArray>) : ByteArray {
        val bytes = countByteArrayListTotalBytes(a)

        val r = ByteArray(bytes)
        var offs = 0
        a.onEach {
            arraycopy(it,0,r,offs,it.size)
            offs += it.size
        }

        return r
    }

    fun toByteArray() : ByteArray {
        val r = mutableListOf<ByteArray>()

        when (cmd) {
            CommandType.Greetings -> {
                /*
                  "Greetings"
                  size    = 8   (4bytes)
                  version = 100 (8bytes)
                 */
                r.add(longToByteArray(VERSION))
            }
            CommandType.PlaylistWant -> {
                // no payload
            }
            CommandType.Exit -> {
                // no payload
            }
            CommandType.Pause -> {
                // no payload
            }
            CommandType.Play -> {
                /*
                   "PLAY"
                   trackIdx (4bytes)
                 */
                r.add(intToByteArray(trackIdx))
            }
            CommandType.Seek -> {
                /*
                    total 8bytes
                    positionMillisec (4bytes)
                    trackMillisec (4bytes)
                 */
                r.add(intToByteArray(positionMillisec))
                r.add(intToByteArray(trackMillisec))
            }
            else -> {
                throw NotImplementedError("RemoteCommand.toByteArray")
            }
        }

        // calc payload size
        val payloadBytes = countByteArrayListTotalBytes(r)

        // insert header and payload bytes in the front
        r.add(0, longToByteArray(payloadBytes.toLong()))
        r.add(0, intToByteArray(cmd.fourcc))

        return byteArrayListToByteArray(r)
    }

}