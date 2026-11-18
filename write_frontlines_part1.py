# Write the FrontLinesMission class file
# Run with: python write_frontlines.py

# Part 1: Class declaration and LoadFrontPointsFromMisFile
with open(r"E:\Cliffs-of-Dover\TWC-campaigns-github\TWC_Mission_Server\Genghis\Genghis-Class-FrontLinesMission.cs", "w", encoding="utf-8") as f:
    f.write("""//$reference parts/core/gamePlay.dll
//$reference parts/core/core.dll

public class FrontLinesMission
{
    private Mission mission;

    public FrontLinesMission(Mission missionRef)
    {
        mission = missionRef;
    }

    public List<List<Point2d>> LoadFrontPointsFromMisFile(string misFilePath)
    {
        List<List<Point2d>> result = new List<List<Point2d>>();
        try
        {
            if (string.IsNullOrEmpty(misFilePath)) return result;

            string fullPath = mission.CLOD_PATH + mission.FILE_PATH + misFilePath;
            ISectionFile f = GamePlay.gpLoadSectionFile(fullPath);
            if (f == null) return result;
            if (!f.exist("FrontMarker")) return result;

            int num = f.lines("FrontMarker");
            List<Point2d> redPoints = new List<Point2d>();
            List<Point2d> bluePoints = new List<Point2d>();

            string key = "";
            string value = "";
            for (int i = 0; i < num; i++)
            {
                f.get("FrontMarker", i, out key, out value);
                string[] parts = value.Trim().Split(null);
                if (parts.Length < 3) continue;

                double x = 0;
                double y = 0;
                int army = 0;
                if (!double.TryParse(parts[0], out x)) continue;
                if (!double.TryParse(parts[1], out y)) continue;
                if (!int.TryParse(parts[2], out army)) continue;

                Point2d p = new Point2d(x, y);
                if (army == 1) { redPoints.Add(p); }
                else if (army == 2) { bluePoints.Add(p); }
            }

            int count = redPoints.Count;
            if (bluePoints.Count < count) count = bluePoints.Count;
            for (int i = 0; i < count; i++)
            {
                List<Point2d> pair = new List<Point2d>();
                pair.Add(redPoints[i]);
                pair.Add(bluePoints[i]);
                result.Add(pair);
            }
        }
        catch (System.Exception ex)
        {
            Console.WriteLine("LoadFrontPointsFromMisFile failed: " + ex.Message);
            return new List<List<Point2d>>();
        }
        return result;
    }

    public ISectionFile DrawFrontLinesPerMapState(double minMapState_ext = -25, double maxMapState_ext = 25, double? currMapState = null, string saveName = "frontfile.mis", bool loadSectionFile = true, ISectionFile f2 = null, bool test = false, double campaign_offset_ext = 0, bool fast = false)
    {
        try
        {
""")

print("Part 1 written")