using System.Collections;
using System.Collections.Generic;
using System.IO;
using System;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.UI;

public class GISData : MonoBehaviour {

    public string fileName;
    public string fileType;
    protected string path;
    public Header header;
    public GameObject point;
    public List<Point> points = new List<Point>();


    [System.Serializable]
    public struct Header {
        public char[] fileSignature; //
        public ushort fileSourceID; //
        public ushort globalEncoding; //
        public uint projectID1;
        public ushort projectID2;
        public ushort projectID3;
        public char[] projectID4;// = new char[8];
        public byte versionMajor;
        public byte versionMinor;
        public char[] systemIdentifier;// = new char[32];
        public char[] generatingSoftware;// = new char[32];
        public ushort creationDayOfYear;
        public ushort creationYear;
        public ushort headerSize;
        public uint offsetToPointData;
        public uint numberOfVariableLengthRecords;
        public byte pointDataRecordFormat;
        public ushort pointDataRecordLength;
        public uint legacyNumberOfPointRecords;
        public uint[] legacyNumberOfPointsByReturn;// = new ulong[5];
        public double xScaleFactor;
        public double yScaleFactor;
        public double zScaleFactor;
        public double xOffset;
        public double yOffset;
        public double zOffset;
        public double xMax;
        public double xMin;
        public double yMax;
        public double yMin;
        public double zMax;
        public double zMin;

        //1.3 + 1.4 only
        public ulong startOfWaveformDataPacketRecord;

        //1.4 only
        public ulong startOfFirstExtendedVariableLengthRecord;
        public uint numberOfExtendedVariableLengthRecords;
        public ulong numberOfPointRecords;
        public ulong[] numberOfPointsByReturn;

    }

    public class Point {
        public GameObject pointObject;
        protected Vector3 localPosition;
        public Vector3 coordinates;

        public Vector3 LocalPosition {
            get {
                return localPosition;
            }
            set {
                localPosition = value;
                pointObject.transform.position = value;
            }
        }
    }

    [System.Serializable]
    public class classification0_1Point2 : Point {
        public ushort intensity;
        //need some bit fields here


    }

    [System.Serializable]
    public class classification1_1Point2 : Point {
    }

    [System.Serializable]
    public class classification2_1Point2 : Point {

    }

    [System.Serializable]
    public class classification3_1Point2 : Point {

    }

    [System.Serializable]
    public class classification4_1Point2 : Point {

    }

    [System.Serializable]
    public class classification5_1Point2 : Point {

    }

    [System.Serializable]
    public class classification6_1Point2 : Point {

    }

    [System.Serializable]
    public class classification7_1Point2 : Point {

    }

    [System.Serializable]
    public class classification8_1Point2 : Point {

    }

    [System.Serializable]
    public class classification9_1Point2 : Point {

    }

    [System.Serializable]
    public class classification10_1Point2 : Point {

    }

    [System.Serializable]
    public class classification11_1Point2 : Point {

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

        BinaryReader br = new BinaryReader(File.Open(path, FileMode.Open));
        ReadHeader(br);
        ReadPoints(br);
    }


    void ReadHeader(BinaryReader br) {
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

    void ReadPoints(BinaryReader br) {
        print("Creating Points...");
        br.ReadBytes((int)header.offsetToPointData - (int)header.headerSize);


        //create origin for normalization
        Vector3 originReference = new Vector3(br.ReadInt32() / 100, br.ReadInt32() / 100, br.ReadInt32() / 100);
        print("Origin at: " + originReference);
        GameObject originObject = Instantiate(point);
        Point p = CreatePointType(br);

        //add origin point info to points
        p.pointObject = originObject;
        p.LocalPosition = Vector3.zero;
        p.coordinates = originReference;
        points.Add(p);
        br.ReadBytes(header.pointDataRecordLength - 12);


        print("Start time: " + System.DateTime.Now);
        //create other points around origin
        for (int i = 0; i < (header.legacyNumberOfPointRecords - 1); i++) { //(header.legacyNumberOfPointRecords - 1)
            GameObject t = Instantiate(point);
            p = CreatePointType(br);

            p.pointObject = t;
            p.coordinates = new Vector3(br.ReadInt32() / 100, br.ReadInt32() / 100, br.ReadInt32() / 100);
            br.ReadBytes(header.pointDataRecordLength - 12);
            print(i);
            print(p.coordinates);
            print(originReference);
            p.LocalPosition = Normalize(originReference, p.coordinates);
            points.Add(p);
            //p.transform.localScale = new Vector3((float)header.xScaleFactor, (float)header.yScaleFactor, (float)header.zScaleFactor);
        }


        print("Finished creating points!");
        print("Finish time: " + System.DateTime.Now);

    }


    Vector3 Normalize(Vector3 origin, Vector3 point) {
        return new Vector3(point.x - origin.x, point.y - origin.y, point.z - origin.z);
    }

    Point CreatePointType(BinaryReader br) {
        switch (header.versionMinor) {
            //version 1.2
            case 2:

                switch (header.pointDataRecordFormat) {
                    case 0:
                        break;
                    case 1:
                        classification1_1Point2 c = new classification1_1Point2();
                        //fill info about c with br
                        //br.ReadBytes(header.pointDataRecordLength - 12);
                        return c;
                }

                break;


            case 3:

                break;

            case 4:

                break;


        }

        return new Point();
    }

}
