# Server Map Demo

This repository contains reference implementations for displaying up to ten
server locations for Europe and the United States on mobile platforms.

- **Android** uses the Google Maps SDK to plot server markers. Selecting a
  marker reveals the server name and a Connect button. If the map cannot be
  displayed, a fallback list of servers is shown instead.
- **iOS** uses MapKit with similar functionality and the same fallback list
  when the map fails to load.

Both examples include a hard-coded list of ten server locations and basic
connect prompts. Networking logic for actual connections should be added as
needed.
