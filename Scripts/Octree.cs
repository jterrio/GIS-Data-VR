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
    private int depth;
    private int maxPointSize;
    public int currentMaxDepth = 1;
    public int currentLeaves = 1;
    public float smallestTile;

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
            switch (GetIndexOfPosition(point)) {
                case 0:
                    //ADD (0, 0, 0)
                    if (subNodes[0].IsLeaf()) {
                        return returnVector;
                    }
                    return subNodes[0].FindCoordinateOnOctreeRecursive(returnVector, point, tree.depth / 2);
                case 1:
                    //ADD (0, 0, N)
                    returnVector = new Vector3(returnVector.x, returnVector.y, returnVector.z + tree.depth);
                    if (subNodes[1].IsLeaf()) {
                        return returnVector;
                    }
                    return subNodes[1].FindCoordinateOnOctreeRecursive(returnVector, point, tree.depth / 2);
                case 2:
                    //ADD (N, 0, 0)
                    returnVector = new Vector3(returnVector.x + tree.depth, returnVector.y, returnVector.z);
                    if (subNodes[2].IsLeaf()) {
                        return returnVector;
                    }
                    return subNodes[2].FindCoordinateOnOctreeRecursive(returnVector, point, tree.depth / 2);
                case 3:
                    //ADD (N, 0, N)
                    returnVector = new Vector3(returnVector.x + tree.depth, returnVector.y, returnVector.z + tree.depth);
                    if (subNodes[3].IsLeaf()) {
                        return returnVector;
                    }
                    return subNodes[3].FindCoordinateOnOctreeRecursive(returnVector, point, tree.depth / 2);
                case 4:
                    //ADD (0, N, 0)
                    returnVector = new Vector3(returnVector.x, returnVector.y + tree.depth, returnVector.z);
                    if (subNodes[4].IsLeaf()) {
                        return returnVector;
                    }
                    return subNodes[4].FindCoordinateOnOctreeRecursive(returnVector, point, tree.depth / 2);
                case 5:
                    //ADD (0, N, N)
                    returnVector = new Vector3(returnVector.x, returnVector.y + tree.depth, returnVector.z + tree.depth);
                    if (subNodes[5].IsLeaf()) {
                        return returnVector;
                    }
                    return subNodes[5].FindCoordinateOnOctreeRecursive(returnVector, point, tree.depth / 2);
                case 6:
                    //ADD (N, N, 0)
                    returnVector = new Vector3(returnVector.x + tree.depth, returnVector.y + tree.depth, returnVector.z);
                    if (subNodes[6].IsLeaf()) {
                        return returnVector;
                    }
                    return subNodes[6].FindCoordinateOnOctreeRecursive(returnVector, point, tree.depth / 2);
                case 7:
                    //ADD (N, N, N)
                    returnVector = new Vector3(returnVector.x + tree.depth, returnVector.y + tree.depth, returnVector.z + tree.depth);
                    if (subNodes[7].IsLeaf()) {
                        return returnVector;
                    }
                    return subNodes[7].FindCoordinateOnOctreeRecursive(returnVector, point, tree.depth / 2);
                default:
                    //ADD (N, N, N)
                    returnVector = new Vector3(returnVector.x + tree.depth, returnVector.y + tree.depth, returnVector.z + tree.depth);
                    if (subNodes[7].IsLeaf()) {
                        return returnVector;
                    }
                    return subNodes[7].FindCoordinateOnOctreeRecursive(returnVector, point, tree.depth / 2);
            }
        }

        Vector3 FindCoordinateOnOctreeRecursive(Vector3 returnVector, Vector3 point, int depth) {
            switch (GetIndexOfPosition(point)) {
                case 0:
                    //ADD (0, 0, 0)
                    if (subNodes[0].IsLeaf()) {
                        return returnVector;
                    }
                    return subNodes[0].FindCoordinateOnOctreeRecursive(returnVector, point, depth / 2);
                case 1:
                    //ADD (0, 0, N)
                    returnVector = new Vector3(returnVector.x, returnVector.y, returnVector.z + depth);
                    if (subNodes[1].IsLeaf()) {
                        return returnVector;
                    }
                    return subNodes[1].FindCoordinateOnOctreeRecursive(returnVector, point, depth / 2);
                case 2:
                    //ADD (N, 0, 0)
                    returnVector = new Vector3(returnVector.x + depth, returnVector.y, returnVector.z);
                    if (subNodes[2].IsLeaf()) {
                        return returnVector;
                    }
                    return subNodes[2].FindCoordinateOnOctreeRecursive(returnVector, point, depth / 2);
                case 3:
                    //ADD (N, 0, N)
                    returnVector = new Vector3(returnVector.x + depth, returnVector.y, returnVector.z + depth);
                    if (subNodes[3].IsLeaf()) {
                        return returnVector;
                    }
                    return subNodes[3].FindCoordinateOnOctreeRecursive(returnVector, point, depth / 2);
                case 4:
                    //ADD (0, N, 0)
                    returnVector = new Vector3(returnVector.x, returnVector.y + depth, returnVector.z);
                    if (subNodes[4].IsLeaf()) {
                        return returnVector;
                    }
                    return subNodes[4].FindCoordinateOnOctreeRecursive(returnVector, point, depth / 2);
                case 5:
                    //ADD (0, N, N)
                    returnVector = new Vector3(returnVector.x, returnVector.y + depth, returnVector.z + depth);
                    if (subNodes[5].IsLeaf()) {
                        return returnVector;
                    }
                    return subNodes[5].FindCoordinateOnOctreeRecursive(returnVector, point, depth / 2);
                case 6:
                    //ADD (N, N, 0)
                    returnVector = new Vector3(returnVector.x + depth, returnVector.y + depth, returnVector.z);
                    if (subNodes[6].IsLeaf()) {
                        return returnVector;
                    }
                    return subNodes[6].FindCoordinateOnOctreeRecursive(returnVector, point, depth / 2);
                case 7:
                    //ADD (N, N, N)
                    returnVector = new Vector3(returnVector.x + depth, returnVector.y + depth, returnVector.z + depth);
                    if (subNodes[7].IsLeaf()) {
                        return returnVector;
                    }
                    return subNodes[7].FindCoordinateOnOctreeRecursive(returnVector, point, depth / 2);
                default:
                    //ADD (N, N, N)
                    returnVector = new Vector3(returnVector.x + depth, returnVector.y + depth, returnVector.z + depth);
                    if (subNodes[7].IsLeaf()) {
                        return returnVector;
                    }
                    return subNodes[7].FindCoordinateOnOctreeRecursive(returnVector, point, depth / 2);
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

    }

    private int GetIndexOfPosition(Vector3 lookupPosition, Vector3 nodePosition) {
        int index = 0;
        


        return index;
    }


    public OctreeNode GetRoot() {
        return node;
    }
}


