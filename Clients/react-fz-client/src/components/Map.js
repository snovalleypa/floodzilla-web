import React, { useContext, useEffect, useState, useRef } from "react";
import GoogleMapReact from "google-map-react";
import getMapIcon from "../lib/getMapIcon";
import { GageDataContext } from "./GageDataContext";

export default function Map({
  gageList,
  gageStatusList,
  gageSelected,
  viewGageDetails,
  isMobile,
}) {
  const [google, setGoogle] = useState();
  const [mapBounds, setMapBounds] = useState();
  const mapMarkers = useRef([]);
  const gageData = useContext(GageDataContext);

  useEffect(() => {
    if (google && !gageList) {
      clearMarkers(mapMarkers.current);
      return;
    }
    if (google && gageList && gageStatusList) {
      // remove filtered out markers
      const gageIds = gageList.map(g => g.id);
      for (var i = 0; i < mapMarkers.current.length; i++) {
        if (!gageIds.includes(mapMarkers.current[i].id)) {
          mapMarkers.current[i].setMap(null);
          mapMarkers.current.splice(i, 1);
          i--;
        }
      }
      const bounds = new google.maps.LatLngBounds();
      for (const gage of gageList) {
        if (gage.latitude && gage.longitude) {
          let marker =
            mapMarkers.current &&
            mapMarkers.current.find(m => m.id === gage.id);

          marker = createOrUpdateGageMarker(gage, marker);

          marker.setMap(google.map);
          mapMarkers.current.push(marker);
          bounds.extend(marker.getPosition());

          google.maps.event.addListener(marker, "click", function() {
            viewGageDetails(this.gage);
          });
        }
      }
      setMapBounds(bounds);
    }
  }, [google, gageList, viewGageDetails]);  // eslint-disable-line react-hooks/exhaustive-deps

  useEffect(() => {
    if (
      google &&
      gageSelected &&
      gageSelected.latitude &&
      gageSelected.longitude
    ) {
      google.map.panTo({
        lat: gageSelected.latitude,
        lng: gageSelected.longitude,
      });
      google.map.setZoom(16);
    }
    if (google && !gageSelected && mapBounds) {
      google.map.fitBounds(mapBounds);
      google.maps.event.trigger(google.map, "resize");
    }
  }, [gageSelected, google, mapBounds]);

  const createOrUpdateGageMarker = (gage, marker) => {
    let gageStatus = gageData.getGageStatus(gage.id);
    var icon = {
      url:
        "data:image/svg+xml;charset=UTF-8;base64," +
        btoa(
          getMapIcon(gage.isCurrentlyOffline, gageStatus.currentStatus.waterStatus, gageStatus.currentStatus.floodLevelIndicator)
        ),
      //url: gage.mapIcon, // url
      scaledSize: new google.maps.Size(54, 60), // scaled size
      origin: new google.maps.Point(0, 0), // origin
      anchor: new google.maps.Point(27, 50), // anchor
    };
    if (marker) {
      marker.setIcon(icon);
      return marker;
    }
    return new google.maps.Marker({
      position: { lat: gage.latitude, lng: gage.longitude },
      //map: google.map,
      title: gage.locationName,
      id: gage.id,
      gage: gage,
      icon: icon,
    });
  };

  const clearMarkers = function(mapMarkers) {
    let marker = mapMarkers.pop();
    while (marker) {
      marker.setMap(null);
      marker = mapMarkers.pop();
    }
  };

  const getMapOptions = maps => {
    return {
      streetViewControl: false,
      scaleControl: true,
      //fullscreenControl: true,
      disableDoubleClickZoom: true,
      maxZoom: 18,
      mapTypeControl: true,
      mapTypeId: maps.MapTypeId.HYBRID,
      mapTypeControlOptions: {
        style: maps.MapTypeControlStyle.HORIZONTAL_BAR,
        position: maps.ControlPosition.BOTTOM_CENTER,
        mapTypeIds: [maps.MapTypeId.ROADMAP, maps.MapTypeId.HYBRID],
      },
      zoomControl: true,
      clickableIcons: false,
      gestureHandling: isMobile ? "cooperative" : "auto",
    };
  };
  return (
    <GoogleMapReact
      bootstrapURLKeys={{ key: window.GoogleMapsApiKey }}
      zoom={4}
      center={{ lat: 47.622403, lng: -121.933723 }}
      options={getMapOptions}
      yesIWantToUseGoogleMapApiInternals
      onGoogleApiLoaded={({ map, maps }) => {
        setGoogle({ map, maps });
      }}
    ></GoogleMapReact>
  );
}
