using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System;

public class GPS_Stuff : MonoBehaviour
{

    [SerializeField]
    private char unit = 'K';

    public TMP_Text debugTxt;

    public GameObject targetSphere;
    private bool targetCreated = false;
    public bool gps_ok = false;
    float PI = Mathf.PI;

    GPSLoc startLoc = new GPSLoc();
    GPSLoc currLoc = new GPSLoc();

    bool measureDistance = false;


    // Start is called before the first frame update
    IEnumerator Start()
    {
        // Check if the user has location service enabled.
        if (!Input.location.isEnabledByUser)
        {
            Debug.Log("Location not enabled on device or app does not have permission to access location");
            debugTxt.text = "Location not enabled on device or app does not have permission to access location";
        }
        // Starts the location service.
        Input.location.Start();

        // Waits until the location service initializes
        int maxWait = 20;
        while (Input.location.status == LocationServiceStatus.Initializing && maxWait > 0)
        {
            yield return new WaitForSeconds(1);
            maxWait--;
        }

        // If the service didn't initialize in 20 seconds this cancels location service use.
        if (maxWait < 1)
        {
            Debug.Log("Timed out");
            debugTxt.text += "\nTimed Out";
            yield break;
        }

        // If the connection failed this cancels location service use.
        if (Input.location.status == LocationServiceStatus.Failed)
        {
            Debug.LogError("Unable to determine device location");
            debugTxt.text += ("\nUnable to determine device location");

            yield break;
        }
        else
        {
            // If the connection succeeded, this retrieves the device's current location and displays it in the Console window.
            Debug.Log("Location: " + Input.location.lastData.latitude + " " + Input.location.lastData.longitude + " " + Input.location.lastData.altitude + " " + Input.location.lastData.horizontalAccuracy + " " + Input.location.lastData.timestamp);
            debugTxt.text
               = "\nLocation: \nLat: " + Input.location.lastData.latitude
                + " \nLon: " + Input.location.lastData.longitude
                + " \nAlt: " + Input.location.lastData.altitude
                + " \nH_Acc: " + Input.location.lastData.horizontalAccuracy
                + " \nTime: " + Input.location.lastData.timestamp;

            gps_ok = true;

        }


    }

    // Update is called once per frame
    void Update()
    {
        if (gps_ok)
        {
            debugTxt.text = "GPS:...";

            // debugTxt.text
            //     = "\nLocation: \nLat: " + Input.location.lastData.latitude
            //     + " \nLon: " + Input.location.lastData.longitude
            //     + " \nH_Acc: " + Input.location.lastData.horizontalAccuracy;



//!!!! TURN THESE BACK ON BEFORE DEPLOYING!!!!!!
            // currLoc.lat = Input.location.lastData.latitude;
            // currLoc.lon = Input.location.lastData.longitude;


            // debugTxt.text += "\nStored: " + startLoc.getLocData();

            // if (measureDistance == true)
            // {
            //     double distanceBetween = distance((double)currLoc.lat, (double)currLoc.lon, (double)startLoc.lat, (double)startLoc.lon, 'K');

            //     debugTxt.text += "\nDistance: " + distanceBetween;
            // }

            // 41.582661, -93.699536

            double distanceBetween = distance((double)currLoc.lat, (double)currLoc.lon, (double)41.582661, (double)-93.699536, 'K');

            debugTxt.text += "\nDistance: " + distanceBetween;
            if(distanceBetween < 0.01)
            {
                if (targetCreated == false)
                {
                    //create target sphere
                    targetCreated = true;
                    Instantiate(targetSphere, new Vector3(), Quaternion.identity);

                }
                debugTxt.text += "Found object 1";
            }
        }
    }

    public void StopGPS()
    {
        Input.location.Stop();

    }

    public void StoreCurrentGPS()
    {
        startLoc = new GPSLoc(currLoc.lon, currLoc.lat);
        measureDistance = true;
    }

    //https://www.geodatasource.com/resources/tutorials/how-to-calculate-the-distance-between-2-locations-using-c/
    private double distance(double lat1, double lon1, double lat2, double lon2, char unit)
    {
        if ((lat1 == lat2) && (lon1 == lon2))
        {
            return 0;
        }
        else
        {
            double theta = lon1 - lon2;
            double dist = Math.Sin(deg2rad(lat1)) * Math.Sin(deg2rad(lat2)) + Math.Cos(deg2rad(lat1)) * Math.Cos(deg2rad(lat2)) * Math.Cos(deg2rad(theta));
            dist = Math.Acos(dist);
            dist = rad2deg(dist);
            dist = dist * 60 * 1.1515;
            if (unit == 'K')
            {
                dist = dist * 1.609344;
            }
            else if (unit == 'N')
            {
                dist = dist * 0.8684;
            }
            return (dist);
        }
    }

    //:::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
    //::  This function converts decimal degrees to radians             :::
    //:::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
    private double deg2rad(double deg)
    {
        return (deg * Math.PI / 180.0);
    }

    //:::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
    //::  This function converts radians to decimal degrees             :::
    //:::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
    private double rad2deg(double rad)
    {
        return (rad / Math.PI * 180.0);
    }

//Methods for Calculating vectors between GPS points!!!!
    public static Vector3 GPSToMapCoords(float latitude, float longitude, float altitude = 0f)
    {
        float earthRadius = 6371000f; // Earth's radius in meters
        float latRad = Mathf.Deg2Rad * latitude;
        float longRad = Mathf.Deg2Rad * longitude;

        // Simplified conversion for x, y (altitude) and z
        float x = earthRadius * longRad;
        float y = altitude; // Assuming altitude directly maps to Unity's Y
        float z = earthRadius * Mathf.Log(Mathf.Tan(Mathf.PI / 4f + latRad / 2f)); // Mercator-like Y for Z in Unity

        return new Vector3(x, y, z);
    }
        
        public Vector3 CalculateVectorBetweenGPS(Vector3 startGPSPoint, Vector3 endGPSPoint)
    {
        // Convert GPS points to Unity Vector3 using your chosen projection
        Vector3 startUnityPoint = GPSToMapCoords(startGPSPoint.x, startGPSPoint.y, startGPSPoint.z); // Assuming x=lat, y=lon, z=alt
        Vector3 endUnityPoint = GPSToMapCoords(endGPSPoint.x, endGPSPoint.y, endGPSPoint.z);

        // Calculate the vector
        Vector3 vectorBetweenPoints = endUnityPoint - startUnityPoint;
        return vectorBetweenPoints;
    }

}


public class GPSLoc
{
    public float lon;
    public float lat;

    public GPSLoc()
    {
        lon = 0;
        lat = 0;
    }
    public GPSLoc(float lon, float lat)
    {
        this.lon = lon;
        this.lat = lat;
    }

    public string getLocData()
    {
        return "Lat: " + lat + " \nLon: " + lon;
    }
}