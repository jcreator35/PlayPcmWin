package com.example.yamamoto2002.ppwremote6

import android.content.Context
import android.graphics.BitmapFactory
import android.view.LayoutInflater
import android.view.View
import android.view.ViewGroup
import android.widget.BaseAdapter
import android.widget.TextView
import android.widget.ImageView

class PlayListViewAdapter(context : Context, private val dataSource : MutableList<MusicItem>) : BaseAdapter() {
    private val inflater: LayoutInflater
            = context.getSystemService(Context.LAYOUT_INFLATER_SERVICE) as LayoutInflater

    var selectedIdx : Int = 0

    private class Holder {
        lateinit var albumCoverArtView : ImageView
        lateinit var albumNameView : TextView
        lateinit var artistNameView : TextView
        lateinit var titleNameView : TextView
        lateinit var durationView : TextView
        lateinit var sampleFormatView : TextView
    }

    override fun getCount(): Int {
        return dataSource.size
    }

    override fun getItem(position: Int): Any {
        return dataSource[position]
    }

    override fun getItemId(position: Int): Long {
        return position.toLong()
    }

    override fun getView(pos: Int, convertView: View?, parent: ViewGroup): View {
        val mi = getItem(pos) as MusicItem

        val v : View
        val h : Holder

        if (convertView == null) {
            v = inflater.inflate(R.layout.list_item_music, parent, false)
            h = Holder()
            h.albumCoverArtView = v.findViewById(R.id.list_item_image_view_album_cover_art)
            h.albumNameView = v.findViewById(R.id.list_item_text_view_album_name)
            h.artistNameView = v.findViewById(R.id.list_item_text_view_artist_name)
            h.titleNameView = v.findViewById(R.id.list_item_text_view_title_name)
            h.durationView = v.findViewById(R.id.list_item_text_view_duration)
            h.sampleFormatView = v.findViewById(R.id.list_item_text_view_sample_format)
            v.tag = h
        } else {
            v = convertView
            h = v.tag as Holder
        }

        //Log.i("PlayListViewAdapter", "mi ${mi.idx} mi.albumCoverArt size = ${mi.albumCoverArt.size}")
        if (mi.albumCoverArt.isEmpty()) {
            h.albumCoverArtView.setImageResource(android.R.drawable.ic_menu_gallery)
        } else {
            h.albumCoverArtView.setImageBitmap(BitmapFactory.decodeByteArray(
                    mi.albumCoverArt, 0, mi.albumCoverArt.size))
        }
        h.albumNameView.text = mi.albumName
        h.artistNameView.text = mi.artistName
        h.titleNameView.text = mi.titleName

        // duration 00:00
        val durationTotalSec = (mi.durationMs / 1000)
        val durationMin = durationTotalSec / 60
        val durationSec = durationTotalSec - durationMin * 60
        h.durationView.text = "%02d:%02d".format(durationMin,durationSec)

        // Sample format display PCM 44.1kHz
        var fmt = "PCM"
        if (mi.bitDepth == 1) {
            fmt = "DSD"
        }
        var rate = mi.sampleRate.toDouble() / 1000.0
        var rateStr = "${rate}kHz"
        if (1000 < rate) {
            rate = (mi.sampleRate / 100000).toDouble() / 10.0
            rateStr = "${rate}MHz"
        }
        h.sampleFormatView.text = "$fmt $rateStr ${mi.bitDepth}bit"

        if (pos == selectedIdx) {
            v.setBackgroundResource(android.R.color.holo_blue_bright)
        } else {
            v.setBackgroundResource(android.R.color.background_light)
        }

        return v
    }
}