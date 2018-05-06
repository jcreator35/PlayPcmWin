package com.example.yamamoto2002.ppwremote6

import android.app.AlertDialog
import android.os.Bundle
import android.support.v7.app.AppCompatActivity
import android.view.Menu
import android.view.MenuItem
import kotlinx.android.synthetic.main.activity_main.*
import kotlinx.android.synthetic.main.content_main.*
import android.os.AsyncTask
import android.view.LayoutInflater
import kotlinx.android.synthetic.main.layout_connection.view.*
import android.view.View.GONE
import android.graphics.BitmapFactory
import android.graphics.Color
import android.graphics.PorterDuff
import android.util.Log
import android.view.View.VISIBLE
import android.widget.Toast
import android.graphics.drawable.Drawable
import android.widget.ImageButton
import android.widget.SeekBar

interface NetworkEventHandler {
    fun onNetworkEventMessage(msg : String)
    fun onNetworkEventCommandReceived(cmd: RemoteCommand)
}

class MainActivity : AppCompatActivity(), NetworkEventHandler, SeekBar.OnSeekBarChangeListener {

    private val mPlayList = mutableListOf<MusicItem>()
    private var mSelectedMusicItem : MusicItem? = null
    private lateinit var mPlayListViewAdapter : PlayListViewAdapter
    private lateinit var mPrefs : Prefs
    private var mConnectTask : ConnectTask? = null
    private var mPlayPositionSkipCounter = 0

    private var mState = State.INITIAL_VOID
    private lateinit var mButtonIconNext : Drawable
    private lateinit var mButtonIconPrev : Drawable
    private lateinit var mButtonIconPlay : Drawable
    private lateinit var mButtonIconPause : Drawable
    private lateinit var mGrayedButtonIconNext : Drawable
    private lateinit var mGrayedButtonIconPrev : Drawable
    private lateinit var mGrayedButtonIconPlay : Drawable
    private lateinit var mGrayedButtonIconPause : Drawable

    companion object {
        const val PLAY_POSITION_SKIP_NUM = 2
    }

    private enum class State {
        INITIAL_VOID,
        PLAYLIST_UPDATING,
        PLAYLIST_EMPTY,
        PLAYLIST_EXISTS
    }

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        setContentView(R.layout.activity_main)
        setSupportActionBar(appbar_toolbar)

        prepareGrayIconImages()

        mPrefs = Prefs(this)

        main_play_list_view.setOnItemClickListener { parent, _, pos, _->
            val item = parent.getItemAtPosition(pos) as MusicItem
            musicItemSelected(item, true)
        }

        main_button_next.setOnClickListener{ _ -> nextButtonPressed() }
        main_button_prev.setOnClickListener{ _ -> prevButtonPressed() }
        main_button_pause.setOnClickListener{ _ -> pauseButtonPressed() }
        main_button_play.setOnClickListener{ _ -> playButtonPressed() }

        main_seek_bar.setOnSeekBarChangeListener(this)

        mPlayListViewAdapter = PlayListViewAdapter(this, mPlayList)
        main_play_list_view.adapter = mPlayListViewAdapter

        // INITIAL_VOID状態。
        playbackStateUpdated(RemoteCommand.STATE_STOPPED)
        updateUIStatus()

        openConnectionDialog()
    }

    override fun onStopTrackingTouch(seekBar: SeekBar?) { }

    override fun onStartTrackingTouch(seekBar: SeekBar?) { }

    override fun onProgressChanged(seekBar: SeekBar?, progress: Int, fromUser: Boolean) {
        if (!fromUser || mSelectedMusicItem == null) {
            return
        }

        mPlayPositionSkipCounter = PLAY_POSITION_SKIP_NUM

        val cmd = RemoteCommand(RemoteCommand.CommandType.Seek, mSelectedMusicItem!!.idx)
        cmd.positionMillisec = progress
        cmd.trackMillisec = mSelectedMusicItem!!.durationMs

        //Log.i("MainActivity", "progress changed ${cmd.positionMillisec} / ${cmd.trackMillisec}")
        trySendMessageAsync(cmd)
    }

    private fun nextButtonPressed() {
        nextPrevButtonPressed(+1)
    }
    private fun prevButtonPressed() {
        nextPrevButtonPressed(-1)
    }
    private fun pauseButtonPressed() {
        trySendMessageAsync(RemoteCommand(RemoteCommand.CommandType.Pause))
    }
    private fun playButtonPressed() {
        if (mSelectedMusicItem == null) {
            return
        }
        trySendMessageAsync(RemoteCommand(RemoteCommand.CommandType.Play, mSelectedMusicItem!!.idx))
    }

    override fun onStop() {
        super.onStop()

        mConnectTask?.stop()
    }

    private fun updateUIStatus() {
        if (mPlayList.isEmpty()){
            main_image_view.visibility = GONE
            main_button_next.visibility = GONE
            main_button_prev.visibility = GONE
            main_button_play.visibility = GONE
            main_button_pause.visibility = GONE
            main_seek_bar.visibility = GONE

            main_text_message.text = getString(R.string.play_list_is_empty)

            main_text_view_album.text = ""
            main_text_view_artist.text = ""
            main_text_view_title.text  = ""
            main_image_view.setImageResource(android.R.drawable.ic_menu_gallery)
            mSelectedMusicItem = null

            main_seek_bar.isEnabled = false
        } else {
            main_image_view.visibility = VISIBLE
            main_button_next.visibility = VISIBLE
            main_button_prev.visibility = VISIBLE
            main_button_play.visibility = VISIBLE
            main_button_pause.visibility = VISIBLE
            main_seek_bar.visibility = VISIBLE

            main_text_message.visibility = GONE
            main_seek_bar.isEnabled = true
        }

        when (mState) {
            State.INITIAL_VOID -> {
                main_text_message.visibility = GONE
                main_progressbar.visibility = GONE
            }
            State.PLAYLIST_UPDATING -> {
                main_text_message.visibility = GONE
                main_progressbar.visibility = VISIBLE
            }
            State.PLAYLIST_EMPTY -> {
                main_progressbar.visibility = GONE
                main_text_message.visibility = VISIBLE
            }
            State.PLAYLIST_EXISTS -> {
                main_text_message.visibility = GONE
                main_progressbar.visibility = GONE
            }
        }
    }

    private fun playbackStateUpdated(state : Int) {
        when (state) {
            RemoteCommand.STATE_STOPPED -> {
                setPlayButtonEnabled(true)
                setPauseButtonEnabled(false)
                setNextButtonEnabled(false)
                setPrevButtonEnabled(false)
            }
            RemoteCommand.STATE_PAUSED -> {
                setPlayButtonEnabled(true)
                setPauseButtonEnabled(false)
                setNextButtonEnabled(true)
                setPrevButtonEnabled(true)
            }
            RemoteCommand.STATE_PLAYING -> {
                setPlayButtonEnabled(false)
                setPauseButtonEnabled(true)
                setNextButtonEnabled(true)
                setPrevButtonEnabled(true)
            }
            else -> {
                Log.e("MainActivity", "playbackStateUpdated unknown state $state")
            }
        }
    }

    private fun setNextButtonEnabled(b : Boolean) {
        if (main_button_next.isEnabled == b){
            return
        }
        main_button_next.isEnabled = b
        setImageButtonEnabled(main_button_next, mButtonIconNext, mGrayedButtonIconNext, b)
    }
    private fun setPrevButtonEnabled(b : Boolean) {
        if (main_button_prev.isEnabled == b){
            return
        }
        main_button_prev.isEnabled = b
        setImageButtonEnabled(main_button_prev, mButtonIconPrev, mGrayedButtonIconPrev, b)
    }
    private fun setPlayButtonEnabled(b : Boolean) {
        if (main_button_play.isEnabled == b){
            return
        }
        main_button_play.isEnabled = b
        setImageButtonEnabled(main_button_play, mButtonIconPlay, mGrayedButtonIconPlay, b)
    }
    private fun setPauseButtonEnabled(b : Boolean) {
        if (main_button_pause.isEnabled == b) {
            return
        }
        main_button_pause.isEnabled = b
        setImageButtonEnabled(main_button_pause, mButtonIconPause, mGrayedButtonIconPause, b)
    }

    private fun prepareGrayIconImages() {
        mGrayedButtonIconNext = convertDrawableToGrayScale(getDrawable(android.R.drawable.ic_media_next)!!)
        mGrayedButtonIconPrev = convertDrawableToGrayScale(getDrawable(android.R.drawable.ic_media_previous)!!)
        mGrayedButtonIconPlay = convertDrawableToGrayScale(getDrawable(android.R.drawable.ic_media_play)!!)
        mGrayedButtonIconPause = convertDrawableToGrayScale(getDrawable(android.R.drawable.ic_media_pause)!!)
        mButtonIconNext  = getDrawable(android.R.drawable.ic_media_next)
        mButtonIconPrev  = getDrawable(android.R.drawable.ic_media_previous)
        mButtonIconPlay  = getDrawable(android.R.drawable.ic_media_play)
        mButtonIconPause  = getDrawable(android.R.drawable.ic_media_pause)
    }

    private fun setImageButtonEnabled(item: ImageButton, enabledDrawable : Drawable, disabledDrawable : Drawable, enabled: Boolean) {
        item.isEnabled = enabled
        val icon = if (enabled) enabledDrawable else disabledDrawable
        item.setImageDrawable(icon)
    }

    private fun convertDrawableToGrayScale(drawable: Drawable): Drawable {
        val res = drawable.mutate()
        res.setColorFilter(Color.GRAY, PorterDuff.Mode.SRC_IN)
        return res
    }

    override fun onCreateOptionsMenu(menu: Menu): Boolean {
        menuInflater.inflate(R.menu.menu_main, menu)
        return true
    }

    override fun onOptionsItemSelected(item: MenuItem): Boolean {
        when (item.itemId) {
            R.id.action_connect -> {
                if (mConnectTask != null && mConnectTask!!.isConnected()) {
                    Toast.makeText(this, "Connection is already established.", Toast.LENGTH_LONG).show()
                    return true
                }

                // 接続ボタン押下。
                mState = State.PLAYLIST_UPDATING
                mPlayList.clear()
                mPlayListViewAdapter.notifyDataSetChanged()
                updateUIStatus()

                openConnectionDialog()
                return true
            }
            R.id.action_disconnect -> {
                if (mConnectTask == null || !mConnectTask!!.isConnected()) {
                    Toast.makeText(this, "Not connected.", Toast.LENGTH_LONG).show()
                    return true
                }
                // 切断。
                mState = State.INITIAL_VOID
                mPlayList.clear()
                mPlayListViewAdapter.notifyDataSetChanged()
                updateUIStatus()

                mConnectTask!!.stop()
                return true
            }
            else -> return super.onOptionsItemSelected(item)
        }
    }

    private fun nextPrevButtonPressed(direction : Int) {
        if (mSelectedMusicItem == null) {
            return
        }

        var nextIdx = mSelectedMusicItem!!.idx + direction
        if (nextIdx < 0) {
            nextIdx = 0
        }
        if (mPlayList.size <= nextIdx) {
            nextIdx = 0
        }
        trySendMessageAsync(RemoteCommand(RemoteCommand.CommandType.Play, nextIdx))
    }

    private fun musicItemSelected(item : MusicItem, byUserOperation : Boolean) {
        //Log.i("PPWRemote6 MainActivity", "musicItemSelected ${item.idx} byUserOperation=$byUserOperation")

        if (item.idx != mPlayListViewAdapter.selectedIdx){
            // update selected position of the playlist
            mPlayListViewAdapter.selectedIdx = item.idx
            mPlayListViewAdapter.notifyDataSetChanged()

            main_play_list_view.smoothScrollToPosition(item.idx)
        }

        main_text_view_album.text = item.albumName
        main_text_view_artist.text = item.artistName
        main_text_view_title.text  = item.titleName

        //Log.i("MainActivity", "musicItemSelected ${item.idx} mi.albumCoverArt size = ${item.albumCoverArt.size}")
        if (item.albumCoverArt.isEmpty()) {
            main_image_view.setImageResource(android.R.drawable.ic_menu_gallery)
        } else {
            main_image_view.setImageBitmap(BitmapFactory.decodeByteArray(
                    item.albumCoverArt, 0, item.albumCoverArt.size)
            )
        }

        mSelectedMusicItem = item

        if (byUserOperation) {
            mPlayPositionSkipCounter = PLAY_POSITION_SKIP_NUM

            trySendMessageAsync(RemoteCommand(RemoteCommand.CommandType.Play, item.idx))
        }
    }

    private fun openConnectionDialog() {
        val builder = AlertDialog.Builder(this)
        builder.setTitle(getString(R.string.connect_dialog_message))
        builder.setMessage("")
        val view = LayoutInflater.from(this).inflate(R.layout.layout_connection, null)
        builder.setView(view)

        if (mPrefs.serverIpAddress.isNotEmpty()) {
            view.settings_ip_address.setText(mPrefs.serverIpAddress)
        } else {
            view.settings_ip_address.setText("")
        }
        view.settings_port.setText(mPrefs.serverPort.toString())

        builder.setPositiveButton(getString(R.string.connect_dialog_connect),
                { _, _ ->
                    // 接続開始。
                    mPrefs.serverIpAddress = view.settings_ip_address.text.toString()
                    mPrefs.serverPort = view.settings_port.text.toString().toInt()

                    mState = State.PLAYLIST_UPDATING
                    updateUIStatus()

                    mConnectTask = ConnectTask(this, mPrefs.serverIpAddress, mPrefs.serverPort)
                    mConnectTask!!.execute(null)
                })
        val dialog = builder.create()
        dialog.show()
    }

    private fun showErrorOKDialog(title: String, msg: String) {
        val builder = AlertDialog.Builder(this@MainActivity,
                android.R.style.Theme_Material_Dialog_Alert)
        builder.setTitle(title)
                .setMessage(msg)
                .setPositiveButton(android.R.string.ok) { _, _ ->
                    // error dialog is closed
                }
                .setIcon(android.R.drawable.ic_dialog_alert)
                .show()
    }

    private fun trySendMessageAsync(cmd : RemoteCommand) : Boolean {
        if (mConnectTask == null) {
            return false
        }

        return mConnectTask!!.sendMessageAsync(cmd)
    }

    override fun onNetworkEventMessage(msg: String) {
        showErrorOKDialog("Network Event", msg)

        mState = MainActivity.State.INITIAL_VOID
        mPlayList.clear()
        mPlayListViewAdapter.notifyDataSetChanged()
        updateUIStatus()
    }

    override fun onNetworkEventCommandReceived(cmd: RemoteCommand) {
        when (cmd.cmd) {
            RemoteCommand.CommandType.Exit -> {
                Log.i("PPWRemote", "onProgressUpdate() Exit.")
            }
            RemoteCommand.CommandType.Stop -> {
                playbackStateUpdated(RemoteCommand.STATE_STOPPED)
            }
            RemoteCommand.CommandType.Pause -> {
                playbackStateUpdated(RemoteCommand.STATE_PAUSED)
                if (mPlayList.size <= cmd.trackIdx) {
                    Log.e("MainActivity", "Error trackIdx  ${cmd.trackIdx} is larger than playlist size ${mPlayList.size}")
                }
                val item = mPlayList[cmd.trackIdx]
                musicItemSelected(item, false)
            }
            RemoteCommand.CommandType.PlayPositionUpdate -> {
                if (0 < mPlayPositionSkipCounter) {
                    // 過去の再生状態が遅れて届くために再生曲が一瞬戻ったように見える問題の修正。

                    //Log.i("MainActivity", "skipped PlayPositionUpdate")
                    --mPlayPositionSkipCounter
                    return
                }

                playbackStateUpdated(cmd.state)
                if (mPlayList.size <= cmd.trackIdx) {
                    //Log.i("MainActivity", "Error trackIdx  ${cmd.trackIdx} is larger than playlist size ${mPlayList.size}")
                    return
                }
                val item = mPlayList[cmd.trackIdx]
                musicItemSelected(item, false)
                main_seek_bar.max = item.durationMs
                if (item.durationMs <= cmd.positionMillisec) {
                    Log.e("MainActivity", "Error item duration ${item.durationMs} smaller than play position ${cmd.positionMillisec}")
                    return
                }
                main_seek_bar.progress = cmd.positionMillisec
            }
            RemoteCommand.CommandType.PlaylistData -> {
                playbackStateUpdated(cmd.state)

                if (cmd.playlist.size == 0) {
                    mPlayList.clear()
                    mPlayListViewAdapter.notifyDataSetChanged()

                    mState = MainActivity.State.PLAYLIST_EMPTY
                    updateUIStatus()
                } else {
                    mPlayList.clear()
                    for (i in 0 until cmd.playlist.size) {
                        val p = cmd.playlist[i]
                        val mi = MusicItem()
                        mi.idx = i

                        //Log.i("MainActivity", "p.albumCoverArt $i size = ${p.albumCoverArt.size}")
                        if (p.albumCoverArt.isEmpty()) {
                            mi.albumCoverArt = ByteArray(0)
                        } else {
                            mi.albumCoverArt = p.albumCoverArt
                        }

                        mi.durationMs = p.durationMillsec
                        mi.sampleRate = p.sampleRate
                        mi.bitDepth = p.bitDepth
                        mi.albumName = p.albumName
                        mi.artistName = p.artistName
                        mi.titleName = p.titleName
                        mPlayList.add(mi)
                    }
                    mPlayListViewAdapter.notifyDataSetChanged()
                    musicItemSelected(mPlayList[cmd.trackIdx], false)

                    mState = MainActivity.State.PLAYLIST_EXISTS
                    updateUIStatus()
                }
            }
            else -> {
                throw NotImplementedError()
            }
        }
    }

    class ConnectTask(private var mNetworkEventHandler : NetworkEventHandler?, private val serverIpAddressStr : String, private val serverPort : Int) : AsyncTask<Void, String, Unit>() {
        private val mRecvCommandQ = mutableListOf<RemoteCommand>()
        private val mLock = Object()
        private var mTcpClient : PPWTcpClient? = null
        private var mEnding = false
        //private var mNetworkEventHandler : NetworkEventHandler? = null

        /** @retval true : succeeded to call stop
         *  @retval false : already TCP disconnected and there is no connection
         */
        fun stop() : Boolean {
            mEnding = true
            if (mTcpClient != null) {
                mTcpClient!!.sendExitAsync()
                return true
            }

            return false
        }

        fun isConnected() : Boolean {
            return mTcpClient != null
        }

        fun sendMessageAsync(cmd: RemoteCommand) : Boolean {
            if (mTcpClient != null) {
                mTcpClient!!.sendMessageAsync(cmd)
                return true
            }

            return false
        }

        override fun doInBackground(vararg params: Void?) {
            mTcpClient = PPWTcpClient(object : PPWTcpClient.OnCommandReceived {
                override fun commandReceived(cmd: RemoteCommand): Boolean {
                    synchronized(mLock) {
                        mRecvCommandQ.add(cmd)
                    }

                    if (!mEnding) {
                        // calls the onProgressUpdate
                        publishProgress()
                    }

                    if (cmd.cmd == RemoteCommand.CommandType.Exit) {
                        // exit
                        return true
                    }

                    // continue
                    return false
                }
            })

            val resultMsg = mTcpClient!!.runBlocking(serverIpAddressStr, serverPort)
            if (!mEnding) {
                publishProgress(resultMsg)
            }
            mTcpClient = null
        }

        override fun onProgressUpdate(vararg v: String) {
            super.onProgressUpdate(*v)

            if (mEnding) {
                return
            }

            if (v.isNotEmpty()) {
                mNetworkEventHandler?.onNetworkEventMessage(v[0])
                return
            }

            var cmdCount = 0
            synchronized(mLock) {
                cmdCount = mRecvCommandQ.size
            }

            while (0 < cmdCount) {
                var cmd = RemoteCommand(RemoteCommand.CommandType.Exit)
                synchronized(mLock) {
                    cmd = mRecvCommandQ.first()
                    mRecvCommandQ.removeAt(0)
                }

                mNetworkEventHandler?.onNetworkEventCommandReceived(cmd)

                synchronized(mLock) {
                    cmdCount = mRecvCommandQ.size
                }
            }
        }
    }
}
