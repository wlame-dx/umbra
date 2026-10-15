package com.umbra.client

import android.content.Context
import android.view.LayoutInflater
import android.view.View
import android.view.ViewGroup
import android.widget.BaseAdapter
import android.widget.TextView

class HostAdapter(private val context: Context, private val items: List<UmbraHost>) : BaseAdapter() {

    override fun getCount(): Int = items.size

    override fun getItem(position: Int): Any = items[position]

    override fun getItemId(position: Int): Long = position.toLong()

    override fun getView(position: Int, convertView: View?, parent: ViewGroup?): View {
        val view = convertView ?: LayoutInflater.from(context).inflate(R.layout.item_host, parent, false)
        val host = items[position]
        view.findViewById<TextView>(R.id.hostName).text = host.name
        view.findViewById<TextView>(R.id.hostAddr).text = host.address + ":" + host.port
        return view
    }
}
