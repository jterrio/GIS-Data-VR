using System.Collections;
using System.Collections.Generic;
using System.IO;
using System;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.UI;
using System.Diagnostics;

public class GISData : GISDefinitions {


    [Header("File Setting")]
    public string fileName;
    public string fileType;
    protected string path;
    public int maxPoints; //max points to generate from file; leave at 0 for no max
    
    public Header header;
    public bool useGlobalValues = false;
    public Vector3 customMin;
    public Vector3 customMax;

    [Header("Point Settings")] 
    public GameObject point;
    public int points;
    protected BinaryReader br;
    public GameObject holderObject;

    [Header("Octree Settings")]
    public Octree octree;
    private Vector3 min, max, origin;
    public int pointsToWritePerBlock = 1000;
    private bool finishedCreatingBin = false;

    [Header("User Settings")]
    public GameObject player;
    public Vector3 lastCoordinatePosition;
    public int viewDistance = 2;
    public bool readyToRender = false;
    public bool makeBin = false;
    public float pointSize = 0.05f;
    public bool renderGizmos = true;

    private float fps;
    private float timeToCompleteFrame;
    private List<GameObject> gameObjectPoints = new List<GameObject>();
    private float percentage = 0f;
    private List<Vector3> positionsToDraw = new List<Vector3>();
    private Vector3 debugPoint;
    private Vector3 globalOrigin;
    private Vector3 globalOffset;
    private bool renderAllPoints = false;

    private void OnDrawGizmos() {
        if (!renderGizmos) {
            return;
        }
        foreach (GameObject g in gameObjectPoints) {
            Vector3 coordinate = octree.GetRoot().FindCoordinateOnOctree(g.transform.position - globalOffset);
            Octree.OctreeNode oc = octree.GetRoot().GetNodeAtCoordinate(coordinate);
            int distance = Distance(lastCoordinatePosition, coordinate);
            if (distance == 0) {
                Gizmos.color = new Color(1, 0, 0, 1f);
            } else if (distance == 1) {
                Gizmos.color = new Color(0, 1, 0, 1f);
            } else {
                Gizmos.color = new Color(0, 0, 1, 1f);
            }
            Gizmos.DrawWireCube(oc.Position + globalOffset, new Vector3(octree.smallestTile, octree.smallestTile, octree.smallestTile));

        }
        //Gizmos.DrawWireCube(origin, max - min);
    }


    // Use this for initialization
    void Start() {
        path = (Application.streamingAssetsPath + "/" + fileName + "." + fileType);

        header.fileSignature = new char[4];
        header.projectID4 = new char[8];
        header.systemIdentifier = new char[32];
        header.generatingSoftware = new char[32];
        header.legacyNumberOfPointsByReturn = new uint[5];
        header.numberOfPointsByReturn = new ulong[15];
        br = new BinaryReader(File.Open(path, FileMode.Open));

        ReadHeader();
        SetOctreeBase();
    }


    void Update() {
        if (!finishedCreatingBin) { //wait until we finish generation
            return;
        }
        if (readyToRender) { //only render when we want to/need
            DrawPoints();
        }
    }

    /// <summary>
    /// Render all of the points
    /// </summary>
    void DrawPoints() {
      
        

        //return;
        Vector3 coordinate = octree.GetRoot().FindCoordinateOnOctree(player.transform.position - globalOffset);
        FileStream fs;
        Stopwatch stopwatch = new Stopwatch();
        stopwatch.Start();
        int sizeOfPoint = GetSizeOfPoint(header.versionMajor, header.versionMinor, header.pointDataRecordFormat);

        positionsToDraw.Clear();
        lastCoordinatePosition = coordinate;
        positionsToDraw.Add(coordinate);

        AddFOV();

        int totalPointsRendered = 0;
        int totalPointsInBin = 0;

        /*
        bool needContinue = true;
        while (needContinue) {
            needContinue = false;
            foreach (GameObject other in new List<GameObject>(gameObjectPoints)) {
                foreach (GameObject p in new List<GameObject>(gameObjectPoints)) {
                    if (other.name == p.name || p.name.ToCharArray()[0] == '-' && other != p) {
                        gameObjectPoints.Remove(p);
                        Destroy(p);
                        needContinue = true;
                        break;
                    }
                }
            }
        }*/


        //Remove duplicates from those that are already drawn
        foreach(Vector3 position in new List<Vector3>(positionsToDraw)) {
            String positionComp = GetRealPosition(position).ToString() + "-" + Distance(lastCoordinatePosition, position).ToString() + "-" + position.x + "-" + position.y + "-" + position.z;
            foreach(GameObject g in gameObjectPoints) {
                if(g.name == positionComp) {
                    positionsToDraw.Remove(position);
                }
            }
        }

        //Remove objects in scene which cannot be viewed
        foreach(GameObject g in new List<GameObject>(gameObjectPoints)) {
            if (!IsVisible(g.transform.position) && lastCoordinatePosition != octree.GetRoot().FindCoordinateOnOctree(g.transform.position-globalOffset)) {
                gameObjectPoints.Remove(g);
                Destroy(g);
            }
        }

        foreach (Vector3 position in new List<Vector3>(positionsToDraw)) {
            totalPointsRendered += RenderVector(position, sizeOfPoint);
        }


        stopwatch.Stop();
        //print("Total points rendered: " + totalPointsRendered);
        //print("Total points in bin(s): " + totalPointsInBin);
        //print("Time to render points (in milliseconds): " + stopwatch.ElapsedMilliseconds);

        timeToCompleteFrame = stopwatch.ElapsedMilliseconds;
        fps = 1 / Time.deltaTime;
    }

    int RenderVector(Vector3 position, int sizeOfPoint) {
        FileStream fs;
        BinaryReader br_pos = new BinaryReader(fs = File.OpenRead((Application.streamingAssetsPath + "/" + fileName + "/" + fileName + "-0" + "/" + fileName + "-0" + ".bin")));
        int pointsInBlock;
        int distance = Distance(lastCoordinatePosition, position);
        Int64 realPos = GetRealPosition(position);

        Vector3 objectPos = octree.GetRoot().GetNodeAtCoordinate(position).Position;
        if (useGlobalValues) {
            objectPos += globalOffset;
        }

        if (distance == 0) {
            pointsInBlock = pointsToWritePerBlock;
        } else if (distance >= 1) {
            pointsInBlock = Mathf.FloorToInt(pointsToWritePerBlock / 8);
        } else {
            pointsInBlock = Mathf.FloorToInt(pointsToWritePerBlock / 64);
        }


        GameObject p = Instantiate(holderObject);

        p.name = realPos.ToString() + "-" + distance.ToString() + "-" + position.x + "-" + position.y + "-" + position.z;

        Int64 a = realPos * (Int64)(sizeOfPoint * pointsToWritePerBlock);
        if (a >= br_pos.BaseStream.Length || a < 0) {
            gameObjectPoints.Remove(p);
            Destroy(p);
            return 0;
        }

        p.transform.position = objectPos;
        gameObjectPoints.Add(p);
        p.SetActive(true);

        br_pos.BaseStream.Position = a;
        int numberOfPoints = br_pos.ReadInt32();
        if (numberOfPoints <= 0) {
            gameObjectPoints.Remove(p);
            Destroy(p);
            return 0;
        }

        Mesh m = new Mesh();

        List<Vector3> pointsForMesh = new List<Vector3>();
        List<int> indecies = new List<int>();
        List<Color> colors = new List<Color>();

        int indeciesValue = -1;
        for (int i = 0; i < Mathf.Min(numberOfPoints, pointsInBlock); i++) {
            double x = br_pos.ReadDouble();
            double y = br_pos.ReadDouble();
            double z = br_pos.ReadDouble();
            byte b = br_pos.ReadByte();
            int colorInt = GetIntFromByte(b);
            if(colorInt <= 1 && !renderAllPoints) {
                continue;
            }
            indeciesValue++;
            br_pos.BaseStream.Position = (a + (((i) * sizeOfPoint)) + sizeof(int));
            Vector3 realCoor = new Vector3((float)x, (float)y, (float)z);
            pointsForMesh.Add(Normalize(p.transform.position, Normalize(origin, realCoor)) + globalOffset);
            colors.Add(GetColorFromByte(colorInt));
            indecies.Add(indeciesValue);
        }
        m.vertices = pointsForMesh.ToArray();
        m.SetIndices(indecies.ToArray(), MeshTopology.Points, 0);
        m.colors = colors.ToArray();
        p.GetComponent<MeshFilter>().mesh = m;
        p.GetComponent<MeshRenderer>().sharedMaterial.SetFloat("_PointSize", pointSize);
        br_pos.Close();
        fs.Close();
        return Mathf.Min(numberOfPoints, pointsInBlock);
    }

    /// <summary>
    /// Calculates infinity distance and returns a number to be used based on that
    /// </summary>
    /// <param name="home"></param>
    /// <param name="away"></param>
    /// <returns>LoD number</returns>
    int Distance(Vector3 home, Vector3 away) {
        int x = (int)Mathf.Abs(home.x - away.x);
        int y = (int)Mathf.Abs(home.y - away.y);
        int z = (int)Mathf.Abs(home.z - away.z);

        int toReturn = (int)(Mathf.Max(x, y, z)) - 1;   
        if(toReturn <= viewDistance / 4) {
            return 0;
        }else if(toReturn <= viewDistance / 2) {
            return 1;
        } else {
            return 2;
        }
    }

    int GetIntFromByte(Byte b){
        return Convert.ToInt32(b);
    }

    /// <summary>
    /// Gets colors from byte, representing the classification
    /// </summary>
    /// <param name="b"></param>
    /// <returns></returns>
    Color GetColorFromByte(int c) {
        Color toReturn = new Color();
        switch (c) {
            case 0: //created, never classified
                toReturn = Color.white;
                break;
            case 1: //unclassified
                toReturn = Color.white;
                break;
            case 2: //ground
                toReturn = new Color(0.396f, 0.263f, 0.129f); //brown
                break;
            case 3: //low vegetation
                toReturn = new Color(0.78f, 0.918f, 0.275f); //lime
                break;
            case 4: //medium vegetation
                toReturn = Color.green;
                break;
            case 5: //high vegetation
                toReturn = new Color(0.043f, 0.4f, 0.137f); //forest green
                break;
            case 6: //building
                toReturn = Color.cyan;
                break;
            case 7: //low point (noise)
                toReturn = Color.red;
                break;
            case 8: //model key-point (mass point)
                toReturn = Color.yellow;
                break;
            case 9: //water
                toReturn = Color.blue;
                break;
            case 10: //RESERVED
                toReturn = new Color(1, 0.647f, 0); //orange
                break;
            case 11: //RESERVED
                toReturn = new Color(1, 0.647f, 0); //orange
                break;
            case 12: //overlap points
                toReturn = toReturn = new Color(1, 0.753f, 0.796f); //pink
                break;
            default: //RESERVED
                toReturn = Color.white;
                break;
        }

        return toReturn;
    }

    /// <summary>
    /// Adds nodes that are visible and within range to be drawn
    /// </summary>
    void AddFOV() {
        //Vector3 cameraDirection = octree.GetRoot().GetNodeAtCoordinate(lastCoordinatePosition).Position + Camera.main.gameObject.transform.forward * octree.smallestTile * viewDistance;
        int x = (int)lastCoordinatePosition.x + viewDistance;
        int y = (int)lastCoordinatePosition.y + viewDistance;
        int z = (int)lastCoordinatePosition.z + viewDistance;

        while (z >= ((int)lastCoordinatePosition.z - viewDistance)) {
            while (y >= ((int)lastCoordinatePosition.y - viewDistance)) {
                while (x >= ((int)lastCoordinatePosition.x - viewDistance)) {
                    if (x >= 0 && y >= 0 && z >= 0) {
                        if ((IsVisible(octree.GetRoot().GetNodeAtCoordinate(new Vector3(x, y, z)).Position + globalOffset) && !positionsToDraw.Contains(new Vector3(x, y, z)))) {
                            positionsToDraw.Add(new Vector3(x, y, z));
                        }
                    }
                    x--;
                }
                y--;
                x = ((int)lastCoordinatePosition.x + viewDistance);
            }
            z--;
            y = ((int)lastCoordinatePosition.y + viewDistance);
        }

    }


    /// <summary>
    /// Checks if the node's center is within the camera's frustum +/- some distance
    /// </summary>
    /// <param name="point"></param>
    /// <returns>True if visible</returns>
    bool IsVisible(Vector3 point) {
        bool isVisible = false;
        Vector3 cameraPoint = Camera.main.WorldToViewportPoint(point);
        if ((cameraPoint.x >= -4 && cameraPoint.x <= 5) && (cameraPoint.y >= -4 && cameraPoint.y <= 5) && (cameraPoint.z >= -3)) {
            isVisible = true;
        }
        return isVisible;
    }



    /// <summary>
    /// Takes a Vector3 coordinate in the Octree and converts into a position via bit-interleaving
    /// </summary>
    /// <param name="coordinate"></param>
    /// <returns>Position in file</returns>
    Int64 GetRealPosition(Vector3 coordinate) {


        char[] xBits = Convert.ToString((int)coordinate.x, 2).ToCharArray();
        char[] yBits = Convert.ToString((int)coordinate.y, 2).ToCharArray();
        char[] zBits = Convert.ToString((int)coordinate.z, 2).ToCharArray();
        char[] bitPos = new char[Mathf.Max(xBits.Length, yBits.Length, zBits.Length) * 3];
        int currentPos = 0;
        for (int b = 0; b < Mathf.Max(xBits.Length, yBits.Length, zBits.Length); b++) {
            if (b > xBits.Length - 1) {
                bitPos[currentPos] = '0';
            } else {
                bitPos[currentPos] = xBits[xBits.Length - (b + 1)];
            }
            currentPos += 1;
            if (b > yBits.Length - 1) {
                bitPos[currentPos] = '0';
            } else {
                bitPos[currentPos] = yBits[yBits.Length - (b + 1)];
            }
            currentPos += 1;
            if (b > zBits.Length - 1) {
                bitPos[currentPos] = '0';
            } else {
                bitPos[currentPos] = zBits[zBits.Length - (b + 1)];
            }
            currentPos += 1;
        }
        Array.Reverse(bitPos);
        string actualPos = new string(bitPos);
        Int64 realPos = Convert.ToInt64(actualPos, 2);
        return realPos;
    }


    /// <summary>
    /// Initializes the base of the Octree
    /// </summary>
    void SetOctreeBase() {
        Vector3 oMin = new Vector3((float)header.xMin, (float)header.zMin, (float)header.yMin);
        Vector3 oMax = new Vector3((float)header.xMax, (float)header.zMax, (float)header.yMax);
        Vector3 origin = new Vector3((oMax.x + oMin.x) / 2, (oMax.y + oMin.y) / 2, (oMax.z + oMin.z) / 2);
        Vector3 normalMin = Normalize(origin, oMin);
        Vector3 normalMax = Normalize(origin, oMax);
        float[] ranges = new float[] { (float)(normalMax.x - normalMin.x), (float)(normalMax.y - normalMin.y), (float)(normalMax.z - normalMin.z) };
        octree = new Octree(Vector3.zero, Mathf.Max(ranges), pointsToWritePerBlock);
        //player.transform.position = normalMax - normalMin;
        print("Init Size: " + octree.SmallestTile);
    }


    /// <summary>
    /// Shuffles the list to randomize when points are written for LoD
    /// </summary>
    /// <param name="toShuffle"></param>
    /// <returns>Shuffled List</returns>
    List<PointData> ShuffleList(List<PointData> toShuffle) {
        List<PointData> toReturn = new List<PointData>();
        int count = toShuffle.Count;
        for(int i = 0; i < count; i++) {
            int randValue = UnityEngine.Random.Range(0, toShuffle.Count - 1);
            toReturn.Add(toShuffle[randValue]);
            toShuffle.RemoveAt(randValue);
        }
        return toReturn;
    } 


    /// <summary>
    /// Writes the sorted/converted file
    /// </summary>
    /// <returns></returns>
    IEnumerator WriteToBin() {
        BinaryReader br_pos;
        BinaryWriter bw;
        float tileSize = octree.SmallestTile;
        Vector3 tilePos = Normalize(origin, min);
        int sizeOfPoint = GetSizeOfPoint(header.versionMajor, header.versionMinor, header.pointDataRecordFormat);
        var folder = Directory.CreateDirectory(Application.streamingAssetsPath + "/" + fileName);

        //AT MAX DEPTH
        folder = Directory.CreateDirectory(Application.streamingAssetsPath + "/" + fileName + "/" + fileName + "-0");
        FileStream fs = File.Create(Application.streamingAssetsPath + "/" + fileName + "/" + fileName + "-0" + "/" + fileName + "-0" + ".bin");
        PointData pd;
        Vector3 normalMin = Normalize(origin, min);
        Vector3 normalMax = Normalize(origin, max);
        br.BaseStream.Position = (int)header.offsetToPointData;
        fs.Close();

        //CREATE FILE WITH SOME N LENGTH
        bw = new BinaryWriter(fs = File.OpenWrite((Application.streamingAssetsPath + "/" + fileName + "/" + fileName + "-0" + "/" + fileName + "-0" + ".bin")));
        bw.BaseStream.Position = (octree.currentLeaves * (Int64)(sizeOfPoint * pointsToWritePerBlock));
        bw.Write(0);
        bw.Close();
        fs.Close();
        //yield return null;

        int numberOfPointsRead = 0;

        int totalSizeOfDic = 0;
        Dictionary<Int64, List<PointData>> pointsToWrite = new Dictionary<Int64, List<PointData>>();

        //WRITE AND CREATE A FILE AT DEPTH = MAX
        while (numberOfPointsRead < (header.legacyNumberOfPointRecords)) { //(header.legacyNumberOfPointRecords - 1)
            br.BaseStream.Position = (int)header.offsetToPointData + (numberOfPointsRead * header.pointDataRecordLength);

            float x = br.ReadInt32();
            float y = br.ReadInt32();
            float z = br.ReadInt32();
            pd = new PointData();
            br.ReadBytes(3);
            pd.classification = br.ReadByte();

            numberOfPointsRead++;
            pd.coordinates = new Vector3((x * (float)header.xScaleFactor) + (float)header.xOffset, (z * (float)header.zScaleFactor) + (float)header.zOffset, (y * (float)header.yScaleFactor) + (float)header.yOffset);
            pd.LocalPosition = Normalize(origin, pd.coordinates);

            Int64 tempRealPos = GetRealPosition(octree.GetRoot().FindCoordinateOnOctree(pd.LocalPosition));
            if (pointsToWrite.ContainsKey(tempRealPos)) {
                pointsToWrite[tempRealPos].Add(pd);
            } else {
                List<PointData> pdTemp = new List<PointData>();
                pdTemp.Add(pd);
                pointsToWrite.Add(tempRealPos, pdTemp);
            }
            totalSizeOfDic++;

            if (numberOfPointsRead % 100000 == 0) {
                percentage = (((float)numberOfPointsRead / maxPoints) * 100);
                yield return null;
            }
            if ((totalSizeOfDic < 100000)) {
                if (((numberOfPointsRead < (header.legacyNumberOfPointRecords)))){
                    continue;
                }
            }

            //GET COORDINATE AND POSITION IN FILE
            foreach (var p in new Dictionary<Int64, List<PointData>>(pointsToWrite)) {
                

                Int64 a = (p.Key * (Int64)(sizeOfPoint * pointsToWritePerBlock));

                //READ HOW MANY POINTS
                br_pos = new BinaryReader(fs = File.OpenRead((Application.streamingAssetsPath + "/" + fileName + "/" + fileName + "-0" + "/" + fileName + "-0" + ".bin")));
                br_pos.BaseStream.Position = a;

                if (a > br_pos.BaseStream.Length) {
                    print("Trying to get to: " + a);
                    print("Realpos: " + p.Key);
                    print("Length: " + br_pos.BaseStream.Length);
                    print("On iteration: " + numberOfPointsRead);
                }

                int numberOfPoints = br_pos.ReadInt32();
                br_pos.Close();
                fs.Close();


                Int64 c = a + (((numberOfPoints) * sizeOfPoint) + sizeof(int));

                bw = new BinaryWriter(fs = File.OpenWrite((Application.streamingAssetsPath + "/" + fileName + "/" + fileName + "-0" + "/" + fileName + "-0" + ".bin")));
                bw.BaseStream.Position = a;
                bw.Write(numberOfPoints + p.Value.Count);
                bw.BaseStream.Position = c;

                //WRITE POINTS
                List<PointData> sListOfPoints = ShuffleList(p.Value);
                foreach (PointData value in sListOfPoints) {
                    bw.Write((Double)value.coordinates.x);
                    bw.Write((Double)value.coordinates.y);
                    bw.Write((Double)value.coordinates.z);
                    bw.Write(value.classification);
                }
                totalSizeOfDic -= sListOfPoints.Count;
                pointsToWrite.Remove(p.Key);
                bw.Close();
                fs.Close();

                if (numberOfPoints > pointsToWritePerBlock) {

                    print("Over in bin at " + numberOfPoints);
                    print("Coordinate at: " + p.Key);
                    yield return null;
                    break;
                }
            }
            
        }

        

        


        finishedCreatingBin = true;
        lastCoordinatePosition = new Vector3(-1f, -1f, -1f);
        print("Finish time: " + System.DateTime.Now);

    }

    /// <summary>
    /// Validates if a point is on a tile
    /// </summary>
    /// <param name="minX"></param>
    /// <param name="minY"></param>
    /// <param name="minZ"></param>
    /// <param name="max"></param>
    /// <param name="point"></param>
    /// <returns></returns>
    bool ValidateTilePoint(float minX, float minY, float minZ, float max, Vector3 point) {
        //print(minX + " " + minY + " " + minZ + ". Max is: " + max + ". Point is: " + point);
        if((point.x >= minX && point.x < (minX + max)) && (point.y >= minY && point.y <= (minY + max)) && (point.z >= minZ && point.z < (minZ + max))) {
            return true;
        }
        return false;
    }


    /// <summary>
    /// Reads the header of the given file
    /// </summary>
    void ReadHeader() {
        print("Reading Header...");
        //how many - where we start
        header.fileSignature = br.ReadChars(4); //4 - 0
        header.fileSourceID = br.ReadUInt16(); // 2 - 4
        header.globalEncoding = br.ReadUInt16(); //2 - 6
        header.projectID1 = br.ReadUInt32(); //4 - 8
        header.projectID2 = br.ReadUInt16(); //2 - 12

        header.projectID3 = br.ReadUInt16(); //2 - 14
        header.projectID4 = br.ReadChars(8); //8 - 16
        header.versionMajor = br.ReadByte(); //1 - 24
        header.versionMinor = br.ReadByte(); //1 - 25
        header.systemIdentifier = br.ReadChars(32); //32 - 26

        header.generatingSoftware = br.ReadChars(32); //32 - 58
        header.creationDayOfYear = br.ReadUInt16(); //2
        header.creationYear = br.ReadUInt16(); //2
        header.headerSize = br.ReadUInt16(); //2
        header.offsetToPointData = br.ReadUInt32(); //4

        header.numberOfVariableLengthRecords = br.ReadUInt32();
        header.pointDataRecordFormat = br.ReadByte();
        header.pointDataRecordLength = br.ReadUInt16();
        header.legacyNumberOfPointRecords = br.ReadUInt32();
        header.legacyNumberOfPointsByReturn[0] = br.ReadUInt32();
        header.legacyNumberOfPointsByReturn[1] = br.ReadUInt32();

        header.legacyNumberOfPointsByReturn[2] = br.ReadUInt32();
        header.legacyNumberOfPointsByReturn[3] = br.ReadUInt32();
        header.legacyNumberOfPointsByReturn[4] = br.ReadUInt32();
        header.xScaleFactor = br.ReadDouble();
        header.yScaleFactor = br.ReadDouble();

        header.zScaleFactor = br.ReadDouble();
        header.xOffset = br.ReadDouble();
        header.yOffset = br.ReadDouble();
        header.zOffset = br.ReadDouble();
        header.xMax = br.ReadDouble();

        header.xMin = br.ReadDouble();
        header.yMax = br.ReadDouble();
        header.yMin = br.ReadDouble();
        header.zMax = br.ReadDouble();
        header.zMin = br.ReadDouble();
        if ((int)header.versionMajor == 1 && (int)header.versionMinor >= 3) { //control for version 1.3
            header.startOfWaveformDataPacketRecord = br.ReadUInt64();
        }
        if ((int)header.versionMajor == 1 && (int)header.versionMinor >= 4) {  //control for version 1.4
            header.startOfFirstExtendedVariableLengthRecord = br.ReadUInt64();
            header.numberOfExtendedVariableLengthRecords = br.ReadUInt32();
            header.numberOfPointRecords = br.ReadUInt64();
            for(int i = 0; i < 15; i++) {
                header.numberOfPointsByReturn[i] = br.ReadUInt64();
            }
        }

        points = (int)header.legacyNumberOfPointRecords;
        print("Done!");
    }

    /// <summary>
    /// Begins to read in points
    /// </summary>
    public void BeginReadingPoints() {
        StartCoroutine("ReadPoints");
    }

    public void ToggleAllPoints() {
        renderAllPoints = !renderAllPoints;

        if (!renderAllPoints)
            pointSize = 1.5f;
        else
            pointSize = 0.2f;

        //===================
        //Resets the point cloud
        //===================

        //Remove all points in the point cloud
        foreach (Vector3 position in new List<Vector3>(positionsToDraw)) {
            positionsToDraw.Remove(position);
        }

        //Remove all point clouds
        foreach (GameObject g in new List<GameObject>(gameObjectPoints)) {
            gameObjectPoints.Remove(g);
            Destroy(g);
        }
    }

    /// <summary>
    /// Read points and
    /// 1) Expand Octree
    /// 2) Make New File/Bin
    /// </summary>
    /// <returns></returns>
    public IEnumerator ReadPoints() {
        print("Creating Points...");

        //xyz
        float x, y, z;
        //create origin for normalization
        min = new Vector3((float)header.xMin, (float)header.zMin, (float)header.yMin);
        max = new Vector3((float)header.xMax, (float)header.zMax, (float)header.yMax);
        origin = new Vector3((max.x + min.x) / 2, (max.y + min.y) / 2, (max.z + min.z) / 2);
        if (useGlobalValues) {
            globalOrigin = new Vector3((customMin.x + customMax.x) / 2, (customMin.y + customMax.y) / 2, (customMin.z + customMax.z) / 2);
            globalOffset = origin - customMin;//globalOrigin - origin;
        }
        PointData p;
        int splitTimes = 0;
        print("Start time: " + System.DateTime.Now);
        //create other points around origin
        while (octree.hasSplit) {
            octree.hasSplit = false;
            for (int i = 0; i < (header.legacyNumberOfPointRecords); i++) { 
                br.BaseStream.Position = (int)header.offsetToPointData + (i * header.pointDataRecordLength);
                if (maxPoints != 0 && i > maxPoints - 1) {
                    break;
                }
                x = br.ReadInt32();
                y = br.ReadInt32();
                z = br.ReadInt32();
                
                p = CreatePointType(br);
                p.coordinates = new Vector3((x * (float)header.xScaleFactor) + (float)header.xOffset, (z * (float)header.zScaleFactor) + (float)header.zOffset, (y * (float)header.yScaleFactor) + (float)header.yOffset);
                p.LocalPosition = Normalize(origin, p.coordinates);
                if(i == 0) {
                    debugPoint = p.LocalPosition + globalOffset;
                }


                octree.GetRoot().ExpandTree(p, octree.MaxPoints);
                if (i % 100000 == 0) {
                    if (maxPoints > 0 && maxPoints < header.legacyNumberOfPointRecords) {
                        percentage = (((float)i / maxPoints) * 100);
                    } else {
                        percentage = (((float)i / header.legacyNumberOfPointRecords) * 100);
                    }

                    yield return null;
                }
            }
            octree.GetRoot().ExpandTreeDepth(octree.CurrentMaxDepth);
            if (octree.hasSplit) {
                splitTimes += 1;
            }
        }

        print("Finished creating points and tree!");
        print("Split " + splitTimes + " times!");
        print("Finish time: " + System.DateTime.Now);
        yield return null;

        


        if (makeBin) {
            print("Creating tile files...");
            yield return null;
            StartCoroutine("WriteToBin");
        } else {
            finishedCreatingBin = true;
        }
        //finished creating bin file
        
    }

    /// <summary>
    /// Normalize a point around some origin
    /// </summary>
    /// <param name="origin"></param>
    /// <param name="point"></param>
    /// <returns></returns>
    Vector3 Normalize(Vector3 origin, Vector3 point) {
        return new Vector3(point.x - origin.x, point.y - origin.y, point.z - origin.z);
    }


    /// <summary>
    /// Create point by reading the given file from BinaryReader
    /// </summary>
    /// <param name="b"></param>
    /// <returns></returns>
    PointData CreatePointType(BinaryReader b) {
        PointData c = new PointData();
        b.ReadBytes(3);
        c.classification = b.ReadByte();
        return new PointData();
    }

}
