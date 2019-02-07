using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GISDefinitions : MonoBehaviour {

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

    [System.Serializable]
    public class Point {
        protected Vector3 localPosition;
        public Vector3 coordinates;

        public Vector3 LocalPosition {
            get {
                return localPosition;
            }
            set {
                localPosition = value;
            }
        }
    }

    public int GetSizeOfPoint(int versionMajor, int versionMinor, int pointDataRecordFormat) {
        //t toReturn = 0;
        return 1 + (sizeof(double) * 3);
        /*
        switch (versionMinor) {
            //version 1.2
            case 2:
                //8
                //get the format
                switch (pointDataRecordFormat) {
                    case 0:
                        toReturn = 8;
                        break;
                    case 1:
                        toReturn = 16;
                        break;
                    case 2:
                        toReturn = 14;
                        break;
                    case 3:
                        toReturn = 22;
                        break;
                }
                break;
            case 3:
                //8
                //get the format
                switch (pointDataRecordFormat) {
                    case 0:
                        toReturn = 8;
                        break;
                    case 1:
                        toReturn = 16;
                        break;
                    case 2:
                        toReturn = 14;
                        break;
                    case 3:
                        toReturn = 22;
                        break;
                    case 4:
                        toReturn = 45;
                        break;
                    case 5:
                        toReturn = 51;
                        break;
                }

                break;

            case 4:
                //8 
                //12 if >=6
                //get the format
                switch (pointDataRecordFormat) {
                    case 0:
                        toReturn = 8;
                        break;
                    case 1:
                        toReturn =  16;
                        break;
                    case 2:
                        toReturn =  14;
                        break;
                    case 3:
                        toReturn =  22;
                        break;
                    case 4:
                        toReturn =  45;
                        break;
                    case 5:
                        toReturn =  51;
                        break;
                    case 6:
                        toReturn =  20;
                        break;
                    case 7:
                        toReturn =  26;
                        break;
                    case 8:
                        toReturn =  28;
                        break;
                    case 9:
                        toReturn =  49;
                        break;
                    case 10:
                        toReturn =  55;
                        break;
                }
                break;


        }
        return toReturn + (sizeof(double) * 3);
        */
    }


    //contains classification info and format each point
    [System.Serializable]
    public class PointData : Point {

        public byte classification; //see classification class; defines the point



        void AddReturnInformation(BitArray b) {

        }

        void AddReturnInformationExtended(BitArray b) {

        }
    }

}
