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
    public List<PointData> points = new List<PointData>();
    protected BinaryReader br;
    public Octree octree;
    private Vector3 min, max, origin;
    private bool finishedCreatingBin = false;
    public GameObject player;
    private List<GameObject> gameObjectPoints = new List<GameObject>();
    public Vector3 lastCoordinatePosition;
    public float percentage = 0f;
    public bool makeBin = false;

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
        Vector3 coordinate = octree.GetRoot().FindCoordinateOnOctree(player.transform.position);
        //print(coordinate);
        if(coordinate == lastCoordinatePosition) { //no need to run code for same block if we are in it
            return;
        }
        Stopwatch stopwatch = new Stopwatch();
        stopwatch.Start();
        lastCoordinatePosition = coordinate;
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
        int sizeOfPoint = GetSizeOfPoint(header.versionMajor, header.versionMinor, header.pointDataRecordFormat);
        FileStream fs;
        BinaryReader br_pos = new BinaryReader(fs = File.OpenRead((Application.streamingAssetsPath + "/" + fileName + ".bin")));
        br_pos.BaseStream.Position = realPos * (sizeOfPoint * 1000);
        int numberOfPoints = br_pos.ReadInt32();

        foreach(GameObject p in gameObjectPoints) {
            Destroy(p);
        }
        gameObjectPoints.Clear();

        for(int i = 0; i < numberOfPoints; i++) {
            br_pos.BaseStream.Position = ((realPos * (sizeOfPoint * 1000)) + (((i) * sizeOfPoint)) + sizeof(int));
            GameObject temp = Instantiate(point);
            Vector3 realCoor = new Vector3((float)br_pos.ReadDouble(), (float)br_pos.ReadDouble(), (float)br_pos.ReadDouble());
            temp.transform.position = Normalize(origin, realCoor);
            gameObjectPoints.Add(temp);
        }



        br_pos.Close();
        fs.Close();


        stopwatch.Stop();
        print("Block ID: " + coordinate);
        print("Number of points in block: " + numberOfPoints);
        print("Time to render points (in milliseconds): " + stopwatch.ElapsedMilliseconds);
    }


    void SetOctreeBase() {
        Vector3 min = new Vector3((float)header.xMin, (float)header.yMin, (float)header.zMin);
        Vector3 max = new Vector3((float)header.xMax, (float)header.yMax, (float)header.zMax);
        Vector3 origin = new Vector3((max.x + min.x) / 2, (max.y + min.y) / 2, (max.z + min.z) / 2);
        Vector3 normalMin = Normalize(origin, min);
        Vector3 normalMax = Normalize(origin, max);
        float[] ranges = new float[] { (float)(normalMax.x - normalMin.x), (float)(normalMax.y - normalMin.y), (float)(normalMax.z - normalMin.z) };
        octree = new Octree(Vector3.zero, Mathf.Max(ranges), 1000);
        print("Init Size: " + octree.SmallestTile);
    }

    IEnumerator WriteToBin() {
        BinaryReader br_pos;
        BinaryWriter bw;
        float tileSize = octree.SmallestTile;
        Vector3 tilePos = Normalize(origin, min);
        int sizeOfPoint = GetSizeOfPoint(header.versionMajor, header.versionMinor, header.pointDataRecordFormat);
        FileStream fs = File.Create((Application.streamingAssetsPath + "/" + fileName + ".bin"));
        PointData p;
        Vector3 normalMin = Normalize(origin, min);
        Vector3 normalMax = Normalize(origin, max);
        br.BaseStream.Position = (int)header.offsetToPointData;
        fs.Close();

        //CREATE FILE WITH SOME N LENGTH
        bw = new BinaryWriter(fs = File.OpenWrite((Application.streamingAssetsPath + "/" + fileName + ".bin")));
        bw.BaseStream.Position = octree.currentLeaves * (sizeOfPoint * 1000);
        bw.Write(0);
        bw.Close();
        fs.Close();

        for (int i = 0; i < (header.legacyNumberOfPointRecords); i++) { //(header.legacyNumberOfPointRecords - 1)
            float x = br.ReadInt32();
            float y = br.ReadInt32();
            float z = br.ReadInt32();
            p = CreatePointType();
            p.coordinates = new Vector3((x * (float)header.xScaleFactor) + (float)header.xOffset, (y * (float)header.yScaleFactor) + (float)header.yOffset, (z * (float)header.zScaleFactor) + (float)header.zOffset);
            p.LocalPosition = Normalize(origin, p.coordinates);
            if (i % 10000 == 0) {
                if (maxPoints > 0 && maxPoints < header.legacyNumberOfPointRecords) {
                    percentage = (((float)i / maxPoints) * 100);
                } else {
                    percentage = (((float)i / header.legacyNumberOfPointRecords) * 100);
                    //print("PERCENTAGE: " + (((float)i / header.legacyNumberOfPointRecords) * 100));
                }

                yield return new WaitForEndOfFrame();
            }
            

            //GET COORDINATE AND POSITION IN FILE
            Vector3 coordinate = octree.GetRoot().FindCoordinateOnOctree(p.LocalPosition);
            char[] xBits = Convert.ToString((int)coordinate.x, 2).ToCharArray();
            char[] yBits = Convert.ToString((int)coordinate.y, 2).ToCharArray();
            char[] zBits = Convert.ToString((int)coordinate.z, 2).ToCharArray();
            char[] bitPos = new char[Mathf.Max(xBits.Length, yBits.Length, zBits.Length) * 3];
            int currentPos = 0;
            for(int b = 0; b < Mathf.Max(xBits.Length, yBits.Length, zBits.Length); b++) {
                if(b > xBits.Length - 1) {
                    bitPos[currentPos] = '0';
                } else {
                    bitPos[currentPos] = xBits[xBits.Length - (b + 1)];
                }
                currentPos += 1;
                if (b > yBits.Length -1) {
                    bitPos[currentPos] = '0';
                } else {
                    bitPos[currentPos] = yBits[yBits.Length - (b + 1)];
                }
                currentPos += 1;
                if (b >zBits.Length - 1) {
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



            //WRITE IT
            br_pos = new BinaryReader(fs = File.OpenRead((Application.streamingAssetsPath + "/" + fileName + ".bin")));
            br_pos.BaseStream.Position = realPos * (sizeOfPoint * 1000);
            int numberOfPoints = br_pos.ReadInt32();
            br_pos.Close();
            fs.Close();

            bw = new BinaryWriter(fs = File.OpenWrite((Application.streamingAssetsPath + "/" + fileName + ".bin")));
            bw.BaseStream.Position = realPos * (sizeOfPoint * 1000);
            bw.Write(numberOfPoints + 1);
            //WRITE POINT
            int a = (realPos * (sizeOfPoint * 1000));
            int d = ((numberOfPoints) * sizeOfPoint) + sizeof(int);
            int c = a + d;

            bw.BaseStream.Position = ((realPos * (sizeOfPoint * 1000)) + ((numberOfPoints) * sizeOfPoint) + sizeof(int));
            bw.Write((Double)p.coordinates.x);
            bw.Write((Double)p.coordinates.y);
            bw.Write((Double)p.coordinates.z);
            bw.Close();
            fs.Close();



            /*
            //DEBUG PRINT
            br_pos = new BinaryReader(fs = File.OpenRead((Application.streamingAssetsPath + "/" + fileName + ".bin")));
            br_pos.BaseStream.Position = realPos * (sizeOfPoint * 1000);
            int temp = br_pos.ReadInt32();
            br_pos.BaseStream.Position = ((realPos * (sizeOfPoint * 1000)) + ((numberOfPoints + 1) * sizeOfPoint));
            print("X: " + br_pos.ReadDouble());
            print("Y: " + br_pos.ReadDouble());
            print("Z: " + br_pos.ReadDouble());
            br_pos.Close();
            fs.Close(); */
            //return new WaitForEndOfFrame();
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
            br.BaseStream.Position = (int)header.offsetToPointData;
            for (int i = 0; i < (header.legacyNumberOfPointRecords); i++) { //(header.legacyNumberOfPointRecords - 1)

                if (maxPoints != 0 && i > maxPoints - 1) {
                    break;
                }
                x = br.ReadInt32();
                y = br.ReadInt32();
                z = br.ReadInt32();
                
                p = CreatePointType();
                p.coordinates = new Vector3((x * (float)header.xScaleFactor) + (float)header.xOffset, (y * (float)header.yScaleFactor) + (float)header.yOffset, (z * (float)header.zScaleFactor) + (float)header.zOffset);
                p.LocalPosition = Normalize(origin, p.coordinates);

                octree.GetRoot().ExpandTree(p, octree.MaxPoints);
                if (i % 100000 == 0) {
                    if (maxPoints > 0 && maxPoints < header.legacyNumberOfPointRecords) {
                        percentage = (((float)i / maxPoints) * 100);
                    } else {
                        percentage = (((float)i / header.legacyNumberOfPointRecords) * 100);
                    }

                    yield return new WaitForEndOfFrame();
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
        yield return new WaitForEndOfFrame();

        


        if (makeBin) {
            print("Creating tile files...");
            yield return new WaitForEndOfFrame();
            StartCoroutine("WriteToBin");
        } else {
            finishedCreatingBin = true;
        }
        //finished creating bin file
        
    }


    Vector3 Normalize(Vector3 origin, Vector3 point) {
        return new Vector3(point.x - origin.x, point.y - origin.y, point.z - origin.z);
    }

    PointData CreatePointType() {
        PointData c = new PointData();
        switch (header.versionMinor) {
            //version 1.2
            case 2:
                //things that all the formats have
                c.intensity = br.ReadUInt16(); //2
                c.returnInformation = br.ReadByte(); //1
                c.classification = br.ReadByte(); //1
                c.scanAngleRank = br.ReadByte(); //1
                c.userData = br.ReadByte(); //1
                c.pointSourceID = br.ReadUInt16(); //2

                //get the format
                switch (header.pointDataRecordFormat) {
                    case 0:
                        return c;
                    case 1:
                        br.ReadBytes(8);
                        //c.GPSTime = br.ReadDouble(); //8
                        return c;
                    case 2:
                        c.red = br.ReadUInt16(); //2
                        c.green = br.ReadUInt16(); //2
                        c.blue = br.ReadUInt16(); //2
                        return c;
                    case 3:
                        c.GPSTime = br.ReadDouble(); //8
                        c.red = br.ReadUInt16(); //2
                        c.green = br.ReadUInt16(); //2
                        c.blue = br.ReadUInt16(); //2
                        return c;
                }
                break;
            case 3:
                //things that all the formats have
                c.intensity = br.ReadUInt16(); //2
                c.returnInformation = br.ReadByte(); //1
                c.classification = br.ReadByte(); //1
                c.scanAngleRank = br.ReadByte(); //1
                c.userData = br.ReadByte(); //1
                c.pointSourceID = br.ReadUInt16(); //2
                //get the format
                switch (header.pointDataRecordFormat) {
                    case 0:
                        return c;
                    case 1:
                        c.GPSTime = br.ReadDouble(); //8
                        return c;
                    case 2:
                        c.red = br.ReadUInt16(); //2
                        c.green = br.ReadUInt16(); //2
                        c.blue = br.ReadUInt16(); //2
                        return c;
                    case 3:
                        c.GPSTime = br.ReadDouble(); //8
                        c.red = br.ReadUInt16(); //2
                        c.green = br.ReadUInt16(); //2
                        c.blue = br.ReadUInt16(); //2
                        return c;
                    case 4:
                        c.GPSTime = br.ReadDouble(); //8
                        c.wavePacketDescriptorIndex = br.ReadByte(); //1
                        c.byteOffsetToWaveformData = br.ReadUInt64(); //8
                        c.waveformPacketSizeInBytes = br.ReadUInt32(); //4
                        c.returnPointWaveformLocation = br.ReadSingle(); //4
                        c.Xt = br.ReadSingle(); //4
                        c.Yt = br.ReadSingle(); //4
                        c.Zt = br.ReadSingle(); //4
                        return c;
                    case 5:
                        c.GPSTime = br.ReadDouble(); //8
                        c.red = br.ReadUInt16(); //2
                        c.green = br.ReadUInt16(); //2
                        c.blue = br.ReadUInt16(); //2
                        c.wavePacketDescriptorIndex = br.ReadByte(); //1
                        c.byteOffsetToWaveformData = br.ReadUInt64(); //8
                        c.waveformPacketSizeInBytes = br.ReadUInt32(); //4
                        c.returnPointWaveformLocation = br.ReadSingle(); //4
                        c.Xt = br.ReadSingle(); //4
                        c.Yt = br.ReadSingle(); //4
                        c.Zt = br.ReadSingle(); //4
                        return c;
                }

                break;

            case 4:
                //things that all the formats have
                c.intensity = br.ReadUInt16(); //2
                c.returnInformation = br.ReadByte(); //1
                if(header.pointDataRecordFormat >= 6) {
                    c.returnInformationExtended = br.ReadByte(); //1
                }
                c.classification = br.ReadByte(); //1
                c.scanAngleRank = br.ReadByte(); //1
                c.userData = br.ReadByte(); //1
                c.pointSourceID = br.ReadUInt16(); //2
                
                //get the format
                switch (header.pointDataRecordFormat) {
                    case 0:
                        return c;
                    case 1:
                        c.GPSTime = br.ReadDouble(); //8
                        return c;
                    case 2:
                        c.red = br.ReadUInt16(); //2
                        c.green = br.ReadUInt16(); //2
                        c.blue = br.ReadUInt16(); //2
                        return c;
                    case 3:
                        c.GPSTime = br.ReadDouble(); //8
                        c.red = br.ReadUInt16(); //2
                        c.green = br.ReadUInt16(); //2
                        c.blue = br.ReadUInt16(); //2
                        return c;
                    case 4:
                        c.GPSTime = br.ReadDouble(); //8
                        c.wavePacketDescriptorIndex = br.ReadByte(); //1
                        c.byteOffsetToWaveformData = br.ReadUInt64(); //8
                        c.waveformPacketSizeInBytes = br.ReadUInt32(); //4
                        c.returnPointWaveformLocation = br.ReadSingle(); //4
                        c.Xt = br.ReadSingle(); //4
                        c.Yt = br.ReadSingle(); //4
                        c.Zt = br.ReadSingle(); //4
                        return c;
                    case 5:
                        c.GPSTime = br.ReadDouble(); //8
                        c.red = br.ReadUInt16(); //2
                        c.green = br.ReadUInt16(); //2
                        c.blue = br.ReadUInt16(); //2
                        c.wavePacketDescriptorIndex = br.ReadByte(); //1
                        c.byteOffsetToWaveformData = br.ReadUInt64(); //8
                        c.waveformPacketSizeInBytes = br.ReadUInt32(); //4
                        c.returnPointWaveformLocation = br.ReadSingle(); //4
                        c.Xt = br.ReadSingle(); //4
                        c.Yt = br.ReadSingle(); //4
                        c.Zt = br.ReadSingle(); //4
                        return c;
                    case 6:
                        c.GPSTime = br.ReadDouble(); //8
                        return c;
                    case 7:
                        c.GPSTime = br.ReadDouble(); //8
                        c.red = br.ReadUInt16(); //2
                        c.green = br.ReadUInt16(); //2
                        c.blue = br.ReadUInt16(); //2
                        return c;
                    case 8:
                        c.GPSTime = br.ReadDouble(); //8
                        c.red = br.ReadUInt16(); //2
                        c.green = br.ReadUInt16(); //2
                        c.blue = br.ReadUInt16(); //2
                        c.NIR = br.ReadUInt16(); //2
                        return c;
                    case 9:
                        c.GPSTime = br.ReadDouble(); //8
                        c.wavePacketDescriptorIndex = br.ReadByte(); //1
                        c.byteOffsetToWaveformData = br.ReadUInt64(); //8
                        c.waveformPacketSizeInBytes = br.ReadUInt32(); //4
                        c.returnPointWaveformLocation = br.ReadSingle(); //4
                        c.Xt = br.ReadSingle(); //4
                        c.Yt = br.ReadSingle(); //4
                        c.Zt = br.ReadSingle(); //4
                        return c;
                    case 10:
                        c.GPSTime = br.ReadDouble(); //8
                        c.red = br.ReadUInt16(); //2
                        c.green = br.ReadUInt16(); //2
                        c.blue = br.ReadUInt16(); //2
                        c.wavePacketDescriptorIndex = br.ReadByte(); //1
                        c.byteOffsetToWaveformData = br.ReadUInt64(); //8
                        c.waveformPacketSizeInBytes = br.ReadUInt32(); //4
                        c.returnPointWaveformLocation = br.ReadSingle(); //4
                        c.Xt = br.ReadSingle(); //4
                        c.Yt = br.ReadSingle(); //4
                        c.Zt = br.ReadSingle(); //4
                        return c;
                }
                break;


        }
        print("Error in reading version! Confirm you are using 1.2, 1.3, or 1.4!");
        return new PointData();
    }

}
