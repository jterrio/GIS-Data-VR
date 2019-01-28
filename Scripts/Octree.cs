using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public enum OctreeIndex {

    // 0-1 RELATIONSHIP
    // BOTTOM-TOP
    // LEFT-RIGHT
    // FRONT-BACK

    BottomLeftFront = 0, //000
    BottomRightFront = 2, //010
    BottomRightBack = 3, //011
    BottomLeftBack = 1, //001
    TopLeftFront = 4, //100
    TopRightFront = 6, //110
    TopRightBack = 7, //111
    TopLeftBack = 5 //101
}

[System.Serializable]
public class Octree {
    private OctreeNode node; //root
    private int maxPointSize;
    public int currentMaxDepth = 1;
    public int currentLeaves = 1;
    public float smallestTile;
    public bool hasSplit = true; //set to true for init

    public Octree(Vector3 position, float size, int maxPointSize) {
        node = new OctreeNode(position, size, "0", this);
        this.maxPointSize = maxPointSize;
        smallestTile = size;
    }

    /// <summary>
    /// Returns the current maximum number of points allowed to exist in a single node
    /// </summary>
    public int MaxPoints {
        get { return maxPointSize; }
    }

    /// <summary>
    /// Returns the current max depth of the tree
    /// </summary>
    public int CurrentMaxDepth {
        get { return currentMaxDepth; }
        set { currentMaxDepth = value; }
    }

    /// <summary>
    /// Returns the smallest tile size given at the deepest node in the tree
    /// </summary>
    public float SmallestTile {
        get { return smallestTile; }
        set { smallestTile = value; }
    }

    public class OctreeNode {
        //bounds of cube of node
        private Octree tree;
        private Vector3 position;
        private float size;
        private string index;
        private int pointCount;

        //children
        private OctreeNode[] subNodes;
        private List<GISDefinitions.PointData> data;

        public OctreeNode(Vector3 pos, float size, string index, Octree tree) {
            this.tree = tree;
            position = pos;
            this.size = size;
            this.index = index;
            data = new List<GISDefinitions.PointData>();
        }

        public Vector3 FindCoordinateOnOctree(Vector3 point) {
            Vector3 returnVector = Vector3.zero;
            int depth = (int)Mathf.Pow(2, (tree.currentMaxDepth - 2));
            switch (GetIndexOfPosition(point)) {
                case 0:
                    //ADD (0, 0, 0)
                    if (subNodes[0].IsLeaf()) {
                        return returnVector;
                    }
                    return subNodes[0].FindCoordinateOnOctreeRecursive(returnVector, point, Mathf.FloorToInt(depth / 2));
                case 1:
                    //ADD (0, 0, N)
                    returnVector = new Vector3(returnVector.x, returnVector.y, returnVector.z + depth);
                    if (subNodes[1].IsLeaf()) {
                        return returnVector;
                    }
                    return subNodes[1].FindCoordinateOnOctreeRecursive(returnVector, point, Mathf.FloorToInt(depth / 2));
                case 2:
                    //ADD (N, 0, 0)
                    returnVector = new Vector3(returnVector.x + depth, returnVector.y, returnVector.z);
                    if (subNodes[2].IsLeaf()) {
                        return returnVector;
                    }
                    return subNodes[2].FindCoordinateOnOctreeRecursive(returnVector, point, Mathf.FloorToInt(depth / 2));
                case 3:
                    //ADD (N, 0, N)
                    returnVector = new Vector3(returnVector.x + depth, returnVector.y, returnVector.z + depth);
                    if (subNodes[3].IsLeaf()) {
                        return returnVector;
                    }
                    return subNodes[3].FindCoordinateOnOctreeRecursive(returnVector, point, Mathf.FloorToInt(depth / 2));
                case 4:
                    //ADD (0, N, 0)
                    returnVector = new Vector3(returnVector.x, returnVector.y + depth, returnVector.z);
                    if (subNodes[4].IsLeaf()) {
                        return returnVector;
                    }
                    return subNodes[4].FindCoordinateOnOctreeRecursive(returnVector, point, Mathf.FloorToInt(depth / 2));
                case 5:
                    //ADD (0, N, N)
                    returnVector = new Vector3(returnVector.x, returnVector.y + depth, returnVector.z + depth);
                    if (subNodes[5].IsLeaf()) {
                        return returnVector;
                    }
                    return subNodes[5].FindCoordinateOnOctreeRecursive(returnVector, point, Mathf.FloorToInt(depth / 2));
                case 6:
                    //ADD (N, N, 0)
                    returnVector = new Vector3(returnVector.x + depth, returnVector.y + depth, returnVector.z);
                    if (subNodes[6].IsLeaf()) {
                        return returnVector;
                    }
                    return subNodes[6].FindCoordinateOnOctreeRecursive(returnVector, point, Mathf.FloorToInt(depth / 2));
                case 7:
                    //ADD (N, N, N)
                    returnVector = new Vector3(returnVector.x + depth, returnVector.y + depth, returnVector.z + depth);
                    if (subNodes[7].IsLeaf()) {
                        return returnVector;
                    }
                    return subNodes[7].FindCoordinateOnOctreeRecursive(returnVector, point, Mathf.FloorToInt(depth / 2));
                default:
                    //ADD (N, N, N)
                    returnVector = new Vector3(returnVector.x + depth, returnVector.y + depth, returnVector.z + depth);
                    if (subNodes[7].IsLeaf()) {
                        return returnVector;
                    }
                    return subNodes[7].FindCoordinateOnOctreeRecursive(returnVector, point, Mathf.FloorToInt(depth / 2));
            }
        }

        Vector3 FindCoordinateOnOctreeRecursive(Vector3 returnVector, Vector3 point, int depth) {
            Vector3 toReturnVector;
            switch (GetIndexOfPosition(point)) {
                case 0:
                    //ADD (0, 0, 0)
                    if (subNodes[0].IsLeaf()) {
                        return returnVector;
                    }
                    return subNodes[0].FindCoordinateOnOctreeRecursive(returnVector, point, depth / 2);
                case 1:
                    //ADD (0, 0, N)
                    toReturnVector = new Vector3(returnVector.x, returnVector.y, returnVector.z + depth);
                    if (subNodes[1].IsLeaf()) {
                        return toReturnVector;
                    }
                    return subNodes[1].FindCoordinateOnOctreeRecursive(toReturnVector, point, depth / 2);
                case 2:
                    //ADD (N, 0, 0)
                    toReturnVector = new Vector3(returnVector.x + depth, returnVector.y, returnVector.z);
                    if (subNodes[2].IsLeaf()) {
                        return toReturnVector;
                    }
                    return subNodes[2].FindCoordinateOnOctreeRecursive(toReturnVector, point, depth / 2);
                case 3:
                    //ADD (N, 0, N)
                    toReturnVector = new Vector3(returnVector.x + depth, returnVector.y, returnVector.z + depth);
                    if (subNodes[3].IsLeaf()) {
                        return toReturnVector;
                    }
                    return subNodes[3].FindCoordinateOnOctreeRecursive(toReturnVector, point, depth / 2);
                case 4:
                    //ADD (0, N, 0)
                    toReturnVector = new Vector3(returnVector.x, returnVector.y + depth, returnVector.z);
                    if (subNodes[4].IsLeaf()) {
                        return toReturnVector;
                    }
                    return subNodes[4].FindCoordinateOnOctreeRecursive(toReturnVector, point, depth / 2);
                case 5:
                    //ADD (0, N, N)
                    toReturnVector = new Vector3(returnVector.x, returnVector.y + depth, returnVector.z + depth);
                    if (subNodes[5].IsLeaf()) {
                        return toReturnVector;
                    }
                    return subNodes[5].FindCoordinateOnOctreeRecursive(toReturnVector, point, depth / 2);
                case 6:
                    //ADD (N, N, 0)
                    toReturnVector = new Vector3(returnVector.x + depth, returnVector.y + depth, returnVector.z);
                    if (subNodes[6].IsLeaf()) {
                        return toReturnVector;
                    }
                    return subNodes[6].FindCoordinateOnOctreeRecursive(toReturnVector, point, depth / 2);
                case 7:
                    //ADD (N, N, N)
                    toReturnVector = new Vector3(returnVector.x + depth, returnVector.y + depth, returnVector.z + depth);
                    if (subNodes[7].IsLeaf()) {
                        return toReturnVector;
                    }
                    return subNodes[7].FindCoordinateOnOctreeRecursive(toReturnVector, point, depth / 2);
                default:
                    //ADD (N, N, N)
                    toReturnVector = new Vector3(returnVector.x + depth, returnVector.y + depth, returnVector.z + depth);
                    if (subNodes[7].IsLeaf()) {
                        return toReturnVector;
                    }
                    return subNodes[7].FindCoordinateOnOctreeRecursive(toReturnVector, point, depth / 2);
            }
        }

        public OctreeNode FindLeafOnOctree(Vector3 point) {
            Vector3 returnVector = Vector3.zero;
            int depth = (int)Mathf.Pow(2, (tree.currentMaxDepth - 2));
            switch (GetIndexOfPosition(point)) {
                case 0:
                    //ADD (0, 0, 0)
                    if (subNodes[0].IsLeaf()) {
                        return subNodes[0];
                    }
                    return subNodes[0].FindLeafOnOctreeRecursive(returnVector, point, Mathf.FloorToInt(depth / 2));
                case 1:
                    //ADD (0, 0, N)
                    returnVector = new Vector3(returnVector.x, returnVector.y, returnVector.z + depth);
                    if (subNodes[1].IsLeaf()) {
                        return subNodes[1];
                    }
                    return subNodes[1].FindLeafOnOctreeRecursive(returnVector, point, Mathf.FloorToInt(depth / 2));
                case 2:
                    //ADD (N, 0, 0)
                    returnVector = new Vector3(returnVector.x + depth, returnVector.y, returnVector.z);
                    if (subNodes[2].IsLeaf()) {
                        return subNodes[2];
                    }
                    return subNodes[2].FindLeafOnOctreeRecursive(returnVector, point, Mathf.FloorToInt(depth / 2));
                case 3:
                    //ADD (N, 0, N)
                    returnVector = new Vector3(returnVector.x + depth, returnVector.y, returnVector.z + depth);
                    if (subNodes[3].IsLeaf()) {
                        return subNodes[3];
                    }
                    return subNodes[3].FindLeafOnOctreeRecursive(returnVector, point, Mathf.FloorToInt(depth / 2));
                case 4:
                    //ADD (0, N, 0)
                    returnVector = new Vector3(returnVector.x, returnVector.y + depth, returnVector.z);
                    if (subNodes[4].IsLeaf()) {
                        return subNodes[4];
                    }
                    return subNodes[4].FindLeafOnOctreeRecursive(returnVector, point, Mathf.FloorToInt(depth / 2));
                case 5:
                    //ADD (0, N, N)
                    returnVector = new Vector3(returnVector.x, returnVector.y + depth, returnVector.z + depth);
                    if (subNodes[5].IsLeaf()) {
                        return subNodes[5];
                    }
                    return subNodes[5].FindLeafOnOctreeRecursive(returnVector, point, Mathf.FloorToInt(depth / 2));
                case 6:
                    //ADD (N, N, 0)
                    returnVector = new Vector3(returnVector.x + depth, returnVector.y + depth, returnVector.z);
                    if (subNodes[6].IsLeaf()) {
                        return subNodes[6];
                    }
                    return subNodes[6].FindLeafOnOctreeRecursive(returnVector, point, Mathf.FloorToInt(depth / 2));
                case 7:
                    //ADD (N, N, N)
                    returnVector = new Vector3(returnVector.x + depth, returnVector.y + depth, returnVector.z + depth);
                    if (subNodes[7].IsLeaf()) {
                        return subNodes[7];
                    }
                    return subNodes[7].FindLeafOnOctreeRecursive(returnVector, point, Mathf.FloorToInt(depth / 2));
                default:
                    //ADD (N, N, N)
                    returnVector = new Vector3(returnVector.x + depth, returnVector.y + depth, returnVector.z + depth);
                    if (subNodes[7].IsLeaf()) {
                        return subNodes[7];
                    }
                    return subNodes[7].FindLeafOnOctreeRecursive(returnVector, point, Mathf.FloorToInt(depth / 2));
            }
        }

        OctreeNode FindLeafOnOctreeRecursive(Vector3 returnVector, Vector3 point, int depth) {
            Vector3 toReturnVector;
            switch (GetIndexOfPosition(point)) {
                case 0:
                    //ADD (0, 0, 0)
                    if (subNodes[0].IsLeaf()) {
                        return subNodes[0];
                    }
                    return subNodes[0].FindLeafOnOctreeRecursive(returnVector, point, depth / 2);
                case 1:
                    //ADD (0, 0, N)
                    toReturnVector = new Vector3(returnVector.x, returnVector.y, returnVector.z + depth);
                    if (subNodes[1].IsLeaf()) {
                        return subNodes[1];
                    }
                    return subNodes[1].FindLeafOnOctreeRecursive(toReturnVector, point, depth / 2);
                case 2:
                    //ADD (N, 0, 0)
                    toReturnVector = new Vector3(returnVector.x + depth, returnVector.y, returnVector.z);
                    if (subNodes[2].IsLeaf()) {
                        return subNodes[2];
                    }
                    return subNodes[2].FindLeafOnOctreeRecursive(toReturnVector, point, depth / 2);
                case 3:
                    //ADD (N, 0, N)
                    toReturnVector = new Vector3(returnVector.x + depth, returnVector.y, returnVector.z + depth);
                    if (subNodes[3].IsLeaf()) {
                        return subNodes[3];
                    }
                    return subNodes[3].FindLeafOnOctreeRecursive(toReturnVector, point, depth / 2);
                case 4:
                    //ADD (0, N, 0)
                    toReturnVector = new Vector3(returnVector.x, returnVector.y + depth, returnVector.z);
                    if (subNodes[4].IsLeaf()) {
                        return subNodes[4];
                    }
                    return subNodes[4].FindLeafOnOctreeRecursive(toReturnVector, point, depth / 2);
                case 5:
                    //ADD (0, N, N)
                    toReturnVector = new Vector3(returnVector.x, returnVector.y + depth, returnVector.z + depth);
                    if (subNodes[5].IsLeaf()) {
                        return subNodes[5];
                    }
                    return subNodes[5].FindLeafOnOctreeRecursive(toReturnVector, point, depth / 2);
                case 6:
                    //ADD (N, N, 0)
                    toReturnVector = new Vector3(returnVector.x + depth, returnVector.y + depth, returnVector.z);
                    if (subNodes[6].IsLeaf()) {
                        return subNodes[6];
                    }
                    return subNodes[6].FindLeafOnOctreeRecursive(toReturnVector, point, depth / 2);
                case 7:
                    //ADD (N, N, N)
                    toReturnVector = new Vector3(returnVector.x + depth, returnVector.y + depth, returnVector.z + depth);
                    if (subNodes[7].IsLeaf()) {
                        return subNodes[7];
                    }
                    return subNodes[7].FindLeafOnOctreeRecursive(toReturnVector, point, depth / 2);
                default:
                    //ADD (N, N, N)
                    toReturnVector = new Vector3(returnVector.x + depth, returnVector.y + depth, returnVector.z + depth);
                    if (subNodes[7].IsLeaf()) {
                        return subNodes[7];
                    }
                    return subNodes[7].FindLeafOnOctreeRecursive(toReturnVector, point, depth / 2);
            }
        }

        public IEnumerable<OctreeNode> Nodes {
            get { return subNodes; }
        }

        public Vector3 Position {
            get { return position; }
        }

        public float Size {
            get { return size; }
        }

        public List<GISDefinitions.PointData> Points {
            get { return data;  }
        }

        public int PointCount {
            get { return pointCount; }
        }

        /// <summary>
        /// Expand every leaf to the given depth
        /// </summary>
        /// <param name="depth">Depth to expand every leaf to</param>
        public void ExpandTreeDepth(int depth) {
            pointCount = 0;
            if (index.Length >= depth) {
                return;
            }
            if (subNodes == null) {
                Subdivide();
            }
            foreach (OctreeNode leaf in subNodes) {
                if (leaf.index.Length < depth) {
                    if (leaf.IsLeaf()) {
                        leaf.Subdivide();
                    }
                    leaf.ExpandTreeDepth(depth);
                }
            }
        }

        /// <summary>
        /// Expands the tree as if the point as actually added
        /// </summary>
        /// <param name="point">Point to simulate expansion</param>
        /// <param name="maxPoints">Maximum number of points in a node</param>
        public void ExpandTree(GISData.PointData point, int maxPoints) {
            int newIndex;
            if (IsLeaf()) {
                if (pointCount >= maxPoints) { //split
                    tree.hasSplit = true;
                    Subdivide();
                    pointCount = 0;
                    newIndex = GetIndexOfPosition(point.LocalPosition);
                    //go down path for point
                    subNodes[newIndex].ExpandTree(point, maxPoints);
                } else { //add
                    pointCount++;
                }
            } else {
                newIndex = GetIndexOfPosition(point.LocalPosition);
                //go down path
                subNodes[newIndex].ExpandTree(point, maxPoints);
            }
        }


        public OctreeNode GetLeafFromExpandedTree(GISData.PointData point) {
            if (IsLeaf()) {
                return this;
            } else {
                int newIndex = GetIndexOfPosition(point.LocalPosition);
                return subNodes[newIndex].GetLeafFromExpandedTree(point);
            }
        }

        /// <summary>
        /// Split the Octree at the given index and return the new node
        /// </summary>
        /// <param name="i">Index to split the Octree at</param>
        /// <returns>OctreeNode to become a new leaf</returns>
        OctreeNode Subdivide(int i) {
            
            Vector3 newPos = position;
            if ((i & 4) == 4) {
                newPos.y += size * 0.25f;
            } else {
                newPos.y -= size * 0.25f;
            }
            if ((i & 2) == 2) {
                newPos.x += size * 0.25f;
            } else {
                newPos.x -= size * 0.25f;
            }
            if ((i & 1) == 1) {
                newPos.z += size * 0.25f;
            } else {
                newPos.z -= size * 0.25f;
            }
            if(size * 0.5f < tree.SmallestTile) {
                tree.SmallestTile = size * 0.5f;
            }
            //Debug.Log(newPos);
            return new OctreeNode(newPos, size * 0.5f, (index + i.ToString()), tree);
        }

        /// <summary>
        /// Split Octree leaf
        /// </summary>
        void Subdivide() {
            tree.currentLeaves += 7;
            subNodes = new OctreeNode[8];
            for(int i = 0; i < 8; i++) {
                subNodes[i] = Subdivide(i);
                if (subNodes[i].index.Length > tree.CurrentMaxDepth) {
                    tree.CurrentMaxDepth = subNodes[i].index.Length;
                }
            }
        }


        

        
        /// <summary>
        /// Returns true if there are no sub-nodes below this one
        /// </summary>
        /// <returns></returns>
        public bool IsLeaf() {
            if (subNodes == null) {
                return true;
            }
            return false;
        }

        private int GetIndexOfPosition(Vector3 lookupPosition) {
            int index = 0;
            if(lookupPosition.x > position.x) { //RIGHT
                if(lookupPosition.y > position.y) { //RIGHT-TOP
                    if(lookupPosition.z > position.z) { //RIGHT-TOP-BACK
                        index = 7;
                    } else { //RIGHT-TOP-FRONT
                        index = 6;
                    }
                } else { //RIGHT-BOTTOM
                    if (lookupPosition.z > position.z) { //RIGHT-BOTTOM-BACK
                        index = 3;
                    } else { //RIGHT-BOTTOM-FRONT
                        index = 2;
                    }
                }
            } else { //LEFT
                if (lookupPosition.y > position.y) { //LEFT-TOP

                    if (lookupPosition.z > position.z) { //LEFT-TOP-BACK
                        index = 5;
                    } else { //LEFT-TOP-FRONT
                        index = 4;
                    }
                } else { //LEFT-BOTTOM
                    if (lookupPosition.z > position.z) { //LEFT-BOTTOM-BACK
                        index = 1;
                    } else { //LEFT-BOTTOM-FRONT
                        index = 0;
                    }

                }

            }

            return index;
        }


        private int GetIndexOfPositionDebug(Vector3 lookupPosition) {
            int index = 0;
            if (lookupPosition.x > position.x) { //RIGHT
                if (lookupPosition.y > position.y) { //RIGHT-TOP
                    if (lookupPosition.z > position.z) { //RIGHT-TOP-BACK
                        index = 7;
                    } else { //RIGHT-TOP-FRONT
                        index = 6;
                    }
                } else { //RIGHT-BOTTOM
                    if (lookupPosition.z > position.z) { //RIGHT-BOTTOM-BACK
                        index = 3;
                    } else { //RIGHT-BOTTOM-FRONT
                        index = 2;
                    }
                }
            } else { //LEFT
                if (lookupPosition.y > position.y) { //LEFT-TOP

                    if (lookupPosition.z > position.z) { //LEFT-TOP-BACK
                        index = 5;
                    } else { //LEFT-TOP-FRONT
                        index = 4;
                    }
                } else { //LEFT-BOTTOM
                    if (lookupPosition.z > position.z) { //LEFT-BOTTOM-BACK
                        index = 1;
                    } else { //LEFT-BOTTOM-FRONT
                        index = 0;
                    }

                }

            }
            return index;
        }

        public OctreeNode GetNodeAtCoordinate(Vector3 coordinate) {
            OctreeNode returnNode = this;
            List<int> path = GetPathToCoordinate(coordinate);
            while (!returnNode.IsLeaf()) {
                int i = path[0];
                path.RemoveAt(0);
                returnNode = returnNode.subNodes[i];
            }

            return returnNode;
        }

        public OctreeNode GetNodeAtCoordinateDepth(Vector3 coordinate, int depth) {
            OctreeNode returnNode = this;
            List<int> path = GetPathToCoordinate(coordinate);
            int d = 0;
            while (!returnNode.IsLeaf() || d == depth) {
                d++;
                int i = path[0];
                path.RemoveAt(0);
                returnNode = returnNode.subNodes[i];
            }

            return returnNode;
        }

        List<int> GetPathToCoordinate(Vector3 coordinate) {
            List<int> path = new List<int>();

            int x = (int)coordinate.x;
            int y = (int)coordinate.y;
            int z = (int)coordinate.z;

            List<int> xComps = new List<int>();
            List<int> yComps = new List<int>();
            List<int> zComps = new List<int>();
            int initDepth = (int)Mathf.Pow(2, tree.currentMaxDepth - 2);

            //break down X
            int depthCalc = initDepth;
            while (true) {
                int index = x - depthCalc;
                if (index >= 0) {
                    x -= depthCalc;
                    xComps.Add(depthCalc);
                }
                if(depthCalc == 1) {
                    break;
                }
                depthCalc = depthCalc / 2;
            }

            //break down Y
            depthCalc = initDepth;
            while (true) {
                int index = y - depthCalc;
                if (index >= 0) {
                    y -= depthCalc;
                    yComps.Add(depthCalc);
                }
                if (depthCalc == 1) {
                    break;
                }
                depthCalc = depthCalc / 2;
            }

            //break down Z
            depthCalc = initDepth;
            while (true) {
                int index = z - depthCalc;
                if (index >= 0) {
                    z -= depthCalc;
                    zComps.Add(depthCalc);
                }
                if (depthCalc == 1) {
                    break;
                }
                depthCalc = depthCalc / 2;
            }


            //Solve duplicates
            depthCalc = initDepth;
            bool xHas = false;
            bool yHas = false;
            bool zHas = false;
            while (depthCalc != 0) {
                if (xComps.Contains(depthCalc)) {
                    xHas = true;
                }
                if (yComps.Contains(depthCalc)) {
                    yHas = true;
                }
                if (zComps.Contains(depthCalc)) {
                    zHas = true;
                }
                if (!xHas && !yHas && !zHas) { //(0, 0, 0) - 0
                    path.Add(0);
                } else if (!xHas && !yHas && zHas) { //(0, 0, N) - 1
                    path.Add(1);
                } else if (xHas && !yHas && !zHas) { //(N, 0, 0) - 2
                    path.Add(2);
                } else if (xHas && !yHas && zHas) { //(N, 0, N) - 3
                    path.Add(3);
                } else if (!xHas && yHas && !zHas) { //(0, N, 0) - 4
                    path.Add(4);
                } else if (!xHas && yHas && zHas) { //(0, N, N) - 5
                    path.Add(5);
                } else if (xHas && yHas && !zHas) { //(N, N, 0) - 6
                    path.Add(6);
                } else if (xHas && yHas && zHas) { //(N, N, N) - 7
                    path.Add(7);
                }

                if (depthCalc == 1) {
                    break;
                }
                depthCalc = depthCalc / 2;

                xHas = false;
                yHas = false;
                zHas = false;
            }

            return path;
        }

    }

    private int GetIndexOfPosition(Vector3 lookupPosition, Vector3 nodePosition) {
        int index = 0;
        


        return index;
    }


    public OctreeNode GetRoot() {
        return node;
    }
}


