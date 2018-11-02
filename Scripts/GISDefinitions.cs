﻿using System.Collections;
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
        public GameObject pointObject;
        protected Vector3 localPosition;
        public Vector3 coordinates;

        public Vector3 LocalPosition {
            get {
                return localPosition;
            }
            set {
                localPosition = value;
                if (pointObject != null) {
                    pointObject.transform.position = value;
                }
            }
        }
    }


    //contains classification info and format each point
    [System.Serializable]
    public class PointData : Point {

        //VERSION 1.2+
        
        public ushort intensity; //integer representation of the pulse return magnitude

        public byte returnInformation; //includes returnNumber, numberOfReturns, scanDirectionFlag, and edgeOfFlightLine
        public BitArray[] returnNumber = new BitArray[3];
        public BitArray[] numberOfReturns = new BitArray[3];
        public BitArray[] scanDirectionFlag = new BitArray[1];
        public BitArray[] edgeOfFlightLine = new BitArray[1];

        public byte classification; //see classification class; defines the point
        public byte scanAngleRank; //(-90 to +90) - Left Side
        public byte userData; //optional user data
        public ushort pointSourceID; //file from which point originated

        public double GPSTime; //double floating point time tag value at which the point was acquired
        public ushort red, green, blue; //used for "colorizing" a LIDAR point using ancillary data, typically from a camera

        //VERSION 1.3+
        public byte wavePacketDescriptorIndex; //indicates user defined record used to describe waveform packed associated with point (0 = no data; goes to 255)
        public ulong byteOffsetToWaveformData; //location of the start of this LIDAR points' waveform packet (From beg of file: Start of Waveform Data Packet Record + Byte Offset to Waveform Packet Data)
        public uint waveformPacketSizeInBytes; //size of eah waveform packet
        public float returnPointWaveformLocation; //offset in picoseconds (10 ^ -12) from the first digitized value to the location within the waveform packet
        //define parametic line equation for extrapolating points along associated waveform
        public float Xt;
        public float Yt;
        public float Zt;


        //VERSION 1.4+
        public byte returnInformationExtended; //in 1.4, returnInformation contains only return number and number of returns (given pulse) and this will contain classification flags, scanner channel,  scan direction flag, and edge of flight line
        public BitArray[] returnNumberExtended = new BitArray[4];
        public BitArray[] numberOfReturnsExtended = new BitArray[4];
        public BitArray[] classificationFlags = new BitArray[4];
        public BitArray[] scanDirectionFlagExtended = new BitArray[2];
        public BitArray[] edgeOfFlightLineExtended = new BitArray[2];

        public ushort NIR; //near infrared channel value



        void AddReturnInformation(BitArray b) {

        }

        void AddReturnInformationExtended(BitArray b) {

        }
    }

}