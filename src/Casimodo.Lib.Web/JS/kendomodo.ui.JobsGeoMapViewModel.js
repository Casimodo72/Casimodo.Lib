"use strict";

var kendomodo;
(function (kendomodo) {
    (function (ui) {

        var JobsGeoMapViewModel = (function (_super) {
            casimodo.__extends(JobsGeoMapViewModel, _super);

            function JobsGeoMapViewModel(options) {
                _super.call(this, options);

                this.markers = [];
                this.selectionCircles = [];

                // Domain objects
                this.projects = [];

                // Filters
                this.currentDate = moment(new Date()).startOf("day").toDate();
                this.datePicker = null;

                this.createComponent();
            }

            var fn = JobsGeoMapViewModel.prototype;

            fn.refresh = function () {

                var self = this;

                return new Promise(function (resolve, reject) {
                    self.clear();
                })
                    .then(self.fetchJobs());
            };

            fn.clear = function () {
                this.clearSelection();

                // Clear markers
                this.removeItems(this.markers);
                this.markers = [];

                this.projects = [];
            };

            fn.clearSelection = function () {
                this.selectionCircles.forEach(function (x) { x.setMap(null); });
            };

            fn.removeItems = function (items) {
                if (!items || !items.length)
                    return;

                var self = this;

                items.forEach(function (x) {
                    x.setMap(null);

                    if (x.labels) {
                        self.removeItems(x.labels);
                        x.labels = [];
                    }

                });
            };

            fn.fetchProjects = function () {
                var self = this;
                casimodo.oDataQuery("/odata/Projects/Query()?$select=Number,Latitude,Longitude,Street,ZipCode&$expand=Contract($select=City)")
                    .then(function (items) {
                        setTimeout(function () {
                            self.addProjects(items);
                        }, 0);
                    });
            };

            fn.addProjects = function (projects) {
                for (var i = 0; i < projects.length; i++) {
                    var project = project[i];
                    this.addMarker({
                        lat: project.Latitude,
                        lng: project.Longitude,
                        title: project.Street
                    });
                }
            };

            fn.fetchJobs = function () {
                var self = this;

                var query = "/odata/JobDefinitions/Query()?$select=Id,ProjectId,StartDateOn&" +
                    "$expand=" +
                    "Person($select=FullName,Color)," +
                    "Project($select=Id,Latitude,Longitude,Street,ZipCode;" +
                    "$expand=" +
                    "Contract($select=City))";

                var date = casimodo.toODataUtcDateFilterString(this.currentDate);
                query += "&$filter=StartDateOn eq " + date; // + " and StartDateOn le " + date;

                //query += "&$orderby=StartDateOn desc";

                return casimodo.oDataQuery(query)
                    .then(function (items) {

                        self.addJobs(items);
                    });
            };

            fn.addJobs = function (allJobs) {

                var jobsByProject = _.toArray(_.groupBy(allJobs, function (x) { return x.ProjectId; }));

                for (var i = 0; i < jobsByProject.length; i++) {
                    var jobs = jobsByProject[i];

                    var firstJob = jobs[0];
                    var project = firstJob.Project;

                    this.projects.push(project);

                    var content = "", title = "";
                    for (var k = 0; k < jobs.length; k++) {
                        var person = jobs[k].Person;
                        title += person.FullName + ", ";
                        content += "<span class='person-name'>" + person.FullName + "</span>";
                        content += "<br/>";
                    }

                    title += project.Street + " " + project.Contract.City;
                    content += "<span class='project-location'>" + project.Street + " " + project.Contract.City + "</span>";

                    var options = {
                        lat: project.Latitude,
                        lng: project.Longitude,
                        title: title,
                        content: content
                    };

                    if (jobs.length === 1) {
                        options.color = firstJob.Person.Color;
                    }
                    else {
                        options.label = "M";
                    }

                    var marker = this.addMarker(options);
                    marker.dataProject = project;
                    marker.dataJobs = jobs;
                    marker.labels = [];
                }
            };

            fn.showDistances = function (marker) {

                var markers = this.markers;

                this.removeMarkerLabel(marker, "ProjectDistance");

                if (!marker.dataJobs)
                    return;

                var project = marker.dataProject;
                var location = new google.maps.LatLng(project.Latitude, project.Longitude);

                for (var i = 0; i < this.markers.length; i++) {

                    var curMarker = this.markers[i];

                    var curProject = curMarker.dataProject;

                    // Skip if selected project.
                    if (!curProject || project === curProject || project.Id === curProject.Id)
                        continue;

                    // Skip if the project shares the location of the selected project.
                    if (project.Latitude === curProject.Latitude && project.Longitude === curProject.Longitude) {
                        this.removeMarkerLabel(curMarker, "ProjectDistance");
                        continue;
                    }

                    var curLocation = new google.maps.LatLng(curProject.Latitude, curProject.Longitude);

                    // Point to poin distance in km.
                    var distance = google.maps.geometry.spherical.computeDistanceBetween(location, curLocation) / 1000;
                    var text = "" + distance.toFixed(1);

                    var label = this.findMarkerLabel(curMarker, "ProjectDistance");
                    if (!label) {
                        label = this.addLabel({
                            position: curLocation,
                            content: text
                        });
                        label.dataRole = "ProjectDistance";
                        curMarker.labels.push(label);
                    }
                    else {
                        label.set("position", curLocation);
                        label.set("text", text);
                    }

                    // Async
                    this._showDrivigDistanceAsync(marker, curMarker);
                }

                //var offset = google.maps.geometry.spherical.computeOffset(from:LatLng, distance:number, heading:number, radius?:number);
            };

            fn._showDrivigDistanceAsync = function (fromMarker, toMarker) {
                var self = this;

                this._getDrivingDistanceAsync(fromMarker.position, toMarker.position, function (data) {
                    var label = self.findMarkerLabel(toMarker, "ProjectDistance");
                    if (!label)
                        return;
                    var text = label.get("text") + "/" + (data.distance.value / 1000).toFixed(1) + " km (" + data.duration.text + ")";
                    label.set("text", text);
                });
            };

            fn._getDrivingDistanceAsync = function (from, to, callback) {
                // DRIVING, WALKING, BICYCLING, TRANSIT
                // Doc: https://developers.google.com/maps/documentation/javascript/directions#TravelModes
                // Example: https://developers.google.com/maps/documentation/javascript/examples/directions-travel-modes?hl=en

                var service = this.matrixService || (this.matrixService = new google.maps.DistanceMatrixService());

                service.getDistanceMatrix(
                    {
                        origins: [from],
                        destinations: [to],
                        travelMode: google.maps.TravelMode.DRIVING,
                        unitSystem: google.maps.UnitSystem.METRIC,
                        avoidHighways: false,
                        avoidTolls: false
                    }, function (response, status) {
                        var data = null;
                        var row = response.rows.length ? response.rows[0] : null;
                        if (row)
                            data = row.elements.length ? row.elements[0] : null;
                        callback(data);
                    });
            };

            fn.removeMarkerLabel = function (marker, role) {
                var label = this.findMarkerLabel(marker, role);
                if (!label)
                    return false;

                label.setMap(null);
                marker.labels.splice(marker.labels.indexOf(label), 1);

                return true;
            };

            fn.addMarker = function (options) {
                var self = this;

                var settings = {
                    map: this.map,
                    position: { lat: options.lat, lng: options.lng },
                    title: options.title,
                    label: options.label
                };

                if (options.color) {
                    var color = options.color.substring(1);

                    /* Source: http://stackoverflow.com/questions/7095574/google-maps-api-3-custom-marker-color-for-default-dot-marker/7686977#7686977 */
                    settings.icon = new google.maps.MarkerImage("http://chart.apis.google.com/chart?chst=d_map_pin_letter&chld=%E2%80%A2|" + color,
                        new google.maps.Size(21, 34),
                        new google.maps.Point(0, 0),
                        new google.maps.Point(10, 34));
                }

                var marker = new google.maps.Marker(settings);

                this.markers.push(marker);

                if (options.content) {
                    google.maps.event.addListener(marker, 'click', function () {

                        self.infoWindow.close();
                        self.infoWindow.setContent(options.content);
                        self.infoWindow.open(self.map, marker);

                        self.showSelection(marker);
                        self.showDistances(marker);
                    });
                }

                return marker;
            };

            fn.findMarkerLabel = function (marker, role) {
                if (!marker.labels || !marker.labels.length)
                    return;

                var label;
                for (var i = 0; i < marker.labels.length; i++) {
                    label = marker.labels[i];
                    if (label.dataRole === role)
                        return label;
                }

                return null;
            };

            fn.addLabel = function (options) {
                // Source: https://github.com/googlemaps/js-map-label
                var label = new MapLabel({
                    text: options.content,
                    position: options.position,
                    map: this.map,
                    fontSize: 14,
                    align: "left"
                });

                return label;
            };

            fn.showSelection = function (marker) {
                var self = this;
                var position = marker.getPosition();

                if (!this.selectionCircles.length) {
                    //setMap(null)

                    var circleOptions = {
                        strokeColor: "black",
                        strokeOpacity: 1,
                        strokeWeight: 1,
                        //fillColor: "yellow",
                        fillOpacity: 0,
                        map: this.map,
                        center: position, // or you can pass a google.maps.LatLng object
                        radius: 5 * 1000 // radius of the circle in metres
                    };

                    this.selectionCircles.push(new google.maps.Circle(circleOptions));

                    circleOptions.radius = 10 * 1000;
                    this.selectionCircles.push(new google.maps.Circle(circleOptions));
                }
                else {
                    this.selectionCircles.forEach(function (x) {
                        x.setCenter(position);
                        x.setMap(self.map);
                    });
                }
            };

            fn._initMap = function () {
                if (this._isMapInitialized)
                    return;

                this._initMapCore();
            };

            fn.onFilterChanged = function (e) {

                this.currentDate = moment(this.datePicker.value()).startOf("day").toDate();

                this.refresh();
            };

            fn.createComponent = function () {
                if (this._isComponentInitialized)
                    return;
                this._isComponentInitialized = true;

                var self = this;

                this.$view = $("#geo-map-view-" + this._options.id);

                this.$coordinatesDisplay = this.$view.find(".map-coordinates");
                this.$mapContainer = this.$view.find(".google-map");
                this.$searchInput = this.$view.find(".pac-input");

                this.datePicker = this.$view.find(".map-date-picker").kendoDatePicker({
                    value: new Date(),
                    change: function (e) {
                        self.onFilterChanged(e);
                    }
                }).data("kendoDatePicker");

                this._initMap();
            };

            return JobsGeoMapViewModel;

        })(kendomodo.ui.GeoMapViewModelBase);
        ui.JobsGeoMapViewModel = JobsGeoMapViewModel;

    })(kendomodo.ui || (kendomodo.ui = {}));
})(kendomodo || (kendomodo = {}));