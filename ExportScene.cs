using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 到处场景（包括物件和地形）
/// 
/// author by monitor1394@gmail.com
/// 
/// </summary>
public class ExportScene : ScriptableObject
{
    private static float boundMinX = 1000;
    private static float boundMaxX = 0;
    private static float boundMinY = 1000;
    private static float boundMaxY = 0;

    [MenuItem("ExportScene/ExportSceneToObj")]
    public static void Export()
    {
        ExportSceneToObj(false);
    }

    [MenuItem("ExportScene/ExportSceneToObj(AutoCut)")]
    public static void ExportAutoCut()
    {
        ExportSceneToObj(true);
    }

    public static void ExportSceneToObj(bool autoCut)
    {
        int vertexOffset = 0;
        StringBuilder sb = new StringBuilder();
        Terrain terrain = UnityEngine.Object.FindObjectOfType<Terrain>();
        GameObject[] objs = UnityEngine.Object.FindObjectsOfType<GameObject>();

        foreach (GameObject obj in objs)
        {
            if (obj.transform.parent == null) // root obj
            {
                MeshFilter[] mfs = obj.GetComponentsInChildren<MeshFilter>();
                if (mfs == null || mfs.Length <= 0)
                {
                    continue;
                }
                foreach (var mf in mfs)
                {
                    vertexOffset += ExportMeshToObj(sb, mf, vertexOffset);
                }
            }
        }
        if (terrain)
        {
            vertexOffset += ExportTerrianToObj(terrain.terrainData, 
                terrain.GetPosition(), 
                sb, vertexOffset, autoCut);
        }
        SaveObjToFile(sb.ToString(), 
            Application.dataPath + (autoCut?"/scene(autoCut).obj": "/scene.obj"));
    }

    private static void SaveObjToFile(string objInfo, string path)
    {
        using (StreamWriter writer = new StreamWriter(path))
        {
            writer.Write(objInfo);
        }
        Debug.Log("ExportSceneToObj SUCCESS:" + path);
    }

    private static int ExportMeshToObj(StringBuilder sb, MeshFilter mf, int vertexOffset)
    {
        Mesh mesh = mf.sharedMesh;
        foreach (Vector3 vertice in mesh.vertices)
        {
            Vector3 v = mf.transform.TransformPoint(vertice);
            if (v.x < boundMinX) boundMinX = v.x;
            if (v.x > boundMaxX) boundMaxX = v.x;
            if (v.z < boundMinY) boundMinY = v.z;
            if (v.z > boundMaxY) boundMaxY = v.z;
            sb.AppendFormat("v {0:f1} {1:f1} {2:f1}\n", -v.x, v.y, v.z);
        }
        for (int i = 0; i < mesh.subMeshCount; i++)
        {
            int[] triangles = mesh.GetTriangles(i);
            for (int j = 0; j < triangles.Length; j += 3)
            {
                sb.AppendFormat("f {1} {0} {2}\n", 
                    triangles[j] + 1 + vertexOffset, 
                    triangles[j + 1] + 1 + vertexOffset, 
                    triangles[j + 2] + 1 + vertexOffset);
            }
        }
        return mesh.vertices.Length;
    }

    private static int ExportTerrianToObj(TerrainData terrain, Vector3 terrainPos, 
        StringBuilder sb, int vertexOffset, bool autoCut)
    {
        int tw = terrain.heightmapWidth;
        int th = terrain.heightmapHeight;

        Vector3 meshScale = terrain.size;
        meshScale = new Vector3(meshScale.x / (tw - 1), meshScale.y, meshScale.z / (th - 1));

        Vector2 terrainBoundLB, terrainBoundRT;
        if (autoCut)
        {
            terrainBoundLB = GetTerrainBoundPos(new Vector3(boundMinX, 0, boundMinY), terrain, terrainPos);
            terrainBoundRT = GetTerrainBoundPos(new Vector3(boundMaxX, 0, boundMaxY), terrain, terrainPos);
        }
        else
        {
            terrainBoundLB = GetTerrainBoundPos("export/bound_lb", terrain, terrainPos);
            terrainBoundRT = GetTerrainBoundPos("export/bound_rt", terrain, terrainPos);
        }

        int bw = (int)(terrainBoundRT.x - terrainBoundLB.x);
        int bh = (int)(terrainBoundRT.y - terrainBoundLB.y);

        int w = bh != 0 && bh < th ? bh : th;
        int h = bw != 0 && bw < tw ? bw : tw;

        int startX = (int)terrainBoundLB.y;
        int startY = (int)terrainBoundLB.x;
        if (startX < 0) startX = 0;
        if (startY < 0) startY = 0;

        Debug.Log(string.Format("Terrian:tw={0},th={1},sw={2},sh={3},startX={4},startY={5}",
            tw, th, bw, bh, startX, startY));

        float[,] tData = terrain.GetHeights(0, 0, tw, th);
        Vector3[] tVertices = new Vector3[w * h];
        int[] tPolys = new int[(w - 1) * (h - 1) * 6];

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                Vector3 pos = new Vector3(-(startY + y), tData[startX + x, startY + y], (startX + x));
                tVertices[y * w + x] = Vector3.Scale(meshScale, pos) + terrainPos;
            }
        }
        int index = 0;
        for (int y = 0; y < h - 1; y++)
        {
            for (int x = 0; x < w - 1; x++)
            {
                tPolys[index++] = (y * w) + x;
                tPolys[index++] = ((y + 1) * w) + x;
                tPolys[index++] = (y * w) + x + 1;
                tPolys[index++] = ((y + 1) * w) + x;
                tPolys[index++] = ((y + 1) * w) + x + 1;
                tPolys[index++] = (y * w) + x + 1;
            }
        }
        for (int i = 0; i < tVertices.Length; i++)
        {
            sb.AppendFormat("v {0:f1} {1:f1} {2:f1}\n", tVertices[i].x, tVertices[i].y, tVertices[i].z);
        }
        for (int i = 0; i < tPolys.Length; i += 3)
        {
            int x = tPolys[i] + 1 + vertexOffset;
            int y = tPolys[i + 1] + 1 + vertexOffset;
            int z = tPolys[i + 2] + 1 + vertexOffset;
            sb.AppendFormat("f {0} {1} {2}\n", x, y, z);
        }
        return tVertices.Length;
    }

    private static Vector2 GetTerrainBoundPos(string path, TerrainData terrain, Vector3 terrainPos)
    {
        var go = GameObject.Find(path);
        if (go)
        {
            Vector3 pos = go.transform.position;
            return GetTerrainBoundPos(pos, terrain, terrainPos);
        }
        return Vector2.zero;
    }

    private static Vector2 GetTerrainBoundPos(Vector3 worldPos, TerrainData terrain, Vector3 terrainPos)
    {
        Vector3 tpos = worldPos - terrainPos;
        return new Vector2((int)(tpos.x / terrain.size.x * terrain.heightmapWidth),
            (int)(tpos.z / terrain.size.z * terrain.heightmapHeight));
    }
}
