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

    public Octree(Vector3 position, float size, int maxPointSize) {
        node = new OctreeNode(position, size, "0", this);
        this.maxPointSize = maxPointSize;
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
        /// Adds a point to the Octree and will split the tree accordingly
        /// </summary>
        /// <param name="point"></param>The point you want to add to the Octree
        /// <param name="maxPoints"></param>Maximum number of points a node should contain
        public void AddPoint(GISData.PointData point, int maxPoints) {
            int newIndex;
            if (IsLeaf()) {
                if (data.Count >= maxPoints) { //split
                    Subdivide();
                    foreach (GISData.PointData p in new List<GISData.PointData>(data)) { //move each point of data into new nodes
                        newIndex = GetIndexOfPosition(p.LocalPosition);
                        //go down path and remove old data
                        subNodes[newIndex].AddPoint(p, maxPoints);
                        data.Remove(p);
                        pointCount--;
                    }
                    newIndex = GetIndexOfPosition(point.LocalPosition);
                    //go down path for point
                    subNodes[newIndex].AddPoint(point, maxPoints);
                } else { //add
                    data.Add(point);
                    pointCount++;
                    
                }
            } else {
                newIndex = GetIndexOfPosition(point.LocalPosition);
                //go down path
                subNodes[newIndex].AddPoint(point, maxPoints);
            }
        }

        /// <summary>
        /// Expands the tree as if the point as actually added
        /// </summary>
        /// <param name="point"></param>Point to simulate expansion
        /// <param name="maxPoints"></param>Maximum number of points in a node
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
        /// <param name="i"></param>Index to split the Octree at
        /// <returns></returns>
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
            return new OctreeNode(newPos, size * 0.5f, (index + i.ToString()), tree);
        }

        /// <summary>
        /// Split Octree leaf
        /// </summary>
        void Subdivide() {
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


