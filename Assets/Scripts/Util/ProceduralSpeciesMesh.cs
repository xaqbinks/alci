using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public static class ProceduralSpeciesMesh
{
    public static Mesh GenerateMesh(SpeciesData species)
    {
        Mesh mesh;

        // Crystalline species get a unique, faceted mesh.
        if (species.biology == BiologicalClass.Silicon_Crystalline)
        {
            float radius = 0.5f + (species.GetCurrentCombatStrength() - 1.0f) * 0.1f;
            mesh = CreateSubdividedCube(radius, 2);
            FacetMesh(ref mesh);
        }
        else // All other (organic) species are generated with Marching Cubes
        {
            mesh = GenerateOrganicMesh(species);
        }

        // Color the mesh based on the species' dominant ethic.
        Color[] colors = new Color[mesh.vertexCount];
        Color speciesColor = GetColorFromEthics(species);
        for (int i = 0; i < colors.Length; i++) colors[i] = speciesColor;
        mesh.colors = colors;

        return mesh;
    }

    private static Mesh GenerateOrganicMesh(SpeciesData species)
    {
        int gridSize = 20;
        float[,,] grid = new float[gridSize, gridSize, gridSize];
        float isoLevel = 0.5f;
        float noiseScale = 3f + species.aggressionFactor * 2f;
        float size = 0.5f + (species.GetCurrentCombatStrength() - 1.0f) * 0.1f;
        Vector3 offset = new Vector3(Random.value * 100, Random.value * 100, Random.value * 100);

        for (int x = 0; x < gridSize; x++)
        for (int y = 0; y < gridSize; y++)
        for (int z = 0; z < gridSize; z++)
        {
            float px = ((float)x / gridSize - 0.5f) * 2f;
            float py = ((float)y / gridSize - 0.5f) * 2f;
            float pz = ((float)z / gridSize - 0.5f) * 2f;
            float distance = px*px + py*py + pz*pz;
            float baseValue = 1.0f - distance;
            float noise = Fbm(new Vector3(x, y, z) * noiseScale + offset, 4, 2.0f, 0.5f);
            grid[x, y, z] = baseValue + noise * 0.2f;
        }

        Mesh mesh = MarchingCubes.GenerateMesh(grid, isoLevel);
        Vector3[] vertices = mesh.vertices;
        Vector3 center = new Vector3((gridSize-1)/2f, (gridSize-1)/2f, (gridSize-1)/2f);
        for(int i=0; i<vertices.Length; i++) { vertices[i] = (vertices[i] - center) * size; }
        mesh.vertices = vertices;
        mesh.RecalculateBounds();
        mesh.RecalculateNormals();
        return mesh;
    }

    private static void FacetMesh(ref Mesh mesh)
    {
        Vector3[] oldVerts = mesh.vertices; int[] triangles = mesh.triangles;
        Vector3[] newVerts = new Vector3[triangles.Length];
        for (int i = 0; i < triangles.Length; i++) { newVerts[i] = oldVerts[triangles[i]]; triangles[i] = i; }
        mesh.vertices = newVerts; mesh.triangles = triangles;
        mesh.RecalculateNormals();
    }

    private static float Fbm(Vector3 p, int oct, float lac, float per)
    {
        float t=0, f=1, a=1, mv=0;
        for(int i=0;i<oct;i++){t+=Mathf.PerlinNoise(p.x*f,p.y*f)*a; mv+=a; a*=per; f*=lac;}
        return t/mv;
    }

    private static Color GetColorFromEthics(SpeciesData species)
    {
        if(species.ethicScores==null||species.ethicScores.Count==0)return Color.white;
        return new Color(species.ethicScores[Ethic.Militarist], species.ethicScores[Ethic.Erudite], species.ethicScores[Ethic.Pacifist]);
    }

    private static Dictionary<long, int> midpointCache;
    private static int GetMidpoint(int p1, int p2, ref List<Vector3> vertices)
    {
        long sI=p1<p2?p1:p2,gI=p1<p2?p2:p1,k=(sI<<32)+gI;
        if(midpointCache.TryGetValue(k,out int ret))return ret;
        Vector3 m=(vertices[p1]+vertices[p2])*0.5f;
        vertices.Add(m.normalized);
        midpointCache.Add(k,vertices.Count-1);
        return vertices.Count-1;
    }

    private static Mesh CreateIcosphere(Vector3 radius, int subdivisionLevel)
    {
        Mesh mesh = new Mesh();
        midpointCache = new Dictionary<long, int>();
        List<Vector3> vertices = new List<Vector3>();
        float t = (1f + Mathf.Sqrt(5f)) / 2f;
        vertices.Add(new Vector3(-1,t,0).normalized); vertices.Add(new Vector3(1,t,0).normalized);
        vertices.Add(new Vector3(-1,-t,0).normalized); vertices.Add(new Vector3(1,-t,0).normalized);
        vertices.Add(new Vector3(0,-1,t).normalized); vertices.Add(new Vector3(0,1,t).normalized);
        vertices.Add(new Vector3(0,-1,-t).normalized); vertices.Add(new Vector3(0,1,-t).normalized);
        vertices.Add(new Vector3(t,0,-1).normalized); vertices.Add(new Vector3(t,0,1).normalized);
        vertices.Add(new Vector3(-t,0,-1).normalized); vertices.Add(new Vector3(-t,0,1).normalized);
        List<int> triangles = new List<int>{0,11,5,0,5,1,0,1,7,0,7,10,0,10,11,1,5,9,5,11,4,11,10,2,10,7,6,7,1,8,3,9,4,3,4,2,3,2,6,3,6,8,3,8,9,4,9,5,2,4,11,6,2,10,8,6,7,9,8,1};
        for (int i=0;i<subdivisionLevel;i++){
            var newTris = new List<int>();
            for(int j=0;j<triangles.Count;j+=3){
                int v1=triangles[j], v2=triangles[j+1], v3=triangles[j+2];
                int a=GetMidpoint(v1,v2,ref vertices), b=GetMidpoint(v2,v3,ref vertices), c=GetMidpoint(v3,v1,ref vertices);
                newTris.AddRange(new int[]{v1,a,c, v2,b,a, v3,c,b, a,b,c});
            }
            triangles=newTris;
        }
        for (int i=0;i<vertices.Count;i++) vertices[i]=Vector3.Scale(vertices[i],radius);
        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.RecalculateNormals();
        return mesh;
    }

    private static Mesh CreateSubdividedCube(float radius, int subdivisions)
    {
        Mesh mesh = new Mesh();
        midpointCache = new Dictionary<long, int>();
        List<Vector3> vertices = new List<Vector3>{new Vector3(-1,-1,-1),new Vector3(1,-1,-1),new Vector3(1,1,-1),new Vector3(-1,1,-1),new Vector3(-1,-1,1),new Vector3(1,-1,1),new Vector3(1,1,1),new Vector3(-1,1,1)};
        for(int i=0;i<vertices.Count;i++) vertices[i]=vertices[i].normalized;
        List<int> triangles = new List<int>{0,2,1,0,3,2,2,3,7,2,7,6,1,2,6,1,6,5,0,1,5,0,5,4,0,4,7,0,7,3,4,5,6,4,6,7};
        for (int i=0;i<subdivisions;i++){
            var newTris = new List<int>();
            for(int j=0;j<triangles.Count;j+=3){
                int v1=triangles[j], v2=triangles[j+1], v3=triangles[j+2];
                int a=GetMidpoint(v1,v2,ref vertices),b=GetMidpoint(v2,v3,ref vertices),c=GetMidpoint(v3,v1,ref vertices);
                newTris.AddRange(new int[]{v1,a,c, v2,b,a, v3,c,b, a,b,c});
            }
            triangles=newTris;
        }
        for(int i=0;i<vertices.Count;i++) vertices[i]*=radius;
        mesh.vertices=vertices.ToArray();
        mesh.triangles=triangles.ToArray();
        mesh.RecalculateNormals();
        return mesh;
    }
}
