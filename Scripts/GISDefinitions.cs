using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GISDefinitions : MonoBehaviour {

    /// <summary>
    /// Header structure
    /// </summary>
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

    /// <summary>
    /// Point class
    /// </summary>
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

    /// <summary>
    /// Returns the size of the point
    /// </summary>
    /// <param name="versionMajor"></param>
    /// <param name="versionMinor"></param>
    /// <param name="pointDataRecordFormat"></param>
    /// <returns></returns>
    public int GetSizeOfPoint(int versionMajor, int versionMinor, int pointDataRecordFormat) {
        return 1 + (sizeof(double) * 3);
    }


    /// <summary>
    /// Contains classification info and format each point
    /// </summary>
    [System.Serializable]
    public class PointData : Point {

        public byte classification; //see classification class; defines the point



        void AddReturnInformation(BitArray b) {

        }

        void AddReturnInformationExtended(BitArray b) {

        }
    }

}
