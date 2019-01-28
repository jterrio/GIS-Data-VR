using System.Collections;
using System.Collections.Generic;
using System.IO;
using System;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.UI;
using System.Diagnostics;

public class GISData : GISDefinitions {

    public string fileName;
    public string fileType;
    protected string path;
    public int maxPoints; //max points to generate from file; leave at 0 for no max
    public Header header;
    public GameObject point;
    public int points;
    protected BinaryReader br;
    public Octree octree;
    private Vector3 min, max, origin;
    private bool finishedCreatingBin = false;
    public GameObject player;
    private List<GameObject> gameObjectPoints = new List<GameObject>();
    public Vector3 lastCoordinatePosition;
    public float percentage = 0f;
    public bool makeBin = false;
    public List<int> positionList = new List<int>();
    public List<PointData> positionCount = new List<PointData>();
    public List<Vector3> positionsToDraw = new List<Vector3>();
    public int viewDistance = 2;
    public GameObject holderObject;
    public bool readyToRender = false;

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
        if (!finishedCreatingBin) {
            return;
        }
        if (readyToRender) {
            DrawPoints();
        }
    }


    private void OnDrawGizmos() {
        //return;
        Gizmos.color = new Color(1, 0, 0, 1f);
        foreach (Vector3 v in positionsToDraw) {
            Octree.OctreeNode oc = octree.GetRoot().GetNodeAtCoordinate(v);
            Gizmos.DrawWireCube(oc.Position, new Vector3(octree.smallestTile, octree.smallestTile, octree.smallestTile));

        }
        //Gizmos.DrawWireCube(origin, max - min);
    }

    void DrawPoints() {
        Vector3 coordinate = octree.GetRoot().FindCoordinateOnOctree(player.transform.position);
        BinaryReader br_pos;
        FileStream fs;
        Stopwatch stopwatch = new Stopwatch();
        stopwatch.Start();
        BinaryReader br_pos0 = new BinaryReader(fs = File.OpenRead((Application.streamingAssetsPath + "/" + fileName + "/" + fileName + "-0" + "/" + fileName + "-0" + ".bin")));
        BinaryReader br_pos1 = new BinaryReader(fs = File.OpenRead((Application.streamingAssetsPath + "/" + fileName + "/" + fileName + "-1" + "/" + fileName + "-1" + ".bin")));
        BinaryReader br_pos2 = new BinaryReader(fs = File.OpenRead((Application.streamingAssetsPath + "/" + fileName + "/" + fileName + "-2" + "/" + fileName + "-2" + ".bin")));
        int sizeOfPoint = GetSizeOfPoint(header.versionMajor, header.versionMinor, header.pointDataRecordFormat);

        positionsToDraw.Clear();
        lastCoordinatePosition = coordinate;
        positionsToDraw.Add(coordinate);

        AddFOV();


        foreach (GameObject p in gameObjectPoints) {
            Destroy(p);
        }
        gameObjectPoints.Clear();

        int totalPointsRendered = 0;

        bool needContinue = true;
        while (needContinue) {
            needContinue = false;
            foreach (GameObject other in new List<GameObject>(gameObjectPoints)) {
                foreach (GameObject p in new List<GameObject>(gameObjectPoints)) {
                    if (other.name == p.name || p.name.ToCharArray()[0] == '-' && other != p) {
                        gameObjectPoints.Remove(p);
                        needContinue = true;
                        break;
                    }
                }
            }
        }

        foreach (Vector3 position in new List<Vector3>(positionsToDraw)) {

            int distance = Distance(lastCoordinatePosition, position);
            Int64 realPos = GetRealPosition(position);
            GameObject p = Instantiate(holderObject);
            p.name = realPos.ToString() + "-" + distance.ToString();
            int pointsInBlock;
            if(distance < 1) {
                p.transform.position = octree.GetRoot().GetNodeAtCoordinate(position).Position;
                br_pos = br_pos0;
                pointsInBlock = 1000;
            } else if(distance < 2) {
                p.transform.position = octree.GetRoot().GetNodeAtCoordinateDepth(position, octree.currentMaxDepth - 2).Position;
                br_pos = br_pos1;
                realPos -= (realPos % 8);
                pointsInBlock = 500;
            } else {
                p.transform.position = octree.GetRoot().GetNodeAtCoordinateDepth(position, octree.currentMaxDepth - 3).Position;
                br_pos = br_pos2;
                realPos -= (realPos % 64);
                pointsInBlock = 250;
            }
            gameObjectPoints.Add(p);
            p.SetActive(true);


            Int64 a = realPos * (Int64)(sizeOfPoint * pointsInBlock);
            if (a >= br_pos.BaseStream.Length || a < 0) {
                continue;
            }
            br_pos.BaseStream.Position = a;
            int numberOfPoints = br_pos.ReadInt32();
            if (numberOfPoints <= 0) {
                gameObjectPoints.Remove(p);
                Destroy(p);
                continue;
            }

            totalPointsRendered += numberOfPoints;
            for (int i = 0; i < numberOfPoints; i++) {
                br_pos.BaseStream.Position = (a + (((i) * sizeOfPoint)) + sizeof(int));
                GameObject temp = Instantiate(point);
                Vector3 realCoor = new Vector3((float)br_pos.ReadDouble(), (float)br_pos.ReadDouble(), (float)br_pos.ReadDouble());
                temp.transform.position = Normalize(origin, realCoor);
                temp.transform.parent = p.transform;
            }

        }

        br_pos0.Close();
        br_pos1.Close();
        br_pos2.Close();
        fs.Close();


        stopwatch.Stop();
        print("Total points rendered: " + totalPointsRendered);
        print("Time to render points (in milliseconds): " + stopwatch.ElapsedMilliseconds);
    }


    int Distance(Vector3 home, Vector3 away) {
        return Mathf.Clamp(Mathf.FloorToInt(Vector3.Distance(home, away))- 1, 0, 2);
    }

    void AddFOV() {
        //Vector3 cameraDirection = octree.GetRoot().GetNodeAtCoordinate(lastCoordinatePosition).Position + Camera.main.gameObject.transform.forward * octree.smallestTile * viewDistance;
        int x = (int)lastCoordinatePosition.x + viewDistance;
        int y = (int)lastCoordinatePosition.y + viewDistance;
        int z = (int)lastCoordinatePosition.z + viewDistance;

        while (z >= ((int)lastCoordinatePosition.z - viewDistance)) {
            while (y >= ((int)lastCoordinatePosition.y - viewDistance)) {
                while (x >= ((int)lastCoordinatePosition.x - viewDistance)) {
                    if (x >= 0 && y >= 0 && z >= 0) {
                        if ((IsVisible(octree.GetRoot().GetNodeAtCoordinate(new Vector3(x, y, z)).Position) && !positionsToDraw.Contains(new Vector3(x, y, z)))) {
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

    bool IsVisible(Vector3 point) {
        bool isVisible = false;
        Vector3 cameraPoint = Camera.main.WorldToViewportPoint(point);
        if ((cameraPoint.x >= 0 && cameraPoint.x <= 1) && (cameraPoint.y >= 0 && cameraPoint.y <= 1) && (cameraPoint.z > 0)) {
            isVisible = true;
        }
        return isVisible;
    }


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


    void SetOctreeBase() {
        Vector3 min = new Vector3((float)header.xMin, (float)header.yMin, (float)header.zMin);
        Vector3 max = new Vector3((float)header.xMax, (float)header.yMax, (float)header.zMax);
        Vector3 origin = new Vector3((max.x + min.x) / 2, (max.y + min.y) / 2, (max.z + min.z) / 2);
        Vector3 normalMin = Normalize(origin, min);
        Vector3 normalMax = Normalize(origin, max);
        float[] ranges = new float[] { (float)(normalMax.x - normalMin.x), (float)(normalMax.y - normalMin.y), (float)(normalMax.z - normalMin.z) };
        octree = new Octree(Vector3.zero, Mathf.Max(ranges), 1000);
        //player.transform.position = normalMax - normalMin;
        print("Init Size: " + octree.SmallestTile);
    }

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
        bw.BaseStream.Position = (octree.currentLeaves * (Int64)(sizeOfPoint * 1000));
        bw.Write(0);
        bw.Close();
        fs.Close();
        //yield return null;

        int numberOfPointsRead = 0;
        List<PointData> pointsToWrite = new List<PointData>();

        //WRITE AND CREATE A FILE AT DEPTH = MAX
        while (numberOfPointsRead < (header.legacyNumberOfPointRecords)) { //(header.legacyNumberOfPointRecords - 1)
            br.BaseStream.Position = (int)header.offsetToPointData + (numberOfPointsRead * header.pointDataRecordLength);

            float x = br.ReadInt32();
            float y = br.ReadInt32();
            float z = br.ReadInt32();
            numberOfPointsRead++;
            pd = CreatePointType(br);
            pd.coordinates = new Vector3((x * (float)header.xScaleFactor) + (float)header.xOffset, (y * (float)header.yScaleFactor) + (float)header.yOffset, (z * (float)header.zScaleFactor) + (float)header.zOffset);
            pd.LocalPosition = Normalize(origin, pd.coordinates);
            pointsToWrite.Add(pd);
            if (numberOfPointsRead % 10000 == 0) {
                if (maxPoints > 0 && maxPoints < header.legacyNumberOfPointRecords) {
                    percentage = (((float)numberOfPointsRead / maxPoints) * 100);
                } else {
                    percentage = (((float)numberOfPointsRead / header.legacyNumberOfPointRecords) * 100);
                    //print("PERCENTAGE: " + (((float)i / header.legacyNumberOfPointRecords) * 100));
                }
                yield return null;
            }
            if (pointsToWrite.Count < 1000) {
                continue;
            }

            //GET COORDINATE AND POSITION IN FILE
            foreach (PointData p in pointsToWrite) {
                Vector3 coordinate = octree.GetRoot().FindCoordinateOnOctree(p.LocalPosition);
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
                int realPos = Convert.ToInt32(actualPos, 2);
                //bw.BaseStream.Position = realPos * sizeOfBlock;
                Int64 a = (realPos * (Int64)(sizeOfPoint * 1000));

                //WRITE IT
                br_pos = new BinaryReader(fs = File.OpenRead((Application.streamingAssetsPath + "/" + fileName + "/" + fileName + "-0" + "/" + fileName + "-0" + ".bin")));
                br_pos.BaseStream.Position = a;

                if (a > br_pos.BaseStream.Length) {
                    print("Trying to get to: " + a);
                    print("Realpos: " + realPos);
                    print("Length: " + br_pos.BaseStream.Length);
                    print("Coordinate: " + coordinate);
                    print("On iteration: " + numberOfPointsRead);
                }
                int numberOfPoints = br_pos.ReadInt32();
                br_pos.Close();
                fs.Close();


                Int64 c = a + (((numberOfPoints) * sizeOfPoint) + sizeof(int));

                bw = new BinaryWriter(fs = File.OpenWrite((Application.streamingAssetsPath + "/" + fileName + "/" + fileName + "-0" + "/" + fileName + "-0" + ".bin")));
                bw.BaseStream.Position = a;
                bw.Write(numberOfPoints + 1);
                //WRITE POINT

                bw.BaseStream.Position = c;
                bw.Write((Double)p.coordinates.x);
                bw.Write((Double)p.coordinates.y);
                bw.Write((Double)p.coordinates.z);
                bw.Close();
                fs.Close();





                if (numberOfPoints > 1000) {

                    print("Over 1000 in bin at " + numberOfPoints);
                    print("Coordinate at: " + coordinate);
                    yield return null;
                    //break;
                }
            }
            pointsToWrite.Clear();
        }

        //WRITE AND CREATE A FILE AT DEPTH = MAX - 1
        pointsToWrite.Clear();
        int nodePosition = 0;
        int numberOfNodesRead = 0;
        folder = Directory.CreateDirectory(Application.streamingAssetsPath + "/" + fileName + "/" + fileName + "-1");
        //CREATE FILE WITH SOME N LENGTH
        bw = new BinaryWriter(fs = File.OpenWrite((Application.streamingAssetsPath + "/" + fileName + "/" + fileName + "-1" + "/" + fileName + "-1" + ".bin")));
        bw.BaseStream.Position = (octree.currentLeaves * (Int64)(sizeOfPoint * 500));
        bw.Write(0);
        bw.Close();
        fs.Close();
        
        while (numberOfNodesRead < octree.currentLeaves) { //(header.legacyNumberOfPointRecords - 1)
            br_pos = new BinaryReader(fs = File.OpenRead((Application.streamingAssetsPath + "/" + fileName + "/" + fileName + "-0" + "/" + fileName + "-0" + ".bin")));
            br_pos.BaseStream.Position = numberOfNodesRead * (Int64)(sizeOfPoint * 1000);
            int pointsInNode = br_pos.ReadInt32();
            for (int n = 0; n < 8; n++) {
                for (int i = 0; i < Mathf.Min(pointsInNode, 62); i++) {
                    float x = br_pos.ReadInt32();
                    float y = br_pos.ReadInt32();
                    float z = br_pos.ReadInt32();
                    pd = CreatePointType(br_pos);
                    pd.coordinates = new Vector3((x * (float)header.xScaleFactor) + (float)header.xOffset, (y * (float)header.yScaleFactor) + (float)header.yOffset, (z * (float)header.zScaleFactor) + (float)header.zOffset);
                    pd.LocalPosition = Normalize(origin, pd.coordinates);
                    pointsToWrite.Add(pd);
                }
                numberOfNodesRead++;
            }
            if(pointsToWrite.Count > 500) {
                print("PROBLEM!");
            }

            br_pos.Close();
            fs.Close();
            bw = new BinaryWriter(fs = File.OpenWrite((Application.streamingAssetsPath + "/" + fileName + "/" + fileName + "-0" + "/" + fileName + "-0" + ".bin")));
            bw.BaseStream.Position = (numberOfNodesRead - 8) * (Int64)(sizeOfPoint * 500);
            bw.Write(pointsToWrite.Count);
            //GET COORDINATE AND POSITION IN FILE
            foreach (PointData p in pointsToWrite) {
                //WRITE POINT
                bw.Write((Double)p.coordinates.x);
                bw.Write((Double)p.coordinates.y);
                bw.Write((Double)p.coordinates.z);
                bw.Write((Double)p.classification);
            }
            bw.Close();
            fs.Close();
            pointsToWrite.Clear();
        }


        //WRITE AND CREATE A FILE AT DEPTH = MAX - 2
        pointsToWrite.Clear();
        nodePosition = 0;
        numberOfNodesRead = 0;
        folder = Directory.CreateDirectory(Application.streamingAssetsPath + "/" + fileName + "/" + fileName + "-2");
        //CREATE FILE WITH SOME N LENGTH
        bw = new BinaryWriter(fs = File.OpenWrite((Application.streamingAssetsPath + "/" + fileName + "/" + fileName + "-2" + "/" + fileName + "-2" + ".bin")));
        bw.BaseStream.Position = (octree.currentLeaves * (Int64)(sizeOfPoint * 250));
        bw.Write(0);
        bw.Close();
        fs.Close();
        
        while (numberOfNodesRead < octree.currentLeaves) { //(header.legacyNumberOfPointRecords - 1)
            br_pos = new BinaryReader(fs = File.OpenRead((Application.streamingAssetsPath + "/" + fileName + "/" + fileName + "-1" + "/" + fileName + "-1" + ".bin")));
            br_pos.BaseStream.Position = numberOfNodesRead * (sizeOfPoint * 500);
            int pointsInNode = br_pos.ReadInt32();
            for (int n = 0; n < 8; n++) {
                
                for (int i = 0; i < Mathf.Min(pointsInNode, 31); i++) {
                    float x = br_pos.ReadInt32();
                    float y = br_pos.ReadInt32();
                    float z = br_pos.ReadInt32();
                    pd = CreatePointType(br_pos);
                    pd.coordinates = new Vector3((x * (float)header.xScaleFactor) + (float)header.xOffset, (y * (float)header.yScaleFactor) + (float)header.yOffset, (z * (float)header.zScaleFactor) + (float)header.zOffset);
                    pd.LocalPosition = Normalize(origin, pd.coordinates);
                    pointsToWrite.Add(pd);
                }
                numberOfNodesRead++;
            }
            if (pointsToWrite.Count > 250) {
                print("PROBLEM!");
            }

            br_pos.Close();
            fs.Close();
            bw = new BinaryWriter(fs = File.OpenWrite((Application.streamingAssetsPath + "/" + fileName + "/" + fileName + "-2" + "/" + fileName + "-2" + ".bin")));
            bw.BaseStream.Position = (numberOfNodesRead - 4) * (Int64)(sizeOfPoint * 250);
            bw.Write(pointsToWrite.Count);
            //GET COORDINATE AND POSITION IN FILE
            foreach (PointData p in pointsToWrite) {
                //WRITE POINT
                bw.Write((Double)p.coordinates.x);
                bw.Write((Double)p.coordinates.y);
                bw.Write((Double)p.coordinates.z);
                bw.Write((Double)p.classification);
            }
            bw.Close();
            fs.Close();
            pointsToWrite.Clear();
        }


        finishedCreatingBin = true;
        lastCoordinatePosition = new Vector3(-1f, -1f, -1f);
        print("Finish time: " + System.DateTime.Now);

    }

    bool ValidateTilePoint(float minX, float minY, float minZ, float max, Vector3 point) {
        //print(minX + " " + minY + " " + minZ + ". Max is: " + max + ". Point is: " + point);
        if((point.x >= minX && point.x < (minX + max)) && (point.y >= minY && point.y <= (minY + max)) && (point.z >= minZ && point.z < (minZ + max))) {
            return true;
        }
        return false;
    }



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


    public void BeginReadingPoints() {
        StartCoroutine("ReadPoints");
    }

    public IEnumerator ReadPoints() {
        print("Creating Points...");

        //xyz
        float x, y, z;
        //create origin for normalization
        min = new Vector3((float)header.xMin, (float)header.yMin, (float)header.zMin);
        max = new Vector3((float)header.xMax, (float)header.yMax, (float)header.zMax);
        origin = new Vector3((max.x + min.x) / 2, (max.y + min.y) / 2, (max.z + min.z) / 2);

        PointData p;
        int splitTimes = 0;
        print("Start time: " + System.DateTime.Now);
        //create other points around origin
        while (octree.hasSplit) {
            octree.hasSplit = false;
            //br.ReadBytes((int)header.offsetToPointData - (int)header.headerSize);
            //br.BaseStream.Position = (int)header.offsetToPointData;
            for (int i = 0; i < (header.legacyNumberOfPointRecords); i++) { //(header.legacyNumberOfPointRecords - 1)
                br.BaseStream.Position = (int)header.offsetToPointData + (i * header.pointDataRecordLength);
                if (maxPoints != 0 && i > maxPoints - 1) {
                    break;
                }
                x = br.ReadInt32();
                y = br.ReadInt32();
                z = br.ReadInt32();
                
                p = CreatePointType(br);
                p.coordinates = new Vector3((x * (float)header.xScaleFactor) + (float)header.xOffset, (y * (float)header.yScaleFactor) + (float)header.yOffset, (z * (float)header.zScaleFactor) + (float)header.zOffset);
                p.LocalPosition = Normalize(origin, p.coordinates);

                if(i == 1) {
                    print(p.LocalPosition.x + " " + p.LocalPosition.y + " " + p.LocalPosition.z);
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


    Vector3 Normalize(Vector3 origin, Vector3 point) {
        return new Vector3(point.x - origin.x, point.y - origin.y, point.z - origin.z);
    }

    PointData CreatePointType(BinaryReader b) {
        PointData c = new PointData();
        c.classification = b.ReadByte();
        return new PointData();
    }

}
