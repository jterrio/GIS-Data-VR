using System.Collections;
using System.Collections.Generic;
using System.IO;
using System;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.UI;

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


    void SetOctreeBase() {
        Vector3 min = new Vector3((float)header.xMin, (float)header.yMin, (float)header.zMin);
        Vector3 max = new Vector3((float)header.xMax, (float)header.yMax, (float)header.zMax);
        float[] ranges = new float[] { (float)(max.x - min.x), (float)(max.y - min.y), (float)(max.z - min.z) };
        octree = new Octree(Vector3.zero, Mathf.Max(ranges), 1000);
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

    public void ReadPoints() {
        print("Creating Points...");
        br.ReadBytes((int)header.offsetToPointData - (int)header.headerSize);

        //xyz
        float x, y, z;
        //create origin for normalization
        Vector3 min = new Vector3((float)header.xMin, (float)header.yMin, (float)header.zMin);
        Vector3 max = new Vector3((float)header.xMax, (float)header.yMax, (float)header.zMax);
        Vector3 origin = new Vector3(((max.x + min.x) / 2), ((max.y + min.y) / 2) ,((max.z + min.z) / 2));

        print("Origin at: " + origin);

        PointData p;
        print("Start time: " + System.DateTime.Now);
        //create other points around origin
        for (int i = 0; i < (header.legacyNumberOfPointRecords); i++) { //(header.legacyNumberOfPointRecords - 1)

            if(maxPoints != 0 && i > maxPoints - 1) {
                break;
            }
            x = br.ReadInt32();
            y = br.ReadInt32();
            z = br.ReadInt32();
            p = CreatePointType();
            p.coordinates = new Vector3((x * (float)header.xScaleFactor) + (float)header.xOffset, (y * (float)header.yScaleFactor) + (float)header.yOffset, (z * (float)header.zScaleFactor) + (float)header.zOffset);
            p.LocalPosition = Normalize(origin, p.coordinates);
            //points.Add(p);
            octree.GetRoot().AddPoint(p, octree.MaxPoints);
        }


        print("Finished creating points!");
        print("Finish time: " + System.DateTime.Now);

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
