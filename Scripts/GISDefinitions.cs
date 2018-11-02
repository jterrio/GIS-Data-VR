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

}
