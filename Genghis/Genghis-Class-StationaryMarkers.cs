//#define DEBUG
//#define TRACE
//$reference parts/core/gamePlay.dll
//$reference System.Core.dll

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using maddox.game;
using maddox.game.world;
using part;

public class StationaryMarker
{
    public double X { get; set; }
    public double Y { get; set; }
    public string Type { get; set; }
    public double Radius { get; set; }
    public string SourceFile { get; set; }

    public StationaryMarker(double x, double y, string type, double radius, string sourceFile)
    {
        X = x;
        Y = y;
        Type = type;
        Radius = radius;
        SourceFile = sourceFile;
    }

    public double DistanceTo(double x, double y)
    {
        double dx = X - x;
        double dy = Y - y;
        return Math.Sqrt(dx * dx + dy * dy);
    }
}

public static class StationaryMarkerManager
{
    private static ConcurrentDictionary<string, List<StationaryMarker>> _markers = 
        new ConcurrentDictionary<string, List<StationaryMarker>>();

    private static readonly Dictionary<string, double> _markerRadii = new Dictionary<string, double>
    {
        { "JerryCan_GER1_1", 71 },
        { "JerryCan_GER1_2", 141 },
        { "JerryCan_GER1_3", 282 },
        { "JerryCan_GER1_5", 1410 },
        { "AEC_Regent_II", 500 },
        { "MG_TA", 250 }
    };

    static StationaryMarkerManager()
    {
        foreach (KeyValuePair<string, double> kvp in _markerRadii)
        {
            _markers[kvp.Key] = new List<StationaryMarker>();
        }
    }

    public static void AddMarker(string type, double x, double y, string sourceFile)
    {
        double radius = -1;
        foreach (KeyValuePair<string, double> kvp in _markerRadii) {
            if (! kvp.Key.ToLower().Contains(type.ToLower())) continue;
            radius = kvp.Value;
            break;
        }
        
        if (radius == -1)
        {
            Console.WriteLine("[StationaryMarkerManager] Unknown marker type: " + type);
            return;
        }

        StationaryMarker marker = new StationaryMarker(x, y, type, radius, sourceFile);
        _markers.AddOrUpdate(type, 
            new List<StationaryMarker> { marker },
            (key, existing) => { existing.Add(marker); return existing; });
    }

    public static List<StationaryMarker> FindNearbyMarkers(double x, double y, double searchRadius)
    {
        List<StationaryMarker> results = new List<StationaryMarker>();
        
        foreach (KeyValuePair<string, List<StationaryMarker>> kvp in _markers)
        {
            string type = kvp.Key;
            double markerRadius = _markerRadii[type];
            double effectiveRadius = searchRadius + markerRadius;
            
            foreach (StationaryMarker marker in kvp.Value)
            {
                if (marker.DistanceTo(x, y) <= effectiveRadius)
                {
                    results.Add(marker);
                }
            }
        }
        
        return results;
    }

    public static List<StationaryMarker> FindNearbyMarkersOfTypes(double x, double y, double searchRadius, params string[] types)
    {
        List<StationaryMarker> results = new List<StationaryMarker>();
        
        foreach (string type in types)
        {
            List<StationaryMarker> list;
            if (!_markers.TryGetValue(type, out list)) continue;
            
            double markerRadius = _markerRadii[type];
            double effectiveRadius = searchRadius + markerRadius;
            
            foreach (StationaryMarker marker in list)
            {
                if (marker.DistanceTo(x, y) <= effectiveRadius)
                {
                    results.Add(marker);
                }
            }
        }
        
        return results;
    }

    public static List<StationaryMarker> GetMarkersOfType(string type)
    {
        List<StationaryMarker> list;
        _markers.TryGetValue(type, out list);
        return list ?? new List<StationaryMarker>();
    }

    public static int GetTotalCount()
    {
        int count = 0;
        foreach (KeyValuePair<string, List<StationaryMarker>> kvp in _markers)
        {
            count += kvp.Value.Count;
        }
        return count;
    }

    public static void Clear()
    {
        foreach (KeyValuePair<string, List<StationaryMarker>> kvp in _markers)
        {
            kvp.Value.Clear();
        }
    }

    public static void DebugPrintAll()
    {
        Console.WriteLine("[StationaryMarkerManager] Total markers: " + GetTotalCount());
        foreach (KeyValuePair<string, List<StationaryMarker>> kvp in _markers)
        {
            if (kvp.Value.Count > 0)
            {
                Console.WriteLine("  " + kvp.Key + ": " + kvp.Value.Count);
                foreach (StationaryMarker m in kvp.Value)
                {
                    Console.WriteLine("    (" + m.X + ", " + m.Y + ") r=" + m.Radius + " from " + m.SourceFile);
                }
            }
        }
    }

    public static string ExtractShortTypeName(string fullTypeName)
    {
        int lastDot = fullTypeName.LastIndexOf('.');
        if (lastDot >= 0 && lastDot < fullTypeName.Length - 1)
        {
            return fullTypeName.Substring(lastDot + 1);
        }
        return fullTypeName;
    }
}