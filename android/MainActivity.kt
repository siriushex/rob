package com.example.rob

import android.os.Bundle
import android.widget.ArrayAdapter
import android.widget.ListView
import androidx.appcompat.app.AlertDialog
import androidx.appcompat.app.AppCompatActivity
import com.google.android.gms.maps.CameraUpdateFactory
import com.google.android.gms.maps.GoogleMap
import com.google.android.gms.maps.MapsInitializer
import com.google.android.gms.maps.SupportMapFragment
import com.google.android.gms.maps.model.LatLng
import com.google.android.gms.maps.model.MarkerOptions

/**
 * Displays up to ten server locations on a Google Map. Selecting a marker
 * shows the server name and offers a Connect button. If the map is not
 * available (e.g. missing services or network), a fallback list view of
 * servers is displayed instead.
 */
class MainActivity : AppCompatActivity(), com.google.android.gms.maps.OnMapReadyCallback {

    data class Server(val name: String, val location: LatLng)

    private val servers = listOf(
        Server("Berlin", LatLng(52.5200, 13.4050)),
        Server("London", LatLng(51.5074, -0.1278)),
        Server("Paris", LatLng(48.8566, 2.3522)),
        Server("Amsterdam", LatLng(52.3676, 4.9041)),
        Server("Warsaw", LatLng(52.2297, 21.0122)),
        Server("New York", LatLng(40.7128, -74.0060)),
        Server("San Francisco", LatLng(37.7749, -122.4194)),
        Server("Chicago", LatLng(41.8781, -87.6298)),
        Server("Dallas", LatLng(32.7767, -96.7970)),
        Server("Miami", LatLng(25.7617, -80.1918))
    )

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        try {
            MapsInitializer.initialize(this)
            val fragment = SupportMapFragment.newInstance()
            supportFragmentManager.beginTransaction()
                .replace(android.R.id.content, fragment)
                .commit()
            fragment.getMapAsync(this)
        } catch (e: Exception) {
            showServerList()
        }
    }

    override fun onMapReady(map: GoogleMap) {
        map.uiSettings.isZoomControlsEnabled = true
        servers.forEach { server ->
            map.addMarker(
                MarkerOptions()
                    .position(server.location)
                    .title(server.name)
            )
        }
        map.moveCamera(CameraUpdateFactory.newLatLngZoom(servers.first().location, 2f))
        map.setOnMarkerClickListener { marker ->
            AlertDialog.Builder(this)
                .setTitle(marker.title)
                .setMessage("Connect to ${marker.title}?")
                .setPositiveButton("Connect", null)
                .setNegativeButton("Cancel", null)
                .show()
            true
        }
    }

    private fun showServerList() {
        val listView = ListView(this)
        val names = servers.map { it.name }
        listView.adapter = ArrayAdapter(this, android.R.layout.simple_list_item_1, names)
        listView.setOnItemClickListener { _, _, position, _ ->
            val server = servers[position]
            AlertDialog.Builder(this)
                .setTitle(server.name)
                .setMessage("Connect to ${server.name}?")
                .setPositiveButton("Connect", null)
                .setNegativeButton("Cancel", null)
                .show()
        }
        setContentView(listView)
    }
}

