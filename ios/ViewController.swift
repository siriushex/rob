import UIKit
import MapKit

/// Shows up to ten server locations on an Apple Map. Selecting a pin
/// displays the server name and a Connect button. If the map fails to
/// load, a table view listing all servers is used instead.
class ViewController: UIViewController, MKMapViewDelegate, UITableViewDataSource, UITableViewDelegate {
    struct Server {
        let name: String
        let coordinate: CLLocationCoordinate2D
    }

    private let servers: [Server] = [
        Server(name: "Berlin", coordinate: CLLocationCoordinate2D(latitude: 52.5200, longitude: 13.4050)),
        Server(name: "London", coordinate: CLLocationCoordinate2D(latitude: 51.5074, longitude: -0.1278)),
        Server(name: "Paris", coordinate: CLLocationCoordinate2D(latitude: 48.8566, longitude: 2.3522)),
        Server(name: "Amsterdam", coordinate: CLLocationCoordinate2D(latitude: 52.3676, longitude: 4.9041)),
        Server(name: "Warsaw", coordinate: CLLocationCoordinate2D(latitude: 52.2297, longitude: 21.0122)),
        Server(name: "New York", coordinate: CLLocationCoordinate2D(latitude: 40.7128, longitude: -74.0060)),
        Server(name: "San Francisco", coordinate: CLLocationCoordinate2D(latitude: 37.7749, longitude: -122.4194)),
        Server(name: "Chicago", coordinate: CLLocationCoordinate2D(latitude: 41.8781, longitude: -87.6298)),
        Server(name: "Dallas", coordinate: CLLocationCoordinate2D(latitude: 32.7767, longitude: -96.7970)),
        Server(name: "Miami", coordinate: CLLocationCoordinate2D(latitude: 25.7617, longitude: -80.1918))
    ]

    private let mapView = MKMapView()
    private let tableView = UITableView()

    override func viewDidLoad() {
        super.viewDidLoad()
        mapView.delegate = self
        tableView.dataSource = self
        tableView.delegate = self

        view.addSubview(mapView)
        mapView.frame = view.bounds

        for server in servers {
            let annotation = MKPointAnnotation()
            annotation.title = server.name
            annotation.coordinate = server.coordinate
            mapView.addAnnotation(annotation)
        }
    }

    // MARK: - MKMapViewDelegate
    func mapView(_ mapView: MKMapView, viewFor annotation: MKAnnotation) -> MKAnnotationView? {
        let view = MKPinAnnotationView(annotation: annotation, reuseIdentifier: nil)
        view.canShowCallout = true
        view.rightCalloutAccessoryView = UIButton(type: .detailDisclosure)
        return view
    }

    func mapView(_ mapView: MKMapView, annotationView view: MKAnnotationView, calloutAccessoryControlTapped control: UIControl) {
        guard let name = view.annotation?.title ?? nil else { return }
        let alert = UIAlertController(title: name, message: "Connect to \(name)?", preferredStyle: .alert)
        alert.addAction(UIAlertAction(title: "Connect", style: .default))
        alert.addAction(UIAlertAction(title: "Cancel", style: .cancel))
        present(alert, animated: true)
    }

    func mapViewDidFailLoadingMap(_ mapView: MKMapView, withError error: Error) {
        mapView.removeFromSuperview()
        showServerList()
    }

    // MARK: - Fallback list
    private func showServerList() {
        tableView.frame = view.bounds
        view.addSubview(tableView)
    }

    func tableView(_ tableView: UITableView, numberOfRowsInSection section: Int) -> Int {
        return servers.count
    }

    func tableView(_ tableView: UITableView, cellForRowAt indexPath: IndexPath) -> UITableViewCell {
        let cell = UITableViewCell(style: .default, reuseIdentifier: nil)
        cell.textLabel?.text = servers[indexPath.row].name
        return cell
    }

    func tableView(_ tableView: UITableView, didSelectRowAt indexPath: IndexPath) {
        let server = servers[indexPath.row]
        let alert = UIAlertController(title: server.name, message: "Connect to \(server.name)?", preferredStyle: .alert)
        alert.addAction(UIAlertAction(title: "Connect", style: .default))
        alert.addAction(UIAlertAction(title: "Cancel", style: .cancel))
        present(alert, animated: true)
    }
}

